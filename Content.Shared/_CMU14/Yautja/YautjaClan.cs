using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Network;

namespace Content.Shared._CMU14.Yautja;

[Flags]
public enum YautjaClanPermission : byte
{
    None = 0,
    UserView = 1 << 0,
    UserModify = 1 << 1,
    AdminView = 1 << 2,
    AdminModify = 1 << 3,
    AdminMove = 1 << 4,
    AdminManager = 1 << 5,
    UserAll = UserView | UserModify,
    AdminAncient = AdminView | AdminModify | AdminMove,
    All = AdminAncient | AdminManager,
}

[Flags]
public enum YautjaWhitelistFlags : byte
{
    None = 0,
    Yautja = 1 << 0,
    Council = 1 << 1,
    Leader = 1 << 2,
}

public sealed record YautjaClanMemberSnapshot(
    NetUserId PlayerId,
    int? ClanId,
    YautjaRank Rank,
    YautjaClanPermission Permissions,
    bool IsLegacy,
    int Honor);

public sealed record YautjaClanRankRule(
    YautjaRank Rank,
    YautjaClanPermission RequiredPermission,
    int? AbsoluteLimit,
    int? MembersPerRankLimit);

public static class YautjaClanPolicy
{
    private static readonly YautjaClanRankRule[] Rules =
    [
        new(YautjaRank.Unblooded, YautjaClanPermission.AdminModify, null, null),
        new(YautjaRank.YoungBlood, YautjaClanPermission.None, null, null),
        new(YautjaRank.Blooded, YautjaClanPermission.UserModify, null, null),
        new(YautjaRank.Elite, YautjaClanPermission.UserModify, 5, null),
        new(YautjaRank.Elder, YautjaClanPermission.UserModify, null, 12),
        new(YautjaRank.Leader, YautjaClanPermission.UserAll | YautjaClanPermission.AdminModify, 1, null),
        new(YautjaRank.Ancient, YautjaClanPermission.AdminAncient, null, null),
    ];

    private static readonly YautjaRank[] NormalAssignableRanks =
    [
        YautjaRank.Unblooded,
        YautjaRank.Blooded,
        YautjaRank.Elite,
        YautjaRank.Elder,
        YautjaRank.Leader,
    ];

    public static YautjaClanRankRule GetRule(YautjaRank rank)
    {
        return Rules.FirstOrDefault(rule => rule.Rank == rank)
               ?? Rules.Single(rule => rule.Rank == YautjaRank.Blooded);
    }

    public static IReadOnlyList<YautjaRank> GetNormalAssignableRanks()
    {
        return NormalAssignableRanks;
    }

    public static bool CanView(YautjaClanMemberSnapshot actor)
    {
        return HasPermissions(actor.Permissions, YautjaClanPermission.UserView);
    }

    public static bool CanTarget(
        YautjaClanMemberSnapshot actor,
        YautjaClanMemberSnapshot target)
    {
        if (actor.PlayerId == target.PlayerId)
            return false;

        if (target.Rank == YautjaRank.Ancient ||
            HasPermissions(target.Permissions, YautjaClanPermission.AdminAncient) ||
            HasPermissions(target.Permissions, YautjaClanPermission.AdminManager))
        {
            return false;
        }

        if (HasPermissions(actor.Permissions, YautjaClanPermission.AdminManager))
            return true;

        return actor.Rank > target.Rank;
    }

    public static bool CanModifyRank(
        YautjaClanMemberSnapshot actor,
        YautjaClanMemberSnapshot target,
        YautjaRank requestedRank,
        int clanSize,
        int currentRankOccupancy)
    {
        if (!CanTarget(actor, target) || actor.ClanId == null || target.ClanId != actor.ClanId)
            return false;

        if (!NormalAssignableRanks.Contains(requestedRank))
            return false;

        var rule = GetRule(requestedRank);
        if (!HasPermissions(actor.Permissions, rule.RequiredPermission))
            return false;

        var occupancyAfterChange = currentRankOccupancy + (target.Rank == requestedRank ? 0 : 1);
        if (rule.AbsoluteLimit is { } absoluteLimit && occupancyAfterChange > absoluteLimit)
            return false;

        if (rule.MembersPerRankLimit is { } membersPerRankLimit)
        {
            if (clanSize < 1)
                return false;

            var rankLimit = (clanSize + membersPerRankLimit - 1) / membersPerRankLimit;
            if (occupancyAfterChange > rankLimit)
                return false;
        }

        return true;
    }

    public static bool CanMove(
        YautjaClanMemberSnapshot actor,
        YautjaClanMemberSnapshot target)
    {
        return HasPermissions(actor.Permissions, YautjaClanPermission.AdminMove) &&
               CanTarget(actor, target);
    }

    public static bool CanSetAncient(
        YautjaClanMemberSnapshot actor,
        YautjaClanMemberSnapshot target)
    {
        return HasPermissions(actor.Permissions, YautjaClanPermission.AdminManager) &&
               CanTarget(actor, target);
    }

    private static bool HasPermissions(
        YautjaClanPermission actual,
        YautjaClanPermission required)
    {
        return (actual & required) == required;
    }
}
