using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Npgsql;
using Robust.Shared.Network;

namespace Content.Server.Database;

public sealed record GovernanceCourtEscalationInfo(long CourtCaseId, bool Created);

public partial interface IServerDbManager
{
    Task<GovernanceCourtEscalationInfo?> EscalateGovernanceIncidentToCourtAsync(
        long ticketId,
        NetUserId responder,
        int roundId,
        string reason,
        CancellationToken cancel = default);
}

public sealed partial class ServerDbManager
{
    public async Task<GovernanceCourtEscalationInfo?> EscalateGovernanceIncidentToCourtAsync(
        long ticketId,
        NetUserId responder,
        int roundId,
        string reason,
        CancellationToken cancel = default)
    {
        if (!_cfg.GetCVar(CCVars.DatabaseEngine).Equals("postgres", StringComparison.OrdinalIgnoreCase) ||
            ticketId <= 0 || roundId <= 0)
            return null;

        reason = reason.Trim();
        if (reason.Length is < 10 or > 1500)
            return null;

        await using var connection = CreateGovernanceConnection();
        await connection.OpenAsync(cancel);
        await using var transaction = await connection.BeginTransactionAsync(cancel);

        await using (var incidentLock = new NpgsqlCommand(
                         "SELECT pg_advisory_xact_lock(hashtextextended('rucm-incident-court', @ticket_id))",
                         connection,
                         transaction))
        {
            incidentLock.Parameters.AddWithValue("ticket_id", ticketId);
            await incidentLock.ExecuteNonQueryAsync(cancel);
        }

        await using var command = new NpgsqlCommand(
            """
            WITH actor AS (
                SELECT users.id
                FROM governance.users AS users
                JOIN governance.duty_sessions AS duty ON duty.user_id = users.id
                JOIN governance.capability_grants AS capability_grant
                  ON capability_grant.user_id = users.id
                 AND capability_grant.source_type = 'duty_session'
                 AND capability_grant.source_id = duty.id::text
                WHERE users.ss14_user_id = @responder
                  AND NOT users.is_governance_suspended
                  AND duty.round_id = @round_id
                  AND duty.status = 'active'
                  AND duty.observer_confirmed
                  AND duty.expires_at > now()
                  AND capability_grant.capability = 'moderation.ahelp'
                  AND capability_grant.expires_at > now()
                  AND capability_grant.revoked_at IS NULL
                LIMIT 1
            ), incident AS (
                SELECT live.id,
                       live.target_user_id,
                       live.reporter_user_id,
                       live.summary,
                       live.type,
                       live.status,
                       live.court_case_id,
                       ticket.id AS ticket_id,
                       actor.id AS actor_id
                FROM governance.live_incidents AS live
                JOIN governance.ahelp_tickets AS ticket ON ticket.id = live.ahelp_ticket_id
                CROSS JOIN actor
                WHERE ticket.id = @ticket_id
                  AND ticket.round_id = @round_id
                  AND ticket.claimed_by_user_id = actor.id
                  AND live.round_id = @round_id
                  AND live.status IN ('active', 'contained', 'escalated_to_court')
                LIMIT 1
            ), existing AS (
                SELECT court_case_id AS id
                FROM incident
                WHERE court_case_id IS NOT NULL
            ), case_source AS (
                SELECT incident.*,
                       CASE
                           WHEN incident.reporter_user_id IS NOT NULL
                            AND incident.reporter_user_id <> incident.target_user_id
                               THEN incident.reporter_user_id
                           ELSE incident.actor_id
                       END AS claimant_user_id
                FROM incident
                WHERE incident.court_case_id IS NULL
            ), created_case AS (
                INSERT INTO governance.court_cases(
                    claimant_user_id, defendant_user_id, round_id, summary,
                    status, filed_at, defense_deadline, version)
                SELECT case_source.claimant_user_id,
                       case_source.target_user_id,
                       @round_id,
                       left(
                           'LiveIncident #' || case_source.id::text || ' (' || case_source.type || ')\n' ||
                           case_source.summary || '\n\nПередано дежурным в Community Court: ' || @reason,
                           1500),
                       'defense', now(), now() + interval '48 hours', 0
                FROM case_source
                RETURNING id, claimant_user_id, defendant_user_id
            ), complaint AS (
                INSERT INTO governance.court_statements(
                    case_id, author_user_id, kind, body, evidence_reference, created_at)
                SELECT created_case.id,
                       created_case.claimant_user_id,
                       'complaint',
                       left(case_source.summary || '\n\nОснование передачи в суд: ' || @reason, 3000),
                       'RUCM Governance: LiveIncident #' || case_source.id::text ||
                           ', AHelp #' || case_source.ticket_id::text ||
                           '. Исходная переписка и аудит сохранены в PostgreSQL.',
                       now()
                FROM created_case
                CROSS JOIN case_source
            ), participants AS (
                INSERT INTO governance.court_participants(case_id, user_id, role, added_at)
                SELECT created_case.id, created_case.claimant_user_id, 'claimant', now()
                FROM created_case
                UNION ALL
                SELECT created_case.id, created_case.defendant_user_id, 'defendant', now()
                FROM created_case
                ON CONFLICT (case_id, user_id) DO NOTHING
            ), linked AS (
                UPDATE governance.live_incidents AS live
                SET status = 'escalated_to_court',
                    court_case_id = created_case.id
                FROM created_case, case_source
                WHERE live.id = case_source.id
                RETURNING live.id, live.court_case_id
            ), audited AS (
                INSERT INTO governance.audit_events(
                    event_type, actor_type, actor_id, target_type, target_id,
                    entity_type, entity_id, payload)
                SELECT 'incident.escalated_to_court', 'ss14_user', @responder::text,
                       'ss14_user', target.ss14_user_id::text,
                       'court_case', linked.court_case_id::text,
                       jsonb_build_object(
                           'round_id', @round_id,
                           'ticket_id', @ticket_id,
                           'incident_id', linked.id,
                           'reason', @reason)
                FROM linked
                JOIN case_source ON case_source.id = linked.id
                JOIN governance.users AS target ON target.id = case_source.target_user_id
            ), selected AS (
                SELECT existing.id, false AS created
                FROM existing
                UNION ALL
                SELECT linked.court_case_id AS id, true AS created
                FROM linked
                LIMIT 1
            )
            SELECT id, created FROM selected
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("ticket_id", ticketId);
        command.Parameters.AddWithValue("responder", responder.UserId);
        command.Parameters.AddWithValue("round_id", roundId);
        command.Parameters.AddWithValue("reason", reason);

        await using var reader = await command.ExecuteReaderAsync(cancel);
        if (!await reader.ReadAsync(cancel))
        {
            await transaction.RollbackAsync(cancel);
            return null;
        }

        var result = new GovernanceCourtEscalationInfo(reader.GetInt64(0), reader.GetBoolean(1));
        await reader.CloseAsync();
        await transaction.CommitAsync(cancel);
        return result;
    }
}
