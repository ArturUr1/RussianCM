# Dense Fog Dialog Serialization Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent the Yautja hunting-grounds dense-fog interaction from crashing the server during dialog replication.

**Architecture:** Keep the existing `YautjaPreserveEdgeSystem` dialog and event flow unchanged. Mark its shared choice event as both CLR-serializable and network-serializable, then exercise the real interaction through a connected integration-test pair so the same PVS boundary that crashed in runtime is covered.

**Tech Stack:** C#, RobustToolbox networking/serialization, NUnit integration tests, `dotnet test`.

## Global Constraints

- Change only the dense-fog crash path; do not alter escape eligibility, text, delay, or role restrictions.
- Preserve unrelated user worktree changes.
- The regression test must run the real server/client interaction and must fail before the production fix.

---

### Task 1: Add the failing dense-fog replication test

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaPreserveConsoleTest.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaPreserveConsoleTest.cs`

**Interfaces:**
- Consumes: `YautjaPreserveEdgeSystem` via `InteractHandEvent`, `DialogComponent`, and `TestPair.ReallyBeIdle`.
- Produces: a regression test named `PreserveEdgeDialogReplicatesWithoutCrashingServer` that proves the dense-fog dialog can be replicated by a connected client.

- [ ] **Step 1: Write the failing test**

Create a connected dirty `TestPair`, create a test map, spawn a human and `CMUYautjaHuntingGroundPreserveEdge`, attach the test player to the human, raise `InteractHandEvent`, assert the dialog was created, and run enough synchronized ticks for PVS replication. Assert `server.IsAlive` and `client.IsAlive` after the idle period. Clean up the spawned entities in `finally`.

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~YautjaPreserveConsoleTest.PreserveEdgeDialogReplicatesWithoutCrashingServer
```

Expected: FAIL because the server cannot serialize `YautjaPreserveEscapeChoiceEvent` and reports `Type not found Content.Shared._CMU14.Yautja.YautjaPreserveEscapeChoiceEvent`.

### Task 2: Register the shared choice event

**Files:**
- Modify: `Content.Shared/_CMU14/Yautja/YautjaHuntEvents.cs:19`

**Interfaces:**
- Consumes: the existing `YautjaPreserveEscapeChoiceEvent(NetEntity User, bool Escape)` payload.
- Produces: a type registered with the same serialization metadata as the other dialog choice events.

- [ ] **Step 1: Add the minimal implementation**

Place `[Serializable, NetSerializable]` immediately above `YautjaPreserveEscapeChoiceEvent`.

- [ ] **Step 2: Run the focused regression test**

Run the focused command from Task 1. Expected: PASS, with the dialog replicated and both pair processes still alive.

- [ ] **Step 3: Run targeted Yautja tests and builds**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~YautjaPreserveConsoleTest
dotnet build Content.Server/Content.Server.csproj --configuration Debug --no-restore --property:UseSharedCompilation=false /m:1 --verbosity:minimal
dotnet build Content.Client/Content.Client.csproj --configuration Debug --no-restore --property:UseSharedCompilation=false /m:1 --verbosity:minimal
```

Expected: all targeted tests pass and both builds finish with zero errors.

- [ ] **Step 4: Verify the worktree diff**

Run `git diff --check` and inspect `git diff -- Content.Shared/_CMU14/Yautja/YautjaHuntEvents.cs Content.IntegrationTests/_CMU14/Yautja/YautjaPreserveConsoleTest.cs`. Confirm no gameplay logic or unrelated files changed.
