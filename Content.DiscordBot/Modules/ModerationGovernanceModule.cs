using Content.DiscordBot.Governance;
using Discord;
using Discord.Interactions;

namespace Content.DiscordBot.Modules;

[Group("дежурство", "AHelp и модерация сообщества")]
public sealed class ModerationGovernanceModule(
    ModerationGovernanceService moderation,
    ModerationTrustService moderationTrust,
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

    [SlashCommand("доверие", "Показать Moderation Trust пользователя")]
    public Task TrustAsync(IUser? user = null) => ExecuteAsync(async () =>
    {
        var target = user ?? Context.User;
        var profile = await moderationTrust.GetProfileAsync(target.Id);
        var embed = new EmbedBuilder()
            .WithTitle($"Moderation Trust • {target.Username}")
            .AddField("Итог", $"{profile.TrustScore}/1000", true)
            .AddField("Уверенность", $"{profile.Confidence}%", true)
            .AddField("Точность решений", $"{profile.DecisionAccuracy}%", true)
            .AddField("Процедурность", $"{profile.ProceduralScore}%", true)
            .AddField("Надёжность", $"{profile.ReliabilityScore}%", true)
            .AddField("Проверено действий", profile.ReviewedActions, true)
            .AddField("Дежурства", $"{profile.CompletedDuties} успешно / {profile.FailedDuties} сорвано", true)
            .AddField("Серьёзные вмешательства", profile.SeriousInterventions, true)
            .WithColor(profile.TrustScore >= 800 ? Color.Green : profile.TrustScore >= 600 ? Color.Gold : Color.Orange)
            .Build();
        await FollowupAsync(embed: embed, ephemeral: true);
    });

    [SlashCommand("аудит-ответ", "Ответить на приглашение проверить действие дежурного")]
    public Task AuditInvitationAsync(
        long action,
        [Choice("Принять", "accepted")][Choice("Отказаться", "declined")][Choice("Самоотвод", "recused")] string response,
        string? reason = null) => ExecuteAsync(async () =>
    {
        var state = await moderationTrust.RespondToInvitationAsync(action, Context.User.Id, response, reason);
        await FollowupAsync($"Ответ на приглашение по действию №{action}: `{state}`.", ephemeral: true);
    });

    [SlashCommand("аудит-материалы", "Показать материалы назначенного независимого аудита")]
    public Task AuditMaterialsAsync(long action) => ExecuteAsync(async () =>
    {
        var packet = await moderationTrust.GetReviewPacketAsync(action, Context.User.Id);
        var embed = new EmbedBuilder()
            .WithTitle($"Независимый аудит • действие №{packet.ActionId}")
            .AddField("Раунд", packet.RoundId, true)
            .AddField("Инцидент", $"#{packet.IncidentId} • {packet.IncidentType}", true)
            .AddField("Тип действия", packet.ActionType, true)
            .AddField("Кворум", $"{packet.Approvals}/{packet.RequiredApprovals}; отклонений {packet.Rejections}", true)
            .AddField("Причина дежурного", packet.Reason)
            .AddField("Контекст инцидента", packet.IncidentSummary)
            .AddField("Выполнено", packet.ExecutedAt?.ToString("u") ?? "нет данных", true)
            .AddField("Передано в суд", packet.EscalatedToCourt ? "да" : "нет", true)
            .WithColor(Color.Blue)
            .Build();
        await FollowupAsync(embed: embed, ephemeral: true);
    });

    [SlashCommand("аудит", "Отправить независимую оценку действия дежурного")]
    public Task AuditAsync(
        long action,
        [Choice("Корректно", "correct")]
        [Choice("Разумно, но ошибочно", "reasonable_but_wrong")]
        [Choice("Процедурная ошибка", "procedural_error")]
        [Choice("Небрежность", "negligent")]
        [Choice("Злоупотребление", "abuse")] string outcome,
        string reasoning) => ExecuteAsync(async () =>
    {
        await moderationTrust.SubmitReviewAsync(action, Context.User.Id, outcome, reasoning);
        await FollowupAsync($"Независимый аудит действия №{action} сохранён: `{outcome}`.", ephemeral: true);
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
