# Yautja Clan Admin Roster Actions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add immediate per-player clan removal and whitelist-clearing actions to the Yautja clan admin roster, plus a separate clanless-player list with whitelist-clearing actions.

**Architecture:** Reuse `YautjaClanAdminMemberState` for clan members and clanless records, add `ClanlessPlayers` to the shared EUI snapshot, and send typed actions addressed by `NetUserId`. The server loads all persisted Yautja clan-member rows once per refresh, groups them by `ClanId`, mutates the database under the existing EUI operation gate, invalidates caches, and republishes state. The client renders compact action buttons inside existing bounded scroll containers and rebuilds rows after every server response.

**Tech Stack:** C#/.NET 10, Robust EUI net serialization, Avalonia/Robust UI controls, NUnit `Content.Tests`, Fluent localization files.

## Global Constraints

- Actions are immediate and never open a confirmation dialog.
- Remove-member preserves the persisted rank, permissions, honor, and legacy flag while setting `ClanId = null`.
- Clear-whitelist sets all Yautja whitelist flags to `YautjaWhitelistFlags.None` and does not change clan membership.
- The clanless list contains persisted Yautja clan-member records with `ClanId == null`; arbitrary player accounts without a Yautja record are not enumerated.
- Existing compact layout, outer window scrolling, clan roster scrolling, mutation gate, cache invalidation, and unrelated working-tree changes must remain intact.

---

### Task 1: Extend the shared EUI state and action protocol

**Files:**
- Modify: `Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs`
- Test: `Content.Tests/Shared/_CMU14/Yautja/YautjaClanAdminEuiStateTest.cs`

**Interfaces:**
- `YautjaClanAdminEuiState` gains `List<YautjaClanAdminMemberState> ClanlessPlayers` and accepts an optional list in its constructor.
- Add `[Serializable, NetSerializable]` `YautjaClanAdminRemoveMemberMessage(NetUserId playerId)` with `NetUserId PlayerId`.
- Add `[Serializable, NetSerializable]` `YautjaClanAdminClearWhitelistMessage(NetUserId playerId)` with `NetUserId PlayerId`.

- [ ] **Step 1: Write the failing shared-state and message tests**

Add a test that constructs a state with one clanless member and asserts `ClanlessPlayers` preserves the id, name, rank, and online state. Add a second test that constructs both new messages and asserts their `PlayerId` values are retained.

```csharp
[Test]
public void StateRetainsClanlessPlayersAndActionTargets()
{
    var playerId = new NetUserId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    var player = new YautjaClanAdminMemberState(playerId, "Unsworn", YautjaRank.Blooded, false);
    var state = new YautjaClanAdminEuiState([], "", "", "", 0, null, YautjaClanAdminMutationKind.None, [player]);
    var remove = new YautjaClanAdminRemoveMemberMessage(playerId);
    var clear = new YautjaClanAdminClearWhitelistMessage(playerId);

    Assert.Multiple(() =>
    {
        Assert.That(state.ClanlessPlayers[0].PlayerId, Is.EqualTo(playerId));
        Assert.That(state.ClanlessPlayers[0].Name, Is.EqualTo("Unsworn"));
        Assert.That(remove.PlayerId, Is.EqualTo(playerId));
        Assert.That(clear.PlayerId, Is.EqualTo(playerId));
    });
}
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --disable-build-servers --filter "FullyQualifiedName~YautjaClanAdminEuiStateTest"
```

Expected: compile failure because `ClanlessPlayers` and the two typed messages do not exist yet.

- [ ] **Step 3: Implement the minimal shared protocol**

Add the optional constructor parameter and property, defaulting null to an empty list, then add both typed EUI messages with `NetUserId PlayerId` properties.

- [ ] **Step 4: Run the focused test to verify it passes**

Run the same command and expect all shared-state tests to pass.

- [ ] **Step 5: Commit**

```powershell
git add Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs Content.Tests/Shared/_CMU14/Yautja/YautjaClanAdminEuiStateTest.cs
git commit -m "feat: add Yautja roster action protocol"
```

### Task 2: Populate clanless records and implement server actions

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs`
- Test: `Content.Tests/Server/_CMU14/Yautja/YautjaClanAdminEuiTest.cs`

**Interfaces:**
- Add internal helper `RemoveFromClan(YautjaClanMemberRecord member)` returning the same record with `ClanId = null` and all other fields unchanged.
- Add internal helper `IsClanless(YautjaClanMemberRecord member)` returning `member.ClanId == null` for deterministic filtering tests.
- Add `RemoveMember` and `ClearWhitelist` message cases to the existing EUI switch.

- [ ] **Step 1: Write failing server tests**

Add tests proving removal preserves rank, permissions, honor, and legacy state while clearing only `ClanId`, and proving the clanless predicate accepts null and rejects a populated clan id.

```csharp
[Test]
public void RemoveFromClanPreservesMemberData()
{
    var source = new YautjaClanMemberRecord(
        Guid.Parse("44444444-4444-4444-4444-444444444444"), 12, 5, 11, 42, true);
    var detached = YautjaClanAdminEui.RemoveFromClan(source);

    Assert.Multiple(() =>
    {
        Assert.That(detached.ClanId, Is.Null);
        Assert.That(detached.PlayerUserId, Is.EqualTo(source.PlayerUserId));
        Assert.That(detached.Rank, Is.EqualTo(source.Rank));
        Assert.That(detached.Permissions, Is.EqualTo(source.Permissions));
        Assert.That(detached.Honor, Is.EqualTo(source.Honor));
        Assert.That(detached.IsLegacy, Is.EqualTo(source.IsLegacy));
    });
}

[Test]
public void IsClanlessOnlyAcceptsRecordsWithoutClan()
{
    Assert.That(YautjaClanAdminEui.IsClanless(new YautjaClanMemberRecord(Guid.NewGuid(), null, 2, 3, 0, false)), Is.True);
    Assert.That(YautjaClanAdminEui.IsClanless(new YautjaClanMemberRecord(Guid.NewGuid(), 9, 2, 3, 0, false)), Is.False);
}
```

- [ ] **Step 2: Run the focused tests to verify they fail**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --disable-build-servers --filter "FullyQualifiedName~YautjaClanAdminEuiTest"
```

Expected: compile failure because the helpers and message handlers are missing.

- [ ] **Step 3: Implement one-pass refresh grouping**

In `RefreshStateAsync`, fetch `GetYautjaClanMembersAsync()` once, map each record to a display state, use `Where(record => record.ClanId == clan.Id)` for each active clan, and map `ClanId == null` records into a sorted `clanlessPlayers` list. Pass that list to the EUI state constructor. Preserve rank-descending/name-ascending sorting.

- [ ] **Step 4: Implement remove-member behavior**

Handle `YautjaClanAdminRemoveMemberMessage` by loading the record, returning a localized “not a clan member” status when missing/already clanless, upserting `RemoveFromClan(existing)`, invalidating `_clanManager` and `_rankManager` caches, setting a localized success status, and writing an admin log entry with the target display name/id.

- [ ] **Step 5: Implement clear-whitelist behavior**

Handle `YautjaClanAdminClearWhitelistMessage` by calling `SetYautjaWhitelistFlagsAsync(target.UserId, (int) YautjaWhitelistFlags.None)`, invalidating both caches, setting a localized success status, and writing an admin log entry. Let the existing EUI exception handler report unexpected database failures.

- [ ] **Step 6: Run the focused tests to verify they pass**

Run the same server test command and expect all server helper tests to pass.

- [ ] **Step 7: Commit**

```powershell
git add Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs Content.Tests/Server/_CMU14/Yautja/YautjaClanAdminEuiTest.cs
git commit -m "feat: handle Yautja roster actions on server"
```

### Task 3: Wire typed actions through the client EUI and render compact controls

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/YautjaClanAdminEui.cs`
- Modify: `Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs`
- Test: `Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminWindowTest.cs`

**Interfaces:**
- Add `event Action<NetUserId>? OnRemoveMember` and `event Action<NetUserId>? OnClearWhitelist` to the window.
- Add matching EUI handlers that send the two typed messages.
- Add `public const int ClanlessMaxHeight = 160` for the bounded clanless section.

- [ ] **Step 1: Write failing client layout/action tests**

Add tests for the new height bound and for a pure target helper used by row callbacks:

```csharp
[Test]
public void ClanlessScrollHeightIsBounded()
{
    Assert.That(YautjaClanAdminWindow.ClanlessMaxHeight, Is.LessThanOrEqualTo(220));
}

[Test]
public void RosterActionTargetUsesMemberId()
{
    var id = new NetUserId(Guid.Parse("55555555-5555-5555-5555-555555555555"));
    var member = new YautjaClanAdminMemberState(id, "Target", YautjaRank.Blooded, true);

    Assert.That(YautjaClanAdminWindow.GetRosterActionTarget(member), Is.EqualTo(id));
}
```

- [ ] **Step 2: Run the focused client tests to verify they fail**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --disable-build-servers --filter "FullyQualifiedName~YautjaClanAdminWindowTest"
```

Expected: compile failure because the new constant/helper and events are missing.

- [ ] **Step 3: Add client EUI event forwarding**

Subscribe/unsubscribe the two window events in `YautjaClanAdminEui`, add `OnRemoveMember` and `OnClearWhitelist` handlers, and send `YautjaClanAdminRemoveMemberMessage` / `YautjaClanAdminClearWhitelistMessage` with the supplied `NetUserId`.

- [ ] **Step 4: Add member-row actions**

Convert the existing roster row wrapper into a horizontal row containing the current member label and two compact buttons. The remove button invokes `OnRemoveMember` with `member.PlayerId`; the whitelist button invokes `OnClearWhitelist` with the same id. Keep the existing hot-red style for removal and use the neutral row style for whitelist clearing.

- [ ] **Step 5: Add the clanless section**

After rendering clan cards, append a localized “players without a clan” header and a bounded `ScrollContainer` with `ClanlessMaxHeight`. Render the same member label for each row and only the whitelist-clearing button. Render a localized empty-state label when the collection is empty.

- [ ] **Step 6: Run focused client tests to verify they pass**

Run the same client test command and expect all existing and new tests to pass.

- [ ] **Step 7: Commit**

```powershell
git add Content.Client/_CMU14/Yautja/YautjaClanAdminEui.cs Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminWindowTest.cs
git commit -m "feat: add Yautja roster action buttons"
```

### Task 4: Add localization and verify the integrated feature

**Files:**
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/admin.ftl`

**Interfaces:**
- Add localized labels/tooltips for remove-member, clear-whitelist, clanless header, empty state, success status, and already-clanless failure status.

- [ ] **Step 1: Add Russian and English localization keys**

Add these keys in both locale files:

```text
cmu-yautja-clan-admin-remove-member
cmu-yautja-clan-admin-remove-member-tooltip
cmu-yautja-clan-admin-clear-whitelist
cmu-yautja-clan-admin-clear-whitelist-tooltip
cmu-yautja-clan-admin-clanless-header
cmu-yautja-clan-admin-clanless-empty
cmu-yautja-clan-admin-member-removed
cmu-yautja-clan-admin-whitelist-cleared
cmu-yautja-clan-admin-member-not-found
```

- [ ] **Step 2: Run the focused integrated test set**

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --disable-build-servers --filter "FullyQualifiedName~YautjaClanAdmin"
```

Expected: all focused Yautja clan-admin tests pass.

- [ ] **Step 3: Build client and server**

```powershell
dotnet build Content.Client/Content.Client.csproj --no-restore --disable-build-servers
dotnet build Content.Server/Content.Server.csproj --no-restore --disable-build-servers
```

Expected: zero errors; existing warnings may remain.

- [ ] **Step 4: Run the complete test suite**

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --no-build
```

Expected: zero failures.

- [ ] **Step 5: Smoke-test the running server and client**

Start the server on port 1212 and the client with `--connect --connect-address localhost:1212`. Verify the server reaches `Ready`, the socket listens on TCP/UDP 1212, and the client logs `Handshake completed`, `Runlevel changed to: InGame`, and `GameplayState`. Scan both stdout/stderr for `ERROR`, `FATL`, `Unhandled`, `NullReference`, or `System.Exception`; report unrelated performance/late-message warnings separately.

- [ ] **Step 6: Commit localization and verification-ready state**

```powershell
git add Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl Resources/Locale/en-US/_CMU14/yautja/admin.ftl
git commit -m "feat: localize Yautja roster actions"
```
