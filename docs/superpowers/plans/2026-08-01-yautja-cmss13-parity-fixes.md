# Yautja CMSS13 Parity Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Исправить медицинское снаряжение, interactive hunter globe tactical map и thermal visor wall-vision до согласованного CMSS13-поведения.

**Architecture:** Медицинский payload и healing-gun lifecycle остаются в существующем CMU Yautja-контуре. Hunter globe использует существующий RMC tactical-map UI, но получает all-faction read-only компьютер с серверной защитой. Wall-vision активируется networked thermal-source component, который создаётся только для фактически надетых visor glasses с корректным ownership.

**Tech Stack:** C#/.NET 10, RobustToolbox ECS, YAML entity prototypes, NUnit integration/unit tests, локальный `cmss13-ref-full` как source reference, RSI/PNG assets.

## Global Constraints

- Внешний `CMUYautjaMedicomp` остаётся `Item.size: Small` как документированная адаптация для `pocket2`; его storage capacity и source payload не уменьшаются.
- Hunter globe показывает all-faction scope, соответствующий CMSS13 `MINIMAP_FLAG_ALL`, и является server-enforced read-only viewer без drawing/label updates.
- Wall-vision не является innate `YautjaComponent` ability; оно работает только при активном фактически надетом visor source.
- Wall-vision показывает только подходящие mob sprites на текущей карте; не раскрывает objects, structures, containers или storage contents.
- Не изменять общий RobustToolbox visibility/FOV renderer и не затрагивать unrelated пользовательские изменения в рабочем дереве.
- Не изменять bracer tactical-map action и не проводить полный аудит weapon/armor sprites в этом цикле.
- Каждая production-правка начинается с теста, который сначала должен упасть по ожидаемой причине.
- Коммитить только файлы текущей задачи; не добавлять `cmss13-ref-full`, `cmss13-ref`, `tmp`, `bin`, `obj`, `client-run.err` или `server-run.err`.

---

### Task 1: Source-equivalent medicomp payload and medical prototype facts

**Files:**
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaMedicompCmss13ParityTest.cs`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/devices.yml:1512-1868`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs:2695-2955,16900-17130` only where old expectations contradict source payload
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaMedicompPocketTest.cs` only if the documented `Small` assertion needs its message/source wording corrected
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/mcaste_items.yml:50-70` to keep military herb containers outside the source medicomp whitelist
- Modify: `Resources/Textures/_CMU14/Yautja/yautja_items.rsi/`, `Resources/Textures/_CMU14/Yautja/healing_gun.rsi/`, and relevant medical RSI directories for source states identified by the parity manifest
- Reference: `D:/RussianCM/cmss13-ref-full/code/modules/cm_preds/yaut_items.dm`, `code/game/objects/items/stacks/medical.dm`, `code/game/objects/items/reagent_containers/autoinjectors.dm`

**Interfaces:**
- Consumes: Existing `Stack`, `SolutionContainerManager`, `AutoInjector`, `Storage`, `FixedItemSizeStorage` and Yautja medical tags.
- Produces: Four medicomp prototypes whose source payload and storage whitelist can be asserted without relying on the unrelated smoke-test fixture; exact local sprite-state mappings for the medical items in scope.

- [ ] **Step 1: Write the failing prototype/payload tests.**

  In `YautjaMedicompCmss13ParityTest`, add separate tests for:

  ```csharp
  [Test]
  public async Task HerbalCaseUsesSourceStackAmountsAndWhitelist()
  {
      // Load the CMU prototype set, spawn the case, and assert two mending
      // and two soothing stacks with the source uses per stack.
  }

  [Test]
  public async Task CrystalVariantsUseOneThirtyUnitDose()
  {
      // Spawn ordinary and thrall variants; assert one use, 30u transfer,
      // source reagent difference, and the thrall visual marker.
  }

  [TestCase("CMUYautjaMedicompFull", "CMUYautjaYautjaAutoInjector", 3)]
  [TestCase("CMUYautjaMedicompThrall", "CMUYautjaThrallAutoInjector", 3)]
  public async Task FilledMedicompUsesThreeDiscreteHealingCapsules(
      string medicompPrototype, string capsulePrototype, int expectedCount)
  {
      // Count child entities by prototype, not Stack.count.
  }
  ```

  Also add prototype assertions for the source sprite states of stabilizer gel, healing gel, wound clamp, analyzer, crystal, herbal case and medicomp. The tests must fail against the current RMC substitute states and current stack counts.

- [ ] **Step 2: Run only the new parity fixture and verify RED.**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaMedicompCmss13ParityTest"
  ```

  Expected result: the project may compile, but at least the source amount/count/state assertions fail against the current implementation. If the testhost dies before assertions, record the exact CLR/testhost failure and use the prototype-loading helper to keep the RED test focused.

- [ ] **Step 3: Correct the medical prototypes and source asset mappings.**

  Update `devices.yml` so herbal stacks expose source uses, crystals use one 30u dose with a distinct thrall reagent/visual, and filled medicomps contain three discrete source-equivalent capsules. Remove the shared `CMUYautjaMedicompItem` tag from the military sibling case if it is not source-whitelisted. Replace the medical sprite states with the DMI-derived states and preserve already matching medicomp/globe/healing-gun frames.

- [ ] **Step 4: Run the new parity tests and the existing medicomp tests.**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaMedicompCmss13ParityTest|FullyQualifiedName~YautjaMedicompPocketTest|FullyQualifiedName~YautjaMedicompPayloadItemsMatchCmss13SourceFacts"
  ```

  Expected result: all source payload/state checks pass. Update only assertions that were explicitly proving the old non-parity stack adaptation; do not weaken unrelated Yautja bundle tests.

- [ ] **Step 5: Run `git diff --check`, inspect asset paths, and commit Task 1.**

  Stage only the Task 1 test, prototype, asset and focused legacy-test hunks. Commit with:

  ```powershell
  git commit -m "fix: align Yautja medicomp payload with CMSS13"
  ```

### Task 2: Finite healing-gun loaded/empty/reload behavior

**Files:**
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaHealingGunParityTest.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaComponents.cs` at the existing healing-gun component definition
- Modify: `Content.Server/_CMU14/Yautja/YautjaHealingGunSystem.cs`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/devices.yml` at healing-gun and capsule prototypes
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs` only for expectations that currently assert an infinite gun
- Reference: `D:/RussianCM/cmss13-ref-full/code/game/objects/items/tools/surgery_tools.dm:422-460`, `code/modules/surgery/mcomp_tendwounds.dm:90-120`

**Interfaces:**
- Consumes: Existing `YautjaHealingGunComponent`, surgery/use events, storage/capsule entities and sprite appearance states.
- Produces: A gun state that starts loaded when it has a capsule, consumes exactly one capsule per successful healing action, enters empty state, and returns to loaded only after a valid reload.

- [ ] **Step 1: Add failing runtime tests for the state machine.**

  `YautjaHealingGunParityTest` must cover these real transitions:

  ```csharp
  [Test]
  public async Task HealingGunConsumesOneCapsuleAndBecomesEmpty()
  {
      // Spawn gun, one capsule and an injured target; use the gun and assert
      // one capsule is consumed plus loaded -> empty appearance/state.
  }

  [Test]
  public async Task EmptyHealingGunRejectsUseUntilReloaded()
  {
      // Assert no second heal while empty and a valid capsule reload restores
      // loaded state and allows the next use.
  }
  ```

- [ ] **Step 2: Run the focused tests and verify RED.**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaHealingGunParityTest"
  ```

  Expected result: current infinite-gun behavior fails the capsule-consumption or empty-state assertion.

- [ ] **Step 3: Implement the minimal source-equivalent state transition.**

  Add an explicit loaded/empty field or component state with appearance updates in `YautjaHealingGunSystem`. Validate capsule presence before healing, atomically consume one capsule after successful treatment, reject empty use, and reload only through the source-equivalent capsule path. Preserve existing cooldown, authorization and wound-treatment rules unless the CMSS13 source facts require the transition.

- [ ] **Step 4: Run focused tests and existing Yautja healing tests.**

  Run the parity fixture plus the existing tests that cover `YautjaHealingGunSystem`. Expected result: loaded/empty/reload tests pass without changing non-Yautja healing behavior.

- [ ] **Step 5: Inspect the diff and commit Task 2.**

  ```powershell
  git diff --check
  git commit -m "fix: make Yautja healing gun finite"
  ```

### Task 3: Interactive all-faction read-only hunter globe

**Files:**
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaHunterGlobeTacticalMapTest.cs`
- Modify: `Content.Shared/_RMC14/TacticalMap/Components/TacticalMapComputerComponent.cs` to carry the replicated read-only flag
- Modify: `Content.Server/_RMC14/TacticalMap/TacticalMapSystem.cs` in computer UI-open/update-canvas/label handlers
- Modify: `Content.Client/_RMC14/TacticalMap/TacticalMapComputerBui.cs` to hide drawing controls when the state is read-only
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml:3283-3300`
- Modify: `Resources/Prototypes/_CMU14/Maps/huntership_visuals.yml` only if generated globe wrapper prototypes override components
- Modify: `Resources/Maps/_CMU14/huntership.yml`, `huntership_upper.yml`, and `huntership_lower.yml` only if a globe room lacks the powered-console/APC setup needed by the inherited base prototype
- Reference: `Content.Server/_RMC14/TacticalMap/TacticalMapSystem.cs`, `Content.Shared/_RMC14/TacticalMap/SharedTacticalMapSystem.cs`, `Resources/Prototypes/_RMC14/Entities/Structures/Machines/rmc_communications_console.yml:61-95`, `D:/RussianCM/cmss13-ref-full/code/modules/cm_preds/yaut_machines.dm`

**Interfaces:**
- Consumes: Existing `ActivatableUI`, `UserInterface`, `TacticalMapComputerBui`, `TacticalMapComputerComponent`, `TacticalMapSystem` and APC/RMC power components.
- Produces: A globe that opens the existing tactical-map UI, reads all faction buckets, rejects drawing/label mutations server-side, and closes when adjacency/power is lost.

- [ ] **Step 1: Add failing globe prototype and interaction tests.**

  Assert all of the following for `CMUYautjaStructureYautjaMachinesGlobe`:

  ```csharp
  Assert.That(entMan.HasComponent<ActivatableUIComponent>(globe), Is.True);
  Assert.That(entMan.HasComponent<UserInterfaceComponent>(globe), Is.True);
  Assert.That(entMan.HasComponent<TacticalMapComputerComponent>(globe), Is.True);
  Assert.That(entMan.HasComponent<TacticalMapTrackedComponent>(globe), Is.False);
  Assert.That(entMan.HasComponent<TacticalMapAlwaysVisibleComponent>(globe), Is.False);
  Assert.That(entMan.GetComponent<TacticalMapComputerComponent>(globe).ReadOnly, Is.True);
  ```

  Add a runtime test that opens the UI from an adjacent user and confirms the state includes Marine, Xeno, XenoStructure, Opfor, Govfor, CLF and Yautja data. Add a message test that attempts canvas and label updates and asserts the authoritative map remains unchanged.

- [ ] **Step 2: Run the globe tests and verify RED.**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaHunterGlobeTacticalMapTest"
  ```

  Expected result: current globe lacks UI/computer and still has tracking/icon/always-visible components.

- [ ] **Step 3: Add the powered all-faction read-only computer prototype.**

  Add the same `UserInterface` binding used by RMC tactical-map consoles, `ActivatableUI`, `TacticalMapComputer` with null/all-faction scope, `ApcPowerReceiver` with `needsPower: true`, a non-zero console load, and `RMCPowerReceiver` on the equipment channel. Mark the globe unbreakable and remove its standalone tactical blip components. Confirm generated wrapper prototypes inherit the computer and power components.

- [ ] **Step 4: Enforce read-only behavior at server and UI layers.**

  Add `ReadOnly` to `TacticalMapComputerComponent` and make `TacticalMapSystem` reject `TacticalMapUpdateCanvasMsg` and all label mutation messages for read-only computers before changing authoritative or user state. Include the flag in the BUI state and make `TacticalMapComputerBui` omit drawing controls. Do not rely on hidden buttons as the only guard.

- [ ] **Step 5: Run globe tests plus existing tactical-map tests.**

  Run the new fixture, `YautjaMachineSourceParityTest`, `YautjaBracerTacticalMapTest`, and the shared tactical-map tests. Confirm the personal bracer map remains unchanged and the globe itself is not a blip.

- [ ] **Step 6: Review generated map wrappers and commit Task 3.**

  Run `git diff --check`, inspect all seven globe placements and powered-room assumptions, then commit:

  ```powershell
  git commit -m "feat: add read-only tactical map to Yautja hunter globe"
  ```

### Task 4: Thermal visor ownership and gated wall-vision

**Files:**
- Create or modify: `Content.Shared/_CMU14/Yautja/YautjaWallVisionTargeting.cs`
- Create or modify: `Content.Client/_CMU14/Yautja/YautjaWallVisionSystem.cs`
- Create or modify: `Content.Client/_CMU14/Yautja/YautjaWallVisionOverlay.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaComponents.cs` to add the networked thermal-source component with visor source ownership
- Modify: `Content.Shared/_CMU14/Yautja/YautjaMaskSystem.cs:164-420`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/devices.yml:489-530`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaThermalVisorLifecycleTest.cs`
- Modify: `Content.Tests/Shared/_CMU14/Yautja/YautjaWallVisionTargetingTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaWallVisionPrototypeTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaYoungbloodTest.cs` only where old assertions require the new active-source contract

**Interfaces:**
- Consumes: `YautjaMaskSystem`, `NightVisionItemComponent`, `NightVisionComponent`, inventory eyes slot, `YautjaMaskVisorGlassesComponent`, local-player attach/detach events and existing sprite overlay APIs.
- Produces: `YautjaThermalVisionComponent` (networked) with source ownership; a pure target predicate that accepts active-source and real viewer-map state; a client overlay that runs only for active local thermal wearers.

- [ ] **Step 1: Replace the old pure-target tests with failing active-source tests.**

  Extend the target predicate contract so inactive source, wrong wearer, missing eyes slot and map mismatch return false while same-map visible mobs outside containers return true. Preserve self/non-mob/hidden/container exclusions.

  Add integration cases for regular mask, powered helmet, thrall/tech-authorized wearer, low-power cleanup, unequip, failed equip and two simultaneous sources. The old test named `YautjaWallVisionIsSeparateFromTheNightVisionVisor` must be replaced because it encodes the rejected innate behavior.

- [ ] **Step 2: Run unit and visor integration tests and verify RED.**

  Run:

  ```powershell
  dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaWallVisionTargetingTest"
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaThermalVisorLifecycleTest|FullyQualifiedName~YautjaWallVisionPrototypeTest"
  ```

  Expected result: the current overlay still activates for `YautjaComponent` without visor and the new active-source assertions fail.

- [ ] **Step 3: Add visor source ownership and atomic apply/remove lifecycle.**

  Give `YautjaMaskComponent` a reference to its created glasses. Make `CreateVisorGlasses` return success and set `VisorEnabled`/thermal-source only after `TryEquip` succeeds. Make deletion match the owning source instead of deleting any `YautjaMaskVisorGlassesComponent`. On glasses equip/unequip, low-power shutdown, mask removal and helmet unequip, synchronize the wearer’s networked thermal-source component.

- [ ] **Step 4: Gate and harden the client overlay.**

  Subscribe `YautjaWallVisionSystem` to the networked thermal-source component and local player attach/detach, not to innate `YautjaComponent`. In `YautjaWallVisionOverlay`, stop before spatial lookup unless the source is active, compare the real viewer transform map to `args.MapId`, keep the existing target filters, and set an explicit render order. Do not modify RobustToolbox rendering or server line-of-sight rules.

- [ ] **Step 5: Run the full visor-focused test set.**

  Run the new lifecycle fixture, the updated targeting/prototype tests, and existing `YautjaYoungbloodTest` cases for visor power, unequip, zoom and powered helmet. Confirm ordinary low-light behavior still works while wall-vision is off when the visor is off.

- [ ] **Step 6: Inspect the ownership/lifecycle diff and commit Task 4.**

  ```powershell
  git diff --check
  git commit -m "feat: gate Yautja wall vision behind thermal visor"
  ```

### Task 5: Cross-contour asset parity and final verification

**Files:**
- Create: `Tools/_CMU14/YautjaParity/verify_sprite_parity.py` or an equivalent existing-tool extension if the repository already has a DMI/RSI converter
- Create: `Tools/_CMU14/YautjaParity/source_sprite_manifest.json`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaScopedSpriteParityTest.cs` for state/path/frame metadata that can run without bundling the reference repository
- Modify: only the asset/prototype files listed by the manifest
- Reference: `D:/RussianCM/cmss13-ref-full/icons/obj/items/hunter/pred_gear.dmi`, `pred_mask.dmi`, `yautja_machines.dmi`, `medical.dmi`, and corresponding mob DMI files

**Interfaces:**
- Consumes: Source DMI path, source state name, local RSI path/state, expected frame count/delay/pixel hash and prototype item mapping.
- Produces: A deterministic local audit command and a committed manifest/test that detects future state/path/frame regressions without importing the reference repository into the game build.

- [ ] **Step 1: Add a failing manifest/state test.**

  Create the manifest entries for all medical/visor/globe/mask states in scope and make `YautjaScopedSpriteParityTest` assert local RSI metadata and prototype state references. Include the three known mask mismatches as expected failures before asset correction.

- [ ] **Step 2: Run the parity test and verify RED.**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaScopedSpriteParityTest"
  ```

  Expected result: the known incorrect medical states, visor frame and three mask states fail with explicit source/local identifiers.

- [ ] **Step 3: Correct only the failing assets and metadata.**

  Extract/copy source pixels into the existing local RSI layout using the repository’s available DMI/RSI tooling. Preserve the local naming convention only where it does not alter pixels. Do not regenerate unrelated masks or overwrite user-modified texture directories.

- [ ] **Step 4: Run the parity tool and tests.**

  Run the tool with the explicit reference path:

  ```powershell
  python Tools/_CMU14/YautjaParity/verify_sprite_parity.py --source D:/RussianCM/cmss13-ref-full --content D:/RussianCM
  ```

  Then rerun the focused integration test. Expected output: every scoped state passes dimensions, frames, delays and pixel hashes.

- [ ] **Step 5: Run final verification without claiming around infrastructure failures.**

  Run:

  ```powershell
  git diff --check
  dotnet test Content.Tests/Content.Tests.csproj --no-restore
  dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore
  git status --short
  ```

  Run focused integration tests again. If the previously observed testhost CLR crash/hang occurs before assertions, preserve the full output and report it as an infrastructure blocker; do not mark the affected tests green.

- [ ] **Step 6: Review the complete task diff and commit the parity tooling/tests.**

  Confirm staged paths contain no unrelated user changes, then commit:

  ```powershell
  git commit -m "test: verify scoped Yautja sprite parity"
  ```

## Completion checklist

- [ ] Tasks 1–5 have focused RED → GREEN test evidence.
- [ ] Medicomp source payload passes with only the documented outer-size adaptation.
- [ ] Healing gun passes loaded/empty/reload tests.
- [ ] Hunter globe opens all-faction read-only tactical map and is not itself a blip.
- [ ] Wall-vision is impossible without an active owned visor source and works for valid mask/helmet wearers on the same map.
- [ ] Scoped medical, visor, globe and mask assets pass metadata/pixel parity.
- [ ] Unit/build/integration results are recorded; any pre-assertion CLR testhost failure is explicitly reported.
- [ ] Final diff contains only task files and preserves the dirty user worktree outside them.
