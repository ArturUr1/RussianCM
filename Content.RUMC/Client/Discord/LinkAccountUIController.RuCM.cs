using Content.Client.Message;
using Content.Shared._RMC14.CCVar;
using Robust.Shared.Localization;

namespace Content.Client._RMC14.LinkAccount;

public sealed partial class LinkAccountUIController
{
    private void InitializeRuCMDiscordLink()
        => _config.OnValueChanged(RMCCVars.RMCDiscordAccountLinkingMessageLink, _ => RefreshRuCMDiscordLinkWindow());

    private void ConfigureRuCMDiscordLinkWindow()
    {
        if (_window == null) return;
        _window.MinWidth = 420;
        _window.SetWidth = 560;
        _window.MaxWidth = 640;
        _window.LinkButton.OnPressed += _ =>
        {
            var url = _config.GetCVar(RMCCVars.RMCDiscordAccountLinkingMessageLink);
            if (!string.IsNullOrWhiteSpace(url)) _uriOpener.OpenUri(url);
        };
        RefreshRuCMDiscordLinkWindow();
    }

    private void RefreshRuCMDiscordLinkWindow()
    {
        if (_window == null) return;
        var ready = !string.IsNullOrWhiteSpace(_config.GetCVar(RMCCVars.RMCDiscordAccountLinkingMessageLink));
        _window.LinkButton.Visible = true;
        _window.LinkButton.Disabled = !ready;
        _window.LinkButton.ToolTip = ready ? null : Loc.GetString("rucm-discord-link-waiting-channel");
        var text = Loc.GetString("rucm-discord-link-instructions");
        if (_linkAccount.Linked)
            text = Loc.GetString("rmc-ui-link-discord-account-already-linked") + "\n\n" + text;
        if (!ready) text += "\n\n" + Loc.GetString("rucm-discord-link-waiting-channel");
        _window.Label.SetMarkupPermissive(text);
    }

    private void CloseRuCMDiscordLinkWindow()
    {
        _code = default;
        _disableUntil = default;
        _window?.Close();
        _window = null;
    }
}
