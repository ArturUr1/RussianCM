using System.Text.Json;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot.Governance;

public sealed record GovernanceIdentity(
    Guid GovernanceUserId,
    Guid Ss14UserId,
    long? DiscordUserId,
    string Name);

public sealed class GovernanceIdentityService(
    Func<GovernanceDbContext> governanceFactory,
    Func<ServerDbContext> gameFactory)
{
    public async Task EnsureAllSs14UsersAsync()
    {
        await using var governance = governanceFactory();
        await governance.Database.ExecuteSqlRawAsync("""
            INSERT INTO governance.users(ss14_user_id, discord_user_id, civic_rating_cache, created_at, updated_at)
            SELECT p.user_id, NULL, 500, p.first_seen_time, now()
            FROM player p
            ON CONFLICT (ss14_user_id) DO NOTHING
            """);

        // Reconcile only currently linked accounts. Historical links are preserved in identity_links.
        var links = await governance.Database.SqlQueryRaw<CurrentGameLink>("""
            SELECT player_id AS "Ss14UserId", discord_id::bigint AS "DiscordUserId"
            FROM rmc_linked_accounts
            WHERE discord_id > 0
            """).ToListAsync();
        foreach (var link in links)
            await AttachDiscordAsync(governance, link.Ss14UserId, link.DiscordUserId, "game_account_link", null);
        await governance.SaveChangesAsync();
    }

    public async Task<GovernanceUser> RequireSs14UserAsync(Guid ss14UserId)
    {
        await using var game = gameFactory();
        var exists = await game.Player.AsNoTracking().AnyAsync(value => value.UserId == ss14UserId);
        if (!exists)
            throw new CourtRuleException("Аккаунт SS14 не найден.");

        await using var governance = governanceFactory();
        var user = await governance.Users.SingleOrDefaultAsync(value => value.Ss14UserId == ss14UserId);
        if (user == null)
        {
            var firstSeen = await game.Player.AsNoTracking().Where(value => value.UserId == ss14UserId)
                .Select(value => value.FirstSeenTime).SingleAsync();
            user = governance.Users.Add(new GovernanceUser
            {
                Id = Guid.NewGuid(),
                Ss14UserId = ss14UserId,
                DiscordUserId = null,
                CivicRatingCache = ReputationPolicy.NeutralScore,
                CreatedAt = firstSeen.ToUniversalTime(),
                UpdatedAt = DateTime.UtcNow,
            }).Entity;
            await governance.SaveChangesAsync();
        }
        return user;
    }

    public async Task<GovernanceUser> RequireSs14UserByNicknameAsync(string nickname)
    {
        nickname = nickname.Trim();
        if (nickname.Length is < 1 or > 64)
            throw new CourtRuleException("Игровой никнейм должен содержать от 1 до 64 символов.");
        var lowered = nickname.ToLowerInvariant();
        await using var game = gameFactory();
        var matches = await game.Player.AsNoTracking()
            .Where(value => value.LastSeenUserName.ToLower() == lowered)
            .Select(value => new { value.UserId, value.LastSeenUserName })
            .Take(3)
            .ToListAsync();
        if (matches.Count == 0)
            throw new CourtRuleException($"Игрок с никнеймом «{nickname}» не найден.");
        var exact = matches.Where(value => value.LastSeenUserName == nickname).ToArray();
        var selected = exact.Length == 1 ? exact[0] : matches.Count == 1 ? matches[0] :
            throw new CourtRuleException("Найдено несколько игроков с таким никнеймом. Укажите точный регистр.");
        return await RequireSs14UserAsync(selected.UserId);
    }

    public async Task<GovernanceUser> RequireDiscordUserAsync(ulong discordId)
    {
        await using var game = gameFactory();
        var ss14UserId = await game.RMCLinkedAccounts.AsNoTracking()
            .Where(value => value.DiscordId == discordId)
            .Select(value => (Guid?) value.PlayerId)
            .SingleOrDefaultAsync();
        if (ss14UserId == null)
            throw new CourtRuleException("Discord-аккаунт не привязан к аккаунту SS14. Репутация SS14-профиля сохраняется, но Discord-функции требуют привязку.");

        await using var governance = governanceFactory();
        var user = await governance.Users.SingleOrDefaultAsync(value => value.Ss14UserId == ss14UserId.Value);
        if (user == null)
        {
            var player = await game.Player.AsNoTracking().SingleAsync(value => value.UserId == ss14UserId.Value);
            user = governance.Users.Add(new GovernanceUser
            {
                Id = Guid.NewGuid(),
                Ss14UserId = player.UserId,
                CivicRatingCache = ReputationPolicy.NeutralScore,
                CreatedAt = player.FirstSeenTime.ToUniversalTime(),
                UpdatedAt = DateTime.UtcNow,
            }).Entity;
            await governance.SaveChangesAsync();
        }

        await AttachDiscordAsync(governance, user.Ss14UserId, checked((long) discordId), "discord_command", discordId.ToString());
        await EnsureBaselineQualificationsAsync(governance, user.Id);
        await governance.SaveChangesAsync();
        return user;
    }

    public async Task<GovernanceIdentity> GetIdentityAsync(Guid governanceUserId)
    {
        await using var governance = governanceFactory();
        var user = await governance.Users.AsNoTracking().SingleOrDefaultAsync(value => value.Id == governanceUserId)
            ?? throw new CourtRuleException("Профиль Governance не найден.");
        await using var game = gameFactory();
        var name = await game.Player.AsNoTracking().Where(value => value.UserId == user.Ss14UserId)
            .Select(value => value.LastSeenUserName).SingleOrDefaultAsync() ?? user.Ss14UserId.ToString();
        return new GovernanceIdentity(user.Id, user.Ss14UserId, user.DiscordUserId, name);
    }

    public async Task DetachDiscordIfStaleAsync(Guid governanceUserId, string source = "reconcile")
    {
        await using var governance = governanceFactory();
        var user = await governance.Users.SingleAsync(value => value.Id == governanceUserId);
        if (user.DiscordUserId == null)
            return;
        await using var game = gameFactory();
        var stillLinked = await game.RMCLinkedAccounts.AsNoTracking().AnyAsync(value =>
            value.PlayerId == user.Ss14UserId && (long) value.DiscordId == user.DiscordUserId.Value);
        if (stillLinked)
            return;
        var now = DateTime.UtcNow;
        var current = await governance.IdentityLinks.SingleOrDefaultAsync(value => value.UserId == user.Id && value.UnlinkedAt == null);
        if (current != null)
            current.UnlinkedAt = now;
        var previous = user.DiscordUserId;
        user.DiscordUserId = null;
        user.UpdatedAt = now;
        AddAudit(governance, "identity.discord_unlinked", "system", null, user.Id,
            new { discord_user_id = previous, source });
        await governance.SaveChangesAsync();
    }

    private static async Task AttachDiscordAsync(
        GovernanceDbContext governance,
        Guid ss14UserId,
        long discordUserId,
        string source,
        string? actorId)
    {
        if (discordUserId <= 0)
            throw new CourtRuleException("Discord ID должен быть положительным snowflake.");
        var user = await governance.Users.SingleOrDefaultAsync(value => value.Ss14UserId == ss14UserId)
            ?? throw new CourtRuleException("Governance-профиль SS14 ещё не создан.");
        var other = await governance.Users.SingleOrDefaultAsync(value => value.DiscordUserId == discordUserId && value.Id != user.Id);
        if (other != null)
            throw new CourtRuleException("Discord уже привязан к другому Governance-профилю. Автоматическое слияние репутации запрещено.");
        if (user.DiscordUserId == discordUserId)
        {
            if (!await governance.IdentityLinks.AnyAsync(value => value.UserId == user.Id && value.DiscordUserId == discordUserId && value.UnlinkedAt == null))
            {
                governance.IdentityLinks.Add(new GovernanceIdentityLink
                {
                    UserId = user.Id,
                    DiscordUserId = discordUserId,
                    LinkedAt = DateTime.UtcNow,
                    Source = source,
                    Metadata = "{}",
                });
            }
            return;
        }

        var now = DateTime.UtcNow;
        var current = await governance.IdentityLinks.SingleOrDefaultAsync(value => value.UserId == user.Id && value.UnlinkedAt == null);
        if (current != null)
            current.UnlinkedAt = now;
        var previousDiscordId = user.DiscordUserId;
        user.DiscordUserId = discordUserId;
        user.UpdatedAt = now;
        governance.IdentityLinks.Add(new GovernanceIdentityLink
        {
            UserId = user.Id,
            DiscordUserId = discordUserId,
            LinkedAt = now,
            Source = source,
            Metadata = JsonSerializer.Serialize(new { previous_discord_user_id = previousDiscordId }),
        });
        AddAudit(governance, "identity.discord_linked", actorId == null ? "system" : "discord_user", actorId, user.Id,
            new { previous_discord_user_id = previousDiscordId, discord_user_id = discordUserId, source });
    }

    private static async Task EnsureBaselineQualificationsAsync(GovernanceDbContext governance, Guid userId)
    {
        foreach (var track in ReputationTracks.ServicePaths)
        {
            if (!await governance.Qualifications.AnyAsync(value => value.UserId == userId && value.Track == track))
            {
                governance.Qualifications.Add(new GovernanceQualification
                {
                    UserId = userId,
                    Track = track,
                    Level = 1,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
        }
    }

    private static void AddAudit(
        GovernanceDbContext governance,
        string eventType,
        string actorType,
        string? actorId,
        Guid userId,
        object payload)
    {
        governance.AuditEvents.Add(new GovernanceAuditEvent
        {
            EventType = eventType,
            ActorType = actorType,
            ActorId = actorId,
            EntityType = "user",
            EntityId = userId.ToString(),
            CreatedAt = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(payload),
        });
    }

    private sealed record CurrentGameLink(Guid Ss14UserId, long DiscordUserId);
}
