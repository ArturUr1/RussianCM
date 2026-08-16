using System;
using Robust.Shared.Network;

namespace Content.Server._RuMC14.Governance;

public sealed record GovernanceDutySession(
    long Id,
    Guid GovernanceUserId,
    NetUserId Ss14UserId,
    int RoundId,
    DateTimeOffset ExpiresAt);

public sealed record GovernanceAuthorization(
    GovernanceDutySession Duty,
    string Capability,
    DateTimeOffset ExpiresAt);

public enum GovernanceDenial
{
    None,
    Disabled,
    DatabaseUnavailable,
    NotOnDuty,
    NotObserver,
    SelfTarget,
    InvalidDuration,
    InvalidInput,
    TargetUnavailable,
    AlreadyFrozen,
}

public readonly record struct GovernanceActionResult(GovernanceDenial Denial)
{
    public bool Allowed => Denial == GovernanceDenial.None;

    public static GovernanceActionResult Success => new(GovernanceDenial.None);
}

public static class GovernancePolicy
{
    public static GovernanceDenial ValidateFreeze(
        bool enabled,
        bool actorIsObserver,
        NetUserId actor,
        NetUserId target,
        int durationSeconds,
        int maximumSeconds)
    {
        if (!enabled)
            return GovernanceDenial.Disabled;
        if (!actorIsObserver)
            return GovernanceDenial.NotObserver;
        if (actor == target)
            return GovernanceDenial.SelfTarget;
        if (durationSeconds < 1 || durationSeconds > maximumSeconds)
            return GovernanceDenial.InvalidDuration;

        return GovernanceDenial.None;
    }
}
