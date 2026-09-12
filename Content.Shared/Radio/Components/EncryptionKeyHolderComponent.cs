using Content.Shared.Chat;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
///     This component is by entities that can contain encryption keys
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EncryptionKeyHolderComponent : Component
{
    [DataField]
    public bool KeysUnlocked = true;

    [DataField]
    public ProtoId<ToolQualityPrototype> KeysExtractionMethod = "Screwing";

    [DataField]
    public int KeySlots = 2;

    [DataField]
    public SoundSpecifier KeyExtractionSound = new SoundPathSpecifier("/Audio/Items/pistol_magout.ogg");

    [DataField]
    public SoundSpecifier KeyInsertionSound = new SoundPathSpecifier("/Audio/Items/pistol_magin.ogg");

    [ViewVariables]
    public Container KeyContainer = default!;
    public const string KeyContainerName = "key_slots";

    [ViewVariables, AutoNetworkedField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();

    [DataField, AutoNetworkedField]
    public string? DefaultChannel;

    [AutoNetworkedField]
    public HashSet<ProtoId<RadioChannelPrototype>> ReadOnlyChannels = new();
}
