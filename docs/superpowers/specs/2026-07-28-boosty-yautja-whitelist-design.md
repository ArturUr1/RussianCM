# Boosty Yautja Whitelist Design

## Goal

Allow players with Boosty sponsor priorities 1 through 4 to use the ordinary whitelisted Yautja hunter role, `CMUYautjaHunter`, while keeping priorities 5 through 7 ineligible and preserving existing manually granted job whitelists.

## Scope

- Applies only to `CMUYautjaHunter`.
- Applies to a connected player's current Boosty tier priority.
- Priority 1, 2, 3, and 4 grant access.
- Priority 5, 6, 7, missing tier, and an unavailable patron status do not grant access.
- `CMUYautjaYoungblood` and `CMUYautjaBadBlood` are unchanged.
- Existing database-backed job whitelists and mentor/admin exceptions remain unchanged.

## Design

Add one shared, side-effect-free policy helper that answers whether a job and optional Boosty priority qualify for this exception. The helper owns the role id and inclusive priority bounds so the client and server cannot drift apart.

The server `JobWhitelistManager` will inject the existing `LinkAccountManager` and treat the policy result as an additional successful whitelist source. The check will run before the existing manual whitelist and parent-role checks, so the normal whitelist behavior remains intact for every other job.

The client `JobRequirementsManager` will inject the client LinkAccountManager and apply the same policy while calculating local whitelist state. It will subscribe to Boosty status updates so the Yautja entry in the lobby and profile editor refresh when the connected player's patron tier is refreshed. The server will still be authoritative for every join and late-join request.

No database migration, role prototype change, or automatic insertion into `RoleWhitelist` is needed. This avoids stale persisted whitelist rows when a patron's tier changes and keeps manual grants distinguishable from sponsor access.

## Data flow

1. Link account loading or `rmcboosty grant/reload` populates the server `LinkAccountManager` cache.
2. A server job access check asks the shared policy whether the cached priority permits `CMUYautjaHunter`.
3. The link account status message updates the client LinkAccountManager.
4. The client job requirements manager raises its existing `Updated` event and applies the same policy for lobby/profile UI.
5. The server rechecks the role when the player actually joins or late-joins.

If the patron cache is absent or the tier is absent, the policy returns false and the existing manual whitelist path remains available.

## Tests

- Shared unit tests cover the allowed priorities 1 through 4, rejected priorities 5 through 7, missing priority, the wrong job id, and the exact inclusive boundaries.
- An integration-level server test exercises the whitelist manager with a connected patron at an allowed and a rejected priority, proving the Boosty result reaches the authoritative role check.
- Existing client/server build and test commands are run after implementation.
- The server and client are started from the built outputs with bounded logs, and logs are checked for startup failure or errors attributable to the new code.

## Alternatives considered

1. Synchronize Boosty access into the database `RoleWhitelist` table. Rejected because tier revocation would require reliable cleanup and could leave stale permanent-looking grants.
2. Make `CMUYautjaHunter` non-whitelisted and add a custom job requirement. Rejected because it would change the semantics and tooling of the existing WL role, including admin whitelist commands.
3. Add the exception only on the server. Rejected because the lobby/profile editor would remain stale and hide a role that the server accepts.
