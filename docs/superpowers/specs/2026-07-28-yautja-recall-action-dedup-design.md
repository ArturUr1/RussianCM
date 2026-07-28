# Yautja recall action deduplication

## Goal

Leave one visible Yautja smart-disc recall action: the existing `CMUActionYautjaRecall`, which already has Russian localization. The unlocalized `CMUActionYautjaCallDisc` must no longer be granted by a worn Yautja bracer.

## Current behavior

`YautjaPowerSystem` grants both `RecallAction` and `CallDiscAction` when a Yautja bracer is worn. Both actions use the same smart-disc icon and appear to the player as duplicate disc-recall controls. `CMUActionYautjaRecall` is localized in `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`; `CMUActionYautjaCallDisc` has no Russian localization.

The recall action already supports smart discs because the smart-disc prototype carries `YautjaRecallableComponent`. Its existing system also handles recall power cost, ownership, range, pickup, and feedback.

## Design

- Remove only the `CallDiscAction` grant from the worn-bracer action list.
- Keep `RecallAction` as the sole visible disc-recall action.
- Preserve the `CMUActionYautjaCallDisc` prototype, event, component fields, and server handler for compatibility with existing serialized data or future migration. They become unreachable from the standard worn-bracer action grant path.
- Do not change the bracer menu, smart-disc behavior, recall ownership/range rules, or other bracer actions.

## Testing

Add or update an integration/static action-roster assertion for a worn Yautja bracer:

- `CMUActionYautjaRecall` is present;
- `CMUActionYautjaCallDisc` is absent;
- the Russian localization key for `CMUActionYautjaRecall` remains present.

Run the focused test or, if the shared integration-test output is locked by another worktree, run the available source/static checks and report that external limitation explicitly.
