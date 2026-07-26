# Falcon Climbing Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make a controlled Yautja Falcon use the standard Alt+LMB climbing flow for every entity with `Climbable`.

**Architecture:** Add the shared `Climbing` component to the deployed Falcon prototype. During Falcon control, relay the controller's interactions to the deployed drone using `SharedInteractionSystem`, mirroring the existing movement relay. Clear the interaction relay along every existing Falcon cleanup path so the controller returns to normal interaction behavior.

**Tech Stack:** C#, Space Station 14 ECS, YAML entity prototypes, NUnit integration tests, Robust integration-test pool.

## Global Constraints

- Use the existing `ClimbSystem`, `ClimbableComponent`, `ClimbingComponent`, `RMCMovementSystem.CanClimbOver`, and standard alternative-verb flow.
- Do not add a Falcon-specific obstacle list or duplicate vault validation.
- Falcon can climb only entities that already have `Climbable`.
- Preserve unrelated working-tree changes; stage only files belonging to this feature.

---

### Task 1: Add a failing Falcon climbing integration test

**Files:**
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaFalconClimbingTest.cs`

**Interfaces:**
- Consumes: `CMUYautjaFalconDrone`, `CMUYautjaBracer`, `CMMobHuman`, `Table`, `YautjaItemSystem` Falcon deployment, `PoolManager.GenerateServer`, and `SharedInteractionSystem.UserInteraction`.
- Produces: A regression test proving that a controller's Alt interaction is relayed to Falcon, starts the standard climb DoAfter, and cleans up the relay on recall.

- [ ] **Step 1: Write the failing test**

Create a focused server-only NUnit fixture with one test. Use `PoolManager.GenerateServer(new PoolSettings(), TestContext.Out)` so the regression test does not depend on unrelated client-side RSI resources. Create a map/grid with plating, spawn a human, bracer, Falcon item, and a table on the same grid. Mark the human as Yautja, equip the bracer, and raise `UseInHandEvent` on the Falcon item to deploy it. Resolve the deployed entity from `YautjaFalconControllerComponent`.

Use these assertions and interaction call in the test body:

```csharp
Assert.That(entMan.HasComponent<ClimbingComponent>(drone), Is.True);
Assert.That(entMan.HasComponent<InteractionRelayComponent>(hunter), Is.True);
Assert.That(
    entMan.GetComponent<InteractionRelayComponent>(hunter).RelayEntity,
    Is.EqualTo(drone));

var interaction = entMan.System<SharedInteractionSystem>();
var tableCoordinates = entMan.GetComponent<TransformComponent>(table).Coordinates;
interaction.UserInteraction(hunter, tableCoordinates, table, altInteract: true);
```

After the interaction, advance the server by 120 ticks (the standard 1.5 second climb delay plus margin) and assert:

```csharp
Assert.That(entMan.GetComponent<ClimbingComponent>(drone).IsClimbing, Is.True);
```

Delete the deployed Falcon to exercise the existing termination cleanup path. After two more server ticks, assert that the hunter no longer has `InteractionRelayComponent` and that the controller component is gone. Dispose the server in `finally`.

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaFalconClimbingTest --no-restore
```

Expected: FAIL before the implementation at the `ClimbingComponent` assertion because deployed Falcon has no climbing component and Falcon control does not install an interaction relay.

- [ ] **Step 3: Commit the failing test**

```powershell
git add -- Content.IntegrationTests/_CMU14/Yautja/YautjaFalconClimbingTest.cs
git commit -m "test: cover Falcon climbing through interaction relay"
```

### Task 2: Enable the shared climbing component on deployed Falcon

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/items.yml:357-396`

**Interfaces:**
- Consumes: The failing integration test from Task 1.
- Produces: `CMUYautjaFalconDroneDeployed` entities with the standard `ClimbingComponent` and its default `CanClimb`, transition rate, and state fields.

- [ ] **Step 1: Add the minimal prototype component**

Insert this component in the `CMUYautjaFalconDroneDeployed` component list, alongside movement and physics components:

```yaml
  - type: Climbing
```

Do not add `Climbable` to Falcon and do not override the standard climbing delay or transition rate.

- [ ] **Step 2: Run the focused test**

Run the same filtered `dotnet test` command from Task 1. Expected: the component-presence assertion passes, while the test still fails at relay setup or the Alt interaction path.

- [ ] **Step 3: Commit the prototype change**

```powershell
git add -- Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/items.yml
git commit -m "feat: make deployed Falcon climbable"
```

### Task 3: Relay and clean up Falcon interactions

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaItemSystem.cs:1-100,1250-1265,1342-1370`

**Interfaces:**
- Consumes: `InteractionRelayComponent`, `SharedInteractionSystem.SetRelay`, deployed Falcon entity, and the existing Falcon cleanup paths.
- Produces: Controller-to-Falcon interaction routing during control and no stale relay after recall, deletion, EMP/destruction conversion, bracer removal, or controller shutdown.

- [ ] **Step 1: Add the interaction dependency and install the relay**

Add `using Content.Shared.Interaction.Components;` and add this dependency beside the existing mover dependency:

```csharp
[Dependency] private SharedInteractionSystem _interaction = default!;
```

Immediately after `_mover.SetRelay(user, deployed);`, install the interaction relay using the existing engine pattern:

```csharp
var interactionRelay = EnsureComp<InteractionRelayComponent>(user);
_interaction.SetRelay(user, deployed, interactionRelay);
```

This keeps movement and interaction routed to the same deployed entity while the player's attached entity remains the hunter.

- [ ] **Step 2: Clear the interaction relay during Falcon cleanup**

In `CleanupFalconController`, before removing the movement relay, clear and remove the interaction relay only when it still points at this Falcon:

```csharp
if (TryComp(controller, out InteractionRelayComponent? interactionRelay) &&
    interactionRelay.RelayEntity == drone)
{
    _interaction.SetRelay(controller, null, interactionRelay);
    RemCompDeferred<InteractionRelayComponent>(controller);
}
```

Keep the existing movement cleanup and action cleanup unchanged. This cleanup is reached by recall, source-bracer removal, Falcon termination, controller shutdown, and controller termination.

- [ ] **Step 3: Run the focused test to verify it passes**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaFalconClimbingTest --no-restore
```

Expected: PASS, including the standard `IsClimbing` assertion and relay cleanup after recall.

- [ ] **Step 4: Commit the server change**

```powershell
git add -- Content.Server/_CMU14/Yautja/YautjaItemSystem.cs
git commit -m "feat: relay Falcon climbing interactions"
```

### Task 4: Run regression verification

**Files:**
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaFalconClimbingTest.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaFalconRuntimeTest.cs`
- Test: `Content.IntegrationTests/Tests/Climbing/ClimbingTest.cs`

**Interfaces:**
- Consumes: The completed prototype and server integration from Tasks 2-3.
- Produces: Evidence that Falcon climbing works and existing Falcon/climbing behavior remains intact.

- [ ] **Step 1: Run the focused Falcon climbing test again**

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaFalconClimbingTest --no-restore
```

Expected: PASS.

- [ ] **Step 2: Run existing Falcon runtime coverage**

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaFalconRuntimeTest --no-restore
```

Expected: PASS; deploy, movement relay, recall, destruction, and Z-level tests remain green.

- [ ] **Step 3: Run shared climbing coverage**

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~ClimbingTest --no-restore
```

Expected: PASS; ordinary doll behavior is unchanged.

- [ ] **Step 4: Build the affected projects**

```powershell
dotnet build Content.Server/Content.Server.csproj --no-restore
dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore
```

Expected: both builds complete successfully with no new errors.

- [ ] **Step 5: Inspect the final diff and status**

```powershell
git diff HEAD~3..HEAD -- Content.Server/_CMU14/Yautja/YautjaItemSystem.cs Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/items.yml Content.IntegrationTests/_CMU14/Yautja/YautjaFalconClimbingTest.cs
git status --short --branch
```

Confirm only the intended feature files were changed by the implementation commits; leave the pre-existing `RobustToolbox` and `cmss13-ref*` changes untouched.
