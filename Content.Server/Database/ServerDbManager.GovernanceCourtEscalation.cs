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

        // A court claimant is always the player who opened the source AHelp. AHelp is available to
        // SS14-only accounts, so create the same upgradeable synthetic Governance identity that live
        // moderation uses for unlinked targets and bind it back to the ticket before filing the case.
        await using (var ensureReporter = new NpgsqlCommand(
                         """
                         WITH source AS (
                             SELECT reporter_ss14_user_id
                             FROM governance.ahelp_tickets
                             WHERE id = @ticket_id AND round_id = @round_id
                         ), inserted AS (
                             INSERT INTO governance.users(ss14_user_id, discord_user_id, created_at, updated_at)
                             SELECT reporter_ss14_user_id,
                                    -((('x' || substr(md5(reporter_ss14_user_id::text), 1, 15))::bit(60)::bigint) + 1),
                                    now(), now()
                             FROM source
                             ON CONFLICT (ss14_user_id) DO NOTHING
                         )
                         UPDATE governance.ahelp_tickets AS ticket
                         SET reporter_user_id = users.id,
                             updated_at = now()
                         FROM governance.users AS users
                         WHERE ticket.id = @ticket_id
                           AND ticket.round_id = @round_id
                           AND users.ss14_user_id = ticket.reporter_ss14_user_id
                           AND ticket.reporter_user_id IS DISTINCT FROM users.id
                         """,
                         connection,
                         transaction))
        {
            ensureReporter.Parameters.AddWithValue("ticket_id", ticketId);
            ensureReporter.Parameters.AddWithValue("round_id", roundId);
            await ensureReporter.ExecuteNonQueryAsync(cancel);
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
                       COALESCE(live.reporter_user_id, ticket.reporter_user_id) AS reporter_user_id,
                       live.summary,
                       live.type,
                       live.status,
                       live.court_case_id,
                       COALESCE(live.target_character_name, '') AS target_character_name,
                       ticket.id AS ticket_id,
                       ticket.reporter_ss14_user_id,
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
                       target.ss14_user_id AS target_ss14_user_id,
                       COALESCE(target_player.last_seen_user_name, target.ss14_user_id::text) AS target_name,
                       reporter.ss14_user_id AS claimant_ss14_user_id,
                       COALESCE(reporter_player.last_seen_user_name, reporter.ss14_user_id::text) AS claimant_name,
                       incident.reporter_user_id AS claimant_user_id
                FROM incident
                JOIN governance.users AS target ON target.id = incident.target_user_id
                JOIN governance.users AS reporter ON reporter.id = incident.reporter_user_id
                LEFT JOIN player AS target_player ON target_player.user_id = target.ss14_user_id
                LEFT JOIN player AS reporter_player ON reporter_player.user_id = reporter.ss14_user_id
                WHERE incident.court_case_id IS NULL
                  AND incident.reporter_user_id IS NOT NULL
                  AND incident.reporter_user_id <> incident.target_user_id
            ), created_case AS (
                INSERT INTO governance.court_cases(
                    claimant_user_id, defendant_user_id, round_id, summary,
                    status, filed_at, defense_deadline, version)
                SELECT case_source.claimant_user_id,
                       case_source.target_user_id,
                       @round_id,
                       left(
                           'LiveIncident #' || case_source.id::text || ' (' || case_source.type || ')' || chr(10) ||
                           'Заявитель: ' || case_source.claimant_name || ' • SS14 ' || case_source.claimant_ss14_user_id::text || chr(10) ||
                           'Ответчик: ' || case_source.target_name ||
                           CASE WHEN case_source.target_character_name <> ''
                               THEN ' • персонаж: ' || case_source.target_character_name ELSE '' END ||
                           ' • SS14 ' || case_source.target_ss14_user_id::text || chr(10) ||
                           case_source.summary || chr(10) || chr(10) ||
                           'Передано дежурным в Community Court: ' || @reason,
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
                       left(
                           case_source.summary || chr(10) || chr(10) ||
                           'Основание передачи в суд: ' || @reason,
                           3000),
                       'RUCM Governance: LiveIncident #' || case_source.id::text ||
                           ', AHelp #' || case_source.ticket_id::text ||
                           ', ответчик ' || case_source.target_name ||
                           CASE WHEN case_source.target_character_name <> ''
                               THEN ' / ' || case_source.target_character_name ELSE '' END ||
                           ' (SS14 ' || case_source.target_ss14_user_id::text || ').',
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
                    reporter_user_id = case_source.claimant_user_id,
                    court_case_id = created_case.id
                FROM created_case, case_source
                WHERE live.id = case_source.id
                RETURNING live.id, live.court_case_id
            ), audited AS (
                INSERT INTO governance.audit_events(
                    event_type, actor_type, actor_id, target_type, target_id,
                    entity_type, entity_id, payload)
                SELECT 'incident.escalated_to_court', 'ss14_user', @responder::text,
                       'ss14_user', case_source.target_ss14_user_id::text,
                       'court_case', linked.court_case_id::text,
                       jsonb_build_object(
                           'round_id', @round_id,
                           'ticket_id', @ticket_id,
                           'incident_id', linked.id,
                           'reason', @reason,
                           'claimant_ss14_user_id', case_source.claimant_ss14_user_id,
                           'target_name', case_source.target_name,
                           'target_character_name', case_source.target_character_name)
                FROM linked
                JOIN case_source ON case_source.id = linked.id
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
