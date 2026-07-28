# Yautja Clan Admin Compact UI

## Goal

Make the clan administration window substantially more compact while preserving all existing clan and player operations, selector behavior, and contextual hints.

## Layout

- Default window size is approximately 760x560 with the existing minimum size kept below the default so the window remains usable on smaller displays.
- The content is a horizontal split:
  - Left pane: clan create/edit form followed by player operations.
  - Right pane: existing clans list with independent vertical scrolling.
- The status message and refresh action stay in a single compact footer in the right pane.
- Section framing is reduced to lightweight headers and spacing; long explanatory hint blocks are removed from the visual flow.

## Controls and hints

- Form labels stay visible but use compact single-line rows.
- Tooltips remain attached to fields and actions for detailed guidance.
- Existing localization keys are reused; only layout-specific wording may be shortened if needed.
- Selector choices and their persisted selections are unchanged.

## Behavior and safety

- Create, edit, cancel, delete confirmation, membership, rank, whitelist, inspect, and refresh events retain their current callbacks and payloads.
- The clan list remains independently scrollable so a large number of clans does not expand the window.
- Editing a clan continues to populate the form without losing the draft when state updates arrive.

## Verification

- The focused `YautjaClanAdmin` tests must pass.
- The client project must build with zero errors.
- A fresh server/client startup must reach a connected in-game state without errors related to the clan admin window or its localization.
