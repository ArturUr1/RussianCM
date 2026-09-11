using Content.Shared.Corvax.TTS;

namespace Content.Client.Corvax.TTS;

public sealed partial class TTSSystem
{
    public event Action<AddReferenceVoiceResponse>? ReferenceVoiceResultReceived;
    public event Action? ReferenceVoiceCatalogUpdated;
    public event Action? ReferenceVoiceAccessUpdated;
    public event Action<DeleteReferenceVoiceResponse>? ReferenceVoiceDeleteResultReceived;
    public IReadOnlyList<string> ReferenceVoices { get; private set; } = Array.Empty<string>();
    public bool CanCreateReferenceVoice { get; private set; }

    public void AddReferenceVoice(string speakerName, byte[] audio)
    {
        RaiseNetworkEvent(new AddReferenceVoiceRequest(speakerName, audio));
    }

    public void RequestReferenceVoiceCatalog()
    {
        RaiseNetworkEvent(new ReferenceVoiceCatalogRequest());
    }

    public void DeleteReferenceVoice(string speakerName)
    {
        RaiseNetworkEvent(new DeleteReferenceVoiceRequest(speakerName));
    }

    private void OnReferenceVoiceResult(AddReferenceVoiceResponse response)
    {
        ReferenceVoiceResultReceived?.Invoke(response);
    }

    private void OnReferenceVoiceCatalog(ReferenceVoiceCatalogResponse response)
    {
        ReferenceVoices = response.SpeakerNames;
        ReferenceVoiceCatalogUpdated?.Invoke();
    }

    private void OnReferenceVoiceAccess(ReferenceVoiceAccessResponse response)
    {
        CanCreateReferenceVoice = response.CanCreate;
        ReferenceVoiceAccessUpdated?.Invoke();
    }

    private void OnReferenceVoiceDeleteResult(DeleteReferenceVoiceResponse response)
    {
        ReferenceVoiceDeleteResultReceived?.Invoke(response);
    }

}
