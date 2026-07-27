# Yautja Clan Rank Workflow and View Clan Info

Date: 2026-07-27

## Goal

Bring the RussianCM Yautja rank workflow in line with the original CMSS13 clan system, while preserving the already implemented server-authoritative rank application, spawn locations, access, slot behavior, and rank icons. Add a separate `View Clan Info` menu comparable to the original `OOC.Records` verb.

The original source is the behavioral reference. The client may request data or an action, but the server remains authoritative for clan membership, rank, permissions, whitelist status, limits, and equipment.

## Original behavior to reproduce

- A clan member has a clan, rank, permissions, and honor; the clan has a name, description, honor, and color.
- Ranks are `Unblooded`, `Young Blood`, `Blooded`, `Elite`, `Elder`, `Leader`, and `Ancient`.
- `Young Blood` is the clanless special role and is not a normal selectable clan rank.
- Normal clan rank changes do not offer `Young Blood` or `Ancient`.
- Removing a member from a clan resets the member to `Blooded`.
- Moving a member into a clan resets them to `Blooded` unless they already have Ancient administrator permissions.
- A member cannot target themselves, an equal-or-higher rank, or a member with Ancient administrator permissions.
- Rank permissions and limits are enforced by the server:
  - `Unblooded`: requires administrator modification permission.
  - `Young Blood`: no clan-management permission; special role only.
  - `Blooded`: ordinary user modification permission.
  - `Elite`: ordinary user modification permission; maximum five members at that rank.
  - `Elder`: ordinary user modification permission; maximum one member per twelve clan members, rounded up.
  - `Leader`: all user permissions and administrator modification permission; maximum one member.
  - `Ancient`: administrator/manager permission; not part of normal clan promotion.
- Only an Ancient with manager permission can create/remove Ancient status. Yautja Leader/Council whitelist status grants Ancient rank and the corresponding permissions when the player profile is loaded.
- Ancient and Leader reservations bypass the normal Yautja slot cap. Young Blood is a separate non-whitelisted special role and does not consume the normal Predator count.
- Rank gear/access is derived from the authoritative rank, not from client profile data.

## Data model

Add persistent clan records and clan-member records in the server database:

- `YautjaClan`: stable id, name, description, honor, color, active flag.
- `YautjaClanMember`: player id, nullable clan id, rank, permission bitmask, honor, timestamps.
- Player whitelist flags for the Yautja role, Council, and Yautja Leader/Senator status, reusing the existing player whitelist infrastructure where possible.

`YautjaClanMember` becomes the source of truth for a normal rank. The existing `Player.YautjaRank` field remains as a compatibility projection during migration and is updated whenever the authoritative rank changes; new gameplay code must not use it as an independent authority.

Existing non-null `Player.YautjaRank` values are migrated exactly once into a member record so the current server does not silently downgrade existing players. The migration marks those records as legacy/unassigned until an operator places them in a real clan. Once assigned or removed, the record follows the original clan rules. Invalid/Young Blood values resolve to Blooded for normal play.

All rank/permission/member changes are atomic and invalidate the rank cache. A failed database operation must not grant the requested rank or access.

## Server API and policy

Create a shared policy layer with explicit permission and limit checks, used by commands, the menu, spawn setup, and tests:

- `CanViewClanInfo(actor)`
- `CanTarget(actor, target)`
- `GetAssignableRanks(actor, clan)`
- `CanModifyRank(actor, target, requestedRank)`
- `CanMoveMember(actor, target, destinationClan)`
- `CanSetAncient(actor, target)`
- `ApplyRankAndPermissions(member, rank)`

The policy must use the original bitmask semantics and target ordering. It must reject self-targeting before any mutation and must re-check permissions and limits inside the transaction to avoid stale UI or concurrent-action bypasses.

The existing direct rank command is retained only as a compatibility/admin surface and is routed through the same policy/service. It cannot bypass self-target, rank-order, Ancient, or limit rules. A separate explicitly logged migration/debug operation is allowed only for host-level maintenance and is not used by normal gameplay.

## View Clan Info menu

Add a separate player-facing `View Clan Info` entry in the OOC/Records menu. It opens a server-backed EUI window rather than reading client-owned profile fields.

The window displays:

- the viewer's current clan, description, honor, and color;
- the viewer's rank and effective clan permissions;
- clan members grouped or sorted by rank, with name, rank, honor, and online status;
- the current rank limits/occupancy for the clan;
- a clear clanless/Young Blood state when no normal clan membership exists.

The server sends only data the viewer is allowed to see. The UI renders available actions from server-provided capabilities, but every action is validated again server-side:

- modify a member's rank;
- move a member to another clan or remove them from a clan;
- promote/demote Ancient status when the actor has manager permission;
- refresh the clan data.

The menu must not expose Ancient or Young Blood as normal rank-selection options. It must show a user-readable denial/error when a stale action is rejected. No rank, permission, whitelist, or access change is performed solely by closing/reopening the UI or by sending a client profile update.

Use the existing EUI state/message conventions in `Content.Shared`, `Content.Server`, and `Content.Client`; keep the state serializable and avoid client-side authority. The menu is available only to appropriate Yautja/authorized viewers, and viewing another clan's private management data requires the corresponding clan permission.

## Spawn, whitelist, and equipment integration

- Normal Yautja spawning resolves the clan-member record, then applies rank, gear, icons, access, and slot behavior through the existing authoritative spawn/apply systems.
- Missing, invalid, or unassigned normal rank falls back to Blooded for gameplay.
- Yautja Leader/Council whitelist status is applied on profile load as Ancient with the original administrator permissions; ordinary clan changes cannot remove that status.
- `Young Blood` remains a separate special spawn flow at the Hunting Grounds and is never persisted as a normal clan rank.
- Normal Yautja cap counting continues to exclude Leader/Ancient reservations and the separate Young Blood role.
- Existing rank icon selection remains centralized in rank metadata and is used by the clan menu, equipment, and HUD/profile presentation.

## Verification strategy

Add tests before implementation for:

- rank-to-permission mapping, target ordering, self-target rejection, Ancient protection, and each rank limit;
- clan/member database round trips and migration of existing `Player.YautjaRank` values;
- authorized and unauthorized View Clan Info access;
- every menu action being rejected when the actor loses permission, the target changes, or the limit is reached;
- normal rank spawn/access/gear/icon parity, Leader/Ancient cap bypass, and Young Blood separation;
- client profile attempts not changing the authoritative rank;
- server and client builds/startup with the new EUI and database migration.

## Implementation boundaries

The change should not rewrite unrelated Yautja mechanics. Existing bracer interactions, hunt/blooding flow, spawn points, rank metadata, and RSI restoration remain intact unless a focused parity fix is required by the clan workflow. Database changes must be additive/migratable, and unrelated dirty files in the worktree must remain untouched.
