using Content.Shared.Chat;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Corvax.TTS;

// RuCM TTS
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;

    private const int MaxMessageChars = 200;

    private bool _isEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(
            CCCVars.TTSEnabled,
            OnTtsEnabledChanged,
            true);

        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    public override void Shutdown()
    {
        _cfg.UnsubValueChanged(
            CCCVars.TTSEnabled,
            OnTtsEnabledChanged);

        base.Shutdown();
    }

    private void OnTtsEnabledChanged(bool enabled)
    {
        _isEnabled = enabled;
    }

    private async void OnEntitySpoke(
        EntityUid uid,
        TTSComponent component,
        EntitySpokeEvent args)
    {
        if (!_isEnabled)
            return;

        // Радио подключим отдельно.
        if (args.Channel != null)
            return;

        // Шёпот тоже подключим отдельно.
        if (args.ObfuscatedMessage != null)
            return;

        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        if (args.Message.Length > MaxMessageChars)
            return;

        var voiceId = component.VoicePrototypeId;

        if (string.IsNullOrWhiteSpace(voiceId))
            return;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(
                voiceId,
                out var voice))
        {
            Logger.Warning(
                $"TTS voice prototype '{voiceId}' was not found.");

            return;
        }

        var text = Sanitize(args.Message);

        if (string.IsNullOrWhiteSpace(text))
            return;

        var soundData = await _ttsManager.ConvertTextToSpeech(
            voice.Speaker,
            text);

        if (soundData == null || soundData.Length == 0)
            return;

        var ev = new PlayTTSEvent(
            soundData,
            GetNetEntity(uid));

        foreach (var session in Filter.Pvs(uid).Recipients)
        {
            RaiseNetworkEvent(ev, session);
        }
    }

    private static string Sanitize(string text)
    {
        return FormattedMessage
            .RemoveMarkupPermissive(text)
            .Trim();
    }
}
