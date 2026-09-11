using Content.Shared.Corvax.TTS;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.Corvax.TTS;

// RuCM TTS
public sealed class TTSSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private static readonly MemoryContentRoot ContentRoot = new();
    private static readonly ResPath Prefix = ResPath.Root / "TTS";

    private static bool _contentRootAdded;
    private int _fileIndex;

    public override void Initialize()
    {
        base.Initialize();

        if (!_contentRootAdded)
        {
            _contentRootAdded = true;
            _res.AddRoot(Prefix, ContentRoot);
        }

        SubscribeNetworkEvent<PlayTTSEvent>(OnPlayTTS);
    }

    private void OnPlayTTS(PlayTTSEvent ev)
    {
        var filePath = new ResPath($"{_fileIndex++}.wav");

        ContentRoot.AddOrUpdateFile(filePath, ev.Data);

        try
        {
            var audioResource = new AudioResource();
            audioResource.Load(
                IoCManager.Instance!,
                Prefix / filePath);

            var soundSpecifier =
                new ResolvedPathSpecifier(Prefix / filePath);

            var audioParams = AudioParams.Default;

            if (ev.SourceUid != null)
            {
                if (!TryGetEntity(ev.SourceUid.Value, out _))
                    return;

                var source = GetEntity(ev.SourceUid.Value);

                _audio.PlayEntity(
                    audioResource.AudioStream,
                    source,
                    soundSpecifier,
                    audioParams);

                return;
            }

            _audio.PlayGlobal(
                audioResource.AudioStream,
                soundSpecifier,
                audioParams);
        }
        finally
        {
            ContentRoot.RemoveFile(filePath);
        }
    }
}
