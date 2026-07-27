# Yautja Rank Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with review checkpoints.

**Goal:** Port the missing CMSS13 Yautja clan-rank behavior into the RussianCM codebase and expose the same rank through authoritative access/loadout/spawn gates and rank icons.

**Architecture:** Add one shared `YautjaRank` contract and metadata resolver. Persist the authoritative rank on the server, project it into the spawned Yautja component/profile, and make all server gates consume the resolver. The client consumes only the replicated rank and shared icon metadata for profile and status-HUD presentation.

**Tech Stack:** C#/.NET 10, RobustToolbox ECS and prototype YAML, EF Core SQLite/Postgres migrations, NUnit integration tests, RSI sprite assets, Fluent localization.

## Global Constraints

- Preserve all pre-existing unrelated worktree changes.
- Keep the single `CMUYautjaHunter` job for normal whitelist ranks; do not create one job per rank.
- Keep trophy-score progression separate from clan rank.
- Do not expose rank selection to the client profile editor.
- Normal clan ranks use Hunter Ship clan spawnpoints; rank does not select a different ordinary spawn tile.
- Non-WL Young Blood and Bad Blood/Stranded remain separate special-role spawn paths.
- All access and loadout decisions must be server-authoritative.
- Use `apply_patch` for source/config edits and run the focused tests before broad verification.

---

### Task 1: Add the shared canonical rank contract

**Files:**
- Create: `Content.Shared/_CMU14/Yautja/YautjaRank.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaComponents.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankParityTest.cs`

**Interfaces:**
- Produces `YautjaRank`, `YautjaRankMetadata`, and `YautjaRankResolver` APIs used by server and client tasks.
- `YautjaRankMetadata.For(YautjaRank rank)` returns the localized name key, icon state, access tier, unique-unlock flag, and slot-bypass flag.
- `YautjaRankResolver.ResolveForHunter(YautjaCharacterProfile profile)` returns Blooded when the profile has no valid rank assignment and otherwise returns the server-sanitized rank.

- [ ] **Step 1: Write failing metadata tests**

```csharp
[TestCase(YautjaRank.Unblooded, "unblooded", false, false)]
[TestCase(YautjaRank.YoungBlood, "youngblood", false, false)]
[TestCase(YautjaRank.Blooded, "blooded", false, false)]
[TestCase(YautjaRank.Elite, "elite", true, false)]
[TestCase(YautjaRank.Elder, "elder", true, false)]
[TestCase(YautjaRank.Leader, "leader", true, true)]
[TestCase(YautjaRank.Ancient, "ancient", true, true)]
public void RankMetadataMatchesCmss13(YautjaRank rank, string icon, bool unique, bool bypassSlots)
{
    var metadata = YautjaRankMetadata.For(rank);
    Assert.That(metadata.IconState, Is.EqualTo(icon));
    Assert.That(metadata.UniqueSetsAllowed, Is.EqualTo(unique));
    Assert.That(metadata.BypassesPredatorSlotCap, Is.EqualTo(bypassSlots));
}

[Test]
public void MissingHunterRankFallsBackToBlooded()
{
    Assert.That(YautjaRankResolver.ResolveForHunter(null), Is.EqualTo(YautjaRank.Blooded));
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaRankParityTest
```

Expected: compile failure because the canonical rank types do not exist.

- [ ] **Step 3: Implement the shared enum and metadata table**

Use one ordered enum and one switch/table. The access tier must map as follows: Secure for Unblooded/Young Blood/Blooded, Secure+Elite for Elite, Secure+Elite+Elder for Elder, Secure+Elite+Elder+Leader for Leader, and all five tags for Ancient.

- [ ] **Step 4: Add the replicated rank to Yautja state and profile compatibility**

Add an auto-networked `ClanRank` field to `YautjaComponent`. Keep `YautjaBracerOwnerRank` as a compatibility projection for existing bracer serialization, with conversion helpers in the shared resolver. Add profile cloning/equality coverage so existing saved profiles remain readable.

- [ ] **Step 5: Run the focused test and verify GREEN**

Run the same command and confirm all metadata and fallback cases pass.

- [ ] **Step 6: Commit the shared contract**

```powershell
git add Content.Shared/_CMU14/Yautja/YautjaRank.cs Content.Shared/_CMU14/Yautja/YautjaComponents.cs Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs Content.IntegrationTests/_CMU14/Yautja/YautjaRankParityTest.cs
git commit -m "feat: add canonical Yautja rank contract"
```

### Task 2: Persist and resolve the authoritative rank

**Files:**
- Modify: `Content.Server.Database/Model.cs`
- Modify: `Content.Server/Database/ServerDbManager.cs`
- Modify: `Content.Server/Database/ServerDbBase.cs`
- Modify: `Content.Server/Database/DatabaseRecords.cs`
- Create: `Content.Server/_CMU14/Yautja/YautjaRankManager.cs`
- Create: `Content.Server/Administration/Commands/YautjaRankCommands.cs`
- Create: `Content.Server.Database/Migrations/Sqlite/20260726000000_YautjaRank.cs`
- Create: `Content.Server.Database/Migrations/Sqlite/20260726000000_YautjaRank.Designer.cs`
- Create: `Content.Server.Database/Migrations/Postgres/20260726000001_YautjaRank.cs`
- Create: `Content.Server.Database/Migrations/Postgres/20260726000001_YautjaRank.Designer.cs`
- Modify: `Content.Server.Database/Migrations/Sqlite/SqliteServerDbContextModelSnapshot.cs`
- Modify: `Content.Server.Database/Migrations/Postgres/PostgresServerDbContextModelSnapshot.cs`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankPersistenceTest.cs`

**Interfaces:**
- `IServerDbManager.GetYautjaRank(Guid)` returns nullable stored rank.
- `IServerDbManager.SetYautjaRank(Guid, YautjaRank)` stores an admin-assigned rank.
- `YautjaRankManager.Resolve(NetUserId)` returns Blooded for a whitelisted Hunter without a stored rank and returns Young Blood only for the special non-WL role path.
- Admin command syntax: `yautjarank player rank` and `yautjaget player`, where `rank` is one of `unblooded`, `blooded`, `elite`, `elder`, `leader`, or `ancient`.

- [ ] **Step 1: Write failing persistence tests**

```csharp
[Test]
public async Task RankRoundTripsThroughSqlite()
{
    await using var pair = await PoolManager.GetServerClient();
    var db = pair.Server.ResolveDependency<IServerDbManager>();
    var userId = pair.Player!.UserId.UserId;

    await db.SetYautjaRank(userId, YautjaRank.Elder);
    Assert.That(await db.GetYautjaRank(userId), Is.EqualTo(YautjaRank.Elder));
    await pair.CleanReturnAsync();
}
```

- [ ] **Step 2: Run the test and verify RED**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaRankPersistenceTest
```

Expected: compile failure because the database methods and model do not exist.

- [ ] **Step 3: Add the database entity and both provider migrations**

Store one nullable/integer rank per `Player.UserId`, add a unique player relationship, and update both model snapshots. Existing rows must remain null so the resolver can apply the Blooded compatibility default.

- [ ] **Step 4: Add manager and admin commands**

Use the existing `IPlayerLocator` and admin command patterns. Reject invalid rank names, reject Young Blood assignment through the persistent clan-rank command, and log every change. Do not add a profile-editor control for rank.

- [ ] **Step 5: Implement server rank resolution**

Resolve the stored rank before normal Hunter equipment is applied. A missing rank for a whitelisted Hunter becomes Blooded; invalid or out-of-range stored values also become Blooded. The special Young Blood ghost role keeps its own role rank and is never promoted by a client profile.

- [ ] **Step 6: Run the focused test and verify GREEN**

Run the same filter and confirm round-trip, default, invalid-value, and Young Blood separation cases pass.

- [ ] **Step 7: Commit persistence and administration**

```powershell
git add Content.Server.Database Content.Server/Database Content.Server/_CMU14/Yautja/YautjaRankManager.cs Content.Server/Administration/Commands/YautjaRankCommands.cs Content.IntegrationTests/_CMU14/Yautja/YautjaRankPersistenceTest.cs
git commit -m "feat: persist Yautja clan ranks"
```

### Task 3: Apply rank to loadout, bracer ID, and profile gates

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaProfileApplySystem.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaBracerUtilitySystem.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaGearRackSystem.cs`
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaCharacterProfileTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaRackAccessTest.cs`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankAccessTest.cs`

**Interfaces:**
- `YautjaProfileApplySystem.ApplyProfile` receives/resolves the authoritative rank and writes it to `YautjaComponent.ClanRank` before applying gear.
- `YautjaRankMetadata.GetAccessTags` is the only rank-to-access mapping used for bracer ID chips.

- [ ] **Step 1: Write failing access and profile-gate tests**

```csharp
[TestCase(YautjaRank.Blooded, false)]
[TestCase(YautjaRank.Elite, true)]
[TestCase(YautjaRank.Elder, true)]
[TestCase(YautjaRank.Leader, true)]
[TestCase(YautjaRank.Ancient, true)]
public void UniqueSetsAreRankGated(YautjaRank rank, bool allowed)
{
    var profile = YautjaCharacterProfile.Default
        .WithRank(rank)
        .WithUnique(YautjaUniqueSet.Ronin);

    Assert.That(YautjaRankResolver.CanUseUnique(profile), Is.EqualTo(allowed));
}
```

- [ ] **Step 2: Run the focused tests and verify RED**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaRankAccessTest
```

Expected: at least the rank-gate and cumulative-access assertions fail because the current profile editor and server path do not use clan rank.

- [ ] **Step 3: Make rank resolution server-authoritative during profile application**

Sanitize requested `Unique` sets to `None` below Elite, set the spawned component's `ClanRank`, and derive `OwnerRank` only after the canonical rank has been resolved. Keep the existing legacy-whitelist behavior unchanged.

- [ ] **Step 4: Replace duplicate bracer access switches**

Route `ApplyIdChipUserData` through the shared metadata access list. Preserve the Bad Blood and Stranded special cases exactly as they work today.

- [ ] **Step 5: Replace duplicate rack rank gates**

Keep role gates for Adult/Youngblood/Thrall/Blooded Thrall/Bad Blood/Stranded, but use canonical access tags for Adult and Elder racks. Ensure Elder and Ancient are accepted by the Elder rack, while Blooded/Elite/Leader remain denied until the required tag exists.

- [ ] **Step 6: Add client-side rank-aware selector behavior**

The profile editor must hide/disable non-`None` Unique choices below Elite and display the server-provided rank. It must not add a rank selector or trust a client-created rank value.

- [ ] **Step 7: Run rack, profile, and access tests and verify GREEN**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~Yautja"
```

Expected: all current rack behavior plus the new rank cases pass.

- [ ] **Step 8: Commit rank application and gates**

```powershell
git add Content.Server/_CMU14/Yautja Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs Content.Shared/_CMU14/Yautja Content.IntegrationTests/_CMU14/Yautja/YautjaCharacterProfileTest.cs Content.IntegrationTests/_CMU14/Yautja/YautjaRackAccessTest.cs Content.IntegrationTests/_CMU14/Yautja/YautjaRankAccessTest.cs
git commit -m "feat: apply Yautja rank access and profile gates"
```

### Task 4: Match rank loadout, spawn, and slot policy

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaPredatorRoundSystem.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaPredatorRoundComponent.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaYoungbloodSystem.cs`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml`
- Modify: `Resources/Prototypes/_CMU14/Maps/huntership_support.yml`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankSpawnTest.cs`

**Interfaces:**
- `YautjaPredatorRoundSystem.GetRankSpawnPolicy(YautjaRank)` returns Hunter Ship clan spawn for all normal ranks and `BypassSlotCap` for Leader/Ancient.
- `YautjaPredatorRoundSystem.ResolveRankForSession` uses the server manager and applies the rank before `SpawnPlayerMob`.

- [ ] **Step 1: Write failing spawn-policy tests**

```csharp
[TestCase(YautjaRank.Unblooded, false)]
[TestCase(YautjaRank.Blooded, false)]
[TestCase(YautjaRank.Elite, false)]
[TestCase(YautjaRank.Elder, false)]
[TestCase(YautjaRank.Leader, true)]
[TestCase(YautjaRank.Ancient, true)]
public void NormalRanksUseHunterShipAndOnlySeniorRanksBypassSlots(YautjaRank rank, bool bypass)
{
    var policy = YautjaPredatorRoundSystem.GetRankSpawnPolicy(rank);
    Assert.That(policy.SpawnKind, Is.EqualTo(YautjaSpawnKind.HunterShipClan));
    Assert.That(policy.BypassSlotCap, Is.EqualTo(bypass));
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaRankSpawnTest
```

Expected: compile failure because the policy API does not exist.

- [ ] **Step 3: Resolve rank before normal Hunter spawn**

Keep the existing random `CMUHunterShipMarkerPredatorSpawn` selection for all normal ranks. Apply the rank-aware loadout and component after spawning. Do not create rank-specific ordinary spawn markers.

- [ ] **Step 4: Apply CMSS13 special paths**

Keep non-WL Young Blood on the Hunting Grounds Youngblood spawn marker and keep Bad Blood/Stranded on the survivor base path. Add explicit assertions that those paths never use the ordinary Hunter Ship clan spawn list.

- [ ] **Step 5: Implement slot bypass**

Leader and Ancient ignore the ordinary Predator rank slot cap; all other normal ranks use the configured cap. Preserve the existing configured minimum/maximum Hunter slots for the rest of the role.

- [ ] **Step 6: Run role and spawn tests and verify GREEN**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~YautjaPredatorRoleTest|FullyQualifiedName~YautjaYoungbloodTest|FullyQualifiedName~YautjaRankSpawnTest"
```

- [ ] **Step 7: Commit spawn and slot behavior**

```powershell
git add Content.Server/_CMU14/Yautja Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml Resources/Prototypes/_CMU14/Maps/huntership_support.yml Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs Content.IntegrationTests/_CMU14/Yautja/YautjaRankSpawnTest.cs
git commit -m "feat: match Yautja rank spawn and slot policy"
```

### Task 5: Add rank icons to prototypes, lobby, and status HUD

**Files:**
- Create: `Resources/Textures/_CMU14/Yautja/rank_icons.rsi/meta.json`
- Create: `Resources/Textures/_CMU14/Yautja/rank_icons.rsi/unblooded.png`
- Create: `Resources/Textures/_CMU14/Yautja/rank_icons.rsi/youngblood.png`
- Create: `Resources/Textures/_CMU14/Yautja/rank_icons.rsi/blooded.png`
- Create: `Resources/Textures/_CMU14/Yautja/rank_icons.rsi/elite.png`
- Create: `Resources/Textures/_CMU14/Yautja/rank_icons.rsi/elder.png`
- Create: `Resources/Textures/_CMU14/Yautja/rank_icons.rsi/leader.png`
- Create: `Resources/Textures/_CMU14/Yautja/rank_icons.rsi/ancient.png`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Interface/status_icons.yml`
- Modify: `Content.Client/_CMU14/Yautja/YautjaHudSystem.cs`
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankIconTest.cs`

**Interfaces:**
- `YautjaRankMetadata.For(rank).IconState` is the only rank-to-sprite mapping.
- `YautjaHudSystem` adds the rank icon only when the local viewer has `YautjaHudViewerComponent` or is an explicitly authorized Yautja viewer.
- The profile editor uses `AnimatedTextureRect.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(...))` with the same icon state.

- [ ] **Step 1: Create the seven-state pixel-art asset set**

Use the existing Yautja HUD visual language and 32×32 single-frame RSI states. Keep each emblem visually distinct by silhouette and rank progression, with transparent background and no text baked into the image. Record the source/license metadata in `meta.json`.

- [ ] **Step 2: Write failing prototype and icon mapping tests**

```csharp
[Test]
public async Task EveryClanRankHasALobbyAndStatusIcon()
{
    await using var pair = await PoolManager.GetServerClient();
    await pair.Server.WaitAssertion(() =>
    {
        var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
        foreach (var rank in YautjaRankMetadata.Order)
        {
            var state = YautjaRankMetadata.For(rank).IconState;
            Assert.That(state, Is.Not.Empty);
            Assert.That(prototypes.HasIndex<HealthIconPrototype>($"CMUYautjaRankIcon{rank}"), Is.True);
        }
    });
    await pair.CleanReturnAsync();
}
```

- [ ] **Step 3: Run the icon test and verify RED**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~YautjaRankIconTest
```

Expected: missing prototype/state failure.

- [ ] **Step 4: Add health-icon prototypes and HUD integration**

Cache rank icons in `YautjaHudSystem`, append the clan icon after existing Yautja honor/mark icons, and keep the existing viewer filter so ordinary humans do not see the hidden Yautja rank.

- [ ] **Step 5: Add lobby rank badge**

Add a read-only rank row near the profile identity controls. It must use the same `IconState`, show the localized rank name, and hide non-allowed Unique buttons rather than allowing a click that the server later rejects.

- [ ] **Step 6: Run icon and client profile tests and verify GREEN**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~YautjaRankIconTest|FullyQualifiedName~YautjaCharacterProfileTest"
```

- [ ] **Step 7: Commit icon and UI behavior**

```powershell
git add Resources/Textures/_CMU14/Yautja/rank_icons.rsi Resources/Prototypes/_CMU14/Threats/Yautja/Interface/status_icons.yml Content.Client/_CMU14/Yautja Content.IntegrationTests/_CMU14/Yautja/YautjaRankIconTest.cs Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl
git commit -m "feat: add Yautja clan rank icons"
```

### Task 6: Full parity verification and handoff

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankParityTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankAccessTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankSpawnTest.cs`
- Modify: `docs/superpowers/specs/2026-07-26-yautja-rank-parity-design.md` only if implementation constraints require an approved design correction.

- [ ] **Step 1: Add a source-parity matrix test**

Cover every rank in one table with expected access tags, unique availability, elder/ancient rack access, spawn kind, slot bypass, and icon state. Include explicit Bad Blood/Stranded/Young Blood rows so special roles cannot accidentally inherit normal clan rank behavior.

- [ ] **Step 2: Run focused integration tests**

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter FullyQualifiedName~Yautja
```

- [ ] **Step 3: Run the full server/client test projects**

```powershell
dotnet test Content.Tests/Content.Tests.csproj
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj
```

- [ ] **Step 4: Run repository build and whitespace checks**

```powershell
dotnet build Content.Server/Content.Server.csproj --no-restore
git diff --check HEAD~1..HEAD
```

- [ ] **Step 5: Inspect the final diff for scope**

```powershell
git status --short
git diff --stat HEAD~6..HEAD
git diff --name-only HEAD~6..HEAD
```

Confirm only the rank parity implementation, tests, migrations, localization, icons, and approved docs are included; preserve all unrelated pre-existing worktree changes.

- [ ] **Step 6: Commit the final test matrix**

```powershell
git add Content.IntegrationTests/_CMU14/Yautja docs/superpowers/plans/2026-07-26-yautja-rank-parity.md
git commit -m "test: verify Yautja rank parity"
```
