# Yautja Bracer Action Cleanup and Linked Tracker Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove worn-bracer action-bar entries whose functionality is already exposed by the Yautja bracer window, keep the bracer-menu opener and independent actions, stop granting the standalone mark-panel action, and make the tracker show only explicitly linked Yautja gear as in CMSS13.

**Architecture:** Keep the existing bracer UI command handlers as the single entry point for panel-backed functions. Narrow the worn-bracer action provider in `YautjaPowerSystem`; leave the held-bracer branch and non-panel actions intact. Define `tracked` exclusively by `YautjaTrackedItemComponent` in both tracker readout construction and add/remove tracking validation, matching CMSS13's explicit tracked-item global list rather than treating every `YautjaTechItemComponent` as linked.

**Tech Stack:** C#, RobustToolbox EntitySystem events, YAML entity prototypes, NUnit integration tests, local CMSS13 reference under `cmss13-ref-full/`.

## Global Constraints

- Preserve all pre-existing user changes in the dirty worktree; do not reset, clean, or rewrite unrelated files.
- Do not remove action prototypes or shared action event types merely because they are no longer granted automatically. Existing tests and direct event-driven code still use several of them.
- The worn bracer must keep `CMUActionYautjaOpenBracerMenu`.
- The worn bracer must keep independent action-bar functions that have no matching bracer-panel button, including cloak, recall, disc, notification/name toggles, add/remove tracked item, healing capsule, and explosion-type selection.
- The held active-bracer branch remains unchanged: held bracer actions are a separate interaction path because the bracer window is opened from the worn item.
- Dead Yautja bio-signature handling, anchored-item filtering, map/area bucketing, UI commands, and explicit link/unlink component mutations must remain unchanged.
- CMSS13 behavior to preserve: `cmss13-ref-full/code/datums/elements/yautja_tracked_item.dm` adds explicitly attached tracked items to the tracked gear set, while `code/modules/cm_preds/yaut_items.dm` only reports items from that set as loose/missing gear.

---

## Task 1: Add failing regression coverage first (TDD RED)

**Files:**

- Modify `Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs`.
- Modify `Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs`.

### 1.1 Worn bracer action-bar regression

- Add or consolidate a focused integration test that equips `CMUYautjaBracer` on a Yautja and raises `GetItemActionsEvent` for the worn item.
- Assert `CMUActionYautjaOpenBracerMenu` is still present.
- Assert the following panel-backed IDs are absent from the worn action list:

  - `CMUActionYautjaOpenMarkPanel`
  - `CMUActionYautjaSelfDestruct`
  - `CMUActionYautjaTranslator`
  - `CMUActionYautjaToggleBracerIdChip`
  - `CMUActionYautjaLinkThrallBracer`
  - `CMUActionYautjaTransmitThrallMessage`
  - `CMUActionYautjaTrackGear`
  - `CMUActionYautjaCreateStabilisingCrystal`
  - `CMUActionYautjaCreateHumanStabilisingCrystal`
  - `CMUActionYautjaCreateHuntingTrap`

- Assert that independent actions which are still intentionally action-bar driven remain available where the existing implementation grants them, especially `CMUActionYautjaCreateHealingCapsule`, `CMUActionYautjaAddTrackedItem`, and `CMUActionYautjaRemoveTrackedItem`.
- Update the existing ID-chip and thrall-link grant tests so worn bracers assert absence while held active bracers still assert presence. Keep their direct event behavior tests unchanged.
- Update the existing injector/crystal action grant expectation so the worn action list no longer expects `CMUActionYautjaCreateStabilisingCrystal`; the bracer panel command remains the supported path.
- Update the existing soldier-bracer action roster expectation in `YautjaBowTest.cs` to retain only the non-panel action entries that the implementation still grants (wrist blades from the gear container and healing capsule from the bracer), rather than the panel-backed crystal/translator/self-destruct entries.
- Extend the existing predator action-bar coverage to assert that `CMUActionYautjaOpenMarkPanel` is not granted as a standalone innate action; keep leap, mark-for-hunt, butcher, and audio actions covered.

### 1.2 Explicitly linked tracker regression

- In the tracker integration coverage, spawn a `CMUYautjaCombistick` on a non-null map position without adding `YautjaTrackedItemComponent`.
- Keep at least one ordinary item with `YautjaTrackedItemComponent` so the test proves both sides of the rule: explicitly linked gear is included, an unlinked Yautja weapon is excluded.
- Invoke the existing tracker action/UI-state build path and assert:

  - the linked item contributes to the gear count and readout;
  - the unlinked combistick contributes neither a gear bucket nor a `TrackedGear` entry;
  - dead Yautja counts and existing anchored-item assertions are unchanged.

### 1.3 Add/remove link regression for Yautja tech

- Extend `BracerAddAndRemoveTrackedItemMatchCmss13ActiveHandRule` to use `CMUYautjaCombistick` (or add a second row using it) in the active hand.
- Assert add creates `YautjaTrackedItemComponent` even though the weapon has `YautjaTechItemComponent`.
- Assert remove deletes the explicit tracking component and preserves the existing active-hand and popup behavior.

### 1.4 Run RED checks

Run the focused integration tests before changing production code:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaBowTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest"
```

The new/updated expectations must fail against the current implementation: duplicated worn actions are still granted and an unlinked Yautja tech weapon is still considered tracked.

---

## Task 2: Remove panel duplicates from action grants

**Files:**

- Modify `Content.Shared/_CMU14/Yautja/YautjaPowerSystem.cs`.
- Modify `Content.Server/_CMU14/Yautja/YautjaAbilitySystem.cs`.
- Modify `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/mcaste_items.yml` only if the soldier-bracer whitelist still names actions that are no longer granted.

### 2.1 Narrow worn-bracer action grants

In `YautjaPowerSystem.OnGetItemActions`, leave the early held-item branch exactly as it is. In the worn-item branch:

- Keep `OpenBracerMenu`, cloak, recall, and disc.
- Remove the worn grants for self-destruct and translator because those functions are handled by `ToggleSelfDestruct` and `OpenTranslator` in the bracer window.
- Remove the worn grants for ID-chip toggle, thrall-bracer linking, thrall transmission, tracker refresh, and stabilising crystal because they map directly to bracer-panel commands.
- Do not add the currently unused human-crystal or hunting-trap action IDs to the action list; their existing bracer-panel commands remain the only path.
- Keep notification/name toggles, add/remove tracked-item actions, healing capsule, and explosion-type selection because they are not represented by the current bracer window controls.

The resulting worn branch must still use `AddAction` and the existing `ActionWhitelist` mechanism for actions that remain; do not duplicate whitelist logic in a new policy abstraction.

### 2.2 Remove the standalone mark-panel grant

In `YautjaAbilitySystem`:

- Remove the `YautjaOpenMarkPanelActionEvent` subscription and its handler, since the bracer mark button already calls `YautjaMarkSystem.TryOpenMarkPanel` through `YautjaBracerMenuSystem`.
- Remove `OpenMarkPanelAction` from `GrantActions` and `RemoveActions`.
- Keep the shared event/prototype/component fields needed by the bracer-side compatibility path unless the compiler proves they are truly unused; this task is about visible action grants, not broad prototype deletion.

If the soldier-bracer `YautjaBracer.actionWhitelist` still lists `CreateStabilisingCrystal`, `Translator`, or `SelfDestruct`, remove only those stale panel-backed entries while retaining `CreateHealingCapsule`.

---

## Task 3: Make tracker membership explicit

**Files:**

- Modify `Content.Server/_CMU14/Yautja/YautjaBracerMenuSystem.cs`.
- Modify `Content.Server/_CMU14/Yautja/YautjaBracerUtilitySystem.cs`.

### 3.1 Fix the readout predicate

Replace the current fallback predicate in `YautjaBracerMenuSystem.IsTrackedItem`:

```csharp
return HasComp<YautjaTechItemComponent>(uid) &&
       !HasComp<YautjaUntrackedItemComponent>(uid);
```

with an explicit-link check:

```csharp
return HasComp<YautjaTrackedItemComponent>(uid);
```

The existing `YautjaTrackedItemComponent` check may be collapsed into this single return. Do not change the surrounding item, anchored, map, or hide filters.

### 3.2 Align add/remove validation

Apply the same explicit-only rule in `YautjaBracerUtilitySystem.IsTrackedItem` so action semantics agree with the tracker:

- an unlinked Yautja tech item can be added to the tracker;
- a linked item can be removed;
- `YautjaUntrackedItemComponent` may continue to be written on removal for compatibility, but it must not make an unlinked item appear in the tracker.

Keep the active-hand rule, `YautjaTrackedItemComponent` add/remove mutations, popup text, and bracer ownership checks unchanged.

---

## Task 4: Verify the green implementation and regressions

### 4.1 Focused tests

Run the same Yautja integration filter after implementing the production changes:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaBowTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest"
```

Confirm that the action-list tests pass with panel duplicates absent, held-bracer actions preserved, direct panel commands still work, and linked/unlinked tracker behavior matches the new assertions.

### 4.2 Build and broader tests

Run the relevant project build and test suites:

```powershell
dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj
dotnet test Content.Tests/Content.Tests.csproj
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~Content.IntegrationTests._CMU14"
```

If the repository's generated `bin/` test workflow is required by the local toolchain, rerun the equivalent DLL command used by CI after the build:

```powershell
dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter "FullyQualifiedName~Content.IntegrationTests._CMU14" -- NUnit.ConsoleOut=0
```

### 4.3 Final review

- Run `git diff --check`.
- Inspect `git diff --stat` and the complete diff for only the planned files plus intentional test expectation changes.
- Verify `git status --short` still shows the user's pre-existing modifications and that none were overwritten.
- Confirm no new action grant was introduced for a bracer-panel command and no generic `YautjaTechItemComponent` fallback remains in either tracker predicate.
