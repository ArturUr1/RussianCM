using Content.Shared.Corvax.TTS;
using Content.Shared.Corvax.CCCVars;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;
using Content.Shared.Chat;
using System.Linq;
using Robust.Shared.Audio.Components;
using Content.Shared.GameTicking;

namespace Content.Client.Corvax.TTS;

// RuCM TTS
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly MemoryContentRoot ContentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private static bool _contentRootAdded;
    private int _fileIndex;
    private readonly Dictionary<EntityUid, (AudioStream Stream, bool Whisper)> _playing = new();

    public override void Initialize()
    {
        base.Initialize();

        if (!_contentRootAdded)
        {
            _contentRootAdded = true;
            _res.AddRoot(Prefix, ContentRoot);
        }

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
        SubscribeNetworkEvent<AddReferenceVoiceResponse>(OnReferenceVoiceResult);
        SubscribeNetworkEvent<ReferenceVoiceCatalogResponse>(OnReferenceVoiceCatalog);
        SubscribeNetworkEvent<ReferenceVoiceAccessResponse>(OnReferenceVoiceAccess);
        SubscribeNetworkEvent<DeleteReferenceVoiceResponse>(OnReferenceVoiceDeleteResult);
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => StopTTS());
        _cfg.OnValueChanged(CCCVars.TTSVolume, OnVolumeChanged);
    }

    public override void Shutdown()
    {
        _cfg.UnsubValueChanged(CCCVars.TTSVolume, OnVolumeChanged);
        StopTTS();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (uid, sound) in _playing.ToArray())
        {
            if (TryComp<AudioComponent>(uid, out var audio) && (!audio.Started || audio.Playing))
                continue;
            sound.Stream.Dispose();
            _playing.Remove(uid);
        }
    }

    private void StopTTS()
    {
        foreach (var (uid, sound) in _playing)
        {
            _audio.Stop(uid);
            sound.Stream.Dispose();
        }
        _playing.Clear();
    }

    private void OnVolumeChanged(float volume)
    {
        if (!float.IsFinite(volume) || volume <= 0)
        {
            StopTTS();
            return;
        }
        foreach (var (uid, sound) in _playing)
            _audio.SetVolume(uid, SharedAudioSystem.GainToVolume(Math.Clamp(volume, 0f, 1f)) - (sound.Whisper ? 6f : 0f));
    }

    public void RequestPreviewTTS(string voiceId)
    {
        RaiseNetworkEvent(new RequestPreviewTTSEvent(voiceId));
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var volume = _cfg.GetCVar(CCCVars.TTSVolume);
        if (!float.IsFinite(volume) || volume <= 0f || ev.Data.Length is < 12 or > 8388608 || _playing.Count >= 32)
            return;

        volume = Math.Clamp(volume, 0f, 1f);
        var filePath = new ResPath($"{_fileIndex++}.wav");

        ContentRoot.AddOrUpdateFile(filePath, ev.Data);
        AudioStream? stream = null;

        try
        {
            var audioResource = new AudioResource();
            audioResource.Load(
                IoCManager.Instance!,
                Prefix / filePath);
            stream = audioResource.AudioStream;

            var soundSpecifier =
                new ResolvedPathSpecifier(Prefix / filePath);

            var audioParams = AudioParams.Default
                .WithVolume(SharedAudioSystem.GainToVolume(volume) - (ev.IsWhisper ? 6f : 0f))
                .WithMaxDistance(ev.IsWhisper ? SharedChatSystem.WhisperMuffledRange : SharedChatSystem.VoiceRange);

            if (ev.SourceUid != null && !ev.IsRadio)
            {
                if (!TryGetEntity(ev.SourceUid.Value, out _))
                    return;

                var source = GetEntity(ev.SourceUid.Value);

                var playback = _audio.PlayEntity(
                    audioResource.AudioStream,
                    source,
                    soundSpecifier,
                    audioParams);

                if (playback is { } played)
                {
                    _playing.Add(played.Entity, (stream, ev.IsWhisper));
                    stream = null;
                }

                return;
            }

            var globalPlayback = _audio.PlayGlobal(
                audioResource.AudioStream,
                soundSpecifier,
                audioParams);
            if (globalPlayback is { } global)
            {
                _playing.Add(global.Entity, (stream, ev.IsWhisper));
                stream = null;
            }
        }
        catch (Exception e)
        {
            Logger.Warning($"Could not play TTS audio: {e.Message}");
        }
        finally
        {
            stream?.Dispose();
            ContentRoot.RemoveFile(filePath);
        }
    }
}
