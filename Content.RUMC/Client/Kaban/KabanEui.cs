using System;
using System.Linq;
using Content.Client.Eui;
using Content.Client.Lobby.UI;
using Content.Client.Options.UI;
using Content.Client.Stylesheets;
using Content.Shared._RuCM.Kaban;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Client._RuCM.Kaban;

// CMU14: private server-authoritative account UI.
[UsedImplicitly]
public sealed class KabanEui : BaseEui
{
    private readonly KabanWindow _window;
    public KabanEui()
    {
        _window = new(request => SendMessage(request));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }
    public override void Opened() => _window.OpenCentered();
    public override void Closed() => _window.Dispose();
    public override void HandleState(EuiStateBase state)
    {
        if (state is KabanState view)
            _window.Update(view);
    }
}

public sealed partial class KabanEntrySystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    public override void Initialize()
    {
        base.Initialize();
        _ui.OnScreenChanged += ScreenChanged;
        _ui.WindowRoot.OnChildAdded += WindowAdded;
        if (_ui.ActiveScreen is LobbyGui lobby)
            Attach(lobby.ObserveButton.Parent!);
    }
    public override void Shutdown()
    {
        _ui.OnScreenChanged -= ScreenChanged;
        _ui.WindowRoot.OnChildAdded -= WindowAdded;
        base.Shutdown();
    }
    private void ScreenChanged((UIScreen? Old, UIScreen? New) screens)
    {
        if (screens.New is LobbyGui lobby)
            Attach(lobby.ObserveButton.Parent!);
    }
    private void WindowAdded(Control control)
    {
        if (control is EscapeMenu escape)
            Attach(escape.RulesButton.Parent!);
    }
    private void Attach(Control parent)
    {
        if (parent.Children.Any(c => c.Name == "RuCMKabanEntry"))
            return;
        var button = new Button { Name = "RuCMKabanEntry", Text = Loc.GetString("cmu-kaban-title"), HorizontalExpand = true, MinHeight = 34 };
        button.AddStyleClass(StyleNano.StyleClassCrtButton);
        button.OnPressed += _ => RaiseNetworkEvent(new KabanOpenEvent());
        parent.AddChild(button);
    }
}
