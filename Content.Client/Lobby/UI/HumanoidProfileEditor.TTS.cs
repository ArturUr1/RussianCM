using Content.Client.Corvax.TTS;
using Content.Shared.Corvax.CCCVars;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private TTSTab? _ttsTab;

    private void InitializeTTSControls()
    {
        _ttsTab = new TTSTab();
        _ttsTab.OnVoiceSelected += voice =>
        {
            if (Profile == null)
                return;
            Profile = Profile.WithTTSVoice(voice);
            _ttsTab.SetSelectedVoice(voice);
            SetDirty();
        };
        _ttsTab.OnPreviewRequested += voice => _entManager.System<TTSSystem>().RequestPreviewTTS(voice);
        TabContainer.AddChild(_ttsTab);
        TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-voice-tab"));
    }

    private void UpdateTTSControls()
    {
        if (_ttsTab == null)
            return;
        _ttsTab.Visible = _cfgManager.GetCVar(CCCVars.TTSEnabled);
        if (Profile != null)
            _ttsTab.UpdateControls(Profile, Profile.Sex);
    }
}