using System;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Ghost;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.Governance;

public sealed class GovernanceSystem : EntitySystem
{
    public const string FreezeCapability = "moderation.freeze";

    [Dependency] private readonly AdminFrozenSystem _adminFrozen = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GovernanceManager _governance = default!;

    public async Task<GovernanceActionResult> TryFreezeAsync(
        ICommonSession actor,
        ICommonSession target,
        int durationSeconds,
        string incidentId,
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
            return await DenyAsync(denial, actor, target, incidentId, durationSeconds);

        if (string.IsNullOrWhiteSpace(incidentId) || incidentId.Length > 128 ||
            string.IsNullOrWhiteSpace(reason) || reason.Length > 512)
        {
            return await DenyAsync(
                GovernanceDenial.InvalidInput,
                actor,
                target,
                incidentId,
                durationSeconds);
        }

        var authorization = await _governance.AuthorizeAsync(actor.UserId, _gameTicker.RoundId, FreezeCapability);
        if (authorization == null)
            return await DenyAsync(GovernanceDenial.NotOnDuty, actor, target, incidentId, durationSeconds);

        // State may have changed while the database request was in flight.
        if (actor.AttachedEntity is not { } currentActor || !HasComp<GhostComponent>(currentActor))
            return await DenyAsync(GovernanceDenial.NotObserver, actor, target, incidentId, durationSeconds);
        if (target.AttachedEntity is not { } targetEntity || Deleted(targetEntity))
            return await DenyAsync(GovernanceDenial.TargetUnavailable, actor, target, incidentId, durationSeconds);
        if (HasComp<AdminFrozenComponent>(targetEntity))
            return await DenyAsync(GovernanceDenial.AlreadyFrozen, actor, target, incidentId, durationSeconds);

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
            incidentId,
            new
            {
                round_id = _gameTicker.RoundId,
                duration_seconds = durationSeconds,
                reason,
                duty_session_id = authorization.Duty.Id,
                capability_expires_at = authorization.ExpiresAt,
            });

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
        string incidentId,
        int durationSeconds)
    {
        await _governance.AuditAsync(
            "moderation.freeze.denied",
            actor.UserId,
            target.UserId,
            "live_incident",
            string.IsNullOrWhiteSpace(incidentId) ? "invalid" : incidentId,
            new
            {
                round_id = _gameTicker.RoundId,
                duration_seconds = durationSeconds,
                denial = denial.ToString(),
            });
        return new GovernanceActionResult(denial);
    }
}
