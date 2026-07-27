# Yautja Bracer Menu Action Migration Design

**Date:** 2026-07-27

## Goal

Move seven Yautja bracer controls out of the action bar and expose them through the existing bracer menu, without changing their gameplay behavior:

- `ChangeExplosionType`
- `RemoveBracerAttachments`
- `CreateHealingCapsule`
- `AddTrackedItem`
- `RemoveTrackedItem`
- `ToggleBracerName`
- `ToggleBracerNotificationSound`

The action prototypes and shared events remain available for compatibility, but these actions must no longer be granted to the player.

## Current context

`YautjaPowerSystem` currently grants `ChangeExplosionType` to a held or worn Yautja bracer and grants the other Yautja bracer utility actions from the worn-bracer action path. `YautjaAttachmentSystem` grants `RemoveBracerAttachments` from a gear-container action path. The existing `YautjaBracerPanelCommand` protocol already routes several menu buttons to server-side systems, while `YautjaBracerWindow` builds the client buttons and sends those commands.

The server already exposes the required behavior through these methods:

- `YautjaBracerUtilitySystem.TryChangeExplosionType`
- `YautjaAttachmentSystem.TryRemoveBracerAttachments`
- `YautjaBracerUtilitySystem.TryCreateHealingCapsule`
- `YautjaBracerUtilitySystem.TryAddTrackedItem`
- `YautjaBracerUtilitySystem.TryRemoveTrackedItem`
- `YautjaBracerUtilitySystem.TryToggleBracerName`
- `YautjaBracerUtilitySystem.TryToggleNotificationSound`

## Design

### 1. Action ownership

Remove only the seven action grants from their current action-provider paths:

- Remove `ChangeExplosionType` from both held and worn `YautjaBracerComponent` action grants.
- Remove the seven listed bracer controls from the worn bracer action grants.
- Remove `RemoveBracerAttachments` from `YautjaAttachmentSystem`'s gear-container action grant.

Do not delete the action prototypes or shared event types in this change. Existing server subscriptions and event handlers may remain unless compilation or tests show a direct dead-code issue; the menu will call the existing typed methods directly.

The existing `OpenBracerMenu`, cloak, recall, disc, gear deployment actions, and unrelated Yautja actions retain their current ownership and behavior.

### 2. Menu protocol and server routing

Add seven values to `YautjaBracerPanelCommand`, with names that map one-to-one to the requested behaviors. Extend `YautjaBracerMenuSystem.OnCommand` with direct calls to the existing systems:

| Menu command | Server operation |
|---|---|
| `ChangeExplosionType` | `_utility.TryChangeExplosionType(ent, actor)` |
| `RemoveBracerAttachments` | `YautjaAttachmentSystem.TryRemoveBracerAttachments` for the bracer's gear container |
| `CreateHealingCapsule` | `_utility.TryCreateHealingCapsule(ent, actor)` |
| `AddTrackedItem` | `_utility.TryAddTrackedItem(ent, actor)` |
| `RemoveTrackedItem` | `_utility.TryRemoveTrackedItem(ent, actor)` |
| `ToggleBracerName` | `_utility.TryToggleBracerName(ent, actor)` |
| `ToggleBracerNotificationSound` | `_utility.TryToggleNotificationSound(ent, actor)` |

The existing `CanUseMenu` validation remains the single access gate. After each command, the server refreshes the existing `YautjaBracerPanelState`, so charge, tracker entries, bracer flags, and cooldown-visible state stay synchronized.

### 3. Client menu layout

Add buttons to `YautjaBracerWindow` and bind each to the corresponding command:

- bracer settings/functions: explosion type, bracer name, notification sound, remove attachments;
- tracker controls: add tracked item and remove tracked item;
- fabricator: healing capsule.

Use the existing `ActionButton` style and localization pattern. The buttons should send only the typed `YautjaBracerPanelCommandMsg`; no client-side gameplay logic is added.

### 4. Error handling and behavior preservation

The menu calls the same guarded server methods that the actions currently use. Existing checks for bracer ownership, worn state, incapacitation, power, cooldowns, active-hand requirements, deployed attachments, and popup feedback remain authoritative. A failed operation still produces the existing popup and the menu state is refreshed afterward.

No new fallback is introduced for tracker membership: linked-item behavior continues to use explicit `YautjaTrackedItemComponent` semantics from the preceding bracer tracker fix.

## Testing and verification

### RED tests before production changes

Add or update integration tests to assert:

1. A worn Yautja bracer does not grant any of the seven action IDs.
2. A held bracer also does not grant `ChangeExplosionType`.
3. A Yautja gear container does not grant `RemoveBracerAttachments`.
4. The bracer panel command enum and server routing cover each of the seven commands.

The RED run must fail because the current action grants and missing menu commands are still present.

### GREEN tests after implementation

Run the focused Yautja integration tests covering bracer action rosters, menu command routing, tracker add/remove behavior, fabricator behavior, and attachment removal. Then run:

```powershell
dotnet build Content.Client/Content.Client.csproj --no-restore --nologo
dotnet build Content.Server/Content.Server.csproj --no-restore --nologo
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja"
```

Finally, start the client and server using the repository's normal launch commands or project entry points and verify that both remain running without startup exceptions. If disk capacity or unrelated dirty-worktree compilation errors prevent a full run, report the exact command, exit code, and blocking error instead of claiming a pass.

## Scope exclusions

- Do not remove action prototypes, icons, or localization entries solely because their grant is being removed.
- Do not change the existing bracer menu layout beyond adding the seven requested controls.
- Do not alter unrelated Yautja, health HUD, clan, weapon, map, or butcher work already present in the dirty worktree.
