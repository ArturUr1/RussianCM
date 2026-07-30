# Yautja Original Spawn Loadout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Hunter, Youngblood, and Bad Blood player spawns match original CMSS13 by starting with only the appropriate bracer and communicator, while leaving full vendor and event gear intact.

**Architecture:** Add dedicated minimal starting-gear prototypes instead of mutating full gear presets. Use a spawn-time profile mode that applies identity and bracer settings without creating profile armor, mask, greaves, or cape; existing post-vendor hooks continue to apply profile visuals to purchased items. Keep Bad Blood Grunt/Leader and other full event loadouts unchanged.

**Tech Stack:** RobustToolbox C#, NUnit integration tests, YAML entity/job/starting-gear prototypes.

## Global Constraints

- Do not reset, overwrite, or stage unrelated existing worktree changes.
- Do not change vendor inventory, prices, vendor points, rank/whitelist rules, or post-vendor profile replacement behavior.
- Do not replace full gear prototypes used by event mobs or vendor bundles.
- Follow TDD: each production change is preceded by a failing test and verified again after implementation.

---

### Task 1: Add failing regression tests for original-style player spawns

**Files:**
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaOriginalSpawnLoadoutTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs:120-420` to update existing direct-spawn expectations after the new regression test has demonstrated the old behavior.

**Interfaces:**
- Consumes: `StationSpawningSystem.SpawnPlayerMob`, `StationSpawningSystem.EquipStartingGear`, `InventorySystem`, `StartingGearPrototype`, and the existing Yautja entity/job prototypes.
- Produces: regression coverage proving the three player roles have only communicator + bracer at initial spawn.

- [ ] **Step 1: Write the failing tests**

  Add tests with these exact behaviors:

  ```csharp
  [Test]
  public async Task HunterPlayerSpawnStartsOnlyWithBracerAndCommunicator()
  {
      // Spawn CMUYautjaHunter through StationSpawningSystem with a profile
      // containing non-default armor, mask, greaves, and cape choices.
      // Assert ears = CMUYautjaCommunicator, gloves = CMUYautjaBracer,
      // and assert mask, outerClothing, shoes, back, jumpsuit, belt,
      // ears2, pocket1, and pocket2 are empty.
  }

  [Test]
  public async Task BadBloodPlayerSpawnStartsOnlyWithBadBloodBracerAndCommunicator()
  {
      // Spawn CMUYautjaBadBlood through StationSpawningSystem.
      // Assert ears = CMUYautjaBadBloodCommunicator,
      // gloves = CMUYautjaBadBloodBracer, and all other equipment slots
      // used by the former full gear are empty.
  }

  [Test]
  public async Task YoungbloodStartingGearContainsOnlyBracerAndCommunicator()
  {
      // Index CMUYautjaYoungbloodGear and assert its equipment map contains
      // exactly ears = CMUYautjaCommunicator and gloves = CMUYautjaBracer.
  }
  ```

  Use real prototypes and an integration server/map; do not mock inventory or prototype behavior.

- [ ] **Step 2: Run the new tests and verify RED**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~YautjaOriginalSpawnLoadoutTest
  ```

  Expected result: the tests fail because the current Hunter and Bad Blood entities still equip full gear and Youngblood still includes a mask.

- [ ] **Step 3: Update pre-existing assertions that describe the old spawn contract**

  Change only assertions that explicitly require the old full player starter kit in `YautjaPredatorRoleTest.cs`; keep assertions for communicator, bracer behavior, faction, access, identity, profile appearance, and post-vendor equipment. Update job `StartingGear` assertions to the new dedicated minimal prototype IDs.

- [ ] **Step 4: Re-run the focused test and confirm it still fails for the missing implementation**

  Run the same `dotnet test` command and record the failure as the expected RED state before touching production YAML/C#.

### Task 2: Add and wire minimal spawn gear prototypes

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml:1-120`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Mobs/mobs.yml:1-200`

**Interfaces:**
- Consumes: Existing `CMUYautjaBracer`, `CMUYautjaBadBloodBracer`, `CMUYautjaCommunicator`, and `CMUYautjaBadBloodCommunicator` item prototypes.
- Produces: Minimal `CMUYautjaHunterSpawnGear` and `CMUYautjaBadBloodSpawnGear` starting gear, and a mask-free `CMUYautjaYoungbloodGear`.

- [ ] **Step 1: Add minimal starting gear definitions**

  Add these exact equipment maps in `jobs.yml`:

  ```yaml
  - type: startingGear
    id: CMUYautjaHunterSpawnGear
    equipment:
      ears: CMUYautjaCommunicator
      gloves: CMUYautjaBracer

  - type: startingGear
    id: CMUYautjaBadBloodSpawnGear
    equipment:
      ears: CMUYautjaBadBloodCommunicator
      gloves: CMUYautjaBadBloodBracer
  ```

  Change the Hunter and Bad Blood job `startingGear` references to these IDs. Change `CMUYautjaYoungbloodGear` to contain only the communicator and bracer.

- [ ] **Step 2: Switch only player-spawn mob Loadout components**

  Set `CMUMobYautja` Loadout prototypes to `[CMUYautjaHunterSpawnGear]`. Set `CMUMobYautjaBadBlood` Loadout prototypes to `[CMUYautjaBadBloodSpawnGear]`. Do not alter the explicit full Loadout lists on `CMUMobYautjaBadBloodGrunt` or `CMUMobYautjaBadBloodLeader`.

- [ ] **Step 3: Run the focused tests**

  Run the Task 1 filter. Expected result: YAML/loadout assertions move closer to passing, but the Hunter test still fails because `YautjaProfileApplySystem` currently creates profile equipment in empty slots.

### Task 3: Prevent profile gear from being created during initial spawn

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaProfileApplySystem.cs:57-110`
- Modify: `Content.Server/Station/Systems/StationSpawningSystem.cs:265-275`

**Interfaces:**
- Consumes: Existing `ApplyProfile` callers and `YautjaAppliedProfileComponent` state used by post-vendor hooks.
- Produces: An explicit initial-spawn profile mode that applies identity, appearance, rank, and bracer settings without filling empty equipment slots.

- [ ] **Step 1: Add the failing assertion for profile gear suppression**

  The Hunter regression test must pass a profile with non-default armor, mask, greaves, and cape and assert those slots remain empty. Run the focused test to capture the failure caused by `ReplaceEquipped` spawning those items.

- [ ] **Step 2: Implement the minimal API change**

  Add an optional `bool equipProfileGear = true` parameter to `YautjaProfileApplySystem.ApplyProfile`. When it is `false`, skip the five `ReplaceEquipped` calls and only apply bracer settings to the already-equipped gloves item. Keep profile persistence, humanoid appearance, entity name, clan rank, flavor text, and `YautjaAppliedProfileComponent` unchanged.

  Pass `equipProfileGear: false` from the initial Yautja branch in `StationSpawningSystem`. Leave all other callers at the existing default so direct profile application and post-vendor behavior retain their current contract.

- [ ] **Step 3: Run the focused tests and verify GREEN**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~YautjaOriginalSpawnLoadoutTest
  ```

  Expected result: all new original-spawn tests pass.

- [ ] **Step 4: Run existing profile and post-vendor tests**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaCharacterProfileTest|FullyQualifiedName~YautjaPostVendorHookTest"
  ```

  Expected result: profile identity/rank tests and post-vendor replacement tests pass, proving the initial-spawn flag did not remove profile customization after vending.

### Task 4: Verify the complete affected surface

**Files:**
- No new production files; review the diff for only the files listed above plus the focused tests.

**Interfaces:**
- Consumes: Completed Tasks 1–3.
- Produces: Verified original-style spawn behavior with no unrelated worktree changes staged.

- [ ] **Step 1: Run the full Yautja integration test group**

  Run:

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja
  ```

- [ ] **Step 2: Build the affected projects**

  Run:

  ```powershell
  dotnet build Content.Server/Content.Server.csproj --no-restore
  dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore
  ```

- [ ] **Step 3: Check the final diff and staged scope**

  Run `git diff --check`, `git diff --stat`, and `git status --short`. Confirm only the intended Yautja production/test files are part of the implementation diff; leave all pre-existing user changes untouched.

- [ ] **Step 4: Commit the implementation separately from the design and plan**

  Stage only the implementation files and create a focused commit named `fix: match original yautja spawn loadouts` after all verification commands pass.
