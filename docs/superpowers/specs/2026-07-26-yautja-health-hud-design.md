# Yautja Health HUD Port Design

**Date:** 2026-07-26

**Goal:** Give the CMU/RMC Yautja mask the CMSS13-equivalent ability to show biological and xenonid health state while preserving the existing Yautja mark/Falcon HUD.

## Current gap

`CMUYautjaMask` currently provides `YautjaMask`, while `YautjaMaskSystem` grants `YautjaHudViewerComponent` for marks and Falcon-related status icons. The existing RMC equipment HUD pipeline already supports `ShowHealthBars` and `ShowHealthIcons`, but the mask does not declare either component.

Human and Yautja mobs use the `Biological` damage container; xenonids use `Xeno`. The generic RMC health bar overlay filters by those containers. The generic health-icon system currently resolves only `RMCHealthIconsComponent`, which is present on biological RMC mobs but not on xenonids. Xenonid health rendering already exists in `XenoHudOverlay.UpdateHealth`, but that overlay is restricted to xenonid/ghost viewers.

## Chosen architecture

1. Declare `ShowHealthBars` and `ShowHealthIcons` directly on `CMUYautjaMask` with `Biological` and `Xeno` containers. Do not inherit `ShowMedicalIcons`, because that abstract prototype also adds `HolocardScanner`, which is not part of the CMSS13 Yautja mask HUD.
2. Make `ShowHealthIconsSystem` honor its configured damage-container list.
3. Extract the existing xenonid health-state calculation into a reusable client helper/system. It will create a dynamic `StatusIconData` using the existing xenonid HUD RSI states, preserving the current 11-step rounding and critical/dead states.
4. Register that provider with `ShowHealthIconsSystem`; the existing `EquipmentHudSystem` and inventory relay remain responsible for activating/deactivating the overlay when the mask is equipped or removed.
5. Keep `YautjaHudSystem` and `YautjaMaskSystem` mark, clan, Falcon, visor, and zoom behavior unchanged.

## Boundaries

- This displays visual health bars/icons, not exact numeric HP.
- The mask health HUD covers `Biological` and `Xeno`; it does not add Holocard or unrelated security/faction overlays.
- All mask variants inheriting `CMUYautjaMask` receive the same health HUD, including thrall and bad-blood variants, matching inherited CMSS13 mask HUD behavior.
- Existing non-Yautja medical HUDs keep their current behavior except for the container-filter correctness fix.

## Verification

- Prototype test asserts both HUD components and both container IDs on `CMUYautjaMask`.
- Unit tests cover xenonid healthy/critical/dead state names and boundary rounding.
- Client/integration tests cover health-icon filtering and mask equip/unequip activation without changing existing Yautja mark/Falcon HUD assertions.
- Targeted test projects and the affected client/server build are run before completion.
