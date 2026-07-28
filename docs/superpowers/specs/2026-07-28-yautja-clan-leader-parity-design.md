# Yautja Clan Leader Parity Design

Date: 2026-07-28

## Goal

Complete the remaining CMSS13 Yautja clan-leader workflow parity in RussianCM. The implementation must keep ordinary clan leadership separate from whitelist Leader/Council authority, expose the missing player actions, and keep all rank and permission changes server-authoritative.

## Reference behavior

The behavioral reference is `cmss13-ref-full/code/modules/clans/` and the related player/job code.

There are two separate concepts:

1. `YautjaRank.Leader` is a normal rank inside a clan.
2. `YautjaWhitelistFlags.Leader` is a global whitelist status that resolves to Ancient-level authority.

An ordinary clan Leader has `UserAll` permissions, can manage lower-ranked members of the relevant clan, and cannot manage Ancient status or other clans. Assigning the Leader rank requires `AdminModify`; this is a permission requirement for the target rank, not a permission granted to every Leader.

Whitelist Leader resolves to `Ancient` with the complete `All` permission mask. It can view and modify all clans, move members, manage Ancient status, and perform manager-only clan actions. Whitelist Council resolves to `Ancient` with `AdminAncient`; it can view and modify clans but cannot manage Ancient status. If both flags exist, Leader authority wins.

## Architecture

Keep the current server-authoritative architecture:

```text
database member/whitelist state
        -> YautjaClanManager.Resolve
        -> authoritative rank and permissions
        -> spawn/profile/equipment integration
        -> View Clan Info EUI
        -> server-side mutation validation
        -> atomic database write and cache invalidation
```

`YautjaClanMember` remains the authority for ordinary persistent clan membership, rank, permissions, honor, and legacy state. `Player.YautjaRank` remains a compatibility projection and is updated together with member changes. Client profiles, EUI state, and command arguments never grant rank, permissions, access, gear, or slot bypass.

## Rank and permission policy

Update the shared permission model to match CMSS13 exactly:

- `UserAll = UserView | UserModify`.
- `AdminAncient = AdminView | AdminModify | AdminMove`.
- `All = UserAll | AdminAncient | AdminManager`.
- Normal `Leader` rank permissions are `UserAll`.
- The required permission for assigning `Leader` is `AdminModify`.
- `Ancient` is not in the normal rank selector.
- `YoungBlood` is a separate special role and is not persisted as a normal clan rank.

The shared policy must reject self-targeting, equal-or-higher targets, Ancient manager targets, missing clan membership, and rank-limit violations. It must preserve the original limits: one Leader, at most five Elite members, and at most one Elder per twelve clan members rounded up.

Whitelist resolution must preserve a member's `ClanId` when a member record exists. It must not replace the member with a clanless special result merely because the player has Leader or Council whitelist. Leader and Council still receive their special effective rank and permissions, but the view layer determines whether they see one clan or all clans from their permissions.

Removing elevated whitelist flags must invalidate rank caches. Removing all Yautja whitelist flags must reset elevated persistent state to the Blooded baseline while retaining the clan membership record where appropriate.

## View Clan Info workflow

The player-facing EUI remains the entry point for gameplay actions. It will provide:

- current viewer rank and effective permissions;
- a selected clan view;
- a selectable list of all clans for viewers with `AdminView`;
- the viewer's own clan and clanless players for ordinary `UserView` viewers where permitted;
- clan name, description, color, honor, and member roster;
- rank, honor, online state, and available actions per member;
- a status/error message after every mutation.

Actions are exposed only when the current snapshot says they may be useful, but the server re-checks every request against fresh state.

Permissions map to actions as follows:

- `UserModify`: edit the description of the viewer's own clan and change ranks of lower-ranked members in that clan.
- `AdminModify`: rename and recolor a clan and assign ranks whose original rule requires administrator modification.
- `AdminMove`: remove a member from a clan or move them to another selected clan, resetting them to Blooded unless Ancient permissions are intentionally preserved by the original rule.
- `AdminManager`: set/remove Ancient status, change honor, purge a clan profile, and delete a clan.

The client never directly edits a clan or member. It sends typed EUI messages, and `YautjaClanManager` performs the authorization and database mutation.

The staff-only clan admin EUI remains separate. It continues to support creating clans and maintenance operations that ordinary gameplay roles do not receive.

## Data flow and persistence

All member mutations update the member row and the compatibility `Player.YautjaRank` projection in one transaction. Successful mutations invalidate the actor and target rank/clan caches. Failed writes must not leave a partial elevated rank or permission state.

Moving a normal member out of a clan or into another clan resets their normal rank to Blooded and applies the Blooded permission mask. Ancient manager state is changed only through the dedicated Ancient action and its policy check.

Clan metadata mutations must update only the requested editable fields and preserve honor unless honor itself is being changed. Clan deletion deactivates the clan and detaches its members without deleting their player records.

## Testing strategy

Use TDD for each behavior change. Add failing tests before production changes, verify the expected failure, implement the smallest change, then verify the focused test passes.

Shared policy tests must cover:

- exact permission masks for ordinary ranks and special whitelist resolutions;
- ordinary Leader cannot target itself, an equal Leader, or Ancient;
- Ancient Council cannot change Ancient status;
- whitelist Leader can change Ancient status;
- rank limits and required permissions;
- `All` includes both user and admin permission groups.

Server/database tests must cover:

- preserving `ClanId` for whitelist Leader/Council resolution;
- selecting all clans for AdminView and only the allowed scope for UserView;
- description, rename, color, honor, move, purge, and Ancient mutations;
- reset to Blooded after removal or ordinary transfer;
- atomic projection updates and cache invalidation;
- clearing whitelist state without stale elevated permissions.

Client/EUI tests must cover:

- clan selector and member roster state;
- target-clan selector for move actions;
- omission of YoungBlood and Ancient from normal rank choices;
- action visibility for ordinary Leader, Council, whitelist Leader, and manager;
- status/error rendering after a rejected action.

Spawn/profile tests must continue to verify that authoritative rank controls access, gear, icons, and slot bypass, and that a client-supplied profile cannot elevate a Blooded hunter.

## Scope boundaries

This change does not rewrite unrelated Yautja mechanics, spawn maps, bracer behavior, weapons, or unrelated dirty worktree files. It modifies only the clan policy, clan manager/database mutations, clan EUI state/client UI, rank resolution integration, localization, and focused tests required for this parity.
