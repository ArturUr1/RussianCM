# Hunter Ship Tactical Map Hive Filter Design

## Goal

Ensure Alpha and Forsaken xenonids only receive tactical-map blips for entities belonging to their own hive, while preserving the global map data used by admins and ghosts.

## Current Problem

`TacticalMapSystem.UpdateUserData` currently copies the complete `TacticalMapComponent.XenoBlips` and `XenoStructureBlips` dictionaries into every xenonid user's private component. Hunter Ship Alpha and Forsaken are separate `HiveComponent` entities, so this exposes the other hive's xenonids and structures through the xenonid tactical-map ability.

## Design

Keep the authoritative tactical-map dictionaries unchanged. When preparing data for a `TacticalMapUserComponent` with `Xenos = true`, resolve the user's `HiveComponent` and filter both xeno dictionaries to entities whose `HiveMemberComponent.Hive` equals that hive. If the user has no hive, retain only the user's own xeno blip and no foreign structure blips.

The existing `Watch Xenonid`, `HiveTracker`, and psychic-order same-hive checks remain unchanged because they already enforce the invariant at their own access boundaries.

## Verification

Add an integration test that creates Alpha and Forsaken hives, one xeno member and one hive-owned structure per hive, then invokes `UpdateUserData` for Alpha and Forsaken users. Each user must receive their own xeno/structure blips and must not receive the other hive's blips. The test must fail before the filter and pass after it.
