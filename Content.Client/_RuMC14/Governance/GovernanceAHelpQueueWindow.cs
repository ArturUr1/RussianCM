using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared._RuMC14.Governance;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RuMC14.Governance;

public sealed class GovernanceAHelpQueueWindow : DefaultWindow
{
    public event Action<GovernanceAHelpQueueAction, long, string?>? ActionRequested;

    private readonly BoxContainer _ticketList;
    private readonly BoxContainer _transcript;
    private readonly Label _counter;
    private readonly RichTextLabel _ticketHeader;
    private readonly RichTextLabel _ticketMeta;
    private readonly Label _error;
    private readonly LineEdit _filter;
    private readonly LineEdit _reply;
    private readonly Button _claim;
    private readonly Button _send;
    private readonly Button _waiting;
    private readonly Button _resolve;

    private IReadOnlyList<GovernanceAHelpQueueItem> _tickets = [];
    private long _selectedTicketId;

    public GovernanceAHelpQueueWindow()
    {
        Title = Loc.GetString("governance-ahelp-title");
        MinSize = new Vector2(1040, 650);
        Stylesheet = IoCManager.Resolve<IStylesheetManager>().SheetNano;
        CrtLobbyTheme.ApplyWindow(this, includeChat: true, useCrtTypography: false);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 12,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var header = new PanelContainer
        {
            StyleClasses = { StyleNano.StyleClassCrtPanel },
            HorizontalExpand = true,
        };
        var headerContent = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 12,
            HorizontalExpand = true,
        };
        var heading = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 2,
        };
        heading.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-workspace-header"),
        });
        heading.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-workspace-subtitle"),
        });
        _counter = new Label();
        var refresh = new Button { Text = Loc.GetString("governance-ahelp-refresh") };
        refresh.OnPressed += _ => ActionRequested?.Invoke(GovernanceAHelpQueueAction.Refresh, 0, null);
        headerContent.AddChild(heading);
        headerContent.AddChild(_counter);
        headerContent.AddChild(refresh);
        header.AddChild(headerContent);
        root.AddChild(header);

        var body = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 12,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var queuePanel = new PanelContainer
        {
            StyleClasses = { StyleNano.StyleClassCrtInsetPanel },
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        var queueColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        queueColumn.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-list-heading"),
        });
        queueColumn.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-list-hint"),
        });
        _filter = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("governance-ahelp-filter-placeholder"),
        };
        _filter.OnTextChanged += _ => RebuildTicketList();
        queueColumn.AddChild(_filter);
        var queueScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        _ticketList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        queueScroll.AddChild(_ticketList);
        queueColumn.AddChild(queueScroll);
        queuePanel.AddChild(queueColumn);
        body.AddChild(queuePanel);

        var conversationPanel = new PanelContainer
        {
            StyleClasses = { StyleNano.StyleClassCrtInsetPanel },
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        var conversation = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        _ticketHeader = new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-select-ticket"),
        };
        _ticketMeta = new RichTextLabel();
        conversation.AddChild(_ticketHeader);
        conversation.AddChild(_ticketMeta);

        var transcriptScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        _transcript = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 7,
            HorizontalExpand = true,
        };
        transcriptScroll.AddChild(_transcript);
        conversation.AddChild(transcriptScroll);

        var templates = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        var helloTemplate = new Button { Text = Loc.GetString("governance-ahelp-template-greeting") };
        helloTemplate.OnPressed += _ => _reply.Text = Loc.GetString("governance-ahelp-template-greeting-text");
        var detailsTemplate = new Button { Text = Loc.GetString("governance-ahelp-template-details") };
        detailsTemplate.OnPressed += _ => _reply.Text = Loc.GetString("governance-ahelp-template-details-text");
        var waitTemplate = new Button { Text = Loc.GetString("governance-ahelp-template-wait") };
        waitTemplate.OnPressed += _ => _reply.Text = Loc.GetString("governance-ahelp-template-wait-text");
        templates.AddChild(helloTemplate);
        templates.AddChild(detailsTemplate);
        templates.AddChild(waitTemplate);
        conversation.AddChild(templates);

        var composer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        _reply = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("governance-ahelp-reply-placeholder"),
        };
        _reply.OnTextEntered += args => SendReply(args.Text);
        _send = new Button
        {
            Text = Loc.GetString("governance-ahelp-send"),
        };
        _send.OnPressed += _ => SendReply(_reply.Text);
        composer.AddChild(_reply);
        composer.AddChild(_send);
        conversation.AddChild(composer);

        var actions = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
        };
        _claim = ActionButton(Loc.GetString("governance-ahelp-claim"), GovernanceAHelpQueueAction.Claim);
        _waiting = ActionButton(Loc.GetString("governance-ahelp-waiting"), GovernanceAHelpQueueAction.WaitingPlayer);
        _resolve = ActionButton(Loc.GetString("governance-ahelp-resolve"), GovernanceAHelpQueueAction.Resolve);
        actions.AddChild(_claim);
        actions.AddChild(_waiting);
        actions.AddChild(_resolve);
        conversation.AddChild(actions);

        _error = new Label
        {
            StyleClasses = { "LabelDanger" },
        };
        conversation.AddChild(_error);

        conversationPanel.AddChild(conversation);
        body.AddChild(conversationPanel);
        root.AddChild(body);
        Contents.AddChild(root);

        UpdateActionState();
    }

    public void UpdateState(GovernanceAHelpQueueEuiState state)
    {
        _tickets = state.Tickets;
        _selectedTicketId = state.SelectedTicketId;
        _error.Text = state.Error ?? string.Empty;
        var mine = state.Tickets.Count(ticket => ticket.ClaimedByMe);
        var open = state.Tickets.Count(ticket => !ticket.ClaimedByMe && ticket.Status == "open");
        _counter.Text = Loc.GetString(
            "governance-ahelp-counter-modern",
            ("open", open),
            ("mine", mine));

        RebuildTicketList();
        UpdateSelectedTicket(state.Transcript);
        UpdateActionState();
    }

    private void RebuildTicketList()
    {
        _ticketList.RemoveAllChildren();
        var filter = _filter.Text.Trim();
        var visible = _tickets
            .Where(ticket => string.IsNullOrWhiteSpace(filter) ||
                             ticket.Id.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                             ticket.ReporterName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                             ticket.Summary.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (visible.Length == 0)
        {
            _ticketList.AddChild(new RichTextLabel
            {
                Text = _tickets.Count == 0
                    ? Loc.GetString("governance-ahelp-empty-modern")
                    : Loc.GetString("governance-ahelp-filter-empty"),
            });
            return;
        }

        foreach (var ticket in visible)
        {
            var summary = ticket.Summary.Length > 110 ? ticket.Summary[..110] + "…" : ticket.Summary;
            var status = StatusText(ticket);
            var selected = ticket.Id == _selectedTicketId
                ? Loc.GetString("governance-ahelp-selected-marker")
                : string.Empty;
            var button = new Button
            {
                HorizontalExpand = true,
                Text = Loc.GetString(
                    "governance-ahelp-ticket-card-modern",
                    ("selected", selected),
                    ("id", ticket.Id),
                    ("reporter", ticket.ReporterName),
                    ("status", status),
                    ("time", ticket.CreatedAt.ToLocalTime().ToString("HH:mm")),
                    ("summary", summary)),
            };
            var id = ticket.Id;
            button.OnPressed += _ => ActionRequested?.Invoke(GovernanceAHelpQueueAction.SelectTicket, id, null);
            _ticketList.AddChild(button);
        }
    }

    private void UpdateSelectedTicket(IReadOnlyList<GovernanceAHelpTranscriptEntry> transcript)
    {
        var selected = _tickets.FirstOrDefault(ticket => ticket.Id == _selectedTicketId);
        _transcript.RemoveAllChildren();

        if (selected == null)
        {
            _ticketHeader.Text = Loc.GetString("governance-ahelp-select-ticket");
            _ticketMeta.Text = string.Empty;
            _transcript.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("governance-ahelp-no-selection-hint"),
            });
            return;
        }

        _ticketHeader.Text = Loc.GetString(
            "governance-ahelp-conversation-header",
            ("id", selected.Id),
            ("reporter", FormattedMessage.EscapeText(selected.ReporterName)));
        _ticketMeta.Text = Loc.GetString(
            "governance-ahelp-conversation-meta",
            ("status", StatusText(selected)),
            ("time", selected.CreatedAt.ToLocalTime().ToString("HH:mm:ss")),
            ("uuid", selected.ReporterUserId.ToString()));

        if (!selected.ClaimedByMe)
        {
            _transcript.AddChild(new RichTextLabel
            {
                Text = Loc.GetString(
                    "governance-ahelp-unclaimed-preview",
                    ("summary", FormattedMessage.EscapeText(selected.Summary))),
            });
            return;
        }

        if (transcript.Count == 0)
        {
            _transcript.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("governance-ahelp-transcript-empty"),
            });
            return;
        }

        foreach (var line in transcript)
        {
            var sender = FormattedMessage.EscapeText(line.SenderName);
            var body = FormattedMessage.EscapeText(line.Body);
            var role = line.FromResponder
                ? Loc.GetString("governance-ahelp-message-role-responder")
                : Loc.GetString("governance-ahelp-message-role-player");
            _transcript.AddChild(new RichTextLabel
            {
                Text = Loc.GetString(
                    "governance-ahelp-message-line",
                    ("time", line.CreatedAt.ToLocalTime().ToString("HH:mm")),
                    ("role", role),
                    ("sender", sender),
                    ("body", body)),
            });
        }
    }

    private void SendReply(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _selectedTicketId == 0)
            return;

        ActionRequested?.Invoke(
            GovernanceAHelpQueueAction.SendMessage,
            _selectedTicketId,
            text.Trim());
        _reply.Clear();
    }

    private Button ActionButton(string text, GovernanceAHelpQueueAction action)
    {
        var button = new Button
        {
            Text = text,
            HorizontalExpand = true,
        };
        button.OnPressed += _ =>
        {
            if (_selectedTicketId == 0)
            {
                _error.Text = Loc.GetString("governance-ahelp-select-ticket");
                return;
            }

            ActionRequested?.Invoke(action, _selectedTicketId, null);
        };
        return button;
    }

    private void UpdateActionState()
    {
        var selected = _tickets.FirstOrDefault(ticket => ticket.Id == _selectedTicketId);
        var mine = selected?.ClaimedByMe == true;
        _claim.Disabled = selected == null || mine;
        _waiting.Disabled = !mine;
        _resolve.Disabled = !mine;
        _reply.Editable = mine;
        _send.Disabled = !mine;
    }

    private static string StatusText(GovernanceAHelpQueueItem ticket)
    {
        if (ticket.ClaimedByMe)
        {
            return ticket.Status switch
            {
                "waiting_player" => Loc.GetString("governance-ahelp-status-waiting-player"),
                _ => Loc.GetString("governance-ahelp-status-mine"),
            };
        }

        return ticket.Status switch
        {
            "open" => Loc.GetString("governance-ahelp-status-open"),
            _ => ticket.Status,
        };
    }
}
