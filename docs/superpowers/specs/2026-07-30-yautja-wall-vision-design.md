# Yautja Wall Vision

## Goal

Bring Yautja vision in the current build to parity with CMSS13: Yautja can see mob sprites through opaque walls as an innate ability, while the Bio-Mask visor remains responsible only for low-light vision.

## Scope

- Add a client-side wall-vision overlay activated by the local player's `YautjaComponent`.
- Render eligible mob sprites in world space above the normal FOV occlusion layer.
- Keep ordinary FOV, lighting, walls, structures, and object visibility unchanged.
- Keep the current visor power and toggle flow unchanged; visor night vision must not be the source of wall vision.
- Do not reveal entities stored inside containers.

## Architecture

The local player's networked `YautjaComponent` is the source of the innate ability. A dedicated client overlay is attached while the local player is a Yautja and removed when the component/player is detached or removed.

Each frame, the overlay queries mobs intersecting the current world bounds, filters to the current map, excludes the local player, and renders their existing sprites through the normal `SpriteSystem`. Rendering in the world-space overlay ensures these sprites are visible above the hard FOV mask while retaining the entity's normal sprite, animation, color, and visibility state.

The overlay will skip mobs that are inside storage/container hierarchies. It will not change server-side line-of-sight checks, interaction range, targeting rules, lighting, or the behavior of other species.

## Visor behavior

`CMUYautjaNightVisionGlasses` continues to use `NightVisionItem` with its current full-state low-light configuration. Enabling or disabling the visor changes only night-vision state and power drain. Wall vision remains available when the visor is disabled, matching CMSS13's species-level `SEE_MOBS` behavior.

## Testing

- Add focused tests for the wall-vision target-selection rules: Yautja source, current map, mob target, self exclusion, and container exclusion.
- Add a prototype regression assertion that the Yautja visor remains a night-vision item and does not accidentally become the source of wall vision.
- Run the focused tests and the relevant project build/test command before reporting completion.

## Non-goals

- No general engine-wide replacement for FOV or visibility masks.
- No thermal coloring, heat signatures, health bars, or target highlighting.
- No changes to mask zoom, power drain, or visor authorization rules.
