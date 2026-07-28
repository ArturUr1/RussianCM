# Yautja CMSS13 Resistance Parity Design

## Goal

Bring the current Yautja innate resistances and resistance-related behaviors into 1:1 parity with the checked-in CMSS13 reference snapshot, without changing unrelated Yautja personalization, equipment, or combat behavior.

## Scope

Included:

- CMSS13 `brute_mod = 0.28` mapped to current physical damage types;
- CMSS13 `burn_mod = 0.65` mapped to Heat damage;
- full poison/toxin immunity;
- neurotoxin immunity;
- no stamina system for Yautja;
- no shrapnel embedding;
- CMSS13 stun and knockdown duration reduction of `1.5`;
- 70% acid-blood splash dodge chance;
- the original Yautja-specific baton, bonebreak, fire-stack, anesthesia/N2O, and xeno-smoke behaviors where current systems expose equivalent events;
- regression tests for every ported behavior and for removal of non-original blanket resistances.

Excluded:

- Yautja armor and equipment protection values, which are separate from species resistances;
- existing Yautja movement, weapons, skills, profile, rank, clan, and pull/infection behavior;
- unrelated dirty-worktree changes.

## Design

The `CMUYautja` damage modifier set will contain only direct species damage mappings supported by the original species: `Blunt`, `Slash`, and `Piercing` use `0.28`, `Heat` uses `0.65`, and `Poison` uses `0`. Current generalized modifiers for Shock, Cold, Caustic, Radiation, Bloodloss, and Asphyxiation will be removed rather than treated as original parity.

Yautja-only protections that cannot be represented by a damage coefficient will be implemented in focused shared/server systems using existing events. The implementation will prefer existing event cancellation or component-removal hooks over duplicating damage pipelines. Stamina immunity will remove/disable the inherited stamina components at Yautja intrinsic-stat initialization, so all stamina sources behave like CMSS13's `/datum/stamina/none`, not only tasers. Status resistance will remain a duration modifier, but will apply only to KnockedDown and Stun with factor `1.5`; Unconscious will retain the normal duration.

The existing `YautjaHeatResistance` component will either be wired into the damage event or removed if the direct `Heat: 0.65` prototype is sufficient; there will be exactly one effective fire-damage multiplier. The resulting behavior must not multiply the original coefficient twice.

## Validation

- Add focused failing tests before production changes.
- Verify each test fails for the missing or incorrect behavior, then implement the smallest change that makes it pass.
- Run focused Yautja tests, relevant shared/integration test projects, and `git diff --check`.
- Confirm the final diff contains only resistance-parity changes and the design/plan artifacts created for this task.
