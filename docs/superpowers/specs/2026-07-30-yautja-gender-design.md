# Yautja male/female personalization

## Context

The Yautja lobby personalization currently exposes no sex selector and several paths force Yautja profiles to `Male`. The project already has separate `Sex` and `Gender` values on `YautjaCharacterProfile`, female Yautja body sprites, and a female Yautja vocal sound prototype, but the profile clone, lobby preview, and server application do not preserve or apply those values.

The original CMSS13 implementation uses one `predator_gender` preference with two values, `MALE` and `FEMALE`. The preference is shown in the Yautja picker, persisted in the preference savefile, and assigned to the spawned mob during `load_name`. The relevant reference files are:

- `cmss13-ref-full/code/modules/client/pred_picker.dm`
- `cmss13-ref-full/code/modules/client/preferences.dm`
- `cmss13-ref-full/code/modules/client/preferences_savefile.dm`
- `cmss13-ref-full/code/modules/gear_presets/yautja.dm`

## Goal

Allow a player to choose Male or Female for a Yautja in the lobby personalization tab and carry that choice through profile persistence, preview, and round-start spawning.

For Female, the applied humanoid must use the female Yautja base sprites, female Yautja vocal collection, and female grammar/pronouns. Hair, dreadlock styles, colors, and equipment remain available exactly as they are for Male.

## Non-goals

- Do not add Epicene, Neuter, or any third gender option to the Yautja selector.
- Do not add gender-specific restrictions to Yautja hair, equipment, armor, masks, or other appearance selectors.
- Do not change database schema or introduce a separate Yautja preference storage mechanism.
- Do not alter the general humanoid profile editor's existing sex/gender controls.

## Design

### Lobby UI

Add a `Пол`/gender row to the existing left-side Yautja profile summary controls, next to name and age. The control offers exactly two mutually exclusive options: Male and Female. It is initialized from the nested `YautjaCharacterProfile` and uses the existing `Mutate`/`OnProfileChanged` flow.

Selecting an option updates the nested profile, immediately rebuilds the Yautja preview, and marks the parent profile dirty through the existing lobby editor callback. Other selectors and equipment choices are not rebuilt or reset.

### Profile model

`YautjaCharacterProfile` remains the source of truth for the Yautja-specific choice. Its existing `Sex` and `Gender` fields are retained for compatibility with the humanoid appearance and grammar systems.

The copy constructor must preserve both fields while sanitizing invalid values to Male. `WithGender` and `WithSex` must keep the pair synchronized for the supported choices:

- Male maps to `Sex.Male` and `Gender.Male`.
- Female maps to `Sex.Female` and `Gender.Female`.

This prevents a profile clone or a caller using either mutator from producing mismatched body, voice, and grammar settings. The default and invalid-value fallback remain Male, matching CMSS13's default `predator_gender`.

The nested sex/gender values must participate in `HumanoidCharacterProfile.MemberwiseEquals`, so changing only the Yautja sex is recognized as a profile change.

### Preview and spawn data flow

The lobby preview profile and server-side `YautjaProfileApplySystem` will copy both nested values into the temporary/authoritative `HumanoidCharacterProfile` instead of hardcoding Male. The normal humanoid appearance pipeline then applies:

```text
YautjaProfile.Sex    -> HumanoidAppearanceComponent.Sex    -> sex-specific body sprites and vocal sounds
YautjaProfile.Gender -> HumanoidAppearanceComponent.Gender -> grammar and pronouns
```

The server will continue to sanitize the profile through the existing capability path before applying it. Since sex/gender are not capability-gated, sanitization only validates them and leaves a valid Female choice intact.

No database migration is required. Existing serialized profiles deserialize with the model defaults, and the profile copy/sanitization path supplies the Male fallback for missing or invalid values.

## Error handling and compatibility

- Invalid enum values received from old or untrusted profile data resolve to Male during profile copying.
- A missing field in an older serialized profile resolves to the field initializer, Male.
- Unsupported gender values cannot be selected in the UI.
- If a Yautja entity is created without a profile, the existing default profile remains Male.
- Preview and spawn must use the same mapping so the lobby never shows a different sex from the round-start character.

## Verification

Add or update automated coverage for:

1. Default Yautja profiles are Male.
2. Selecting Female preserves Female through `WithGender`, `WithSex`, cloning, and capability sanitization.
3. Male and Female profiles are distinguished by `MemberwiseEquals`.
4. Applying a Female Yautja profile produces Female sex and gender on the humanoid appearance component.
5. Existing appearance, equipment, and capability behavior remains unchanged.

Manual acceptance in the lobby:

1. Open Yautja personalization.
2. Select Female.
3. Confirm the preview changes to the female body and remains equipped with the selected gear.
4. Reopen or reload the personalization and confirm Female remains selected.
5. Spawn as Yautja and confirm female body, vocal sounds, and pronouns/grammar.
6. Switch back to Male and confirm the male body and vocal/grammar behavior return.

## Expected implementation surface

- `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`
- `Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs`
- `Content.Server/_CMU14/Yautja/YautjaProfileApplySystem.cs`
- `Content.Shared/Preferences/HumanoidCharacterProfile.cs`
- Existing Yautja profile tests, plus focused regression coverage where needed
- Yautja lobby localization entries for the label and two options
