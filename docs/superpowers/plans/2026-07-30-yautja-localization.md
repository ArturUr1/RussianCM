# Yautja Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with verification checkpoints.

**Goal:** Complete English and Russian localization for all Yautja-related runtime messages, YAML entity names/descriptions, profile UI strings, placeholders, and dynamic localization keys.

**Architecture:** Keep Fluent as the runtime source of truth. Add explicit EN/RU entries for generated `ent-*` prototype messages and missing runtime keys, replace hardcoded Yautja UI strings with `Loc.GetString`, and add a read-only Python audit that derives expected keys from YAML and source code and verifies both locales.

**Tech Stack:** Fluent `.ftl`, C# client/shared code, XAML, YAML prototypes, Python 3, PyYAML, `unittest`, existing Space Station 14 build/test tooling.

## Global Constraints

- Preserve all unrelated pre-existing worktree changes.
- Do not change gameplay behavior; only localize text and add audit coverage.
- Keep English source meaning unchanged and use natural Russian translations.
- Every shared Fluent placeholder must use the same name in EN and RU and match the calling code.
- The audit must fail on missing keys, YAML parse failures, placeholder mismatches, or known hardcoded Yautja UI strings.

---

### Task 1: Add a failing localization parity test

**Files:**
- Create: `Tools/_CMU14/YautjaLocalization/__init__.py`
- Create: `Tools/_CMU14/YautjaLocalization/test_localization.py`
- Test target: existing `Resources/Locale/en-US/_CMU14/yautja`, `Resources/Locale/ru-RU/_CMU14/yautja`, `Content.Client/_CMU14/Yautja`, `Content.Server/_CMU14/Yautja`, `Content.Shared/_CMU14/Yautja`, and `Resources/Prototypes/_CMU14/Threats/Yautja`

**Interfaces:**
- Produces a `unittest` entry point that imports `audit` from the same package and asserts the repository audit has no errors.

- [ ] **Step 1: Write the failing test**

Create `test_localization.py` with a test that calls `audit_repository(Path(__file__).parents[3])`, asserts `result.errors == []`, and prints each error in the assertion message. The test must cover the real repository so the current known missing Russian keys make it fail.

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
python -m unittest Tools._CMU14.YautjaLocalization.test_localization -v
```

Expected: import or assertion failure because `audit.py` does not yet exist or because the current repository has missing locale keys.

- [ ] **Step 3: Commit the failing test**

```powershell
git add -- Tools/_CMU14/YautjaLocalization/__init__.py Tools/_CMU14/YautjaLocalization/test_localization.py
git commit -m "test: add yautja localization parity gate"
```

### Task 2: Implement the read-only localization audit

**Files:**
- Create: `Tools/_CMU14/YautjaLocalization/audit.py`
- Modify: `Tools/_CMU14/YautjaLocalization/test_localization.py`

**Interfaces:**
- `audit_repository(root: pathlib.Path) -> AuditResult`
- `AuditResult.errors: list[str]`
- `AuditResult.expected_keys: set[str]`

- [ ] **Step 1: Add focused parser tests**

Add `unittest` cases for Fluent messages with attributes, placeholder extraction from `{$name}` and `{$name -> ...}`, YAML entity key derivation as `ent-<id>` and `ent-<id>.desc`, and literal `Loc.GetString("...")` plus static key maps in the relevant Yautja C# files.

- [ ] **Step 2: Run focused parser tests to verify they fail**

Run:

```powershell
python -m unittest Tools._CMU14.YautjaLocalization.test_localization -v
```

Expected: failures because the parser and audit functions are not implemented.

- [ ] **Step 3: Implement the minimal audit**

Implement deterministic parsing with PyYAML `BaseLoader` so custom YAML tags do not prevent scanning. Collect:

1. explicit `ent-*` and `.desc` messages from both Yautja locale trees, including Fluent attributes;
2. entity `name` and `description` keys from every Yautja prototype YAML file;
3. literal runtime keys from `Loc.GetString` and equivalent localization calls in Yautja production files;
4. statically selected keys from the Yautja hunt console, profile editor, and mark systems;
5. the known hardcoded UI patterns in the Yautja XAML/profile files;
6. placeholder sets for every shared EN/RU key.

Return structured errors with the source path and key. Do not modify any file.

- [ ] **Step 4: Run the audit test and confirm it reports the known gaps**

Run the same unittest command and verify it fails with actionable missing-key, placeholder, and hardcoded-string messages rather than parser errors.

- [ ] **Step 5: Commit the audit implementation**

```powershell
git add -- Tools/_CMU14/YautjaLocalization
git commit -m "test: audit yautja localization parity"
```

### Task 3: Close runtime EN/RU key gaps and placeholder mismatches

**Files:**
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/admin.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/admin.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl`

**Interfaces:**
- Provides every literal and statically selected runtime key required by the Yautja production code in both languages.

- [ ] **Step 1: Add the failing runtime key assertions**

Extend the audit test with explicit assertions for `cmu-yautja-hivebreaker-requires-recent-death`, the 14 dynamic hunt/profile/mark keys, and representative missing-RU groups such as self-destruct, hunt console, bracer, relay, and ceremonial dagger.

- [ ] **Step 2: Run the assertions and verify they fail**

Run the focused unittest command and confirm the failure identifies the missing keys in the current locale files.

- [ ] **Step 3: Add minimal EN/RU Fluent messages**

Add matching English and Russian messages, preserving existing key names and using the calling code's argument names. Correct the 14 shared placeholder mismatches, including `cleanser-held`, `tech-shock`, thrall broadcasts/messages, trap messages, and self-destruct messages.

- [ ] **Step 4: Run the focused audit and verify runtime parity passes**

Run:

```powershell
python -m unittest Tools._CMU14.YautjaLocalization.test_localization -v
```

Expected: runtime-key and placeholder assertions pass; entity and hardcoded UI checks remain red until later tasks.

- [ ] **Step 5: Commit the runtime localization changes**

```powershell
git add -- Resources/Locale/en-US/_CMU14/yautja Resources/Locale/ru-RU/_CMU14/yautja
git commit -m "feat: complete yautja runtime localization"
```

### Task 4: Add complete EN/RU YAML entity localization

**Files:**
- Create or modify: `Resources/Locale/en-US/_CMU14/yautja/entities.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/entities.ftl`
- Read-only inputs: `Resources/Prototypes/_CMU14/Threats/Yautja/**/*.yml`, `Resources/Prototypes/_CMU14/Yautja/**/*.yml`, and `Resources/Prototypes/_CMU14/Roles/Shared/Skills/Yautja.yml`

**Interfaces:**
- Provides explicit `ent-<prototype-id>` and `.desc` entries for all 1,522 extracted Yautja-related entity name/description values.

- [ ] **Step 1: Add a failing complete-entity assertion**

Add a test that compares the full derived YAML key set with both locale key sets and asserts that no generated entity name or description key is missing.

- [ ] **Step 2: Run the test to verify it fails**

Run the focused unittest command and confirm it reports the current missing EN/RU generated entity keys.

- [ ] **Step 3: Build the English entity catalog**

Create `en-US/_CMU14/yautja/entities.ftl` with the current YAML English values, resolving values that already name another Fluent key as Fluent references rather than literal key text. Preserve punctuation, capitalization, and technical identifiers.

- [ ] **Step 4: Translate and add the Russian entity catalog**

Add natural Russian translations for every generated entity name and description, including decorative structures, equipment, masks, weapons, traps, mobs, actions, roles, and hunter-ship entities. Keep proper nouns, IDs, units, and technical tokens unchanged.

- [ ] **Step 5: Run the complete entity parity test**

Run the focused unittest command and verify both generated locale catalogs are complete and parse without errors.

- [ ] **Step 6: Commit the entity catalogs**

```powershell
git add -- Resources/Locale/en-US/_CMU14/yautja/entities.ftl Resources/Locale/ru-RU/_CMU14/yautja/entities.ftl
git commit -m "feat: localize yautja entity names and descriptions"
```

### Task 5: Localize Yautja client UI and profile display names

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/YautjaBadBloodWeaponChoiceWindow.xaml`
- Modify: `Content.Client/_CMU14/Yautja/YautjaBadBloodWeaponChoiceWindow.xaml.cs`
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`

**Interfaces:**
- Profile display-name helpers accept the current localization service or return localized strings at the client presentation boundary without changing serialized profile data.

- [ ] **Step 1: Add failing UI localization tests**

Extend the audit test to assert the XAML title/warning, filter tooltip/label, `ALL` option, section labels, and every display-name string returned by `YautjaCharacterProfile` are Fluent-backed rather than hardcoded English.

- [ ] **Step 2: Run the UI assertions to verify they fail**

Run the focused unittest command and confirm it identifies the known XAML, filter, and profile display-name strings.

- [ ] **Step 3: Replace hardcoded strings with localization keys**

Use `Loc.GetString` in the code-behind for dynamic labels and add Fluent keys for weapon-choice title/warning, material filters, section labels, and profile customization names. Keep profile serialization and enum values unchanged.

- [ ] **Step 4: Run focused client tests**

Run the existing profile layout tests and the localization audit. Confirm the UI assertions pass and the profile behavior tests still pass.

- [ ] **Step 5: Commit the UI localization changes**

```powershell
git add -- Content.Client/_CMU14/Yautja Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs Resources/Locale/en-US/_CMU14/yautja Resources/Locale/ru-RU/_CMU14/yautja
git commit -m "feat: localize yautja profile and client UI"
```

### Task 6: Run full verification and review the isolated diff

**Files:**
- Modify only files already listed in Tasks 2-5 if verification discovers a defect.

- [ ] **Step 1: Run the complete localization audit**

```powershell
python -m unittest Tools._CMU14.YautjaLocalization.test_localization -v
```

Expected: all audit tests pass with zero missing keys, zero placeholder mismatches, zero YAML parse errors, and zero hardcoded Yautja UI findings.

- [ ] **Step 2: Run focused C# tests**

```powershell
dotnet test Content.Tests/Content.Tests.csproj --filter "FullyQualifiedName~YautjaProfileEditorLayoutTest|FullyQualifiedName~YautjaCharacterProfile"
```

Expected: exit code 0 with no failing tests attributable to localization changes.

- [ ] **Step 3: Build the affected projects**

```powershell
dotnet build Content.Client/Content.Client.csproj --no-restore
dotnet build Content.Shared/Content.Shared.csproj --no-restore
```

Expected: both commands exit 0.

- [ ] **Step 4: Inspect only the task diff**

```powershell
git diff HEAD~5 --stat
git diff HEAD~5 --check
git status --short
```

Confirm the diff contains only the localization spec/plan, audit tool/tests, Yautja locale files, and intended Yautja UI/profile changes; preserve all unrelated pre-existing modifications.

- [ ] **Step 5: Record the verification result**

Report the exact audit, test, and build results and any remaining limitations instead of claiming completion without fresh command output.
