using Content.DiscordBot.Governance;
using Discord.Interactions;

namespace Content.DiscordBot.Modules;

public sealed class CourtJuryInviteComponentModule(CommunityCourtService court)
    : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("court-jury-accept:*")]
    public Task AcceptAsync(string caseId) => HandleAsync(caseId, InvitationStates.Accepted, null);

    [ComponentInteraction("court-jury-decline:*")]
    public Task DeclineAsync(string caseId) => HandleAsync(caseId, InvitationStates.Declined, null);

    [ComponentInteraction("court-jury-recuse:*")]
    public Task RecuseAsync(string caseId) => HandleAsync(
        caseId,
        InvitationStates.Recused,
        "Самоотвод выбран пользователем через кнопку приглашения.");

    private async Task HandleAsync(string caseIdText, string response, string? reason)
    {
        await DeferAsync();
        try
        {
            if (!long.TryParse(caseIdText, out var caseId) || caseId <= 0)
                throw new CourtRuleException("Некорректный номер дела в приглашении.");

            var state = await court.RespondToInvitationAsync(caseId, Context.User.Id, response, reason);
            var message = state switch
            {
                InvitationStates.Accepted => $"Вы приняли приглашение в присяжные по делу №{caseId}.",
                InvitationStates.Declined => $"Вы отказались от участия в деле №{caseId}.",
                InvitationStates.Recused => $"Самоотвод по делу №{caseId} зафиксирован.",
                _ => $"Ответ по делу №{caseId} зафиксирован: {state}.",
            };
            await FollowupAsync(message);
        }
        catch (CourtRuleException exception)
        {
            await FollowupAsync(exception.Message);
        }
        catch (Exception exception)
        {
            await Logger.Error($"Community Court jury invitation button failed for {Context.User.Id}", exception);
            await FollowupAsync("Не удалось обработать ответ на приглашение. Ошибка записана в журнал Discord-бота.");
        }
    }
}
