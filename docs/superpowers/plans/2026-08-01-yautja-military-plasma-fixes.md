# Yautja Military Plasma and HUD Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the Yautja military plasma cannon, popup text, military HUD icons, and on-mob military gear sprites to the approved CMSS13-compatible behavior.

**Architecture:** Keep cannon ownership in `YautjaCannonPackSystem`; extend its linked-cannon event handling so both drop and throw paths share one internal-container return operation. Represent military caste explicitly with a networked shared component, select dedicated client status icons before ordinary rank icons, and keep object RSI assets separate from the imported four-direction worn RSI.

**Tech Stack:** C#/.NET, Robust ECS and integration tests, YAML prototypes/localization, RSI metadata and PNG sprite sheets, PowerShell, Pillow for extracting the source DMI.

## Global Constraints

- Preserve unrelated dirty worktree changes; only modify files listed in the task that owns the change.
- Do not change cannon balance values, projectile behavior, or the generic magnetic-item system.
- Keep examine rich markup (`<bold>`) intact; remove only literal `[bold]` popup markup.
- Use the original CMSS13 `mcaste_gear.dmi` and `hud_yautja.dmi` states without redrawing or recoloring.
- Add regression coverage before each corresponding production change and run the narrowest useful test command after each change.
- Stop and report if a scoped file has an unrelated overlapping edit that cannot be preserved safely.

---

### Task 1: Replace literal popup markup with plain text

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs: around YautjaBracerDrainFailureUsesValidBoldMarkup`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl: cmu-yautja-drain-power-failed and cmu-yautja-cannon-pack-drain-failed`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/runtime_extra.ftl: cmu-yautja-drain-power-failed and cmu-yautja-cannon-pack-drain-failed`

**Interfaces:**
- Consumes: `Loc.GetString` popup localization used by `YautjaPowerSystem` and `YautjaCannonPackSystem`.
- Produces: plain localized popup strings whose interpolated values contain no `[bold]` or `[/bold]` tokens.

- [ ] **Step 1: Write the failing test.**

Change `YautjaBracerDrainFailureUsesValidBoldMarkup` into a regression test that reads both locale files and asserts the localized drain templates contain `{$charge}/{$max}` and `{$amount}`, contain neither `[bold]` nor `[/bold]`, and do not introduce angle-bracket markup. Update the existing cannon-pack assertion in `CannonPackExamineAndLowPowerDrainUseCmss13SourceText` to expect:

```csharp
"Your pack lacks the energy. It only has 40/2000 remaining and needs 50."
```

Keep the examine assertion expecting `It currently has <bold>40/2000</bold> charge.` so the test distinguishes popup rendering from examine markup.

- [ ] **Step 2: Run the focused tests to verify failure.**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaBracerDrainFailureUsesValidBoldMarkup|FullyQualifiedName~CannonPackExamineAndLowPowerDrainUseCmss13SourceText"
```

Expected: failure because the current locale files and `Loc.GetString` result still contain literal `[bold]` tokens.

- [ ] **Step 3: Write the minimal implementation.**

Remove only the four markup wrappers from the English and Russian popup templates. Preserve all wording, interpolation names, and punctuation. Do not change any `<bold>` text in examine code or examine tests.

- [ ] **Step 4: Run the focused tests to verify the fix.**

Run the same `dotnet test` command. Expected: both tests pass.

- [ ] **Step 5: Commit the scoped change.**

```powershell
git add Content.IntegrationTests/_CMU14/Yautja/YautjaPredatorRoleTest.cs Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/runtime_extra.ftl
git commit -m "fix: render yautja power popups as plain text"
```

### Task 2: Return thrown dual cannons to the source pack

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs: after CannonPackRetractsDroppedInternalCannonsLikeCmss13`
- Modify: `Content.Server/_CMU14/Yautja/YautjaCannonPackSystem.cs: Initialize, linked-cannon handlers, and retract helper`

**Interfaces:**
- Consumes: `ThrownEvent(EntityUid? User, EntityUid Thrown)`, `ThrownItemSystem.StopThrow`, `YautjaCannonPackLinkedCannonComponent.Pack`, and the existing `RetractCannons` operation.
- Produces: `YautjaCannonPackSystem.OnLinkedCannonThrown`, which returns a valid linked cannon to its pack before the throw impulse can leave it on the map.

- [ ] **Step 1: Write the failing integration test.**

Add `CannonPackReturnsThrownInternalCannonsLikeCmss13` to `YautjaBowTest`. Spawn a test map, a `CMMobHuman`, a `CMUYautjaCannonPack`, and the pack action; equip the pack on the back, deploy it with the existing `RaiseUsePlasmaCannons` helper, then call `entMan.System<ThrowingSystem>().TryThrow(cannon, Vector2.UnitX, user: hunter, animated: false, playSound: false, doSpin: false)`. Assert after the call/tick that:

```csharp
Assert.That(packComp.CannonContainer!.Contains(cannon), Is.True);
Assert.That(packComp.CannonsDeployed, Is.False);
Assert.That(actionComp.Toggled, Is.False);
Assert.That(hands.IsHolding(hunter, cannon), Is.False);
Assert.That(entMan.HasComponent<ThrownItemComponent>(cannon), Is.False);
```

Use the same `try/finally` cleanup pattern as the adjacent cannon-pack tests.

- [ ] **Step 2: Run the new test to verify failure.**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~CannonPackReturnsThrownInternalCannonsLikeCmss13"
```

Expected: failure because the current `DroppedEvent` handler does not handle the subsequent `ThrownEvent`, leaving the cannon outside `CannonContainer`.

- [ ] **Step 3: Implement the minimal shared return path.**

In `YautjaCannonPackSystem`:

1. Add `using Content.Shared.Throwing;` and inject `ThrownItemSystem`.
2. Subscribe `YautjaCannonPackLinkedCannonComponent` to `ThrownEvent`.
3. Validate the linked pack with the same ownership checks already used by `OnLinkedCannonDropped`.
4. Refactor the existing drop logic so a shared helper inserts the cannon into `EnsureCannonContainer`, clears `CannonsDeployed`, and turns off `UseCannonsAction`.
5. For `ThrownEvent`, call `ThrownItemSystem.StopThrow` when a `ThrownItemComponent` is present, then use the shared return helper without duplicating the manual-drop popup.
6. Preserve the existing drop and unequip semantics, including the current user-visible deactivation popup and hand-drop guard.

The handler must leave invalid/unlinked cannons alone so ordinary throwing remains possible for non-pack-owned entities.

- [ ] **Step 4: Run the cannon regression set.**

Run the new test plus the existing deploy/drop/unequip/live-fire tests:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~CannonPackReturnsThrownInternalCannonsLikeCmss13|FullyQualifiedName~CannonPackDeploysInternalCannonsAndRetractsLikeCmss13|FullyQualifiedName~CannonPackRetractsDroppedInternalCannonsLikeCmss13|FullyQualifiedName~CannonPackUnequipRetractsDeployedInternalCannonsLikeCmss13|FullyQualifiedName~DualPlasmaCannonsLiveFireUsesSourcePackLanceLikeCmss13"
```

Expected: all selected tests pass.

- [ ] **Step 5: Commit the scoped change.**

```powershell
git add Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs Content.Server/_CMU14/Yautja/YautjaCannonPackSystem.cs
git commit -m "fix: return thrown yautja cannons to their pack"
```

### Task 3: Add CMSS13 military HUD icon selection

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaMilitaryCasteRoleTest.cs`
- Modify: `Content.Client/_CMU14/Yautja/YautjaHudSystem.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaComponents.cs`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Mobs/mobs.yml`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Interface/status_icons.yml`
- Modify: `Resources/Textures/_CMU14/Yautja/hud_yautja.rsi/meta.json`
- Create: `Resources/Textures/_CMU14/Yautja/hud_yautja.rsi/soldierhud.png`
- Create: `Resources/Textures/_CMU14/Yautja/hud_yautja.rsi/soldierhud_wl.png`
- Create: `Resources/Textures/_CMU14/Yautja/hud_yautja.rsi/enforcerhud.png`
- Create: `Resources/Textures/_CMU14/Yautja/hud_yautja.rsi/enforcerhud_wl.png`

**Interfaces:**
- Consumes: `YautjaMilitaryCasteComponent.Caste`, local HUD visibility rules, and CMSS13 `hud_yautja.dmi` states.
- Produces: `CMUYautjaMilitarySoldierIcon` and `CMUYautjaMilitaryEnforcerIcon` health-icon prototypes and client selection before clan-rank icons.

- [ ] **Step 1: Write the failing tests.**

Extend `YautjaMilitaryCasteRoleTest` with a client-connected test that resolves the local client entities for `CMUMobYautjaMilitaryCasteSoldier` and `CMUMobYautjaMilitaryCasteEnforcer`, attaches a `YautjaHudViewerComponent` to the local viewer when needed, raises `GetStatusIconsEvent`, and extracts `SpriteSpecifier.Rsi.RsiState`. Assert soldier returns `soldierhud`, enforcer returns `enforcerhud`, and a normal `CMUMobYautja` still returns `predhud` or its rank-specific state. Add a resource assertion that `hud_yautja.rsi` contains `soldierhud`, `soldierhud_wl`, `enforcerhud`, and `enforcerhud_wl`.

The test should also assert the military mob prototypes contain the explicit caste component with the expected enum value on the server side.

- [ ] **Step 2: Run the tests to verify failure.**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~MilitaryCasteHudIconsUseCmss13States"
```

Expected: failure because the military prototypes do not expose a caste component, the HUD states are absent from the RSI, and `YautjaHudSystem` only maps `ClanRank`.

- [ ] **Step 3: Implement the shared caste marker and prototypes.**

Add:

```csharp
public enum YautjaMilitaryCaste : byte
{
    Soldier,
    Enforcer,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class YautjaMilitaryCasteComponent : Component
{
    [DataField, AutoNetworkedField]
    public YautjaMilitaryCaste Caste = YautjaMilitaryCaste.Soldier;
}
```

Add `YautjaMilitaryCaste` to the soldier and enforcer mob prototypes with the corresponding `Caste` value. Add health icon prototypes pointing to the `hud_yautja.rsi` states. In `YautjaHudSystem`, cache the two icon prototypes and handle `YautjaMilitaryCasteComponent` in the same `GetStatusIconsEvent` path as Yautja rank icons, returning the military icon first and retaining current visibility checks.

- [ ] **Step 4: Import the four HUD pixels and update RSI metadata.**

Use the local CMSS13 reference `cmss13-ref-full/icons/mob/hud/hud_yautja.dmi` to extract the four one-direction states into four 32x32 PNGs. Add each state to `meta.json` with `directions: 1`, preserving the existing license/copyright metadata.

- [ ] **Step 5: Run the HUD tests.**

Run the military test and the existing rank HUD tests:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~MilitaryCasteHudIconsUseCmss13States|FullyQualifiedName~YautjaRankHudIconsAreVisibleInGameAndUseCmss13States|FullyQualifiedName~YautjaRankHudIconsAreVisibleAfterShipSpawn"
```

Expected: all selected tests pass, with military icons replacing rank icons only for military caste mobs.

- [ ] **Step 6: Commit the scoped change.**

```powershell
git add Content.IntegrationTests/_CMU14/Yautja/YautjaMilitaryCasteRoleTest.cs Content.Client/_CMU14/Yautja/YautjaHudSystem.cs Content.Shared/_CMU14/Yautja/YautjaComponents.cs Resources/Prototypes/_CMU14/Threats/Yautja/Mobs/mobs.yml Resources/Prototypes/_CMU14/Threats/Yautja/Interface/status_icons.yml Resources/Textures/_CMU14/Yautja/hud_yautja.rsi
git commit -m "fix: show military yautja HUD icons"
```

### Task 4: Import and wire the original on-mob military gear RSI

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs: existing static military-cannon prototype test or a new adjacent asset test`
- Modify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/mcaste_items.yml: Clothing sprite fields for powered armor, greaves, powered helmet, and cannon pack`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/meta.json`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/ARMOR.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/fullarmor_soldier.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/fullarmor_soldier_lead.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/SHOES.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/y-boots_powered.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/HELMET.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/helmet_powered.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/BACK.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/cannonpack.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/SHOULDER.png`
- Create: `Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi/plasma_cannons.png`

**Interfaces:**
- Consumes: original CMSS13 DMI state order and Robust RSI directional sheet format.
- Produces: a 32x32 RSI whose four-direction states are 128x32 horizontal sheets and whose one-direction states are 32x32 images; relevant Clothing prototypes reference it.

- [ ] **Step 1: Write the failing asset/prototype test.**

Add assertions that load `/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi`, verify all eleven source states, verify four-direction metadata for `fullarmor_soldier`, `fullarmor_soldier_lead`, `y-boots_powered`, `helmet_powered`, `cannonpack`, and `plasma_cannons`, and verify the `Clothing` components of `CMUYautjaPoweredArmor`, `CMUYautjaPoweredGreaves`, `CMUYautjaPoweredHelmet`, and `CMUYautjaCannonPack` point to `mcaste_gear_worn.rsi`.

- [ ] **Step 2: Run the asset test to verify failure.**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~MilitaryCasteWornGearUsesOriginalCmss13OnMobRsi"
```

Expected: failure because the worn RSI does not yet exist and the Clothing prototypes still use the object RSI.

- [ ] **Step 3: Extract the original DMI into RSI sheets.**

Read the DMI `Description` metadata and image atlas from `cmss13-ref-full/icons/mob/humans/onmob/hunter/mcaste_gear.dmi`. For each state, preserve the DMI frame order and write a PNG sheet with one 32x32 tile for one-direction states or four horizontal 32x32 tiles for four-direction states. Create `meta.json` with license `CC-BY-SA-3.0`, CMSS13 source attribution, size 32x32, and the exact eleven state entries/direction counts.

- [ ] **Step 4: Wire only worn layers to the new RSI.**

Change the `Clothing.sprite` path for powered armor, powered greaves, powered helmet, and cannon pack to `_CMU14/Yautja/mcaste_gear_worn.rsi`. Leave their `Sprite` and `Item` paths on `_CMU14/Yautja/mcaste_gear.rsi`, preserving object/held presentation. Do not alter the cannon prototype's weapon stats or pack ownership logic.

- [ ] **Step 5: Run the asset and military regression tests.**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~MilitaryCasteWornGearUsesOriginalCmss13OnMobRsi|FullyQualifiedName~MilitaryCasteHudIconsUseCmss13States|FullyQualifiedName~MilitaryCasteJobsSpawnWithFixedRoleGear|FullyQualifiedName~DualPlasmaCannonsStaticGunConfigMatchesCmss13MilitaryCasteFacts"
```

Expected: all selected tests pass and the original object RSI remains the item/world sprite.

- [ ] **Step 6: Commit the scoped change.**

```powershell
git add Content.IntegrationTests/_CMU14/Yautja/YautjaBowTest.cs Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/mcaste_items.yml Resources/Textures/_CMU14/Yautja/mcaste_gear_worn.rsi
git commit -m "fix: use original yautja military worn sprites"
```

### Task 5: Full verification and client/server restart

**Files:**
- Modify: none unless verification identifies a scoped defect.
- Inspect: `runclient.bat`, `runserver.bat`, `client-run.log`, `server-run.log`, process command lines.

**Interfaces:**
- Consumes: all completed task changes and the repository launch scripts.
- Produces: verified test/build results and fresh RussianCM client/server processes, with previous matching instances stopped.

- [ ] **Step 1: Run focused tests together.**

Run the combined Yautja regression filter used in Tasks 1–4. If the combined run exceeds the test host timeout, run the filters individually and record each result instead of claiming a combined pass.

- [ ] **Step 2: Run the relevant project build.**

Run:

```powershell
dotnet build Content.Server/Content.Server.csproj --no-restore
dotnet build Content.Client/Content.Client.csproj --no-restore
```

Expected: both builds pass with no new warnings treated as errors.

- [ ] **Step 3: Inspect current project processes before stopping anything.**

Use `Get-CimInstance Win32_Process` and filter by `Content.Server`/`Content.Client` in the command line. Confirm each candidate's executable, arguments, and working directory point to `D:\RussianCM`. Do not stop unrelated .NET processes or processes from another workspace.

- [ ] **Step 4: Stop only matching old RussianCM client/server processes.**

Stop the verified process IDs with `Stop-Process -Id <pid> -Force`, then confirm the selected PIDs exit. Do not delete logs or alter unrelated processes.

- [ ] **Step 5: Start fresh client and server instances.**

Launch `runserver.bat` and `runclient.bat` from `D:\RussianCM` with hidden windows, wait for startup, then verify fresh process IDs and the newest server/client log output. If a launcher waits on `pause`, start the underlying `dotnet run --project Content.Server` and `dotnet run --project Content.Client` commands instead, still from the repository root.

- [ ] **Step 6: Perform final verification before reporting completion.**

Run `git diff --check`, inspect `git status --short`, confirm only intended scoped files were changed beyond the existing user worktree state, and report test/build results plus the new client/server PIDs. Do not claim success without command output confirming it.

