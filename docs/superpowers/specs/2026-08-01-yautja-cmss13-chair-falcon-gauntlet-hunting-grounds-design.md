# Yautja CMSS13 Chair, Falcon, Gauntlet, and Hunting Grounds Parity

## Goal

Bring the four audited Yautja behaviors in the current port in line with the
CMSS13 reference without changing unrelated work already present in the
working tree:

1. A mob buckled to a directional Yautja chair faces the chair direction.
2. Chain gauntlet examination contains no obsolete `HARM`, `HELP`, `SHOVE`, or
   `GRAB` combat-intent instructions.
3. The deployed Falcon is hidden from ordinary living mobs, while Yautja can
   use/track it and ghosts/administrators retain visibility.
4. Hunting grounds use a non-destructible Preserve Shutter and escape-console
   flow matching CMSS13, including hunter-mask verification and the 15-second
   escape interaction.

## Current boundaries

The repository contains a large unrelated dirty working tree. Only files
needed for these four behaviors and their focused tests may be changed. The
implementation must reuse existing buckle, visibility, Falcon, and hunting
ground systems where possible; no broad renderer or map-system rewrite is
included.

## Design

### Directional Yautja chairs

The Hunter Ship chair wrappers already encode the visual direction with
`Sprite.overrideDir`. They will also encode the corresponding buckle rotation
on their `Strap` component. The buckle path already applies
`Strap.Rotation` to the buckled entity, so the change is data-driven and keeps
the generic buckle system intact.

All four directional wrappers (north, east, south, west) must set both visual
direction and strap rotation. A focused integration test will buckle a mob to
at least two differently oriented Hunter Ship chairs and assert that the
mob's `Transform` direction matches the chair direction. Existing buckle
eligibility and teleport/buckled tests remain unchanged.

### Chain gauntlet examination

The base item description remains the existing RMC-compatible text. The
Yautja-only `ExaminedEvent` handler will no longer append the CMSS13 combo
instructions. The handler may be removed if it has no remaining behavior; the
integration test will assert that examining the gauntlet produces no combat
intent guidance for either Yautja or non-Yautja examiners.

No attack, combo meter, or damage behavior is changed by this task.

### Falcon visibility

The deployed Falcon remains an entity with its existing world sprite and
Yautja HUD icon, but its world rendering will use a dedicated viewer-aware
visibility marker rather than globally visible `Sprite` data. The client
visibility decision is:

| Viewer | Falcon world sprite |
|---|---|
| Ordinary living non-Yautja | Hidden |
| Living Yautja / Falcon controller | Visible/usable |
| Ghost or administrator | Visible |

The implementation will follow an existing viewer-filter pattern in the
repository if one exists. Otherwise it will add the smallest dedicated
client-side system needed to filter Falcon visibility without changing global
FOV, line-of-sight, or interaction rules. The Yautja HUD/status icon remains
Yautja-only. The visibility test will cover an ordinary human, a Yautja, and a
ghost/admin-compatible viewer path available in the integration/client test
framework.

### Hunting grounds and escape gates

The existing lazy-loaded Jungle Moon, Desert Moon, and cave maps remain the
map source. The implementation will verify and complete the following
authority chain:

`hunt_ground_escape` console -> Yautja open/close command or hunter-mask
verification -> Preserve Shutter global signal -> all matching
`poddoor/yautja/hunting_grounds` shutters open/close.

The actual Preserve Shutter must be power-independent, unbreakable,
explosion-proof, unslashable, and unacidable. The escape console must reject
ordinary use, accept a held Yautja hunter mask for prey verification, require
15 seconds for the scan, and broadcast the escape event to Yautja. Internal
Yautja doors remain separate from the Preserve Shutter and are not converted
into escape gates.

Existing hunting-ground map and preserve-console integration tests will be
extended only for missing parity assertions: gate durability/properties,
mask-held verification, 15-second delay, and synchronized shutter state.

## Lifecycle and failure behavior

- Buckling to a chair without a configured strap rotation keeps the generic
  default angle; only corrected Hunter Ship chair prototypes opt into the
  directional behavior.
- Chain gauntlet examination has no extra combo guidance and must not fail if
  the examiner is not Yautja.
- Falcon visibility is a presentation decision only. It must not grant or
  remove control, HUD authorization, interaction range, or server-side
  existence. If the viewer is not classified, the safe default is hidden for
  ordinary living viewers and visible for observer/admin views according to
  existing engine conventions.
- Preserve Shutter operations are authoritative on the server. Failed mask
  verification, interruption, invalid access, or a missing target shutter
  leaves every shutter closed and emits no successful escape signal.

## Testing strategy

Tests are written before production changes and must first fail for the
audited behavior. Focused tests cover:

- directional buckle rotation for Hunter Ship chairs;
- absence of all four obsolete intent words from chain-gauntlet examine text;
- Falcon hidden/visible viewer matrix;
- hunting-ground shutter prototype properties;
- escape-console access, held-mask verification, 15-second delay, and global
  shutter synchronization.

Verification consists of `git diff --check`, focused integration/client tests,
and the smallest applicable build/test command for changed projects. Existing
unrelated modifications must remain unstaged and uncommitted.

## Acceptance criteria

- Buckled mobs face every directional Hunter Ship Yautja chair correctly.
- Chain gauntlet examine text contains none of `HARM`, `HELP`, `SHOVE`, or
  `GRAB` as combat instructions.
- Ordinary living non-Yautja viewers cannot see the deployed Falcon; Yautja,
  ghosts, and administrators retain the intended access/visibility.
- Hunting-ground escape behavior matches the specified Preserve Shutter and
  hunter-mask flow, with no regression to ordinary internal Yautja shutters.
- Focused tests and build verification provide fresh evidence for each claim.

