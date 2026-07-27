Exit code: 0
Wall time: 0.2 seconds
Output:
# Yautja Dropship Destination Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure Hunter Ship landing destinations are offered and accepted only by the Yautja Hunter Shuttle, while preserving ordinary and standard ERT routing.

**Architecture:** Give the Hunter Shuttle and its three landing destinations a dedicated `yautja` route faction. Add one `DropshipSystem` authorization predicate called by both navigation-state construction and `FlyTo`, with an explicit exception for ephemeral tactical-landing destinations. Cover the observable UI partition and server-side launch rejection with integration tests.

**Tech Stack:** C#/.NET 10, RobustToolbox EntitySystem/BUI APIs, YAML entity prototypes, NUnit integration tests.

## Global Constraints

- Standard ERT consoles and destinations remain `thirdparty`.
- Yautja destinations use `FactionControlling: yautja`.
- Ordinary and ERT clients must not receive Hunter Ship destinations in `DropshipNavigationDestinationsBuiState`.
- A forged `DropshipNavigationLaunchMsg` must be rejected before destination ownership or FTL state changes.
- Existing destination-type, tactical-hover, return-vector, occupancy, and withdraw rules remain unchanged.
- Do not modify unrelated dirty-worktree files.

---

### Task 1: Add the failing route-isolation integration test

**Files:**
- Create: `Content.IntegrationTests/_CMU14/Yautja/HunterShipDropshipDestinationIsolationTest.cs`

**Interfaces:**
- Consumes: existing `DropshipNavigationDestinationsBuiState`, `UserInterfaceSystem`, `DropshipSystem.FlyTo`, and the Hunter/ERT console and destination prototypes.
- Produces: regression coverage that fails against the current shared `thirdparty` configuration and proves UI filtering plus server launch authorization.

- [ ] **Step 1: Write the failing test**

Create a `PoolManager.GetServerClient()` fixture that creates one test grid, spawns these destination prototypes, and opens navigation UIs for `CMComputerDropshipNavigationThirdParty`, `CMComputerDropshipNavigationOpfor`, and `CMUYautjaHunterShuttleConsole`:

~~~csharp
private static readonly string[] HunterDestinationPrototypes =
[
    "CMUHunterShipYautjaLandingPadAFTLBeacon",
    "CMUHunterShipYautjaLandingPadBFTLBeacon",
    "CMUHunterShipYautjaHangarA",
];

// After TryOpenUi for each console, read DropshipNavigationDestinationsBuiState.
// ERT and ordinary states must exclude all three destination NetEntities;
// the Yautja state must contain all three.
Assert.That(ertState!.Destinations.Select(x => x.Id).Intersect(hunterNetEntities),
    Is.Empty);
Assert.That(ordinaryState!.Destinations.Select(x => x.Id).Intersect(hunterNetEntities),
    Is.Empty);
Assert.That(hunterNetEntities.IsSubsetOf(yautjaState!.Destinations.Select(x => x.Id)),
    Is.True);
~~~

Use a human viewer for ERT and ordinary consoles and `CMUMobYautja` for the Yautja console, so the test exercises the real `YautjaShuttleConsole` access guard. Assert the resolved whitelist factions (`thirdparty`, `opfor`, `yautja`) and all three destination `FactionController` values as part of the same fixture.

Add a second test in this fixture for direct authorization. Give the test grid a `ShuttleComponent`, call the server `DropshipSystem.FlyTo` from ERT and ordinary consoles toward a Hunter destination, and assert `false`, no `FTLComponent`, and unchanged destination `Ship`. Call it from the Yautja console and assert `true` with the destination assigned. Use real prototype components; do not mock the authorization path.

- [ ] **Step 2: Run the focused test to verify the expected red state**

Run:

~~~powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~HunterShipDropshipDestinationIsolationTest" --logger "console;verbosity=minimal"
~~~

Expected: failure because Hunter destinations are currently `thirdparty`, ERT UI state contains them, and the current `FlyTo` path accepts the non-Yautja request. Fix only test setup errors before proceeding.

### Task 2: Isolate route ownership in prototypes

**Files:**
- Modify: `Resources/Prototypes/_CMU14/Maps/huntership_support.yml:191-218`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml:4953-4974`
- Test: `Content.IntegrationTests/_CMU14/Yautja/HunterShipDropshipDestinationIsolationTest.cs`

**Interfaces:**
- Consumes: the failing integration test from Task 1.
- Produces: `yautja` ownership for the three Hunter destinations and Hunter Shuttle without changing ERT prototypes.

- [ ] **Step 1: Change only the prototype route values**

Change all three Hunter `FactionControlling: thirdparty` values to `FactionControlling: yautja`. Override the inherited whitelist component in `CMUYautjaHunterShuttleConsole` with:

~~~yaml
- type: WhitelistedShuttle
  faction: yautja
  ShuttleType: Dropship
  autoReturn: false
~~~

Keep the existing access, `YautjaShuttleConsole`, navigation, and tactical-landing settings intact. Do not edit standard ERT route prototypes.

- [ ] **Step 2: Re-run the focused test**

Run the Task 1 command. Expected: prototype and UI ownership assertions pass; direct ERT/ordinary `FlyTo` assertions still fail because server authorization has not yet been added.

### Task 3: Centralize and enforce destination authorization

**Files:**
- Modify: `Content.Server/_RMC14/Dropship/DropshipSystem.cs:516-555,824-859`
- Test: `Content.IntegrationTests/_CMU14/Yautja/HunterShipDropshipDestinationIsolationTest.cs`

**Interfaces:**
- Consumes: `WhitelistedShuttleComponent.Faction`, `DropshipDestinationComponent.FactionController`, `EphemeralDropshipDestinationComponent`, and Task 2 route ownership.
- Produces: `private bool CanUseDestination(EntityUid navConsole, EntityUid destination, DropshipDestinationComponent destinationComp)` used by both `RefreshUI` and `FlyTo`.

- [ ] **Step 1: Add and use the shared predicate in `RefreshUI`**

Implement these semantics:

~~~csharp
private bool CanUseDestination(EntityUid navConsole, EntityUid destination,
    DropshipDestinationComponent destinationComp)
{
    if (HasComp<EphemeralDropshipDestinationComponent>(destination))
        return true;

    string? faction = null;
    if (TryComp(navConsole, out WhitelistedShuttleComponent? whitelist) &&
        !string.IsNullOrWhiteSpace(whitelist.Faction))
    {
        faction = whitelist.Faction;
    }

    if (IsStrictThirdPartyFaction(faction))
        return IsThirdPartyDestination(destinationComp);

    if (string.IsNullOrWhiteSpace(destinationComp.FactionController))
        return true;

    return !string.IsNullOrWhiteSpace(faction) &&
           string.Equals(destinationComp.FactionController, faction,
               StringComparison.OrdinalIgnoreCase);
}
~~~

Replace the duplicated faction block in `RefreshUI` with `if (!CanUseDestination(computer.Owner, uid, comp)) continue;`. Keep the existing UI-only ephemeral skip so tactical-only points do not appear as normal destinations.

- [ ] **Step 2: Enforce the predicate before `FlyTo` mutates state**

At the start of `DropshipSystem.FlyTo`, after resolving the destination component and before changing `Ship`, `Destination`, landing lights, or FTL state, reject unauthorized routes:

~~~csharp
if (!TryComp(destination, out DropshipDestinationComponent? destinationComp) ||
    !CanUseDestination(computer.Owner, destination, destinationComp))
{
    if (user is { } actor)
        _popup.PopupEntity("This shuttle cannot use that landing destination.",
            computer.Owner, actor, PopupType.MediumCaution);

    Log.Warning($"{ToPrettyString(user)} tried to route {ToPrettyString(computer.Owner)} " +
                $"to unauthorized destination {ToPrettyString(destination)}");
    return false;
}
~~~

Remove the old strict-third-party-only block once the new predicate is in place. Retain the separate third-party return-vector ownership check and every later flight-state guard.

- [ ] **Step 3: Run the focused test to verify green**

Run the Task 1 command. Expected: ERT and ordinary UI states exclude Hunter destinations, Yautja state includes them, forbidden direct launches return `false` without FTL mutation, and the Yautja launch is accepted.

### Task 4: Full verification and handoff

**Files:**
- Modify: none beyond Tasks 1вЂ“3.
- Test: `HunterShipDropshipDestinationIsolationTest`, `RmcErtThirdPartyDropshipMapTest`, and `HunterShipDockingTest`.

**Interfaces:**
- Consumes: completed prototype and server authorization changes.
- Produces: verified regression coverage with unrelated dirty-worktree changes untouched.

- [ ] **Step 1: Run focused neighboring tests**

~~~powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~HunterShipDropshipDestinationIsolationTest|FullyQualifiedName~RmcErtThirdPartyDropshipMapTest|FullyQualifiedName~HunterShipDockingTest" --logger "console;verbosity=minimal"
~~~

Expected: selected tests pass with zero failed tests.

- [ ] **Step 2: Run build and whitespace checks**

~~~powershell
dotnet build Content.Server/Content.Server.csproj --no-restore --no-incremental
git diff --check
~~~

Expected: build exit code 0 and no whitespace errors.

- [ ] **Step 3: Inspect and commit only task files**

~~~powershell
git status --short
git diff -- Content.Server/_RMC14/Dropship/DropshipSystem.cs Resources/Prototypes/_CMU14/Maps/huntership_support.yml Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml Content.IntegrationTests/_CMU14/Yautja/HunterShipDropshipDestinationIsolationTest.cs
git add -- Content.Server/_RMC14/Dropship/DropshipSystem.cs Resources/Prototypes/_CMU14/Maps/huntership_support.yml Resources/Prototypes/_CMU14/Threats/Yautja/Structures/structures.yml Content.IntegrationTests/_CMU14/Yautja/HunterShipDropshipDestinationIsolationTest.cs
git commit -m "fix: isolate Yautja dropship destinations"
~~~


