# Hunter Ship Alpha/Forsaken Eggs and Hives Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Hunter Ship's red Alpha and lilac Forsaken eggs fully interactive while giving their spawned parasites and xenomorphs separate hive identity, color, and NPC relations.

**Architecture:** Reuse RMC's `HiveComponent`/`HiveMemberComponent` and `XenoEggSystem`. A station-scoped Hunter Ship bootstrap creates dedicated hidden Alpha/Forsaken hives and assigns map entities through an explicit assignment component, avoiding the existing global name lookup. Hive-specific NPC factions are synchronized from `SetHive`, while the existing hive color visualizer remains the only sprite tint source.

**Tech Stack:** RobustToolbox C#, YAML entity prototypes, NUnit integration tests, existing CMU Hunter Ship map/RSI assets.

## Global Constraints

- Do not introduce numeric BYOND-style `hivenumber` state.
- Do not modify the generic `CMUAlphaHive` semantics used by Ciphering; Hunter Ship uses dedicated prototypes.
- Do not add new raster textures; reuse `_CMU14/HunterShip/mob/xenos/effects.rsi` and `weeds.rsi`.
- Keep the generated Hunter Ship coordinates, offsets, and visual regression counts unchanged.
- Preserve the main checkout's dirty `RobustToolbox`, `cmss13-ref`, and `cmss13-ref-full` state by working only in `C:\Users\bebebe\.config\superpowers\worktrees\RussianCM\yautja-hunter-ship-hives`.
- Follow red-green-refactor for behavior changes: each production change starts with a failing integration test.

---

### Task 1: Add hive-specific faction and ally semantics

**Files:**
- Modify: `Content.Shared/_RMC14/Xenonids/Hive/HiveComponent.cs`
- Modify: `Content.Shared/_RMC14/Xenonids/Hive/SharedXenoHiveSystem.cs`
- Modify: `Resources/Prototypes/_RMC14/Factions/rmc_factions.yml`
- Modify: `Resources/Prototypes/_CMU14/Entities/Mobs/Xeno/hive.yml`
- Create: `Content.IntegrationTests/_CMU14/Xenonids/CMUIndependentXenoHiveTest.cs`

**Interfaces:**
- `HiveComponent.NpcFaction` is an optional `ProtoId<NpcFactionPrototype>` configured by hive prototypes.
- `SharedXenoHiveSystem.SetHive` synchronizes `NpcFactionMemberComponent` when a member changes between a hive with and without `NpcFaction`.
- `SharedXenoHiveSystem.FromSameHiveOrAlly` returns `true` when the second entity is a member or configured faction ally of the first entity's hive.

- [x] **Step 1: Write the prototype and behavior tests.**

Add tests that index `CMUHunterShipAlphaHive` and `CMUHunterShipForsakenHive`, assert their colors and NPC faction IDs, then spawn `CMXenoParasite` entities, assign each to its hive, and assert that their `NpcFactionMemberComponent.Factions` contain different dedicated factions and do not retain `RMCXeno`.

```csharp
Assert.That(alpha.Comp.HiveColor, Is.EqualTo(Color.FromHex("#ff4040")));
Assert.That(forsaken.Comp.HiveColor, Is.EqualTo(Color.FromHex("#cc8ec4")));
Assert.That(alpha.Comp.NpcFaction, Is.EqualTo((ProtoId<NpcFactionPrototype>) "CMUXenoAlpha"));
Assert.That(forsaken.Comp.NpcFaction, Is.EqualTo((ProtoId<NpcFactionPrototype>) "CMUXenoForsaken"));
Assert.That(alphaFactions.Factions, Does.Contain("CMUXenoAlpha"));
Assert.That(alphaFactions.Factions, Does.Not.Contain("RMCXeno"));
Assert.That(forsakenFactions.Factions, Does.Contain("CMUXenoForsaken"));
Assert.That(forsakenFactions.Factions, Does.Not.Contain("RMCXeno"));
Assert.That(npcFaction.IsEntityFriendly((alphaParasite, alphaFactions), (forsakenParasite, forsakenFactions)), Is.False);
```

Run: `dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter FullyQualifiedName~Content.IntegrationTests._CMU14.Xenonids.CMUIndependentXenoHiveTest`

Expected: FAIL because the dedicated hive prototypes, component field, and faction synchronization do not exist.

- [x] **Step 2: Add the optional hive NPC faction field.**

In `HiveComponent.cs`, add a networked/data field:

```csharp
[DataField, AutoNetworkedField, ViewVariables]
public ProtoId<NpcFactionPrototype>? NpcFaction;
```

Keep the field nullable so normal RMC hives continue using their current faction behavior.

- [x] **Step 3: Add Alpha/Forsaken faction prototypes.**

In `rmc_factions.yml`, add `CMUXenoAlpha` and `CMUXenoForsaken`. Each faction must list `RMCXeno`, the other dedicated hive faction, and the current human/neutral hostiles used by `RMCXeno` as hostile. Add both dedicated factions to `RMCXeno.hostile` so the relation is bidirectional for NPC target discovery.

- [x] **Step 4: Synchronize faction membership from `SetHive`.**

Inject `NpcFactionSystem` into `SharedXenoHiveSystem`. Before assigning the new hive, inspect the old hive's optional faction. When a member enters a dedicated hive, remove `RMCXeno` and the old dedicated faction, then add the new faction. When a member leaves a dedicated hive for a normal hive, remove the dedicated faction and restore `RMCXeno`. Only touch entities that already have `NpcFactionMemberComponent`.

- [x] **Step 5: Implement `FromSameHiveOrAlly`.**

Resolve the first entity's hive and return `_hive.IsAllyOfHive(b.Owner, hive.Owner)`. Preserve `false` for rogue entities and missing hives.

- [x] **Step 6: Run the focused test.**

Run the same filtered test and confirm it passes. Commit:

```powershell
git add Content.Shared/_RMC14/Xenonids/Hive/HiveComponent.cs Content.Shared/_RMC14/Xenonids/Hive/SharedXenoHiveSystem.cs Resources/Prototypes/_RMC14/Factions/rmc_factions.yml Resources/Prototypes/_CMU14/Entities/Mobs/Xeno/hive.yml Content.IntegrationTests/_CMU14/Xenonids/CMUIndependentXenoHiveTest.cs
git commit -m "feat: separate Alpha and Forsaken hive factions"
```

### Task 2: Add Hunter Ship hive bootstrap and explicit assignments

**Files:**
- Create: `Content.Shared/_CMU14/Yautja/HunterShip/CMUHunterShipHiveComponents.cs`
- Create: `Content.Server/_CMU14/Yautja/CMUHunterShipHiveBootstrapSystem.cs`
- Modify: `Resources/Prototypes/_CMU14/Maps/huntership.yml`
- Modify: `Resources/Prototypes/_CMU14/Entities/Mobs/Xeno/hive.yml`
- Create: `Content.IntegrationTests/_CMU14/HunterShip/HunterShipHiveBootstrapTest.cs`

**Interfaces:**
- `CMUHunterShipHiveKind` has `Alpha` and `Forsaken` values.
- `CMUHunterShipHiveAssignmentComponent.Hive` stores the desired kind on map entities.
- `CMUHunterShipHiveBootstrapComponent` is placed on the `CMUYautjaHunterShip` station prototype.
- `CMUHunterShipHiveBootstrapSystem` creates one hidden hive per kind and calls `SharedXenoHiveSystem.SetHive` for every assignment entity.

- [x] **Step 1: Write the bootstrap test.**

Spawn a test station entity with `CMUHunterShipHiveBootstrap`, two assigned entities, and run the system startup. Assert that exactly one dedicated Alpha and one dedicated Forsaken hive are present and each assignment has a `HiveMemberComponent` pointing at the expected hive.

```csharp
var alphaAssignments = server.EntMan.GetEntityQuery<CMUHunterShipHiveAssignmentComponent>();
Assert.That(alphaAssignments.GetComponent(alphaEgg).Hive, Is.EqualTo(CMUHunterShipHiveKind.Alpha));
Assert.That(server.EntMan.GetComponent<HiveMemberComponent>(alphaEgg).Hive, Is.EqualTo(alphaHive));
Assert.That(server.EntMan.GetComponent<HiveMemberComponent>(forsakenEgg).Hive, Is.EqualTo(forsakenHive));
Assert.That(hives.Count(h => h.Comp.NpcFaction == "CMUXenoAlpha"), Is.EqualTo(1));
Assert.That(hives.Count(h => h.Comp.NpcFaction == "CMUXenoForsaken"), Is.EqualTo(1));
```

Run: `dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter FullyQualifiedName~Content.IntegrationTests._CMU14.HunterShip.HunterShipHiveBootstrapTest`

Expected: FAIL because the components, server system, and dedicated prototypes do not exist.

- [x] **Step 2: Add shared assignment/bootstrap components.**

Create the enum and components in the shared CMU Yautja folder. Mark assignment as networked/data-driven and bootstrap as a server-triggered station component recognized by both prototype loaders.

- [x] **Step 3: Add dedicated Hunter Ship hive prototypes.**

Define `CMUHunterShipAlphaHive` and `CMUHunterShipForsakenHive` as hidden entities with `Hive` components, colors `#ff4040`/`#cc8ec4`, `lateJoinGainLarva: false`, and NPC factions `CMUXenoAlpha`/`CMUXenoForsaken`.

- [x] **Step 4: Implement station-scoped bootstrap with z-level retry.**

On station initialization, create the dedicated hives, enumerate all `CMUHunterShipHiveAssignmentComponent` entities on the station's root map and connected z-level maps, and assign each entity to the matching hive. Retry while z-level maps finish loading, store the created hive UIDs on the bootstrap component, and delete the hives when the station is removed.

- [x] **Step 5: Register the bootstrap on the Hunter Ship station.**

Add `CMUHunterShipHiveBootstrap` to the `CMUYautjaHunterShip` station components in the game map prototype. Do not modify generated map entity UIDs or the visual conversion files in this task.

- [x] **Step 6: Run the focused bootstrap test.**

Run the filtered bootstrap test and commit:

```powershell
git add Content.Shared/_CMU14/Yautja/HunterShip/CMUHunterShipHiveComponents.cs Content.Server/_CMU14/Yautja/CMUHunterShipHiveBootstrapSystem.cs Resources/Prototypes/_CMU14/Maps/huntership.yml Resources/Prototypes/_CMU14/Entities/Mobs/Xeno/hive.yml Content.IntegrationTests/_CMU14/HunterShip/HunterShipHiveBootstrapTest.cs
git commit -m "feat: bootstrap Hunter Ship specimen hives"
```

### Task 3: Extend egg lifecycle and target restrictions

**Files:**
- Modify: `Content.Shared/_RMC14/Xenonids/Egg/XenoEggComponent.cs`
- Modify: `Content.Shared/_RMC14/Xenonids/Egg/XenoEggSystem.cs`
- Modify: `Content.Server/_RMC14/Xenonids/Parasite/XenoParasiteRoleSystem.cs`
- Create: `Content.IntegrationTests/_CMU14/HunterShip/HunterShipXenoEggLifecycleTest.cs`

**Interfaces:**
- `XenoEggComponent.CanSpawnGhostParasite` defaults to `true`.
- `XenoEggSystem` initializes non-item eggs through the existing state transition and uses hive ally checks consistently.
- `XenoEggRoleSystem` refuses ghost claims when `CanSpawnGhostParasite` is `false`.

- [x] **Step 1: Cover lifecycle configuration in the focused CMU test; verify target restrictions through the shared trigger path.**

Cover four independent behaviors: a growing egg reaches grown state, `Open` gives the parasite the egg hive, Yautja/synth targets do not pass the trigger path, and the ghost verb/BUI path is unavailable when the component flag is false.

```csharp
Assert.That(egg.Comp.State, Is.EqualTo(XenoEggState.Grown));
Assert.That(server.EntMan.GetComponent<HiveMemberComponent>(parasite).Hive, Is.EqualTo(forsakenHive));
Assert.That(egg.Comp.CanSpawnGhostParasite, Is.False);
Assert.That(ghostRoleSystem.CanClaimEggForTest(egg), Is.False);
```

Use the public step-trigger/activation events for target checks, or expose only the smallest internal test seam needed; do not make production internals public solely for broad test access.

Run: `dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter FullyQualifiedName~Content.IntegrationTests._CMU14.HunterShip.HunterShipXenoEggLifecycleTest`

Expected: FAIL because the flag, startup transition, restrictions, and ghost-role guard do not exist.

- [x] **Step 2: Add `CanSpawnGhostParasite`.**

Add the networked data field with default `true` so ordinary RMC eggs keep current gameplay.

- [x] **Step 3: Initialize non-item egg state.**

Subscribe to `XenoEggComponent, ComponentStartup`. For an initial state other than `Item`, call the existing `SetEggState` with the current state so growing fixtures and visuals are initialized without changing ordinary item eggs.

- [x] **Step 4: Unify hive checks and reject protected targets.**

Change anchored xeno activation to `_hive.IsAllyOfHive(args.User, hive)`. Update `CanTrigger` to reject `YautjaComponent` and `SynthComponent`, while retaining all infection/death checks. Ensure a missing egg hive cannot trigger or open via xeno interaction.

- [x] **Step 5: Guard both ghost-role entry points.**

Skip the ghost activation verb when the flag is false and reject the same state in `OnXenoEggGhostBuiChosen` before calling `Open`. Keep AI parasite hatching unaffected.

- [x] **Step 6: Run the focused CMU test.**

Run the filtered lifecycle test and commit:

```powershell
git add Content.Shared/_RMC14/Xenonids/Egg/XenoEggComponent.cs Content.Shared/_RMC14/Xenonids/Egg/XenoEggSystem.cs Content.Server/_RMC14/Xenonids/Parasite/XenoParasiteRoleSystem.cs Content.IntegrationTests/_CMU14/HunterShip/HunterShipXenoEggLifecycleTest.cs
git commit -m "feat: preserve hive ownership across specimen eggs"
```

### Task 4: Convert Hunter Ship visual wrappers into gameplay prototypes

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Maps/huntership_visuals.yml`
- Modify: `Resources/Prototypes/_CMU14/Yautja/hunter_ship_backends.yml`
- Create: `Content.IntegrationTests/_CMU14/HunterShip/HunterShipSpecimenPrototypeTest.cs`

**Interfaces:**
- The existing map prototype IDs remain unchanged so `Resources/Maps/_CMU14/huntership.yml` and `huntership_lower.yml` keep their coordinates.
- All eight item egg wrappers contain `XenoEgg` plus `CMUHunterShipHiveAssignment`.
- The active Forsaken egg contains `XenoEgg`, `State: Growing`, `CMUHunterShipHiveAssignment: Forsaken`, custom CMU state names, and `CanSpawnGhostParasite: false`.
- The Forsaken weed node is based on `XenoHiveWeedsSource` and carries the Forsaken assignment.

- [x] **Step 1: Write prototype assertions.**

Enumerate the eight item egg prototypes and assert their assignment kind matches the five red and three lilac IDs. Assert the active egg is a real `XenoEgg` with `Growing` state and disabled ghost parasite, and the weed node has `HiveWeeds` plus Forsaken assignment.

```csharp
Assert.That(prototype.TryGetComponent<XenoEggComponent>(out var egg, factory), Is.True, prototype.ID);
Assert.That(prototype.TryGetComponent<CMUHunterShipHiveAssignmentComponent>(out var assignment, factory), Is.True, prototype.ID);
Assert.That(assignment!.Hive, Is.EqualTo(expectedHive));
```

Run: `dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter FullyQualifiedName~Content.IntegrationTests._CMU14.HunterShip.HunterShipSpecimenPrototypeTest`

Expected: FAIL because the generated wrappers are currently visual-only or unassigned.

- [x] **Step 2: Add shared CMU egg configuration to each wrapper.**

Override `XenoEgg` fields with `_CMU14/HunterShip/mob/xenos/effects.rsi`, `ItemState: egg_item`, `GrowingState: Egg Growing`, `GrownState: Egg`, `OpeningState: Egg Opening`, and `OpenedState: Egg Opened`. Remove manual layer `color` values from item wrappers. Preserve their sprite offsets, draw depth, and direction.

- [x] **Step 3: Convert the active egg prototype.**

Change its parent from `CMUHunterShipStaticAlienEffect` to `XenoEgg`, add anchored transform and Forsaken assignment, set `state: Growing`, set `currentSprite`/`normalSprite` to the CMU effects RSI, and set `canSpawnGhostParasite: false`. Retain the exact `Egg Growing` sprite state and map prototype ID.

- [x] **Step 4: Convert the weed node prototype.**

Change its parent to `XenoHiveWeedsSource`, add Forsaken assignment, and override the sprite with the existing CMU `weeds.rsi` `weednode` art. Keep it anchored and visible at the same map coordinate.

- [x] **Step 5: Run prototype, map-load, and Hunter Ship visual regression tests.**

Run the focused prototype test, the existing `Content.IntegrationTests._CMU14.HunterShip.HunterShipVisualRegressionTest`, and the map-load test that covers `CMUYautjaHunterShip`. Confirm no entity count or coordinate changes.

- [x] **Step 6: Prepare the consolidated feature commit.**

```powershell
git add Resources/Prototypes/_CMU14/Maps/huntership_visuals.yml Resources/Prototypes/_CMU14/Yautja/hunter_ship_backends.yml Content.IntegrationTests/_CMU14/HunterShip/HunterShipSpecimenPrototypeTest.cs
git commit -m "feat: make Hunter Ship specimen eggs interactive"
```

### Task 5: Full verification and handoff

**Files:**
- Modify: none unless verification reveals a failing assertion covered by Tasks 1–4.
- Inspect: `git diff --check`, `git status`, generated prototype validation, focused integration tests, full CMU integration test shard.

- [x] **Step 1: Run the focused CMU hive/prototype tests.**

Run:

```powershell
dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Xenonids.CMUIndependentXenoHiveTest|FullyQualifiedName~Content.IntegrationTests._CMU14.HunterShip.HunterShipHiveBootstrapTest|FullyQualifiedName~Content.IntegrationTests._CMU14.HunterShip.HunterShipXenoEggLifecycleTest|FullyQualifiedName~Content.IntegrationTests._CMU14.HunterShip.HunterShipSpecimenPrototypeTest"
```

Expected: all tests pass with zero failures and no prototype-load errors.

- [x] **Step 2: Run the existing CMU Hunter Ship visual regression suite.**

Run:

```powershell
dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter FullyQualifiedName~Content.IntegrationTests._CMU14.HunterShip
```

Expected: all existing Hunter Ship tests pass, including visual regression and map loading.

- [x] **Step 3: Run solution build and diff validation.**

Run:

```powershell
git diff --check
dotnet build Content.Server/Content.Server.csproj --no-restore
dotnet build Content.Client/Content.Client.csproj --no-restore
git status --short
```

Expected: both builds exit 0, `git diff --check` is empty, and only intentional feature files plus the committed spec/plan are present.

- [x] **Step 4: Review requirements before reporting completion.**

Verify line by line that: Hunter Ship eggs are real gameplay entities; red/lilac colors come from hives; active eggs grow and open; AI parasite hatching remains possible; ghost claims are disabled only for the designated active eggs; Yautja/synth immunity is enforced; Alpha/Forsaken membership and NPC factions are separate; generic Ciphering Alpha remains intact; no main-checkout files were changed.
