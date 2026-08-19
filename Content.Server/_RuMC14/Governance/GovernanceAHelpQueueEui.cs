using System.Linq;
using System.Threading.Tasks;
using Content.Server.EUI;
using Content.Shared._RuMC14.Governance;
using Content.Shared.Eui;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._RuMC14.Governance;

public sealed class GovernanceAHelpQueueEui : BaseEui
{
    private readonly GovernanceAHelpSystem _system =
        IoCManager.Resolve<IEntityManager>().System<GovernanceAHelpSystem>();

    private GovernanceAHelpQueueItem[] _tickets = [];
    private GovernanceAHelpTranscriptEntry[] _transcript = [];
    private long _selectedTicketId;
    private string? _error;
    private bool _busy;

    public override void Opened()
    {
        base.Opened();
        _system.RegisterResponderEui(this);
        _ = HandleAsync(new GovernanceAHelpQueueMessage(GovernanceAHelpQueueAction.Refresh));
    }

    public override void Closed()
    {
        _system.UnregisterResponderEui(this);
        base.Closed();
    }

    public override EuiStateBase GetNewState() => new GovernanceAHelpQueueEuiState(
        _tickets,
        _selectedTicketId,
        _transcript,
        _error);

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (_busy || msg is not GovernanceAHelpQueueMessage action)
            return;
        _ = HandleAsync(action);
    }

    public async Task RefreshFromSystemAsync()
    {
        if (!_busy)
            await RefreshAsync();
    }

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
                case GovernanceAHelpQueueAction.SelectTicket:
                    _selectedTicketId = message.TicketId;
                    break;
                case GovernanceAHelpQueueAction.Claim:
                    if (!await _system.ClaimAsync(Player, message.TicketId))
                    {
                        _error = Loc.GetString("governance-ahelp-claim-failed");
                    }
                    else
                    {
                        _selectedTicketId = message.TicketId;
                    }
                    break;
                case GovernanceAHelpQueueAction.SendMessage:
                    if (string.IsNullOrWhiteSpace(message.Text) ||
                        !await _system.SendResponderMessageAsync(Player, message.TicketId, message.Text))
                        _error = Loc.GetString("governance-ahelp-send-failed");
                    break;
                case GovernanceAHelpQueueAction.WaitingPlayer:
                    if (!await _system.SetStatusAsync(Player, message.TicketId, "waiting_player"))
                        _error = Loc.GetString("governance-ahelp-status-failed");
                    break;
                case GovernanceAHelpQueueAction.Resolve:
                    if (!await _system.SetStatusAsync(Player, message.TicketId, "resolved"))
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
        var queue = await _system.GetQueueAsync(Player);
        _tickets = queue.Select(value => new GovernanceAHelpQueueItem(
            value.Id,
            value.ReporterUserId,
            value.ReporterName,
            value.Summary,
            value.Status,
            value.CreatedAt.UtcDateTime,
            value.ClaimedByMe)).ToArray();

        if (_selectedTicketId == 0 || _tickets.All(ticket => ticket.Id != _selectedTicketId))
        {
            _selectedTicketId = _tickets.FirstOrDefault(ticket => ticket.ClaimedByMe)?.Id
                ?? _tickets.FirstOrDefault()?.Id
                ?? 0;
        }

        var selected = _tickets.FirstOrDefault(ticket => ticket.Id == _selectedTicketId);
        if (selected?.ClaimedByMe == true)
        {
            var transcript = await _system.GetResponderTranscriptAsync(Player, selected.Id);
            _transcript = transcript.Select(line => new GovernanceAHelpTranscriptEntry(
                line.SenderName,
                line.Body,
                line.CreatedAt.UtcDateTime,
                line.SenderUserId == Player.UserId)).ToArray();
        }
        else
        {
            _transcript = [];
        }

        StateDirty();
    }
}
