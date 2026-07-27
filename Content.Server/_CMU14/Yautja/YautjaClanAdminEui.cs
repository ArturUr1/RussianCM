using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared._CMU14.Yautja;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server._CMU14.Yautja;

public sealed class YautjaClanAdminEui : BaseEui
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private YautjaClanManager _clanManager = default!;
    [Dependency] private YautjaRankManager _rankManager = default!;
    [Dependency] private IPlayerLocator _playerLocator = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;

    private string _statusMessage = "";
    private string _inspectedPlayer = "";
    private string _inspectedSummary = "";
    private long _clanMutationVersion;
    private int? _lastMutatedClanId;
    private YautjaClanAdminMutationKind _lastMutationKind;
    private readonly YautjaClanAdminStateStore _stateStore = new();
    private readonly SemaphoreSlim _stateRefreshGate = new(1, 1);
    private bool _closed;

    public YautjaClanAdminEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        _closed = false;
        _admin.OnPermsChanged += OnAdminPermsChanged;

        if (!_admin.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
            return;
        }

        _ = RefreshStateAsync();
    }

    public override void Closed()
    {
        base.Closed();
        _closed = true;
        _admin.OnPermsChanged -= OnAdminPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        return _stateStore.Get();
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        if (!_admin.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
            return;
        }

        base.HandleMessage(msg);
        if (msg is CloseEuiMessage)
            return;

        try
        {
            switch (msg)
            {
                case YautjaClanAdminRefreshMessage:
                    break;
                case YautjaClanAdminCreateClanMessage create:
                    await CreateClan(create);
                    break;
                case YautjaClanAdminUpdateClanMessage update:
                    await UpdateClan(update);
                    break;
                case YautjaClanAdminDeleteClanMessage delete:
                    await DeleteClan(delete);
                    break;
                case YautjaClanAdminSetMembershipMessage membership:
                    await SetMembership(membership);
                    break;
                case YautjaClanAdminSetRankMessage rank:
                    await SetRank(rank);
                    break;
                case YautjaClanAdminSetWhitelistMessage whitelist:
                    await SetWhitelist(whitelist);
                    break;
                case YautjaClanAdminInspectMessage inspect:
                    await Inspect(inspect);
                    break;
            }
        }
        catch (Exception e)
        {
            _statusMessage = e.Message;
            Logger.GetSawmill("cmu.yautja.clan_admin").Error($"Yautja clan admin action failed:\n{e}");
        }

        await RefreshStateAsync();
    }

    private async Task RefreshStateAsync()
    {
        await _stateRefreshGate.WaitAsync();
        try
        {
            var clans = await _db.GetYautjaClansAsync();
            var clanStates = new List<YautjaClanAdminClanState>(clans.Count);

            foreach (var clan in clans)
            {
                var memberCount = (await _db.GetYautjaClanMembersAsync(clan.Id)).Count;
                clanStates.Add(new YautjaClanAdminClanState(
                    clan.Id,
                    clan.Name,
                    clan.Description,
                    clan.Honor,
                    clan.Color,
                    memberCount));
            }

            _stateStore.Set(new YautjaClanAdminEuiState(
                clanStates,
                _inspectedPlayer,
                _inspectedSummary,
                _statusMessage,
                _clanMutationVersion,
                _lastMutatedClanId,
                _lastMutationKind));
        }
        catch (Exception e)
        {
            _statusMessage = e.Message;
            Logger.GetSawmill("cmu.yautja.clan_admin").Error($"Yautja clan admin state refresh failed:\n{e}");

            var previousState = _stateStore.Get();
            _stateStore.Set(new YautjaClanAdminEuiState(
                previousState.Clans,
                _inspectedPlayer,
                _inspectedSummary,
                _statusMessage,
                _clanMutationVersion,
                _lastMutatedClanId,
                _lastMutationKind));
        }
        finally
        {
            _stateRefreshGate.Release();
        }

        if (!_closed && !IsShutDown)
            StateDirty();
    }

    private async Task CreateClan(YautjaClanAdminCreateClanMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Name) || string.IsNullOrWhiteSpace(message.Description))
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-invalid-clan");
            return;
        }

        var color = string.IsNullOrWhiteSpace(message.Color) ? "#ffffff" : message.Color.Trim();
        var id = await _db.CreateYautjaClanAsync(message.Name.Trim(), message.Description.Trim(), 0, color);
        _statusMessage = Loc.GetString("cmu-yautja-clan-admin-created", ("id", id));
        _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{Player.Name} created Yautja clan {id} ({message.Name}).");
    }

    private async Task UpdateClan(YautjaClanAdminUpdateClanMessage message)
    {
        if (!YautjaClanAdminValidation.TryNormalize(
                message.Name,
                message.Description,
                message.Color,
                out var fields,
                out var error))
        {
            _statusMessage = error == YautjaClanAdminValidationError.InvalidColor
                ? Loc.GetString("cmu-yautja-clan-admin-invalid-color")
                : Loc.GetString("cmu-yautja-clan-admin-invalid-clan");
            return;
        }

        if (!await _db.UpdateYautjaClanAsync(
                message.ClanId,
                fields.Name,
                fields.Description,
                fields.Color))
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-clan-not-found");
            return;
        }

        _clanMutationVersion++;
        _lastMutatedClanId = message.ClanId;
        _lastMutationKind = YautjaClanAdminMutationKind.Updated;
        _statusMessage = Loc.GetString("cmu-yautja-clan-admin-updated", ("id", message.ClanId));
        _adminLog.Add(
            LogType.AdminCommands,
            LogImpact.Medium,
            $"{Player.Name} updated Yautja clan {message.ClanId} ({fields.Name}).");
    }

    private async Task DeleteClan(YautjaClanAdminDeleteClanMessage message)
    {
        var result = await _db.DeactivateYautjaClanAsync(message.ClanId);
        if (!result.Succeeded)
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-clan-not-found");
            return;
        }

        foreach (var detachedPlayer in result.DetachedPlayers)
        {
            var userId = new NetUserId(detachedPlayer);
            _clanManager.InvalidateCache(userId);
            _rankManager.InvalidateCached(userId);
        }

        _clanMutationVersion++;
        _lastMutatedClanId = message.ClanId;
        _lastMutationKind = YautjaClanAdminMutationKind.Deleted;
        _statusMessage = Loc.GetString(
            "cmu-yautja-clan-admin-deleted",
            ("id", message.ClanId),
            ("members", result.DetachedPlayers.Count));
        _adminLog.Add(
            LogType.AdminCommands,
            LogImpact.Medium,
            $"{Player.Name} deleted Yautja clan {message.ClanId} and detached {result.DetachedPlayers.Count} members.");
    }

    private async Task SetMembership(YautjaClanAdminSetMembershipMessage message)
    {
        var player = await FindPlayer(message.Player);
        if (player == null)
            return;
        if (!YautjaRankManager.IsPersistentRank(message.Rank))
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-invalid-rank");
            return;
        }

        int? clanId;
        if (message.ClanId.Trim().Equals("none", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(message.ClanId))
        {
            clanId = null;
        }
        else if (int.TryParse(message.ClanId, out var parsed) &&
                 await _db.GetYautjaClanAsync(parsed) is { Active: true })
        {
            clanId = parsed;
        }
        else
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-invalid-clan-id");
            return;
        }

        var existing = await _db.GetYautjaClanMemberAsync(player.UserId.UserId);
        await _db.UpsertYautjaClanMemberAsync(new YautjaClanMemberRecord(
            player.UserId.UserId,
            clanId,
            (int) message.Rank,
            (int) YautjaClanManager.PermissionsForRank(message.Rank),
            existing?.Honor ?? 0,
            false));
        _clanManager.InvalidateCache(player.UserId);
        _rankManager.InvalidateCached(player.UserId);
        _statusMessage = Loc.GetString("cmu-yautja-clan-admin-membership-updated", ("player", player.Username));
        _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{Player.Name} set Yautja clan membership for {player.Username} ({player.UserId}) to clan {clanId?.ToString() ?? "none"} at rank {message.Rank}.");
    }

    private async Task SetRank(YautjaClanAdminSetRankMessage message)
    {
        var player = await FindPlayer(message.Player);
        if (player == null)
            return;
        if (!YautjaRankManager.IsPersistentRank(message.Rank))
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-invalid-rank");
            return;
        }

        await _rankManager.Set(player.UserId, message.Rank);
        _statusMessage = Loc.GetString("cmu-yautja-clan-admin-rank-updated", ("player", player.Username));
        _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{Player.Name} set Yautja rank for {player.Username} ({player.UserId}) to {message.Rank}.");
    }

    private async Task SetWhitelist(YautjaClanAdminSetWhitelistMessage message)
    {
        var player = await FindPlayer(message.Player);
        if (player == null)
            return;
        if (!Enum.IsDefined(message.Flags))
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-invalid-whitelist");
            return;
        }

        await _db.SetYautjaWhitelistFlagsAsync(player.UserId.UserId, (int) message.Flags);
        _clanManager.InvalidateCache(player.UserId);
        _rankManager.InvalidateCached(player.UserId);
        _statusMessage = Loc.GetString("cmu-yautja-clan-admin-whitelist-updated", ("player", player.Username));
        _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
            $"{Player.Name} set Yautja whitelist flags for {player.Username} ({player.UserId}) to {message.Flags}.");
    }

    private async Task Inspect(YautjaClanAdminInspectMessage message)
    {
        var player = await FindPlayer(message.Player);
        if (player == null)
            return;

        var resolution = await _clanManager.Resolve(player.UserId);
        var clan = resolution.ClanId is { } clanId
            ? (await _db.GetYautjaClanAsync(clanId))?.Name ?? clanId.ToString()
            : "none";
        _inspectedPlayer = player.Username;
        _inspectedSummary = Loc.GetString(
            "cmu-yautja-clan-admin-inspection",
            ("rank", resolution.Rank),
            ("clan", clan),
            ("permissions", resolution.Permissions),
            ("whitelist", resolution.WhitelistFlags),
            ("legacy", resolution.IsLegacy));
        _statusMessage = Loc.GetString("cmu-yautja-clan-admin-inspected", ("player", player.Username));
    }

    private async Task<LocatedPlayerData?> FindPlayer(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-player-required");
            return null;
        }

        var found = await _playerLocator.LookupIdByNameOrIdAsync(query.Trim());
        if (found == null)
        {
            _statusMessage = Loc.GetString("cmu-yautja-clan-admin-player-not-found", ("player", query));
            return null;
        }

        return found;
    }

    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_admin.HasAdminFlag(Player, AdminFlags.Admin))
            Close();
    }
}
