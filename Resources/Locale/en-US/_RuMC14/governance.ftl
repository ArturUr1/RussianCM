cmd-governance-player-only = This command is only available to a connected player.
cmd-governance-status-description = Shows the active RUCM Community Governance duty session.
cmd-governance-status-help = Usage: {$command}
cmd-governance-status-inactive = No active DutySession was found for the current round.
cmd-governance-status-active = DutySession #{$session} is active for round #{$round} until {$expires}.

cmd-governance-freeze-description = Temporarily freezes a player under an active Governance incident.
cmd-governance-freeze-help = Usage: {$command} <player|UUID> <1-120 seconds> <incident-id> <reason>
cmd-governance-freeze-denied = The server denied this action: {$reason}
cmd-governance-freeze-success = {$target} was frozen for {$seconds} seconds. Incident: {$incident}.

governance-duty-observer-only = Active Community Governance duty only allows participation in this round as an observer.
governance-denial-disabled = Governance is disabled
governance-denial-invalid-input = the incident id or reason is invalid
governance-denial-not-on-duty = no active DutySession or moderation.freeze capability
governance-denial-not-observer = the responder must be an observer
governance-denial-self-target = responders cannot target themselves
governance-denial-invalid-duration = the duration is outside the allowed range
governance-denial-target-unavailable = the target is unavailable or has no attached entity
governance-denial-already-frozen = another mechanism has already frozen the target
governance-denial-unknown = an unknown authorization error occurred
