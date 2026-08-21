using Content.DiscordBot.Governance;
using Discord;
using Discord.Interactions;

namespace Content.DiscordBot.Modules;

[Group("репутация", "Репутация и пути участия RUCM")]
public sealed class ReputationModule(
    GovernanceCommunityService community,
    ReputationService reputation,
    ReputationHistoryService history) : InteractionModuleBase<SocketInteractionContext>
{
    private const double MinimumTrustDisplayEvidence = 1.0;

    [SlashCommand("профиль", "Показать репутацию, игровую активность и доверие по направлениям")]
    public Task ProfileAsync() => ExecuteAsync(async () =>
    {
        var user = await community.RequireUserAsync(Context.User.Id);
        var profile = await reputation.GetProfileAsync(user.Id);
        var selectedPaths = profile.Paths.Select(value => value.Track).ToHashSet(StringComparer.Ordinal);
        var pathText = profile.Paths.Count == 0
            ? "Пути пока не выбраны. Используйте `/репутация пути`."
            : string.Join("\n", profile.Paths.Select(value =>
                $"{(value.Slot == 1 ? "Основной" : "Дополнительный")}: **{TrackName(value.Track)}**"));
        var trustText = string.Join("\n", ReputationTracks.ServicePaths.Select(track =>
        {
            var posterior = profile.Tracks.GetValueOrDefault(track);
            var evidence = posterior?.EvidenceWeight ?? 0.0;
            var pathState = selectedPaths.Contains(track) ? string.Empty : " • путь не выбран";
            if (posterior == null || evidence < MinimumTrustDisplayEvidence)
            {
                return $"• **{TrackName(track)}** — недостаточно данных; вес свидетельств {evidence:F1}{pathState}";
            }

            return $"• **{TrackName(track)}** — {posterior.Score}/1000; нижняя 90% граница {posterior.LowerBound:P0}; " +
                   $"вес свидетельств {evidence:F1}{pathState}";
        }));

        var activity = profile.Activity;
        var embed = new EmbedBuilder()
            .WithTitle($"Репутация • {profile.Name}")
            .WithDescription(
                $"**Общая репутация: {profile.General.Score}/1000**\n" +
                $"Оценка надёжности: {profile.General.Mean:P1}; консервативная 90% граница: {profile.General.LowerBound:P1}.\n\n" +
                "Репутация — статистическая оценка устойчивого поведения, а не сумма очков.")
            .AddField("Игровая активность",
                $"Эффективное время: **{activity.OverallHours:F0} ч**\n" +
                $"Активных недель: **{activity.ActiveWeeks}**\n" +
                $"Возраст аккаунта: **{activity.AccountAgeDays} дн.**\n" +
                $"Индекс активности: **{activity.ActivityIndex:P0}**", true)
            .AddField("Пути участия", pathText, true)
            .AddField("Доверие по направлениям", trustText)
            .WithColor(profile.Suspended ? Color.Red : Color.Blue)
            .WithFooter("RUCM Community Governance • байесовская репутация v2")
            .Build();
        await RespondAsync(embed: embed, ephemeral: true);
    });

    [SlashCommand("пути", "Выбрать один или два направления помощи сообществу")]
    public Task PathsAsync(
        [Summary("основной", "Основной путь участия")]
        [Choice("Поддержка игроков", ReputationTracks.Support)]
        [Choice("Модерация", ReputationTracks.Moderation)]
        [Choice("Community Court", ReputationTracks.Jury)]
        [Choice("События", ReputationTracks.Event)]
        [Choice("Контрибьюторство", ReputationTracks.Contributor)] string primary,
        [Summary("дополнительный", "Необязательный второй путь")]
        [Choice("Нет", "none")]
        [Choice("Поддержка игроков", ReputationTracks.Support)]
        [Choice("Модерация", ReputationTracks.Moderation)]
        [Choice("Community Court", ReputationTracks.Jury)]
        [Choice("События", ReputationTracks.Event)]
        [Choice("Контрибьюторство", ReputationTracks.Contributor)] string secondary = "none") => ExecuteAsync(async () =>
    {
        var user = await community.RequireUserAsync(Context.User.Id);
        await reputation.SetPathsAsync(user.Id, primary, secondary == "none" ? null : secondary);
        await RespondAsync(
            $"Пути сохранены: **{TrackName(primary)}**" +
            (secondary == "none" ? "." : $" + **{TrackName(secondary)}**."),
            ephemeral: true);
    });

    [SlashCommand("история", "Показать последние статистические события репутации")]
    public Task HistoryAsync() => ExecuteAsync(async () =>
    {
        var user = await community.RequireUserAsync(Context.User.Id);
        var rows = await history.GetAsync(user.Id, 25);
        var description = rows.Count == 0
            ? "Значимых репутационных наблюдений пока нет. Игровая активность всё равно участвует в базовой оценке."
            : string.Join("\n", rows.Select(value =>
            {
                var signal = value.SuccessWeight > 0 && value.FailureWeight > 0
                    ? $"+{value.SuccessWeight:F2} / −{value.FailureWeight:F2}"
                    : value.SuccessWeight > 0
                        ? $"+{value.SuccessWeight:F2}"
                        : $"−{value.FailureWeight:F2}";
                var auditOnly = ReputationMath.IsAuthoritativeReason(value.Reason)
                    ? string.Empty
                    : " • _архив, не участвует в расчёте v2_";
                return $"• <t:{new DateTimeOffset(value.OccurredAt).ToUnixTimeSeconds()}:d> " +
                       $"**{TrackName(value.Track)}** • {ReasonName(value.Reason)} • `{signal}`" +
                       (value.SeriousNegative ? " ⚠️" : string.Empty) + auditOnly;
            }));
        if (description.Length > 3900)
            description = description[..3900] + "…";
        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("История репутации")
            .WithDescription(description)
            .WithColor(Color.DarkBlue)
            .WithFooter("Архивные события старой системы сохраняются для аудита, но не входят в Reputation v2. Вес актуальных событий уменьшается со временем; серьёзные ошибки реабилитируются устойчивым хорошим поведением.")
            .Build(), ephemeral: true);
    });

    private async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (CourtRuleException exception)
        {
            if (Context.Interaction.HasResponded)
                await FollowupAsync(exception.Message, ephemeral: true);
            else
                await RespondAsync(exception.Message, ephemeral: true);
        }
        catch (Exception exception)
        {
            await Logger.Error($"Reputation command failed for {Context.User.Id}", exception);
            const string message = "Не удалось выполнить действие с репутацией. Ошибка записана в журнал Discord-бота.";
            if (Context.Interaction.HasResponded)
                await FollowupAsync(message, ephemeral: true);
            else
                await RespondAsync(message, ephemeral: true);
        }
    }

    private static string TrackName(string track) => track switch
    {
        ReputationTracks.General => "Общая",
        ReputationTracks.Support => "Поддержка игроков",
        ReputationTracks.Moderation => "Модерация",
        ReputationTracks.Jury => "Community Court",
        ReputationTracks.Event => "События",
        ReputationTracks.Contributor => "Контрибьюторство",
        _ => track,
    };

    private static string ReasonName(string reason) => reason switch
    {
        ReputationReasons.AHelpResolved => "успешно обработан AHelp",
        ReputationReasons.DutyCompleted => "дежурство завершено",
        ReputationReasons.DutyFailed => "дежурство сорвано",
        ReputationReasons.JuryCompleted => "обязанность присяжного выполнена",
        ReputationReasons.JuryFailed => "принятая обязанность присяжного не выполнена",
        ReputationReasons.EventReviewCompleted => "рецензия события завершена",
        ReputationReasons.EventReviewFailed => "принятая рецензия не завершена",
        ReputationReasons.EventSessionCompleted => "событие корректно завершено",
        ReputationReasons.EventSessionAborted => "событие аварийно завершено",
        ReputationReasons.ModerationReviewCompleted => "независимый аудит завершён",
        ReputationReasons.ModerationReviewFailed => "принятый аудит не завершён",
        ReputationReasons.ModerationActionCorrect => "действие подтверждено аудитом",
        ReputationReasons.ModerationActionMinorIssue => "в действии найдены недостатки",
        ReputationReasons.ModerationActionWrong => "серьёзная ошибка модерации",
        ReputationReasons.FalseReport => "заведомо ложная жалоба",
        ReputationReasons.ContributionAccepted => "подтверждён вклад в проект",
        _ when reason.StartsWith("legacy:", StringComparison.Ordinal) => "архивное событие старой системы",
        _ => reason,
    };
}
