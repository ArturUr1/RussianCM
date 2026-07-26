using System;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared._CMU14.Yautja;
using Robust.Shared.Network;

namespace Content.Server._CMU14.Yautja;

/// <summary>
/// Resolves the server-owned clan rank without allowing the client profile to grant Young Blood status.
/// </summary>
public sealed partial class YautjaRankManager
{
    [Dependency] private IServerDbManager _db = default!;
    private readonly Dictionary<NetUserId, YautjaRank> _cache = new();

    public async Task<YautjaRank> Resolve(NetUserId userId, bool youngbloodRole = false)
    {
        if (youngbloodRole)
            return YautjaRank.YoungBlood;

        var rank = Sanitize(await _db.GetYautjaRank(userId));
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

        return _cache.GetValueOrDefault(userId, YautjaRank.Blooded);
    }

    public async Task Set(NetUserId userId, YautjaRank rank)
    {
        if (!IsPersistentRank(rank))
            throw new ArgumentException("Young Blood is reserved for the special hunt role.", nameof(rank));

        await _db.SetYautjaRank(userId.UserId, rank);
        _cache[userId] = rank;
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
}
