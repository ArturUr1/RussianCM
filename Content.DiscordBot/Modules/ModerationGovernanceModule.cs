using Content.DiscordBot.Governance;
using Discord;
using Discord.Interactions;

namespace Content.DiscordBot.Modules;

[Group("дежурство", "AHelp и модерация сообщества")]
public sealed class ModerationGovernanceModule(
    ModerationGovernanceService moderation,
    CourtDiscordCoordinator discord) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ahelp", "Создать AHelp в общей очереди")]
    public Task AHelpAsync(int round, string description, IUser? target = null) => ExecuteAsync(async () =>
    {
        var ticket = await moderation.CreateAHelpAsync(Context.User.Id, target?.Id, round, description);
        await discord.EnsureAHelpThreadAsync(ticket);
        await FollowupAsync($"AHelp №{ticket.Id} создан со статусом `open`.", ephemeral: true);
    });

    [SlashCommand("статус", "Изменить состояние взятого AHelp")]
    public Task StatusAsync(long ahelp,
        [Choice("Открыт", "open")][Choice("Ожидание игрока", "waiting_player")][Choice("Решён", "resolved")] string status) => ExecuteAsync(async () =>
    {
        await moderation.SetAHelpStatusAsync(ahelp, Context.User.Id, status);
        await FollowupAsync($"AHelp №{ahelp}: `{status}`.", ephemeral: true);
    });

    [SlashCommand("инцидент", "Эскалировать взятый AHelp в активный инцидент")]
    public Task IncidentAsync(long ahelp, string type) => ExecuteAsync(async () =>
    {
        var incident = await moderation.EscalateToIncidentAsync(ahelp, Context.User.Id, type);
        await FollowupAsync($"Создан LiveIncident №{incident.Id}. Все действия требуют временного полномочия и кворума.", ephemeral: true);
    });

    [SlashCommand("действие", "Предложить действие по активному инциденту")]
    public Task ActionAsync(long incident,
        [Choice("Заморозить", "freeze")][Choice("Удалить до конца раунда", "round_remove")]
        [Choice("Запросить объяснение", "request_explanation")][Choice("Просмотр логов", "view_logs")] string action,
        string reason, int? seconds = null) => ExecuteAsync(async () =>
    {
        var outcome = await moderation.ProposeActionAsync(incident, Context.User.Id, action, reason, seconds);
        await FollowupAsync($"Действие №{outcome.ActionId}: `{outcome.Status}`, одобрений {outcome.Approvals}/{outcome.RequiredApprovals}.", ephemeral: true);
    });

    [SlashCommand("решение", "Одобрить или отклонить действие вторым дежурным")]
    public Task ReviewAsync(long action,
        [Choice("Одобрить", "approve")][Choice("Отклонить", "reject")][Choice("Нужны сведения", "more_information")] string decision) => ExecuteAsync(async () =>
    {
        var outcome = await moderation.ReviewActionAsync(action, Context.User.Id, decision);
        await FollowupAsync($"Действие №{outcome.ActionId}: `{outcome.Status}`, одобрений {outcome.Approvals}/{outcome.RequiredApprovals}.", ephemeral: true);
    });

    [SlashCommand("закрыть-инцидент", "Закрыть активный инцидент")]
    public Task CloseAsync(long incident) => ExecuteAsync(async () =>
    {
        await moderation.CloseIncidentAsync(incident, Context.User.Id);
        await FollowupAsync($"Инцидент №{incident} закрыт.", ephemeral: true);
    });

    private async Task ExecuteAsync(Func<Task> action)
    {
        await DeferAsync(ephemeral: true);
        try { await action(); }
        catch (CourtRuleException exception) { await FollowupAsync(exception.Message, ephemeral: true); }
        catch (Exception exception)
        {
            await Logger.Error($"Moderation governance command failed for {Context.User.Id}", exception);
            await FollowupAsync("Внутренняя ошибка дежурства. Событие записано в журнал.", ephemeral: true);
        }
    }
}
