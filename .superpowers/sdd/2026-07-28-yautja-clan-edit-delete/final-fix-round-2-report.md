# Final fix round 2 report — Yautja clan admin entry and cache safety

Status: **GREEN_WITH_TEST_QUALITY_NOTES**

Date: 2026-07-28 (Europe/Moscow).

This round addresses exactly the three confirmed re-review findings. It was performed in the existing shared `fix/yautja` checkout. Unrelated dirty files were preserved and excluded.

## Changes

### 1. Committed F7 entry point

- Added the existing dirty `AdminTab.xaml` command button to the commit scope:
  - `Command="yautja_clan_admin"`
  - `Text="{Loc cmu-yautja-clan-admin-open}"`
- Added a runtime client integration test that constructs `AdminTab`, locates the command button, and verifies its localized text.

### 2. Per-user clan cache generations

- Added `YautjaClanCacheVersions`, keyed by `NetUserId`.
- `YautjaClanManager.Resolve` captures the user's generation before its first database await and writes the completed resolution into `_cache` only if the generation is still current.
- `InvalidateCache` increments each user's generation before removing the cached value.
- Added a focused pure regression test that simulates an in-flight capture, invalidation, and stale completion.

### 3. Mutation acknowledgement delivery barrier

- `YautjaClanAdminStateStore` now distinguishes:
  - a successful mutation still awaiting a fresh snapshot;
  - an acknowledgement snapshot still awaiting delivery through `GetNewState`.
- Acknowledgement publication sets a delivery barrier. `StageMutation` rejects another mutation until `GetForDelivery` returns that acknowledgement state.
- `YautjaClanAdminEui.GetNewState` consumes the delivery barrier.
- If an incoming handler recovers a pending mutation, it returns after publishing the recovered acknowledgement instead of executing another mutation in the same handler.
- Added a regression test proving a recovered `Created` acknowledgement cannot be replaced by a subsequent `Updated` mutation before delivery.

## TDD evidence

### Cache and acknowledgement RED

Command:

```powershell
dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore
```

Exit code: `1`.

Expected errors:

- `CS0246`: `YautjaClanCacheVersions` did not exist.
- `CS1061`: `YautjaClanAdminStateStore.GetForDelivery` did not exist.

### Cache and acknowledgement GREEN

Command:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~InvalidatedClanResolutionRejectsStaleInFlightCompletion|FullyQualifiedName~RecoveredAcknowledgementMustBeDeliveredBeforeNextMutationCanStart|FullyQualifiedName~PendingMutationIsAcknowledgedOnlyWithFreshSnapshot"
```

Exit code: `0`.

Result: failed `0`, passed `3`, skipped `0`, total `3`.

### F7 entry RED

The pre-existing dirty button line was temporarily removed after the runtime test was written, so the test exercised the state that would be committed without this fix.

Command:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-build --no-restore --filter "FullyQualifiedName=Content.IntegrationTests._CMU14.Yautja.YautjaClanAdminEntryTest.AdminTabProvidesLocalizedClanAdministrationCommand"
```

Exit code: `1`.

Expected failure:

```text
Assert.That(button, Is.Not.Null)
Expected: not null
But was: null
```

### F7 entry GREEN

After restoring the exact requested XAML hunk, the same test was rerun with compilation.

Command:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName=Content.IntegrationTests._CMU14.Yautja.YautjaClanAdminEntryTest.AdminTabProvidesLocalizedClanAdministrationCommand"
```

Exit code: `0`.

Result: failed `0`, passed `1`, skipped `0`, total `1`.

## Final sequential verification

1. Focused unit tests:

   ```powershell
   dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdmin"
   ```

   Exit code: `0`. Failed `0`, passed `9`, skipped `0`, total `9`.

2. Focused integration tests:

   ```powershell
   dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminEntryTest|FullyQualifiedName~YautjaClanAdminStateStoreTest|FullyQualifiedName=Content.IntegrationTests._CMU14.Yautja.YautjaRankPersistenceTest.InvalidatedClanResolutionRejectsStaleInFlightCompletion|FullyQualifiedName~YautjaClanMutationPersistenceTest|FullyQualifiedName~YautjaClanWorkflowTest|FullyQualifiedName~YautjaClanPersistenceTest"
   ```

   Exit code: `0`. Failed `0`, passed `28`, skipped `0`, total `28`.

3. Server build:

   ```powershell
   dotnet build Content.Server/Content.Server.csproj --no-restore
   ```

   Exit code: `0`. Warnings `19`, errors `0`.

4. Client build:

   ```powershell
   dotnet build Content.Client/Content.Client.csproj --no-restore
   ```

   Exit code: `0`. Warnings `14`, errors `0`.

5. Diff check:

   ```powershell
   git diff --check
   ```

   Exit code: `0`, no output.

All accepted commands ran sequentially. No `restore`, `clean`, parallel `dotnet`, or server launch was performed.

## Test-quality notes

- An exploratory broad filter containing all of `YautjaRankPersistenceTest` exited `1`: two pre-existing database tests call `PoolManager.GetServerClient()` without `Connected = true`, then access `pair.Player`. Both failed with `InvalidOperationException: Nullable object must have a value` before reaching production code. They were not changed because this round is limited to the three re-review findings. The new cache-generation test is included by exact fully-qualified name in the green integration command.
- Per instruction, the minor overlap in existing database concurrency coverage was not changed. The production transaction/row-lock invariant and its regression coverage were already verified in round 1.
- Existing `NU1900` vulnerability-feed retrieval warnings and unrelated source/analyzer warnings remain.

## Intended commit scope

- `.superpowers/sdd/2026-07-28-yautja-clan-edit-delete/final-fix-round-2-report.md`
- `Content.Client/Administration/UI/Tabs/AdminTab/AdminTab.xaml`
- `Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminEntryTest.cs`
- `Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminStateStoreTest.cs`
- `Content.IntegrationTests/_CMU14/Yautja/YautjaRankPersistenceTest.cs`
- `Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs`
- `Content.Server/_CMU14/Yautja/YautjaClanAdminStateStore.cs`
- `Content.Server/_CMU14/Yautja/YautjaClanManager.cs`

Commit message: `fix: complete Yautja clan admin entry and cache safety`.

## Remaining workspace state

The checkout remains intentionally dirty with unrelated tracked, untracked, binary, map, locale, test, and submodule work. None of it is part of this round.
