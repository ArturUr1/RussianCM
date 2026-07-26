# Yautja Rank Parity Design

**Date:** 2026-07-26

**Goal:** Bring the missing CMSS13 Yautja rank behavior into the RussianCM port, including authoritative rank handling, rank-gated equipment and access, spawn/role behavior, and rank icons in the lobby and in-world status HUD.

## Scope

The implementation covers the seven clan ranks used by the original CMSS13 Predator role:

- Unblooded
- Young Blood
- Blooded
- Elite
- Elder
- Clan Leader
- Ancient

Bad Blood, Stranded Predator, Blooded Thrall, and other special roles remain separate role/status categories. The existing trophy-score progression (`Hunter`, `Blooded`, `Elite`, `Elder`) is not the clan rank and must not be merged with it.

## Design

### Canonical rank model

Add one shared canonical `YautjaRank` model and rank metadata. The metadata is the single source for the localized rank name, rank icon state, cumulative access tier, profile unlocks, loadout preset, spawn category, and slot-policy flags.

The server is authoritative for a player's clan rank. Client-supplied profile data may contain legacy rank fields for compatibility, but profile application must sanitize or overwrite them from the server-resolved rank before granting equipment or access.

Existing `YautjaBracerOwnerRank` values may be retained as a compatibility projection for bracer serialization, but all new checks must resolve through the canonical rank metadata rather than maintaining independent rank tables.

### Rank behavior

Normal whitelisted Predator players use the single Hunter job. Their resolved rank selects the appropriate loadout and bracer access. A missing rank record for an otherwise whitelisted Hunter defaults to Blooded, matching the original job fallback.

Young Blood is represented separately for the non-whitelisted Hunting Grounds role and uses the Young Blood gear/rack path. Unblooded remains an administrative/clan state and does not become a self-selectable profile option.

Rank access is cumulative:

- Unblooded/Blooded: Secure
- Elite: Secure + Elite
- Elder: Secure + Elite + Elder
- Clan Leader: Secure + Elite + Elder + Leader
- Ancient: Secure + Elite + Elder + Leader + Ancient

Generic Yautja secure doors and the base Hunter rack require Secure. Elder doors and the Elder rack require Elder or Ancient. Ancient doors require Ancient. No separate Leader-only door or rack is introduced because the current CMSS13 source does not define one.

`Unique` profile sets are available only to Elite, Elder, Clan Leader, and Ancient. Legacy sets remain governed by their separate legacy-whitelist rule. Leader and Ancient bypass the ordinary Predator rank slot cap; ordinary spawn locations do not vary by rank.

### Spawn behavior

Normal ranks spawn at a random Hunter Ship clan spawn in the Middle Deck sleeping/prep area. Rank changes the role equipment and access after spawn, not the ordinary spawn point.

The special role paths remain distinct:

- non-whitelisted Young Blood: Hunting Grounds Young Blood spawn;
- Bad Blood and Stranded Predator: Predator Survivor Base spawn.

### Rank icons

Create a dedicated rank icon RSI/prototype set with seven states matching the canonical ranks. The same rank-to-icon mapping is consumed by:

1. the Yautja lobby/profile UI, where the current rank is shown next to rank information and rank-gated options;
2. the in-world status icon system, where the icon is shown to Yautja/authorized viewers without exposing the hidden faction to ordinary characters.

Icons are not derived from arbitrary item sprites and are not tied to trophy-score ranks.

### Testing

Tests must cover:

- rank metadata and cumulative access mapping;
- server-side profile rank sanitization;
- rank-specific loadout and profile unlock checks;
- door and rack access thresholds;
- normal, Young Blood, Bad Blood, and Stranded spawn categories;
- Leader/Ancient slot-policy behavior;
- rank icon state selection and viewer filtering;
- compatibility behavior for existing profiles with no stored rank.

## Non-goals

- Replacing the existing Yautja trophy progression system.
- Creating separate jobs for each rank.
- Making rank selectable by the client in the profile editor.
- Changing unrelated Yautja equipment, combat, or honor-code behavior.

