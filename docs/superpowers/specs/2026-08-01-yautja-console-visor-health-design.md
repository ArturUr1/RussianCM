# Yautja Console, Thermal Visor, and Xeno Health Design

**Status:** Approved by the user on 2026-08-01.

## Goal

Ensure that every Yautja-related console command requires the `Clans` admin flag, keep the Yautja mask's thermal vision able to reveal mobs through walls using the existing server-authoritative visor link, and prevent the mask from displaying health information for dead xenonids.

## Current findings

- Nine Yautja console commands are registered across `Content.Server/_CMU14/Yautja` and `Content.Server/Administration/Commands/Yautja*.cs`.
- Only `yautja_clan_admin` currently uses `AdminFlags.Clans`; other Yautja commands use weaker or missing command attributes.
- Thermal wall vision is already implemented by a linked, networked visor-glasses component and a client world overlay. The overlay renders eligible mobs independently of normal wall occlusion.
- `CMUYautjaMask` enables health icons for the `Xeno` damage container. `CMXenoHealthIconState` currently returns `xenohealth0` for `MobState.Dead`, which exposes a dead xeno's health state.

## Design

### Command authorization

Set `[AdminCommand(AdminFlags.Clans)]` on all nine Yautja command classes. Keep the existing EUI-side `Clans` checks as defense in depth. Add a reflection-based regression test covering the complete command inventory and asserting that every command attribute has exactly `AdminFlags.Clans`.

### Thermal visor

Retain the existing server-authoritative lifecycle:

1. The mask must be worn by the user.
2. The user must be allowed to use Yautja technology and have a worn Yautja power source.
3. Enabling the visor creates and equips linked visor glasses with `ThermalVisionEnabled = true`.
4. The client overlay renders only same-map, visible, uncontained mobs while the linked visor is active.

The overlay remains a visual wall-vision pass and does not synthesize health data. Its eligibility predicate stays covered by shared unit tests.

### Dead xenonid health information

Change `CMXenoHealthIconState.GetState` to return `null` for `MobState.Dead`. This removes the xeno health status icon while leaving the dead xenonid's normal sprite and thermal visibility intact. Alive and critical xeno health states remain unchanged.

## Testing and verification

- Unit test command authorization for all nine Yautja commands.
- Update the xeno health icon unit test to assert no icon state for dead xenos.
- Preserve and run the thermal visor eligibility tests.
- Build `Content.IntegrationTests` and run focused tests for Yautja authorization, visor behavior, and xeno health icons.
- Run `git diff --check`, inspect fresh server/client logs, and verify both binaries are running after rebuild.

## Scope exclusions

- No new shader or rendering pipeline is introduced.
- No Yautja combat or medical mechanics are changed.
- No health data is removed from analyzers or crew-monitoring consoles; only the mask's dead-xeno health icon is suppressed.
