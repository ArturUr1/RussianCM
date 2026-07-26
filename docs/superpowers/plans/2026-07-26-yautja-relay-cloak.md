# Yautja Relay Cloak Preservation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Preserve active invisibility for a Yautja relay user and any pulled living passenger after the relay teleport completes.

**Architecture:** Keep relay teleport responsible only for resolving the destination, showing relay feedback, and moving the teleport train. Remove the relay-specific `ForceDecloak` calls; `YautjaTeleportSystem.TeleportTrain` already moves entities without changing cloak components. Update the two existing integration regressions that currently encode the old decloak behavior.

**Tech Stack:** C#, RobustToolbox EntitySystem events, NUnit integration tests, .NET test runner.

## Global Constraints

- Do not change `YautjaTeleportSystem`, `YautjaCloakSystem`, or non-relay decloak rules.
- Preserve existing relay disappear popups and pulled-train behavior.
- The user and pulled living passenger each retain invisibility only when they had it before teleport.
- Run the focused integration test before claiming the fix works.

---

### Task 1: Change relay regression tests to the required behavior

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs:1225-1355`
- Reuse: `MakeActivelyCloaked(IEntityManager entMan, EntityUid user)` at the bottom of the same file

**Interfaces:**
- Consumes: Existing `SimpleRelayBeaconSuccessfulDoAfterDecloaksUserLikeCmss13ThrallTeleporter` and `SimpleRelayBeaconSuccessfulDoAfterDecloaksPulledLivingPassengerAndTeleportsTrainLikeCmss13ThrallTeleporter` scenarios.
- Produces: Two regression tests that fail against the current `ForceDecloak` implementation and pass only when relay preserves active invisibility.

- [ ] **Step 1: Rename the user test and invert its expected behavior assertion**

Rename the test to `SimpleRelayBeaconSuccessfulDoAfterKeepsUserCloaked` and change the post-teleport assertion to:

```csharp
Assert.That(entMan.HasComponent<EntityActiveInvisibleComponent>(bloodedThrall), Is.True,
    "A relay teleport must preserve the user's active invisibility.");
```

Keep the existing `MakeActivelyCloaked(entMan, bloodedThrall)` setup and relay do-after flow unchanged.

- [ ] **Step 2: Rename the passenger test and invert its expected behavior assertion**

Rename the test to `SimpleRelayBeaconSuccessfulDoAfterKeepsPulledPassengerCloakedAndTeleportsTrain` and change the passenger assertion to:

```csharp
Assert.That(entMan.HasComponent<EntityActiveInvisibleComponent>(passenger), Is.True,
    "A relay teleport must preserve a pulled passenger's active invisibility.");
```

Keep the existing `MakeActivelyCloaked(entMan, passenger)` setup and coordinate/pull-link assertions unchanged.

- [ ] **Step 3: Run the focused tests and verify the red failure**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest --no-restore
```

Expected: the two renamed relay cloak tests fail because `YautjaItemSystem.OnRelayBeaconDoAfter` currently calls `ForceDecloak` for the user and pulled passenger. Existing unrelated tests in the fixture must not be changed to make this failure pass.

### Task 2: Remove only relay-triggered decloaking

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaItemSystem.cs:842-861`

**Interfaces:**
- Consumes: The relay do-after completion path and its existing popup/coordinate resolution.
- Produces: Relay teleport completion that calls `TeleportTrain` without changing active cloak state.

- [ ] **Step 1: Remove the user `ForceDecloak` call**

In `OnRelayBeaconDoAfter`, delete the line immediately after the user disappear popup:

```csharp
_cloak.ForceDecloak(args.User);
```

Leave the popup and all validation/destination logic intact.

- [ ] **Step 2: Remove the pulled-passenger `ForceDecloak` call**

Inside the existing pulled living-mob branch, delete:

```csharp
_cloak.ForceDecloak(pulled);
```

Keep the passenger popup and `TeleportTrain` call unchanged. Do not alter the branch condition, so only the existing living pulled passenger train behavior is affected.

- [ ] **Step 3: Run the focused tests and verify the green result**

Run the same focused command:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest --no-restore
```

Expected: both cloak-preservation tests pass, the user and passenger arrive at the destination, and the existing relay tests in `YautjaPredatorRoleTest` remain green.

### Task 3: Full verification and handoff

**Files:**
- Inspect: `Content.Server/_CMU14/Yautja/YautjaItemSystem.cs`
- Inspect: `Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs`

- [ ] **Step 1: Check the diff for scope and formatting**

Run:

```powershell
git diff --check
git diff -- Content.Server/_CMU14/Yautja/YautjaItemSystem.cs Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs
```

Confirm the diff contains only the two removed relay `ForceDecloak` calls and the two test renames/assertion updates.

- [ ] **Step 2: Build the integration-test project**

Run:

```powershell
dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore
```

Expected: exit code 0 with no compile errors.

- [ ] **Step 3: Run the focused Yautja integration fixture again**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest --no-restore
```

Expected: the complete fixture passes, including both cloak-preservation regressions.

- [ ] **Step 4: Review the final working-tree status**

Run:

```powershell
git status --short
```

Report only the files changed for this task and leave unrelated pre-existing user modifications untouched.
