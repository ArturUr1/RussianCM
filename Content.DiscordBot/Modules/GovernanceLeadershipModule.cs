using Content.DiscordBot.Governance;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Content.DiscordBot.Modules;

[Group("руководство", "Контролируемые действия руководства с обязательной причиной")]
public sealed class GovernanceLeadershipModule(
    GovernanceCommunityService community,
    ReputationService reputation,
    CourtPunishmentService punishments,
    CourtDiscordCoordinator discord,
    ModerationTrustService moderationTrust,
    Config config) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("отменить-решение", "Отменить решение суда и откатить наказание")]
    public Task OverturnAsync(long courtCase, string reason) => ExecuteAsync(async () =>
    {
        await punishments.OverturnAsync(courtCase, Context.User.Id, reason);
        await discord.PublishLeadershipNoticeAsync(courtCase, $"Решение по делу №{courtCase} отменено руководством",
            $"Причина: {reason}\nИсполненная мера отозвана. Обычный апелляционный пересмотр не предусмотрен.", Color.Purple);
        await FollowupAsync($"Решение по делу №{courtCase} отменено; исполненная мера отозвана.", ephemeral: true);
    });

    [SlashCommand("ложная-жалоба", "Зафиксировать заведомо ложную жалобу как серьёзное репутационное событие")]
    public Task FalseReportAsync(long courtCase, string reason) => ExecuteAsync(async () =>
    {
        await community.MarkFalseReportAsync(courtCase, Context.User.Id, reason);
        await discord.PublishLeadershipNoticeAsync(courtCase, $"По делу №{courtCase} зафиксирована ложная жалоба",
            $"Причина: {reason}", Color.DarkRed);
        await FollowupAsync($"Для дела №{courtCase} зафиксирована заведомо ложная жалоба. Репутационный движок учтёт её как серьёзное наблюдение.", ephemeral: true);
    });

    [SlashCommand("квалификация", "Изменить квалификацию пользователя вручную")]
    public Task QualificationAsync(IUser user,
        [Choice("Поддержка игроков", ReputationTracks.Support)]
        [Choice("Модерация", ReputationTracks.Moderation)]
        [Choice("Присяжные", ReputationTracks.Jury)]
        [Choice("События", ReputationTracks.Event)]
        [Choice("Контрибьюторство", ReputationTracks.Contributor)] string track,
        int level) => ExecuteAsync(async () =>
    {
        await community.SetQualificationAsync(Context.User.Id, user.Id, track, checked((short) level));
        await FollowupAsync($"Квалификация `{track}` пользователя {user.Mention}: {level}.", ephemeral: true);
    });

    [SlashCommand("профиль-ss14", "Проверить Governance-профиль игрока SS14 без изменения данных")]
    public Task Ss14ProfileAsync(
        [Summary("игрок", "Игровой никнейм SS14; Discord-привязка не требуется")] string player) => ExecuteAsync(async () =>
    {
        var target = await community.RequireSs14UserByNicknameAsync(player);
        var profile = await reputation.GetProfileAsync(target.Id);
        var discordText = profile.DiscordUserId is > 0
            ? $"<@{profile.DiscordUserId}> (`{profile.DiscordUserId}`)"
            : "не привязан";
        var paths = profile.Paths.Count == 0
            ? "не выбраны"
            : string.Join("\n", profile.Paths.Select(value =>
                $"{(value.Slot == 1 ? "основной" : "дополнительный")}: `{value.Track}`"));

        await FollowupAsync(embed: new EmbedBuilder()
            .WithTitle($"Governance-профиль • {profile.Name}")
            .AddField("Governance user_id", $"`{profile.UserId}`")
            .AddField("SS14 UUID", $"`{profile.Ss14UserId}`")
            .AddField("Discord", discordText, true)
            .AddField("Общая репутация", $"{profile.General.Score}/1000", true)
            .AddField("Надёжность", $"средняя {profile.General.Mean:P1}\nнижняя 90% граница {profile.General.LowerBound:P1}", true)
            .AddField("Игровая активность",
                $"{profile.Activity.OverallHours:F0} ч • {profile.Activity.ActiveWeeks} активных нед. • аккаунту {profile.Activity.AccountAgeDays} дн.\n" +
                $"индекс {profile.Activity.ActivityIndex:P0} • вес свидетельства {profile.Activity.EvidenceWeight:F2}")
            .AddField("Пути участия", paths)
            .AddField("Допуск", profile.Suspended ? "приостановлен" : "активен", true)
            .WithColor(profile.Suspended ? Color.Red : Color.DarkBlue)
            .WithFooter("Read-only диагностика Identity / Reputation v2")
            .Build(), ephemeral: true);
    });

    [SlashCommand("вклад", "Зафиксировать подтверждённый вклад игрока в проект")]
    public Task ContributionAsync(
        [Summary("игрок", "Игровой никнейм SS14; Discord-привязка не требуется")] string player,
        [Summary("ссылка", "PR, документ, задача или другой проверяемый идентификатор")] string reference,
        [Summary("тип", "Код, локализация, карта, графика, документация, тестирование и т. п.")] string kind,
        [Summary("значимость", "0.1–3.0: масштаб полезного изменения")] double impact,
        [Summary("качество", "0.1–1.5: качество исполнения")] double quality,
        [Summary("устойчивость", "0.1–1.5: подтверждённая устойчивость результата")] double stability) => ExecuteAsync(async () =>
    {
        var target = await community.RequireSs14UserByNicknameAsync(player);
        var contribution = await reputation.RecordContributionAsync(
            target.Id,
            reference,
            kind,
            impact,
            quality,
            stability,
            DateTime.UtcNow,
            Context.User.Id);
        await FollowupAsync(
            $"Вклад №{contribution.Id} игрока **{player}** зафиксирован. " +
            "Репутация рассчитывается по значимости, качеству и устойчивости с насыщением — размер diff сам по себе очков не даёт.",
            ephemeral: true);
    });

    [SlashCommand("допуск", "Приостановить или восстановить участие во всех контурах")]
    public Task SuspensionAsync(IUser user, bool suspended, string reason) => ExecuteAsync(async () =>
    {
        await community.SetSuspendedAsync(Context.User.Id, user.Id, suspended, reason);
        await FollowupAsync($"Допуск пользователя {user.Mention}: {(suspended ? "приостановлен" : "восстановлен")}.", ephemeral: true);
    });

    [SlashCommand("аудит-действия", "Случайно назначить независимый аудит исполненного действия дежурного")]
    public Task AssignModerationAuditAsync(long action) => ExecuteAsync(async () =>
    {
        var assignment = await moderationTrust.AssignRandomReviewAsync(action);
        var reviewer = assignment.ReviewerDiscordId is > 0 ? $"<@{assignment.ReviewerDiscordId}>" : "SS14-профиль без Discord";
        await FollowupAsync(
            $"Для действия №{action} назначен независимый рецензент {reviewer}. " +
            $"Приглашение №{assignment.InvitationId} действительно до {assignment.ExpiresAt:u}.",
            ephemeral: true);
    });

    private async Task ExecuteAsync(Func<Task> action)
    {
        await DeferAsync(ephemeral: true);
        try
        {
            EnsureLeadership();
            await action();
        }
        catch (CourtRuleException exception)
        {
            await FollowupAsync(exception.Message, ephemeral: true);
        }
        catch (Exception exception)
        {
            await Logger.Error($"Leadership command failed for {Context.User.Id}", exception);
            await FollowupAsync("Внутренняя ошибка руководства. Событие записано в журнал.", ephemeral: true);
        }
    }

    private void EnsureLeadership()
    {
        if (Context.Guild.OwnerId == Context.User.Id)
            return;
        if (config.CourtLeadershipRole != 0 && Context.User is SocketGuildUser member && member.Roles.Any(value => value.Id == config.CourtLeadershipRole))
            return;
        throw new CourtRuleException("Команда доступна только владельцу сервера или настроенной роли руководства.");
    }
}
