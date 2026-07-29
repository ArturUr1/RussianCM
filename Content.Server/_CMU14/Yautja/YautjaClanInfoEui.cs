using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Server.Players.JobWhitelist;
using Content.Shared._CMU14.Yautja;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server._CMU14.Yautja;

public sealed class YautjaClanInfoEui : BaseEui
{
    [Dependency] private YautjaClanManager _clanManager = default!;
    [Dependency] private YautjaRankManager _rankManager = default!;
    [Dependency] private JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPlayerManager _players = default!;

    private string _statusMessage = "";
    private int? _selectedClanId;

    public YautjaClanInfoEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();

        var view = _clanManager.GetView(Player.UserId, _selectedClanId).GetAwaiter().GetResult();
        var viewer = new YautjaClanMemberSnapshot(
            Player.UserId,
            view.Viewer.ClanId,
            view.Viewer.Rank,
            view.Viewer.Permissions,
            view.Viewer.IsLegacy,
            view.Viewer.Honor);
        if (!YautjaClanPolicy.CanView(viewer))
        {
            Close();
            return;
        }

        if (YautjaClanPolicy.HasPermission(viewer.Permissions, YautjaClanPermission.AdminView))
            _selectedClanId = view.Viewer.ClanId;

        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        var view = _clanManager.GetView(Player.UserId, _selectedClanId).GetAwaiter().GetResult();
        _selectedClanId = view.ClanId;
        var viewer = new YautjaClanMemberSnapshot(
            Player.UserId,
            view.Viewer.ClanId,
            view.Viewer.Rank,
            view.Viewer.Permissions,
            view.Viewer.IsLegacy,
            view.Viewer.Honor);
        var members = view.Members
            .OrderByDescending(member => member.Rank)
            .ThenBy(member => GetPlayerName(member.PlayerId))
            .Select(member =>
            {
                var canManage = YautjaClanPolicy.GetNormalAssignableRanks().Any(requestedRank =>
                    YautjaClanPolicy.CanModifyRank(
                        viewer,
                        member,
                        requestedRank,
                        view.Members.Count,
                        view.Members.Count(candidate => candidate.Rank == requestedRank)));
                var canSetAncient = YautjaClanPolicy.CanSetAncient(viewer, member, true) ||
                                    YautjaClanPolicy.CanSetAncient(viewer, member, false);
                var canMove = YautjaClanPolicy.CanMove(viewer, member);
                return new YautjaClanInfoMemberState(
                    member.PlayerId,
                    GetPlayerName(member.PlayerId),
                    member.Rank,
                    YautjaRankMetadata.For(member.Rank).IconState,
                    member.Honor,
                    _players.TryGetSessionById(member.PlayerId, out _),
                    canManage,
                    canSetAncient,
                    canMove);
            })
            .ToList();

        var canEditDescription = view.ClanId is { } descriptionClanId &&
                                 YautjaClanPolicy.CanManageClan(
                                     viewer,
                                     descriptionClanId,
                                     YautjaClanPermission.UserModify);
        var canEditAppearance = YautjaClanPolicy.HasPermission(
                                    viewer.Permissions,
                                    YautjaClanPermission.AdminView) &&
                                YautjaClanPolicy.HasPermission(
                                    viewer.Permissions,
                                    YautjaClanPermission.AdminModify);
        var canSetHonor = view.ClanId is not null &&
                          YautjaClanPolicy.HasPermission(
                              viewer.Permissions,
                              YautjaClanPermission.AdminManager);
        var canPurge = YautjaClanPolicy.HasPermission(
            viewer.Permissions,
            YautjaClanPermission.AdminManager);

        return new YautjaClanInfoEuiState(
            view.ClanId,
            view.ClanName,
            view.ClanDescription,
            view.ClanHonor,
            view.ClanColor,
            viewer.Rank,
            viewer.Permissions,
            view.AvailableClans.ToList(),
            canEditDescription,
            canEditAppearance,
            canSetHonor,
            canPurge,
            canSetHonor,
            YautjaClanPolicy.GetNormalAssignableRanks().Any(requestedRank =>
                view.Members.Any(member =>
                    YautjaClanPolicy.CanModifyRank(
                        viewer,
                        member,
                        requestedRank,
                        view.Members.Count,
                        view.Members.Count(candidate => candidate.Rank == requestedRank)))),
            view.Members.Any(member => YautjaClanPolicy.CanSetAncient(viewer, member, true)),
            view.Members.Any(member => YautjaClanPolicy.CanMove(viewer, member)),
            members,
            _statusMessage);
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        var currentView = await _clanManager.GetView(Player.UserId, _selectedClanId);
        var currentViewer = new YautjaClanMemberSnapshot(
            Player.UserId,
            currentView.Viewer.ClanId,
            currentView.Viewer.Rank,
            currentView.Viewer.Permissions,
            currentView.Viewer.IsLegacy,
            currentView.Viewer.Honor);
        if (!YautjaClanPolicy.CanView(currentViewer))
        {
            Close();
            return;
        }

        base.HandleMessage(msg);

        switch (msg)
        {
            case YautjaClanInfoInitializeMessage:
            case YautjaClanInfoRefreshMessage:
                StateDirty();
                break;
            case YautjaClanInfoSelectClanMessage selectClan:
                _selectedClanId = selectClan.ClanId;
                StateDirty();
                break;
            case YautjaClanInfoSetRankMessage setRank:
                var rankResult = await _clanManager.SetRank(Player.UserId, setRank.Target, setRank.Rank);
                _statusMessage = rankResult.Succeeded
                    ? Loc.GetString("cmu-yautja-clan-info-rank-updated")
                    : rankResult.Error ?? Loc.GetString("cmu-yautja-clan-info-action-denied");
                if (rankResult.Succeeded)
                {
                    await _rankManager.Refresh(setRank.Target);
                    await _jobWhitelist.RefreshYautjaWhitelist(setRank.Target);
                    _adminLog.Add(
                        LogType.Action,
                        LogImpact.Medium,
                        $"{Player.Name} changed Yautja rank for {setRank.Target} to {setRank.Rank}.");
                }
                StateDirty();
                break;
            case YautjaClanInfoSetAncientMessage setAncient:
                var ancientResult = await _clanManager.SetAncient(Player.UserId, setAncient.Target, setAncient.Enabled);
                _statusMessage = ancientResult.Succeeded
                    ? Loc.GetString("cmu-yautja-clan-info-ancient-updated")
                    : ancientResult.Error ?? Loc.GetString("cmu-yautja-clan-info-action-denied");
                if (ancientResult.Succeeded)
                {
                    await _rankManager.Refresh(setAncient.Target);
                    await _jobWhitelist.RefreshYautjaWhitelist(setAncient.Target);
                    _adminLog.Add(
                        LogType.Action,
                        LogImpact.Medium,
                        $"{Player.Name} { (setAncient.Enabled ? "made" : "demoted") } Yautja {setAncient.Target} { (setAncient.Enabled ? "Ancient" : "from Ancient") }.");
                }
                StateDirty();
                break;
            case YautjaClanInfoUpdateDescriptionMessage description:
                var descriptionResult = await _clanManager.UpdateDescription(
                    Player.UserId,
                    description.ClanId,
                    description.Description);
                _statusMessage = descriptionResult.Succeeded
                    ? Loc.GetString("cmu-yautja-clan-info-description-updated")
                    : descriptionResult.Error ?? Loc.GetString("cmu-yautja-clan-info-action-denied");
                StateDirty();
                break;
            case YautjaClanInfoUpdateAppearanceMessage appearance:
                var appearanceResult = await _clanManager.UpdateAppearance(
                    Player.UserId,
                    appearance.ClanId,
                    appearance.Name,
                    appearance.Color);
                _statusMessage = appearanceResult.Succeeded
                    ? Loc.GetString("cmu-yautja-clan-info-appearance-updated")
                    : appearanceResult.Error ?? Loc.GetString("cmu-yautja-clan-info-action-denied");
                StateDirty();
                break;
            case YautjaClanInfoSetHonorMessage honor:
                var honorResult = await _clanManager.SetClanHonor(
                    Player.UserId,
                    honor.ClanId,
                    honor.Honor);
                _statusMessage = honorResult.Succeeded
                    ? Loc.GetString("cmu-yautja-clan-info-honor-updated")
                    : honorResult.Error ?? Loc.GetString("cmu-yautja-clan-info-action-denied");
                StateDirty();
                break;
            case YautjaClanInfoPurgeMemberMessage purge:
                var purgeResult = await _clanManager.PurgeMember(Player.UserId, purge.Target);
                _statusMessage = purgeResult.Succeeded
                    ? Loc.GetString("cmu-yautja-clan-info-member-purged")
                    : purgeResult.Error ?? Loc.GetString("cmu-yautja-clan-info-action-denied");
                if (purgeResult.Succeeded)
                {
                    foreach (var affectedPlayer in purgeResult.AffectedPlayers ?? [])
                    {
                        await _rankManager.Refresh(affectedPlayer);
                        await _jobWhitelist.RefreshYautjaWhitelist(affectedPlayer);
                    }
                    _adminLog.Add(
                        LogType.Action,
                        LogImpact.Medium,
                        $"{Player.Name} purged Yautja clan profile {purge.Target}.");
                }
                StateDirty();
                break;
            case YautjaClanInfoDeleteClanMessage deleteClan:
                var deleteResult = await _clanManager.DeleteClan(Player.UserId, deleteClan.ClanId);
                _statusMessage = deleteResult.Succeeded
                    ? Loc.GetString("cmu-yautja-clan-info-clan-deleted")
                    : deleteResult.Error ?? Loc.GetString("cmu-yautja-clan-info-action-denied");
                if (deleteResult.Succeeded)
                {
                    _selectedClanId = null;
                    foreach (var affectedPlayer in deleteResult.AffectedPlayers ?? [])
                    {
                        await _rankManager.Refresh(affectedPlayer);
                        await _jobWhitelist.RefreshYautjaWhitelist(affectedPlayer);
                    }
                    _adminLog.Add(
                        LogType.Action,
                        LogImpact.Medium,
                        $"{Player.Name} deleted Yautja clan {deleteClan.ClanId}.");
                }
                StateDirty();
                break;
            case YautjaClanInfoMoveMemberMessage moveMember:
                var moveResult = await _clanManager.MoveMember(Player.UserId, moveMember.Target, moveMember.ClanId);
                _statusMessage = moveResult.Succeeded
                    ? Loc.GetString("cmu-yautja-clan-info-member-moved")
                    : moveResult.Error ?? Loc.GetString("cmu-yautja-clan-info-action-denied");
                if (moveResult.Succeeded)
                {
                    await _rankManager.Refresh(moveMember.Target);
                    await _jobWhitelist.RefreshYautjaWhitelist(moveMember.Target);
                    _adminLog.Add(
                        LogType.Action,
                        LogImpact.Medium,
                        $"{Player.Name} moved Yautja {moveMember.Target} to clan {moveMember.ClanId?.ToString() ?? "none"}.");
                }
                StateDirty();
                break;
        }
    }

    private string GetPlayerName(NetUserId userId)
    {
        if (_players.TryGetSessionById(userId, out var session))
            return session.Name;

        return _players.TryGetPlayerData(userId, out var data)
            ? data.UserName
            : userId.ToString();
    }
}
