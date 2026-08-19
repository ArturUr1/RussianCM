using System.Numerics;
using Content.Shared._RuMC14.Governance;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RuMC14.Governance;

public sealed class GovernanceAHelpPlayerWindow : DefaultWindow
{
    public event Action<GovernanceAHelpPlayerAction, string?>? ActionRequested;

    private readonly RichTextLabel _status;
    private readonly RichTextLabel _assignee;
    private readonly BoxContainer _transcript;
    private readonly LineEdit _message;
    private readonly Button _send;
    private readonly Button _resolve;
    private readonly Label _error;

    public GovernanceAHelpPlayerWindow()
    {
        Title = Loc.GetString("governance-ahelp-player-title");
        MinSize = new Vector2(700, 560);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 12,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var hero = new PanelContainer
        {
            HorizontalExpand = true,
        };
        var heroContent = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalExpand = true,
        };
        heroContent.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-player-header"),
        });
        heroContent.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-player-description"),
        });
        hero.AddChild(heroContent);
        root.AddChild(hero);

        var meta = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 16,
        };
        _status = new RichTextLabel
        {
            HorizontalExpand = true,
        };
        _assignee = new RichTextLabel
        {
            HorizontalExpand = true,
        };
        meta.AddChild(_status);
        meta.AddChild(_assignee);
        root.AddChild(meta);

        var transcriptPanel = new PanelContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        var transcriptColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        transcriptColumn.AddChild(new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-player-conversation-title"),
        });
        var transcriptScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        _transcript = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true,
        };
        transcriptScroll.AddChild(_transcript);
        transcriptColumn.AddChild(transcriptScroll);
        transcriptPanel.AddChild(transcriptColumn);
        root.AddChild(transcriptPanel);

        var tips = new RichTextLabel
        {
            Text = Loc.GetString("governance-ahelp-player-tips"),
        };
        root.AddChild(tips);

        var composer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        _message = new LineEdit
        {
            HorizontalExpand = true,
        };
        _message.OnTextEntered += args => Send(args.Text);
        _send = new Button
        {
            Text = Loc.GetString("governance-ahelp-player-send"),
        };
        _send.OnPressed += _ => Send(_message.Text);
        composer.AddChild(_message);
        composer.AddChild(_send);
        root.AddChild(composer);

        var footer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        var refresh = new Button
        {
            Text = Loc.GetString("governance-ahelp-refresh"),
            HorizontalExpand = true,
        };
        refresh.OnPressed += _ => ActionRequested?.Invoke(GovernanceAHelpPlayerAction.Refresh, null);
        _resolve = new Button
        {
            Text = Loc.GetString("governance-ahelp-player-resolve"),
            HorizontalExpand = true,
        };
        _resolve.OnPressed += _ => ActionRequested?.Invoke(GovernanceAHelpPlayerAction.Resolve, null);
        footer.AddChild(refresh);
        footer.AddChild(_resolve);
        root.AddChild(footer);

        _error = new Label
        {
            StyleClasses = { "LabelDanger" },
        };
        root.AddChild(_error);

        Contents.AddChild(root);
    }

    public void UpdateState(GovernanceAHelpPlayerEuiState state)
    {
        _error.Text = state.Error ?? string.Empty;
        _status.Text = Loc.GetString(
            "governance-ahelp-player-status",
            ("status", StatusText(state.Status)));
        _assignee.Text = string.IsNullOrWhiteSpace(state.ResponderName)
            ? Loc.GetString("governance-ahelp-player-assignee-waiting")
            : Loc.GetString(
                "governance-ahelp-player-assignee",
                ("name", FormattedMessage.EscapeText(state.ResponderName)));

        _transcript.RemoveAllChildren();
        if (state.Transcript.Length == 0)
        {
            _transcript.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("governance-ahelp-player-empty"),
            });
        }
        else
        {
            foreach (var line in state.Transcript)
            {
                var role = line.FromResponder
                    ? Loc.GetString("governance-ahelp-message-role-responder")
                    : Loc.GetString("governance-ahelp-message-role-player");
                _transcript.AddChild(new RichTextLabel
                {
                    Text = Loc.GetString(
                        "governance-ahelp-message-line",
                        ("time", line.CreatedAt.ToLocalTime().ToString("HH:mm")),
                        ("role", role),
                        ("sender", FormattedMessage.EscapeText(line.SenderName)),
                        ("body", FormattedMessage.EscapeText(line.Body))),
                });
            }
        }

        _message.Editable = state.CanSend;
        _send.Disabled = !state.CanSend;
        _resolve.Disabled = state.TicketId == null;
    }

    private void Send(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        ActionRequested?.Invoke(GovernanceAHelpPlayerAction.SendMessage, text.Trim());
        _message.Clear();
    }

    private static string StatusText(string status)
    {
        return status switch
        {
            "new" => Loc.GetString("governance-ahelp-player-status-new"),
            "open" => Loc.GetString("governance-ahelp-player-status-open"),
            "claimed" => Loc.GetString("governance-ahelp-player-status-claimed"),
            "waiting_player" => Loc.GetString("governance-ahelp-player-status-waiting"),
            "escalated_to_incident" => Loc.GetString("governance-ahelp-player-status-escalated"),
            _ => status,
        };
    }
}
