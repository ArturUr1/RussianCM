using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot.Governance;

public sealed class CandidateSelectionService(
    Func<GovernanceDbContext> governanceFactory,
    Func<ServerDbContext> gameFactory,
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
        var average = await governance.Users.AverageAsync(value => (double?) value.CivicRatingCache) ?? 0;
        var effectiveMinimumQualification = config?.CourtTestMode == true && track == "event"
            ? (short) Math.Max(minimumQualification, 4)
            : minimumQualification;
        var qualified = governance.Users.AsNoTracking()
            .Join(governance.Qualifications.Where(value => value.Track == track && value.Level >= effectiveMinimumQualification),
                user => user.Id,
                qualification => qualification.UserId,
                (user, _) => user)
            .Where(user => !user.IsGovernanceSuspended && !excludedUsers.Contains(user.Id));
        var bypassRatingForLocalTest = config?.CourtTestMode == true && track is "jury" or "event";
        if (aboveAverage && !bypassRatingForLocalTest)
            qualified = qualified.Where(user => user.CivicRatingCache > average);

        var candidates = await qualified.ToListAsync();
        if (availableDiscordIds != null)
        {
            // Synthetic SS14-only Governance identities deliberately use negative internal Discord ids.
            // They are valid court participants, but they can never receive a Discord jury invitation.
            candidates = candidates
                .Where(user => user.DiscordUserId > 0 && availableDiscordIds.Contains((ulong) user.DiscordUserId))
                .ToList();
        }

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

        return candidates
            .Where(value => !banned.Contains(value.Ss14UserId))
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .ToArray();
    }
}
