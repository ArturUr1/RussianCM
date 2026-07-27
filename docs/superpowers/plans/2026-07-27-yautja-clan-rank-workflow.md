# Yautja Clan Rank Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with review checkpoints.

**Goal:** Implement the original CMSS13 Yautja clan rank workflow, including persistent clans, permissions, limits, whitelist-driven Ancient status, and a server-authoritative `View Clan Info` menu.

**Architecture:** Keep `YautjaClanMember` as the authoritative source for normal rank, permissions, clan membership, and honor. A shared policy layer evaluates the original CMSS13 rank/permission/limit rules; the server database manager performs atomic mutations and the existing rank/spawn systems consume its resolved result. The EUI is a read/action client for a server-generated snapshot, with every requested mutation validated again on the server.

**Tech Stack:** C#/.NET, RobustToolbox EntitySystem/EUI/Verb APIs, Entity Framework Core SQLite and PostgreSQL migrations, NUnit integration/client tests, existing Yautja rank metadata and RSI assets.

## Global Constraints

- `Young Blood` is a separate special role and must never be persisted as a normal clan rank.
- `Ancient` is not a normal promotion option; only Ancient manager permission or Yautja Leader/Council whitelist status can grant it.
- No client profile, EUI state, icon, or command argument may grant rank, permission, access, gear, or slot bypass.
- Every mutation must reject self-targeting, equal/higher targets, Ancient targets, missing membership, and rank limits before writing.
- Existing `Player.YautjaRank` values must be migrated without silently downgrading players; it remains a compatibility projection, not a new authority.
- Database changes are additive and must include both SQLite and PostgreSQL migrations/snapshots.
- Existing dirty and untracked files, including `cmss13-ref/` and `cmss13-ref-full/`, must not be staged or overwritten.
- No new NuGet dependency is allowed; reuse current EUI, localization, verb, database, and rank-icon infrastructure.

## File Map

- Shared rules and wire types: `Content.Shared/_CMU14/Yautja/YautjaClan.cs`, `YautjaClanInfoEuiState.cs`.
- Database entities and access: `Content.Server.Database/YautjaClanModel.cs`, `Content.Server/Database/ServerDbBase.YautjaClan.cs`, `Content.Server/Database/ServerDbManager.cs`, generated migration files.
- Server authority: `Content.Server/_CMU14/Yautja/YautjaClanManager.cs`, `YautjaClanInfoEui.cs`, `YautjaClanInfoSystem.cs`, and the existing rank/spawn/profile/command files.
- Client menu: `Content.Client/_CMU14/Yautja/YautjaClanInfoEui.cs`, `YautjaClanInfoWindow.cs`.
- Tests: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanPolicyTest.cs`, `YautjaClanPersistenceTest.cs`, `YautjaClanWorkflowTest.cs`, and `Content.Tests/Client/_CMU14/Yautja/YautjaClanInfoWindowTest.cs`.
- Text: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl` and the matching Russian locale file if the existing locale layout provides one.

---

### Task 1: Shared clan permissions, rank policy, and wire models

**Files:**
- Create: `Content.Shared/_CMU14/Yautja/YautjaClan.cs`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanPolicyTest.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaRank.cs` only where the shared rank metadata needs the original selectable-rank distinction.

**Interfaces:**
- Produces `[Flags] enum YautjaClanPermission` with `UserView = 1`, `UserModify = 2`, `AdminView = 4`, `AdminModify = 8`, `AdminMove = 16`, `AdminManager = 32`, `AdminAncient = AdminView | AdminModify | AdminMove`, `UserAll = UserView | UserModify`, and `All = AdminAncient | AdminManager`.
- Produces `YautjaClanMemberSnapshot(NetUserId PlayerId, int? ClanId, YautjaRank Rank, YautjaClanPermission Permissions, bool IsLegacy, int Honor)`.
- Produces `YautjaClanRankRule(YautjaRank Rank, YautjaClanPermission RequiredPermission, int? AbsoluteLimit, int? MembersPerRankLimit)`.
- Produces `YautjaClanPolicy.GetRule`, `GetNormalAssignableRanks`, `CanView`, `CanTarget`, `CanModifyRank`, `CanMove`, and `CanSetAncient`.
- Produces serializable `YautjaClanViewMember` and `YautjaClanAction` values for the later EUI state.

- [ ] **Step 1: Write the failing policy tests**

Add focused NUnit cases that call the real policy API:

```csharp
[TestCase(YautjaRank.Unblooded, YautjaClanPermission.AdminModify, null, null)]
[TestCase(YautjaRank.Blooded, YautjaClanPermission.UserModify, null, null)]
[TestCase(YautjaRank.Elite, YautjaClanPermission.UserModify, 5, null)]
[TestCase(YautjaRank.Elder, YautjaClanPermission.UserModify, null, 12)]
[TestCase(YautjaRank.Leader, YautjaClanPermission.AdminModify | YautjaClanPermission.UserAll, 1, null)]
[TestCase(YautjaRank.Ancient, YautjaClanPermission.AdminAncient, null, null)]
public void RankRulesMatchCmss13(YautjaRank rank, YautjaClanPermission permission, int? absolute, int? perRank)
{
    var rule = YautjaClanPolicy.GetRule(rank);
    Assert.That(rule.RequiredPermission, Is.EqualTo(permission));
    Assert.That(rule.AbsoluteLimit, Is.EqualTo(absolute));
    Assert.That(rule.MembersPerRankLimit, Is.EqualTo(perRank));
}

[Test]
public void ActorCannotTargetSelfOrEqualOrHigherRank()
{
    var leader = Member(1, YautjaRank.Leader, YautjaClanPermission.UserAll | YautjaClanPermission.AdminModify);
    Assert.That(YautjaClanPolicy.CanTarget(leader, leader), Is.False);
    Assert.That(YautjaClanPolicy.CanTarget(leader, Member(2, YautjaRank.Leader, YautjaClanPermission.UserAll)), Is.False);
    Assert.That(YautjaClanPolicy.CanTarget(leader, Member(3, YautjaRank.Ancient, YautjaClanPermission.AdminAncient)), Is.False);
}

[Test]
public void NormalRankOptionsExcludeYoungBloodAndAncient()
{
    var options = YautjaClanPolicy.GetNormalAssignableRanks();
    Assert.That(options, Does.Not.Contain(YautjaRank.YoungBlood));
    Assert.That(options, Does.Not.Contain(YautjaRank.Ancient));
}
```

- [ ] **Step 2: Run the policy tests and verify the expected RED failure**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaClanPolicyTest --no-restore`

Expected: compile/test failure because the new policy types and methods do not exist yet.

- [ ] **Step 3: Implement the minimal shared policy**

Implement the exact bit values and rules above. `CanTarget` must reject `actor.PlayerId == target.PlayerId`, target Ancient administrator permissions, and `actor.Rank <= target.Rank` unless the actor has `AdminManager`; even manager actors still cannot target Ancient administrators. `CanModifyRank` must require target clan membership, reject `YoungBlood`/`Ancient`, require the rule permission, and enforce `Elite <= 5`, `Elder <= ceil(clanSize / 12)`, and `Leader <= 1` using the post-change occupancy.

- [ ] **Step 4: Run the tests and verify GREEN**

Run the same focused command. Expected: PASS with no new warnings or errors. Run the existing rank tests in the same project to ensure the rank metadata remains compatible.

- [ ] **Step 5: Commit the shared policy**

```powershell
git add Content.Shared/_CMU14/Yautja/YautjaClan.cs Content.Shared/_CMU14/Yautja/YautjaRank.cs Content.IntegrationTests/_CMU14/Yautja/YautjaClanPolicyTest.cs
git commit -m "feat: add yautja clan rank policy"
```

### Task 2: Persistent clans, members, whitelist flags, and legacy migration

**Files:**
- Create: `Content.Server.Database/YautjaClanModel.cs`
- Create: `Content.Server/Database/ServerDbBase.YautjaClan.cs`
- Modify: `Content.Server.Database/Model.cs` to register `DbSet<YautjaClan>` and `DbSet<YautjaClanMember>` and player whitelist flags.
- Modify: `Content.Server/Database/ServerDbManager.cs` to expose the matching async methods through `IServerDbManager` and its forwarding implementation.
- Create: generated `Content.Server.Database/Migrations/Sqlite/<timestamp>_YautjaClanWorkflow.cs` and designer/snapshot changes.
- Create: generated `Content.Server.Database/Migrations/Postgres/<timestamp>_YautjaClanWorkflow.cs` and designer/snapshot changes.
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanPersistenceTest.cs`

**Interfaces:**
- Produces `Task<YautjaClanRecord?> GetYautjaClanAsync(int clanId)` and `Task<IReadOnlyList<YautjaClanRecord>> GetYautjaClansAsync()`.
- Produces `Task<YautjaClanMemberRecord?> GetYautjaClanMemberAsync(Guid userId)` and `Task<IReadOnlyList<YautjaClanMemberRecord>> GetYautjaClanMembersAsync(int clanId)`.
- Produces atomic `Task UpsertYautjaClanMemberAsync(YautjaClanMemberRecord member)` and `Task UpdateYautjaClanMemberAndLegacyRankAsync(...)`.
- Produces `Task<int> CreateYautjaClanAsync(...)`, `Task SetYautjaWhitelistFlagsAsync(Guid, YautjaWhitelistFlags)`, and `Task<YautjaWhitelistFlags> GetYautjaWhitelistFlagsAsync(Guid)`.
- Uses a unique member row per player, nullable `ClanId`, integer rank/permission storage, honor, `IsLegacy`, and foreign keys to `player`/`yautja_clan`.

- [ ] **Step 1: Write failing persistence and migration tests**

Add tests that create a player, persist a clan/member, read it back, and verify the old rank projection is updated. Add a migration test fixture that inserts a player with `Player.YautjaRank = Elder`, runs the migration/bootstrap path, and expects one `YautjaClanMember` with `Rank = Elder`, `IsLegacy = true`, and no clan. Invalid and `YoungBlood` projections must become `Blooded`.

- [ ] **Step 2: Run the persistence tests and verify RED**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaClanPersistenceTest --no-restore`

Expected: compile/test failure because the new entities, database methods, and migration do not exist.

- [ ] **Step 3: Add entities and database methods**

Add the EF entities, model relationships, unique indexes, and both database-provider mappings. Keep the old `Player.YautjaRank` column. Implement one transaction for member mutation plus the compatibility projection; do not save a new rank if the player row is missing or the transaction fails.

- [ ] **Step 4: Generate and inspect both migrations**

Run from `Content.Server.Database`: `./add-migration.ps1 YautjaClanWorkflow` using the repository's configured EF tooling. Confirm the migration creates only the clan/member/whitelist columns and indexes, has a reversible `Down`, and does not drop or rewrite unrelated tables. If the generated migration cannot express the legacy backfill safely, add the explicit provider-specific SQL/data step in the migration and test it.

- [ ] **Step 5: Run the persistence tests and verify GREEN**

Run the focused persistence filter and the existing `YautjaRankPersistenceTest` filter. Expected: PASS, with SQLite and PostgreSQL model snapshots compiling.

- [ ] **Step 6: Commit the database layer**

```powershell
git add Content.Server.Database/Model.cs Content.Server.Database/YautjaClanModel.cs Content.Server/Database/ServerDbBase.YautjaClan.cs Content.Server/Database/ServerDbManager.cs Content.Server.Database/Migrations/Sqlite Content.Server.Database/Migrations/Postgres Content.IntegrationTests/_CMU14/Yautja/YautjaClanPersistenceTest.cs
git commit -m "feat: persist yautja clans and members"
```

### Task 3: Server clan manager, whitelist workflow, rank resolution, and commands

**Files:**
- Create: `Content.Server/_CMU14/Yautja/YautjaClanManager.cs`
- Create: `Content.Server/_CMU14/Yautja/YautjaClanActionResult.cs` if the shared result cannot be kept in the shared model file.
- Modify: `Content.Server/_CMU14/Yautja/YautjaRankManager.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaProfileApplySystem.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaPredatorRoundSystem.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaYoungbloodSystem.cs`
- Modify: `Content.Server/Administration/Commands/YautjaRankCommands.cs`
- Create: `Content.Server/Administration/Commands/YautjaClanCommands.cs`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanWorkflowTest.cs`

**Interfaces:**
- `YautjaClanManager.Resolve(NetUserId userId, bool youngbloodRole = false)` returns `YautjaClanResolution` with rank, clan id, permissions, legacy state, and whitelist flags.
- `YautjaClanManager.SetRank(NetUserId actor, NetUserId target, YautjaRank rank)` validates policy and atomically applies rank/permissions/projection.
- `YautjaClanManager.MoveMember(NetUserId actor, NetUserId target, int? clanId)` removes to Blooded or moves to Blooded unless target is Ancient-authorized.
- `YautjaClanManager.SetAncient(NetUserId actor, NetUserId target, bool enabled)` requires manager permission and preserves the original Ancient permission mask.
- `YautjaClanManager.GetView(NetUserId actor)` returns the filtered clan snapshot used by EUI.

- [ ] **Step 1: Write failing workflow tests**

Cover these real service behaviors:

```csharp
[Test]
public async Task RemovingMemberResetsRankToBlooded() { /* create actor, target, clan; call MoveMember(actor, target, null); assert Blooded and no clan */ }

[Test]
public async Task LeaderCannotPromoteSelfOrEqualRank() { /* call SetRank(actor, actor, Leader) and SetRank(actor, equalTarget, Leader); assert denial and unchanged rows */ }

[Test]
public async Task AncientAndCouncilWhitelistResolveAsAncient() { /* persist Yautja Leader/Council flags; resolve; assert Ancient/AdminAncient */ }

[Test]
public async Task YoungBloodIsNeverStoredAsNormalRank() { /* call resolver with special role and attempt persistence; assert YoungBlood only in role result and Blooded in DB */ }

[Test]
public async Task ClientProfileCannotOverrideResolvedRank() { /* send profile with Leader while DB says Blooded; apply authoritative result; assert component/access/icon remains Blooded */ }
```

- [ ] **Step 2: Run workflow tests and verify RED**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaClanWorkflowTest --no-restore`

Expected: failure because `YautjaClanManager` does not exist and current rank commands still write the legacy field directly.

- [ ] **Step 3: Implement the manager and authoritative resolution**

Make `YautjaClanMember` the first source consulted. For a valid legacy member, preserve its migrated rank until a clan action touches it. For a new/no-clan member, resolve Blooded. If whitelist flags contain Yautja Leader/Council, resolve Ancient and `YautjaClanPermission.All`; if the special Young Blood role is active, resolve Young Blood without writing it. Sanitize invalid values to Blooded.

- [ ] **Step 4: Route commands and existing systems through the manager**

Change `yautjarank` to use the same validated manager path and remove its direct `_db.SetYautjaRank` bypass. Add `yautjaget` output for clan/rank/permissions and a logged maintenance command for creating/moving members only when the actor has the required server admin/maintenance authority. Update lobby priming, spawn reservation, profile application, Young Blood, equipment/access, and slot-cap calls to use the resolution object.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run the workflow tests, all existing Yautja rank tests, and the job/round tests:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~YautjaClanWorkflowTest|FullyQualifiedName~YautjaRank" --no-restore
```

Expected: PASS with no new warnings. Confirm Leader/Ancient cap bypass and Young Blood Hunting Grounds behavior remain unchanged.

- [ ] **Step 6: Commit the server workflow**

```powershell
git add Content.Server/_CMU14/Yautja Content.Server/Administration/Commands/YautjaRankCommands.cs Content.Server/Administration/Commands/YautjaClanCommands.cs Content.IntegrationTests/_CMU14/Yautja/YautjaClanWorkflowTest.cs
git commit -m "feat: enforce yautja clan rank workflow"
```

### Task 4: `View Clan Info` EUI, OOC/Records entry, and action handling

**Files:**
- Create: `Content.Shared/_CMU14/Yautja/YautjaClanInfoEuiState.cs`
- Create: `Content.Server/_CMU14/Yautja/YautjaClanInfoEui.cs`
- Create: `Content.Server/_CMU14/Yautja/YautjaClanInfoSystem.cs`
- Create: `Content.Server/_CMU14/Yautja/YautjaClanInfoCommand.cs`
- Create: `Content.Client/_CMU14/Yautja/YautjaClanInfoEui.cs`
- Create: `Content.Client/_CMU14/Yautja/YautjaClanInfoWindow.cs`
- Create: `Content.Tests/Client/_CMU14/Yautja/YautjaClanInfoWindowTest.cs`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl` and the matching Russian locale file when present.

**Interfaces:**
- `YautjaClanInfoEuiState` contains clan identity/description/honor/color, viewer rank/permissions, member rows, occupancy/limits, allowed `YautjaClanAction` values, and the latest server status message.
- EUI messages are `Refresh`, `SetRank(NetUserId target, YautjaRank rank)`, `MoveMember(NetUserId target, int? clanId)`, and `SetAncient(NetUserId target, bool enabled)`.
- The server EUI calls only `YautjaClanManager.GetView` and the three mutation methods; the client never decides whether a button is valid.

- [ ] **Step 1: Write failing shared/client tests**

Test that the EUI state carries every rank row's localized rank id and `YautjaRankMetadata.For(row.Rank).IconState`, that normal rank options exclude Young Blood/Ancient, and that the window creates the title `View Clan Info`, a refresh control, and a member row with the correct icon state.

- [ ] **Step 2: Run the client tests and verify RED**

Run: `dotnet test Content.Tests/Content.Tests.csproj --filter FullyQualifiedName~YautjaClanInfoWindowTest --no-restore`

Expected: compile/test failure because the shared state and window do not exist.

- [ ] **Step 3: Implement the shared EUI state/messages**

Use `[Serializable, NetSerializable]` types and immutable/read-only collections. Include only the member fields allowed by `GetView`; action identifiers are capability hints, not authorization.

- [ ] **Step 4: Implement the server EUI and player entry**

Register a self/player `Verb` named `View Clan Info` in `VerbCategory.OOC`/Records using the existing global verb pattern, and also expose `yautja_clan_info` for accessibility/testing. Open `YautjaClanInfoEui` only for a Yautja/authorized viewer. On every message, re-check the player session, reload the current snapshot, call the manager, set a localized denial/status on failure, and dirty the state. Closing sends `CloseEuiMessage`.

- [ ] **Step 5: Implement the client window**

Build the window with existing Robust controls. Show clan header, rank, permissions, honor, member list, occupancy/limits, rank icons from `/Textures/_CMU14/Yautja/rank_icons.rsi`, and only server-advertised controls. Keep the window usable when there are no clan members and show the Young Blood/clanless state explicitly.

- [ ] **Step 6: Run client tests and verify GREEN**

Run the focused filter and the existing Yautja client tests. Expected: PASS without RSI state errors; every icon state must be present in `rank_icons.rsi`.

- [ ] **Step 7: Commit the EUI**

```powershell
git add Content.Shared/_CMU14/Yautja/YautjaClanInfoEuiState.cs Content.Server/_CMU14/Yautja/YautjaClanInfoEui.cs Content.Server/_CMU14/Yautja/YautjaClanInfoSystem.cs Content.Server/_CMU14/Yautja/YautjaClanInfoCommand.cs Content.Client/_CMU14/Yautja/YautjaClanInfoEui.cs Content.Client/_CMU14/Yautja/YautjaClanInfoWindow.cs Content.Tests/Client/_CMU14/Yautja/YautjaClanInfoWindowTest.cs Resources/Locale
git commit -m "feat: add yautja view clan info menu"
```

### Task 5: Full parity verification and runtime startup checks

**Files:**
- Modify only test files or focused implementation files when a failing verification identifies a real regression.
- Do not stage runtime logs, local database files, generated RSI output, `cmss13-ref/`, or unrelated existing changes.

- [ ] **Step 1: Run formatting and static checks**

Run: `git diff --check` and `dotnet build Content.Server/Content.Server.csproj --no-restore`, then `dotnet build Content.Client/Content.Client.csproj --no-restore`.

Expected: both builds succeed without new compiler warnings/errors tied to clans, rank resolution, EUI serialization, or RSI assets.

- [ ] **Step 2: Run focused server/client tests**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~YautjaClan|FullyQualifiedName~YautjaRank|FullyQualifiedName~YautjaPredator" --no-restore
dotnet test Content.Tests/Content.Tests.csproj --filter "FullyQualifiedName~Yautja" --no-restore
```

Expected: all focused tests pass; any pre-existing infrastructure failure is recorded separately and is not hidden as a feature failure.

- [ ] **Step 3: Start server and client and inspect logs**

Use the repository's existing launch scripts/configuration. Capture the server and client output, verify the database migration applies, open `View Clan Info`, refresh it, and exercise one permitted and one denied action. Search logs for `Exception`, `Error`, `RSI`, `YautjaClan`, and `YautjaRank`.

- [ ] **Step 4: Run the complete relevant test/build set**

Run the repository's standard server/client build and test commands after focused checks. Confirm no new migration, serialization, localization, client crash, or missing-RSI errors appear.

- [ ] **Step 5: Final review and commit**

Review `git status --short`, `git diff --stat`, and `git diff --check`. Stage only the implementation files from this plan, then commit:

```powershell
git commit -m "feat: complete cmss13 yautja clan rank parity"
```

## Self-review against the approved specification

- Original ranks, permission flags, target ordering, and limits are covered by Task 1.
- Persistent clans, members, honor, whitelist flags, and one-time legacy migration are covered by Task 2.
- Server-authoritative rank resolution, profile protection, spawn/cap behavior, Young Blood separation, and command routing are covered by Task 3.
- Separate `View Clan Info`, member listing, rank icons, action capabilities, and server-side action revalidation are covered by Task 4.
- SQLite/PostgreSQL migration checks, client/server builds, startup, RSI inspection, and focused/full tests are covered by Task 5.
- No task relies on a placeholder or unverified client-side permission decision; all mutations terminate in `YautjaClanManager`.
