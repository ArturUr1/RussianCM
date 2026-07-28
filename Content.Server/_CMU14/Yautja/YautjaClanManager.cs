using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._CMU14.Yautja;
using Robust.Shared.Network;

namespace Content.Server._CMU14.Yautja;

public sealed partial class YautjaClanManager
{
    [Dependency] private IServerDbManager _db = default!;

    private readonly Dictionary<NetUserId, YautjaClanResolution> _cache = new();
    private readonly YautjaClanCacheVersions _cacheVersions = new();

    public async Task<YautjaClanResolution> Resolve(NetUserId userId, bool youngbloodRole = false)
    {
        if (youngbloodRole)
            return ResolveSpecial(YautjaWhitelistFlags.None, true);

        if (_cache.TryGetValue(userId, out var cached))
            return cached;

        var requestVersion = _cacheVersions.Capture(userId);
        var whitelistFlags = (YautjaWhitelistFlags) await _db.GetYautjaWhitelistFlagsAsync(userId.UserId);
        var member = await _db.GetYautjaClanMemberAsync(userId.UserId);
        YautjaClanResolution resolution;

        if (whitelistFlags.HasFlag(YautjaWhitelistFlags.Leader) || whitelistFlags.HasFlag(YautjaWhitelistFlags.Council))
        {
            resolution = ResolveSpecial(whitelistFlags, false);
        }
        else if (member == null)
        {
            var legacyRank = await _db.GetYautjaRank(userId.UserId);
            resolution = new(
                SanitizeStoredRank(legacyRank is { } rank ? (int) rank : null),
                null,
                YautjaClanPermission.UserView,
                legacyRank != null,
                0,
                whitelistFlags);
        }
        else
        {
            var rank = SanitizeStoredRank(member.Rank);
            var permissions = !member.IsLegacy && TryReadPermissions(member.Permissions, out var storedPermissions)
                ? storedPermissions
                : PermissionsForRank(rank);
            resolution = new(rank, member.ClanId, permissions, member.IsLegacy, member.Honor, whitelistFlags);
        }

        if (_cacheVersions.IsCurrent(userId, requestVersion))
            _cache[userId] = resolution;

        return resolution;
    }

    public async Task<YautjaClanView> GetView(NetUserId userId)
    {
        var viewer = await Resolve(userId);
        if (viewer.ClanId is not { } clanId)
        {
            return new(viewer, null, "", "", 0, "", Array.Empty<YautjaClanMemberSnapshot>());
        }

        var clan = await _db.GetYautjaClanAsync(clanId);
        var members = await _db.GetYautjaClanMembersAsync(clanId);
        return new(
            viewer,
            clanId,
            clan?.Name ?? "",
            clan?.Description ?? "",
            clan?.Honor ?? 0,
            clan?.Color ?? "",
            members.Select(member => new YautjaClanMemberSnapshot(
                new NetUserId(member.PlayerUserId),
                member.ClanId,
                SanitizeStoredRank(member.Rank),
                !member.IsLegacy && TryReadPermissions(member.Permissions, out var storedPermissions)
                    ? storedPermissions
                    : PermissionsForRank(SanitizeStoredRank(member.Rank)),
                member.IsLegacy,
                member.Honor)).ToArray());
    }

    public async Task<YautjaClanMutationResult> SetRank(
        NetUserId actorId,
        NetUserId targetId,
        YautjaRank requestedRank)
    {
        var actor = await Resolve(actorId);
        var target = await Resolve(targetId);
        var actorSnapshot = ToSnapshot(actorId, actor);
        var targetSnapshot = ToSnapshot(targetId, target);

        if (actor.ClanId is not { } clanId || target.ClanId != clanId)
            return YautjaClanMutationResult.Denied("Both hunters must belong to the same clan.");

        var members = await _db.GetYautjaClanMembersAsync(clanId);
        var clanSize = members.Count;
        var occupancy = members.Count(member => SanitizeStoredRank(member.Rank) == requestedRank);
        if (!YautjaClanPolicy.CanModifyRank(actorSnapshot, targetSnapshot, requestedRank, clanSize, occupancy))
            return YautjaClanMutationResult.Denied("You do not have permission to assign that rank.");

        if (!await _db.UpsertYautjaClanMemberAsync(new YautjaClanMemberRecord(
            targetId.UserId,
            clanId,
            (int) requestedRank,
            (int) PermissionsForRank(requestedRank),
            target.Honor,
            false)))
        {
            return YautjaClanMutationResult.Denied("That clan no longer exists.");
        }

        InvalidateCache(actorId, targetId);
        return YautjaClanMutationResult.Successful;
    }

    public async Task<YautjaClanMutationResult> MoveMember(
        NetUserId actorId,
        NetUserId targetId,
        int? destinationClanId)
    {
        var actor = await Resolve(actorId);
        var target = await Resolve(targetId);
        if (!YautjaClanPolicy.CanMove(ToSnapshot(actorId, actor), ToSnapshot(targetId, target)))
            return YautjaClanMutationResult.Denied("You do not have permission to move that hunter.");

        var keepAncient = target.Rank == YautjaRank.Ancient &&
                          target.Permissions.HasFlag(YautjaClanPermission.AdminAncient);
        var rank = keepAncient ? YautjaRank.Ancient : YautjaRank.Blooded;
        var permissions = keepAncient ? YautjaClanPermission.AdminAncient : PermissionsForRank(YautjaRank.Blooded);
        if (!await _db.UpsertYautjaClanMemberAsync(new YautjaClanMemberRecord(
            targetId.UserId,
            destinationClanId,
            (int) rank,
            (int) permissions,
            target.Honor,
            false)))
        {
            return YautjaClanMutationResult.Denied("That clan does not exist or is no longer active.");
        }

        InvalidateCache(actorId, targetId);
        return YautjaClanMutationResult.Successful;
    }

    public async Task<YautjaClanMutationResult> SetAncient(
        NetUserId actorId,
        NetUserId targetId,
        bool enabled)
    {
        var actor = await Resolve(actorId);
        var target = await Resolve(targetId);
        if (!YautjaClanPolicy.CanSetAncient(ToSnapshot(actorId, actor), ToSnapshot(targetId, target), enabled))
            return YautjaClanMutationResult.Denied("Only an Ancient manager can change Ancient status.");

        if (actor.ClanId is not { } clanId || target.ClanId != clanId)
            return YautjaClanMutationResult.Denied("Both hunters must belong to the same clan.");

        var rank = enabled ? YautjaRank.Ancient : YautjaRank.Blooded;
        if (!await _db.UpsertYautjaClanMemberAsync(new YautjaClanMemberRecord(
            targetId.UserId,
            clanId,
            (int) rank,
            (int) PermissionsForRank(rank),
            target.Honor,
            false)))
        {
            return YautjaClanMutationResult.Denied("That clan no longer exists.");
        }

        InvalidateCache(actorId, targetId);
        return YautjaClanMutationResult.Successful;
    }

    public async Task<bool> SetMaintenanceRank(NetUserId userId, YautjaRank rank)
    {
        if (!YautjaClanPolicy.GetNormalAssignableRanks().Contains(rank) && rank != YautjaRank.Ancient)
            throw new ArgumentException("The requested rank cannot be persisted.", nameof(rank));

        var existing = await _db.GetYautjaClanMemberAsync(userId.UserId);
        if (!await _db.UpsertYautjaClanMemberAsync(new YautjaClanMemberRecord(
            userId.UserId,
            existing?.ClanId,
            (int) rank,
            (int) PermissionsForRank(rank),
            existing?.Honor ?? 0,
            true)))
        {
            return false;
        }

        InvalidateCache(userId);
        return true;
    }

    public static YautjaClanResolution ResolveSpecial(
        YautjaWhitelistFlags whitelistFlags,
        bool youngbloodRole)
    {
        if (youngbloodRole)
            return new(YautjaRank.YoungBlood, null, YautjaClanPermission.None, false, 0, whitelistFlags);

        if (whitelistFlags.HasFlag(YautjaWhitelistFlags.Leader) || whitelistFlags.HasFlag(YautjaWhitelistFlags.Council))
            return new(YautjaRank.Ancient, null, YautjaClanPermission.All, false, 0, whitelistFlags);

        return new(YautjaRank.Blooded, null, YautjaClanPermission.UserView, false, 0, whitelistFlags);
    }

    public static YautjaRank SanitizeStoredRank(int? value)
    {
        if (value is not { } raw || raw < byte.MinValue || raw > byte.MaxValue || !Enum.IsDefined((YautjaRank) raw))
            return YautjaRank.Blooded;

        var rank = (YautjaRank) raw;
        return rank == YautjaRank.YoungBlood ? YautjaRank.Blooded : rank;
    }

    private static bool TryReadPermissions(int raw, out YautjaClanPermission permissions)
    {
        if (raw < byte.MinValue || raw > byte.MaxValue)
        {
            permissions = YautjaClanPermission.None;
            return false;
        }

        permissions = (YautjaClanPermission) (byte) raw;
        return Enum.IsDefined(permissions);
    }

    public static YautjaClanPermission PermissionsForRank(YautjaRank rank)
    {
        return rank switch
        {
            YautjaRank.Unblooded => YautjaClanPermission.AdminModify,
            YautjaRank.Blooded => YautjaClanPermission.UserAll,
            YautjaRank.Elite => YautjaClanPermission.UserAll,
            YautjaRank.Elder => YautjaClanPermission.UserAll,
            YautjaRank.Leader => YautjaClanPermission.UserAll | YautjaClanPermission.AdminModify,
            YautjaRank.Ancient => YautjaClanPermission.AdminAncient,
            _ => YautjaClanPermission.None,
        };
    }

    private static YautjaClanMemberSnapshot ToSnapshot(NetUserId userId, YautjaClanResolution resolution)
    {
        return new(userId, resolution.ClanId, resolution.Rank, resolution.Permissions, resolution.IsLegacy, resolution.Honor);
    }

    public void InvalidateCache(params NetUserId[] userIds)
    {
        foreach (var userId in userIds)
        {
            _cacheVersions.Increment(userId);
            _cache.Remove(userId);
        }
    }
}

internal sealed class YautjaClanCacheVersions
{
    private readonly Dictionary<NetUserId, long> _versions = new();

    public long Capture(NetUserId userId)
    {
        return _versions.TryGetValue(userId, out var version) ? version : 0;
    }

    public void Increment(NetUserId userId)
    {
        _versions[userId] = Capture(userId) + 1;
    }

    public bool IsCurrent(NetUserId userId, long capturedVersion)
    {
        return Capture(userId) == capturedVersion;
    }
}

public readonly record struct YautjaClanMutationResult(bool Succeeded, string? Error)
{
    public static readonly YautjaClanMutationResult Successful = new(true, null);

    public static YautjaClanMutationResult Denied(string error)
    {
        return new(false, error);
    }
}
