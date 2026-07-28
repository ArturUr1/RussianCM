# Yautja Recall Action Deduplication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Russian-localized `CMUActionYautjaRecall` the only visible smart-disc recall action on a worn Yautja bracer.

**Architecture:** Keep the existing `YautjaRecallSystem` path as the player-facing recall behavior. Remove only the `CallDiscAction` grant from `YautjaPowerSystem`; preserve the legacy `CallDisc` prototype/event/handler so manually serialized or compatibility paths remain loadable. Update the worn-bracer roster tests to assert the new visible action surface.

**Tech Stack:** C# 10/.NET, RobustToolbox ECS, NUnit integration tests, YAML entity prototypes, Fluent localization.

## Global Constraints

- The visible action must be `CMUActionYautjaRecall`, which has Russian localization.
- `CMUActionYautjaCallDisc` must not be granted by the standard worn-bracer action path.
- Preserve smart-disc recall behavior, power/range/ownership rules, and the legacy CallDisc prototype/event/server handler.
- Do not change unrelated bracer actions or unrelated dirty worktree changes.

---

### Task 1: Remove duplicate grant and update action-roster coverage

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs:5453-7365`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaPowerSystem.cs:128-131`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs`

**Interfaces:**
- Consumes: `YautjaBracerComponent.CallDiscActionId` and `YautjaBracerComponent.RecallActionId` from the existing component.
- Produces: worn hunter bracers grant `CMUActionYautjaRecall` but not `CMUActionYautjaCallDisc`; direct `YautjaCallDiscActionEvent` compatibility behavior remains available.

- [ ] **Step 1: Write the failing roster assertions**

In `BracerTrackerActionsAreGrantedToWornHunterBracer`, replace the positive `CallDisc` assertion with an explicit absence assertion while retaining the positive `Recall` assertion:

```csharp
Assert.That(actionIds, Does.Contain("CMUActionYautjaRecall"));
Assert.That(actionIds, Does.Not.Contain("CMUActionYautjaCallDisc"));
```

In `BracerRecallActionsRemainDistinct`, rename the test to `BracerRecallActionIsLocalizedAndSoleDiscAction` and assert that the worn roster contains only the localized action:

```csharp
Assert.That(actionNames, Contains.Key("CMUActionYautjaRecall"));
Assert.That(actionNames, Does.Not.ContainKey("CMUActionYautjaCallDisc"));
```

Verify the Russian entity localization separately with this static check:

```powershell
rg -n '^ent-CMUActionYautjaRecall\s*=' Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl
```

Keep `CallDiscActionRecallsNearbySmartDiscWithCmss13RangeAndPower` unchanged so the preserved legacy event handler continues to have direct behavior coverage.

- [ ] **Step 2: Run the focused roster test to verify RED**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest.BracerTrackerActionsAreGrantedToWornHunterBracer|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest.BracerRecallActionIsLocalizedAndSoleDiscAction" -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed
```

Expected: the roster assertion fails because the current worn-bracer grant still contains `CMUActionYautjaCallDisc`. If an unrelated process locks `bin/Content.Server/Content.Server.exe`, record that external test-host failure and use the static roster diff as the RED evidence.

- [ ] **Step 3: Remove only the duplicate action grant**

In `YautjaPowerSystem.OnGetItemActions`, delete this line from the worn-bracer branch:

```csharp
AddAction(ent.Comp, ref args, ref ent.Comp.CallDiscAction, ent.Comp.CallDiscActionId);
```

Leave these lines and all legacy CallDisc definitions untouched:

```csharp
AddAction(ent.Comp, ref args, ref ent.Comp.RecallAction, ent.Comp.RecallActionId);
```

- [ ] **Step 4: Run the focused roster/static checks to verify GREEN**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest.BracerTrackerActionsAreGrantedToWornHunterBracer|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest.BracerRecallActionIsLocalizedAndSoleDiscAction" -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed
git diff --check
```

Expected: focused tests pass, or the only failure is the previously identified external `Content.Server.exe` lock; `git diff --check` reports no whitespace errors in task files.

- [ ] **Step 5: Build the affected projects**

Run:

```powershell
dotnet build Content.Server/Content.Server.csproj --no-restore --nologo -m:1 /p:BuildInParallel=false /p:UseAppHost=false /p:RunAnalyzers=false
dotnet build Content.Client/Content.Client.csproj --no-restore --nologo -m:1 /p:BuildInParallel=false /p:UseAppHost=false /p:RunAnalyzers=false
```

Expected: both builds exit `0` with zero errors. Existing repository warnings are acceptable and must be reported if present.

- [ ] **Step 6: Commit the implementation**

Stage only the task-owned changes and commit:

```powershell
git add Content.Shared/_CMU14/Yautja/YautjaPowerSystem.cs Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs
git commit -m "fix: deduplicate Yautja disc recall action"
```
