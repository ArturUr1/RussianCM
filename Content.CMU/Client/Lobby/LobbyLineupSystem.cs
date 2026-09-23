using Content.Shared.CMU14.Lobby;

namespace Content.Client.CMU14.Lobby;

public sealed class LobbyLineupSystem : EntitySystem
{
    public event Action<LobbyLineupEmoteEvent>? EmoteReceived;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<LobbyLineupEmoteEvent>(OnEmote);
    }

    private void OnEmote(LobbyLineupEmoteEvent ev)
    {
        EmoteReceived?.Invoke(ev);
    }

    public void RequestEmote(LobbyLineupEmote emote)
    {
        RaiseNetworkEvent(new LobbyLineupEmoteRequest(emote));
    }
}
