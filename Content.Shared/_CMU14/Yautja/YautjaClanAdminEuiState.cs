using System;
using System.Collections.Generic;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._CMU14.Yautja;

[Serializable, NetSerializable]
public sealed class YautjaClanAdminEuiState : EuiStateBase
{
    public YautjaClanAdminEuiState(
        List<YautjaClanAdminClanState> clans,
        string inspectedPlayer,
        string inspectedSummary,
        string statusMessage,
        long clanMutationVersion,
        int? lastMutatedClanId,
        YautjaClanAdminMutationKind lastMutationKind)
    {
        Clans = clans;
        InspectedPlayer = inspectedPlayer;
        InspectedSummary = inspectedSummary;
        StatusMessage = statusMessage;
        ClanMutationVersion = clanMutationVersion;
        LastMutatedClanId = lastMutatedClanId;
        LastMutationKind = lastMutationKind;
    }

    public List<YautjaClanAdminClanState> Clans { get; }
    public string InspectedPlayer { get; }
    public string InspectedSummary { get; }
    public string StatusMessage { get; }
    public long ClanMutationVersion { get; }
    public int? LastMutatedClanId { get; }
    public YautjaClanAdminMutationKind LastMutationKind { get; }
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminClanState
{
    public YautjaClanAdminClanState(
        int id,
        string name,
        string description,
        int honor,
        string color,
        int members)
    {
        Id = id;
        Name = name;
        Description = description;
        Honor = honor;
        Color = color;
        Members = members;
    }

    public int Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int Honor { get; }
    public string Color { get; }
    public int Members { get; }
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminRefreshMessage : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class YautjaClanAdminCreateClanMessage(
    string name,
    string description,
    string color) : EuiMessageBase
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Color { get; } = color;
}

[Serializable, NetSerializable]
public enum YautjaClanAdminMutationKind : byte
{
    None,
    Created,
    Updated,
    Deleted,
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminUpdateClanMessage(
    int clanId,
    string name,
    string description,
    string color) : EuiMessageBase
{
    public int ClanId { get; } = clanId;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Color { get; } = color;
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminDeleteClanMessage(int clanId) : EuiMessageBase
{
    public int ClanId { get; } = clanId;
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminSetMembershipMessage(
    string player,
    string clanId,
    YautjaRank rank) : EuiMessageBase
{
    public string Player { get; } = player;
    public string ClanId { get; } = clanId;
    public YautjaRank Rank { get; } = rank;
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminSetRankMessage(string player, YautjaRank rank) : EuiMessageBase
{
    public string Player { get; } = player;
    public YautjaRank Rank { get; } = rank;
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminSetWhitelistMessage(string player, YautjaWhitelistFlags flags) : EuiMessageBase
{
    public string Player { get; } = player;
    public YautjaWhitelistFlags Flags { get; } = flags;
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminInspectMessage(string player) : EuiMessageBase
{
    public string Player { get; } = player;
}
