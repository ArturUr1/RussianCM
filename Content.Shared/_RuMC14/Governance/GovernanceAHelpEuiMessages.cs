using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Governance;

[Serializable, NetSerializable]
public enum GovernanceAHelpQueueAction
{
    Refresh,
    Claim,
    OpenChat,
    WaitingPlayer,
    Resolve,
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpQueueItem(
    long id,
    NetUserId reporterUserId,
    string reporterName,
    string summary,
    string status,
    DateTime createdAt,
    bool claimedByMe)
{
    public readonly long Id = id;
    public readonly NetUserId ReporterUserId = reporterUserId;
    public readonly string ReporterName = reporterName;
    public readonly string Summary = summary;
    public readonly string Status = status;
    public readonly DateTime CreatedAt = createdAt;
    public readonly bool ClaimedByMe = claimedByMe;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpQueueEuiState(
    GovernanceAHelpQueueItem[] tickets,
    string? error = null) : EuiStateBase
{
    public readonly GovernanceAHelpQueueItem[] Tickets = tickets;
    public readonly string? Error = error;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpQueueMessage(
    GovernanceAHelpQueueAction action,
    long ticketId = 0) : EuiMessageBase
{
    public readonly GovernanceAHelpQueueAction Action = action;
    public readonly long TicketId = ticketId;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpAccessUpdated(bool active) : EntityEventArgs
{
    public readonly bool Active = active;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpOpenChannel(NetUserId reporterUserId) : EntityEventArgs
{
    public readonly NetUserId ReporterUserId = reporterUserId;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpQueueChanged(
    long ticketId,
    NetUserId reporterUserId,
    string reporterName,
    string summary,
    int openCount) : EntityEventArgs
{
    public readonly long TicketId = ticketId;
    public readonly NetUserId ReporterUserId = reporterUserId;
    public readonly string ReporterName = reporterName;
    public readonly string Summary = summary;
    public readonly int OpenCount = openCount;
}
