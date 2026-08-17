cmd-governance-player-only = This command is only available to a connected player.
cmd-governance-status-description = Shows the active RUCM Community Governance duty session.
cmd-governance-status-help = Usage: {$command}
cmd-governance-status-inactive = No active DutySession was found for the current round.
cmd-governance-status-active = DutySession #{$session} is active for round #{$round} until {$expires}.

cmd-governance-freeze-description = Temporarily freezes a player under an active Governance incident.
cmd-governance-freeze-help = Usage: {$command} <player|UUID> <1-120 seconds> <action-id> <reason>
cmd-governance-freeze-denied = The server denied this action: {$reason}
cmd-governance-freeze-success = {$target} was frozen for {$seconds} seconds. Approved action: {$incident}.
cmd-governance-round-remove-description = Removes a player until round end under an approved Governance action.
cmd-governance-round-remove-help = Usage: {$command} <player|UUID> <action-id> <reason>
cmd-governance-round-remove-success = {$target} was removed until round end. Approved action: {$action}.

governance-duty-observer-only = Active Community Governance duty only allows participation in this round as an observer.
governance-duty-invite-title = RUCM Community Duty
governance-duty-invite-description =
    Round #{$round} needs a community responder.

    Duty is offered only to observers and grants limited temporary capabilities until the session ends. Accepting adds {$acceptReward} Civic Rating, declining removes {$declinePenalty}, and recusal does not change rating. Ignoring the invitation removes {$expiryPenalty}.

    Respond before {$expires}.
governance-duty-invite-accept = Accept (+{$reward})
governance-duty-invite-decline = Decline (-{$penalty})
governance-duty-invite-recuse = Unavailable / recuse
governance-duty-response-accepted = Duty accepted. Temporary capabilities are active. Your Civic Rating is {$rating}.
governance-duty-response-declined = You declined community duty. Your Civic Rating is {$rating}.
governance-duty-response-recused = Recusal accepted without a rating change. A replacement will be selected.
governance-duty-response-expired = The invitation expired. Your Civic Rating is {$rating}.
governance-duty-response-handled = This invitation has already been handled.
governance-duty-response-invalid = The invitation is no longer valid or Governance is unavailable.
governance-duty-response-observer-required = You must be an observer to accept community duty.
governance-jury-invite-title = RUCM Jury Invitation
governance-jury-invite-description =
    You were randomly selected as a juror candidate for case #{$case}.

    The case and evidence are available in its public Discord thread. Accepting adds {$acceptReward} Civic Rating, declining removes {$declinePenalty}, and recusal does not change rating. Ignoring the invitation removes {$expiryPenalty}.

    Respond before {$expires}. The bot will automatically continue the Discord case after your response.
governance-jury-response-accepted = You accepted jury service. Discord has received your response. Your Civic Rating is {$rating}.
governance-jury-response-declined = You declined jury service. Discord has received your response. Your Civic Rating is {$rating}.
governance-jury-response-recused = Recusal accepted without a rating change. The bot will select a replacement.
governance-jury-response-expired = The jury invitation expired. Your Civic Rating is {$rating}.
governance-jury-response-handled = This jury invitation has already been handled.
governance-jury-response-invalid = The jury invitation is no longer valid or Governance is unavailable.
governance-denial-disabled = Governance is disabled
governance-denial-invalid-input = the incident id or reason is invalid
governance-denial-not-on-duty = no active DutySession or moderation.freeze capability
governance-denial-not-observer = the responder must be an observer
governance-denial-self-target = responders cannot target themselves
governance-denial-invalid-duration = the duration is outside the allowed range
governance-denial-target-unavailable = the target is unavailable or has no attached entity
governance-denial-already-frozen = another mechanism has already frozen the target
governance-denial-action-not-approved = the action is missing, lacks quorum, or does not match the target and round
governance-denial-unknown = an unknown authorization error occurred

cmd-governance-ahelp-description = Opens the AHelp queue for an active community responder.
cmd-governance-ahelp-help = Usage: {$command}
governance-ahelp-title = RUCM AHelp Queue
governance-ahelp-description = Open tickets are visible to all responders. Claimed tickets are restricted to their assigned responder.
governance-ahelp-ticket-placeholder = AHelp number
governance-ahelp-refresh = Refresh
governance-ahelp-claim = Claim
governance-ahelp-open = Open chat
governance-ahelp-waiting = Waiting for player
governance-ahelp-resolve = Resolved
governance-ahelp-empty = There are no open AHelps or AHelps assigned to you.
governance-ahelp-status-open = open
governance-ahelp-status-mine = assigned to you
governance-ahelp-ticket-invalid = Enter a valid AHelp number.
governance-ahelp-claim-failed = This AHelp was already claimed, closed, or is unavailable during this duty session.
governance-ahelp-open-failed = You can only open an AHelp assigned to you in the current round.
governance-ahelp-status-failed = Only the assigned responder can change this AHelp state.
governance-ahelp-unavailable = The AHelp queue is temporarily unavailable. Try refreshing it later.
governance-ahelp-access-denied = The AHelp queue is only available to an active responder observer.
governance-explanation-message = [bold]Community responder {$responder} requested an explanation[/bold] (action #{$action}). Reply in this AHelp. Reason: {$reason}
cmd-governance-explanation-description = Sends an approved explanation request to a player through AHelp.
cmd-governance-explanation-help = Usage: {$command} <player|UUID> <action-id> <reason>
cmd-governance-explanation-denied = Explanation request denied: {$reason}
cmd-governance-explanation-success = An explanation request for action #{$action} was sent to {$target}.
cmd-governance-logs-description = Shows up to 100 current-round logs for a player under an approved action.
cmd-governance-logs-help = Usage: {$command} <player|UUID> <action-id>
cmd-governance-logs-denied = Log access denied: {$reason}
cmd-governance-logs-header = Logs for {$target}: {$count} entries (maximum 100).
governance-denial-ahelp-unavailable = the player's AHelp is assigned to another responder or unavailable
