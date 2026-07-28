# Yautja Loadout Restrictions, Dread Color, Preview, and Cache Design

## Goal

Make Yautja lobby personalization enforce the requested rank and whitelist
rules, add an independent dreadlock color, correct the Anubys and Ronin mask
position, and prevent `CMYYautjaHunter` late-join selection from throwing when
the clan cache is temporarily unavailable.

## Equipment access policy

The shared profile capability model is the authority for both the lobby editor
and server-side profile sanitization.

| Selection | Required capability |
| --- | --- |
| Ceremonial cape | Effective rank `Leader` or `Ancient` |
| Bronze, Crimson, or Bone bracer | Effective rank `Elite` or higher |
| Dragon, Swamp, Enforcer, or Collector legacy bracer | `Legacy` or `CouncilLegacy` whitelist |
| Any non-`None` legacy set | `Legacy` or `CouncilLegacy` whitelist |

All other existing equipment rules remain unchanged. Locked options remain
visible in the lobby editor, are disabled, and explain the required rank or
whitelist in their tooltip.

The client profile is untrusted. `YautjaCharacterProfile` sanitization applies
the same shared policy before persistence and again before spawning. A
disallowed ceremonial cape falls back to the full cape, a disallowed bracer
falls back to ebony, and a disallowed legacy set falls back to `None`.

## Dreadlock color

Add a dedicated dread-color setting to `YautjaCharacterProfile` instead of
continuing to force `Appearance.HairColor` to the selected skin color.

The appearance page gains a swatch selector alongside the existing skin and eye
selectors. The palette contains a backward-compatible `Match skin` default plus
muted black, dark brown, brown, auburn, ash, and bone colors. Changing skin
color updates the dread marking only while `Match skin` is selected. Selecting
a fixed dread color updates both `Appearance.HairColor` and the active Yautja
dreadlock marking, and changing the dread style preserves that color.

Clone, serialization, sanitization, preview, persistence, and spawned character
appearance all carry the dedicated value.

## Anubys and Ronin mask preview

The Anubys and Ronin mask RSI files use 32x64 frames while the equivalent
Cleopatra and Plated masks use 32x32 frames. Their equipped pixels are centered
inside the 64-pixel canvas, which anchors them around the character's groin.

Normalize both elite and dormant unique Anubys/Ronin RSI variants to 32x32.
Preserve the item icon pixels and crop/reposition the equipped directional
frames so their on-mob origin matches the working 32x32 masks. Prototype IDs and
equipment slots remain unchanged.

## Clan cache and late-join safety

The exception is caused by synchronous job-selection handlers calling
`ResolveCached` after a clan or rank mutation has invalidated the cache but
before an asynchronous prime has completed. Some mutation paths currently do
not prime the cache at all.

Introduce one refresh path that invalidates and asynchronously rebuilds both the
clan resolution and derived rank/capability cache. All rank, membership,
whitelist, move, remove, purge, and clan-delete mutation paths use it before
reporting success.

Synchronous job-selection reads must never throw. An unexpected cache miss
returns the safe Blooded/no-special-capabilities resolution, so it cannot grant
rank bypasses or restricted equipment. Normal player-data loading and completed
mutations keep the authoritative cache populated, making the fallback a
fail-closed last resort rather than the ordinary path.

## Validation

- Shared tests cover every rank boundary and both legacy whitelist variants.
- Profile tests prove unauthorized selections are cleared and authorized
  selections survive cloning and sanitization.
- Dread tests cover the default skin-linked mode, independent fixed colors,
  skin changes, style changes, and profile cloning.
- Client layout tests cover disabled selector decisions and tooltip policy.
- RSI integration tests require Anubys and Ronin to load as 32x32 with valid
  four-direction `equipped-MASK` states.
- Cache tests cover initial load, invalidation, safe synchronous cache miss, and
  refresh after each mutation class.
- Focused unit/integration tests, compilation, and `git diff --check` run before
  launching the server and client.
