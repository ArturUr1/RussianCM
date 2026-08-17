using System;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.Governance;

public sealed class GovernanceSystem : EntitySystem
{
    public const string FreezeCapability = "moderation.freeze";
    public const string RoundRemoveCapability = "moderation.round_remove";

    [Dependency] private readonly AdminFrozenSystem _adminFrozen = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GovernanceManager _governance = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private readonly Dictionary<NetUserId, int> _roundRemoved = new();

    public override void Initialize()
    {
        base.Initialize();
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        base.Shutdown();
    }

    public async Task<GovernanceActionResult> TryFreezeAsync(
        ICommonSession actor,
        ICommonSession target,
        int durationSeconds,
        long actionId,
        string reason)
    {
        var actorIsObserver = actor.AttachedEntity is { } actorEntity && HasComp<GhostComponent>(actorEntity);
        var maximumSeconds = Math.Clamp(_cfg.GetCVar(CCCVars.GovernanceFreezeMaxSeconds), 1, 120);
        var denial = GovernancePolicy.ValidateFreeze(
            _governance.Enabled,
            actorIsObserver,
            actor.UserId,
            target.UserId,
            durationSeconds,
            maximumSeconds);

        if (denial != GovernanceDenial.None)
            return await DenyAsync(denial, actor, target, actionId, durationSeconds);

        if (actionId <= 0 || string.IsNullOrWhiteSpace(reason) || reason.Length > 512)
        {
            return await DenyAsync(
                GovernanceDenial.InvalidInput,
                actor,
                target,
                actionId,
                durationSeconds);
        }

        var authorization = await _governance.AuthorizeAsync(actor.UserId, _gameTicker.RoundId, FreezeCapability);
        if (authorization == null)
            return await DenyAsync(GovernanceDenial.NotOnDuty, actor, target, actionId, durationSeconds);

        var action = await _governance.AuthorizeActionAsync(
            actor.UserId, target.UserId, _gameTicker.RoundId, actionId, "freeze");
        if (action == null)
            return await DenyAsync(GovernanceDenial.ActionNotApproved, actor, target, actionId, durationSeconds);

        // State may have changed while the database request was in flight.
        if (actor.AttachedEntity is not { } currentActor || !HasComp<GhostComponent>(currentActor))
            return await DenyAsync(GovernanceDenial.NotObserver, actor, target, actionId, durationSeconds);
        if (target.AttachedEntity is not { } targetEntity || Deleted(targetEntity))
            return await DenyAsync(GovernanceDenial.TargetUnavailable, actor, target, actionId, durationSeconds);
        if (HasComp<AdminFrozenComponent>(targetEntity))
            return await DenyAsync(GovernanceDenial.AlreadyFrozen, actor, target, actionId, durationSeconds);

        var token = Guid.NewGuid();
        var governanceFrozen = EnsureComp<GovernanceFrozenComponent>(targetEntity);
        governanceFrozen.Token = token;
        _adminFrozen.FreezeAndMute(targetEntity);

        Timer.Spawn(TimeSpan.FromSeconds(durationSeconds), () => ReleaseFreeze(targetEntity, token));
        await _governance.AuditAsync(
            "moderation.freeze.executed",
            actor.UserId,
            target.UserId,
            "live_incident",
            action.IncidentId.ToString(),
            new
            {
                round_id = _gameTicker.RoundId,
                duration_seconds = durationSeconds,
                reason,
                duty_session_id = authorization.Duty.Id,
                capability_expires_at = authorization.ExpiresAt,
                moderation_action_id = actionId,
            });
        await _governance.CompleteActionAsync(actionId);

        return GovernanceActionResult.Success;
    }

    public async Task<GovernanceActionResult> TryRoundRemoveAsync(
        ICommonSession actor,
        ICommonSession target,
        long actionId,
        string reason)
    {
        if (!_governance.Enabled)
            return await DenyAsync(GovernanceDenial.Disabled, actor, target, actionId, 0, "round_remove");
        if (actor.AttachedEntity is not { } actorEntity || !HasComp<GhostComponent>(actorEntity))
            return await DenyAsync(GovernanceDenial.NotObserver, actor, target, actionId, 0, "round_remove");
        if (actor.UserId == target.UserId || actionId <= 0 || string.IsNullOrWhiteSpace(reason) || reason.Length > 512)
            return await DenyAsync(GovernanceDenial.InvalidInput, actor, target, actionId, 0, "round_remove");
        var capability = await _governance.AuthorizeAsync(actor.UserId, _gameTicker.RoundId, RoundRemoveCapability);
        if (capability == null)
            return await DenyAsync(GovernanceDenial.NotOnDuty, actor, target, actionId, 0, "round_remove");
        var action = await _governance.AuthorizeActionAsync(
            actor.UserId, target.UserId, _gameTicker.RoundId, actionId, "round_remove");
        if (action == null)
            return await DenyAsync(GovernanceDenial.ActionNotApproved, actor, target, actionId, 0, "round_remove");

        _roundRemoved[target.UserId] = _gameTicker.RoundId;
        await _governance.CompleteActionAsync(actionId);
        await _governance.AuditAsync(
            "moderation.round_remove.executed", actor.UserId, target.UserId, "live_incident", action.IncidentId.ToString(),
            new { round_id = _gameTicker.RoundId, reason, moderation_action_id = actionId, duty_session_id = capability.Duty.Id });
        target.Channel.Disconnect("Вы удалены до конца раунда решением дежурных сообщества.");
        return GovernanceActionResult.Success;
    }

    private void ReleaseFreeze(EntityUid target, Guid token)
    {
        if (Deleted(target) || !TryComp<GovernanceFrozenComponent>(target, out var governanceFrozen) ||
            governanceFrozen.Token != token)
        {
            return;
        }

        RemComp<GovernanceFrozenComponent>(target);
        RemComp<AdminFrozenComponent>(target);
    }

    private async Task<GovernanceActionResult> DenyAsync(
        GovernanceDenial denial,
        ICommonSession actor,
        ICommonSession target,
        long actionId,
        int durationSeconds,
        string actionType = "freeze")
    {
        await _governance.AuditAsync(
            $"moderation.{actionType}.denied",
            actor.UserId,
            target.UserId,
            "moderation_action",
            actionId <= 0 ? "invalid" : actionId.ToString(),
            new
            {
                round_id = _gameTicker.RoundId,
                duration_seconds = durationSeconds,
                denial = denial.ToString(),
            });
        return new GovernanceActionResult(denial);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (!_roundRemoved.TryGetValue(args.Session.UserId, out var roundId))
            return;
        if (roundId != _gameTicker.RoundId)
        {
            _roundRemoved.Remove(args.Session.UserId);
            return;
        }
        if (args.NewStatus is SessionStatus.Connected or SessionStatus.InGame)
            args.Session.Channel.Disconnect("Вы удалены до конца текущего раунда решением дежурных сообщества.");
    }
}
