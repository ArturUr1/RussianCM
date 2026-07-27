# Yautja Crew Monitor: CMSS13 parity in CMU/RMC

## Status

Approved direction; awaiting written-spec review before implementation.

## Goal

Port the CMSS13 Yautja health monitor to CMU/RMC so that every mapped Yautja monitor on the Hunter Ship and shuttle has the same gameplay data and filtering semantics as the original, while using the existing CMU/RMC powered computer and crew-monitor window.

The port must not depend on ordinary human suit sensors. Yautja armor and masks are profile-driven and do not currently provide `SuitSensor`, while the original CMSS13 monitor reads the Yautja population directly.

## Source behavior to preserve

The reference implementation is `/obj/structure/machinery/computer/crew/alt/yautja` and `/datum/crewmonitor/yautja` in `cmss13-ref-full/code/modules/cm_marines/marines_consoles.dm`.

The monitor:

1. Is a Yautja health monitor with the Yautja console visual and description.
2. Includes the normal Yautja faction and the Young faction.
3. Iterates all human mobs, keeps only Yautja, and excludes every other faction.
4. Includes entries even when the Yautja is dead, provided a valid position exists.
5. Reports name, rank/assignment, oxygen, toxin, burn, and brute damage, tracking capability, area, and whether the target is on the main ship.
6. Maps the original presets to Ancient, Leader, Elder, Elite, Blooded, Young Blood, Unblooded, or Bad Blood.
7. Shows the main ship in blue and hunting locations in red on the map.

CMU/RMC equivalents are:

- `YautjaComponent` identifies a Yautja entity, including ordinary, Young Blood, and Bad Blood variants.
- `YautjaComponent.ClanRank` and `YautjaRankMetadata` are the authoritative local rank model.
- `AreaSystem` supplies the current area name.
- `DamageableComponent.Damage.DamageDict` supplies the current damage values.
- `MobStateSystem` supplies alive/dead state.
- `TransformComponent` and `SharedTransformSystem` supply a trackable position.

## Chosen architecture

### Console specialization

Add a server-only marker component for the specialized console, for example `YautjaCrewMonitoringConsoleComponent`, and a specialized system under the CMU14 Yautja namespace.

The existing `ComputerCrewMonitoring` parent remains the source of power handling, computer interaction, UI wiring, and device-network compatibility. The generic `CrewMonitoringConsoleSystem` must detect the Yautja marker and skip packet-driven data handling for that entity. The specialized system owns the Yautja data cache and publishes the same `CrewMonitoringState` used by the existing bound UI.

This keeps ordinary Crew Monitor behavior unchanged and prevents the Yautja console from showing an unrelated station Suit Sensor feed.

### Direct population scan

The specialized system periodically rebuilds the status list while at least one Yautja monitor exists. The scan uses the authoritative server entity state and does not require:

- `CrewMonitoringServer` on the Hunter Ship;
- a `SuitSensor` component on Yautja equipment;
- a Yautja `DeviceNetwork` transmitter;
- an equipped armor or mask prototype to opt into the monitor.

The existing monitor update cadence may be reused, but opening the UI must immediately publish the latest cached/rebuilt list just as the ordinary monitor does.

### Status contract

Extend the shared crew-monitor status payload rather than creating a second incompatible UI protocol. Existing suit-sensor fields retain their meaning. The status must additionally carry:

- four typed damage values: oxygen, toxin, burn, and brute;
- area label;
- location side/kind (`MainShip` or `HuntingGround`/other non-ship location);
- a rank identifier suitable for localization and client-side display;
- a tracking flag, which is true whenever valid coordinates are sent.

For a direct Yautja entry, the Yautja entity is both the owner and the tracking source identifier. This lets the existing nav-map selection logic work without inventing a fake sensor entity. Generic suit-sensor entries continue to populate the same payload with their current fields.

The aggregate damage and health icon used by the current window are calculated from the same four values. Typed damage remains in the payload so a Yautja detail row or tooltip can expose the exact CMSS13-equivalent values instead of losing them in an aggregate.

### Filtering and display

Reuse `CrewMonitoringWindow` and adapt it functionally:

- show Yautja name and localized rank as the job/assignment column;
- group entries by area, preserving the existing department-list mechanism;
- extend the search to name, rank, and area;
- keep dead entries visible, with the existing dead status icon;
- show exact oxygen/toxin/burn/brute values in the row tooltip or details area;
- draw main-ship entries blue and non-main-ship hunting entries red;
- keep the existing coordinate selection, centering, and list synchronization;
- continue to show the monitor itself on the nav map.

The ordinary monitor keeps its current name/job/departments behavior. Yautja-specific labels and colors are driven by status fields, not by client guesses about map names.

### Rank mapping

Use `YautjaComponent.ClanRank` as the single source of truth. The display mapping is:

| CMU/RMC rank | CMSS13 assignment | Display source |
| --- | --- | --- |
| `Ancient` | `CLAN_RANK_ADMIN` | `YautjaRankMetadata.For(Ancient)` |
| `Leader` | `CLAN_RANK_LEADER` | `YautjaRankMetadata.For(Leader)` |
| `Elder` | `CLAN_RANK_ELDER` | `YautjaRankMetadata.For(Elder)` |
| `Elite` | `CLAN_RANK_ELITE` | `YautjaRankMetadata.For(Elite)` |
| `Blooded` | `CLAN_RANK_BLOODED` | `YautjaRankMetadata.For(Blooded)` |
| `YoungBlood` | `CLAN_RANK_YOUNG` | `YautjaRankMetadata.For(YoungBlood)` |
| `Unblooded` | `CLAN_RANK_UNBLOODED` | `YautjaRankMetadata.For(Unblooded)` |
| `BadBlood` prototype/job role | `JOB_BADBLOOD` | explicit Bad Blood display fallback/metadata |

The current enum has no `BadBlood` value. Bad Blood must therefore be represented explicitly by the existing `YautjaBadBloodComponent`/job identity or an equivalent authoritative marker, without changing ordinary rank persistence rules. The UI must never infer Bad Blood from a client-supplied profile.

### Main-ship classification

Implement the CMSS13 `is_mainship_level` distinction through a single server-side helper used by the collector. It must classify the Hunter Ship/main ship grids as `MainShip` and all other valid Yautja locations as hunting/non-ship locations. The helper must be map/grid aware and must not rely on localized area text, so moving or renaming an area cannot change the color semantics.

When a target has no valid area or coordinates, omit the entry in the same way the original omits targets with no turf. A valid coordinate always sets `CanTrack = true`; an absent coordinate sets it false and the row remains visible only if the source status can still be represented safely.

## Prototype and map changes

Update `CMUYautjaHunterShuttleHealthMonitor` so it has the specialized marker while retaining the existing powered computer, UI, sprite, and interaction inheritance. Do not add a generic Crew Monitoring Server to the Hunter Ship solely for this feature.

Preserve every existing mapped monitor instance:

- four middle-deck monitors in `Resources/Maps/_CMU14/huntership.yml`;
- one lower-deck monitor in `Resources/Maps/_CMU14/huntership_lower.yml`;
- the shuttle monitor in `Resources/Maps/_CMU14/Shuttles/hunter_shuttle.yml`.

The port is complete only when all six local instances resolve to the specialized prototype and the CMSS13 reference count remains five original Yautja console instances.

## Tests and acceptance criteria

Write tests before production implementation. The test coverage must include:

1. Prototype parity: the local monitor has the Yautja name/description, specialized marker, powered computer/UI components, and no accidental dependency on a Crew Monitoring Server.
2. Map parity: the five Hunter Ship positions and shuttle position use the specialized prototype; no instance is lost or changed to a generic monitor.
3. Population filtering: ordinary humans and non-Yautja entities are excluded; ordinary Yautja, Young Blood, and Bad Blood are included.
4. Dead-state parity: dead Yautja remain listed when they have valid coordinates.
5. Rank parity: all rank mappings above produce the expected localized/display identifier, including the Bad Blood fallback.
6. Damage parity: oxygen, toxin, burn, and brute values are copied independently and aggregate health remains consistent.
7. Location parity: main-ship entries are classified blue, hunting entries red, and areas are included in the payload.
8. UI parity: opening the specialized monitor returns a `CrewMonitoringState`, search matches name/rank/area, dead entries remain visible, and nav-map selection tracks the source entity.
9. Regression: ordinary suit-sensor Crew Monitoring continues to receive and display its network status collection.

The targeted integration test command must be run after implementation. If map visual tests are blocked by unrelated missing reference assets, report that exact blocker separately from logic/test results; do not treat a build-only result as parity proof.

## Non-goals

- Pixel-perfect cloning of the old CMSS13 TGUI layout.
- Adding Suit Sensor components to Yautja armor, masks, or every Yautja mob.
- Creating a second rank authority or allowing profile data to grant a higher rank.
- Changing ordinary station Crew Monitor networking or behavior.
- Adding or removing map monitors beyond the existing CMSS13-equivalent placements.

## Definition of done

The specialized console opens in CMU/RMC, displays the complete authoritative Yautja population with the CMSS13 fields and rank/faction semantics, keeps dead hunters visible, colors and tracks locations correctly, and passes the parity/regression tests above. The final report must include the changed files, test commands and results, and any remaining asset-only limitation.
