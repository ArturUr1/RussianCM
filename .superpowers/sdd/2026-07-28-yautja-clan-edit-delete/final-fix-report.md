# Final fix report — Yautja clan administration hardening

Status: **GREEN_WITH_WORKSPACE_CONCERNS**

Date: 2026-07-28 (Europe/Moscow).

The fixes were developed and verified in the existing `fix/yautja` checkout because the required clan-manager foundation existed only as dirty/untracked work in that checkout. The repository contained extensive unrelated user changes before this task; those changes were preserved and excluded from this commit.

## Confirmed review findings addressed

1. **Required foundation absent from `HEAD`**
   - Added `YautjaClanAdminCommand` and `YautjaClanManager`.
   - Included the required IoC registration and `YautjaRankManager` integration.
   - Included only the required `YautjaClanResolution`, `YautjaClanView`, and `CanSetAncient(..., bool enabled)` slice from the mixed dirty shared policy file.

2. **Membership assignment/delete TOCTOU**
   - `UpsertYautjaClanMemberAsync` now returns `Task<bool>`.
   - A non-null clan assignment performs an active-clan conditional update inside the same database transaction before the member write.
   - Inactive or missing clan IDs return `false` without writing membership.
   - The conditional update is serialized against the existing transactional soft-delete update, so concurrent delete/assignment cannot leave membership pointing at an inactive clan.
   - Server callers in the committed scope propagate failure before cache invalidation or success logging.

3. **EUI mutation/refresh ordering and false acknowledgements**
   - One operation gate now serializes initial refreshes, manual refreshes, and every admin action.
   - Successful mutations are staged with clan ID, mutation kind, and success status.
   - Mutation metadata/version is published only with a successful fresh clan snapshot.
   - Refresh failure preserves the prior clan list, inspection fields, version, and mutation acknowledgement while retaining the pending mutation.
   - A failed recovery refresh blocks the next mutation.

4. **Create draft state**
   - Create-mode input is captured as a draft.
   - Error and manual refresh states preserve that draft.
   - A fresh `Created` acknowledgement clears it.

5. **Russian locale corruption**
   - Replaced all 48 mojibake values in `admin_clan.ftl` with readable UTF-8 Russian text.

## TDD evidence

### Expected RED

- Client draft tests initially failed to compile with `CS0117` because `YautjaClanAdminMutationKind.Created` did not exist.
  - Command: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdmin"`
  - Exit code: `1`.
- Database/state tests initially failed to compile because the DB upsert still returned `Task` and the state store lacked mutation staging/publication APIs (`CS0815`, `CS1061`).
  - Command: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanMutationPersistenceTest|FullyQualifiedName~YautjaClanAdminStateStoreTest"`
  - Exit code: `1`.
- The pending-success-status regression test then failed to compile because the initial staging API did not preserve the mutation status (`CS1501`).
  - Command: `dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore`
  - Exit code: `1`.
- The exact Russian-locale smoke test failed both readability assertions against the original mojibake resource.
  - Command: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-build --no-restore --filter "FullyQualifiedName=Content.IntegrationTests._CMU14.Yautja.YautjaClanAdminStateStoreTest.RussianClanAdminLocaleIsReadableUtf8"`
  - Exit code: `1`.

### GREEN regressions

- Exact Russian-locale smoke test: passed `1/1`.
- Exact pending-mutation state test: passed `1/1`.
- New database/state regression group: passed `8/8`.
- Exact concurrent delete/assignment invariant test: passed `1/1`.

One earlier combined test invocation was terminated after its outer timeout and is not counted as verification evidence. Its remaining child processes were allowed to finish before any subsequent `dotnet` command; all accepted commands below ran sequentially.

## Final sequential verification

1. `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdmin"`
   - Exit code: `0`
   - Result: failed `0`, passed `9`, skipped `0`, total `9`.

2. `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanMutationPersistenceTest|FullyQualifiedName~YautjaClanAdminStateStoreTest|FullyQualifiedName~YautjaClanWorkflowTest|FullyQualifiedName~YautjaClanPersistenceTest"`
   - Exit code: `0`
   - Result: failed `0`, passed `25`, skipped `0`, total `25`.

3. `dotnet build Content.Server/Content.Server.csproj --no-restore`
   - Exit code: `0`
   - Result: warnings `19`, errors `0`.

4. `dotnet build Content.Client/Content.Client.csproj --no-restore`
   - Exit code: `0`
   - Result: warnings `14`, errors `0`.

5. `git diff --check`
   - Exit code: `0`, no output.

No `restore`, `clean`, parallel `dotnet`, or server launch was performed.

## Intended commit scope

- `.superpowers/sdd/2026-07-28-yautja-clan-edit-delete/final-fix-report.md`
- `Content.Client/_CMU14/Yautja/YautjaClanAdminEditorState.cs`
- `Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminStateStoreTest.cs`
- `Content.IntegrationTests/_CMU14/Yautja/YautjaClanMutationPersistenceTest.cs`
- `Content.Server/Database/ServerDbBase.YautjaClan.cs`
- `Content.Server/Database/ServerDbManager.cs`
- `Content.Server/IoC/ServerContentIoC.cs`
- `Content.Server/_CMU14/Yautja/YautjaClanAdminCommand.cs`
- `Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs`
- `Content.Server/_CMU14/Yautja/YautjaClanAdminStateStore.cs`
- `Content.Server/_CMU14/Yautja/YautjaClanManager.cs`
- `Content.Server/_CMU14/Yautja/YautjaRankManager.cs`
- Required partial foundation from `Content.Shared/_CMU14/Yautja/YautjaClan.cs`
- `Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs`
- `Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminEditorStateTest.cs`
- `Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl`

Commit message: `fix: harden Yautja clan administration`.

## Remaining workspace concerns

- Existing `NU1900` package vulnerability-feed retrieval warnings and unrelated source/analyzer warnings remain; all final commands exited successfully.
- The checkout remains intentionally dirty with many unrelated tracked, untracked, binary, map, locale, test, and submodule changes.
- `Content.Shared/_CMU14/Yautja/YautjaClan.cs` contains unrelated dirty `CanView` and `CanMove` policy edits; they are deliberately excluded from this commit and will remain in the working tree.
- Untracked `Content.Server/Administration/Commands/YautjaClanCommands.cs` is outside this task and is deliberately excluded. It currently awaits the boolean upsert result without handling `false`; that must be addressed if that separate file is later committed.
