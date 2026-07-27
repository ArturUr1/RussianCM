# Yautja Dropship Destination Isolation Design

## Goal

Only the Yautja Hunter Shuttle may receive or launch toward Hunter Ship dropship destinations. Standard ERT and ordinary faction dropships must neither see those destinations in their navigation UI nor reach them through a forged launch request.

## Current Problem

The Hunter Shuttle console and all three Hunter Ship destinations currently use the generic `thirdparty` route faction. Standard ERT dropships use that same faction, so the existing navigation filter treats the Hunter Ship pads as valid ERT destinations.

The UI and launch paths also perform different validation. The UI filters destinations by faction, while the normal launch handler accepts any entity with `DropshipDestinationComponent` and relies on `FlyTo`. `FlyTo` only has a special restriction for strict third-party shuttles and does not enforce the general faction-matching rule used by the UI.

## Route Ownership

Introduce `yautja` as a dedicated value of the existing string-based route faction:

- The `CMUYautjaHunterShuttleConsole` prototype keeps its existing navigation-console behavior but overrides `WhitelistedShuttle` with:
  - `faction: yautja`
  - `ShuttleType: Dropship`
  - `autoReturn: false`
- `CMUHunterShipYautjaLandingPadAFTLBeacon`, `CMUHunterShipYautjaLandingPadBFTLBeacon`, and `CMUHunterShipYautjaHangarA` use `FactionControlling: yautja`.
- Standard ERT consoles and destinations remain `thirdparty`.

The Hunter Shuttle may continue to use neutral destinations whose `FactionController` is empty. It may not use destinations owned by `thirdparty`, `govfor`, `opfor`, or another non-matching faction.

## Shared Destination Authorization

`DropshipSystem` will have one destination-authorization predicate used by both `RefreshUI` and `FlyTo`.

The predicate follows these rules:

1. A strict `thirdparty` console may use only `thirdparty` destinations, preserving existing ERT behavior.
2. A console without a route faction may use only neutral destinations.
3. A non-third-party console with a route faction may use neutral destinations and destinations whose `FactionController` matches the console faction case-insensitively.
4. A destination with a non-empty, non-matching `FactionController` is rejected.

Existing destination-type checks, per-shuttle third-party return-vector ownership, occupancy checks, tactical-hover restrictions, and withdraw restrictions remain separate and unchanged.

## Server Enforcement

`FlyTo` applies the shared authorization predicate before changing destination ownership or starting FTL.

When a user requests a forbidden destination:

- the launch returns `false`;
- the user receives a caution popup;
- the server writes a warning identifying the user, console, and rejected destination;
- no destination, FTL, or landing-light state is mutated.

This makes a forged `DropshipNavigationLaunchMsg` subject to the same route rules as the navigation UI.

## Testing

Add focused integration coverage that exercises the production authorization path:

- verify the Hunter Shuttle console resolves to route faction `yautja` with automatic return disabled;
- verify all three Hunter Ship destinations resolve to route faction `yautja`;
- verify a standard `thirdparty` ERT console does not receive Hunter Ship destinations;
- verify an ordinary faction console does not receive Hunter Ship destinations;
- verify the Yautja console receives all three Hunter Ship destinations;
- verify direct `FlyTo` requests from ERT and ordinary consoles to a Hunter Ship destination return `false` without entering FTL;
- verify a direct Yautja `FlyTo` request to a Hunter Ship destination is accepted when the shuttle is otherwise launchable;
- preserve the existing standard ERT-to-`thirdparty` and ordinary-console-to-neutral routes.

The regression test must fail against the current `thirdparty` Hunter Ship configuration before production prototypes or authorization code are changed.

## Scope

This change does not alter character access to the Hunter Shuttle console, tactical landing mechanics, ERT spawning, Hunter Ship map geometry, or unrelated faction gameplay. It only isolates navigation destinations and enforces the existing route-faction model on the server.
