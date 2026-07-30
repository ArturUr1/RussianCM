using Content.Shared._CMU14.Yautja;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client._CMU14.Yautja;

public sealed class YautjaWallVisionSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<YautjaComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<YautjaComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(Entity<YautjaComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlay.RemoveOverlay<YautjaWallVisionOverlay>();
        _overlay.AddOverlay(new YautjaWallVisionOverlay(EntityManager, _players));
    }

    private void OnPlayerDetached(Entity<YautjaComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay<YautjaWallVisionOverlay>();
    }
}
