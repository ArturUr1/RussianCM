# Yautja Clan Admin Roster Actions Design

## Goal

Add compact inline actions to the Yautja clan administration roster: remove a player from a clan, clear their Yautja whitelist, and show a separate list of Yautja records that currently have no clan.

## Scope

- Each member row in an expanded clan roster exposes two immediate actions:
  - remove the player from the current clan;
  - set all Yautja whitelist flags to `None`.
- A separate “players without a clan” block appears below the clan list and exposes the whitelist-clearing action.
- Actions use the player’s `NetUserId` rather than a display name.
- Every successful or failed action refreshes the EUI state and status message.
- No confirmation dialog is shown for either action.

## Data model

`YautjaClanAdminMemberState` remains the shared row DTO and is reused for both clan members and clanless players. `YautjaClanAdminEuiState` gains a `ClanlessPlayers` collection.

The server obtains all rows from `GetYautjaClanMembersAsync()` once per refresh, groups rows by `ClanId`, and maps rows with `ClanId == null` into `ClanlessPlayers`. Both clan members and clanless players are sorted by sanitized rank descending, then display name case-insensitively ascending. Display names use the active session name when online and the `NetUserId` fallback otherwise.

The list therefore represents persisted Yautja records whose clan membership is absent, including records created by the legacy migration. It does not enumerate arbitrary player accounts that have never had a Yautja record.

## Protocol and server behavior

Add two typed EUI messages:

- `YautjaClanAdminRemoveMemberMessage(NetUserId playerId)`
- `YautjaClanAdminClearWhitelistMessage(NetUserId playerId)`

For remove-member:

1. Load the persisted Yautja clan member record.
2. If it is missing or already clanless, publish a localized failure status.
3. Upsert the record with `ClanId = null`, preserving rank, permissions, honor, and legacy state.
4. Invalidate clan and rank caches for the target and publish a localized success status.

For clear-whitelist:

1. Set the target player’s Yautja whitelist flags to `YautjaWhitelistFlags.None`.
2. Invalidate clan and rank caches for the target.
3. Publish a localized success status; database errors use the existing EUI error path.

Both operations are handled under the existing EUI operation gate and are recorded in the existing admin log with the target user id/name.

## UI behavior

- Member rows keep the existing name, rank, and online/offline presentation.
- The action buttons are compact and placed at the row’s right edge; the remove action uses the existing hot-red visual treatment, while whitelist clearing uses the neutral row style.
- The clanless block uses the same row layout and has only the whitelist-clearing button.
- The existing clan list scroll container remains the scroll boundary for both sections; the roster’s bounded nested scroll remains in place.
- Buttons have localized labels and tooltips.
- State updates rebuild the rows, so removing a member moves them to the clanless block immediately and clearing a whitelist leaves the player in place.

## Error handling

- Missing target records and already-clanless members produce localized status text and do not mutate data.
- The existing server exception handler logs unexpected failures and publishes the exception message through the existing status channel.
- Client actions are fire-and-refresh; no optimistic row mutation is performed.

## Testing

- Shared state test verifies `ClanlessPlayers` serialization data.
- Server tests verify remove-member preserves row data while clearing `ClanId`, and clear-whitelist targets the requested user id through the typed message path/helper.
- Client tests verify action callbacks are exposed for roster rows, the clanless section is bounded/scrollable, and row action selection uses the correct target id.
- Focused Yautja clan-admin tests run before the full `Content.Tests` suite.
