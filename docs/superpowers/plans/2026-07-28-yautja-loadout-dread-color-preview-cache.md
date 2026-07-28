# Yautja Loadout, Dread Color, Preview, and Cache Implementation Plan

> **For agentic workers:** Execute each task test-first and preserve unrelated
> dirty-worktree changes.

**Goal:** Enforce the requested Yautja equipment permissions, add independent
dreadlock colors, repair Anubys/Ronin mask positioning, and make
`CMYYautjaHunter` selection resilient to clan-cache invalidation.

**Architecture:** Put all equipment decisions in the shared authoritative
capability/profile layer. Let the lobby consume those decisions for disabled
selectors while the server sanitizes the same fields before persistence and
spawn. Keep dread color in the Yautja profile and materialize it into humanoid
hair/marking colors. Repair mask coordinates at the RSI resource boundary.
Refresh clan/rank caches after mutations and use a fail-closed cached fallback
in synchronous job events.

**Tech stack:** C#/.NET 10, RobustToolbox UI/network/profile systems, YAML/RSI
resources, NUnit integration and client tests.

---

## Task 1: Shared equipment policy

**Files:**

- Modify: `Content.Shared/_CMU14/Yautja/YautjaRank.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaCharacterProfileTest.cs`
- Test: `Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs`

1. Add failing cases for cape, advanced bracer, legacy bracer, and legacy-set
   boundaries, including both `Legacy` and `CouncilLegacy` capability inputs.
2. Run the focused tests and confirm failures correspond to missing policy.
3. Add pure capability helpers and sanitize disallowed profile selections to
   full cape, ebony bracer, or no legacy set.
4. Run the focused tests and confirm they pass.

## Task 2: Independent dreadlock color

**Files:**

- Modify: `Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs`
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaCharacterProfileTest.cs`

1. Add failing tests for the skin-linked default, fixed dread colors, skin
   changes, style changes, sanitization, and cloning.
2. Run the focused profile tests and confirm the new API is absent.
3. Add the serializable dread-color enum/property, palette helpers, and
   appearance synchronization.
4. Add a localized swatch selector to the appearance page and rebuild it with
   the other visual selectors.
5. Run profile and client compilation tests.

## Task 3: Lobby selector locks

**Files:**

- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditorLayout.cs`
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`
- Test: `Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs`

1. Add failing layout tests for every requested disabled/enabled boundary.
2. Add policy-backed lock helpers and pass disabled/tooltips to ceremonial,
   advanced, and legacy bracer selectors plus the legacy-set selector.
3. Ensure selector mutations sanitize immediately when status changes.
4. Run the focused client tests.

## Task 4: Anubys and Ronin RSI alignment

**Files:**

- Modify:
  `Resources/Textures/_CMU14/Yautja/masks/pred_mask_elite_anubys.rsi/`
- Modify:
  `Resources/Textures/_CMU14/Yautja/masks/pred_mask_elite_ronin.rsi/`
- Modify:
  `Resources/Textures/_CMU14/Yautja/masks/pred_mask_unique_anubys.rsi/`
- Modify:
  `Resources/Textures/_CMU14/Yautja/masks/pred_mask_unique_ronin.rsi/`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaCharacterProfileTest.cs`

1. Change the existing special-mask expectation to 32x32 and run it to confirm
   both resources currently fail at 32x64.
2. Normalize metadata and crop/reposition item and four-direction equipped PNG
   frames without changing their pixels or prototype mapping.
3. Run resource-loading/profile mask tests and visually inspect the generated
   PNGs.

## Task 5: Clan-cache refresh and fail-closed reads

**Files:**

- Modify: `Content.Server/_CMU14/Yautja/YautjaClanManager.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaRankManager.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaClanInfoEui.cs`
- Modify: `Content.Server/Administration/Commands/YautjaClanCommands.cs`
- Modify other mutation call sites found by exhaustive `InvalidateCache` search.
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankPersistenceTest.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanWorkflowTest.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaWhitelistAccessTest.cs`

1. Replace the existing cache-miss exception expectations with failing tests
   for Blooded/no-special fail-closed results.
2. Add mutation tests that invalidate membership/rank state and require an
   immediately usable refreshed capability snapshot.
3. Implement non-throwing synchronous cached reads and one asynchronous refresh
   path for both clan and derived rank state.
4. Route every rank, membership, whitelist, move, remove, purge, and delete
   mutation through refresh before success is reported.
5. Run rank, clan workflow, whitelist, and predator-role tests.

## Task 6: Verification and launch

1. Run focused `Content.Tests` for `YautjaProfileEditorLayoutTest`.
2. Run focused integration tests for character profile, rank persistence, clan
   workflow, whitelist access, and predator role.
3. Build `Content.Server` and `Content.Client`.
4. Run `git diff --check` and inspect the scoped diff without resetting any
   unrelated user changes.
5. Stop only the previously identified RussianCM server/client processes if
   they still hold build/runtime files, then launch the rebuilt server and
   client with fresh scoped logs.
6. Verify both processes stay running and inspect their logs for prototype,
   cache, connection, and unhandled-exception errors.
