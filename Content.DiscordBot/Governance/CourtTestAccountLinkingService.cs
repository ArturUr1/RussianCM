using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.DiscordBot.Governance;

/// <summary>
/// Local-only helper for full Community Court smoke tests. It creates the same game-database
/// Discord↔SS14 relation used by the normal linking flow, then synchronizes the Governance identity.
/// The helper is disabled unless COURT_TEST_MODE=true.
/// </summary>
public sealed class CourtTestAccountLinkingService(
    Func<ServerDbContext> gameFactory,
    GovernanceCommunityService community,
    Config config)
{
    public async Task<string> LinkJurorAsync(ulong actorDiscordId, ulong targetDiscordId, string playerQuery)
    {
        if (!config.CourtTestMode)
            throw new CourtRuleException("Тестовая привязка отключена. Для локального стенда задайте COURT_TEST_MODE=true.");
        if (targetDiscordId > long.MaxValue)
            throw new CourtRuleException("Discord ID не поддерживается Governance.");

        playerQuery = playerQuery.Trim();
        if (playerQuery.Length == 0)
            throw new CourtRuleException("Укажите ник или SS14 UUID тестировщика.");

        await using var game = gameFactory();
        var player = await ResolvePlayerAsync(game, playerQuery);

        var linkedByPlayer = await game.RMCLinkedAccounts.AsNoTracking()
            .SingleOrDefaultAsync(value => value.PlayerId == player.UserId);
        if (linkedByPlayer != null && linkedByPlayer.DiscordId != targetDiscordId)
        {
            throw new CourtRuleException(
                $"SS14-аккаунт «{player.LastSeenUserName}» уже привязан к другому Discord ID ({linkedByPlayer.DiscordId}).");
        }

        var linkedByDiscord = await game.RMCLinkedAccounts.AsNoTracking()
            .SingleOrDefaultAsync(value => value.DiscordId == targetDiscordId);
        if (linkedByDiscord != null && linkedByDiscord.PlayerId != player.UserId)
        {
            throw new CourtRuleException(
                $"Этот Discord-аккаунт уже привязан к другому SS14 UUID ({linkedByDiscord.PlayerId}).");
        }

        if (linkedByPlayer == null && linkedByDiscord == null)
        {
            var discord = await game.RMCDiscordAccounts.SingleOrDefaultAsync(value => value.Id == targetDiscordId);
            if (discord == null)
                game.RMCDiscordAccounts.Add(new RMCDiscordAccount { Id = targetDiscordId });

            game.RMCLinkedAccounts.Add(new RMCLinkedAccount
            {
                PlayerId = player.UserId,
                DiscordId = targetDiscordId,
            });
            game.RMCLinkedAccountLogs.Add(new RMCLinkedAccountLogs
            {
                PlayerId = player.UserId,
                DiscordId = targetDiscordId,
                At = DateTime.UtcNow,
            });
            await game.SaveChangesAsync();
        }

        var profile = await community.GetProfileAsync(targetDiscordId);
        if (!profile.Qualifications.TryGetValue("jury", out var juryLevel) || juryLevel < 1)
            await community.SetQualificationAsync(actorDiscordId, targetDiscordId, "jury", 1);

        await Logger.Info(
            $"Court test link: Discord {targetDiscordId} -> SS14 {player.UserId} ({player.LastSeenUserName}), actor {actorDiscordId}.");

        return $"Тестовая привязка создана: <@{targetDiscordId}> → {player.LastSeenUserName} (`{player.UserId}`). Допуск присяжного: jury ≥ 1.";
    }

    private static async Task<PlayerIdentity> ResolvePlayerAsync(ServerDbContext game, string query)
    {
        if (Guid.TryParse(query, out var playerId))
        {
            return await game.Player.AsNoTracking()
                .Where(value => value.UserId == playerId)
                .Select(value => new PlayerIdentity(value.UserId, value.LastSeenUserName))
                .SingleOrDefaultAsync()
                ?? throw new CourtRuleException("Игрок с таким SS14 UUID не найден в локальной базе.");
        }

        var normalized = query.ToLower();
        var matches = await game.Player.AsNoTracking()
            .Where(value => value.LastSeenUserName.ToLower() == normalized)
            .Select(value => new PlayerIdentity(value.UserId, value.LastSeenUserName))
            .Take(3)
            .ToListAsync();
        if (matches.Count == 0)
            throw new CourtRuleException($"Игрок с ником «{query}» не найден в локальной базе.");

        var exact = matches.Where(value => value.LastSeenUserName == query).ToArray();
        if (exact.Length == 1)
            return exact[0];
        if (matches.Count == 1)
            return matches[0];

        throw new CourtRuleException("Найдено несколько игроков с таким ником. Укажите SS14 UUID.");
    }

    private sealed record PlayerIdentity(Guid UserId, string LastSeenUserName);
}
