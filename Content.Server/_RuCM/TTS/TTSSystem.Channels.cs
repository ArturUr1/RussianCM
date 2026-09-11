using System.Linq;
using System.Threading.Tasks;
using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Content.Shared.Corvax.TTS;
using Content.Shared.Preferences;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Corvax.TTS;

public sealed partial class TTSSystem
{
    private PlayTTSEvent LocalPlayback(byte[] data, EntityUid source, EntityUid listener, bool whisper)
    {
        var origin = Transform(source);
        var target = Transform(listener);
        var range = whisper ? Content.Shared.Chat.SharedChatSystem.WhisperMuffledRange : Content.Shared.Chat.SharedChatSystem.VoiceRange;
        // Camera, vehicle, overwatch and other chat relays have already authorized this listener.
        var spatial = origin.MapID == target.MapID &&
                      origin.Coordinates.TryDistance(EntityManager, target.Coordinates, out var distance) && distance < range;
        return new PlayTTSEvent(data, spatial ? GetNetEntity(source) : null, isWhisper: whisper);
    }

    [Dependency] private readonly SharedLanguageSystem _language = default!;
    private readonly Dictionary<(string Speaker, string Text), Task<byte[]?>> _pendingSpeech = new();
    private readonly Dictionary<(ICommonSession Session, ulong Transmission), TimeSpan> _radioDeliveries = new();

    private void InitializeChannels()
    {
        SubscribeLocalEvent<ActorComponent, HeadsetRadioReceiveRelayEvent>(OnHeadsetTTS);
        // SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicTTS);
        SubscribeLocalEvent<RMCAnnouncementMadeEvent>(OnAnnouncementTTS);
    }

    private Task<byte[]?> GenerateSpeech(string speaker, string text)
    {
        text = Sanitize(text);
        if (!_isEnabled || string.IsNullOrWhiteSpace(text) || text.Length > 4000)
            return Task.FromResult<byte[]?>(null);
        var key = (speaker, text);
        if (_pendingSpeech.TryGetValue(key, out var pending))
            return pending;
        // Bound simultaneous work; identical radio deliveries share one synthesis request.
        if (_pendingSpeech.Count >= 16)
            return Task.FromResult<byte[]?>(null);
        var task = GenerateSpeechCore(key);
        _pendingSpeech.Add(key, task);
        return task;
    }

    private async Task<byte[]?> GenerateSpeechCore((string Speaker, string Text) key)
    {
        await Task.Yield();
        try
        {
            return await _ttsManager.ConvertTextToSpeech(key.Speaker, key.Text);
        }
        finally
        {
            _pendingSpeech.Remove(key);
        }
    }

    private void OnHeadsetTTS(EntityUid uid, ActorComponent actor, ref HeadsetRadioReceiveRelayEvent args)
    {
        _ = SendRadioTTS(uid, actor.PlayerSession, args.RelayedEvent);
    }

    // private void OnIntrinsicTTS(Entity<IntrinsicRadioReceiverComponent> ent, ref RadioReceiveEvent args)
    // {
    //     if (TryComp<ActorComponent>(ent, out var actor))
    //         _ = SendRadioTTS(ent.Owner, actor.PlayerSession, args);
    // }

    private async Task SendRadioTTS(EntityUid listener, ICommonSession session, RadioReceiveEvent args)
    {
        if (!_isEnabled || !_prototypeManager.TryIndex(args.Language, out LanguagePrototype? language) || !language.NeedsSpeech ||
            !_language.CanUnderstand(listener, args.Language) ||
            !TryComp<TTSComponent>(args.MessageSource, out var tts) || tts.VoicePrototypeId == null)
            return;

        foreach (var (key, expiry) in _radioDeliveries.ToArray())
        {
            if (expiry <= _timing.CurTime)
                _radioDeliveries.Remove(key);
        }
        if (args.TransmissionId != 0 && !_radioDeliveries.TryAdd((session, args.TransmissionId), _timing.CurTime + TimeSpan.FromSeconds(30)))
            return;

        var generation = _roundGeneration;
        var voice = tts.VoicePrototypeId;
        if (CustomTTSVoice.TryGetSpeaker(voice, out _))
            await EnsureReferenceVoiceCatalogLoaded();
        if (!TryResolveSpeaker(voice, out var speaker))
            return;
        var data = await GenerateSpeech(speaker, args.Message);
        if (data is not { Length: > 0 } || !_isEnabled || generation != _roundGeneration ||
            session.Status != SessionStatus.InGame || session.AttachedEntity != listener || TerminatingOrDeleted(listener))
            return;

        // Radio audio has no spatial source: the remote speaker need not be in the client's PVS.
        RaiseNetworkEvent(new PlayTTSEvent(data, isRadio: true), session);
    }

    private async void OnAnnouncementTTS(RMCAnnouncementMadeEvent args)
    {
        if (!_isEnabled || args.Filter == null)
            return;
        var recipients = args.Filter.Recipients
            .Where(s => s.Status == SessionStatus.InGame)
            .Select(s => (Session: s, Entity: s.AttachedEntity)).ToArray();
        if (recipients.Length == 0)
            return;

        var generation = _roundGeneration;
        var voice = TryComp<TTSComponent>(args.Source, out var tts) ? tts.VoicePrototypeId : "TURRET_FLOOR";
        if (voice == null)
            voice = HumanoidCharacterProfile.DefaultTTSVoice;
        if (CustomTTSVoice.TryGetSpeaker(voice, out _))
            await EnsureReferenceVoiceCatalogLoaded();
        if (!TryResolveSpeaker(voice, out var speaker))
            return;
        var data = await GenerateSpeech(speaker, args.RawMessage);
        if (data is not { Length: > 0 } || !_isEnabled || generation != _roundGeneration)
            return;
        foreach (var (session, entity) in recipients)
        {
            if (session.Status == SessionStatus.InGame && session.AttachedEntity == entity)
                RaiseNetworkEvent(new PlayTTSEvent(data, isRadio: true), session);
        }
    }
}
