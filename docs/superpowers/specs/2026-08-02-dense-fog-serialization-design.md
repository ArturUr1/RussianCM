# Dense Fog Dialog Serialization Design

## Goal

Clicking the dense fog at Yautja hunting grounds must open the existing escape confirmation dialog without terminating the server or disconnecting the client.

## Root cause

`YautjaPreserveEdgeSystem` places `YautjaPreserveEscapeChoiceEvent` instances inside networked `DialogOption` values. The event is the only neighboring Yautja dialog event without `[Serializable, NetSerializable]`, so PVS serialization throws `KeyNotFoundException` when the dialog is replicated.

## Chosen design

Add `[Serializable, NetSerializable]` to `YautjaPreserveEscapeChoiceEvent` in `Content.Shared/_CMU14/Yautja/YautjaHuntEvents.cs`. This follows the established event pattern and changes no gameplay behavior: non-Yautja players still receive the existing confirmation dialog and five-second escape do-after, while Yautja players remain denied at the edge.

Add a shared serialization-contract regression test for the event. The connected runtime scenario remains part of manual verification because the current integration harness aborts during client startup on pre-existing duplicate-localization errors before it can reach the interaction.

## Alternatives considered

1. Register the event for network serialization (chosen): smallest change, preserves the existing dialog and event handler.
2. Replace the event with an existing serializable event: would require changing event semantics or adding overloaded state and gives no benefit.
3. Remove the dialog and deny or immediately process interaction: avoids serialization but changes the approved hunting-grounds escape behavior.

## Verification

- The new regression test fails before the attribute is added because the event has neither required serialization attribute.
- The new test passes after the attribute is added.
- Existing targeted Yautja integration tests and Debug server/client builds pass.
- Runtime logs contain no fatal PVS serialization error when the dense-fog dialog is opened; duplicate-localization warnings remain a pre-existing non-fatal issue.
