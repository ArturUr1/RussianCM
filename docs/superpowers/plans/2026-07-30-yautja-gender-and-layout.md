# Yautja Gender Personalization and Responsive Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add selectable male/female Yautja gender to lobby personalization, preserve it through profile transport and spawn, and make the editor usable when the available width is smaller than its current fixed horizontal layout.

**Architecture:** `YautjaCharacterProfile` remains the source of truth. Its public mutators normalize the selected variant into synchronized `Sex` and `Gender` values. The lobby editor exposes exactly Male/Female, previews the selected values, and writes them into the profile. Server profile application copies the values into the humanoid profile, where nested equality also compares them. The editor’s fixed preview/workspace row reflows vertically below a narrow-width threshold; category pages retain responsive grids and horizontal scrolling as a last-resort fallback.

**Tech Stack:** C#, RobustToolbox UI controls, YAML localization/prototypes, NUnit, `dotnet test`.

## Global Constraints

- Preserve all unrelated pre-existing working-tree changes; stage only files changed for this feature.
- Keep the existing default and backward-compatible value as male.
- Expose only Male and Female in the Yautja selector; do not add gender-based equipment restrictions.
- Do not add a database migration: existing serialized profiles without the new value must deserialize to the male default.
- Use the existing `Sex` and `Gender` enums and existing female Yautja species/body/voice resources.
- Write each test before the corresponding production change and run the focused test red, then green.

---

## 1. Lock down profile gender semantics with failing tests

**Files:**

- Modify `Content.IntegrationTests/_CMU14/Yautja/YautjaCharacterProfileTest.cs`.
- Modify `Content.Tests/Shared/Preferences/HumanoidCharacterProfileTest.cs`.

- [ ] Replace the stale male-only test with assertions that the default is male and that selecting female through either public mutator results in `Sex.Female` and `Gender.Female`.
- [ ] Add assertions that cloning/sanitizing a female Yautja profile preserves both values.
- [ ] Add a `HumanoidCharacterProfile.MemberwiseEquals` regression test proving two otherwise equal profiles with different nested Yautja sex/gender are unequal, while a cloned female profile remains equal.
- [ ] Run the two focused test filters and confirm the new tests fail against the current hardcoded implementation.

## 2. Implement normalized profile storage and equality

**Files:**

- Modify `Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs`.
- Modify `Content.Shared/Preferences/HumanoidCharacterProfile.cs`.

- [ ] Make the Yautja copy constructor copy the source `Sex` and `Gender` instead of resetting them.
- [ ] Make `WithSex` and `WithGender` normalize the selected value to the supported male/female pair, with unsupported values falling back to male.
- [ ] Ensure both mutators synchronize both fields so no profile can represent a mismatched Yautja sex/gender pair through the public API.
- [ ] Include nested Yautja `Sex` and `Gender` in `HumanoidCharacterProfile.MemberwiseEquals`.
- [ ] Re-run the focused profile/equality tests and confirm they pass.

## 3. Add the lobby gender selector and female preview behavior

**Files:**

- Modify `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`.
- Modify `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditorLayout.cs` if a small pure layout/helper API is needed by tests.
- Modify `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`.
- Modify `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl` if the English key set is maintained separately.

- [ ] Add one gender row beside the existing identity fields with exactly Male and Female options.
- [ ] Initialize the selector from the stored profile and update the profile through `WithSex`/`WithGender` when the user chooses an option.
- [ ] Remove preview hardcoding and pass the profile’s synchronized `Sex` and `Gender` into the preview humanoid profile.
- [ ] Keep all existing appearance, hair/quill, mask, armor, and equipment options available for either gender.
- [ ] Add localized label and option strings, reusing existing localization conventions.
- [ ] Verify that switching to Female updates the preview species body/voice-related profile inputs without resetting unrelated selections.

## 4. Fix narrow lobby layout and add regression coverage

**Files:**

- Modify `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditorLayout.cs`.
- Modify `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`.
- Modify `Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs`.

- [ ] Add a pure helper/constant describing when the current horizontal work area cannot fit its fixed preview, navigation, and control minimums.
- [ ] Add tests for wide mode and narrow mode, including the boundary and a very small width.
- [ ] Store the work-area/category workspace controls as needed and switch the work area to vertical orientation below the threshold, placing the preview above the category workspace.
- [ ] Make the category workspace use the available width in stacked mode and continue using the existing responsive selector-grid column calculation.
- [ ] Re-enable horizontal scrolling inside the category workspace as the fallback for controls that still have unavoidable minimum widths; do not clip overflow silently.
- [ ] Preserve the existing tooltips for labels whose text is shortened by selector-card width.
- [ ] Run the layout test filter and verify all existing responsive-column tests remain green.

## 5. Apply gender during server spawn and verify end-to-end transport

**Files:**

- Modify `Content.Server/_CMU14/Yautja/YautjaProfileApplySystem.cs`.
- Modify or extend `Content.IntegrationTests/_CMU14/Yautja/YautjaOriginalSpawnLoadoutTest.cs` and/or the focused character/profile integration test file, depending on the smallest existing fixture that can inspect the spawned humanoid profile.

- [ ] Replace server-side male hardcoding with the selected Yautja `Sex` and `Gender` values.
- [ ] Add an integration assertion that a female Yautja profile reaches the spawned humanoid as female for both fields.
- [ ] Confirm existing default male spawn behavior remains unchanged.
- [ ] Confirm the female species resources resolve through the existing species/body and vocal mappings.

## 6. Verification and handoff

- [ ] Run focused tests for Yautja profile semantics, humanoid profile equality, editor layout, and spawn behavior.
- [ ] Run the relevant client/server/integration project test suites without rebuilding unrelated projects where possible.
- [ ] Run `dotnet build`/`dotnet test` for affected projects if focused tests pass, and record any environment-only limitations.
- [ ] Run `git diff --check` and inspect the final diff, ensuring unrelated dirty files are not staged.
- [ ] Update the implementation plan checkboxes as each task completes and report the exact tests run.

