# Dense Fog Dialog Serialization Design

## Goal

Clicking the dense fog at Yautja hunting grounds must open the existing escape confirmation dialog without terminating the server or disconnecting the client.

## Root cause

`YautjaPreserveEdgeSystem` places `YautjaPreserveEscapeChoiceEvent` instances inside networked `DialogOption` values. The event is the only neighboring Yautja dialog event without `[Serializable, NetSerializable]`, so PVS serialization throws `KeyNotFoundException` when the dialog is replicated.

## Chosen design

Add `[Serializable, NetSerializable]` to `YautjaPreserveEscapeChoiceEvent` in `Content.Shared/_CMU14/Yautja/YautjaHuntEvents.cs`. This follows the established event pattern and changes no gameplay behavior: non-Yautja players still receive the existing confirmation dialog and five-second escape do-after, while Yautja players remain denied at the edge.

Add a connected server/client integration regression test that spawns the preserve edge, triggers the real hand interaction, allows the dialog state to replicate, and verifies both processes remain alive. The test protects the failure boundary rather than asserting source attributes alone.

## Alternatives considered

1. Register the event for network serialization (chosen): smallest change, preserves the existing dialog and event handler.
2. Replace the event with an existing serializable event: would require changing event semantics or adding overloaded state and gives no benefit.
3. Remove the dialog and deny or immediately process interaction: avoids serialization but changes the approved hunting-grounds escape behavior.

## Verification

- The new regression test fails before the attribute is added with the same missing-type serialization failure.
- The new test passes after the attribute is added.
- Existing targeted Yautja integration tests and Debug server/client builds pass.
- Runtime logs contain no fatal PVS serialization error when the dense-fog dialog is opened.
