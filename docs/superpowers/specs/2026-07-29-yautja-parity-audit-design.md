# Yautja parity audit and targeted fixes

## Goal

Audit the current CMU Yautja implementation against the bundled CMSS13 reference and fix the confirmed behavior gaps without broad unrelated refactoring. The scope covers ship chemistry visuals, ground relay placement, defibrillation, weapon transfer, self-destruct parameters, and throwing through the existing `ThrowItemInHand` action (the client action that may be bound to Ctrl+Q).

## Scope and decisions

### Ship chemistry visuals

Extend `SolutionContainerVisuals` with an optional independent fill RSI. The base and lid layers may remain in a ship-specific RSI while the dynamic fill layer uses the existing RMC asset. Configure the large silver beaker and ship vial wrappers to use the standard large-beaker/vial fill states. Preserve solution colorization and current in-hand/icon behavior.

Add a prototype-level validation for solution containers: every configured fill layer must have a fill base name and all required states in the RSI actually used by that layer. This covers the ship containers and similar examples.

### Ground relay points

Treat the 17 primary `InRotation` planet maps in `planets.yml` as the playable CMU map set for ground relay validation. Relocate every `CMUYautjaGroundRelayDestination` to an accessible open location at least 12 tiles from classified human infrastructure. Human infrastructure includes human spawns, walls, doors, consoles, furniture, and other map objects representing a human structure; natural rocks and vegetation are excluded.

Extend the existing Yautja map integration test to require a ground relay marker on every in-rotation map and validate the 12-tile minimum distance.

### Defibrillation

Match the original standard-defibrillator behavior: Yautja cannot be defibrillated. Add an early Yautja guard to the current defibrillator validation path and cover it with an integration test that confirms a dead Yautja is rejected and remains dead. Do not block unrelated revival mechanisms or ordinary humans.

### Weapon transfer and throwing

Use the existing `ThrowItemInHand` action path rather than hard-coding a server-side Ctrl+Q binding. Yautja tech permission remains the authorization boundary. Chained combistick/war-axe charge gating remains enforced; health shards and stored attachments remain non-weapon exceptions.

Make the existing `ItemThrowRange` component effective in `HandsSystem`, preserving source ranges such as the harpoon's four tiles. Audit all concrete Yautja weapon and throwable-device prototypes against `cmss13-ref-full/code/modules/cm_preds/yaut_weapons.dm` and related device sources. Add focused tests for throw permission, chained-weapon gating, range behavior, and representative weapon categories. Report ambiguous or intentionally different force/damage/speed values rather than changing them without a clear source mapping.

The exact Ctrl+Q default is a client setting and is not stored in this repository. The acceptance criterion is that the `ThrowItemInHand` action succeeds for every item that the source implementation allows a Yautja to throw.

### Self-destruct

Preserve CMSS13 intensity pairs: large `600/50`, small `800/550`. For the large predator self-destruct, pass `maxTileBreak: 0` so the explosion cannot excavate tiles beneath the Yautja toward space. Keep the current small-mode tile behavior unless the source audit proves otherwise. Test both intensity pairs and the large-mode tile-break policy. Audit the thrall detonation path for an accidental bypass.

## Testing strategy

1. Add tests before implementation for prototype visual-layer validity, relay-marker coverage and distance, Yautja defib rejection, item throw range/permission/gating, and self-destruct policy.
2. Implement only the targeted component, system, prototype, asset-reference, and map changes required by failing tests.
3. Run focused integration tests and static audits for the touched areas, then run the relevant broader test project if time permits.
4. Review the final diff by path so pre-existing user changes remain untouched.

## Non-goals

- No hard-coded Ctrl+Q binding.
- No wholesale Yautja weapon rebalance.
- No redesign of hunting-ground destination markers.
- No modification of unrelated existing worktree changes.
