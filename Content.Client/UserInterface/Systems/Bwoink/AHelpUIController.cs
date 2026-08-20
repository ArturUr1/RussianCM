using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Client._RMC14.Mentor;
using Content.Client.Administration.Managers;
using Content.Client.Administration.Systems;
using Content.Client.Administration.UI.Bwoink;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Administration;
using Content.Shared._RuMC14.Governance;
using Content.Shared.CCVar;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Bwoink;

[UsedImplicitly]
public sealed partial class AHelpUIController: UIController, IOnSystemChanged<BwoinkSystem>, IOnStateChanged<GameplayState>, IOnStateChanged<LobbyState>
{
    [Dependency] private IClientAdminManager _adminManager = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private StaffHelpUIController _staffHelp = default!;
    [UISystemDependency] private AudioSystem _audio = default!;

    private BwoinkSystem? _bwoinkSystem;
    public MenuButton? GameAHelpButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.AHelpButton;
    public Button? LobbyAHelpButton => (UIManager.ActiveScreen as LobbyGui)?.AHelpButton;
    public IAHelpUIHandler? UIHelper;
    private bool _discordRelayActive;
    private bool _hasUnreadAHelp;
    private bool _hasUnreadMHelp; // RMC14
    private bool _bwoinkSoundEnabled;
    private string? _aHelpSound;
    private bool _governanceResponder;

    protected override string SawmillName => "c.s.go.es.bwoink";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BwoinkDiscordRelayUpdated>(DiscordRelayUpdated);
        SubscribeNetworkEvent<BwoinkPlayerTypingUpdated>(PeopleTypingUpdated);
        SubscribeNetworkEvent<GovernanceAHelpAccessUpdated>(GovernanceAccessUpdated);
        SubscribeNetworkEvent<GovernanceAHelpOpenChannel>(GovernanceOpenChannel);

        _adminManager.AdminStatusUpdated += OnAdminStatusUpdated;
        _config.OnValueChanged(CCVars.AHelpSound, v => _aHelpSound = v, true);
        _config.OnValueChanged(CCVars.BwoinkSoundEnabled, v => _bwoinkSoundEnabled = v, true);
    }

    public void UnloadButton()
    {
        if (GameAHelpButton != null)
            GameAHelpButton.OnPressed -= AHelpButtonPressed;

        if (LobbyAHelpButton != null)
            LobbyAHelpButton.OnPressed -= AHelpButtonPressed;
    }

    public void LoadButton()
    {
        if (GameAHelpButton != null)
            GameAHelpButton.OnPressed += AHelpButtonPressed;

        if (LobbyAHelpButton != null)
            LobbyAHelpButton.OnPressed += AHelpButtonPressed;
    }

    private void OnAdminStatusUpdated()
    {
        if (UIHelper is not { IsOpen: true })
            return;
        EnsureUIHelper();
    }

    private void AHelpButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        _staffHelp.ToggleWindow();
    }

    public void OnSystemLoaded(BwoinkSystem system)
    {
        _bwoinkSystem = system;
        _bwoinkSystem.OnBwoinkTextMessageRecieved += ReceivedBwoink;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenAHelp,
                InputCmdHandler.FromDelegate(_ => _staffHelp.ToggleWindow()))
            .Register<AHelpUIController>();
    }

    public void OnSystemUnloaded(BwoinkSystem system)
    {
        CommandBinds.Unregister<AHelpUIController>();

        DebugTools.Assert(_bwoinkSystem != null);
        _bwoinkSystem!.OnBwoinkTextMessageRecieved -= ReceivedBwoink;
        _bwoinkSystem = null;
    }

    private void SetAHelpPressed(bool pressed)
    {
        if (GameAHelpButton != null)
        {
            GameAHelpButton.Pressed = pressed;
        }

        if (LobbyAHelpButton != null)
        {
            LobbyAHelpButton.Pressed = pressed;
        }

        UIManager.ClickSound();
        UnreadAHelpRead();
    }

    private void ReceivedBwoink(object? sender, SharedBwoinkSystem.BwoinkTextMessage message)
    {
        Log.Info($"@{message.UserId}: {message.Text}");
        var localPlayer = _playerManager.LocalSession;
        if (localPlayer == null)
        {
            return;
        }
        if (message.PlaySound && localPlayer.UserId != message.TrueSender)
        {
            if (_aHelpSound != null && (_bwoinkSoundEnabled || !_adminManager.IsActive()))
                _audio.PlayGlobal(new ResolvedPathSpecifier(_aHelpSound), Filter.Local(), false);
            _clyde.RequestWindowAttention();
        }

        EnsureUIHelper();

        if (!UIHelper!.IsOpen)
        {
            UnreadAHelpReceived();
        }

        UIHelper!.Receive(message);
    }

    private void DiscordRelayUpdated(BwoinkDiscordRelayUpdated args, EntitySessionEventArgs session)
    {
        _discordRelayActive = args.DiscordRelayEnabled;
        UIHelper?.DiscordRelayChanged(_discordRelayActive);
    }

    private void PeopleTypingUpdated(BwoinkPlayerTypingUpdated args, EntitySessionEventArgs session)
    {
        UIHelper?.PeopleTypingUpdated(args);
    }

    public void EnsureUIHelper()
    {
        var isAdmin = _adminManager.HasFlag(AdminFlags.Adminhelp) || _governanceResponder;

        if (UIHelper != null && UIHelper.IsAdmin == isAdmin)
            return;

        UIHelper?.Dispose();
        var ownerUserId = _playerManager.LocalUser!.Value;
        UIHelper = isAdmin ? new AdminAHelpUIHandler(ownerUserId) : new UserAHelpUIHandler(ownerUserId);
        UIHelper.DiscordRelayChanged(_discordRelayActive);

        UIHelper.SendMessageAction = (userId, textMessage, playSound, adminOnly) => _bwoinkSystem?.Send(userId, textMessage, playSound, adminOnly);
        UIHelper.InputTextChanged += (channel, text) => _bwoinkSystem?.SendInputTextUpdated(channel, text.Length > 0);
        UIHelper.OnClose += () => { SetAHelpPressed(false); };
        UIHelper.OnOpen +=  () => { SetAHelpPressed(true); };
        SetAHelpPressed(UIHelper.IsOpen);
    }

    public void Open()
    {
        var localUser = _playerManager.LocalUser;
        if (localUser == null)
        {
            return;
        }
        EnsureUIHelper();
        if (UIHelper!.IsOpen)
            return;
        UIHelper!.Open(localUser.Value, _discordRelayActive);
    }

    public void Open(NetUserId userId)
    {
        EnsureUIHelper();
        if (!UIHelper!.IsAdmin)
            return;
        UIHelper?.Open(userId, _discordRelayActive);
    }

    public void ToggleWindow()
    {
        EnsureUIHelper();
        UIHelper?.ToggleWindow();
    }

    public void PopOut()
    {
        EnsureUIHelper();
        if (UIHelper is not AdminAHelpUIHandler helper)
            return;

        if (helper.Window == null || helper.Control == null)
        {
            return;
        }

        helper.Control.Orphan();
        helper.Window.Orphan();
        helper.Window = null;
        helper.EverOpened = false;

        var monitor = _clyde.EnumerateMonitors().First();
        helper.ClydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = "Admin Help",
            Monitor = monitor,
            Width = 900,
            Height = 500
        });

        helper.ClydeWindow.RequestClosed += helper.OnRequestClosed;
        helper.ClydeWindow.DisposeOnClose = true;

        helper.WindowRoot = _uiManager.CreateWindowRoot(helper.ClydeWindow);
        helper.WindowRoot.AddChild(helper.Control);

        helper.Control.PopOut.Disabled = true;
        helper.Control.PopOut.Visible = false;
    }

    public void UnreadAHelpReceived()
    {
        _hasUnreadAHelp = true;
        UpdateHelpButtons();
    }

    public void UnreadAHelpRead()
    {
        _hasUnreadAHelp = false;
        UpdateHelpButtons();
    }

    public void UnreadMHelpReceived()
    {
        _hasUnreadMHelp = true;
        UpdateHelpButtons();
    }

    public void UnreadMHelpRead()
    {
        _hasUnreadMHelp = false;
        UpdateHelpButtons();
    }

    private void UpdateHelpButtons()
    {
        if (GameAHelpButton != null)
            GameAHelpButton.Alert = _hasUnreadAHelp || _hasUnreadMHelp;
        if (LobbyAHelpButton != null)
            LobbyAHelpButton.Alert = _hasUnreadAHelp || _hasUnreadMHelp;
    }

    public void OnStateEntered(GameplayState state)
    {
        LoadButton();
    }

    public void OnStateExited(GameplayState state)
    {
        UnloadButton();
    }

    public void OnStateEntered(LobbyState state)
    {
        LoadButton();
    }

    public void OnStateExited(LobbyState state)
    {
        UnloadButton();
    }

    private void GovernanceAccessUpdated(GovernanceAHelpAccessUpdated args, EntitySessionEventArgs session)
    {
        _governanceResponder = args.IsResponder;
    }

    private void GovernanceOpenChannel(GovernanceAHelpOpenChannel args, EntitySessionEventArgs session)
    {
        _staffHelp.ToggleWindow();
    }
}
