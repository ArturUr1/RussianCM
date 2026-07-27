# Yautja Crew Monitor CMU/RMC Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the CMSS13 Yautja health monitor in CMU/RMC with authoritative direct Yautja collection, matching rank, damage, faction, dead-state, area, and map-location behavior while preserving ordinary Crew Monitor behavior.

**Architecture:** Keep `ComputerCrewMonitoring` as the powered computer and UI parent, add a Yautja-only marker and server system, and have that system populate the existing `CrewMonitoringState` without using `CrewMonitoringServer` or Yautja Suit Sensors. Extend the shared status payload with typed damage and location metadata, then adapt the existing client window for Yautja search, details, and map colors while leaving normal sensor entries compatible.

**Tech Stack:** C#/.NET, RobustToolbox entity systems and bound UIs, NetSerializable shared state, YAML entity prototypes/maps, NUnit integration and client tests.

## Global Constraints

- The monitor must not depend on ordinary human suit sensors.
- The specialized system must not require `CrewMonitoringServer` on the Hunter Ship.
- `YautjaComponent.ClanRank` and the server-owned Yautja rank path remain the only rank authority.
- Existing ordinary station Crew Monitor networking and behavior must remain unchanged.
- Preserve the existing five Hunter Ship monitor placements and one shuttle monitor placement.
- Reuse the existing CMU/RMC window functionally; pixel-perfect cloning of the CMSS13 TGUI is not required.
- Keep dead Yautja visible when they have a valid position, as in CMSS13.
- Main-ship entries are blue; non-main-ship hunting entries are red; classification is map/grid based, not localized-text based.
- Do not add Suit Sensor components to Yautja armor, masks, or every Yautja mob.
- Do not add or remove map monitors beyond the existing CMSS13-equivalent placements.
- Do not overwrite or stage unrelated working-tree changes.

---

## File map

Create these focused files:

- `Content.Shared/_CMU14/Yautja/YautjaCrewMonitoring.cs` — shared Yautja monitor location enum and rank/damage metadata helpers.
- `Content.Server/_CMU14/Yautja/YautjaCrewMonitoringConsoleComponent.cs` — marker component identifying a direct-collection Yautja console.
- `Content.Server/_CMU14/Yautja/YautjaCrewMonitoringConsoleSystem.cs` — authoritative population scan, status construction, location classification, cache refresh, and UI state publication.
- `Content.Client/Medical/CrewMonitoring/CrewMonitoringFilter.cs` — pure client-side name/rank/area matching helper.
- `Content.IntegrationTests/_CMU14/Yautja/YautjaCrewMonitoringTest.cs` — prototype, map-wrapper, collection, state, damage, location, and ordinary-monitor regression tests.
- `Content.Tests/Client/Medical/CrewMonitoring/CrewMonitoringFilterTest.cs` — deterministic search/filter tests.
- `Content.Tests/Shared/Medical/CrewMonitoring/YautjaCrewMonitoringMetadataTest.cs` — deterministic rank and damage-group tests.

Modify these existing files:

- `Content.Shared/Medical/SuitSensor/SharedSuitSensor.cs` — add optional typed damage and Yautja location fields without changing the existing constructor or generic sensor semantics.
- `Content.Server/Medical/CrewMonitoring/CrewMonitoringConsoleSystem.cs` — bypass packet-driven updates only for the Yautja marker and retain all generic behavior for other consoles.
- `Content.Client/Medical/CrewMonitoring/CrewMonitoringWindow.xaml.cs` — rerender on search changes, include area in matching, show Yautja damage details, and color location blips.
- `Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml` — add the marker to `CMUYautjaHunterShuttleHealthMonitor` while retaining the current parent, sprite, power, and UI components.
- `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl` and `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl` — add Yautja monitor assignment, location, and damage-detail strings.
- `Content.IntegrationTests/_CMU14/HunterShip/HunterShipYautjaMachineryTest.cs` — add the static wrapper-parent assertion beside existing Hunter Ship machinery parity checks if the new feature test does not own it.

The existing map files and five generated Hunter Ship wrapper prototypes should not be edited unless a failing map-parity test proves that one of the six already-mapped instances does not inherit `CMUYautjaHunterShuttleHealthMonitor`.

## Task 1: Define the shared status contract and pure metadata helpers

**Files:**
- Create: `Content.Shared/_CMU14/Yautja/YautjaCrewMonitoring.cs`
- Modify: `Content.Shared/Medical/SuitSensor/SharedSuitSensor.cs`
- Test: `Content.Tests/Shared/Medical/CrewMonitoring/YautjaCrewMonitoringMetadataTest.cs`

**Interfaces:**
- Produces `YautjaCrewMonitoringLocationKind` with values `Unknown`, `MainShip`, and `HuntingGround`.
- Produces `YautjaCrewMonitoringMetadata.GetAssignment(YautjaRank rank, bool isBadBlood) -> LocId`.
- Produces `YautjaCrewMonitoringMetadata.SumDamageGroup(DamageSpecifier damage, IReadOnlyList<string> damageTypes) -> int`.
- Adds optional `OxygenDamage`, `ToxinDamage`, `BurnDamage`, `BruteDamage`, `Area`, `LocationKind`, and `CanTrack` properties to `SuitSensorStatus`; generic suit-sensor callers continue using the existing constructor and fields.

- [ ] **Step 1: Write failing metadata tests for every CMSS13 rank mapping.**

```csharp
[TestCase(YautjaRank.Ancient, false, "cmu-yautja-crew-monitor-rank-ancient")]
[TestCase(YautjaRank.Leader, false, "cmu-yautja-crew-monitor-rank-leader")]
[TestCase(YautjaRank.Elder, false, "cmu-yautja-crew-monitor-rank-elder")]
[TestCase(YautjaRank.Elite, false, "cmu-yautja-crew-monitor-rank-elite")]
[TestCase(YautjaRank.Blooded, false, "cmu-yautja-crew-monitor-rank-blooded")]
[TestCase(YautjaRank.YoungBlood, false, "cmu-yautja-crew-monitor-rank-youngblood")]
[TestCase(YautjaRank.Unblooded, false, "cmu-yautja-crew-monitor-rank-unblooded")]
[TestCase(YautjaRank.Blooded, true, "cmu-yautja-crew-monitor-rank-badblood")]
public void AssignmentUsesAuthoritativeRankOrBadBloodMarker(YautjaRank rank, bool isBadBlood, string expected)
{
    Assert.That(YautjaCrewMonitoringMetadata.GetAssignment(rank, isBadBlood).ToString(), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Add the failing damage-group test.**

```csharp
[Test]
public void SumDamageGroupAddsOnlyTheRequestedTypes()
{
    var damage = new DamageSpecifier
    {
        DamageDict = new()
        {
            ["Asphyxiation"] = 3,
            ["Bloodloss"] = 2,
            ["Poison"] = 7,
            ["Radiation"] = 1,
            ["Heat"] = 11,
            ["Shock"] = 2,
            ["Cold"] = 4,
            ["Caustic"] = 3,
            ["Blunt"] = 13,
            ["Slash"] = 5,
            ["Piercing"] = 2,
        }
    };

    Assert.Multiple(() =>
    {
        Assert.That(YautjaCrewMonitoringMetadata.SumDamageGroup(damage, ["Asphyxiation", "Bloodloss"]), Is.EqualTo(5));
        Assert.That(YautjaCrewMonitoringMetadata.SumDamageGroup(damage, ["Poison", "Radiation"]), Is.EqualTo(8));
        Assert.That(YautjaCrewMonitoringMetadata.SumDamageGroup(damage, ["Heat", "Shock", "Cold", "Caustic"]), Is.EqualTo(20));
        Assert.That(YautjaCrewMonitoringMetadata.SumDamageGroup(damage, ["Blunt", "Slash", "Piercing"]), Is.EqualTo(20));
    });
}
```

- [ ] **Step 3: Run the focused shared test and verify it fails for missing types/helpers.**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaCrewMonitoringMetadataTest" --logger "console;verbosity=minimal"`

Expected: FAIL because the new enum, helper, and status properties do not exist yet.

- [ ] **Step 4: Add the shared enum, rank mapping, damage helper, and nullable status fields.**

Use these exact damage mappings in the implementation:

```csharp
public enum YautjaCrewMonitoringLocationKind : byte
{
    Unknown,
    MainShip,
    HuntingGround,
}

public static LocId GetAssignment(YautjaRank rank, bool isBadBlood)
{
    return isBadBlood
        ? "cmu-yautja-crew-monitor-rank-badblood"
        : YautjaRankMetadata.For(rank).LocalizedName;
}
```

`SumDamageGroup` must sum only positive values from the supplied type names and return `DamageSpecifier` values rounded with `.Int()`, matching the integer values exposed by the existing Crew Monitor state and the CMSS13 `round(..., 1)` calls. Do not alter the existing `SuitSensorStatus` constructor or generic network parsing in this task.

- [ ] **Step 5: Run the shared tests and commit the shared contract.**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaCrewMonitoringMetadataTest" --logger "console;verbosity=minimal"`

Expected: PASS.

```powershell
git add -- Content.Shared/_CMU14/Yautja/YautjaCrewMonitoring.cs Content.Shared/Medical/SuitSensor/SharedSuitSensor.cs Content.Tests/Shared/Medical/CrewMonitoring/YautjaCrewMonitoringMetadataTest.cs
git commit -m "feat: add Yautja crew monitor status contract"
```

## Task 2: Write the failing end-to-end parity tests

**Files:**
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaCrewMonitoringTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/HunterShip/HunterShipYautjaMachineryTest.cs` if wrapper assertions are kept with the existing machinery suite.

**Interfaces:**
- The integration tests will call the future public system method `YautjaCrewMonitoringConsoleSystem.Refresh(EntityUid monitor)` to make collection tests deterministic.
- The tests will inspect `CrewMonitoringConsoleComponent.ConnectedSensors` and `UserInterfaceSystem.TryGetUiState<CrewMonitoringState>`; no test will depend on client-only window rendering to verify server data.

- [ ] **Step 1: Add a prototype and wrapper-parent test that currently fails.**

```csharp
[Test]
public async Task AllMappedYautjaMonitorsUseTheSpecializedPrototype()
{
    await using var pair = await PoolManager.GetServerClient();
    await pair.Server.WaitAssertion(() =>
    {
        var prototypes = pair.Server.ResolveDependency<IPrototypeManager>();
        var factory = pair.Server.EntMan.ComponentFactory;
        var ids = new[]
        {
            "CMUYautjaHunterShuttleHealthMonitor",
            "CMUHunterShipPlacedComputerCrewMonitoringCrewNorthOffset28x1",
            "CMUHunterShipPlacedComputerCrewMonitoringCrewNorthOffset4x1",
            "CMUHunterShipPlacedComputerCrewMonitoringCrewSouthOffset26x27",
            "CMUHunterShipPlacedComputerCrewMonitoringCrewSouthOffsetNeg2x32",
            "CMUHunterShipPlacedComputerCrewMonitoringSmallmonitorSouthOffset0x23",
        };

        foreach (var id in ids)
        {
            var prototype = prototypes.Index<EntityPrototype>(id);
            Assert.That(prototype.TryGetComponent<YautjaCrewMonitoringConsoleComponent>(out _, factory), Is.True, id);
            if (id != "CMUYautjaHunterShuttleHealthMonitor")
                Assert.That(prototype.Parents, Does.Contain("CMUYautjaHunterShuttleHealthMonitor"), id);
        }
    });
    await pair.CleanReturnAsync();
}
```

- [ ] **Step 2: Add the failing population, dead-state, rank, damage, and location test.**

The test must create a test map, spawn one `CMUMobYautja`, one `CMUMobYautjaYoungblood`, one `CMUMobYautjaBadBlood`, and one ordinary human. Place the three Yautja on valid grid coordinates, set the bad-blood component on its entity through its prototype, set rank components for the ordinary and Young Blood hunters, inject the following damage, and mark one hunter dead:

```csharp
var damageable = server.EntMan.GetComponent<DamageableComponent>(bloodedHunter);
damageable.Damage.DamageDict["Asphyxiation"] = 3;
damageable.Damage.DamageDict["Bloodloss"] = 2;
damageable.Damage.DamageDict["Poison"] = 7;
damageable.Damage.DamageDict["Radiation"] = 1;
damageable.Damage.DamageDict["Heat"] = 11;
damageable.Damage.DamageDict["Shock"] = 2;
damageable.Damage.DamageDict["Cold"] = 4;
damageable.Damage.DamageDict["Caustic"] = 3;
damageable.Damage.DamageDict["Blunt"] = 13;
damageable.Damage.DamageDict["Slash"] = 5;
damageable.Damage.DamageDict["Piercing"] = 2;
server.EntMan.System<MobStateSystem>().ChangeMobState(deadHunter, MobState.Dead);
```

Spawn `ComputerCrewMonitoring` and the specialized monitor on the same test grid, call the future `Refresh`, then assert exactly three entries, the ordinary human is absent, the dead hunter is present, and the entries contain:

```csharp
Assert.That(status.OxygenDamage, Is.EqualTo(5));
Assert.That(status.ToxinDamage, Is.EqualTo(8));
Assert.That(status.BurnDamage, Is.EqualTo(20));
Assert.That(status.BruteDamage, Is.EqualTo(20));
Assert.That(status.IsAlive, Is.False, "Dead Yautja remains listed");
Assert.That(status.CanTrack, Is.True);
Assert.That(status.LocationKind, Is.EqualTo(YautjaCrewMonitoringLocationKind.HuntingGround));
Assert.That(status.Area, Is.Not.Empty);
```

- [ ] **Step 3: Add the failing state-publication and ordinary-monitor regression tests.**

Open the specialized UI with `UserInterfaceSystem.TryOpenUi`, then inspect its state:

```csharp
Assert.That(ui.TryGetUiState<CrewMonitoringState>(monitor, CrewMonitoringUIKey.Key, out var state), Is.True);
Assert.That(state!.Sensors.Select(sensor => sensor.Name), Does.Contain("Blooded Hunter"));
```

For the ordinary monitor, raise a `DeviceNetworkPacketEvent` containing `CmdUpdatedState` and a valid `NET_STATUS_COLLECTION` dictionary on a `ComputerCrewMonitoring` entity without the Yautja marker. Assert that `CrewMonitoringConsoleComponent.ConnectedSensors` receives the packet. This proves the specialized bypass does not disable the normal path.

- [ ] **Step 4: Run the new integration test before implementation.**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaCrewMonitoringTest" --logger "console;verbosity=minimal"`

Expected: FAIL because the marker, refresh system, shared fields, and specialized UI publication do not exist.

## Task 3: Implement the authoritative Yautja collector and console specialization

**Files:**
- Create: `Content.Server/_CMU14/Yautja/YautjaCrewMonitoringConsoleComponent.cs`
- Create: `Content.Server/_CMU14/Yautja/YautjaCrewMonitoringConsoleSystem.cs`
- Modify: `Content.Server/Medical/CrewMonitoring/CrewMonitoringConsoleSystem.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaCrewMonitoringTest.cs`

**Interfaces:**
- `YautjaCrewMonitoringConsoleComponent` is a marker registered with `[RegisterComponent]` and access restricted to `YautjaCrewMonitoringConsoleSystem`.
- `YautjaCrewMonitoringConsoleSystem.Refresh(EntityUid monitor)` rebuilds the monitor's `CrewMonitoringConsoleComponent.ConnectedSensors` and publishes `CrewMonitoringState` when the UI is open.
- `YautjaCrewMonitoringConsoleSystem.TryBuildStatus(EntityUid target, out SuitSensorStatus status)` is a private collector helper used by `Refresh`.

- [ ] **Step 1: Add the marker component and wire the prototype-independent system shell.**

The marker has no mutable data. The system subscribes to `YautjaCrewMonitoringConsoleComponent` for `BoundUIOpenedEvent`, runs a three-second refresh cadence, and uses the existing `CrewMonitoringConsoleComponent` dictionary as the UI cache. `Refresh` must clear stale entities before rebuilding.

- [ ] **Step 2: Make the generic console system ignore only specialized monitors.**

At the top of `OnPacketReceived` and `OnUIOpened` in `CrewMonitoringConsoleSystem`, return when `HasComp<YautjaCrewMonitoringConsoleComponent>(uid)` is true. Do not alter packet parsing, power use, sensor timeout, or UI state construction for ordinary monitors.

- [ ] **Step 3: Implement direct Yautja filtering and status construction.**

Enumerate `YautjaComponent` with `TransformComponent`; do not enumerate Suit Sensors. `TryBuildStatus` must:

1. Reject deleted/nullspace entities and entities without valid transform coordinates.
2. Include all entities with `YautjaComponent`, including dead entities and Young Blood entities.
3. Detect `YautjaBadBloodComponent` before mapping the ordinary `ClanRank`.
4. Set `Name` from `MetaDataComponent.EntityName`.
5. Set `Job` from `Loc.GetString(YautjaCrewMonitoringMetadata.GetAssignment(yautja.ClanRank, isBadBlood))` and use `JobIconNoId`.
6. Set `JobDepartments` to a one-item list containing the localized area name.
7. Sum damage using these exact groups: Airloss (`Asphyxiation`, `Bloodloss`), Toxin (`Poison`, `Radiation`), Burn (`Heat`, `Shock`, `Cold`, `Caustic`), and Brute (`Blunt`, `Slash`, `Piercing`).
8. Set `TotalDamage` from `DamageableComponent.TotalDamage.Int()` and the existing Mob Threshold critical value when available.
9. Set `IsAlive` using `MobStateSystem.IsDead`.
10. Set `Coordinates`, `CanTrack`, and `LocationKind` only after valid coordinates are available.

Use `AreaSystem.TryGetArea` for the localized area label. If the area or coordinates cannot be resolved, omit the entry exactly as CMSS13 omits a human without a valid turf.

- [ ] **Step 4: Implement map-aware main-ship classification.**

Add a private helper with this behavior:

```csharp
private YautjaCrewMonitoringLocationKind GetLocationKind(EntityUid target)
{
    var grid = Transform(target).GridUid;
    return grid != null &&
           TryComp<BecomesStationComponent>(grid.Value, out var station) &&
           station.Id == "CMUYautjaHunterShip"
        ? YautjaCrewMonitoringLocationKind.MainShip
        : YautjaCrewMonitoringLocationKind.HuntingGround;
}
```

This intentionally classifies every valid non-Hunter-Ship location as the red hunting/non-ship side, matching the original two-color distinction without using area text. Keep the helper in the server system so the client only renders the authoritative enum.

- [ ] **Step 5: Publish the existing state and retain power/UI behavior.**

Use the same UI guard and nav-map preparation as the generic console: only call `SetUiState` while `CrewMonitoringUIKey.Key` is open and ensure the monitor grid has `NavMapComponent`. On `BoundUIOpenedEvent`, use the existing activatable-charge check, call `Refresh`, and never route the specialized console through `DeviceNetworkPacketEvent`.

- [ ] **Step 6: Run the parity integration tests and commit the server implementation.**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaCrewMonitoringTest" --logger "console;verbosity=minimal"`

Expected: PASS for collection, dead-state, rank, damage, location, state publication, and ordinary-monitor regression tests.

```powershell
git add -- Content.Server/_CMU14/Yautja/YautjaCrewMonitoringConsoleComponent.cs Content.Server/_CMU14/Yautja/YautjaCrewMonitoringConsoleSystem.cs Content.Server/Medical/CrewMonitoring/CrewMonitoringConsoleSystem.cs Content.IntegrationTests/_CMU14/Yautja/YautjaCrewMonitoringTest.cs
git commit -m "feat: add direct Yautja crew monitor collection"
```

## Task 4: Attach the specialized backend to the mapped monitors and localize its data

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaCrewMonitoringTest.cs`

**Interfaces:**
- `CMUYautjaHunterShuttleHealthMonitor` gains `YautjaCrewMonitoringConsole` while retaining `ComputerCrewMonitoring` as its parent.
- Existing five Hunter Ship wrapper prototypes continue to inherit `CMUYautjaHunterShuttleHealthMonitor`.
- Locale keys consumed by `YautjaCrewMonitoringMetadata` and the UI exist in both English and Russian.

- [ ] **Step 1: Add the marker to the base Yautja monitor prototype.**

Add exactly one component under `CMUYautjaHunterShuttleHealthMonitor`:

```yaml
    - type: YautjaCrewMonitoringConsole
```

Do not remove or replace `CrewMonitoringConsole`, `Computer`, `ActivatableUI`, `UserInterface`, `DeviceNetwork`, sprite, power, or `RemoveComponents`. The generic parent remains necessary for the common UI and machine surface.

- [ ] **Step 2: Add complete English and Russian localization keys.**

Add rank keys for `ancient`, `leader`, `elder`, `elite`, `blooded`, `youngblood`, `unblooded`, and `badblood`, plus a tooltip with named arguments for `area`, `oxygen`, `toxin`, `burn`, and `brute`. Add location labels for main ship and hunting ground if the UI displays them separately. Keep the existing `cmu-yautja-rank-*` keys untouched.

- [ ] **Step 3: Assert prototype and map parity.**

The integration test must assert the marker and parent chain for the base plus all five generated wrappers, and must load/count the existing placement records at:

```text
Resources/Maps/_CMU14/huntership.yml          4 instances
Resources/Maps/_CMU14/huntership_lower.yml    1 instance
Resources/Maps/_CMU14/Shuttles/hunter_shuttle.yml 1 instance
```

The test must also assert no `CrewMonitoringServer` is required by the specialized monitor prototype.

- [ ] **Step 4: Run prototype/map tests and commit.**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaCrewMonitoringTest|FullyQualifiedName~HunterShipYautjaMachineryTest" --logger "console;verbosity=minimal"`

Expected: PASS, with the pre-existing Hunter Ship visual asset blocker reported separately if it is still encountered.

```powershell
git add -- Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl Content.IntegrationTests/_CMU14/Yautja/YautjaCrewMonitoringTest.cs
git commit -m "feat: wire Yautja health monitor prototypes"
```

## Task 5: Adapt the existing Crew Monitor client window

**Files:**
- Create: `Content.Client/Medical/CrewMonitoring/CrewMonitoringFilter.cs`
- Create: `Content.Tests/Client/Medical/CrewMonitoring/CrewMonitoringFilterTest.cs`
- Modify: `Content.Client/Medical/CrewMonitoring/CrewMonitoringWindow.xaml.cs`

**Interfaces:**
- `CrewMonitoringFilter.Matches(SuitSensorStatus status, string query) -> bool` matches name, job/rank, and area case-insensitively; blank query matches every status.
- `CrewMonitoringWindow.ShowSensors` remains the existing public entry point used by `CrewMonitoringBoundUserInterface`.

- [ ] **Step 1: Write the failing pure filter tests.**

```csharp
[TestCase("blooded hunter", true)]
[TestCase("blooded", true)]
[TestCase("cryo chamber", true)]
[TestCase("marine", false)]
public void SearchMatchesNameRankAndArea(string query, bool expected)
{
    var status = new SuitSensorStatus(default, default, "Blooded Hunter", "Blooded", "JobIconNoId", ["Cryo Chamber"])
    {
        Area = "Cryo Chamber",
    };

    Assert.That(CrewMonitoringFilter.Matches(status, query), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run the client filter test and verify it fails.**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~CrewMonitoringFilterTest" --logger "console;verbosity=minimal"`

Expected: FAIL because the filter helper does not exist.

- [ ] **Step 3: Implement filter-driven rerendering.**

Store the last status list, monitor UID, and monitor coordinates in `CrewMonitoringWindow`. Subscribe to `SearchLineEdit.OnTextChanged` in the constructor and call a private render method that clears and repopulates the table from the stored list. Replace the current name/job-only condition with:

```csharp
if (!CrewMonitoringFilter.Matches(sensor, SearchLineEdit.Text))
    continue;
```

Do not change the generic grouping behavior for statuses whose `Area` is null or empty.

- [ ] **Step 4: Display Yautja details and authoritative map colors.**

Set the row button tooltip only when typed Yautja damage fields are present:

```csharp
if (sensor.OxygenDamage is { } oxygen &&
    sensor.ToxinDamage is { } toxin &&
    sensor.BurnDamage is { } burn &&
    sensor.BruteDamage is { } brute)
{
    sensorButton.ToolTip = Loc.GetString(
        "cmu-yautja-crew-monitor-tooltip",
        ("area", sensor.Area ?? string.Empty),
        ("oxygen", oxygen),
        ("toxin", toxin),
        ("burn", burn),
        ("brute", brute));
}
```

Use `MainShip` blue, `HuntingGround` red, and the existing green color for generic/unknown statuses when constructing nav-map blips. Keep list selection, nav-map centering, dead icon selection, and monitor blip behavior intact.

- [ ] **Step 5: Run client tests and commit the UI adaptation.**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~CrewMonitoringFilterTest" --logger "console;verbosity=minimal"`

Expected: PASS.

```powershell
git add -- Content.Client/Medical/CrewMonitoring/CrewMonitoringFilter.cs Content.Client/Medical/CrewMonitoring/CrewMonitoringWindow.xaml.cs Content.Tests/Client/Medical/CrewMonitoring/CrewMonitoringFilterTest.cs
git commit -m "feat: adapt Crew Monitor UI for Yautja status"
```

## Task 6: Run full parity verification and hand off safely

**Files:**
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaCrewMonitoringTest.cs`
- Verify only: all files changed by Tasks 1–5 and the pre-existing dirty worktree.

- [ ] **Step 1: Run shared metadata and client filter tests together.**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaCrewMonitoringMetadataTest|FullyQualifiedName~CrewMonitoringFilterTest" --logger "console;verbosity=minimal"`

Expected: PASS.

- [ ] **Step 2: Run the focused server/integration parity suite.**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaCrewMonitoringTest" --logger "console;verbosity=minimal"`

Expected: PASS for all new Yautja monitor tests, including ordinary Crew Monitor packet regression.

- [ ] **Step 3: Run the existing Hunter Ship machinery and visual regression tests.**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~HunterShipYautjaMachineryTest|FullyQualifiedName~HunterShipVisualRegressionTest" --logger "console;verbosity=minimal"`

Expected: all relevant assertions pass. If the run fails before assertions because of the already-known missing `pred_gear.rsi` or `pred_mask.rsi` assets, record that exact asset-only blocker and report the logic test result independently.

- [ ] **Step 4: Recheck source/map counts and working-tree scope.**

Run:

```powershell
$original = (rg -n '/obj/structure/machinery/computer/crew/alt/yautja' cmss13-ref-full/code/modules/cm_marines/marines_consoles.dm).Count
$middle = (rg -n 'proto: CMUHunterShipPlacedComputerCrewMonitoring' Resources/Maps/_CMU14/huntership.yml).Count
$lower = (rg -n 'proto: CMUHunterShipPlacedComputerCrewMonitoring' Resources/Maps/_CMU14/huntership_lower.yml).Count
$shuttle = (rg -n 'proto: CMUYautjaHunterShuttleHealthMonitor' Resources/Maps/_CMU14/Shuttles/hunter_shuttle.yml).Count
Write-Output "original=$original middle=$middle lower=$lower shuttle=$shuttle"
git diff --check
git status --short --branch
```

Expected counts: `original=5 middle=4 lower=1 shuttle=1`. The final status may still contain the pre-existing unrelated user changes, but no unrelated file may be staged by the feature commits.

- [ ] **Step 5: Review the final diff and commit only if all verification is green.**

Run: `git log --oneline --decorate -n 8` and `git show --stat --oneline HEAD` and inspect each feature commit. If any required test is red, keep the issue explicit and fix it before claiming completion. When all required checks are green, create the final integration commit:

```powershell
git add -- Content.Shared/_CMU14/Yautja/YautjaCrewMonitoring.cs Content.Shared/Medical/SuitSensor/SharedSuitSensor.cs Content.Server/_CMU14/Yautja/YautjaCrewMonitoringConsoleComponent.cs Content.Server/_CMU14/Yautja/YautjaCrewMonitoringConsoleSystem.cs Content.Server/Medical/CrewMonitoring/CrewMonitoringConsoleSystem.cs Content.Client/Medical/CrewMonitoring/CrewMonitoringFilter.cs Content.Client/Medical/CrewMonitoring/CrewMonitoringWindow.xaml.cs Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl Content.IntegrationTests/_CMU14/Yautja/YautjaCrewMonitoringTest.cs Content.Tests/Shared/Medical/CrewMonitoring/YautjaCrewMonitoringMetadataTest.cs Content.Tests/Client/Medical/CrewMonitoring/CrewMonitoringFilterTest.cs
git commit -m "feat: port Yautja Crew Monitor to CMU/RMC"
```

The final report must link the changed files, list every verification command and result, state the six local placement counts, and call out any remaining asset-only visual-test limitation.
