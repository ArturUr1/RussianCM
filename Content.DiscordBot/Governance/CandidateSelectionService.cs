using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot.Governance;

public sealed class CandidateSelectionService(
    Func<GovernanceDbContext> governanceFactory,
    Func<ServerDbContext> gameFactory,
    ReputationService? reputation = null,
    Config? config = null)
{
    public async Task<IReadOnlyList<GovernanceUser>> SelectAsync(
        string track,
        short minimumQualification,
        string entityType,
        string entityId,
        int count,
        IReadOnlyCollection<Guid> excludedUsers,
        IReadOnlySet<ulong>? availableDiscordIds,
        TimeSpan cooldown,
        bool aboveAverage = true)
    {
        if (count <= 0)
            return [];

        await using var governance = governanceFactory();
        var now = DateTime.UtcNow;
        var effectiveMinimumQualification = config?.CourtTestMode == true && track == ReputationTracks.Event
            ? Math.Max(minimumQualification, (short) 4)
            : minimumQualification;

        var qualified = governance.Users.AsNoTracking()
            .Join(governance.Qualifications.AsNoTracking()
                    .Where(value => value.Track == track && value.Level >= effectiveMinimumQualification),
                user => user.Id,
                qualification => qualification.UserId,
                (user, qualification) => new { User = user, qualification.Level })
            .Join(governance.ServicePaths.AsNoTracking().Where(value => value.Track == track),
                row => row.User.Id,
                path => path.UserId,
                (row, _) => row)
            .Where(row => !row.User.IsGovernanceSuspended && !excludedUsers.Contains(row.User.Id));

        // Jury/event/moderation review work is currently delivered through Discord. SS14-only users
        // keep their reputation and can use in-game Governance, but cannot be selected for a DM-only role.
        if (track is ReputationTracks.Jury or ReputationTracks.Event or ReputationTracks.Moderation)
            qualified = qualified.Where(row => row.User.DiscordUserId != null && row.User.DiscordUserId > 0);

        var qualifiedRows = await qualified.ToListAsync();
        var candidates = qualifiedRows.Select(value => value.User).ToList();
        var qualificationLevels = qualifiedRows
            .GroupBy(value => value.User.Id)
            .ToDictionary(group => group.Key, group => group.Max(value => value.Level));

        if (availableDiscordIds != null)
        {
            candidates = candidates
                .Where(user => user.DiscordUserId is > 0 && availableDiscordIds.Contains(checked((ulong) user.DiscordUserId.Value)))
                .ToList();
        }

        if (candidates.Count == 0)
            return [];

        var candidateIds = candidates.Select(value => value.Id).ToArray();
        var conflicts = await governance.Conflicts.AsNoTracking()
            .Where(value => value.EndsAt == null || value.EndsAt > now)
            .Where(value => candidateIds.Contains(value.UserId))
            .Where(value =>
                value.EntityType == entityType && value.EntityId == entityId ||
                value.RelatedUserId != null && excludedUsers.Contains(value.RelatedUserId.Value))
            .Select(value => value.UserId)
            .ToListAsync();

        var friendships = await governance.Friendships.AsNoTracking()
            .Where(value => value.ConfirmedAt != null)
            .Where(value => candidateIds.Contains(value.UserId) && excludedUsers.Contains(value.FriendUserId) ||
                            candidateIds.Contains(value.FriendUserId) && excludedUsers.Contains(value.UserId))
            .Select(value => candidateIds.Contains(value.UserId) ? value.UserId : value.FriendUserId)
            .ToListAsync();

        var recent = await governance.ServiceAssignments.AsNoTracking()
            .Where(value => candidateIds.Contains(value.UserId) && value.Track == track && value.AssignedAt > now - cooldown)
            .Select(value => value.UserId)
            .ToListAsync();
        var pending = await governance.Invitations.AsNoTracking()
            .Where(value => candidateIds.Contains(value.UserId) && value.State == InvitationStates.Pending)
            .Select(value => value.UserId)
            .ToListAsync();
        var activeDuty = await governance.Database.SqlQuery<Guid>($"""
                SELECT user_id AS "Value" FROM governance.duty_sessions
                WHERE status = 'active' AND expires_at > now()
                UNION
                SELECT director_user_id AS "Value" FROM governance.event_sessions
                WHERE status = 'active' AND expires_at > now()
                """).ToListAsync();

        var unavailable = conflicts.Concat(friendships).Concat(recent).Concat(pending).Concat(activeDuty).ToHashSet();
        candidates = candidates.Where(value => !unavailable.Contains(value.Id)).ToList();
        if (candidates.Count == 0)
            return [];

        var playerIds = candidates.Select(value => value.Ss14UserId).ToArray();
        await using var game = gameFactory();
        var banned = await game.Ban.AsNoTracking()
            .Where(value => value.PlayerUserId != null && playerIds.Contains(value.PlayerUserId.Value))
            .Where(value => !value.Hidden && value.Unban == null && (value.ExpirationTime == null || value.ExpirationTime > now))
            .Select(value => value.PlayerUserId!.Value)
            .Concat(game.RoleBan.AsNoTracking()
                .Where(value => value.PlayerUserId != null && playerIds.Contains(value.PlayerUserId.Value))
                .Where(value => !value.Hidden && value.Unban == null && (value.ExpirationTime == null || value.ExpirationTime > now))
                .Select(value => value.PlayerUserId!.Value))
            .Distinct()
            .ToListAsync();
        candidates = candidates.Where(value => !banned.Contains(value.Ss14UserId)).ToList();
        if (candidates.Count == 0)
            return [];

        if (reputation == null)
            return candidates.OrderBy(_ => Guid.NewGuid()).Take(count).ToArray();

        await reputation.RefreshUsersAsync(candidates.Select(value => value.Id));
        var remainingIds = candidates.Select(value => value.Id).ToArray();
        var snapshots = await governance.ReputationSnapshots.AsNoTracking()
            .Where(value => remainingIds.Contains(value.UserId) &&
                            (value.Track == track || value.Track == ReputationTracks.General))
            .ToListAsync();

        var byUser = snapshots.GroupBy(value => value.UserId)
            .ToDictionary(group => group.Key, group => group.ToDictionary(value => value.Track, StringComparer.Ordinal));

        // `aboveAverage` is intentionally ignored in Reputation v2. It is kept in the signature for
        // source compatibility. Thompson Sampling balances proven reliability with exploration of
        // newer candidates instead of forming a permanent above-average caste.
        return candidates
            .Select(user =>
            {
                byUser.TryGetValue(user.Id, out var userSnapshots);
                userSnapshots?.TryGetValue(track, out var trackSnapshot);
                userSnapshots?.TryGetValue(ReputationTracks.General, out var generalSnapshot);
                var alpha = trackSnapshot?.Alpha ?? ReputationPolicy.TrackPriorStrength * 0.5;
                var beta = trackSnapshot?.Beta ?? ReputationPolicy.TrackPriorStrength * 0.5;
                var thompson = ReputationMath.SampleBeta(alpha, beta);
                var generalFactor = 0.85 + 0.30 * ((generalSnapshot?.Score ?? ReputationPolicy.NeutralScore) / 1000.0);
                var qualificationFactor = 1.0 + 0.03 * Math.Max(0, qualificationLevels.GetValueOrDefault(user.Id, (short) 1) - 1);
                return (User: user, Priority: thompson * generalFactor * qualificationFactor);
            })
            .OrderByDescending(value => value.Priority)
            .Take(count)
            .Select(value => value.User)
            .ToArray();
    }
}
