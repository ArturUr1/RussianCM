using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    public async Task<YautjaClanRecord?> GetYautjaClanAsync(int clanId)
    {
        await using var db = await GetDb();
        var clan = await db.DbContext.YautjaClans.SingleOrDefaultAsync(entry => entry.Id == clanId);
        return clan == null ? null : ToRecord(clan);
    }

    public async Task<List<YautjaClanRecord>> GetYautjaClansAsync()
    {
        await using var db = await GetDb();
        var clans = await db.DbContext.YautjaClans
            .Where(entry => entry.Active)
            .OrderBy(entry => entry.Name)
            .ToListAsync();
        return clans.Select(ToRecord).ToList();
    }

    public async Task<YautjaClanMemberRecord?> GetYautjaClanMemberAsync(Guid userId)
    {
        await using var db = await GetDb();
        var member = await db.DbContext.YautjaClanMembers
            .SingleOrDefaultAsync(entry => entry.PlayerUserId == userId);
        return member == null ? null : ToRecord(member);
    }

    public async Task<List<YautjaClanMemberRecord>> GetYautjaClanMembersAsync(int? clanId = null)
    {
        await using var db = await GetDb();
        var query = db.DbContext.YautjaClanMembers.AsQueryable();
        if (clanId is { } id)
            query = query.Where(entry => entry.ClanId == id);

        var members = await query.OrderByDescending(entry => entry.Rank).ToListAsync();
        return members.Select(ToRecord).ToList();
    }

    public async Task<int> CreateYautjaClanAsync(
        string name,
        string description,
        int honor,
        string color,
        bool active = true)
    {
        await using var db = await GetDb();
        var clan = new YautjaClan
        {
            Name = name,
            Description = description,
            Honor = honor,
            Color = color,
            Active = active,
        };
        db.DbContext.YautjaClans.Add(clan);
        await db.DbContext.SaveChangesAsync();
        return clan.Id;
    }

    public async Task<bool> UpdateYautjaClanAsync(
        int clanId,
        string name,
        string description,
        string color)
    {
        await using var db = await GetDb();
        var clan = await db.DbContext.YautjaClans
            .SingleOrDefaultAsync(entry => entry.Id == clanId && entry.Active);
        if (clan == null)
            return false;

        clan.Name = name;
        clan.Description = description;
        clan.Color = color;
        await db.DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<YautjaClanDeleteResult> DeactivateYautjaClanAsync(int clanId)
    {
        await using var db = await GetDb();
        await using var transaction = await db.DbContext.Database.BeginTransactionAsync();

        var clan = await db.DbContext.YautjaClans
            .SingleOrDefaultAsync(entry => entry.Id == clanId && entry.Active);
        if (clan == null)
            return new(false, []);

        var members = await db.DbContext.YautjaClanMembers
            .Where(entry => entry.ClanId == clanId)
            .ToListAsync();
        var detachedPlayers = members
            .Select(entry => entry.PlayerUserId)
            .ToList();

        clan.Active = false;
        foreach (var member in members)
            member.ClanId = null;

        await db.DbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return new(true, detachedPlayers);
    }

    public async Task UpsertYautjaClanMemberAsync(YautjaClanMemberRecord member)
    {
        await using var db = await GetDb();
        await using var transaction = await db.DbContext.Database.BeginTransactionAsync();

        var player = await db.DbContext.Player
            .SingleOrDefaultAsync(entry => entry.UserId == member.PlayerUserId);
        if (player == null)
            throw new InvalidOperationException($"Cannot set Yautja clan member for unknown player {member.PlayerUserId}.");

        var existing = await db.DbContext.YautjaClanMembers
            .SingleOrDefaultAsync(entry => entry.PlayerUserId == member.PlayerUserId);
        if (existing == null)
        {
            db.DbContext.YautjaClanMembers.Add(new YautjaClanMember
            {
                PlayerUserId = member.PlayerUserId,
                ClanId = member.ClanId,
                Rank = member.Rank,
                Permissions = member.Permissions,
                Honor = member.Honor,
                IsLegacy = member.IsLegacy,
            });
        }
        else
        {
            existing.ClanId = member.ClanId;
            existing.Rank = member.Rank;
            existing.Permissions = member.Permissions;
            existing.Honor = member.Honor;
            existing.IsLegacy = member.IsLegacy;
        }

        player.YautjaRank = member.Rank;
        await db.DbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<int> GetYautjaWhitelistFlagsAsync(Guid userId)
    {
        await using var db = await GetDb();
        return await db.DbContext.Player
            .Where(entry => entry.UserId == userId)
            .Select(entry => entry.YautjaWhitelistFlags)
            .SingleOrDefaultAsync();
    }

    public async Task SetYautjaWhitelistFlagsAsync(Guid userId, int flags)
    {
        await using var db = await GetDb();
        var player = await db.DbContext.Player
            .SingleOrDefaultAsync(entry => entry.UserId == userId);
        if (player == null)
            throw new InvalidOperationException($"Cannot set Yautja whitelist flags for unknown player {userId}.");

        player.YautjaWhitelistFlags = flags;
        await db.DbContext.SaveChangesAsync();
    }

    private static YautjaClanRecord ToRecord(YautjaClan clan)
    {
        return new(clan.Id, clan.Name, clan.Description, clan.Honor, clan.Color, clan.Active);
    }

    private static YautjaClanMemberRecord ToRecord(YautjaClanMember member)
    {
        return new(member.PlayerUserId, member.ClanId, member.Rank, member.Permissions, member.Honor, member.IsLegacy);
    }
}
