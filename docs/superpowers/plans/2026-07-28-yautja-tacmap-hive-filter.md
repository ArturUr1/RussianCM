# Hunter Ship Tactical Map Hive Filter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Filter xenonid tactical-map data so Alpha and Forsaken users see only their own hive's xenonid and structure blips.

**Architecture:** Preserve the shared `TacticalMapComponent` as the authoritative global map. Filter only the per-user copies created by `TacticalMapSystem.UpdateUserData`, using `HiveMemberComponent.Hive` identity rather than NPC faction. Add an integration regression test around the real `UpdateUserData` path.

**Tech Stack:** C#, RobustToolbox ECS, NUnit integration tests, `dotnet test`.

## Global Constraints

- Do not change `Watch Xenonid`, `HiveTracker`, or psychic communication checks; they already use same-hive validation.
- Do not mutate global `TacticalMapComponent.XenoBlips` or `XenoStructureBlips` while filtering a user.
- Preserve unrelated uncommitted files in the main `fix/yautja` worktree.
- Validate the change with a failing test before production code and a passing targeted integration test afterward.

---

### Task 1: Add the failing tactical-map isolation test

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Xenonids/CMUIndependentXenoHiveTest.cs`
- Test: `Content.IntegrationTests/_CMU14/Xenonids/CMUIndependentXenoHiveTest.cs`

**Interfaces:**
- Consumes: `XenoHiveSystem.CreateHive`, `XenoHiveSystem.SetHive`, `TacticalMapSystem.UpdateUserData`, `TacticalMapComponent`, and `TacticalMapUserComponent`.
- Produces: A regression test proving both hive directions are isolated.

- [ ] **Step 1: Write the failing test**

Add `HunterShipTacticalMapOnlyShowsOwnHive` to the existing Hunter Ship hive fixture. Create separate Alpha and Forsaken hives, spawn one `CMXenoParasite` and one hive-owned `CMXenoHive` structure for each, assign each entity to its hive, and ensure a tactical map exists. Create one tactical-map user component per xeno with `Xenos = true`, call `TacticalMapSystem.UpdateUserData`, and assert each user has its own xeno and structure IDs but not the other hive's IDs.

- [ ] **Step 2: Run the focused test to verify RED**

Run from the isolated worktree:

```powershell
dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --verbosity normal --filter FullyQualifiedName~Content.IntegrationTests._CMU14.Xenonids.CMUIndependentXenoHiveTest.HunterShipTacticalMapOnlyShowsOwnHive -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed
```

Expected: the test fails because `UpdateUserData` currently copies both hive dictionaries unchanged.

### Task 2: Implement per-user hive filtering

**Files:**
- Modify: `Content.Server/_RMC14/TacticalMap/TacticalMapSystem.cs:1670-1690`

**Interfaces:**
- Consumes: The user entity, its hive membership, and the global xeno blip dictionaries.
- Produces: Per-user `XenoBlips` and `XenoStructureBlips` containing only same-hive entities.

- [ ] **Step 1: Add the minimal filter**

In the `user.Comp.Xenos` branch, copy the selected live/last-update dictionaries as before, then remove entries whose entity does not have a `HiveMemberComponent` pointing to the user's hive. Keep the user's own xeno blip for a hive-less user; do not add foreign structure blips. Do not mutate `map.XenoBlips` or `map.XenoStructureBlips`.

- [ ] **Step 2: Run the focused test to verify GREEN**

Run the same focused command from Task 1. Expected: the test passes with both Alpha and Forsaken assertions.

### Task 3: Verify the fixture and source hygiene

**Files:**
- No additional source files.

- [ ] **Step 1: Run the complete Hunter Ship xenonid fixture**

```powershell
dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --verbosity normal --filter FullyQualifiedName~Content.IntegrationTests._CMU14.Xenonids.CMUIndependentXenoHiveTest -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed
```

Expected: all discovered tests pass.

- [ ] **Step 2: Check the diff**

```powershell
git diff --check
git status --short --branch
```

Expected: only the tactical-map source and regression-test files are modified in the isolated worktree.

- [ ] **Step 3: Commit the implementation**

```powershell
git add Content.Server/_RMC14/TacticalMap/TacticalMapSystem.cs Content.IntegrationTests/_CMU14/Xenonids/CMUIndependentXenoHiveTest.cs
git commit -m "fix: isolate xenonid tactical maps by hive"
```
