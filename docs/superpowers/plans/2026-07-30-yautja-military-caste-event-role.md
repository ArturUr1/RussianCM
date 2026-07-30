# Военный яутжа как ивентовая роль Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить в RussianCM две скрытые event-only роли CMSS13 Military Caste — Soldier и Enforcer — с фиксированным боевым снаряжением и спавном через существующий админский `Spawn Here As Job`.

**Architecture:** Роли будут обычными скрытыми `JobPrototype`, но каждая будет использовать отдельный производный `CMUMobYautja` с собственным `LoadoutComponent`. Это необходимо, потому что custom `JobEntity` экипируется при `MapInit`, а не через job `StartingGear`; базовый `CMUMobYautja` иначе выдаст hunter loadout. Автоматический deathsquad/event-call не добавляется.

**Tech Stack:** RobustToolbox / C# integration tests, YAML entity/job prototypes, Fluent localization, NUnit.

## Global Constraints

- Роли `CMUYautjaMilitaryCasteSoldier` и `CMUYautjaMilitaryCasteEnforcer` должны иметь `hidden: true`, `whitelisted: false`, `canBeAntag: false`, `joinNotifyCrew: false`, `usePlayerProfile: false`.
- Роли доступны только через существующий админский `Spawn Here As Job`; отдельный emergency call и автоматический набор отряда не создаются.
- Hunter, Youngblood и Bad Blood loadout/flow не изменяются.
- Для Soldier используется `CMUYautjaPoweredArmor`, для Enforcer — `CMUYautjaPoweredArmorEnforcer`.
- Военное радио использует существующий `CMUYautjaMilitaryCommunicator` и канал `CMUYautjaMilitary`.
- Все изменения должны быть изолированы от уже имеющихся незакоммиченных изменений: в `git add` и коммиты включаются только файлы текущей задачи.

---

### Task 1: Add failing integration coverage for the two event roles

**Files:**
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaMilitaryCasteRoleTest.cs`

**Interfaces:**
- Consumes: current `JobPrototype`, `IPrototypeManager`, `StationSpawningSystem`, `InventorySystem`, `YautjaComponent`, and the existing `AssertEquippedPrototype` pattern from `YautjaPredatorRoleTest`.
- Produces: named failing tests that lock the job IDs, mob IDs, loadout IDs, event-only flags, and equipped role-specific items before production prototypes exist.

- [ ] **Step 1: Write the failing tests**

Create a new NUnit fixture in namespace `Content.IntegrationTests._CMU14.Yautja` with these tests:

```csharp
[Test]
public async Task MilitaryCasteJobsAreHiddenEventOnlyRoles()
{
    await using var pair = await PoolManager.GetServerClient();
    var server = pair.Server;

    await server.WaitAssertion(() =>
    {
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        var soldier = prototypes.Index<JobPrototype>("CMUYautjaMilitaryCasteSoldier");
        var enforcer = prototypes.Index<JobPrototype>("CMUYautjaMilitaryCasteEnforcer");

        foreach (var job in new[] { soldier, enforcer })
        {
            Assert.That(job.Hidden, Is.True);
            Assert.That(job.Whitelisted, Is.False);
            Assert.That(job.CanBeAntag, Is.False);
            Assert.That(job.UsePlayerProfile, Is.False);
            Assert.That(job.JoinNotifyCrew, Is.False);
        }

        Assert.That(soldier.JobEntity?.ToString(), Is.EqualTo("CMUMobYautjaMilitaryCasteSoldier"));
        Assert.That(enforcer.JobEntity?.ToString(), Is.EqualTo("CMUMobYautjaMilitaryCasteEnforcer"));
        Assert.That(soldier.StartingGear?.ToString(), Is.EqualTo("CMUYautjaMilitaryCasteSoldierGear"));
        Assert.That(enforcer.StartingGear?.ToString(), Is.EqualTo("CMUYautjaMilitaryCasteEnforcerGear"));
    });

    await pair.CleanReturnAsync();
}

[Test]
public async Task MilitaryCasteJobsSpawnWithFixedRoleGear()
{
    await using var pair = await PoolManager.GetServerClient();
    var server = pair.Server;
    var map = await pair.CreateTestMap();

    await server.WaitAssertion(() =>
    {
        var entMan = server.EntMan;
        var inventory = entMan.System<InventorySystem>();
        var stationSpawning = entMan.System<StationSpawningSystem>();
        var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
            .WithName("Military Caste Test")
            .WithYautjaProfile(YautjaCharacterProfile.Default.WithName("Military Caste Test"));

        var soldier = stationSpawning.SpawnPlayerMob(
            map.GridCoords.Offset(new Vector2(-1, 0)),
            "CMUYautjaMilitaryCasteSoldier", profile, station: null);
        var enforcer = stationSpawning.SpawnPlayerMob(
            map.GridCoords.Offset(new Vector2(1, 0)),
            "CMUYautjaMilitaryCasteEnforcer", profile, station: null);

        Assert.That(entMan.HasComponent<YautjaComponent>(soldier), Is.True);
        Assert.That(entMan.HasComponent<YautjaComponent>(enforcer), Is.True);
        AssertEquippedPrototype(entMan, inventory, soldier, "ears", "CMUYautjaMilitaryCommunicator");
        AssertEquippedPrototype(entMan, inventory, soldier, "head", "CMUYautjaPoweredHelmet");
        AssertEquippedPrototype(entMan, inventory, soldier, "gloves", "CMUYautjaSoldierBracers");
        AssertEquippedPrototype(entMan, inventory, soldier, "outerClothing", "CMUYautjaPoweredArmor");
        AssertEquippedPrototype(entMan, inventory, soldier, "shoes", "CMUYautjaPoweredGreaves");
        AssertEquippedPrototype(entMan, inventory, enforcer, "outerClothing", "CMUYautjaPoweredArmorEnforcer");
        AssertEquippedPrototype(entMan, inventory, enforcer, "back", "CMUYautjaCannonPack");
    });

    await pair.CleanReturnAsync();
}
```

Implement a local `AssertEquippedPrototype` helper in the new fixture using `InventorySystem.TryGetSlotEntity` and `MetaDataComponent.EntityPrototype?.ID`, so the test remains independent from the large existing fixture.

- [ ] **Step 2: Run the focused tests and verify the failure is meaningful**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaMilitaryCasteRoleTest"
```

Expected: the new fixture is discovered, then fails because `CMUYautjaMilitaryCasteSoldier` and `CMUYautjaMilitaryCasteEnforcer` are not yet indexed. Do not weaken the assertions or mark the tests inconclusive.

- [ ] **Step 3: Commit the red tests only**

```powershell
git add -- Content.IntegrationTests/_CMU14/Yautja/YautjaMilitaryCasteRoleTest.cs
git commit --only -m "test: cover military caste event roles" -- Content.IntegrationTests/_CMU14/Yautja/YautjaMilitaryCasteRoleTest.cs
```

### Task 2: Add fixed Military Caste loadouts and event mob prototypes

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Mobs/mobs.yml`

**Interfaces:**
- Consumes: existing `CMUMobYautja`, `CMUYautjaSoldierBracers`, MCaste armor/equipment, and existing Yautja weapons/medical prototypes.
- Produces: `CMUYautjaMilitaryCasteSoldierGear`, `CMUYautjaMilitaryCasteEnforcerGear`, `CMUMobYautjaMilitaryCasteSoldier`, and `CMUMobYautjaMilitaryCasteEnforcer`.

- [ ] **Step 1: Add the two starting gear prototypes**

Add the prototypes before the existing hunter variants in `jobs.yml`.

The shared equipment slots must include:

```yaml
equipment:
  ears: CMUYautjaMilitaryCommunicator
  head: CMUYautjaPoweredHelmet
  gloves: CMUYautjaSoldierBracers
  jumpsuit: CMUYautjaBodyMesh
  shoes: CMUYautjaPoweredGreaves
  belt: CMUYautjaHuntingPouch
  pocket2: CMUYautjaMedicompFull
```

Soldier must set `outerClothing: CMUYautjaPoweredArmor` and `back: CMUYautjaHeavyGelDefoliatorDeathsquad`. Its belt storage must contain two `CMUYautjaWristBladesAttachment` entities, `CMUYautjaCleanserGelVial`, `CMUYautjaPlasmaPistol`, `CMUYautjaSpikeLauncher`, three `CMUYautjaDefoliatorFuelTankDeathsquad` tanks, `CMUYautjaMcasteHerbContainerFilled`, `RMCHandcuffs`, and `RMCZipties`.

Enforcer must inherit the soldier gear where the prototype system permits and override `outerClothing: CMUYautjaPoweredArmorEnforcer` and `back: CMUYautjaCannonPack`. The cannon pack is authoritative for the internal `CMUYautjaDualPlasmaCannons`; do not put a second standalone cannon in an inventory slot.

- [ ] **Step 2: Add dedicated mob entities with loadouts**

Append two entities to `mobs.yml`:

```yaml
- type: entity
  parent: CMUMobYautja
  id: CMUMobYautjaMilitaryCasteSoldier
  name: Yautja Military Caste Soldier
  suffix: Yautja, Military Caste, Event
  components:
  - type: Loadout
    prototypes:
    - CMUYautjaMilitaryCasteSoldierGear

- type: entity
  parent: CMUMobYautja
  id: CMUMobYautjaMilitaryCasteEnforcer
  name: Yautja Military Caste Enforcer
  suffix: Yautja, Military Caste, Event
  components:
  - type: Loadout
    prototypes:
    - CMUYautjaMilitaryCasteEnforcerGear
```

The child `Loadout` components must replace the inherited hunter prototype list rather than append to it. Do not modify `CMUMobYautja`'s existing hunter prototypes.

- [ ] **Step 3: Run prototype loading and the focused tests**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaMilitaryCasteRoleTest"
```

Expected: job assertions may still fail because the job entries have not been added, while any prototype parsing or missing-entity errors must be fixed before continuing.

- [ ] **Step 4: Commit the loadout and mob prototype changes**

```powershell
git add -- Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml Resources/Prototypes/_CMU14/Threats/Yautja/Mobs/mobs.yml
git commit --only -m "feat: add military caste event loadouts" -- Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml Resources/Prototypes/_CMU14/Threats/Yautja/Mobs/mobs.yml
```

### Task 3: Register hidden jobs, trackers, and localization

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`

**Interfaces:**
- Consumes: the two mob/loadout prototype IDs from Task 2.
- Produces: job IDs that existing admin spawn-as-job code can resolve, plus localized role names/descriptions and persisted playtime tracker IDs.

- [ ] **Step 1: Add the Soldier and Enforcer job prototypes**

Add two `job` prototypes with this shape:

```yaml
- type: job
  parent: CMJobBase
  id: CMUYautjaMilitaryCasteSoldier
  name: cmu-yautja-job-name-military-soldier
  description: cmu-yautja-job-description-military-soldier
  playTimeTracker: CMUYautjaMilitaryCasteSoldier
  icon: CMJobIconEmpty
  supervisors: cm-job-supervisors-nobody
  joinNotifyCrew: false
  hidden: true
  canBeAntag: false
  whitelisted: false
  jobEntity: CMUMobYautjaMilitaryCasteSoldier
  jobPreviewEntity: CMUMobYautjaMilitaryCasteSoldier
  startingGear: CMUYautjaMilitaryCasteSoldierGear
  usePlayerProfile: false
```

Define Enforcer with the corresponding Enforcer IDs. Do not add either role to a public department role list or to normal round-start job slots.

- [ ] **Step 2: Add playtime trackers**

Append these two prototypes alongside the existing Yautja trackers in `jobs.yml`:

```yaml
- type: playTimeTracker
  id: CMUYautjaMilitaryCasteSoldier

- type: playTimeTracker
  id: CMUYautjaMilitaryCasteEnforcer
```

- [ ] **Step 3: Add English and Russian locale entries**

Add these keys to both locale files:

```ftl
cmu-yautja-job-name-military-soldier = Yautja Military Caste Soldier
cmu-yautja-job-description-military-soldier = An event-only military caste Yautja soldier deployed as part of a Predator Deathsquad.
cmu-yautja-job-name-military-enforcer = Yautja Military Caste Enforcer
cmu-yautja-job-description-military-enforcer = An event-only military caste Yautja enforcer leading a Predator Deathsquad.
```

Use natural Russian translations in `ru-RU`; preserve the key names exactly. No new locale keys are required for the excluded deathsquad command.

- [ ] **Step 4: Run the focused tests and prototype validation**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaMilitaryCasteRoleTest"
```

Expected: both focused tests pass. If a mapped weapon cannot be equipped in its intended slot, keep the fixed armor/communicator/bracer/medical requirements and place that weapon in the existing Yautja belt storage using the valid slot accepted by `CMUYautjaHuntingPouch`; do not reintroduce the hunter loadout.

- [ ] **Step 5: Commit the role registration and locale changes**

```powershell
git add -- Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl
git commit --only -m "feat: register military caste event jobs" -- Resources/Prototypes/_CMU14/Threats/Yautja/Roles/jobs.yml Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl
```

### Task 4: Verify isolation, role behavior, and repository health

**Files:**
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaMilitaryCasteRoleTest.cs`
- Inspect only: all files changed by this task, plus existing Yautja role tests.

**Interfaces:**
- Consumes: completed role prototypes, fixed loadouts, and focused integration coverage.
- Produces: evidence that the new event roles work and existing Yautja roles remain unchanged.

- [ ] **Step 1: Run the focused role fixture again**

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaMilitaryCasteRoleTest"
```

Expected: all tests in `YautjaMilitaryCasteRoleTest` pass.

- [ ] **Step 2: Run the existing Yautja role regression fixture**

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest"
```

Expected: the existing hunter, youngblood, bad blood, and related Yautja role tests pass.

- [ ] **Step 3: Run YAML/prototype and localization checks**

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests.Tests.PrototypeTests|FullyQualifiedName~Content.IntegrationTests.Tests.Localization"
```

Expected: prototype and localization suites pass without new errors.

- [ ] **Step 4: Review the final diff and dirty-worktree boundaries**

```powershell
git diff HEAD~3..HEAD --stat
git diff HEAD~3..HEAD --check
git status --short
```

Confirm that the task commits contain only the new test, Yautja role/mob prototypes, and the two locale files; preserve all unrelated pre-existing modifications and untracked files.
