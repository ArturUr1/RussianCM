# Task 2 report — Yautja bracer panel action cleanup

Date: 2026-07-26

Scope completed:

- Modified `Content.Shared/_CMU14/Yautja/YautjaPowerSystem.cs`.
- Modified `Content.Server/_CMU14/Yautja/YautjaAbilitySystem.cs`.
- Modified `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/mcaste_items.yml`.
- Did not modify Task 1 tests.

Implemented production-side changes:

- In `YautjaPowerSystem.OnGetItemActions`, removed only the worn-bracer standalone grants for panel-backed commands:
  - `CMUActionYautjaSelfDestruct`
  - `CMUActionYautjaTranslator`
  - `CMUActionYautjaToggleBracerIdChip`
  - `CMUActionYautjaLinkThrallBracer`
  - `CMUActionYautjaTransmitThrallMessage`
  - `CMUActionYautjaTrackGear`
  - `CMUActionYautjaCreateStabilisingCrystal`
- Kept the held active-bracer branch unchanged.
- Kept the worn `OpenBracerMenu`, cloak, recall, and disc grants unchanged.
- Kept the worn independent actions unchanged:
  - `CMUActionYautjaChangeExplosionType`
  - `CMUActionYautjaToggleNotificationSound`
  - `CMUActionYautjaToggleBracerName`
  - `CMUActionYautjaAddTrackedItem`
  - `CMUActionYautjaRemoveTrackedItem`
  - `CMUActionYautjaCreateHealingCapsule`
- Left the existing `AddAction` + `ActionWhitelist` flow intact.

- In `YautjaAbilitySystem`:
  - removed the standalone `YautjaOpenMarkPanelActionEvent` subscription;
  - removed standalone `OpenMarkPanelAction` grant/removal from `GrantActions` and `RemoveActions`;
  - removed the local standalone handler that opened the mark panel through the innate action.
- Preserved the shared bracer-side compatibility fields and prototypes in shared Yautja components/actions.
- Preserved unrelated existing user hunks in `Content.Server/_CMU14/Yautja/YautjaAbilitySystem.cs` and prepared staging to include only the Task 2 hunks from that file.

- In soldier bracer whitelist data, removed only stale panel-backed entries:
  - `CMUActionYautjaCreateStabilisingCrystal`
  - `CMUActionYautjaSelfDestruct`
  - `CMUActionYautjaTranslator`
- Retained `CMUActionYautjaCreateHealingCapsule`.

Verification attempts:

- Focused RED/green verification command attempted:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaBowTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest"
```

- Observed result:
  - exit code: `1`
  - failure mode: build/compile never reached the relevant Yautja assertions because the machine ran out of disk space during compilation
  - representative errors:
    - `CS8104 ... IOException: Недостаточно места на диске`
    - `MSB3883 ... Недостаточно места на диске`
- Additional environment check:
  - `Get-PSDrive -Name C | Select-Object Used,Free` reported `Free = 0`

Notes:

- Because Task 3 tracker behavior is intentionally not implemented yet, no claim is made that the full Yautja suite passes.
- The main blocker to deeper verification in this task was disk capacity, not a known Yautja-specific compile error from the edited files.
