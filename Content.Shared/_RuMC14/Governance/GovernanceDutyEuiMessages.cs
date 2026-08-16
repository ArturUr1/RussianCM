using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Governance;

[Serializable, NetSerializable]
public enum GovernanceDutyInviteChoice
{
    Accept,
    Decline,
    Recuse,
}

[Serializable, NetSerializable]
public sealed class GovernanceDutyInviteChoiceMessage(
    GovernanceDutyInviteChoice choice) : EuiMessageBase
{
    public readonly GovernanceDutyInviteChoice Choice = choice;
}

[Serializable, NetSerializable]
public sealed class GovernanceDutyInviteEuiState(
    int roundId,
    DateTime expiresAt,
    int acceptReward,
    int declinePenalty,
    int expiryPenalty) : EuiStateBase
{
    public readonly int RoundId = roundId;
    public readonly DateTime ExpiresAt = expiresAt;
    public readonly int AcceptReward = acceptReward;
    public readonly int DeclinePenalty = declinePenalty;
    public readonly int ExpiryPenalty = expiryPenalty;
}
