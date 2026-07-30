# Yautja Localization Completeness Design

## Goal

Provide complete English and Russian localization for all Yautja-related content, including Yautja, Predalien, and Hellhound runtime messages, YAML entity names and descriptions, profile customization UI, and dynamically selected messages.

## Scope

- Add explicit `en-US` and `ru-RU` entries for every Yautja-related YAML entity `name` and `description` that can be shown to a player.
- Add missing runtime localization keys used by Yautja production code, including keys selected through lookup tables or conditional branches.
- Keep the current English meaning as the English source text and provide natural Russian translations for the corresponding Russian entries.
- Align Fluent placeholders with the variables supplied by the calling code.
- Replace hardcoded English text in Yautja client UI and profile customization labels with localization lookups.
- Do not alter unrelated worktree changes or change gameplay behavior.

## Architecture

The existing Fluent files remain the runtime source of truth. Generated entity messages are grouped in the existing Yautja locale files, with a dedicated entity section/file used when that is clearer than appending to the large general file. YAML prototype values remain intact as fallback/source text; explicit locale entries override them for both supported languages.

A read-only audit tool under `Tools/_CMU14` will parse the relevant YAML and source files, collect expected keys, inspect Fluent messages and attributes, compare placeholders, and fail with actionable file/key output when parity is incomplete. The audit will be runnable independently of the game client and will be used as the regression test for future Yautja additions.

## Data Flow

1. YAML prototypes define entity identifiers and source `name`/`description` values.
2. The audit derives the corresponding `ent-<prototype-id>` and `.desc` keys.
3. EN and RU Fluent files provide explicit values for those keys.
4. Client/server code resolves runtime messages through `Loc.GetString`; dynamic key selectors are included in the audit's expected-key set.
5. The audit verifies key presence and placeholder parity before build verification.

## Error Handling

- Missing EN or RU keys are reported with the source file and line when available.
- Missing or extra placeholders are reported per key and language.
- YAML parse failures are fatal so the audit cannot silently skip prototypes.
- Hardcoded user-facing English strings in the Yautja client surface are reported for manual review or converted to keys.
- The audit does not rewrite source files; translations are reviewed and committed as normal project changes.

## Verification

- Run the localization audit and require zero missing EN/RU runtime keys, zero missing explicit YAML entity entries, zero placeholder mismatches, and zero known hardcoded Yautja UI strings.
- Run focused unit/integration tests covering the changed profile/UI localization paths.
- Run the relevant Content build/test commands and inspect the final diff for accidental changes outside the Yautja localization scope.

## Acceptance Criteria

- Every Yautja-related player-visible name, description, status, prompt, dialog, action, HUD, admin, lobby, and profile customization string has an English and Russian variant.
- No runtime Yautja localization key is missing in either language.
- Every shared key uses the same placeholder names in EN and RU and those names match the calling code.
- The localization audit passes and prevents reintroducing the audited gaps.
- No unrelated existing worktree changes are overwritten.
