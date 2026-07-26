using Content.Shared.Access;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CMU14.Yautja;

[Serializable, NetSerializable]
public enum YautjaRank : byte
{
    Unblooded,
    YoungBlood,
    Blooded,
    Elite,
    Elder,
    Leader,
    Ancient,
}

public sealed record YautjaRankInfo(
    LocId LocalizedName,
    string IconState,
    ProtoId<AccessLevelPrototype>[] AccessTags,
    bool UniqueSetsAllowed,
    bool BypassesPredatorSlotCap);

public static class YautjaRankMetadata
{
    private static readonly ProtoId<AccessLevelPrototype>[] SecureAccess =
    [
        "CMUAccessYautjaSecure",
    ];

    private static readonly ProtoId<AccessLevelPrototype>[] EliteAccess =
    [
        "CMUAccessYautjaSecure",
        "CMUAccessYautjaElite",
    ];

    private static readonly ProtoId<AccessLevelPrototype>[] ElderAccess =
    [
        "CMUAccessYautjaSecure",
        "CMUAccessYautjaElite",
        "CMUAccessYautjaElder",
    ];

    private static readonly ProtoId<AccessLevelPrototype>[] LeaderAccess =
    [
        "CMUAccessYautjaSecure",
        "CMUAccessYautjaElite",
        "CMUAccessYautjaElder",
        "CMUAccessYautjaLeader",
    ];

    private static readonly ProtoId<AccessLevelPrototype>[] AncientAccess =
    [
        "CMUAccessYautjaSecure",
        "CMUAccessYautjaElite",
        "CMUAccessYautjaElder",
        "CMUAccessYautjaLeader",
        "CMUAccessYautjaAncient",
    ];

    public static readonly YautjaRank[] Order =
    [
        YautjaRank.Unblooded,
        YautjaRank.YoungBlood,
        YautjaRank.Blooded,
        YautjaRank.Elite,
        YautjaRank.Elder,
        YautjaRank.Leader,
        YautjaRank.Ancient,
    ];

    public static YautjaRankInfo For(YautjaRank rank)
    {
        return rank switch
        {
            YautjaRank.Unblooded => new YautjaRankInfo("cmu-yautja-rank-unblooded", "unblooded", SecureAccess, false, false),
            YautjaRank.YoungBlood => new YautjaRankInfo("cmu-yautja-rank-youngblood", "youngblood", SecureAccess, false, false),
            YautjaRank.Blooded => new YautjaRankInfo("cmu-yautja-rank-blooded", "blooded", SecureAccess, false, false),
            YautjaRank.Elite => new YautjaRankInfo("cmu-yautja-rank-elite", "elite", EliteAccess, true, false),
            YautjaRank.Elder => new YautjaRankInfo("cmu-yautja-rank-elder", "elder", ElderAccess, true, false),
            YautjaRank.Leader => new YautjaRankInfo("cmu-yautja-rank-leader", "leader", LeaderAccess, true, true),
            YautjaRank.Ancient => new YautjaRankInfo("cmu-yautja-rank-ancient", "ancient", AncientAccess, true, true),
            _ => new YautjaRankInfo("cmu-yautja-rank-blooded", "blooded", SecureAccess, false, false),
        };
    }
}

public static class YautjaRankResolver
{
    public static YautjaRank ResolveForHunter(YautjaCharacterProfile? profile)
    {
        if (profile == null)
            return YautjaRank.Blooded;

        if (profile.ClanRank is { } clanRank && Enum.IsDefined(clanRank))
            return clanRank;

        return FromOwnerRank(profile.OwnerRank);
    }

    public static YautjaRank FromOwnerRank(YautjaBracerOwnerRank ownerRank)
    {
        return ownerRank switch
        {
            YautjaBracerOwnerRank.Elite => YautjaRank.Elite,
            YautjaBracerOwnerRank.Elder => YautjaRank.Elder,
            YautjaBracerOwnerRank.Leader => YautjaRank.Leader,
            YautjaBracerOwnerRank.Admin => YautjaRank.Ancient,
            _ => YautjaRank.Blooded,
        };
    }

    public static YautjaBracerOwnerRank ToOwnerRank(YautjaRank rank)
    {
        return rank switch
        {
            YautjaRank.Elite => YautjaBracerOwnerRank.Elite,
            YautjaRank.Elder => YautjaBracerOwnerRank.Elder,
            YautjaRank.Leader => YautjaBracerOwnerRank.Leader,
            YautjaRank.Ancient => YautjaBracerOwnerRank.Admin,
            _ => YautjaBracerOwnerRank.Unblooded,
        };
    }
}
