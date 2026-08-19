using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Governance;

[Serializable, NetSerializable]
public enum GovernanceAHelpQueueAction
{
    Refresh,
    SelectTicket,
    Claim,
    SendMessage,
    WaitingPlayer,
    Resolve,
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpTranscriptEntry(
    string senderName,
    string body,
    DateTime createdAt,
    bool fromResponder)
{
    public readonly string SenderName = senderName;
    public readonly string Body = body;
    public readonly DateTime CreatedAt = createdAt;
    public readonly bool FromResponder = fromResponder;
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
    long selectedTicketId,
    GovernanceAHelpTranscriptEntry[] transcript,
    string? error = null) : EuiStateBase
{
    public readonly GovernanceAHelpQueueItem[] Tickets = tickets;
    public readonly long SelectedTicketId = selectedTicketId;
    public readonly GovernanceAHelpTranscriptEntry[] Transcript = transcript;
    public readonly string? Error = error;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpQueueMessage(
    GovernanceAHelpQueueAction action,
    long ticketId = 0,
    string? text = null) : EuiMessageBase
{
    public readonly GovernanceAHelpQueueAction Action = action;
    public readonly long TicketId = ticketId;
    public readonly string? Text = text;
}

[Serializable, NetSerializable]
public enum GovernanceAHelpPlayerAction
{
    Refresh,
    SendMessage,
    Resolve,
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpPlayerEuiState(
    long? ticketId,
    string status,
    string responderName,
    GovernanceAHelpTranscriptEntry[] transcript,
    bool canSend,
    string? error = null) : EuiStateBase
{
    public readonly long? TicketId = ticketId;
    public readonly string Status = status;
    public readonly string ResponderName = responderName;
    public readonly GovernanceAHelpTranscriptEntry[] Transcript = transcript;
    public readonly bool CanSend = canSend;
    public readonly string? Error = error;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpPlayerMessage(
    GovernanceAHelpPlayerAction action,
    string? text = null) : EuiMessageBase
{
    public readonly GovernanceAHelpPlayerAction Action = action;
    public readonly string? Text = text;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpOpenRequest : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class GovernanceAHelpPlayerReplyReceived(long ticketId, string preview) : EntityEventArgs
{
    public readonly long TicketId = ticketId;
    public readonly string Preview = preview;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpAccessUpdated(bool active) : EntityEventArgs
{
    public readonly bool Active = active;
}

/// <summary>
/// Legacy bridge event kept only so old Bwoink integration can compile while the Governance UI
/// migration is in progress. New Governance AHelp code does not emit this event.
/// </summary>
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
