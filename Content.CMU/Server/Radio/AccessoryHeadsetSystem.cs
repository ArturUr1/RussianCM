using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.CMU14.Radio;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Containers;
using Content.Server.CMU14.Marines.Roles.Ranks;
using Content.Shared.CMU14.Marines.Roles.Ranks;
using Content.Server._RMC14.Marines.Roles.Ranks;

namespace Content.Server.CMU14.Radio;

public sealed partial class AccessoryHeadsetSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private RankChangerSystem _rankChanger = default!;
    [Dependency] private RankSystem _rank = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AccessoryHeadsetComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<AccessoryHeadsetComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, ClothingGotEquippedEvent>(OnHolderEquipped);
        SubscribeLocalEvent<UniformAccessoryHolderComponent, ClothingGotUnequippedEvent>(OnHolderUnequipped);
        SubscribeLocalEvent<AccessoryRadioWearerComponent, GetDefaultRadioChannelEvent>(OnGetDefaultRadioChannel);
    }

    private void OnGetDefaultRadioChannel(EntityUid uid, AccessoryRadioWearerComponent component, GetDefaultRadioChannelEvent args)
    {
        args.Channel ??= component.DefaultChannel;
    }

    private void OnInserted(Entity<AccessoryHeadsetComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var uniform = args.Container.Owner;
        if (!TryComp<UniformAccessoryHolderComponent>(uniform, out var holder) || args.Container.ID != holder.ContainerId)
            return;

        if (TryGetWearer(uniform, out var wearer))
            GrantRadio(ent, wearer);
    }

    private void OnRemoved(Entity<AccessoryHeadsetComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RevokeRadio(ent);
    }

    private void OnHolderEquipped(Entity<UniformAccessoryHolderComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        foreach (var accessory in container.ContainedEntities)
        {
            if (TryComp<AccessoryHeadsetComponent>(accessory, out var headset))
                GrantRadio((accessory, headset), args.Wearer);

            if (TryComp<RankChangerComponent>(accessory, out var changer))
                _rankChanger.ApplyRank(args.Wearer, changer);
        }
    }

    private void OnHolderUnequipped(Entity<UniformAccessoryHolderComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        foreach (var accessory in container.ContainedEntities)
        {
            if (TryComp<AccessoryHeadsetComponent>(accessory, out var headset))
                RevokeRadio((accessory, headset));

            if (TryComp<RankChangerComponent>(accessory, out var changer))
                _rankChanger.RevertRank(args.Wearer, changer);
        }
    }

    private bool TryGetWearer(EntityUid uniform, out EntityUid wearer)
    {
        wearer = default;
        if (!_container.TryGetContainingContainer((uniform, null, null), out var container))
            return false;
        if (!_inventory.TryGetContainingSlot((uniform, null, null), out var slot))
            return false;
        if ((slot.SlotFlags & (SlotFlags.INNERCLOTHING | SlotFlags.OUTERCLOTHING)) == SlotFlags.NONE)
            return false;

        wearer = container.Owner;
        return wearer.IsValid();
    }

    private void GrantRadio(Entity<AccessoryHeadsetComponent> ent, EntityUid target)
    {
        if (ent.Comp.RadioGrantedTo == target)
            return;
        if (ent.Comp.RadioGrantedTo != null)
            RevokeRadio(ent);

        ent.Comp.RadioGrantedTo = target;

        var activeRadio = EnsureComp<ActiveRadioComponent>(target);
        foreach (var channel in ent.Comp.Channels)
        {
            if (activeRadio.Channels.Add(channel))
                ent.Comp.ActiveAddedChannels.Add(channel);
        }

        EnsureComp<IntrinsicRadioReceiverComponent>(target);

        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(target);
        foreach (var channel in ent.Comp.Channels)
        {
            if (transmitter.Channels.Add(channel))
                ent.Comp.TransmitterAddedChannels.Add(channel);
        }
        Dirty(target, transmitter);

        var defaultChannel = ent.Comp.DefaultChannel?.Id;
        if (defaultChannel == null)
        {
            foreach (var ch in ent.Comp.Channels)
            {
                defaultChannel = ch;
                break;
            }
        }

        if (defaultChannel != null)
        {
            var wearerComp = EnsureComp<AccessoryRadioWearerComponent>(target);
            wearerComp.DefaultChannel ??= defaultChannel;
            Dirty(target, wearerComp);
        }

        Dirty(ent);
    }

    private void RevokeRadio(Entity<AccessoryHeadsetComponent> ent)
    {
        var target = ent.Comp.RadioGrantedTo;
        if (target == null || !Exists(target.Value))
        {
            ClearTracking(ent);
            return;
        }

        if (TryComp<ActiveRadioComponent>(target.Value, out var activeRadio))
        {
            foreach (var channel in ent.Comp.ActiveAddedChannels)
                activeRadio.Channels.Remove(channel);

            if (activeRadio.Channels.Count == 0)
                RemCompDeferred<ActiveRadioComponent>(target.Value);
        }

        if (TryComp<IntrinsicRadioTransmitterComponent>(target.Value, out var transmitter))
        {
            foreach (var channel in ent.Comp.TransmitterAddedChannels)
                transmitter.Channels.Remove(channel);

            if (transmitter.Channels.Count == 0)
                RemCompDeferred<IntrinsicRadioTransmitterComponent>(target.Value);
            else
                Dirty(target.Value, transmitter);
        }

        if (activeRadio?.Channels.Count == 0)
            RemCompDeferred<IntrinsicRadioReceiverComponent>(target.Value);

        RemCompDeferred<AccessoryRadioWearerComponent>(target.Value);
        ClearTracking(ent);
    }

    private void ClearTracking(Entity<AccessoryHeadsetComponent> ent)
    {
        ent.Comp.RadioGrantedTo = null;
        ent.Comp.ActiveAddedChannels.Clear();
        ent.Comp.TransmitterAddedChannels.Clear();
        Dirty(ent);
    }
}
