# Dense Fog Dialog Serialization Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent the Yautja hunting-grounds dense-fog interaction from crashing the server during dialog replication.

**Architecture:** Keep the existing `YautjaPreserveEdgeSystem` dialog and event flow unchanged. Mark its shared choice event as both CLR-serializable and network-serializable, then cover that serialization contract with a focused shared test. Verify the connected runtime path separately because the current integration harness fails during client startup on pre-existing duplicate-localization errors.

**Tech Stack:** C#, RobustToolbox networking/serialization, NUnit integration tests, `dotnet test`.

## Global Constraints

- Change only the dense-fog crash path; do not alter escape eligibility, text, delay, or role restrictions.
- Preserve unrelated user worktree changes.
- The regression test must cover the event's serialization contract and must fail before the production fix.

---

### Task 1: Add the failing dense-fog serialization-contract test

**Files:**
- Create: `Content.Tests/Shared/_CMU14/Yautja/YautjaHuntEventsTest.cs`
- Test: `Content.Tests/Shared/_CMU14/Yautja/YautjaHuntEventsTest.cs`

**Interfaces:**
- Consumes: `YautjaPreserveEscapeChoiceEvent` and the RobustToolbox serialization attributes.
- Produces: a regression test named `PreserveEscapeChoiceIsNetworkSerializable` that fails when the event cannot be registered for dialog-state serialization.

- [ ] **Step 1: Write the failing test**

Create a unit test that checks `YautjaPreserveEscapeChoiceEvent` has both `SerializableAttribute` and `NetSerializableAttribute`.

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --property:UseSharedCompilation=false --maxcpucount:1 --filter FullyQualifiedName~YautjaHuntEventsTest.PreserveEscapeChoiceIsNetworkSerializable
```

Expected: FAIL because both serialization attributes are absent.

### Task 2: Register the shared choice event

**Files:**
- Modify: `Content.Shared/_CMU14/Yautja/YautjaHuntEvents.cs:19`

**Interfaces:**
- Consumes: the existing `YautjaPreserveEscapeChoiceEvent(NetEntity User, bool Escape)` payload.
- Produces: a type registered with the same serialization metadata as the other dialog choice events.

- [ ] **Step 1: Add the minimal implementation**

Place `[Serializable, NetSerializable]` immediately above `YautjaPreserveEscapeChoiceEvent`.

- [ ] **Step 2: Run the focused regression test**

Run the focused command from Task 1. Expected: PASS, with both serialization attributes discovered on the event.

- [ ] **Step 3: Run targeted Yautja tests and builds**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~YautjaPreserveConsoleTest
dotnet build Content.Server/Content.Server.csproj --configuration Debug --no-restore --property:UseSharedCompilation=false /m:1 --verbosity:minimal
dotnet build Content.Client/Content.Client.csproj --configuration Debug --no-restore --property:UseSharedCompilation=false /m:1 --verbosity:minimal
```

Expected: all targeted tests pass and both builds finish with zero errors.

- [ ] **Step 4: Verify the worktree diff**

Run `git diff --check` and inspect `git diff -- Content.Shared/_CMU14/Yautja/YautjaHuntEvents.cs Content.Tests/Shared/_CMU14/Yautja/YautjaHuntEventsTest.cs`. Confirm no gameplay logic or unrelated files changed.

- [ ] **Step 5: Verify the runtime path**

Start the freshly built server and client, open the dense-fog interaction in the hunting grounds, and confirm the dialog opens without a fatal `YautjaPreserveEscapeChoiceEvent` PVS serialization error. Treat duplicate-localization messages as the known unrelated warning described above.
