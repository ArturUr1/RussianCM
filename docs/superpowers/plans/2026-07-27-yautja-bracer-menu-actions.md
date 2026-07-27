# Yautja Bracer Menu Actions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove seven bracer utility controls from the action bar and expose the same guarded behavior through the existing Yautja bracer menu.

**Architecture:** Keep the existing server-side utility and attachment methods as the single source of gameplay behavior. Add typed values to the shared bracer-panel command protocol, route them in `YautjaBracerMenuSystem`, and add client buttons in the code-built `YautjaBracerWindow`; action prototypes/events remain for compatibility but no longer receive action grants.

**Tech Stack:** C#/.NET, RobustToolbox ECS, BoundUserInterface messages, NUnit integration tests, YAML prototypes, Fluent localization.

## Global Constraints

- The seven migrated controls are `ChangeExplosionType`, `RemoveBracerAttachments`, `CreateHealingCapsule`, `AddTrackedItem`, `RemoveTrackedItem`, `ToggleBracerName`, and `ToggleBracerNotificationSound`.
- `ChangeExplosionType` must be removed from both held and worn bracer action grants; the other bracer controls must be removed from their current worn/gear-container action grants.
- Do not delete the action prototypes or shared action-event types solely because their grant path is removed.
- Preserve `OpenBracerMenu`, cloak, recall, disc, gear deployment actions, and unrelated Yautja behavior.
- Preserve explicit `YautjaTrackedItemComponent` tracker membership; do not reintroduce `YautjaTechItemComponent` fallback logic.
- Preserve all unrelated dirty-worktree changes. Stage only files belonging to the current task for each commit.
- Production behavior must be test-first: add a failing regression before changing the corresponding production path, and record the failure output.

---

### Task 1: Add RED coverage for action ownership

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs` for the `YautjaGearContainerComponent` action-grant case.

**Interfaces:**
- Consumes: existing action-list helpers and `GetItemActionsEvent` assertions.
- Produces: failing assertions proving that the seven requested action IDs are absent while unrelated bracer actions remain present.

- [ ] **Step 1: Identify the existing action roster fixtures and exact action IDs.**

  Inspect the current Yautja smoke, bow, and predator-role tests. Use these exact IDs in assertions:

  ```csharp
  var migratedActionIds = new[]
  {
      "CMUActionYautjaChangeExplosionType",
      "CMUActionYautjaRemoveBracerAttachments",
      "CMUActionYautjaCreateHealingCapsule",
      "CMUActionYautjaAddTrackedItem",
      "CMUActionYautjaRemoveTrackedItem",
      "CMUActionYautjaToggleBracerName",
      "CMUActionYautjaToggleBracerNotificationSound",
  };
  ```

- [ ] **Step 2: Add the failing worn-bracer assertions.**

  In the existing worn-bracer action roster assertion, require every migrated ID to be absent and retain positive assertions for `CMUActionYautjaOpenBracerMenu`, cloak, recall, disc, and the existing non-migrated bracer controls. The test must still assert `CMUActionYautjaAddTrackedItem` and `CMUActionYautjaRemoveTrackedItem` are absent even though their tracker behavior remains available through the menu.

- [ ] **Step 3: Add the failing held-bracer assertion.**

  In `YautjaSmokeTest.BracerIdChipActionIsPanelOnlyWhenWornButGrantedWhenHeldLikeCmss13Keybind` and `YautjaSmokeTest.BracerLinkThrallActionIsPanelOnlyWhenWornButGrantedWhenHeldLikeCmss13Keybind`, assert that `CMUActionYautjaChangeExplosionType` is absent from the held action list while preserving the existing positive assertions for held ID-chip and thrall-link actions.

- [ ] **Step 4: Add the failing gear-container assertion.**

  Exercise the existing `YautjaGearContainerComponent` action collection path and assert that `CMUActionYautjaRemoveBracerAttachments` is absent while installed gear deployment actions remain available.

- [ ] **Step 5: Run only the focused tests and capture the expected RED failure.**

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaBowTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest"
  ```

  Expected result: the new absence assertions fail because the current action providers still grant the migrated controls. If compilation fails because an existing dirty-worktree change is incomplete, record that exact error and do not modify unrelated files.

- [ ] **Step 6: Commit only the RED test changes.**

  ```powershell
  git add -- Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs
  git commit -m "test: cover bracer menu action ownership"
  ```

---

### Task 2: Move grants and route server menu commands

**Files:**
- Modify: `Content.Shared/_CMU14/Yautja/YautjaPowerSystem.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaAttachmentSystem.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaActions.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaBracerMenuSystem.cs`
- Test: the Task 1 Yautja integration tests.

**Interfaces:**
- Consumes: the failing action-roster tests from Task 1 and existing methods on `YautjaBracerUtilitySystem`/`YautjaAttachmentSystem`.
- Produces: a complete shared menu command protocol and server dispatch for all seven controls.

- [ ] **Step 1: Extend the shared command enum.**

  Add these values to `YautjaBracerPanelCommand` in `YautjaActions.cs`:

  ```csharp
  ChangeExplosionType,
  RemoveBracerAttachments,
  CreateHealingCapsule,
  AddTrackedItem,
  RemoveTrackedItem,
  ToggleBracerName,
  ToggleBracerNotificationSound,
  ```

- [ ] **Step 2: Remove the action grants only.**

  In `YautjaPowerSystem`, remove `ChangeExplosionType` from the held and worn `GetItemActionsEvent` branches and remove the other bracer utility grants from the worn branch. In `YautjaAttachmentSystem`, remove the `RemoveBracerAttachmentsAction` add from the gear-container action provider. Leave action prototypes, event subscriptions, handler methods, and unrelated actions intact.

- [ ] **Step 3: Route each command through existing guarded methods.**

  Extend `YautjaBracerMenuSystem.OnCommand` with these exact operations:

  ```csharp
  case YautjaBracerPanelCommand.ChangeExplosionType:
      _utility.TryChangeExplosionType(ent, args.Actor);
      break;
  case YautjaBracerPanelCommand.RemoveBracerAttachments:
      if (TryComp(ent.Owner, out YautjaGearContainerComponent? gearContainer))
          EntityManager.System<YautjaAttachmentSystem>().TryRemoveBracerAttachments((ent.Owner, gearContainer), args.Actor);
      break;
  case YautjaBracerPanelCommand.CreateHealingCapsule:
      _utility.TryCreateHealingCapsule(ent, args.Actor);
      break;
  case YautjaBracerPanelCommand.AddTrackedItem:
      _utility.TryAddTrackedItem(ent, args.Actor);
      break;
  case YautjaBracerPanelCommand.RemoveTrackedItem:
      _utility.TryRemoveTrackedItem(ent, args.Actor);
      break;
  case YautjaBracerPanelCommand.ToggleBracerName:
      _utility.TryToggleBracerName(ent, args.Actor);
      break;
  case YautjaBracerPanelCommand.ToggleBracerNotificationSound:
      _utility.TryToggleNotificationSound(ent, args.Actor);
      break;
  ```

  Keep `CanUseMenu` as the access gate and keep the existing `UpdateUi` call after the switch.

- [ ] **Step 4: Run the focused tests and verify GREEN.**

  ```powershell
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaBowTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest"
  ```

  Expected result: the Task 1 action-roster tests pass, with no new failures in the selected Yautja tests. Record counts and exit code.

- [ ] **Step 5: Commit the server/shared migration.**

  ```powershell
  git add -- Content.Shared/_CMU14/Yautja/YautjaPowerSystem.cs Content.Server/_CMU14/Yautja/YautjaAttachmentSystem.cs Content.Shared/_CMU14/Yautja/YautjaActions.cs Content.Server/_CMU14/Yautja/YautjaBracerMenuSystem.cs
  git commit -m "feat: route bracer utilities through menu"
  ```

---

### Task 3: Add client bracer menu controls and localization

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/YautjaBracerWindow.xaml.cs`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`
- Test: the client build plus existing Yautja integration tests that open/use the bracer menu.

**Interfaces:**
- Consumes: the seven `YautjaBracerPanelCommand` values and the existing `YautjaBracerPanelCommandMsg` transport.
- Produces: visible buttons that send only typed menu commands; no client-side gameplay logic.

- [ ] **Step 1: Add client button fields, construction, and bindings.**

  Use `YautjaBracerUiStyle.ActionButton` and the existing `Bind` helper. Add controls for:

  ```csharp
  ChangeExplosionType
  RemoveBracerAttachments
  CreateHealingCapsule
  AddTrackedItem
  RemoveTrackedItem
  ToggleBracerName
  ToggleBracerNotificationSound
  ```

  Place settings controls in the functions section, tracker controls in the tracker section, removal in the bracer/utility controls, and healing capsule in the fabricator section. Each button must bind to its matching enum value.

- [ ] **Step 2: Add English and Russian localization keys.**

  Add title/detail keys for all seven controls in both locale files. Follow the existing `cmu-yautja-bracer-menu-*` naming and preserve the existing localization style; do not hard-code user-facing strings in C#.

- [ ] **Step 3: Build the client and run the selected integration tests.**

  ```powershell
  dotnet build Content.Client/Content.Client.csproj --no-restore --nologo
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaSmokeTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaBowTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaPredatorRoleTest"
  ```

  Expected result: client compilation succeeds and focused Yautja tests remain green. Record output and exit codes.

- [ ] **Step 4: Commit the client menu slice.**

  ```powershell
  git add -- Content.Client/_CMU14/Yautja/YautjaBracerWindow.xaml.cs Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl
  git commit -m "feat: add bracer utility controls to menu"
  ```

---

### Task 4: Verify server/client build and bounded startup

**Files:**
- No production edits expected.
- Inspect: `git diff --check`, `git status --short`, task commits, and build/start logs.

**Interfaces:**
- Consumes: completed Tasks 1-3 and their test evidence.
- Produces: fresh evidence that client and server compile and start without startup exceptions, or an exact blocker report.

- [ ] **Step 1: Run server build and focused Yautja integration tests.**

  ```powershell
  dotnet build Content.Server/Content.Server.csproj --no-restore --nologo
  dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja"
  ```

- [ ] **Step 2: Start the server for a bounded smoke window.**

  Run this PowerShell sequence from the repository root, using only the process ID returned by this command:

  ```powershell
  $logRoot = Join-Path (Get-Location) '.superpowers/sdd/2026-07-27-yautja-bracer-menu-actions/runtime-logs'
  New-Item -ItemType Directory -Force $logRoot | Out-Null
  $server = Start-Process dotnet -ArgumentList 'run --project Content.Server/Content.Server.csproj --no-build' -WorkingDirectory (Get-Location) -RedirectStandardOutput (Join-Path $logRoot 'server.out.log') -RedirectStandardError (Join-Path $logRoot 'server.err.log') -PassThru -WindowStyle Hidden
  Start-Sleep -Seconds 15
  $server.Refresh()
  $server.HasExited
  Get-Content (Join-Path $logRoot 'server.out.log')
  Get-Content (Join-Path $logRoot 'server.err.log')
  if (!$server.HasExited) { Stop-Process -Id $server.Id -Force }
  ```

  A non-exited process after 15 seconds and logs without `Unhandled exception`, `Fatal`, or startup errors is a pass. Preserve the log path and exact result; if the process exits early, report its exit code and logs.

- [ ] **Step 3: Start the client for a bounded smoke window.**

  Run the same sequence with the client project and separate logs:

  ```powershell
  $client = Start-Process dotnet -ArgumentList 'run --project Content.Client/Content.Client.csproj --no-build' -WorkingDirectory (Get-Location) -RedirectStandardOutput (Join-Path $logRoot 'client.out.log') -RedirectStandardError (Join-Path $logRoot 'client.err.log') -PassThru -WindowStyle Hidden
  Start-Sleep -Seconds 15
  $client.Refresh()
  $client.HasExited
  Get-Content (Join-Path $logRoot 'client.out.log')
  Get-Content (Join-Path $logRoot 'client.err.log')
  if (!$client.HasExited) { Stop-Process -Id $client.Id -Force }
  ```

  A non-exited process after 15 seconds and logs without startup exceptions is a pass. If the desktop environment cannot initialize the client, report the exact graphics/display error rather than treating it as a code pass.

- [ ] **Step 4: Run final static checks.**

  ```powershell
  git diff --check
  git diff --cached --check
  rg -n "CMUActionYautja(ChangeExplosionType|RemoveBracerAttachments|CreateHealingCapsule|AddTrackedItem|RemoveTrackedItem|ToggleBracerName|ToggleBracerNotificationSound)" Content.Shared/_CMU14/Yautja Content.Server/_CMU14/Yautja
  git status --short
  ```

  Confirm the action IDs remain only as prototypes/handlers or compatibility fields, not as action grants, and confirm unrelated dirty files remain untouched.

- [ ] **Step 5: Commit no files in this task unless a narrowly scoped verification fix is required.**
