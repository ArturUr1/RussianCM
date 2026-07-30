using System.Numerics;
using Content.Shared._CMU14.Yautja;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Content.Client._CMU14.Yautja;

public sealed class YautjaWallVisionOverlay : Overlay
{
    private readonly IEntityManager _entity;
    private readonly IPlayerManager _players;
    private readonly ContainerSystem _container;
    private readonly EntityLookupSystem _lookup;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly HashSet<Entity<MobStateComponent>> _mobs = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public YautjaWallVisionOverlay(IEntityManager entity, IPlayerManager players)
    {
        _entity = entity;
        _players = players;
        _container = entity.System<ContainerSystem>();
        _lookup = entity.System<EntityLookupSystem>();
        _sprite = entity.System<SpriteSystem>();
        _transform = entity.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_players.LocalEntity is not { } viewer || !_entity.HasComponent<YautjaComponent>(viewer))
            return;

        _mobs.Clear();
        _lookup.GetEntitiesIntersecting(args.MapId, args.WorldAABB, _mobs, LookupFlags.Uncontained);

        var handle = args.WorldHandle;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        foreach (var (target, _) in _mobs)
        {
            if (!_entity.TryGetComponent(target, out SpriteComponent? sprite) ||
                !_entity.TryGetComponent(target, out TransformComponent? xform))
            {
                continue;
            }

            var inContainer = _container.IsEntityOrParentInContainer(target, xform: xform);
            if (!YautjaWallVisionTargeting.IsEligible(
                    viewer,
                    target,
                    args.MapId,
                    xform.MapID,
                    targetIsMob: true,
                    sprite.Visible,
                    inContainer))
            {
                continue;
            }

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            _sprite.RenderSprite((target, sprite), handle, eyeRotation, rotation, position);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
