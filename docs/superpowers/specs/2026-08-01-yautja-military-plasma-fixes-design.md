# Yautja military plasma and HUD fixes — design

## Context

The dual plasma cannons are implemented as an entity stored in the cannon pack's internal slot container while deployed. The current server code handles `DroppedEvent`, but a thrown cannon reaches the throwing pipeline after the drop event and is left in the world. The cannon therefore needs a CMSS13-compatible return path for both ordinary drops and throws.

Popup strings are rendered by the plain-text popup UI. The Yautja power failure strings currently contain `[bold]...[/bold]`, which are displayed literally. Examine text uses a separate rich-markup path and must remain unchanged.

CMSS13 selects military Yautja HUD states by military caste: `soldierhud` for soldiers and `enforcerhud` for enforcers. The local client currently derives the Yautja status icon only from `ClanRank`, so military caste mobs do not expose those states.

The repository already contains the object/item RSI imported from CMSS13 at `mcaste_gear.rsi`. The requested sprite variant is the original on-mob DMI, imported as a separate directional RSI so worn layers do not reuse object sprites.

## Goals

- Return a thrown dual plasma cannon to its linked cannon pack instead of leaving it on the map.
- Preserve the existing ordinary-drop return behavior and the deployed/retracted action state.
- Ensure Yautja power/cannon popup text contains no literal `[bold]` tags.
- Display CMSS13 military HUD icons for soldier and enforcer Yautja.
- Import the original CMSS13 on-mob `mcaste_gear.dmi` into `mcaste_gear_worn.rsi` with the original states and directions.
- Use the worn RSI for the relevant `Clothing` layers while retaining the object RSI for world/item presentation.
- Add regression coverage before changing production behavior.

## Non-goals

- Do not replace the existing generic magnetic-item system.
- Do not change the cannon's fire cost, projectile, accuracy, or fire-rate balance.
- Do not change rich examine markup such as `<bold>`.
- Do not redraw, recolor, or otherwise reinterpret CMSS13 sprite art.
- Do not change tactical-map blips; the reported missing icons are the Yautja status/HUD icons selected from `hud_yautja.dmi`.

## Design

### 1. Cannon return on drop and throw

Keep `DroppedEvent` as the ordinary drop entry point and add a linked-cannon `ThrownEvent` handler in `YautjaCannonPackSystem`. Both paths validate that the cannon's linked pack still exists and that the pack still owns that cannon.

The shared retract operation will:

1. Drop the cannon from the user's hand when it is still held.
2. Insert it into the pack's internal `ContainerSlot` with force enabled.
3. Set `CannonsDeployed` to false.
4. Turn off the pack action.
5. When the event is a throw, stop the active throw so no impulse continues to move the entity.
6. Preserve the existing deactivation popup behavior for user-visible manual drops, without producing a duplicate popup for the throw pipeline.

This mirrors CMSS13's cannon `dropped()` behavior, where the cannon is moved directly into its source pack and the deployed state is reset. If the source pack is invalid or no longer owns the cannon, the handler will do nothing and the normal throw/drop behavior will remain available.

### 2. Popup text

Remove `[bold]` and `[/bold]` from the English and Russian localized strings used by `YautjaPowerSystem` and `YautjaCannonPackSystem` for popup notifications. Keep the interpolation values and wording intact. Existing examine strings continue to use `PushMarkup` and angle-bracket markup because they are rendered by the examine markup pipeline.

### 3. Military HUD caste selection

Add a shared, networked `YautjaMilitaryCasteComponent` with an enum value identifying `Soldier` or `Enforcer`. Add it to the two military mob prototypes. This keeps role identity explicit and avoids relying on prototype IDs or client-side job lookup.

Add health-icon prototypes backed by the original `hud_yautja.rsi` states:

- soldier icon → `soldierhud`;
- enforcer icon → `enforcerhud`.

Also import the original `_wl` states into the RSI so the asset set remains complete, but do not invent whitelist behavior for the current non-whitelisted event jobs. `YautjaHudSystem` will check the military component before the normal rank mapping and add the appropriate caste icon. Visibility rules remain the current CMSS13-compatible rules: the local Yautja sees its own icon, and other Yautja icons require the HUD viewer component.

### 4. On-mob military gear RSI

Extract the original CMSS13 `icons/mob/humans/onmob/hunter/mcaste_gear.dmi` into a new RSI directory:

`Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi`

Preserve all original states and their direction counts:

- one direction: `ARMOR`, `SHOES`, `HELMET`, `BACK`, `SHOULDER`;
- four directions: `fullarmor_soldier`, `fullarmor_soldier_lead`, `y-boots_powered`, `helmet_powered`, `cannonpack`, `plasma_cannons`.

The `Clothing` components for powered armor, greaves, powered helmet, and cannon pack will use the worn RSI. The existing object/item `Sprite` and `Item` components will continue to use `mcaste_gear.rsi`; their state names and gameplay behavior do not change. The dual cannons remain an internal held item and keep the existing object/item sprite configuration unless a worn-layer consumer is present.

## Verification strategy

Tests will be added or extended before production changes:

- an integration test that throws the deployed cannon and asserts it is back in the pack container, deployment is disabled, and the throw does not leave the cannon on the map;
- a localization regression test that checks the popup strings used for power failures contain no `[bold]` tags;
- a client/integration HUD test that spawns soldier and enforcer mobs and asserts `soldierhud`/`enforcerhud` status states are returned while preserving ordinary rank-icon behavior for other Yautja;
- an asset/prototype test that loads `hud_yautja.rsi` and `mcaste_gear_worn.rsi`, checks the CMSS13 HUD states and directional worn states, and verifies the relevant `Clothing` prototypes point at the worn RSI.

Focused integration tests will be run first, followed by the appropriate project build/test command. The client and server will only be restarted after verification succeeds.

## Operational handoff

After implementation and verification, inspect running project processes by executable and command line, stop only the old RussianCM client/server instances, start the repository's client and server launchers, and verify the new process IDs and logs. Unrelated processes and unrelated dirty worktree changes must remain untouched.
