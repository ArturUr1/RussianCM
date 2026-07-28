# Yautja Clan Admin Roster and Scrolling

## Goal

Let administrators expand an existing clan entry to inspect its roster and each member's Yautja rank, while keeping the compact two-pane window usable when the clan list or a roster is long.

## Data contract

- Extend `YautjaClanAdminClanState` with a serialized member list.
- Each member entry contains the stable player user id, the display name resolved by the server, the stored/sanitized `YautjaRank`, and whether the player currently has an active session.
- The server loads members for each active clan during the existing admin snapshot refresh and orders them by rank descending, then display name ascending.
- Existing clan mutation callbacks and state versioning remain unchanged.

## Interaction and layout

- Each clan card keeps its current summary, edit, and delete actions.
- Add a compact `Состав`/`Roster` toggle to the card. The toggle expands an inline roster below the summary.
- Only one roster is expanded at a time; expanding another clan collapses the previous one.
- Expanded rows show player name, localized rank name, and a small online/offline indicator.
- A clan with no members shows a localized empty-roster message.

## Scrolling

- The existing clan-list `ScrollContainer` remains the main vertical scroll area for the right pane.
- The expanded roster uses a bounded nested `ScrollContainer` so a large roster cannot push the footer out of view.
- The whole window remains resizable; controls continue to fit at the compact default size.

## Localization and verification

- Add RU and EN strings for the roster toggle, member row, empty roster, and online/offline labels.
- Preserve existing tooltip behavior for edit/delete/refresh controls and add a tooltip to the roster toggle.
- Add serialization/state tests for member data, client tests for rank rendering/expansion contract, run focused tests, build the client/server, and smoke-test startup.
