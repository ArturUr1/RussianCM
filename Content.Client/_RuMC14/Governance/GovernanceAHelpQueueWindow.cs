using System.Numerics;
using System.Linq;
using Content.Shared._RuMC14.Governance;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RuMC14.Governance;

public sealed class GovernanceAHelpQueueWindow : DefaultWindow
{
    public event Action<GovernanceAHelpQueueAction, long>? ActionRequested;

    private readonly RichTextLabel _queue;
    private readonly Label _error;
    private readonly LineEdit _ticketId;

    public GovernanceAHelpQueueWindow()
    {
        Title = Loc.GetString("governance-ahelp-title");
        MinSize = new Vector2(680, 480);

        _queue = new RichTextLabel { VerticalExpand = true };
        _error = new Label { StyleClasses = { "LabelDanger" } };
        _ticketId = new LineEdit
        {
            PlaceHolder = Loc.GetString("governance-ahelp-ticket-placeholder"),
            HorizontalExpand = true,
        };

        var actions = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        actions.AddChild(Button("governance-ahelp-refresh", GovernanceAHelpQueueAction.Refresh, false));
        actions.AddChild(Button("governance-ahelp-claim", GovernanceAHelpQueueAction.Claim));
        actions.AddChild(Button("governance-ahelp-open", GovernanceAHelpQueueAction.OpenChat));
        actions.AddChild(Button("governance-ahelp-waiting", GovernanceAHelpQueueAction.WaitingPlayer));
        actions.AddChild(Button("governance-ahelp-resolve", GovernanceAHelpQueueAction.Resolve));

        var layout = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 10,
        };
        layout.AddChild(new Label { Text = Loc.GetString("governance-ahelp-description") });
        layout.AddChild(_queue);
        layout.AddChild(_error);
        layout.AddChild(_ticketId);
        layout.AddChild(actions);
        Contents.AddChild(layout);
    }

    public void UpdateQueue(IReadOnlyList<GovernanceAHelpQueueItem> tickets, string? error)
    {
        _error.Text = error ?? string.Empty;
        if (tickets.Count == 0)
        {
            _queue.Text = Loc.GetString("governance-ahelp-empty");
            return;
        }

        _queue.Text = string.Join("\n\n", tickets.Select(ticket =>
        {
            var summary = ticket.Summary.Length > 320 ? ticket.Summary[..320] + "…" : ticket.Summary;
            summary = FormattedMessage.EscapeText(summary);
            var reporter = FormattedMessage.EscapeText(ticket.ReporterName);
            var status = ticket.ClaimedByMe
                ? Loc.GetString("governance-ahelp-status-mine")
                : Loc.GetString("governance-ahelp-status-open");
            return $"[bold]#{ticket.Id}[/bold] • {reporter} • {status} • {ticket.CreatedAt.ToLocalTime():HH:mm}\n{summary}";
        }));
    }

    private Button Button(string locale, GovernanceAHelpQueueAction action, bool ticketRequired = true)
    {
        var button = new Button { Text = Loc.GetString(locale) };
        button.OnPressed += _ =>
        {
            var ticketId = 0L;
            if (ticketRequired && !long.TryParse(_ticketId.Text, out ticketId))
            {
                _error.Text = Loc.GetString("governance-ahelp-ticket-invalid");
                return;
            }
            ActionRequested?.Invoke(action, ticketRequired ? ticketId : 0);
        };
        return button;
    }
}
