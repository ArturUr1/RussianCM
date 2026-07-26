# Task 1 report — Yautja bracer actions tracker RED

Date: 2026-07-26

Scope completed:

- Modified the requested Task 1 integration test files:
  - `Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs`
  - `Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs`
- Modified the existing predator action-bar coverage requested by the full brief:
  - `Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs`
- Preserved the pre-existing user hunks in:
  - `YautjaSmokeTest.cs` butcher-selection change
  - `YautjaBowTest.cs` torso butcher row
- Did not modify production code.

Implemented RED-side test changes:

- Worn hunter bracer action-bar coverage now expects:
  - `CMUActionYautjaOpenBracerMenu` present
  - panel-backed actions absent from the worn item action list:
    - `CMUActionYautjaOpenMarkPanel`
    - `CMUActionYautjaSelfDestruct`
    - `CMUActionYautjaTranslator`
    - `CMUActionYautjaToggleBracerIdChip`
    - `CMUActionYautjaLinkThrallBracer`
    - `CMUActionYautjaTransmitThrallMessage`
    - `CMUActionYautjaCreateStabilisingCrystal`
    - `CMUActionYautjaCreateHumanStabilisingCrystal`
    - `CMUActionYautjaCreateHuntingTrap`
  - intentionally action-bar-driven entries still present:
    - `CMUActionYautjaCreateHealingCapsule`
    - `CMUActionYautjaTrackGear`
    - `CMUActionYautjaAddTrackedItem`
    - `CMUActionYautjaRemoveTrackedItem`
- Updated worn-vs-held expectations so:
  - worn bracer no longer expects `CMUActionYautjaToggleBracerIdChip`
  - held active bracer still expects `CMUActionYautjaToggleBracerIdChip`
  - worn bracer no longer expects `CMUActionYautjaLinkThrallBracer`
  - held active bracer still expects `CMUActionYautjaLinkThrallBracer`
- Updated worn injector expectation so it no longer expects standalone `CMUActionYautjaCreateStabilisingCrystal`.
- Updated worn thrall-message expectation so it no longer expects standalone `CMUActionYautjaTransmitThrallMessage`.
- Updated soldier-bracer action roster expectation to retain only:
  - `CMUActionYautjaToggleWristBlades`
  - `CMUActionYautjaCreateHealingCapsule`
- Extended predator innate action-bar coverage so:
  - `CMUActionYautjaLeap`, `CMUActionYautjaMarkForHunt`, `CMUActionYautjaButcher`, and `CMUActionYautjaAudioPanel` remain covered
  - `CMUActionYautjaOpenMarkPanel` is asserted absent as a standalone innate action
- Extended tracker coverage with an unlinked `CMUYautjaCombistick` on-map and asserted it does not contribute tracked gear output while linked ordinary gear still does.
- Extended add/remove tracked-item coverage to use `CMUYautjaCombistick`, proving explicit tracking can still be added/removed on Yautja tech and that the item stays in the active hand.

Focused RED command run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaBowTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest"
```

Observed result from the run:

- Exit code: `1`
- Runtime: about `198` seconds
- Console summary reported: `2 failed, 8 passed, 10 total`

Notes about the RED run:

- The run was intentionally stopped at the recorded RED state and not expanded into more broad execution.
- Console output also surfaced an unrelated pre-existing client/prototype issue during predator-role coverage:
  - `Unable to load RSI '/Textures/_CMU14/HunterShip/obj/items/hunter/pred_mask.rsi'.`
- Because the run output was extremely large and truncated in the harness, I recorded the exact command, exit code, duration, and summary counts as the authoritative RED evidence.

Commit/staging notes:

- Staged only the Task 1 test/report hunks with explicit partial staging.
- Left the pre-existing butcher and torso hunks unstaged.

Standalone mark-panel assertion was added to the predator-role action-bar coverage.

Fix-round note: the worn tracker action expectation was corrected to assert `CMUActionYautjaTrackGear` is absent while leaving the add/remove tracker assertions intact.
