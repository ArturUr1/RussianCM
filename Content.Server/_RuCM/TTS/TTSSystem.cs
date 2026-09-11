using Content.Shared.Chat;
using System.Linq;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Preferences;
using Robust.Shared.Timing;
using Content.Server.Chat.Systems;
using Content.Shared.GameTicking;

namespace Content.Server.Corvax.TTS;

// RuCM TTS
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    private uint _roundGeneration;

    private const int MaxMessageChars = 200;

    private bool _isEnabled;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ICommonSession, PreviewLimit> _previewLimits = new();

    private sealed class PreviewLimit
    {
        public TimeSpan ResetAt;
        public int Count;
        public bool Pending;
    }

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(
            CCCVars.TTSEnabled,
            OnTtsEnabledChanged,
            true);

        SubscribeLocalEvent<EntitySpokeEvent>(OnSpoke);
        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
        InitializeReferenceVoices();
        InitializeChannels();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ =>
        {
            _roundGeneration++;
            _radioDeliveries.Clear();
            foreach (var user in _referenceVoiceCooldowns.Keys.ToArray())
            {
                if (_referenceVoiceCooldowns[user] <= _timing.CurTime)
                    _referenceVoiceCooldowns.Remove(user);
            }
        });
    }

    public override void Shutdown()
    {
        _roundGeneration++;
        _isEnabled = false;
        ShutdownReferenceVoices();
        _cfg.UnsubValueChanged(
            CCCVars.TTSEnabled,
            OnTtsEnabledChanged);

        base.Shutdown();
    }

    private void OnTtsEnabledChanged(bool enabled)
    {
        _isEnabled = enabled;
        if (!enabled)
            _roundGeneration++;
    }

    private void OnSpoke(EntitySpokeEvent args)
    {
        if (TryComp<TTSComponent>(args.VoiceSource ?? args.Source, out var tts))
            OnEntitySpoke(args.Source, tts, args);
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

        if (string.IsNullOrWhiteSpace(args.Message))
            return;

        if (args.Message.Length > MaxMessageChars)
            return;

        var voiceId = component.VoicePrototypeId;

        if (string.IsNullOrWhiteSpace(voiceId))
            return;

        var voiceEvent = new TransformSpeakerVoiceEvent(args.VoiceSource ?? uid, voiceId);
        RaiseLocalEvent(args.VoiceSource ?? uid, voiceEvent);
        voiceId = voiceEvent.VoiceId;

        if (CustomTTSVoice.TryGetSpeaker(voiceId, out _))
            await EnsureReferenceVoiceCatalogLoaded();

        if (!TryResolveSpeaker(voiceId, out var speaker))
        {
            Logger.Warning(
                $"TTS voice prototype '{voiceId}' was not found.");

            return;
        }

        var text = Sanitize(args.Message);

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (TerminatingOrDeleted(uid))
            return;
        var whisper = args.ObfuscatedMessage != null;
        var recipients = _chat.GetLocalTTSRecipients(uid, args.Language, args.TransmitRange, whisper, ignoreXenos: args.IgnoreXenos);
        var muffledRecipients = whisper
            ? _chat.GetLocalTTSRecipients(uid, args.Language, args.TransmitRange, true, true, args.IgnoreXenos)
            : new Dictionary<ICommonSession, EntityUid>();
        if (recipients.Count == 0 && muffledRecipients.Count == 0)
            return;

        var roundGeneration = _roundGeneration;
        var soundData = recipients.Count > 0 ? await GenerateSpeech(speaker, text) : null;
        var muffledData = muffledRecipients.Count > 0 ? await GenerateSpeech(speaker, args.ObfuscatedMessage!) : null;

        if (!_isEnabled ||
            roundGeneration != _roundGeneration || !Exists(uid) || TerminatingOrDeleted(uid))
            return;

        var currentRecipients = _chat.GetLocalTTSRecipients(uid, args.Language, args.TransmitRange, whisper, ignoreXenos: args.IgnoreXenos);
        foreach (var (session, listener) in recipients)
        {
            if (soundData is { Length: > 0 } && currentRecipients.TryGetValue(session, out var currentListener) && currentListener == listener)
                RaiseNetworkEvent(LocalPlayback(soundData, uid, listener, whisper), session);
        }
        if (muffledData is { Length: > 0 })
        {
            var currentMuffled = _chat.GetLocalTTSRecipients(uid, args.Language, args.TransmitRange, true, true, args.IgnoreXenos);
            foreach (var (session, listener) in muffledRecipients)
            {
                if (currentMuffled.TryGetValue(session, out var currentListener) && currentListener == listener)
                    RaiseNetworkEvent(LocalPlayback(muffledData, uid, listener, true), session);
            }
        }
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled || string.IsNullOrWhiteSpace(ev.VoiceId) || ev.VoiceId.Length > 128)
            return;

        var limit = _previewLimits.GetValue(args.SenderSession, _ => new PreviewLimit());
        if (limit.Pending)
            return;
        if (_timing.CurTime >= limit.ResetAt)
        {
            limit.ResetAt = _timing.CurTime + TimeSpan.FromSeconds(Math.Max(0.1f, _cfg.GetCVar(CCCVars.TTSRateLimitPeriod)));
            limit.Count = 0;
        }
        if (limit.Count >= _cfg.GetCVar(CCCVars.TTSRateLimitCount))
            return;

        limit.Count++;
        limit.Pending = true;
        try
        {
            if (CustomTTSVoice.TryGetSpeaker(ev.VoiceId, out _))
                await EnsureReferenceVoiceCatalogLoaded();
            else if (!_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var voice) ||
                     !HumanoidCharacterProfile.IsSelectableTTSVoice(voice))
                return;
            if (!TryResolveSpeaker(ev.VoiceId, out var speaker))
                return;
            var data = await GenerateSpeech(speaker,
                Loc.GetString("tts-preview-text"));
            if (_isEnabled && data is { Length: > 0 } &&
                args.SenderSession.Status == Robust.Shared.Enums.SessionStatus.InGame)
                RaiseNetworkEvent(new PlayTTSEvent(data), args.SenderSession);
        }
        finally
        {
            limit.Pending = false;
        }
    }

    private static string Sanitize(string text)
    {
        var clean = FormattedMessage.RemoveMarkupPermissive(text);
        // Keep all languages, but strip chat formatting sentinels and SSML delimiters.
        return new string(clean.Where(c => !char.IsControl(c) &&
            char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.PrivateUse &&
            c is not '<' and not '>').ToArray()).Trim();
    }
}
