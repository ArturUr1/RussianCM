using System.Numerics;
using Content.Shared._RuMC14.Governance;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RuMC14.Governance;

public sealed class GovernanceAHelpQueueWindow : DefaultWindow
{
    public event Action<GovernanceAHelpQueueAction, long>? ActionRequested;

    private readonly BoxContainer _ticketList;
    private readonly RichTextLabel _details;
    private readonly Label _counter;
    private readonly Label _error;
    private readonly Button _claim;
    private readonly Button _open;
    private readonly Button _waiting;
    private readonly Button _resolve;

    private IReadOnlyList<GovernanceAHelpQueueItem> _tickets = [];
    private long? _selectedTicketId;

    public GovernanceAHelpQueueWindow()
    {
        Title = Loc.GetString("governance-ahelp-title");
        MinSize = new Vector2(820, 560);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 10,
        };

        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        var title = new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-header"),
            HorizontalExpand = true,
        };
        _counter = new Label();
        var refresh = Button(Loc.GetString("governance-ahelp-refresh"), GovernanceAHelpQueueAction.Refresh, false);
        header.AddChild(title);
        header.AddChild(_counter);
        header.AddChild(refresh);

        var body = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 12,
            VerticalExpand = true,
        };

        var queueColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        queueColumn.AddChild(new Label { Text = Loc.GetString("governance-ahelp-list-title") });
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };
        _ticketList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };
        scroll.AddChild(_ticketList);
        queueColumn.AddChild(scroll);

        var detailsColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        detailsColumn.AddChild(new Label { Text = Loc.GetString("governance-ahelp-details-title") });
        _details = new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-select-ticket"),
            VerticalExpand = true,
        };
        detailsColumn.AddChild(_details);

        _claim = Button(Loc.GetString("governance-ahelp-claim"), GovernanceAHelpQueueAction.Claim);
        _open = Button(Loc.GetString("governance-ahelp-open"), GovernanceAHelpQueueAction.OpenChat);
        _waiting = Button(Loc.GetString("governance-ahelp-waiting"), GovernanceAHelpQueueAction.WaitingPlayer);
        _resolve = Button(Loc.GetString("governance-ahelp-resolve"), GovernanceAHelpQueueAction.Resolve);

        var primaryActions = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        primaryActions.AddChild(_claim);
        primaryActions.AddChild(_open);

        var statusActions = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        statusActions.AddChild(_waiting);
        statusActions.AddChild(_resolve);

        detailsColumn.AddChild(primaryActions);
        detailsColumn.AddChild(statusActions);

        body.AddChild(queueColumn);
        body.AddChild(detailsColumn);

        _error = new Label { StyleClasses = { "LabelDanger" } };

        root.AddChild(header);
        root.AddChild(new RichTextLabel { Text = Loc.GetString("governance-ahelp-description") });
        root.AddChild(body);
        root.AddChild(_error);
        Contents.AddChild(root);

        UpdateActionState();
    }

    public void UpdateQueue(IReadOnlyList<GovernanceAHelpQueueItem> tickets, string? error)
    {
        _tickets = tickets;
        _error.Text = error ?? string.Empty;
        _counter.Text = Loc.GetString("governance-ahelp-counter", ("count", tickets.Count));
        _ticketList.RemoveAllChildren();

        if (tickets.Count == 0)
        {
            _ticketList.AddChild(new RichTextLabel { Text = Loc.GetString("governance-ahelp-empty") });
            _selectedTicketId = null;
            UpdateSelection();
            return;
        }

        foreach (var ticket in tickets)
        {
            var summary = ticket.Summary.Length > 120 ? ticket.Summary[..120] + "…" : ticket.Summary;
            var status = ticket.ClaimedByMe
                ? Loc.GetString("governance-ahelp-status-mine")
                : Loc.GetString("governance-ahelp-status-open");
            var button = new Button
            {
                Text = Loc.GetString(
                    "governance-ahelp-ticket-card",
                    ("id", ticket.Id),
                    ("reporter", ticket.ReporterName),
                    ("status", status),
                    ("time", ticket.CreatedAt.ToLocalTime().ToString("HH:mm")),
                    ("summary", summary)),
                HorizontalExpand = true,
            };
            var selectedId = ticket.Id;
            button.OnPressed += _ =>
            {
                _selectedTicketId = selectedId;
                UpdateSelection();
            };
            _ticketList.AddChild(button);
        }

        if (_selectedTicketId == null || tickets.All(ticket => ticket.Id != _selectedTicketId.Value))
            _selectedTicketId = tickets[0].Id;
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        var selected = _selectedTicketId == null
            ? null
            : _tickets.FirstOrDefault(ticket => ticket.Id == _selectedTicketId.Value);
        if (selected == null)
        {
            _details.Text = Loc.GetString("governance-ahelp-select-ticket");
            UpdateActionState();
            return;
        }

        var summary = FormattedMessage.EscapeText(selected.Summary);
        var reporter = FormattedMessage.EscapeText(selected.ReporterName);
        var status = selected.ClaimedByMe
            ? Loc.GetString("governance-ahelp-status-mine")
            : Loc.GetString("governance-ahelp-status-open");
        _details.Text = Loc.GetString(
            "governance-ahelp-ticket-details",
            ("id", selected.Id),
            ("reporter", reporter),
            ("status", status),
            ("time", selected.CreatedAt.ToLocalTime().ToString("HH:mm:ss")),
            ("summary", summary));
        UpdateActionState();
    }

    private void UpdateActionState()
    {
        var selected = _selectedTicketId == null
            ? null
            : _tickets.FirstOrDefault(ticket => ticket.Id == _selectedTicketId.Value);
        var hasSelection = selected != null;
        var mine = selected?.ClaimedByMe == true;
        _claim.Disabled = !hasSelection || mine;
        _open.Disabled = !mine;
        _waiting.Disabled = !mine;
        _resolve.Disabled = !mine;
    }

    private Button Button(string text, GovernanceAHelpQueueAction action, bool ticketRequired = true)
    {
        var button = new Button { Text = text, HorizontalExpand = true };
        button.OnPressed += _ =>
        {
            var ticketId = _selectedTicketId ?? 0;
            if (ticketRequired && ticketId == 0)
            {
                _error.Text = Loc.GetString("governance-ahelp-select-ticket");
                return;
            }
            ActionRequested?.Invoke(action, ticketRequired ? ticketId : 0);
        };
        return button;
    }
}
