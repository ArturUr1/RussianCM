using System.Text.Json;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot.Governance;

public sealed record GovernanceProfile(Guid UserId, Guid Ss14UserId, ulong DiscordId, string Name, int Rating, bool Suspended,
    IReadOnlyDictionary<string, short> Qualifications);

public sealed class GovernanceCommunityService(
    Func<GovernanceDbContext> governanceFactory,
    Func<ServerDbContext> gameFactory,
    Config config)
{
    public async Task<GovernanceUser> RequireUserAsync(ulong discordId)
    {
        await using var game = gameFactory();
        var linked = await game.RMCLinkedAccounts.AsNoTracking()
            .Where(value => value.DiscordId == discordId)
            .Select(value => value.PlayerId)
            .SingleOrDefaultAsync();
        if (linked == Guid.Empty)
            throw new CourtRuleException("Discord-аккаунт не привязан к аккаунту SS14.");

        var storedDiscordId = checked((long) discordId);
        await using var governance = governanceFactory();
        var now = DateTime.UtcNow;
        var bySs14 = await governance.Users.SingleOrDefaultAsync(value => value.Ss14UserId == linked);
        var byDiscord = await governance.Users.SingleOrDefaultAsync(value => value.DiscordUserId == storedDiscordId);

        if (bySs14 != null && byDiscord != null && bySs14.Id != byDiscord.Id)
        {
            throw new CourtRuleException(
                "Обнаружен конфликт привязки Governance: SS14 и Discord уже принадлежат разным профилям. " +
                "Необходимо проверить историю перепривязки аккаунта.");
        }

        var user = bySs14 ?? byDiscord;
        if (user == null)
        {
            user = governance.Users.Add(new GovernanceUser
            {
                Id = Guid.NewGuid(),
                Ss14UserId = linked,
                DiscordUserId = storedDiscordId,
                CreatedAt = now,
                UpdatedAt = now,
            }).Entity;
            await governance.SaveChangesAsync();
        }
        else
        {
            var rebound = user.Ss14UserId != linked || user.DiscordUserId != storedDiscordId;
            if (rebound)
            {
                var previousSs14UserId = user.Ss14UserId;
                var previousDiscordUserId = user.DiscordUserId;
                user.Ss14UserId = linked;
                user.DiscordUserId = storedDiscordId;
                user.UpdatedAt = now;
                governance.AuditEvents.Add(new GovernanceAuditEvent
                {
                    EventType = "identity.rebound",
                    ActorType = "system",
                    EntityType = "user",
                    EntityId = user.Id.ToString(),
                    CreatedAt = now,
                    Payload = JsonSerializer.Serialize(new
                    {
                        previous_ss14_user_id = previousSs14UserId,
                        previous_discord_user_id = previousDiscordUserId,
                        ss14_user_id = linked,
                        discord_user_id = storedDiscordId,
                    }),
                });
                await governance.SaveChangesAsync();
            }
        }

        foreach (var track in new[] { "jury", "moderation", "event" })
        {
            if (!await governance.Qualifications.AnyAsync(value => value.UserId == user.Id && value.Track == track))
                governance.Qualifications.Add(new GovernanceQualification { UserId = user.Id, Track = track, Level = 1, UpdatedAt = now });
        }
        await governance.SaveChangesAsync();
        return user;
    }

    public async Task<GovernanceProfile> GetProfileAsync(ulong discordId)
    {
        var user = await RequireUserAsync(discordId);
        await using var governance = governanceFactory();
        await using var game = gameFactory();
        var name = await game.Player.AsNoTracking().Where(value => value.UserId == user.Ss14UserId)
            .Select(value => value.LastSeenUserName).SingleAsync();
        var qualifications = await governance.Qualifications.AsNoTracking().Where(value => value.UserId == user.Id)
            .ToDictionaryAsync(value => value.Track, value => value.Level);
        return new GovernanceProfile(user.Id, user.Ss14UserId, discordId, name, user.CivicRatingCache,
            user.IsGovernanceSuspended, qualifications);
    }

    public async Task<string> RequestFriendshipAsync(ulong requesterDiscordId, ulong friendDiscordId)
    {
        var requester = await RequireUserAsync(requesterDiscordId);
        var friend = await RequireUserAsync(friendDiscordId);
        if (requester.Id == friend.Id)
            throw new CourtRuleException("Нельзя добавить в друзья самого себя.");
        var first = requester.Id.CompareTo(friend.Id) < 0 ? requester.Id : friend.Id;
        var second = requester.Id.CompareTo(friend.Id) < 0 ? friend.Id : requester.Id;
        await using var governance = governanceFactory();
        var friendship = await governance.Friendships.SingleOrDefaultAsync(value => value.UserId == first && value.FriendUserId == second);
        var now = DateTime.UtcNow;
        if (friendship == null)
        {
            governance.Friendships.Add(new GovernanceFriendship
            {
                UserId = first, FriendUserId = second, RequestedByUserId = requester.Id, CreatedAt = now,
            });
            AddAudit(governance, "friendship.requested", requesterDiscordId, "friendship", $"{first}:{second}", new { friend_user_id = friend.Id });
            await governance.SaveChangesAsync();
            return "Запрос дружбы сохранён. Связь начнёт исключать совместный отбор после подтверждения второй стороной.";
        }
        if (friendship.ConfirmedAt != null)
            return "Дружба уже подтверждена.";
        if (friendship.RequestedByUserId == requester.Id)
            return "Запрос уже ожидает подтверждения второй стороны.";
        friendship.ConfirmedAt = now;
        AddAudit(governance, "friendship.confirmed", requesterDiscordId, "friendship", friendship.Id.ToString(), new { });
        await governance.SaveChangesAsync();
        return "Дружба подтверждена. Вы не будете вместе отбираться в конфликтующие роли.";
    }

    public async Task RemoveFriendshipAsync(ulong actorDiscordId, ulong friendDiscordId)
    {
        var actor = await RequireUserAsync(actorDiscordId);
        var friend = await RequireUserAsync(friendDiscordId);
        var first = actor.Id.CompareTo(friend.Id) < 0 ? actor.Id : friend.Id;
        var second = actor.Id.CompareTo(friend.Id) < 0 ? friend.Id : actor.Id;
        await using var governance = governanceFactory();
        var row = await governance.Friendships.SingleOrDefaultAsync(value => value.UserId == first && value.FriendUserId == second)
            ?? throw new CourtRuleException("Такая связь не найдена.");
        governance.Friendships.Remove(row);
        AddAudit(governance, "friendship.removed", actorDiscordId, "friendship", row.Id.ToString(), new { });
        await governance.SaveChangesAsync();
    }

    public async Task SetQualificationAsync(ulong actorDiscordId, ulong targetDiscordId, string track, short level)
    {
        if (track is not ("jury" or "moderation" or "event") || level is < 0 or > 4)
            throw new CourtRuleException("Направление должно быть jury/moderation/event, уровень — от 0 до 4.");
        var target = await RequireUserAsync(targetDiscordId);
        await using var governance = governanceFactory();
        var row = await governance.Qualifications.SingleAsync(value => value.UserId == target.Id && value.Track == track);
        row.Level = level;
        row.UpdatedAt = DateTime.UtcNow;
        AddAudit(governance, "qualification.changed", actorDiscordId, "user", target.Id.ToString(), new { track, level });
        await governance.SaveChangesAsync();
    }

    public async Task SetSuspendedAsync(ulong actorDiscordId, ulong targetDiscordId, bool suspended, string reason)
    {
        if (reason.Trim().Length < 10)
            throw new CourtRuleException("Укажите содержательную причину (не менее 10 символов).");
        var target = await RequireUserAsync(targetDiscordId);
        await using var governance = governanceFactory();
        var user = await governance.Users.SingleAsync(value => value.Id == target.Id);
        user.IsGovernanceSuspended = suspended;
        user.UpdatedAt = DateTime.UtcNow;
        if (suspended)
        {
            var now = DateTime.UtcNow;
            foreach (var grant in await governance.CapabilityGrants.Where(value => value.UserId == user.Id && value.RevokedAt == null).ToListAsync())
                grant.RevokedAt = now;
            foreach (var duty in await governance.DutySessions.Where(value => value.UserId == user.Id && value.Status == "active").ToListAsync())
            {
                duty.Status = "revoked";
                duty.EndedAt = now;
                duty.Version++;
            }
        }
        governance.LeadershipOverrides.Add(new GovernanceLeadershipOverride
        {
            EntityType = "user", EntityId = user.Id.ToString(), Action = suspended ? "suspend" : "restore",
            Reason = reason.Trim(), ActorDiscordId = checked((long) actorDiscordId), CreatedAt = DateTime.UtcNow,
        });
        AddAudit(governance, suspended ? "leadership.user_suspended" : "leadership.user_restored", actorDiscordId,
            "user", user.Id.ToString(), new { reason });
        await governance.SaveChangesAsync();
    }

    public async Task MarkFalseReportAsync(long caseId, ulong actorDiscordId, string reason)
    {
        if (reason.Trim().Length < 20)
            throw new CourtRuleException("Причина должна содержать не менее 20 символов.");
        await using var governance = governanceFactory();
        var courtCase = await governance.CourtCases.SingleOrDefaultAsync(value => value.Id == caseId)
            ?? throw new CourtRuleException("Дело не найдено.");
        if (courtCase.FalseReportAt != null)
            return;
        if (courtCase.Status is not (CourtStatuses.Verdict or CourtStatuses.Executed or CourtStatuses.Overturned))
            throw new CourtRuleException("Ложность жалобы можно фиксировать только после решения по делу.");
        await governance.Database.ExecuteSqlRawAsync(
            "SELECT governance.append_rating_entry({0}, {1}, 'false_report', 'court_case', {2}, 'leadership', {3}, {4}, '{}'::jsonb)",
            courtCase.ClaimantUserId, -config.CourtFalseReportPenalty, caseId.ToString(), actorDiscordId.ToString(), $"court:{caseId}:false-report");
        courtCase.FalseReportAt = DateTime.UtcNow;
        governance.LeadershipOverrides.Add(new GovernanceLeadershipOverride
        {
            EntityType = "court_case", EntityId = caseId.ToString(), Action = "false_report",
            Reason = reason.Trim(), ActorDiscordId = checked((long) actorDiscordId), CreatedAt = DateTime.UtcNow,
        });
        AddAudit(governance, "leadership.false_report", actorDiscordId, "court_case", caseId.ToString(),
            new { penalty = config.CourtFalseReportPenalty, reason });
        await governance.SaveChangesAsync();
    }

    private static void AddAudit(GovernanceDbContext db, string eventType, ulong actorDiscordId, string entityType, string entityId, object payload)
    {
        db.AuditEvents.Add(new GovernanceAuditEvent
        {
            EventType = eventType, ActorType = "discord_user", ActorId = actorDiscordId.ToString(),
            EntityType = entityType, EntityId = entityId, CreatedAt = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(payload),
        });
    }
}
