using Content.DiscordBot.Governance;
using Discord.Interactions;

namespace Content.DiscordBot.Modules;

[Group("событие", "Управление событиями сообщества")]
public sealed class EventGovernanceModule(EventGovernanceService events, CourtDiscordCoordinator discord) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("предложить", "Подать событие на независимую рецензию")]
    public Task ProposeAsync(string title, string description, int minutes, string manifest) => ExecuteAsync(async () =>
    {
        var proposal = await events.ProposeAsync(Context.User.Id, title, description, minutes, manifest);
        await discord.EnsureEventThreadAsync(proposal);
        await FollowupAsync($"Заявка события №{proposal.Id} создана. Независимым кандидатам отправляются приглашения на рецензирование.", ephemeral: true);
    });

    [SlashCommand("приглашение", "Ответить на приглашение стать рецензентом события")]
    public Task InvitationAsync(
        long proposal,
        [Choice("Принять", "accepted")][Choice("Отказаться", "declined")][Choice("Самоотвод", "recused")] string response,
        string? reason = null) => ExecuteAsync(async () =>
    {
        var state = await events.RespondToReviewInvitationAsync(proposal, Context.User.Id, response, reason);
        await FollowupAsync($"Ответ на приглашение по заявке №{proposal}: `{state}`.", ephemeral: true);
    });

    [SlashCommand("рецензия", "Отправить принятую независимую рецензию")]
    public Task ReviewAsync(long proposal,
        [Choice("Одобрить", "approve")][Choice("Отклонить", "reject")] string decision,
        string reasoning) => ExecuteAsync(async () =>
    {
        var result = await events.ReviewAsync(proposal, Context.User.Id, decision, reasoning);
        if (result.Status is "approved" or "rejected")
            await discord.PublishEventStatusAsync(proposal, $"Рецензирование завершено: {result.Approvals} за / {result.Rejections} против.");
        await FollowupAsync($"Заявка №{proposal}: `{result.Status}`, голоса {result.Approvals} за / {result.Rejections} против.", ephemeral: true);
    });

    [SlashCommand("начать", "Начать одобренное событие и выдать ограниченные event.* полномочия")]
    public Task StartAsync(long proposal, int round) => ExecuteAsync(async () =>
    {
        var session = await events.StartAsync(proposal, Context.User.Id, round);
        await discord.PublishEventStatusAsync(proposal, $"Событие запущено как EventSession №{session.Id}. Полномочия ограничены утверждённым манифестом.");
        await FollowupAsync($"EventSession №{session.Id} активна до <t:{new DateTimeOffset(session.ExpiresAt).ToUnixTimeSeconds()}:F>.", ephemeral: true);
    });

    [SlashCommand("действие", "Передать игровому серверу действие из утверждённого манифеста")]
    public Task ActionAsync(long session, string capability, string resource, string? payload = null) => ExecuteAsync(async () =>
    {
        var action = await events.RecordActionAsync(session, Context.User.Id, capability, resource, payload);
        await FollowupAsync($"Действие №{action.Id} разрешено манифестом и передано игровому серверу на исполнение. Фактический результат будет записан в аудит.", ephemeral: true);
    });

    [SlashCommand("завершить", "Завершить или аварийно остановить свою сессию")]
    public Task EndAsync(long session, bool abort = false) => ExecuteAsync(async () =>
    {
        await events.EndAsync(session, Context.User.Id, abort);
        await FollowupAsync($"EventSession №{session} завершена; все event.* полномочия отозваны.", ephemeral: true);
    });

    private async Task ExecuteAsync(Func<Task> action)
    {
        await DeferAsync(ephemeral: true);
        try { await action(); }
        catch (CourtRuleException exception) { await FollowupAsync(exception.Message, ephemeral: true); }
        catch (Exception exception)
        {
            await Logger.Error($"Event governance command failed for {Context.User.Id}", exception);
            await FollowupAsync("Внутренняя ошибка событий. Событие записано в журнал.", ephemeral: true);
        }
    }
}
