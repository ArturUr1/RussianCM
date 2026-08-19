using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot.Governance;

public sealed class ModerationQualificationService(
    Func<GovernanceDbContext> governanceFactory,
    ModerationTrustService trust)
{
    public async Task<int> ReconcileAsync()
    {
        List<(Guid UserId, ulong DiscordId, short Level)> users;
        await using (var governance = governanceFactory())
        {
            var rows = await governance.Users.AsNoTracking()
                .Join(governance.Qualifications.AsNoTracking().Where(value => value.Track == "moderation"),
                    user => user.Id,
                    qualification => qualification.UserId,
                    (user, qualification) => new
                    {
                        user.Id,
                        user.DiscordUserId,
                        qualification.Level,
                        user.IsGovernanceSuspended,
                    })
                .Where(value => !value.IsGovernanceSuspended && value.DiscordUserId > 0)
                .ToListAsync();

            // governance.users may contain synthetic/test identities represented by non-positive
            // signed bigint values. They are not Discord snowflakes and must never participate in
            // Discord-backed qualification reconciliation.
            users = rows
                .Select(value => (value.Id, (ulong) value.DiscordUserId, value.Level))
                .ToList();
        }

        var changed = 0;
        foreach (var (userId, discordId, currentLevel) in users)
        {
            ModerationTrustProfile profile;
            try
            {
                profile = await trust.GetProfileAsync(discordId);
            }
            catch (CourtRuleException)
            {
                // A stale Governance identity may outlive its current SS14↔Discord link.
                // Skip it instead of aborting the entire scheduler iteration.
                continue;
            }

            var eligibleLevel = ModerationQualificationPolicy.EligibleLevel(profile);
            if (eligibleLevel <= currentLevel)
                continue;

            await using var governance = governanceFactory();
            var qualification = await governance.Qualifications.SingleAsync(value =>
                value.UserId == userId && value.Track == "moderation");
            if (eligibleLevel <= qualification.Level)
                continue;

            var previousLevel = qualification.Level;
            qualification.Level = eligibleLevel;
            qualification.UpdatedAt = DateTime.UtcNow;
            governance.AuditEvents.Add(new GovernanceAuditEvent
            {
                EventType = "qualification.automatic_promotion",
                ActorType = "system",
                EntityType = "user",
                EntityId = userId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Payload = JsonSerializer.Serialize(new
                {
                    track = "moderation",
                    from = previousLevel,
                    to = eligibleLevel,
                    trust_score = profile.TrustScore,
                    confidence = profile.Confidence,
                    completed_duties = profile.CompletedDuties,
                    reviewed_actions = profile.ReviewedActions,
                }),
            });
            await governance.SaveChangesAsync();
            changed++;
        }

        return changed;
    }
}
