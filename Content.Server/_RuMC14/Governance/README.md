# RUCM Community Governance: game boundary

This module is the server-side trust boundary between RussianCM and the Community Governance bot.
The bot may create duty sessions and capability grants, but the game never accepts permissions,
scope, expiry, or target state from a client.

## Deployment

1. Configure RussianCM to use the same PostgreSQL database as the Governance bot.
2. Apply the bot-owned `governance` schema before enabling this module. The required contract is:
   `users`, `duty_sessions`, `capability_grants`, and append-only `audit_events`.
3. Enable the module in the server configuration:

   ```toml
   [governance]
   enabled = true
   freeze_max_seconds = 120
   ```

SQLite deliberately fails closed: no duty session or capability can be authorized.

## In-game commands

- `governance_status` refreshes and displays the caller's current duty session.
- `governance_freeze <player|UUID> <seconds> <incident-id> <reason>` requires an active
  `moderation.freeze` grant scoped to the current round. The caller must be an observer,
  cannot target themselves, and cannot freeze for longer than 120 seconds.

Every successful or denied freeze attempt is appended to `governance.audit_events` when the
database is available. Long-term punishments remain outside this responder surface and are
executed by the Governance verdict pipeline.
