using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared._RuMC14.Governance;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Threading.Tasks;

namespace Content.Server._RuMC14.Governance;

/// <summary>
/// Keeps the current round staffed with temporary community responders and owns the in-game
/// invitation flow. PostgreSQL remains authoritative for eligibility, responses, rating and grants.
/// </summary>
public sealed class GovernanceDutySystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _database = default!;
    [Dependency] private readonly EuiManager _euis = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly GovernanceManager _governance = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private float _elapsed = float.MaxValue;
    private bool _checking;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_governance.Enabled || _ticker.RunLevel != GameRunLevel.InRound)
            return;

        _elapsed += frameTime;
        var interval = Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyCheckSeconds), 10, 600);
        if (_checking || _elapsed < interval)
            return;

        _elapsed = 0;
        _ = CheckStaffingAsync();
    }

    private async Task CheckStaffingAsync()
    {
        if (_checking)
            return;

        _checking = true;
        try
        {
            var observers = new List<NetUserId>();
            foreach (var session in _players.Sessions)
            {
                if (session.Status is not (SessionStatus.Connected or SessionStatus.InGame) ||
                    session.AttachedEntity is not { } entity ||
                    !HasComp<GhostComponent>(entity))
                {
                    continue;
                }

                observers.Add(session.UserId);
            }

            var target = Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyTargetResponders), 0, 16);
            var inviteSeconds = Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyInviteSeconds), 30, 600);
            var acceptReward = Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyAcceptReward), 0, 1000);
            var declinePenalty = Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyDeclinePenalty), 0, 1000);
            var expiryPenalty = Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyExpiryPenalty), 0, 1000);
            var invitations = await _database.CreateGovernanceDutyInvitationsAsync(
                _ticker.RoundId,
                observers,
                target,
                TimeSpan.FromSeconds(inviteSeconds),
                expiryPenalty);

            foreach (var invitation in invitations)
            {
                if (!_players.TryGetSessionById(invitation.UserId, out var session) ||
                    session.AttachedEntity is not { } entity ||
                    !HasComp<GhostComponent>(entity))
                {
                    // Never punish a candidate for an invitation that could not be delivered.
                    await _database.RespondGovernanceDutyInvitationAsync(
                        invitation.Id,
                        invitation.UserId,
                        invitation.RoundId,
                        GovernanceDutyInvitationChoice.Recuse,
                        TimeSpan.FromMinutes(1),
                        0,
                        0,
                        0);
                    continue;
                }

                _euis.OpenEui(
                    new GovernanceDutyInviteEui(
                        invitation.Id,
                        invitation.RoundId,
                        invitation.ExpiresAt,
                        acceptReward,
                        declinePenalty,
                        expiryPenalty,
                        this),
                    session);
            }
        }
        catch (Exception exception)
        {
            Log.Error($"Community duty staffing check failed: {exception}");
        }
        finally
        {
            _checking = false;
        }
    }

    public async Task RespondToInvitationAsync(
        ICommonSession player,
        long invitationId,
        GovernanceDutyInviteChoice choice)
    {
        if (!_governance.Enabled || _ticker.RunLevel != GameRunLevel.InRound)
        {
            _chat.DispatchServerMessage(player, Loc.GetString("governance-duty-response-invalid"));
            return;
        }

        if (choice == GovernanceDutyInviteChoice.Accept &&
            (player.AttachedEntity is not { } entity || !HasComp<GhostComponent>(entity)))
        {
            // The player returned to a body after receiving the observer-only invitation.
            // Treat this as unavailable instead of leaving it to expire with a penalty.
            try
            {
                await _database.RespondGovernanceDutyInvitationAsync(
                    invitationId,
                    player.UserId,
                    _ticker.RoundId,
                    GovernanceDutyInvitationChoice.Recuse,
                    TimeSpan.FromMinutes(1),
                    0,
                    0,
                    0);
            }
            catch (Exception exception)
            {
                Log.Error($"Could not recuse invalid duty acceptance {invitationId}: {exception}");
            }

            _chat.DispatchServerMessage(player, Loc.GetString("governance-duty-response-observer-required"));
            _elapsed = float.MaxValue;
            return;
        }

        try
        {
            var response = await _database.RespondGovernanceDutyInvitationAsync(
                invitationId,
                player.UserId,
                _ticker.RoundId,
                choice switch
                {
                    GovernanceDutyInviteChoice.Accept => GovernanceDutyInvitationChoice.Accept,
                    GovernanceDutyInviteChoice.Decline => GovernanceDutyInvitationChoice.Decline,
                    _ => GovernanceDutyInvitationChoice.Recuse,
                },
                TimeSpan.FromMinutes(Math.Clamp(
                    _cfg.GetCVar(CCCVars.GovernanceDutySessionMinutes),
                    1,
                    1440)),
                Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyAcceptReward), 0, 1000),
                Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyDeclinePenalty), 0, 1000),
                Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceDutyExpiryPenalty), 0, 1000));

            if (response.Status == GovernanceDutyResponseStatus.Accepted)
                await _governance.RefreshDutyAsync(player.UserId);

            _chat.DispatchServerMessage(
                player,
                Loc.GetString(
                    ResponseLocale(response.Status),
                    ("rating", response.CivicRating)));
            _elapsed = float.MaxValue;
        }
        catch (Exception exception)
        {
            Log.Error($"Community duty invitation {invitationId} response failed: {exception}");
            _chat.DispatchServerMessage(player, Loc.GetString("governance-duty-response-invalid"));
        }
    }

    private static string ResponseLocale(GovernanceDutyResponseStatus status)
    {
        return status switch
        {
            GovernanceDutyResponseStatus.Accepted => "governance-duty-response-accepted",
            GovernanceDutyResponseStatus.Declined => "governance-duty-response-declined",
            GovernanceDutyResponseStatus.Recused => "governance-duty-response-recused",
            GovernanceDutyResponseStatus.Expired => "governance-duty-response-expired",
            GovernanceDutyResponseStatus.AlreadyHandled => "governance-duty-response-handled",
            GovernanceDutyResponseStatus.NotObserver => "governance-duty-response-observer-required",
            _ => "governance-duty-response-invalid",
        };
    }
}
