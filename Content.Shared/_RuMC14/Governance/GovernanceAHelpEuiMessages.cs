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
    CreateIncident,
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
    long incidentId = 0,
    string incidentTargetName = "",
    string incidentType = "",
    string? error = null) : EuiStateBase
{
    public readonly GovernanceAHelpQueueItem[] Tickets = tickets;
    public readonly long SelectedTicketId = selectedTicketId;
    public readonly GovernanceAHelpTranscriptEntry[] Transcript = transcript;
    public readonly long IncidentId = incidentId;
    public readonly string IncidentTargetName = incidentTargetName;
    public readonly string IncidentType = incidentType;
    public readonly string? Error = error;
}

[Serializable, NetSerializable]
public sealed class GovernanceAHelpQueueMessage(
    GovernanceAHelpQueueAction action,
    long ticketId = 0,
    string? text = null,
    string? auxiliaryText = null) : EuiMessageBase
{
    public readonly GovernanceAHelpQueueAction Action = action;
    public readonly long TicketId = ticketId;
    public readonly string? Text = text;
    public readonly string? AuxiliaryText = auxiliaryText;
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
public sealed class GovernanceAHelpResponderReplyReceived(
    long ticketId,
    string reporterName,
    string preview) : EntityEventArgs
{
    public readonly long TicketId = ticketId;
    public readonly string ReporterName = reporterName;
    public readonly string Preview = preview;
}

/// <summary>
/// Compatibility event for the old AHelpUIController. Governance responders must never be promoted
/// into the legacy AdminAHelpUIHandler, so Active intentionally remains false. The actual responder
/// workspace is opened and authorized by GovernanceAHelpSystem.
/// </summary>
[Serializable, NetSerializable]
public sealed class GovernanceAHelpAccessUpdated(bool _) : EntityEventArgs
{
    public readonly bool Active = false;
}

/// <summary>
/// Legacy bridge event kept only so old Bwoink integration can compile. New Governance AHelp code
/// never emits it and never opens a Bwoink channel.
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
