# Yautja Entitlements, Clan Permission, Hellhound Workflow, and UI Design

## Goal

Correct Yautja personalization access for senior ranks outside clan membership,
restrict the F7 clan administration entry to a dedicated permission, verify and
repair the complete hellhound takeover workflow, and make the personalization
controls fit cleanly with long localized labels.

## Rank and personalization policy

The profile model must distinguish two concepts:

- **Entitlement capabilities** come from the player's external Yautja rank,
  clan membership, and whitelist flags. They determine which personalization
  options the player may save.
- **Active capabilities** come from applying the selected profile status to the
  entitlement capabilities. They determine the rank and special status used by
  the spawned character.

Every rank-gated personalization decision uses entitlement capabilities. This
includes the unique set, ceremonial cape, advanced bracers, legacy equipment,
and any other option whose requirement is expressed as a minimum rank or
whitelist capability. A player with external rank `Ancient` can therefore use
all options available to `Elite`, `Leader`, and `Ancient`, even when the profile
status is `Normal`.

The selected status continues to control the in-round identity. External
`Ancient` plus `Normal` spawns as the ordinary `Blooded` gameplay rank; selecting
an allowed senior status produces that senior gameplay rank.

The client editor and server sanitizer consume the same shared entitlement
policy. The server remains authoritative and removes only selections that the
base entitlement capabilities do not permit. Changing status must not silently
strip otherwise permitted personalization.

## Dedicated clan administration permission

Add `Clans` as a new independent `AdminFlags` bit. The existing permissions
editor discovers enum flags automatically, so it will expose a `CLANS`
checkbox without a parallel configuration list.

The `yautja_clan_admin` command requires `AdminFlags.Clans`. The clan EUI checks
the same flag when it opens, for every incoming mutation message, and whenever
permissions change. Losing the flag closes or denies the EUI immediately.
Possessing generic `Admin` permission alone does not grant clan administration.

The F7 command button remains command-backed. Consequently it is shown only
when the server advertises the command to the current admin, keeping client
visibility and server authorization aligned.

## Hellhound workflow

The intended flow is:

1. A player clicks the sleeping hellhound with either hand interaction or
   in-world activation.
2. A confirmation dialog explains that the hellhound will awaken.
3. Cancelling changes nothing. Confirming replaces the sleeping entity with the
   active hellhound and preserves the configured owner/ship relationship.
4. The active hellhound exposes a ghost role using the standard raffle
   settings.
5. A ghost can click the role, join the normal shuffle and, if selected, have
   their mind transferred into the hellhound body. The role is then marked
   taken and removed from the raffle.

Existing production behavior is retained when the end-to-end tests prove all
five stages. Implementation changes are limited to any stage that fails, so the
workflow is not duplicated or replaced unnecessarily.

## Personalization UI

Replace the fixed-width horizontal technology rows with responsive option
blocks:

- title and optional requirement/help text occupy their own line;
- the selector fills the available width below the title;
- a preview action, when present, sits beside the selector only while both fit
  and otherwise remains in a compact secondary position;
- long Russian labels wrap or receive enough horizontal space instead of being
  clipped;
- existing disabled-option tooltips and preview callbacks remain unchanged.

The layout keeps the current visual language and grouping, but removes hard
label widths that assume short English text. Translator type, cloak sound, and
future localized values must fit at the editor's supported minimum width.

## Data flow and failure behavior

The server sends the resolved entitlement capabilities to the client. The
editor uses them for option availability and sends the chosen profile normally.
On persistence and spawn, the server sanitizes the profile with the same base
capabilities, then derives active capabilities from the selected status for
in-round application.

Missing or stale rank data remains fail-closed: default capabilities do not
grant restricted equipment. Missing `Clans` permission denies the command and
all EUI operations. A cancelled hellhound dialog or a raffle loss leaves the
requesting player outside the hellhound body.

## Validation

- Shared and integration tests cover external `Ancient` with `Normal`, proving
  ceremonial cape, unique set, and every other lower-rank entitlement survive
  client policy and server sanitization while active rank remains `Blooded`.
- Admin flag tests cover serialization/bit uniqueness, command availability,
  EUI open/message authorization, permission removal, and the F7 entry.
- Hellhound tests cover both click paths, cancel/confirm, active ghost-role
  creation, default shuffle enrollment, winner mind transfer, and role cleanup.
- Client layout tests cover minimum-width sizing and long translator/cloak
  labels without fixed-width clipping.
- Focused tests run first, followed by client and server builds and
  `git diff --check`.
