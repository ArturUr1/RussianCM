# Yautja Clan Administration UI/UX Design

## Goal

Make the Yautja clan administration window easier to scan and safer to use while preserving all existing clan and player-management actions. Add contextual hints so an administrator can understand expected input formats and destructive consequences without consulting external documentation.

## Scope

The scope is the client-side `YautjaClanAdminWindow` opened from the Administration tab. Server messages, permissions, persistence, and command names remain unchanged. The existing clan information window is not redesigned in this pass, except that its rank selector must retain the selected value consistently with the admin window.

## Recommended layout

Use a single responsive vertical work panel with three visually distinct sections, in the order an administrator normally works:

1. **Clan editor**
   - A section header identifies whether the form is creating a clan or editing one.
   - Name, description, and color fields are grouped together.
   - The primary create/save button is adjacent to the cancel button; cancel is hidden outside edit mode.
   - The color field has a tooltip explaining `#RRGGBB` and showing an example.

2. **Player operations**
   - Player identity and clan id inputs are grouped in a compact identity row.
   - Membership assignment is a dedicated action row: clan id, membership rank, and one action button.
   - Rank, whitelist, inspect, and refresh-related actions are separated from the identity inputs so destructive or state-changing actions are not visually mixed with data entry.
   - Selectors expose tooltips explaining what the chosen rank or whitelist changes.

3. **Existing clans**
   - A short section hint explains that edit loads a clan into the editor and delete detaches its members after confirmation.
   - Clan entries use a stable row/card layout with the name as the primary text and id, member count, honor, and color as secondary metadata.
   - Edit is the neutral action; delete is visually and textually destructive and carries a tooltip describing the confirmation consequence.
   - The list remains independently scrollable so the editor and player actions remain accessible when many clans exist.

The status and inspection messages remain at the bottom of their relevant section or in a shared status area, with empty states phrased as guidance rather than as an unexplained blank.

## Hints and copy

Add localized tooltips in both Russian and English for:

- clan name: required, concise display name;
- clan description: required, shown to clan members;
- clan color: hexadecimal `#RRGGBB`, with an example;
- player: accepted player name or UserId;
- clan id: existing numeric id or `none` to detach membership;
- membership rank selector: rank assigned when membership is created or changed;
- rank selector: persistent rank applied to the inspected player;
- whitelist selector: access group changed for the inspected player;
- inspect: refreshes the diagnostic summary for the entered player;
- edit: loads the selected clan into the editor;
- delete: opens confirmation and detaches all members if accepted;
- refresh: reloads the current server-side clan state.

Keep visible button labels short. Put explanatory text in tooltips and section hints rather than expanding every row with prose. All strings must be localized; no user-facing English literals are introduced in the Russian UI.

## Interaction and safety

- Preserve the current draft while server state refreshes.
- When editing, show the edit header and save action; cancel restores create mode without sending a mutation.
- Keep delete behind the existing confirmation dialog and make the dialog text explicit about member detachment.
- Ensure every `OptionButton` updates its selected id when the user chooses an item, so action buttons always submit the visible choice.
- Keep action controls enabled/disabled according to existing server-side validation; the UI should not invent new permission rules.

## Visual treatment

Reuse the existing Yautja UI style helpers (`YautjaBracerUiStyle.Section`, muted labels, accent borders, and compact action buttons) instead of introducing a new theme. Use consistent spacing, minimum widths for labels/selectors, and a clear accent difference between neutral, inspect/refresh, and destructive actions. The window should remain usable at its current minimum size and continue to support vertical scrolling.

## Verification

- Add or update client tests covering selector state persistence and the editor mode transition.
- Build `Content.Client` and run the focused Yautja clan admin tests.
- Manually smoke-test create, edit, cancel, inspect, rank/whitelist selection, refresh, and delete-confirmation flows with the client connected to the local server.

## Acceptance criteria

- An administrator can identify the three workflows without reading the source or guessing which button belongs to which input.
- Every field and action has a useful localized hint available on hover.
- The clan list remains usable with many entries and does not push the action sections off-screen permanently.
- Selector choices submitted by action buttons match the visible choice.
- Existing clan edit/delete and player-operation behavior remains intact.
