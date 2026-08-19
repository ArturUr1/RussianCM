using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Npgsql;
using Robust.Shared.Network;

namespace Content.Server.Database;

public sealed record GovernanceAHelpIncidentInfo(
    long Id,
    NetUserId TargetUserId,
    string TargetName,
    string Type);

public partial interface IServerDbManager
{
    Task<GovernanceAHelpIncidentInfo?> CreateGovernanceAHelpIncidentAsync(
        long ticketId,
        NetUserId responder,
        NetUserId target,
        string targetName,
        int roundId,
        string type,
        CancellationToken cancel = default);

    Task<GovernanceAHelpIncidentInfo?> GetGovernanceAHelpIncidentAsync(
        long ticketId,
        NetUserId responder,
        int roundId,
        CancellationToken cancel = default);
}

public sealed partial class ServerDbManager
{
    public async Task<GovernanceAHelpIncidentInfo?> CreateGovernanceAHelpIncidentAsync(
        long ticketId,
        NetUserId responder,
        NetUserId target,
        string targetName,
        int roundId,
        string type,
        CancellationToken cancel = default)
    {
        if (!_cfg.GetCVar(CCVars.DatabaseEngine).Equals("postgres", StringComparison.OrdinalIgnoreCase) ||
            ticketId <= 0 || roundId <= 0 || responder == target)
            return null;

        type = type.Trim();
        if (type.Length is < 2 or > 64)
            return null;

        targetName = targetName.Trim();
        if (targetName.Length == 0)
            targetName = target.ToString();
        if (targetName.Length > 128)
            targetName = targetName[..128];

        await using var connection = CreateGovernanceConnection();
        await connection.OpenAsync(cancel);
        await using var transaction = await connection.BeginTransactionAsync(cancel);

        await using (var incidentLock = new NpgsqlCommand(
                         "SELECT pg_advisory_xact_lock(hashtextextended('rucm-ahelp-incident', @ticket_id))",
                         connection,
                         transaction))
        {
            incidentLock.Parameters.AddWithValue("ticket_id", ticketId);
            await incidentLock.ExecuteNonQueryAsync(cancel);
        }

        // Live moderation must also work for players who have never linked Discord. Governance uses
        // an internal negative Discord id for those SS14-only identities; the migration upgrades the
        // same row when the player links a real Discord account later.
        await using (var ensureTarget = new NpgsqlCommand(
                         """
                         INSERT INTO governance.users(ss14_user_id, discord_user_id, created_at, updated_at)
                         VALUES (
                             @target,
                             -((('x' || substr(md5(@target::text), 1, 15))::bit(60)::bigint) + 1),
                             now(), now())
                         ON CONFLICT (ss14_user_id) DO NOTHING
                         """,
                         connection,
                         transaction))
        {
            ensureTarget.Parameters.AddWithValue("target", target.UserId);
            await ensureTarget.ExecuteNonQueryAsync(cancel);
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
                  AND capability_grant.capability = 'moderation.freeze'
                  AND capability_grant.expires_at > now()
                  AND capability_grant.revoked_at IS NULL
                LIMIT 1
            ), target_user AS (
                SELECT id, ss14_user_id
                FROM governance.users
                WHERE ss14_user_id = @target
                LIMIT 1
            ), ticket AS (
                UPDATE governance.ahelp_tickets AS ticket
                SET target_user_id = target_user.id,
                    updated_at = now()
                FROM actor, target_user
                WHERE ticket.id = @ticket_id
                  AND ticket.round_id = @round_id
                  AND ticket.claimed_by_user_id = actor.id
                  AND ticket.status IN ('claimed', 'waiting_player')
                RETURNING ticket.id,
                          ticket.reporter_user_id,
                          ticket.summary,
                          actor.id AS actor_id,
                          target_user.id AS target_user_id,
                          target_user.ss14_user_id AS target_ss14_user_id
            ), created AS (
                INSERT INTO governance.live_incidents(
                    round_id, target_user_id, reporter_user_id, created_by_user_id,
                    type, summary, status, created_at, ahelp_ticket_id)
                SELECT @round_id, ticket.target_user_id, ticket.reporter_user_id, ticket.actor_id,
                       @type, ticket.summary, 'active', now(), ticket.id
                FROM ticket
                ON CONFLICT (ahelp_ticket_id) WHERE ahelp_ticket_id IS NOT NULL
                DO NOTHING
                RETURNING id, target_user_id, type
            ), selected AS (
                SELECT created.id, created.target_user_id, created.type
                FROM created
                UNION ALL
                SELECT incident.id, incident.target_user_id, incident.type
                FROM governance.live_incidents AS incident
                JOIN ticket ON ticket.id = incident.ahelp_ticket_id
                WHERE NOT EXISTS (SELECT 1 FROM created)
                LIMIT 1
            ), audited AS (
                INSERT INTO governance.audit_events(
                    event_type, actor_type, actor_id, target_type, target_id,
                    entity_type, entity_id, payload)
                SELECT 'incident.created_from_ahelp', 'ss14_user', @responder::text,
                       'ss14_user', @target::text,
                       'live_incident', selected.id::text,
                       jsonb_build_object(
                           'round_id', @round_id,
                           'ticket_id', @ticket_id,
                           'type', @type,
                           'target_name', @target_name)
                FROM selected
                WHERE NOT EXISTS (
                    SELECT 1 FROM governance.audit_events AS old
                    WHERE old.event_type = 'incident.created_from_ahelp'
                      AND old.entity_type = 'live_incident'
                      AND old.entity_id = selected.id::text)
            )
            SELECT selected.id,
                   target_user.ss14_user_id,
                   @target_name,
                   selected.type
            FROM selected
            JOIN target_user ON target_user.id = selected.target_user_id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("ticket_id", ticketId);
        command.Parameters.AddWithValue("responder", responder.UserId);
        command.Parameters.AddWithValue("target", target.UserId);
        command.Parameters.AddWithValue("target_name", targetName);
        command.Parameters.AddWithValue("round_id", roundId);
        command.Parameters.AddWithValue("type", type);

        await using var reader = await command.ExecuteReaderAsync(cancel);
        if (!await reader.ReadAsync(cancel))
        {
            await transaction.RollbackAsync(cancel);
            return null;
        }

        var result = new GovernanceAHelpIncidentInfo(
            reader.GetInt64(0),
            new NetUserId(reader.GetGuid(1)),
            reader.GetString(2),
            reader.GetString(3));
        await reader.CloseAsync();
        await transaction.CommitAsync(cancel);
        return result;
    }

    public async Task<GovernanceAHelpIncidentInfo?> GetGovernanceAHelpIncidentAsync(
        long ticketId,
        NetUserId responder,
        int roundId,
        CancellationToken cancel = default)
    {
        if (!_cfg.GetCVar(CCVars.DatabaseEngine).Equals("postgres", StringComparison.OrdinalIgnoreCase) ||
            ticketId <= 0 || roundId <= 0)
            return null;

        await using var connection = CreateGovernanceConnection();
        await connection.OpenAsync(cancel);
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
            )
            SELECT incident.id,
                   target.ss14_user_id,
                   COALESCE(player.last_seen_user_name, target.ss14_user_id::text),
                   incident.type
            FROM governance.live_incidents AS incident
            JOIN governance.ahelp_tickets AS ticket ON ticket.id = incident.ahelp_ticket_id
            JOIN actor ON actor.id = ticket.claimed_by_user_id
            JOIN governance.users AS target ON target.id = incident.target_user_id
            LEFT JOIN player ON player.user_id = target.ss14_user_id
            WHERE ticket.id = @ticket_id
              AND ticket.round_id = @round_id
              AND incident.status IN ('active', 'contained')
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("ticket_id", ticketId);
        command.Parameters.AddWithValue("responder", responder.UserId);
        command.Parameters.AddWithValue("round_id", roundId);

        await using var reader = await command.ExecuteReaderAsync(cancel);
        if (!await reader.ReadAsync(cancel))
            return null;

        return new GovernanceAHelpIncidentInfo(
            reader.GetInt64(0),
            new NetUserId(reader.GetGuid(1)),
            reader.GetString(2),
            reader.GetString(3));
    }
}
