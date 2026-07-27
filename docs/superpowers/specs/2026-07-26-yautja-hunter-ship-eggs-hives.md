# Hunter Ship Alpha/Forsaken Eggs and Hives

## Goal

Перенести механику красных Alpha и лиловых Forsaken яиц из оригинального CMSS13 в текущую CMU/RMC entity-based архитектуру RussianCM на двух уровнях одновременно:

1. рабочие яйца и weeds на Hunter Ship;
2. самостоятельные Alpha/Forsaken ульи с корректным членством, цветом, передачей улья паразиту и отдельными NPC-отношениями.

## Current state

- `HiveComponent` and `HiveMemberComponent` already provide runtime hive identity.
- `CMUAlphaHive` already exists for chemistry/Ciphering and must not become a global lookup target for Hunter Ship eggs.
- `XenoEggSystem` already implements item, growing, grown, opening and parasite-spawn lifecycle.
- `XenoHiveColorVisualizerSystem` already applies `HiveColor` to any entity with `HiveMemberComponent`, so Alpha/Forsaken mobs do not need separate caste textures.
- The Hunter Ship active Forsaken egg and weed node are currently static visual prototypes in generated `huntership_visuals.yml`.
- Current `AutoAssignHiveComponent` resolves hives by global metadata name and does not retry or scope the lookup to a map.

## Design

### Hunter Ship bootstrap

Add shared components for Hunter Ship hive setup:

- `CMUHunterShipHiveBootstrapComponent` on the Hunter Ship station prototype;
- `CMUHunterShipHiveAssignmentComponent` on map eggs and the Forsaken weed node;
- `CMUHunterShipHiveKind` with `Alpha` and `Forsaken` values.

The server bootstrap system runs once when the Hunter Ship station starts, creates two hidden hives in nullspace from dedicated prototypes, and assigns every Hunter Ship assignment entity to the correct hive. This covers the middle, lower and upper map layers without adding runtime UIDs or editing generated map entity records.

Dedicated prototypes are used to avoid collisions with Ciphering:

- `CMUHunterShipAlphaHive`, red `#ff4040`;
- `CMUHunterShipForsakenHive`, lilac `#cc8ec4`.

The existing generic `CMUAlphaHive` remains unchanged for chemistry.

### Hive independence and NPC factions

Add optional `NpcFaction` to `HiveComponent`. A hive with this field synchronizes assigned xeno/parasite NPC faction membership when `SetHive` runs:

- dedicated Alpha and Forsaken factions replace the generic `RMCXeno` faction on their members;
- changing back to a normal hive restores `RMCXeno`;
- Alpha and Forsaken factions are hostile to each other and to the existing RMC xeno faction;
- their human hostility list matches the current RMC xeno hostility list.

Implement `FromSameHiveOrAlly` using the existing `IsAllyOfHive` rules, so all hive-aware construction and egg checks share one relation path.

### Egg behavior

Extend `XenoEggComponent` with `CanSpawnGhostParasite`, defaulting to `true`.

- Hunter Ship active Forsaken eggs set it to `false`; AI parasites can still hatch, but ghosts cannot claim a playable parasite role.
- Both the ghost verb and server BUI handler enforce this flag.
- `XenoEggSystem` uses the egg's assigned hive for activation, parasite inheritance and ally checks.
- `CanTrigger` explicitly excludes `YautjaComponent` and `SynthComponent` in addition to the existing infection checks.
- Xeno activation accepts members/allies of the egg hive rather than only the default hive path.
- A non-item egg declared with `state: Growing` initializes its fixtures through the normal egg state transition on component startup.

### Hunter Ship prototypes and visuals

- Convert `CMUHunterShipObjEffectAlienEggForsakenEggGrowingSouth` into a real `XenoEgg` with `Growing` state, Forsaken assignment, CMU effects RSI state names, and ghost parasite disabled.
- Convert `CMUHunterShipObjEffectAlienWeedsNodeForsakenWeednodeSouth` into a real `XenoHiveWeedsSource` with Forsaken assignment and the existing CMU weeds RSI art.
- Add Alpha assignments to all five red item egg wrappers and Forsaken assignments to all three lilac item egg wrappers.
- Remove hand-authored red/lilac layer colors from those wrappers; the shared hive color visualizer is the single color source.
- Keep the existing map coordinates and offsets unchanged.

No new raster textures are required. The existing CMU `effects.rsi` and `weeds.rsi` states are reused.

## Verification requirements

Add integration coverage for:

- dedicated Alpha/Forsaken hive prototypes and colors;
- Hunter Ship bootstrap creating exactly one Alpha and one Forsaken hive;
- all mapped egg/weed assignment components receiving the correct hive;
- an active Forsaken egg progressing from Growing to Grown;
- parasite spawned from an assigned egg inheriting that hive;
- `CanSpawnGhostParasite: false` suppressing the ghost role while leaving normal opening available;
- Yautja and synth targets being rejected by egg triggering;
- Alpha and Forsaken members having separate NPC factions and not being friendly through `RMCXeno` overlap;
- Hunter Ship visual regression counts remaining stable.

The implementation must leave the main checkout's existing `RobustToolbox`, `cmss13-ref`, and `cmss13-ref-full` changes untouched.
