# Yautja CMSS13 Chair, Falcon, Gauntlet, and Hunting Grounds Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the four approved CMSS13 parity fixes for Yautja chairs, chain gauntlets, Falcon visibility, and hunting-ground escape gates.

**Architecture:** Use a small shared chair-facing component/system to apply the chair's configured cardinal direction to a buckled mob. Remove only the obsolete chain-gauntlet examine handler. Use a client-side Falcon layer filter driven by the local viewer's Yautja/ghost/admin state, leaving server control and world entity state unchanged. Add concrete Preserve Shutter and escape-console prototypes to the existing hunt-console flow, then place them in the three local hunting-ground maps.

**Tech Stack:** RobustToolbox C#, YAML entity prototypes/maps, NUnit integration tests, `dotnet test`.

## Global Constraints

- Preserve all unrelated existing working-tree changes; stage only files changed for this task.
- Do not change global FOV, line-of-sight, interaction range, or Falcon control authorization.
- Do not remove chain-gauntlet attacks, combo state, damage, or door-forcing behavior.
- Ordinary living non-Yautja viewers must not see the deployed Falcon; Yautja, ghosts, and administrators retain visibility.
- Preserve Shutters are power-independent, unbreakable, explosion-proof, unslashable, and unacidable.

---

### Task 1: Make Hunter Ship chairs face buckled mobs

**Files:**
- Create: `Content.Shared/_CMU14/Yautja/YautjaChairFacingComponent.cs`
- Create: `Content.Shared/_CMU14/Yautja/YautjaChairFacingSystem.cs`
- Modify: `Resources/Prototypes/_CMU14/Maps/huntership_visuals.yml` at the 24 `CMUHunterShipPlacedCMChairComfy*`/`CMUHunterShipPlacedCMChairNonFold*` wrappers
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs`

**Interfaces:**
- `YautjaChairFacingComponent.Direction` stores the chair's cardinal facing.
- `YautjaChairFacingSystem` subscribes to `StrappedEvent` and calls `SharedTransformSystem.SetLocalRotation(buckle, Direction.ToAngle())`.
- Each Hunter Ship chair wrapper sets `YautjaChairFacing.direction` to the same direction as its `Sprite.overrideDir`.

- [ ] **Step 1: Add the failing integration test**

Add `HunterShipChairBuckleFacesChairDirection` to `YautjaSmokeTest.cs`. Spawn `CMUHunterShipPlacedCMChairNonFoldChairEast` and `CMUHunterShipPlacedCMChairComfyComfychairNorth`, buckle a `CMMobHuman` to each with `RMCBuckleSystem.TryBuckle`, and assert:

```csharp
Assert.That(transform.GetWorldRotation(hunter).GetCardinalDir(), Is.EqualTo(Direction.East));
Assert.That(transform.GetWorldRotation(hunter).GetCardinalDir(), Is.EqualTo(Direction.North));
```

- [ ] **Step 2: Run the focused test and verify the expected failure**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~HunterShipChairBuckleFacesChairDirection" --no-restore
```

Expected: the buckle succeeds but the mob remains at its default direction, proving the regression test catches the missing behavior.

- [ ] **Step 3: Implement the smallest shared chair-facing behavior**

Create the registered component with a `Direction Direction = Direction.South` data field. In the system's `OnStrapped` handler, apply the configured local rotation after the generic buckle system has set the buckle coordinates. Do not alter unbuckling or generic straps.

- [ ] **Step 4: Configure every Hunter Ship chair wrapper**

Add the component to all directional comfy-chair, hunter-chair, and throne-seat wrappers. Use `East`, `North`, `South`, or `West` exactly matching each wrapper's `overrideDir`; leave decorative non-seat props without the component.

- [ ] **Step 5: Run the focused test and the existing buckle regression**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~HunterShipChairBuckleFacesChairDirection|FullyQualifiedName~YautjaSmokeTest" --no-restore
```

Expected: the new direction assertions and existing buckle/teleport cases pass.

### Task 2: Remove obsolete chain-gauntlet intent instructions

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaChainGauntletSystem.cs:64-123`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaMeleeWeaponTest.cs:293-344`

**Interfaces:**
- Chain-gauntlet examine behavior is provided only by the prototype's normal description after this task.
- All action, melee, combo, execution, chain-pull, and door-force handlers remain registered.

- [ ] **Step 1: Replace the old test with a failing absence assertion**

Rename the existing test to `ChainGauntletExamineHasNoCombatIntentGuidance`. Examine the gauntlet as a Yautja, a tech-authorized non-Yautja, and a normal human; assert that every result does not contain `HARM`, `HELP`, `SHOVE`, `GRAB`, `combo meter`, or `Finish your combo`.

- [ ] **Step 2: Run the focused test and verify it fails**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~ChainGauntletExamineHasNoCombatIntentGuidance" --no-restore
```

Expected: the Yautja examine text fails because the current handler appends the four obsolete intent instructions.

- [ ] **Step 3: Remove only the examine subscription and handler**

Delete the `ExaminedEvent` subscription and `OnExamined` method. Keep `OnGetItemActions`, `OnInteractUsing`, `OnMeleeHit`, execution, guard, chain pull, and force-door code intact.

- [ ] **Step 4: Run the focused melee test class**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~YautjaMeleeWeaponTest" --no-restore
```

Expected: all gauntlet combat behavior passes, including the new clean-examine assertion.

### Task 3: Hide deployed Falcon from ordinary living viewers

**Files:**
- Create: `Content.Shared/_CMU14/Yautja/YautjaFalconVisualLayers.cs`
- Create: `Content.Client/_CMU14/Yautja/YautjaFalconVisibilitySystem.cs`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/items.yml:393-445`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaFalconRuntimeTest.cs`

**Interfaces:**
- `YautjaFalconVisualLayers.Base` is the layer map used by both deployed Falcon sprites.
- `YautjaFalconVisibilitySystem` queries `YautjaFalconDroneDeployedComponent` and toggles only that sprite layer.
- Viewer authorization is true when the local entity has `YautjaComponent`, has `GhostComponent`, or `IClientAdminManager.IsAdmin()` is true. Unknown/ordinary living viewers are hidden.

- [ ] **Step 1: Add the failing viewer-matrix test**

Add `FalconDroneIsHiddenFromOrdinaryViewersButVisibleToYautjaAndGhosts` to `YautjaFalconRuntimeTest.cs`. Spawn a deployed Falcon, resolve its client `SpriteComponent`, and assert the mapped base layer is hidden for the connected ordinary human. Add `YautjaComponent` to the attached local entity and assert the layer becomes visible after synchronization; remove it, add `GhostComponent` on the client local entity, and assert visibility returns.

- [ ] **Step 2: Run the focused test and verify it fails**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~FalconDroneIsHiddenFromOrdinaryViewersButVisibleToYautjaAndGhosts" --no-restore
```

Expected: the ordinary viewer assertion fails because the current Falcon sprite layer is visible to everyone.

- [ ] **Step 3: Add the layer map and client visibility system**

Map the active Falcon layer to `enum.YautjaFalconVisualLayers.Base` in both deployed prototypes. In the client system, use the existing `LayerMapTryGet`/`LayerSetVisible` pattern from `RMCVisibleOnlyToGhostsSystem`; never set `SpriteComponent.Visible`, so Z-level culling and the existing foreground tests remain valid.

- [ ] **Step 4: Run the viewer and existing Falcon tests**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~YautjaFalconRuntimeTest|FullyQualifiedName~YautjaFalconZLevelCullingTest" --no-restore
```

Expected: ordinary viewers lose only the Falcon layer, Yautja control/Z-level rendering remains visible, and existing HUD/control tests pass.

### Task 4: Add CMSS13-style Preserve Shutters and escape consoles to hunting grounds

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml`
- Modify: `Resources/Maps/_CMU14/HuntingGrounds/jungle_moon.yml`
- Modify: `Resources/Maps/_CMU14/HuntingGrounds/desert_moon.yml`
- Modify: `Resources/Maps/_CMU14/HuntingGrounds/desert_moon_caves.yml`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaHuntingGroundMapTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaPreserveConsoleTest.cs`

**Interfaces:**
- `CMUYautjaHuntingGroundPreserveShutter` inherits `RMCPodDoorIndestructible`, adds `YautjaPreserveShutterComponent`, uses the Yautja shutter sprite, sets `Door` closed by default, disables APC/power requirements, and disables corrosion.
- `CMUYautjaHuntingGroundEscapeConsole` inherits `CMUYautjaStructureBase`, adds `YautjaHuntEscapeConsoleComponent`, a static non-colliding console sprite/fixture, `Clickable`, and `InteractionOutline`.
- `YautjaHuntConsoleSystem` continues to open/close every `YautjaPreserveShutterComponent` through its existing authoritative query and 15-second mask-scan flow.

- [ ] **Step 1: Add failing map/prototype assertions**

Extend `HuntingGroundMapsLoadWithSourceLandmarkMarkers` to require at least one `CMUYautjaHuntingGroundPreserveShutter` and one `CMUYautjaHuntingGroundEscapeConsole` in each of the three hunting-ground map files. Add a prototype test that the shutter has `YautjaPreserveShutterComponent`, `Door`, `ApcPowerReceiver.NeedsPower == false`, and no breakable/corrosible path.

- [ ] **Step 2: Run the focused hunting-ground tests and verify failure**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~YautjaHuntingGroundMapTest|FullyQualifiedName~YautjaPreserveConsoleTest" --no-restore
```

Expected: the new map count/prototype assertions fail because the converted local maps currently contain no actual escape gate or console prototypes.

- [ ] **Step 3: Implement the two concrete prototypes**

Add the shutter and console prototypes to the Yautja structures prototype file. Preserve the existing `YautjaHuntConsoleSystem` component contracts and avoid reusing ordinary powered/internal Yautja door buttons as escape consoles.

- [ ] **Step 4: Place gates and consoles in all hunting-ground maps**

Add source-port map entity blocks with the new shutter and console prototypes. Use a four-tile shutter row plus adjacent console for Jungle Moon, the surface gate/console arrangement for Desert Moon, and the cave gate/console arrangement for Desert Moon Caves. Keep gate entities on open map tiles and keep the console adjacent to the gate.

- [ ] **Step 5: Add and run the authoritative escape regression**

Extend `YautjaPreserveConsoleTest` to spawn the real shutter prototype, assert it starts closed and power-independent, verify non-Yautja hand interaction is rejected, verify a non-held mask is rejected, verify a held Yautja mask starts a 15-second do-after, and verify completion opens all real preserve shutters. Run the focused map/console test command again and require all assertions to pass.

### Task 5: Final verification and handoff

**Files:**
- Verify only: all files modified by Tasks 1-4

- [ ] **Step 1: Run whitespace and changed-file checks**

Run:

```powershell
git diff --check
git diff --stat
git status --short
```

Confirm unrelated pre-existing modifications remain unstaged.

- [ ] **Step 2: Run all focused tests together**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~YautjaSmokeTest|FullyQualifiedName~YautjaMeleeWeaponTest|FullyQualifiedName~YautjaFalconRuntimeTest|FullyQualifiedName~YautjaFalconZLevelCullingTest|FullyQualifiedName~YautjaHuntingGroundMapTest|FullyQualifiedName~YautjaPreserveConsoleTest" --no-restore
```

Record the actual passed/failed count; do not claim completion if the test host exits before assertions or if unrelated infrastructure failures prevent execution.

- [ ] **Step 3: Build the affected projects**

Run:

```powershell
dotnet build Content.Client/Content.Client.csproj --no-restore
dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore
```

Use fresh exit codes and output as the evidence for the final report.

