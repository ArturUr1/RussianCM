using Content.DiscordBot.Governance;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Content.DiscordBot.Modules;

[Group("руководство", "Контролируемые действия руководства с обязательной причиной")]
public sealed class GovernanceLeadershipModule(
    GovernanceCommunityService community,
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

    [SlashCommand("ложная-жалоба", "Зафиксировать заведомо ложную жалобу и снизить рейтинг")]
    public Task FalseReportAsync(long courtCase, string reason) => ExecuteAsync(async () =>
    {
        await community.MarkFalseReportAsync(courtCase, Context.User.Id, reason);
        await discord.PublishLeadershipNoticeAsync(courtCase, $"По делу №{courtCase} зафиксирована ложная жалоба",
            $"Причина: {reason}", Color.DarkRed);
        await FollowupAsync($"Для дела №{courtCase} зафиксирована ложная жалоба.", ephemeral: true);
    });

    [SlashCommand("квалификация", "Изменить независимую квалификацию пользователя")]
    public Task QualificationAsync(IUser user,
        [Choice("Присяжные", "jury")][Choice("Модерация", "moderation")][Choice("События", "event")] string track,
        int level) => ExecuteAsync(async () =>
    {
        await community.SetQualificationAsync(Context.User.Id, user.Id, track, checked((short) level));
        await FollowupAsync($"Квалификация `{track}` пользователя {user.Mention}: {level}.", ephemeral: true);
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
        await FollowupAsync(
            $"Для действия №{action} назначен независимый рецензент <@{assignment.ReviewerDiscordId}>. " +
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
        catch (CourtRuleException exception) { await FollowupAsync(exception.Message, ephemeral: true); }
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
