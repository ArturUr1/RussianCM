using System.Linq;
using System.Threading.Tasks;
using Content.Server.EUI;
using Content.Shared._RuMC14.Governance;
using Content.Shared.Eui;

namespace Content.Server._RuMC14.Governance;

public sealed class GovernanceAHelpQueueEui(GovernanceDutySystem dutySystem) : BaseEui
{
    private GovernanceAHelpQueueItem[] _tickets = [];
    private string? _error;
    private bool _busy;

    public override void Opened()
    {
        base.Opened();
        dutySystem.RegisterAHelpEui(this);
        _ = HandleAsync(new GovernanceAHelpQueueMessage(GovernanceAHelpQueueAction.Refresh));
    }

    public override void Closed()
    {
        dutySystem.UnregisterAHelpEui(this);
        base.Closed();
    }

    public override EuiStateBase GetNewState() => new GovernanceAHelpQueueEuiState(_tickets, _error);

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (_busy || msg is not GovernanceAHelpQueueMessage action)
            return;
        _ = HandleAsync(action);
    }

    public Task RefreshFromSystemAsync() => RefreshAsync();

    private async Task HandleAsync(GovernanceAHelpQueueMessage message)
    {
        _busy = true;
        _error = null;
        try
        {
            switch (message.Action)
            {
                case GovernanceAHelpQueueAction.Refresh:
                    break;
                case GovernanceAHelpQueueAction.Claim:
                    if (!await dutySystem.ClaimAHelpAsync(Player, message.TicketId))
                        _error = Loc.GetString("governance-ahelp-claim-failed");
                    else
                        await dutySystem.OpenAHelpChatAsync(Player, message.TicketId);
                    break;
                case GovernanceAHelpQueueAction.OpenChat:
                    if (!await dutySystem.OpenAHelpChatAsync(Player, message.TicketId))
                        _error = Loc.GetString("governance-ahelp-open-failed");
                    break;
                case GovernanceAHelpQueueAction.WaitingPlayer:
                    if (!await dutySystem.SetAHelpStatusAsync(Player, message.TicketId, "waiting_player"))
                        _error = Loc.GetString("governance-ahelp-status-failed");
                    break;
                case GovernanceAHelpQueueAction.Resolve:
                    if (!await dutySystem.SetAHelpStatusAsync(Player, message.TicketId, "resolved"))
                        _error = Loc.GetString("governance-ahelp-status-failed");
                    break;
            }
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            Logger.GetSawmill("governance.ahelp").Error(
                $"Governance AHelp EUI failed for {Player.UserId}: {exception}");
            _error = Loc.GetString("governance-ahelp-unavailable");
            StateDirty();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RefreshAsync()
    {
        if (_busy)
            return;
        var queue = await dutySystem.GetAHelpQueueAsync(Player);
        _tickets = queue.Select(value => new GovernanceAHelpQueueItem(
            value.Id,
            value.ReporterUserId,
            value.ReporterName,
            value.Summary,
            value.Status,
            value.CreatedAt.UtcDateTime,
            value.ClaimedByMe)).ToArray();
        StateDirty();
    }
}
