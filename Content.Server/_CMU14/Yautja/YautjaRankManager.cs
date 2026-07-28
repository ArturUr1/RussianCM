using System;
using System.Threading.Tasks;
using Content.Shared._CMU14.Yautja;
using Robust.Shared.Network;

namespace Content.Server._CMU14.Yautja;

/// <summary>
/// Resolves the server-owned clan rank without allowing the client profile to grant Young Blood status.
/// </summary>
public sealed partial class YautjaRankManager
{
    [Dependency] private YautjaClanManager _clanManager = default!;
    private readonly Dictionary<NetUserId, YautjaRank> _cache = new();
    private readonly Dictionary<NetUserId, long> _cacheVersions = new();

    public async Task<YautjaRank> Resolve(NetUserId userId, bool youngbloodRole = false)
    {
        if (youngbloodRole)
            return YautjaRank.YoungBlood;

        var requestVersion = GetCacheVersion(userId);
        var rank = (await _clanManager.Resolve(userId)).Rank;
        if (IsCacheVersionCurrent(requestVersion, GetCacheVersion(userId)))
            _cache[userId] = rank;

        return rank;
    }

    public async Task Prime(NetUserId userId)
    {
        await Resolve(userId);
    }

    public YautjaRank ResolveCached(NetUserId userId, bool youngbloodRole = false)
    {
        if (youngbloodRole)
            return YautjaRank.YoungBlood;

        if (_cache.TryGetValue(userId, out var rank))
            return rank;

        // Spawn and job-selection events are synchronous. A cache miss must
        // still resolve the authoritative DB value before they grant a role,
        // otherwise a slow lobby prime can silently downgrade a senior rank.
        return Resolve(userId).GetAwaiter().GetResult();
    }

    public async Task Set(NetUserId userId, YautjaRank rank)
    {
        if (!IsPersistentRank(rank))
            throw new ArgumentException("Young Blood is reserved for the special hunt role.", nameof(rank));

        var writeVersion = NextCacheVersion(userId);
        if (!await _clanManager.SetMaintenanceRank(userId, rank))
            throw new InvalidOperationException("The player's Yautja clan no longer exists or is inactive.");

        if (IsCacheVersionCurrent(writeVersion, GetCacheVersion(userId)))
            _cache[userId] = rank;
    }

    public void InvalidateCached(NetUserId userId)
    {
        NextCacheVersion(userId);
        _cache.Remove(userId);
    }

    private long GetCacheVersion(NetUserId userId)
    {
        return _cacheVersions.TryGetValue(userId, out var version) ? version : 0;
    }

    private long NextCacheVersion(NetUserId userId)
    {
        var version = GetCacheVersion(userId) + 1;
        _cacheVersions[userId] = version;
        return version;
    }

    public static YautjaRank Sanitize(YautjaRank? rank)
    {
        if (rank is not { } value || !Enum.IsDefined(value) || value == YautjaRank.YoungBlood)
            return YautjaRank.Blooded;

        return value;
    }

    public static bool IsPersistentRank(YautjaRank rank)
    {
        return Enum.IsDefined(rank) && rank != YautjaRank.YoungBlood;
    }

    public static bool IsCacheVersionCurrent(long requestVersion, long currentVersion)
    {
        return requestVersion == currentVersion;
    }
}
