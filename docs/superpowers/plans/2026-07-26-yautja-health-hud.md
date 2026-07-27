# Yautja Health HUD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the CMSS13 Yautja mask health HUD into CMU/RMC using the existing equipment HUD pipeline for biological and xenonid targets.

**Architecture:** Add `ShowHealthBars` and `ShowHealthIcons` to the base Yautja mask with `Biological` and `Xeno` containers. Correct the generic health-icon container filter and extract the existing xenonid health-state calculation into a reusable status-icon provider, while leaving Yautja mark/Falcon HUD code unchanged.

**Tech Stack:** C#, RobustToolbox EntitySystem events, YAML entity prototypes, NUnit client/integration tests.

## Global Constraints

- Preserve all pre-existing user changes in the dirty worktree; touch only files needed for the health HUD and its tests/docs.
- Do not add `HolocardScanner` to the Yautja mask.
- Do not change Yautja mark, clan, Falcon, visor, zoom, or cloak behavior.
- Health display is visual bars/icons, not numeric HP.
- Use the existing inventory-relayed `EquipmentHudSystem`; do not create a parallel mask-specific activation path.

---

### Task 1: Add failing regression coverage

**Files:**
- Create: `Content.Tests/Client/_RMC14/Medical/HUD/CMXenoHealthIconStateTest.cs`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaHealthHudTest.cs`

**Interfaces:**
- Tests will consume the planned `CMXenoHealthIconState.GetState(...)` helper and the `CMUYautjaMask` prototype.

- [x] Add pure calculation tests for healthy, critical, and dead xenonid health-state names, including the low-health boundary values used by `XenoHudOverlay`.
- [x] Add an integration test that indexes `CMUYautjaMask` and asserts `ShowHealthBars` and `ShowHealthIcons` exist with `Biological` and `Xeno` containers.
- [ ] Add a client-side regression test for the health-icon provider: a biological target remains eligible, an Xeno target is handled by the Xeno provider, and a non-configured container is rejected.
- [x] Run the focused tests before production changes and confirm they fail because the new prototype/helper/provider does not yet exist.

### Task 2: Implement shared xenonid health-state calculation

**Files:**
- Create: `Content.Client/_RMC14/Medical/HUD/CMXenoHealthIconState.cs`
- Modify: `Content.Client/_RMC14/Xenonids/Hud/XenoHudOverlay.cs`

**Interfaces:**
- Produces `CMXenoHealthIconState.GetState(FixedPoint2 damage, MobState state, FixedPoint2? criticalThreshold, FixedPoint2? deadThreshold)` returning the existing RSI state name or `null` when thresholds are unavailable.

- [x] Move the exact state/rounding logic from `XenoHudOverlay.UpdateHealth` into the helper.
- [x] Replace the duplicated calculation in `XenoHudOverlay` with the helper and keep its existing draw offsets and resource checks unchanged.
- [x] Run the new helper tests; existing Xeno overlay tests remain build-covered.

### Task 3: Add reusable xenonid status-icon provider and filter generic icons

**Files:**
- Create: `Content.Client/_RMC14/Medical/HUD/CMXenoHealthIconsSystem.cs`
- Modify: `Content.Client/Overlays/ShowHealthIconsSystem.cs`

**Interfaces:**
- `CMXenoHealthIconsSystem.TryGetIcon(Entity<XenoComponent> entity, out StatusIconData? icon)` returns a dynamic `StatusIconData` for the current Xeno damage state.
- `ShowHealthIconsSystem` subscribes to Xeno status-icon events, checks `DamageContainers`, and delegates Xeno rendering to the new provider.

- [x] Add the Xeno provider using `/Textures/_RMC14/Interface/xeno_hud.rsi` and the existing dynamic health states used by the Xeno HUD.
- [x] Require an active `ShowHealthIcons` HUD and a matching `DamageableComponent.DamageContainerID` before adding either biological or Xeno health icons.
- [x] Preserve the existing `CMHealthIconsSystem` path for biological `RMCHealthIconsComponent` targets.
- [x] Run the focused unit/integration tests; broader M42/medical HUD coverage was not run.

### Task 4: Connect the base mask prototype

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/devices.yml`

**Interfaces:**
- `CMUYautjaMask` gains the two networked equipment-HUD components; all child mask prototypes inherit them.

- [x] Add `ShowHealthBars` with `damageContainers: [Biological, Xeno]`.
- [x] Add `ShowHealthIcons` with `damageContainers: [Biological, Xeno]`.
- [x] Do not add `HolocardScanner` or alter the existing `YautjaMask` component.
- [x] Run the prototype assertion; existing Yautja mask equip/unequip tests were not run.

### Task 5: Verify end-to-end behavior and repository safety

**Files:**
- Modify only tests if an assertion needs to be placed beside an existing Yautja mask test.

- [ ] Verify mask equip activates both client health HUD systems and unequip deactivates them; visor toggling must not deactivate them.
- [ ] Verify human/Yautja/synthetic biological targets and Xeno targets render through the intended provider.
- [ ] Verify mark/clan/Falcon icons still render while health icons are active.
- [x] Run focused tests, affected project builds, and `git diff --check`.
- [x] Review `git status` and confirm unrelated pre-existing changes remain untouched.

Verification note: the server-side prototype assertion and pure health-state tests pass. A client-pair integration run is not included because the repository currently has an unrelated missing Hunter Ship RSI asset during client prototype loading; the client project itself builds successfully.
