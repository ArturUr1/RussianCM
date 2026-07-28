# Yautja Clan Admin Roster Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add expandable clan rosters with localized player ranks and robust scrolling to the compact Yautja clan admin window.

**Architecture:** Extend the existing serialized admin snapshot with a small member DTO. The server enriches each clan with member names and online state during refresh; the client renders an inline accordion inside the existing clan-list scroll area, with a bounded nested roster scroll and one expanded clan at a time.

**Tech Stack:** C#, RobustToolbox EUI serialization, EF-backed server database records, client UI controls, Fluent localization, NUnit, .NET 10.

## Global Constraints

- Preserve existing clan mutation callbacks, state versioning, edit/delete behavior, and selector synchronization.
- Keep the compact two-pane window and its resizable behavior.
- Use localized rank names and RU/EN strings for roster controls and status labels.
- Keep detailed guidance in tooltips; add a tooltip to the roster toggle.

---

### Task 1: Add the serialized roster contract

**Files:**
- Modify: `Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs`
- Test: `Content.Tests/Shared/_CMU14/Yautja/YautjaClanAdminEuiStateTest.cs`

**Interfaces:**
- Produces `YautjaClanAdminMemberState(NetUserId playerId, string name, YautjaRank rank, bool online)`.
- Extends `YautjaClanAdminClanState` with `List<YautjaClanAdminMemberState> Members`.

- [ ] **Step 1: Write the failing state test**

Create a test that constructs a member and clan state, then asserts the member id, name, rank, online flag, and clan member list are retained.

- [ ] **Step 2: Run the state test**

Run `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminEuiState"`.
Expected: FAIL to compile because the member DTO and constructor parameter do not yet exist.

- [ ] **Step 3: Implement the shared DTO and clan property**

Add `[Serializable, NetSerializable]` to the new member state, use `Robust.Shared.Network.NetUserId`, and add a `List<YautjaClanAdminMemberState>` constructor parameter/property to `YautjaClanAdminClanState`. Update existing constructor call sites with `[]` until the server task supplies real members.

- [ ] **Step 4: Run the state test**

Run the same focused command; expected result is PASS.

- [ ] **Step 5: Commit**

```powershell
git add Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs Content.Tests/Shared/_CMU14/Yautja/YautjaClanAdminEuiStateTest.cs
git commit -m "feat: add Yautja clan admin roster state"
```

### Task 2: Populate roster data on the server

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs`
- Test: `Content.Tests/Server/_CMU14/Yautja/YautjaClanAdminEuiTest.cs`

**Interfaces:**
- Consumes `YautjaClanManager.SanitizeStoredRank`, `IServerDbManager.GetYautjaClanMembersAsync(int?)`, and `IPlayerManager`.
- Produces each clan snapshot with members sorted by rank descending and display name ascending.

- [ ] **Step 1: Add a server-side mapping test/fixture expectation**

Create a test for the pure `YautjaClanAdminEui.ToMemberState(YautjaClanMemberRecord, string, bool)` helper. Cover that the returned member keeps the id and display name, carries the online flag, and sanitizes an out-of-range stored rank to `YautjaRank.Blooded`.

- [ ] **Step 2: Run the server-focused test**

Run `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminEui"`.
Expected: FAIL until roster mapping is implemented.

- [ ] **Step 3: Inject player lookup and build member states**

Add `[Dependency] private IPlayerManager _players`, fetch members once per clan refresh, map `PlayerUserId` to `NetUserId`, resolve the name with `GetPlayerName`, compute online state with `TryGetSessionById`, and call the tested `ToMemberState` helper. Pass the sorted member list and its count into `YautjaClanAdminClanState`.

- [ ] **Step 4: Run focused server tests**

Run the same command; expected result is PASS.

- [ ] **Step 5: Commit**

```powershell
git add Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs Content.Tests/Server/_CMU14/Yautja/YautjaClanAdminEuiTest.cs
git commit -m "feat: populate Yautja clan admin rosters"
```

### Task 3: Render the accordion and scrolling UI

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs`
- Modify: `Content.Client/_CMU14/Yautja/YautjaClanAdminEui.cs` only if event/state wiring needs an explicit refresh hook
- Test: `Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminWindowTest.cs`

**Interfaces:**
- Consumes `YautjaClanAdminClanState.Members` and the roster localization keys.
- Produces a compact roster toggle, one-open-clan accordion behavior, bounded member scrolling, and retained edit/delete actions.

- [ ] **Step 1: Add failing client contract tests**

Add tests for a bounded roster scroll contract and for the localized rank text helper used by roster rows. Keep the existing size, selector, and tooltip tests unchanged.

- [ ] **Step 2: Run focused client tests**

Run `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminWindow"`.
Expected: FAIL until the new UI helpers exist.

- [ ] **Step 3: Add one-open roster state and toggle**

Track `int? _expandedClanId`. Add a compact toggle button to each clan row, apply the roster tooltip, and rebuild the list when toggled. The toggle must preserve the current expanded id across `UpdateState` when that clan still exists and clear it when the clan disappears.

- [ ] **Step 4: Render the bounded roster**

Under the expanded clan summary, add a `ScrollContainer` with a fixed maximum height and a vertical member list. Render each member as `Name — localized rank`, plus localized online/offline text. Render a localized empty message when the list is empty.

- [ ] **Step 5: Ensure parent window scrolling remains usable**

Keep the right-pane clan list in its existing vertical `ScrollContainer`, ensure nested roster scrolling does not expand the footer away, and preserve the window's `Resizable`, `SetSize`, and `MinSize` values.

- [ ] **Step 6: Run focused client tests**

Run the same command; expected result is PASS.

- [ ] **Step 7: Commit**

```powershell
git add Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminWindowTest.cs
git commit -m "feat: add expandable Yautja clan roster UI"
```

### Task 4: Localize, build, and smoke-test

**Files:**
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/admin.ftl`

- [ ] **Step 1: Add RU/EN roster strings**

Add keys for roster toggle, roster tooltip, member row, empty roster, online, and offline labels.

- [ ] **Step 2: Run all focused tests**

Run `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdmin"`.
Expected: all Yautja clan admin tests pass.

- [ ] **Step 3: Build client and server**

Run `dotnet build Content.Client/Content.Client.csproj --no-restore` and `dotnet build Content.Server/Content.Server.csproj --no-restore`.
Expected: exit 0 with zero errors.

- [ ] **Step 4: Smoke-test runtime**

Start the server and client against localhost, verify UDP 1212, server `Ready`, client handshake, and `Runlevel changed to: InGame`; confirm stderr files are empty and no clan-admin exceptions appear.

- [ ] **Step 5: Review diff**

Run `git diff --check` and inspect the shared/server/client/localization diff for accidental changes outside the roster feature.
