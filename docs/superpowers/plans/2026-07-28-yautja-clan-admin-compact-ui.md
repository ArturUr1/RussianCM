# Yautja Clan Admin Compact UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Reorganize the Yautja clan admin window into a compact 760x560 two-pane layout while preserving all existing operations, selectors, and tooltips.

**Architecture:** Keep `YautjaClanAdminWindow` as the single view and preserve its event callbacks and editor state. Replace the current vertical root with a horizontal split: a narrow left pane for the clan form and player actions, and a right pane for the scrollable clan list plus status footer.

**Tech Stack:** C#, RobustToolbox UI controls, localization FTL files, NUnit tests, .NET 10.

## Global Constraints

- Preserve create, edit, cancel, delete confirmation, membership, rank, whitelist, inspect, and refresh callback payloads.
- Keep selector choices and persisted selections unchanged.
- Keep detailed guidance in tooltips; remove long hint blocks from the main visual flow.
- Verify the focused Yautja admin tests and a zero-error client build.

---

### Task 1: Define the compact layout contract

**Files:**
- Modify: `Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminWindowTest.cs`

**Interfaces:**
- Produces a test contract that the window default size is compact and the tooltip helper remains available.

- [ ] **Step 1: Add a failing size assertion**

Add a test that constructs the window and asserts `SetSize.X <= 760` and `SetSize.Y <= 560`, while retaining the existing tooltip test.

- [ ] **Step 2: Run the focused test**

Run `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdmin"`.
Expected: FAIL because the current window is 900x760.

- [ ] **Step 3: Commit the contract**

```powershell
git add Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminWindowTest.cs
git commit -m "test: define compact Yautja clan admin window contract"
```

### Task 2: Implement the two-pane compact UI

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs`

**Interfaces:**
- Consumes the existing `_editor`, callbacks, localization keys, and `YautjaBracerUiStyle` helpers.
- Produces the same event wiring and `UpdateState` behavior in a compact layout.

- [ ] **Step 1: Set compact window dimensions**

Use a default size around `new Vector2(760, 560)` and a smaller usable `MinSize`, keeping `Resizable = true`.

- [ ] **Step 2: Replace the vertical root with a horizontal split**

Create a horizontal root with two expanding child panes. Put the clan form and player operations in the left pane; put the scrollable clan list, status label, and refresh button in the right pane.

- [ ] **Step 3: Collapse field rows without changing callbacks**

Keep visible labels and single-line controls, but remove the long `CreateHint` blocks and excess section padding. Preserve every existing button callback and selector construction.

- [ ] **Step 4: Keep list scrolling and row actions compact**

Retain the independent `ScrollContainer`; make each clan row a tight horizontal card with compact edit/delete buttons. Keep delete confirmation and edit population unchanged.

- [ ] **Step 5: Run the focused test**

Run `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdmin"`.
Expected: PASS, including the new size contract and all existing selector/tooltip tests.

- [ ] **Step 6: Commit the UI change**

```powershell
git add Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs
git commit -m "feat: compact Yautja clan admin layout"
```

### Task 3: Build and runtime smoke check

**Files:**
- No source changes expected.

**Interfaces:**
- Validates the compiled client and runtime startup against the existing server.

- [ ] **Step 1: Build the client**

Run `dotnet build Content.Client/Content.Client.csproj --no-restore`.
Expected: exit 0 with zero errors.

- [ ] **Step 2: Start server and client with fresh logs**

Start `bin/Content.Server/Content.Server.exe`, then `bin/Content.Client/Content.Client.exe --connect --connect-address localhost`, capturing stdout/stderr to `server-run.log`, `server-run.err`, `client-run.log`, and `client-run.err`.

- [ ] **Step 3: Verify connection and feature-specific logs**

Confirm both processes remain responsive, UDP port 1212 is owned by the server, and the client log contains handshake completion plus `Runlevel changed to: InGame`. Confirm both stderr files are empty and there are no `error`, `exception`, `yautja`, `clan`, `tooltip`, or `admin` failures.

- [ ] **Step 4: Review the diff**

Run `git diff --check HEAD~2..HEAD` and inspect the compact window diff for accidental callback or selector changes.
