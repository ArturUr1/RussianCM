# Yautja Medicomp Pocket Fit

## Goal

Allow the Yautja medicomp loadout item to be equipped in a Yautja pocket without changing the medicomp's own contents or storage behavior.

## Design

Change only the outer `Item.size` on the shared `CMUYautjaMedicomp` prototype from `Normal` to `Small`. The filled variants (`CMUYautjaMedicompFull`, `CMUYautjaMedicompSurvivor`, and `CMUYautjaMedicompThrall`) inherit that size automatically. Keep `Storage.maxItemSize: Normal` unchanged so the medical tools and cases inside the medicomp continue to fit.

Add a focused regression test that loads the Yautja medicomp prototypes and asserts that the base and filled variants have `Small` outer item size. The existing Yautja spawn tests continue to verify that the variants are assigned to `pocket2`.

## Scope and compatibility

No inventory-slot rules, storage grid, whitelist, contents, sprites, or Yautja starting-gear assignments change. The fix is limited to the item's external pocket compatibility and is inherited by all medicomp variants.

## Verification

Run the focused medicomp pocket regression test, the existing Yautja predator-role tests that cover `pocket2`, and `git diff --check`.
