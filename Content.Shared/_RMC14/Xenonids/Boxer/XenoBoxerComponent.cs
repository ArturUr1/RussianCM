using System;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Boxer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoBoxerSystem))]
public sealed partial class XenoBoxerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? KoTarget;

    [DataField, AutoNetworkedField]
    public float KoMeter;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan LastKoHitAt;

    [DataField, AutoNetworkedField]
    public int ClearHeadCharges = XenoBoxerRules.ClearHeadMaxCharges;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextClearHeadRegenAt;

    [DataField]
    public float PunchRange = 2f;

    [DataField]
    public float JabRange = 3f;

    [DataField]
    public float UppercutRange = 1.5f;

    [DataField]
    public float PunchThrowSpeed = 10f;

    [DataField]
    public float JabSlowMultiplier = 0.5f;

    [DataField]
    public TimeSpan JabDazeDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan JabSlowDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public DamageSpecifier PunchDamage = new()
    {
        DamageDict = { ["Blunt"] = 22.5 },
    };

    [DataField]
    public DamageSpecifier UppercutDamage = new()
    {
        DamageDict = { ["Blunt"] = 15 },
    };

    [DataField]
    public float UppercutKnockBackDistance = 1f;

    [DataField]
    public float UppercutKnockBackSpeed = 8f;

    [DataField]
    public TimeSpan UppercutKnockDownDuration = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan UppercutKnockOutDuration = TimeSpan.FromSeconds(11);

    [DataField]
    public float UppercutHealPercentPerKo = 0.05f;

    [DataField]
    public float XenoVsXenoHealMultiplier = 0.35f;
}
