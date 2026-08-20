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
    public event Action<GovernanceAHelpQueueAction, long, string?, string?>? ActionRequested;

    private readonly BoxContainer _ticketList;
    private readonly BoxContainer _approvalList;
    private readonly BoxContainer _transcript;
    private readonly BoxContainer _incidentActionList;
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
    private readonly LineEdit _recordsTarget;
    private readonly Button _openFullLogs;
    private readonly Button _openNotes;
    private readonly RichTextLabel _incidentStatus;
    private readonly RichTextLabel _courtStatus;
    private readonly LineEdit _incidentTarget;
    private readonly LineEdit _incidentType;
    private readonly Button _createIncident;
    private readonly LineEdit _actionReason;
    private readonly LineEdit _freezeSeconds;
    private readonly Button _requestExplanation;
    private readonly Button _freeze;
    private readonly Button _roundRemove;
    private readonly Button _escalateCourt;

    private IReadOnlyList<GovernanceAHelpQueueItem> _tickets = [];
    private long _selectedTicketId;
    private long _incidentId;
    private long _courtCaseId;
    private long _lastRenderedTicketId;

    public GovernanceAHelpQueueWindow()
    {
        Title = Loc.GetString("governance-ahelp-title");
        MinSize = new Vector2(1180, 720);
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
        refresh.OnPressed += _ => ActionRequested?.Invoke(GovernanceAHelpQueueAction.Refresh, 0, null, null);
        headerContent.AddChild(heading);
        headerContent.AddChild(_counter);
        headerContent.AddChild(refresh);
        header.AddChild(headerContent);
        root.AddChild(header);

        var recordsPanel = new PanelContainer
        {
            StyleClasses = { StyleNano.StyleClassCrtPanel },
            HorizontalExpand = true,
        };
        var recordsRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        recordsRow.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-records-heading"),
        });
        _recordsTarget = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("governance-ahelp-records-target-placeholder"),
        };
        _openNotes = new Button
        {
            Text = Loc.GetString("governance-ahelp-records-open-notes"),
        };
        _openNotes.OnPressed += _ => OpenNotes();
        _openFullLogs = new Button
        {
            Text = Loc.GetString("governance-ahelp-records-open-logs"),
        };
        _openFullLogs.OnPressed += _ => ActionRequested?.Invoke(
            GovernanceAHelpQueueAction.OpenFullLogs,
            0,
            null,
            null);
        recordsRow.AddChild(_recordsTarget);
        recordsRow.AddChild(_openNotes);
        recordsRow.AddChild(_openFullLogs);
        recordsPanel.AddChild(recordsRow);
        root.AddChild(recordsPanel);

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

        var approvalPanel = new PanelContainer
        {
            StyleClasses = { StyleNano.StyleClassCrtPanel },
            HorizontalExpand = true,
        };
        var approvalColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };
        approvalColumn.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-approval-heading"),
        });
        _approvalList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 5,
            HorizontalExpand = true,
        };
        approvalColumn.AddChild(_approvalList);
        approvalPanel.AddChild(approvalColumn);
        queueColumn.AddChild(approvalPanel);

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

        var incidentPanel = new PanelContainer
        {
            StyleClasses = { StyleNano.StyleClassCrtPanel },
            HorizontalExpand = true,
        };
        var incidentColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        incidentColumn.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-incident-heading"),
        });
        _incidentStatus = new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-incident-none"),
        };
        incidentColumn.AddChild(_incidentStatus);
        _courtStatus = new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-court-none"),
        };
        incidentColumn.AddChild(_courtStatus);

        var incidentInputs = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        _incidentTarget = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("governance-ahelp-incident-target-placeholder"),
        };
        _incidentType = new LineEdit
        {
            HorizontalExpand = true,
            Text = Loc.GetString("governance-ahelp-incident-type-default"),
            PlaceHolder = Loc.GetString("governance-ahelp-incident-type-placeholder"),
        };
        _createIncident = new Button
        {
            Text = Loc.GetString("governance-ahelp-incident-create"),
        };
        _createIncident.OnPressed += _ => CreateIncident();
        incidentInputs.AddChild(_incidentTarget);
        incidentInputs.AddChild(_incidentType);
        incidentInputs.AddChild(_createIncident);
        incidentColumn.AddChild(incidentInputs);

        incidentColumn.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-actions-heading"),
        });
        var actionInputs = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        _actionReason = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("governance-ahelp-action-reason-placeholder"),
        };
        _freezeSeconds = new LineEdit
        {
            Text = "60",
            PlaceHolder = Loc.GetString("governance-ahelp-action-freeze-seconds-placeholder"),
        };
        actionInputs.AddChild(_actionReason);
        actionInputs.AddChild(_freezeSeconds);
        incidentColumn.AddChild(actionInputs);

        var moderationButtons = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        _requestExplanation = IncidentActionButton(
            Loc.GetString("governance-ahelp-action-request-explanation"),
            GovernanceAHelpQueueAction.RequestExplanation);
        _freeze = IncidentActionButton(
            Loc.GetString("governance-ahelp-action-freeze"),
            GovernanceAHelpQueueAction.Freeze);
        _roundRemove = IncidentActionButton(
            Loc.GetString("governance-ahelp-action-round-remove"),
            GovernanceAHelpQueueAction.RoundRemove);
        _escalateCourt = new Button
        {
            Text = Loc.GetString("governance-ahelp-court-escalate"),
            HorizontalExpand = true,
        };
        _escalateCourt.OnPressed += _ => EscalateToCourt();
        moderationButtons.AddChild(_requestExplanation);
        moderationButtons.AddChild(_freeze);
        moderationButtons.AddChild(_roundRemove);
        moderationButtons.AddChild(_escalateCourt);
        incidentColumn.AddChild(moderationButtons);

        incidentColumn.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-action-history-heading"),
        });
        _incidentActionList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalExpand = true,
        };
        incidentColumn.AddChild(_incidentActionList);

        incidentPanel.AddChild(incidentColumn);
        conversation.AddChild(incidentPanel);

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
        var detailsTemplate = new Button { Text = Loc.GetString("governance-ahelp-template-details") };
        var waitTemplate = new Button { Text = Loc.GetString("governance-ahelp-template-wait") };
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
        helloTemplate.OnPressed += _ => _reply.Text = Loc.GetString("governance-ahelp-template-greeting-text");
        detailsTemplate.OnPressed += _ => _reply.Text = Loc.GetString("governance-ahelp-template-details-text");
        waitTemplate.OnPressed += _ => _reply.Text = Loc.GetString("governance-ahelp-template-wait-text");
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
        _incidentId = state.IncidentId;
        _courtCaseId = state.CourtCaseId;
        _error.Text = state.Error ?? string.Empty;
        var mine = state.Tickets.Count(ticket => ticket.ClaimedByMe);
        var open = state.Tickets.Count(ticket => !ticket.ClaimedByMe && ticket.Status == "open");
        _counter.Text = Loc.GetString(
            "governance-ahelp-counter-modern",
            ("open", open),
            ("mine", mine));

        RebuildTicketList();
        RebuildPendingApprovals(state.PendingApprovals);
        UpdateSelectedTicket(
            state.Transcript,
            state.IncidentTargetName,
            state.IncidentType,
            state.CourtCaseId,
            state.IncidentActions);
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
            button.OnPressed += _ => ActionRequested?.Invoke(GovernanceAHelpQueueAction.SelectTicket, id, null, null);
            _ticketList.AddChild(button);
        }
    }

    private void RebuildPendingApprovals(IReadOnlyList<GovernanceAHelpPendingApprovalEntry> approvals)
    {
        _approvalList.RemoveAllChildren();
        if (approvals.Count == 0)
        {
            _approvalList.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("governance-ahelp-approval-empty"),
            });
            return;
        }

        foreach (var approval in approvals)
        {
            var card = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 3,
                HorizontalExpand = true,
            };
            card.AddChild(new RichTextLabel
            {
                Text = Loc.GetString(
                    "governance-ahelp-approval-card",
                    ("id", approval.ActionId),
                    ("incident", approval.IncidentId),
                    ("actor", FormattedMessage.EscapeText(approval.ActorName)),
                    ("target", FormattedMessage.EscapeText(approval.TargetName)),
                    ("type", ActionTypeText(approval.ActionType)),
                    ("reason", FormattedMessage.EscapeText(approval.Reason)),
                    ("approvals", approval.Approvals),
                    ("required", approval.RequiredApprovals)),
            });

            var reviewButtons = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 4,
                HorizontalExpand = true,
            };
            var approve = new Button
            {
                Text = Loc.GetString("governance-ahelp-approval-approve"),
                HorizontalExpand = true,
            };
            var reject = new Button
            {
                Text = Loc.GetString("governance-ahelp-approval-reject"),
                HorizontalExpand = true,
            };
            var actionId = approval.ActionId;
            approve.OnPressed += _ => ActionRequested?.Invoke(
                GovernanceAHelpQueueAction.ApproveModerationAction,
                actionId,
                null,
                null);
            reject.OnPressed += _ => ActionRequested?.Invoke(
                GovernanceAHelpQueueAction.RejectModerationAction,
                actionId,
                null,
                null);
            reviewButtons.AddChild(approve);
            reviewButtons.AddChild(reject);
            card.AddChild(reviewButtons);
            _approvalList.AddChild(card);
        }
    }

    private void UpdateSelectedTicket(
        IReadOnlyList<GovernanceAHelpTranscriptEntry> transcript,
        string incidentTargetName,
        string incidentType,
        long courtCaseId,
        IReadOnlyList<GovernanceAHelpModerationActionEntry> incidentActions)
    {
        var selected = _tickets.FirstOrDefault(ticket => ticket.Id == _selectedTicketId);
        _transcript.RemoveAllChildren();
        _incidentActionList.RemoveAllChildren();

        if (selected == null)
        {
            _ticketHeader.Text = Loc.GetString("governance-ahelp-select-ticket");
            _ticketMeta.Text = string.Empty;
            _incidentStatus.Text = Loc.GetString("governance-ahelp-incident-none");
            _courtStatus.Text = Loc.GetString("governance-ahelp-court-none");
            _incidentActionList.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("governance-ahelp-action-history-empty"),
            });
            _transcript.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("governance-ahelp-no-selection-hint"),
            });
            return;
        }

        if (_lastRenderedTicketId != selected.Id)
        {
            _lastRenderedTicketId = selected.Id;
            _incidentTarget.Clear();
            _incidentType.Text = Loc.GetString("governance-ahelp-incident-type-default");
            _actionReason.Clear();
            _freezeSeconds.Text = "60";
            if (!string.IsNullOrWhiteSpace(incidentTargetName))
                _recordsTarget.Text = incidentTargetName;
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

        _incidentStatus.Text = _incidentId > 0
            ? Loc.GetString(
                "governance-ahelp-incident-active",
                ("id", _incidentId),
                ("target", FormattedMessage.EscapeText(incidentTargetName)),
                ("type", FormattedMessage.EscapeText(incidentType)))
            : Loc.GetString("governance-ahelp-incident-none");
        _courtStatus.Text = courtCaseId > 0
            ? Loc.GetString("governance-ahelp-court-active", ("id", courtCaseId))
            : Loc.GetString("governance-ahelp-court-none");

        RebuildIncidentActions(incidentActions);

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

    private void RebuildIncidentActions(IReadOnlyList<GovernanceAHelpModerationActionEntry> actions)
    {
        if (actions.Count == 0)
        {
            _incidentActionList.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("governance-ahelp-action-history-empty"),
            });
            return;
        }

        foreach (var action in actions)
        {
            var duration = action.DurationSeconds > 0
                ? Loc.GetString("governance-ahelp-action-duration", ("seconds", action.DurationSeconds))
                : string.Empty;
            _incidentActionList.AddChild(new RichTextLabel
            {
                Text = Loc.GetString(
                    "governance-ahelp-action-card",
                    ("id", action.Id),
                    ("type", ActionTypeText(action.ActionType)),
                    ("status", ActionStatusText(action.Status)),
                    ("approvals", action.Approvals),
                    ("required", action.RequiredApprovals),
                    ("duration", duration),
                    ("reason", FormattedMessage.EscapeText(action.Reason))),
            });
        }
    }

    private void CreateIncident()
    {
        if (_selectedTicketId == 0)
        {
            _error.Text = Loc.GetString("governance-ahelp-select-ticket");
            return;
        }

        var target = _incidentTarget.Text.Trim();
        var type = _incidentType.Text.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            _error.Text = Loc.GetString("governance-ahelp-incident-target-required");
            return;
        }

        ActionRequested?.Invoke(
            GovernanceAHelpQueueAction.CreateIncident,
            _selectedTicketId,
            target,
            type);
    }

    private void OpenNotes()
    {
        var target = _recordsTarget.Text.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            _error.Text = Loc.GetString("governance-ahelp-notes-target-required");
            return;
        }

        ActionRequested?.Invoke(
            GovernanceAHelpQueueAction.OpenPlayerNotes,
            0,
            target,
            null);
    }

    private void EscalateToCourt()
    {
        if (_selectedTicketId == 0 || _incidentId == 0)
        {
            _error.Text = Loc.GetString("governance-ahelp-action-no-incident");
            return;
        }

        var reason = _actionReason.Text.Trim();
        if (reason.Length is < 10 or > 512)
        {
            _error.Text = Loc.GetString("governance-ahelp-court-reason-invalid");
            return;
        }

        ActionRequested?.Invoke(
            GovernanceAHelpQueueAction.EscalateToCourt,
            _selectedTicketId,
            reason,
            null);
    }

    private Button IncidentActionButton(string text, GovernanceAHelpQueueAction action)
    {
        var button = new Button
        {
            Text = text,
            HorizontalExpand = true,
        };
        button.OnPressed += _ => RunIncidentAction(action);
        return button;
    }

    private void RunIncidentAction(GovernanceAHelpQueueAction action)
    {
        if (_selectedTicketId == 0 || _incidentId == 0)
        {
            _error.Text = Loc.GetString("governance-ahelp-action-no-incident");
            return;
        }

        var reason = _actionReason.Text.Trim();
        if (reason.Length is < 10 or > 512)
        {
            _error.Text = Loc.GetString("governance-ahelp-action-reason-invalid");
            return;
        }

        string? auxiliary = null;
        if (action == GovernanceAHelpQueueAction.Freeze)
        {
            if (!int.TryParse(_freezeSeconds.Text.Trim(), out var seconds) || seconds < 1 || seconds > 120)
            {
                _error.Text = Loc.GetString("governance-ahelp-action-freeze-duration-invalid");
                return;
            }
            auxiliary = seconds.ToString();
        }

        ActionRequested?.Invoke(action, _selectedTicketId, reason, auxiliary);
    }

    private void SendReply(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _selectedTicketId == 0)
            return;

        ActionRequested?.Invoke(
            GovernanceAHelpQueueAction.SendMessage,
            _selectedTicketId,
            text.Trim(),
            null);
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

            ActionRequested?.Invoke(action, _selectedTicketId, null, null);
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

        var canCreateIncident = mine && _incidentId == 0;
        _incidentTarget.Editable = canCreateIncident;
        _incidentType.Editable = canCreateIncident;
        _createIncident.Disabled = !canCreateIncident;

        var canAct = mine && _incidentId > 0 && _courtCaseId == 0;
        _actionReason.Editable = canAct;
        _freezeSeconds.Editable = canAct;
        _requestExplanation.Disabled = !canAct;
        _freeze.Disabled = !canAct;
        _roundRemove.Disabled = !canAct;
        _escalateCourt.Disabled = !canAct;
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

    private static string ActionTypeText(string actionType)
    {
        return actionType switch
        {
            "request_explanation" => Loc.GetString("governance-ahelp-action-type-explanation"),
            "view_logs" => Loc.GetString("governance-ahelp-action-type-logs"),
            "freeze" => Loc.GetString("governance-ahelp-action-type-freeze"),
            "round_remove" => Loc.GetString("governance-ahelp-action-type-round-remove"),
            _ => actionType,
        };
    }

    private static string ActionStatusText(string status)
    {
        return status switch
        {
            "proposed" => Loc.GetString("governance-ahelp-action-status-proposed"),
            "approved" => Loc.GetString("governance-ahelp-action-status-approved"),
            "executed" => Loc.GetString("governance-ahelp-action-status-executed"),
            "rejected" => Loc.GetString("governance-ahelp-action-status-rejected"),
            "expired" => Loc.GetString("governance-ahelp-action-status-expired"),
            _ => status,
        };
    }
}
