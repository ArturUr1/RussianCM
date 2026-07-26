# Yautja bracer actions and tracker design

## Goal

Remove Yautja action-bar entries whose functionality is already exposed by the worn Yautja bracer UI, while retaining the bracer UI entry point and independent abilities. Fix the bracer tracker so ordinary Yautja technology is not reported as lost gear unless it has an explicit tracking link.

## Current behavior and root cause

The worn-bracer action collection in `YautjaPowerSystem` exposes several commands that are also present as buttons in `YautjaBracerWindow`. `YautjaAbilitySystem` separately grants an action for opening the marks panel, which is also available from the bracer UI.

`YautjaBracerMenuSystem.IsTrackedItem` currently treats every entity with `YautjaTechItemComponent` as tracked unless it has `YautjaUntrackedItemComponent`. Since Yautja weapons normally have `YautjaTechItemComponent`, they are all included in the lost-gear readout. CMSS13 separates the tracking marker from general Yautja technology; only items with the explicit tracked element enter the loose-gear list.

## Scope

### Remove from the worn-bracer action bar

Stop exposing these actions when the bracer is worn:

- self-destruct;
- translator;
- ID chip toggle;
- thrall-bracer linking;
- thrall message transmission;
- gear tracking/open tracker action;
- stabilising crystal creation;
- human stabilising crystal creation;
- hunting trap creation;
- standalone marks-panel action on the Yautja entity.

The bracer menu action remains as the entry point to the bracer UI. In-hand actions needed to operate a bracer that is not worn remain available. Independent actions such as cloak, recall, disc, notification/name controls, explicit add/remove tracking, and healing capsule creation are outside the duplicate set and remain available.

### Tracker behavior

The tracker includes an item only when it has `YautjaTrackedItemComponent`. General `YautjaTechItemComponent` remains useful for Yautja technology restrictions and damage behavior, but no longer implies tracker registration. Existing add/remove tracking operations continue to add/remove the explicit tracking component.

## Implementation

1. Adjust the worn-bracer branch in `YautjaPowerSystem` and the Yautja ability grant/removal path so duplicate action entities are no longer exposed.
2. Keep bracer UI command handlers and their underlying public systems unchanged; the UI remains the single presentation path for the duplicated functions.
3. Change the tracker predicate in `YautjaBracerMenuSystem` to require explicit tracking.
4. Add focused regression tests for explicit versus implicit tracker registration and for preservation of non-duplicated action behavior where the existing test harness supports it.

## Verification

- Run the focused Yautja tests and confirm the new tracker regression cases fail before the production change and pass after it.
- Run the complete relevant test project/build command.
- Inspect the final diff to ensure only the requested Yautja code/tests and this design document are included; preserve all pre-existing user changes.

## Alternatives considered

1. A policy abstraction for all action-bar visibility: cleaner centralization, but unnecessary for this targeted removal.
2. Deleting every now-unused action prototype, event, component field, and localization entry: a larger compatibility risk and beyond the requested UI behavior.

The selected design is the minimal behavior change that matches the CMSS13 tracking model and the approved scope.
