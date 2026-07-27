# Yautja Clan Editing and Deletion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add administrator-only editing and soft deletion of Yautja clans to the existing F7 EUI.

**Architecture:** The database layer owns transactional clan mutations and returns the detached player IDs after deletion. Shared code owns validation and the serialized EUI contract; the server validates permissions, calls the database asynchronously, invalidates caches, and publishes mutation metadata. A focused client editor-state object preserves drafts and lets the existing window switch between create/edit modes and reset after deletion.

**Tech Stack:** C#/.NET, RobustToolbox EUI and controls, Entity Framework Core, SQLite/PostgreSQL-compatible EF operations, NUnit, Fluent localization.

## Global Constraints

- Deletion is soft: set `YautjaClan.Active = false`; do not physically delete clan rows.
- In the same database transaction, set `ClanId = null` for every member of the deleted clan.
- Preserve member rank, permissions, honor, and `IsLegacy`.
- Require `AdminFlags.Admin` for both new operations.
- Trim name, description, and color; require non-empty name and description.
- Use `#ffffff` for an empty color; every non-empty color must match `#RRGGBB`.
- Do not add a migration: `Active` and nullable `ClanId` already exist.
- Do not perform synchronous database waits from `GetNewState()` or the server tick.
- Add both English and Russian UI/status text.
- Preserve unrelated working-tree changes; stage only the exact files listed by each task and inspect `git diff --cached` before every commit.

---

## File Structure

- `Content.Server.Database/YautjaClanModel.cs`: database mutation result record.
- `Content.Server/Database/ServerDbBase.YautjaClan.cs`: transactional update and soft-delete implementations.
- `Content.Server/Database/ServerDbManager.cs`: public database interface and metrics/thread-pool wrappers.
- `Content.IntegrationTests/_CMU14/Yautja/YautjaClanMutationPersistenceTest.cs`: end-to-end SQLite database contract tests.
- `Content.Shared/_CMU14/Yautja/YautjaClanAdminValidation.cs`: pure normalization and color validation.
- `Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs`: update/delete messages and mutation metadata in the EUI state.
- `Content.Tests/Shared/_CMU14/Yautja/YautjaClanAdminValidationTest.cs`: validation tests.
- `Content.Server/_CMU14/Yautja/YautjaClanAdminStateStore.cs`: initialize the expanded EUI state.
- `Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs`: admin authorization, mutation orchestration, cache invalidation, status, and audit logs.
- `Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminStateStoreTest.cs`: update the cached-state regression fixture.
- `Content.Client/_CMU14/Yautja/YautjaClanAdminEditorState.cs`: edit-mode and draft reconciliation logic.
- `Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminEditorStateTest.cs`: client state tests.
- `Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs`: form mode, row actions, and confirmation window.
- `Content.Client/_CMU14/Yautja/YautjaClanAdminEui.cs`: send update/delete messages.
- `Resources/Locale/en-US/_CMU14/yautja/admin.ftl`: English strings.
- `Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl`: Russian strings.

### Task 1: Transactional database mutations

**Files:**

- Modify: `Content.Server.Database/YautjaClanModel.cs:54-61`
- Modify: `Content.Server/Database/ServerDbBase.YautjaClan.cs:47-104`
- Modify: `Content.Server/Database/ServerDbManager.cs:208-215`
- Modify: `Content.Server/Database/ServerDbManager.cs:808-842`
- Create: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanMutationPersistenceTest.cs`

**Interfaces:**

- Consumes: existing `CreateYautjaClanAsync`, `GetYautjaClanAsync`, `GetYautjaClansAsync`, `GetYautjaClanMemberAsync`, and `UpsertYautjaClanMemberAsync`.
- Produces: `Task<bool> UpdateYautjaClanAsync(int clanId, string name, string description, string color)`.
- Produces: `Task<YautjaClanDeleteResult> DeactivateYautjaClanAsync(int clanId)`.
- Produces: `YautjaClanDeleteResult(bool Succeeded, List<Guid> DetachedPlayers)`.

- [ ] **Step 1: Write failing SQLite contract tests**

Create `YautjaClanMutationPersistenceTest.cs` with three NUnit tests. The update test creates a clan with honor `42`, updates the three editable fields, and asserts that honor and `Active` are unchanged:

```csharp
[Test]
public async Task UpdateChangesEditableFieldsOnly()
{
    await using var pair = await PoolManager.GetServerClient();
    var db = pair.Server.ResolveDependency<IServerDbManager>();
    var clanId = await db.CreateYautjaClanAsync("Old", "Old description", 42, "#111111");

    var updated = await db.UpdateYautjaClanAsync(clanId, "New", "New description", "#AABBCC");
    var clan = await db.GetYautjaClanAsync(clanId);

    Assert.Multiple(() =>
    {
        Assert.That(updated, Is.True);
        Assert.That(clan, Is.Not.Null);
        Assert.That(clan!.Name, Is.EqualTo("New"));
        Assert.That(clan.Description, Is.EqualTo("New description"));
        Assert.That(clan.Color, Is.EqualTo("#AABBCC"));
        Assert.That(clan.Honor, Is.EqualTo(42));
        Assert.That(clan.Active, Is.True);
    });

    await pair.CleanReturnAsync();
}
```

Add an inactive/missing guard test:

```csharp
[Test]
public async Task UpdateRejectsInactiveAndMissingClans()
{
    await using var pair = await PoolManager.GetServerClient();
    var db = pair.Server.ResolveDependency<IServerDbManager>();
    var inactiveId = await db.CreateYautjaClanAsync(
        "Inactive",
        "Inactive description",
        0,
        "#111111",
        active: false);

    var inactiveUpdated =
        await db.UpdateYautjaClanAsync(inactiveId, "Changed", "Changed", "#222222");
    var missingUpdated =
        await db.UpdateYautjaClanAsync(int.MaxValue, "Missing", "Missing", "#333333");

    Assert.Multiple(() =>
    {
        Assert.That(inactiveUpdated, Is.False);
        Assert.That(missingUpdated, Is.False);
    });

    await pair.CleanReturnAsync();
}
```

The deletion test uses `pair.Player!.UserId`, creates one membership with non-default persistent data, calls soft delete twice, and asserts the exact result:

```csharp
[Test]
public async Task DeleteDeactivatesClanAndDetachesMemberWithoutChangingPersistentData()
{
    await using var pair = await PoolManager.GetServerClient();
    var db = pair.Server.ResolveDependency<IServerDbManager>();
    var playerId = pair.Player!.UserId.UserId;
    var clanId = await db.CreateYautjaClanAsync("Delete me", "Deletion test", 7, "#123456");
    await db.UpsertYautjaClanMemberAsync(new YautjaClanMemberRecord(
        playerId,
        clanId,
        (int) YautjaRank.Elder,
        (int) (YautjaClanPermission.UserModify | YautjaClanPermission.UserView),
        13,
        true));

    var first = await db.DeactivateYautjaClanAsync(clanId);
    var second = await db.DeactivateYautjaClanAsync(clanId);
    var clan = await db.GetYautjaClanAsync(clanId);
    var member = await db.GetYautjaClanMemberAsync(playerId);
    var activeClans = await db.GetYautjaClansAsync();

    Assert.Multiple(() =>
    {
        Assert.That(first.Succeeded, Is.True);
        Assert.That(first.DetachedPlayers, Is.EqualTo(new[] { playerId }));
        Assert.That(second.Succeeded, Is.False);
        Assert.That(second.DetachedPlayers, Is.Empty);
        Assert.That(clan!.Active, Is.False);
        Assert.That(activeClans.All(entry => entry.Id != clanId), Is.True);
        Assert.That(member!.ClanId, Is.Null);
        Assert.That(member.Rank, Is.EqualTo((int) YautjaRank.Elder));
        Assert.That(member.Permissions,
            Is.EqualTo((int) (YautjaClanPermission.UserModify | YautjaClanPermission.UserView)));
        Assert.That(member.Honor, Is.EqualTo(13));
        Assert.That(member.IsLegacy, Is.True);
    });

    await pair.CleanReturnAsync();
}
```

- [ ] **Step 2: Run the tests and verify the red phase**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~YautjaClanMutationPersistenceTest
```

Expected: compilation fails because `UpdateYautjaClanAsync`, `DeactivateYautjaClanAsync`, and `YautjaClanDeleteResult` do not exist.

- [ ] **Step 3: Add the database result and public contract**

Append to `YautjaClanModel.cs`:

```csharp
public sealed record YautjaClanDeleteResult(
    bool Succeeded,
    List<Guid> DetachedPlayers);
```

Add both method signatures to `IServerDbManager`:

```csharp
Task<bool> UpdateYautjaClanAsync(int clanId, string name, string description, string color);
Task<YautjaClanDeleteResult> DeactivateYautjaClanAsync(int clanId);
```

Add these wrappers to `ServerDbManager`:

```csharp
public Task<bool> UpdateYautjaClanAsync(
    int clanId,
    string name,
    string description,
    string color)
{
    DbWriteOpsMetric.Inc();
    return RunDbCommand(() => _db.UpdateYautjaClanAsync(clanId, name, description, color));
}

public Task<YautjaClanDeleteResult> DeactivateYautjaClanAsync(int clanId)
{
    DbWriteOpsMetric.Inc();
    return RunDbCommand(() => _db.DeactivateYautjaClanAsync(clanId));
}
```

- [ ] **Step 4: Implement update and atomic soft deletion**

Add to `ServerDbBase.YautjaClan.cs`:

```csharp
public async Task<bool> UpdateYautjaClanAsync(
    int clanId,
    string name,
    string description,
    string color)
{
    await using var db = await GetDb();
    var clan = await db.DbContext.YautjaClans
        .SingleOrDefaultAsync(entry => entry.Id == clanId && entry.Active);
    if (clan == null)
        return false;

    clan.Name = name;
    clan.Description = description;
    clan.Color = color;
    await db.DbContext.SaveChangesAsync();
    return true;
}

public async Task<YautjaClanDeleteResult> DeactivateYautjaClanAsync(int clanId)
{
    await using var db = await GetDb();
    await using var transaction = await db.DbContext.Database.BeginTransactionAsync();

    var clan = await db.DbContext.YautjaClans
        .SingleOrDefaultAsync(entry => entry.Id == clanId && entry.Active);
    if (clan == null)
        return new(false, []);

    var members = await db.DbContext.YautjaClanMembers
        .Where(entry => entry.ClanId == clanId)
        .ToListAsync();
    var detachedPlayers = members
        .Select(entry => entry.PlayerUserId)
        .ToList();

    clan.Active = false;
    foreach (var member in members)
        member.ClanId = null;

    await db.DbContext.SaveChangesAsync();
    await transaction.CommitAsync();
    return new(true, detachedPlayers);
}
```

- [ ] **Step 5: Run the database tests**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~YautjaClanMutationPersistenceTest
```

Expected: both tests pass.

- [ ] **Step 6: Commit the database slice**

```powershell
git add -- Content.Server.Database/YautjaClanModel.cs Content.Server/Database/ServerDbBase.YautjaClan.cs Content.Server/Database/ServerDbManager.cs Content.IntegrationTests/_CMU14/Yautja/YautjaClanMutationPersistenceTest.cs
git diff --cached --check
git diff --cached --stat
git commit -m "feat: add transactional Yautja clan mutations"
```

### Task 2: Shared validation and EUI contract

**Files:**

- Create: `Content.Shared/_CMU14/Yautja/YautjaClanAdminValidation.cs`
- Modify: `Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs:8-99`
- Create: `Content.Tests/Shared/_CMU14/Yautja/YautjaClanAdminValidationTest.cs`

**Interfaces:**

- Produces: `YautjaClanAdminValidation.TryNormalize(string name, string description, string color, out YautjaClanAdminFields fields, out YautjaClanAdminValidationError error)`.
- Produces: `YautjaClanAdminUpdateClanMessage` and `YautjaClanAdminDeleteClanMessage`.
- Produces: `YautjaClanAdminMutationKind` plus `ClanMutationVersion`, `LastMutatedClanId`, and `LastMutationKind` state properties.

- [ ] **Step 1: Write failing validation tests**

Cover whitespace normalization, the default color, a valid uppercase hex color, empty name/description, and invalid colors:

```csharp
[Test]
public void EmptyColorUsesWhiteAndTextIsTrimmed()
{
    var valid = YautjaClanAdminValidation.TryNormalize(
        "  Clan  ",
        "  Description  ",
        "  ",
        out var fields,
        out var error);

    Assert.Multiple(() =>
    {
        Assert.That(valid, Is.True);
        Assert.That(error, Is.EqualTo(YautjaClanAdminValidationError.None));
        Assert.That(fields, Is.EqualTo(new YautjaClanAdminFields("Clan", "Description", "#ffffff")));
    });
}

[TestCase("", "Description", "#ffffff", YautjaClanAdminValidationError.MissingNameOrDescription)]
[TestCase("Clan", "", "#ffffff", YautjaClanAdminValidationError.MissingNameOrDescription)]
[TestCase("Clan", "Description", "red", YautjaClanAdminValidationError.InvalidColor)]
[TestCase("Clan", "Description", "#12345G", YautjaClanAdminValidationError.InvalidColor)]
public void InvalidFieldsAreRejected(
    string name,
    string description,
    string color,
    YautjaClanAdminValidationError expected)
{
    Assert.That(
        YautjaClanAdminValidation.TryNormalize(name, description, color, out _, out var error),
        Is.False);
    Assert.That(error, Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run validation tests and verify the red phase**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaClanAdminValidationTest
```

Expected: compilation fails because the validation types do not exist.

- [ ] **Step 3: Implement the pure validator**

Create the shared file with:

```csharp
namespace Content.Shared._CMU14.Yautja;

public enum YautjaClanAdminValidationError : byte
{
    None,
    MissingNameOrDescription,
    InvalidColor,
}

public readonly record struct YautjaClanAdminFields(
    string Name,
    string Description,
    string Color);

public static class YautjaClanAdminValidation
{
    public static bool TryNormalize(
        string name,
        string description,
        string color,
        out YautjaClanAdminFields fields,
        out YautjaClanAdminValidationError error)
    {
        var normalizedName = name.Trim();
        var normalizedDescription = description.Trim();
        var normalizedColor = string.IsNullOrWhiteSpace(color) ? "#ffffff" : color.Trim();

        if (normalizedName.Length == 0 || normalizedDescription.Length == 0)
        {
            fields = default;
            error = YautjaClanAdminValidationError.MissingNameOrDescription;
            return false;
        }

        if (normalizedColor.Length != 7 ||
            normalizedColor[0] != '#' ||
            !normalizedColor[1..].All(IsHexDigit))
        {
            fields = default;
            error = YautjaClanAdminValidationError.InvalidColor;
            return false;
        }

        fields = new(normalizedName, normalizedDescription, normalizedColor);
        error = YautjaClanAdminValidationError.None;
        return true;
    }

    private static bool IsHexDigit(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }
}
```

Add `using System.Linq;` because the range is checked with `All`.

- [ ] **Step 4: Add serialized messages and mutation metadata**

Define:

```csharp
[Serializable, NetSerializable]
public enum YautjaClanAdminMutationKind : byte
{
    None,
    Updated,
    Deleted,
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminUpdateClanMessage(
    int clanId,
    string name,
    string description,
    string color) : EuiMessageBase
{
    public int ClanId { get; } = clanId;
    public string Name { get; } = name;
    public string Description { get; } = description;
    public string Color { get; } = color;
}

[Serializable, NetSerializable]
public sealed class YautjaClanAdminDeleteClanMessage(int clanId) : EuiMessageBase
{
    public int ClanId { get; } = clanId;
}
```

Expand `YautjaClanAdminEuiState` with explicit constructor arguments and read-only properties:

```csharp
public YautjaClanAdminEuiState(
    List<YautjaClanAdminClanState> clans,
    string inspectedPlayer,
    string inspectedSummary,
    string statusMessage,
    long clanMutationVersion,
    int? lastMutatedClanId,
    YautjaClanAdminMutationKind lastMutationKind)
{
    Clans = clans;
    InspectedPlayer = inspectedPlayer;
    InspectedSummary = inspectedSummary;
    StatusMessage = statusMessage;
    ClanMutationVersion = clanMutationVersion;
    LastMutatedClanId = lastMutatedClanId;
    LastMutationKind = lastMutationKind;
}

public long ClanMutationVersion { get; }
public int? LastMutatedClanId { get; }
public YautjaClanAdminMutationKind LastMutationKind { get; }
```

- [ ] **Step 5: Run the validation tests**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaClanAdminValidationTest
```

Expected: all validation cases pass.

- [ ] **Step 6: Commit the shared contract**

```powershell
git add -- Content.Shared/_CMU14/Yautja/YautjaClanAdminValidation.cs Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs Content.Tests/Shared/_CMU14/Yautja/YautjaClanAdminValidationTest.cs
git diff --cached --check
git diff --cached --stat
git commit -m "feat: define Yautja clan mutation contract"
```

### Task 3: Server mutation orchestration

**Files:**

- Modify: `Content.Server/_CMU14/Yautja/YautjaClanAdminStateStore.cs:11-24`
- Modify: `Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs:20-214`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminStateStoreTest.cs:8-21`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/admin.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl`

**Interfaces:**

- Consumes: database methods from Task 1 and shared validator/messages from Task 2.
- Produces: successful update/delete status, monotonically increasing mutation version, detached-player cache invalidation, and active-clan membership enforcement.

- [ ] **Step 1: Extend the cached-state regression test**

Construct the expanded state with mutation metadata and assert the exact same instance is returned:

```csharp
var state = new YautjaClanAdminEuiState(
    [],
    "player",
    "summary",
    "status",
    4,
    12,
    YautjaClanAdminMutationKind.Updated);
```

- [ ] **Step 2: Run the state-store test and verify the red phase**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~YautjaClanAdminStateStoreTest
```

Expected: compilation fails until the state store and EUI state construction sites supply the new metadata.

- [ ] **Step 3: Publish mutation metadata from every state refresh**

Initialize `YautjaClanAdminStateStore` with mutation version `0`, null clan ID, and `None`. Add these EUI fields:

```csharp
private long _clanMutationVersion;
private int? _lastMutatedClanId;
private YautjaClanAdminMutationKind _lastMutationKind;
```

Pass the fields into both normal and error-state `YautjaClanAdminEuiState` construction. The error path reuses the previous clan list but publishes the current mutation fields; it never increments the version itself. This still reports a database mutation that succeeded immediately before a list-refresh failure.

- [ ] **Step 4: Handle update and delete messages**

Add switch cases before the player operations:

```csharp
case YautjaClanAdminUpdateClanMessage update:
    await UpdateClan(update);
    break;
case YautjaClanAdminDeleteClanMessage delete:
    await DeleteClan(delete);
    break;
```

`UpdateClan` contains:

```csharp
private async Task UpdateClan(YautjaClanAdminUpdateClanMessage message)
{
    if (!YautjaClanAdminValidation.TryNormalize(
            message.Name,
            message.Description,
            message.Color,
            out var fields,
            out var error))
    {
        _statusMessage = error == YautjaClanAdminValidationError.InvalidColor
            ? Loc.GetString("cmu-yautja-clan-admin-invalid-color")
            : Loc.GetString("cmu-yautja-clan-admin-invalid-clan");
        return;
    }

    if (!await _db.UpdateYautjaClanAsync(
            message.ClanId,
            fields.Name,
            fields.Description,
            fields.Color))
    {
        _statusMessage = Loc.GetString("cmu-yautja-clan-admin-clan-not-found");
        return;
    }

    _clanMutationVersion++;
    _lastMutatedClanId = message.ClanId;
    _lastMutationKind = YautjaClanAdminMutationKind.Updated;
    _statusMessage = Loc.GetString("cmu-yautja-clan-admin-updated", ("id", message.ClanId));
    _adminLog.Add(
        LogType.AdminCommands,
        LogImpact.Medium,
        $"{Player.Name} updated Yautja clan {message.ClanId} ({fields.Name}).");
}
```

`DeleteClan` contains:

```csharp
private async Task DeleteClan(YautjaClanAdminDeleteClanMessage message)
{
    var result = await _db.DeactivateYautjaClanAsync(message.ClanId);
    if (!result.Succeeded)
    {
        _statusMessage = Loc.GetString("cmu-yautja-clan-admin-clan-not-found");
        return;
    }

    foreach (var detachedPlayer in result.DetachedPlayers)
    {
        var userId = new NetUserId(detachedPlayer);
        _clanManager.InvalidateCache(userId);
        _rankManager.InvalidateCached(userId);
    }

    _clanMutationVersion++;
    _lastMutatedClanId = message.ClanId;
    _lastMutationKind = YautjaClanAdminMutationKind.Deleted;
    _statusMessage = Loc.GetString(
        "cmu-yautja-clan-admin-deleted",
        ("id", message.ClanId),
        ("members", result.DetachedPlayers.Count));
    _adminLog.Add(
        LogType.AdminCommands,
        LogImpact.Medium,
        $"{Player.Name} deleted Yautja clan {message.ClanId} and detached {result.DetachedPlayers.Count} members.");
}
```

- [ ] **Step 5: Reject inactive clans during membership assignment**

Replace the current null-only lookup with:

```csharp
else if (int.TryParse(message.ClanId, out var parsed) &&
         await _db.GetYautjaClanAsync(parsed) is { Active: true })
{
    clanId = parsed;
}
```

This closes the path that could assign a player to a known soft-deleted ID.

- [ ] **Step 6: Add server and UI localization**

Add to the English locale:

```text
cmu-yautja-clan-admin-invalid-color = Clan color must use the #RRGGBB format.
cmu-yautja-clan-admin-clan-not-found = The clan does not exist or has already been deleted.
cmu-yautja-clan-admin-updated = Clan #{$id} updated.
cmu-yautja-clan-admin-deleted = Clan #{$id} deleted; {$members} members detached.
```

Add to the Russian locale:

```text
cmu-yautja-clan-admin-invalid-color = Цвет клана должен быть в формате #RRGGBB.
cmu-yautja-clan-admin-clan-not-found = Клан не существует или уже удалён.
cmu-yautja-clan-admin-updated = Клан №{$id} обновлён.
cmu-yautja-clan-admin-deleted = Клан №{$id} удалён; отвязано участников: {$members}.
```

- [ ] **Step 7: Run server-focused tests**

Run:

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminStateStoreTest|FullyQualifiedName~YautjaClanMutationPersistenceTest"
```

Expected: all state-store and database mutation tests pass.

- [ ] **Step 8: Commit the server slice**

```powershell
git add -- Content.Server/_CMU14/Yautja/YautjaClanAdminStateStore.cs Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminStateStoreTest.cs Resources/Locale/en-US/_CMU14/yautja/admin.ftl Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl
git diff --cached --check
git diff --cached --stat
git commit -m "feat: handle Yautja clan editing and deletion"
```

### Task 4: Client edit mode and deletion confirmation

**Files:**

- Create: `Content.Client/_CMU14/Yautja/YautjaClanAdminEditorState.cs`
- Create: `Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminEditorStateTest.cs`
- Modify: `Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs:10-159`
- Modify: `Content.Client/_CMU14/Yautja/YautjaClanAdminEui.cs:8-68`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/admin.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl`

**Interfaces:**

- Consumes: mutation metadata and messages from Task 2.
- Produces: reusable create/edit form, row actions, standard confirmation dialog, draft preservation after errors, and automatic reset after deletion.

- [ ] **Step 1: Write failing editor-state tests**

Create a test that begins editing, captures a dirty draft, applies an error refresh with an unchanged mutation version, applies a successful update with a newer version, and then applies a successful deletion:

```csharp
[Test]
public void DraftSurvivesErrorsThenSynchronizesOrResetsOnSuccessfulMutation()
{
    var editor = new YautjaClanAdminEditorState();
    var original = Clan(3, "Original", "Original description", "#111111");
    editor.ApplyState(State(0, null, YautjaClanAdminMutationKind.None, original));
    editor.BeginEdit(original);
    editor.CaptureDraft("Draft", "Draft description", "#222222");

    editor.ApplyState(State(0, null, YautjaClanAdminMutationKind.None, original));
    Assert.That(editor.Name, Is.EqualTo("Draft"));

    var updated = Clan(3, "Saved", "Saved description", "#333333");
    editor.ApplyState(State(1, 3, YautjaClanAdminMutationKind.Updated, updated));
    Assert.Multiple(() =>
    {
        Assert.That(editor.EditingClanId, Is.EqualTo(3));
        Assert.That(editor.Name, Is.EqualTo("Saved"));
        Assert.That(editor.Description, Is.EqualTo("Saved description"));
        Assert.That(editor.Color, Is.EqualTo("#333333"));
    });

    editor.ApplyState(State(2, 3, YautjaClanAdminMutationKind.Deleted));
    Assert.Multiple(() =>
    {
        Assert.That(editor.EditingClanId, Is.Null);
        Assert.That(editor.Name, Is.Empty);
        Assert.That(editor.Description, Is.Empty);
        Assert.That(editor.Color, Is.Empty);
    });
}
```

The test helpers construct complete `YautjaClanAdminClanState` and `YautjaClanAdminEuiState` objects with empty inspection/status strings.

```csharp
private static YautjaClanAdminClanState Clan(
    int id,
    string name,
    string description,
    string color)
{
    return new(id, name, description, 0, color, 0);
}

private static YautjaClanAdminEuiState State(
    long version,
    int? clanId,
    YautjaClanAdminMutationKind kind,
    params YautjaClanAdminClanState[] clans)
{
    return new(clans.ToList(), "", "", "", version, clanId, kind);
}
```

- [ ] **Step 2: Run the editor-state test and verify the red phase**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaClanAdminEditorStateTest
```

Expected: compilation fails because `YautjaClanAdminEditorState` does not exist.

- [ ] **Step 3: Implement the focused editor-state object**

The class starts with:

```csharp
using System;
using System.Linq;
using Content.Shared._CMU14.Yautja;

namespace Content.Client._CMU14.Yautja;

public sealed class YautjaClanAdminEditorState
{
    private long _lastMutationVersion;

    public int? EditingClanId { get; private set; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public string Color { get; private set; } = "";
    public bool IsEditing => EditingClanId != null;

    public void BeginEdit(YautjaClanAdminClanState clan)
    {
        EditingClanId = clan.Id;
        Name = clan.Name;
        Description = clan.Description;
        Color = clan.Color;
    }

    public void CaptureDraft(string name, string description, string color)
    {
        if (!IsEditing)
            return;

        Name = name;
        Description = description;
        Color = color;
    }

    public void ApplyState(YautjaClanAdminEuiState state)
    {
        var isNewMutation = state.ClanMutationVersion > _lastMutationVersion;
        _lastMutationVersion = Math.Max(_lastMutationVersion, state.ClanMutationVersion);

        if (EditingClanId is not { } editingClanId)
            return;

        if (isNewMutation &&
            state.LastMutatedClanId == editingClanId &&
            state.LastMutationKind == YautjaClanAdminMutationKind.Deleted)
        {
            Cancel();
            return;
        }

        var clan = state.Clans.FirstOrDefault(entry => entry.Id == editingClanId);
        if (clan == null)
        {
            Cancel();
            return;
        }

        if (isNewMutation &&
            state.LastMutatedClanId == editingClanId &&
            state.LastMutationKind == YautjaClanAdminMutationKind.Updated)
        {
            BeginEdit(clan);
        }
    }

    public void Cancel()
    {
        EditingClanId = null;
        Name = "";
        Description = "";
        Color = "";
    }
}
```

- [ ] **Step 4: Convert the form to create/edit mode**

Add stored controls for the header, submit button, and cancel button. The submit handler sends `OnUpdateClan` when `EditingClanId` is present; otherwise it sends `OnCreateClan`. `SyncEditorControls()` copies model values to the three `LineEdit` controls, switches the localized header/button text, and shows the cancel button only in edit mode.

Before applying a server state while editing, call `CaptureDraft` with the current text boxes. Then call `ApplyState` and `SyncEditorControls`.

Use these event signatures and submit branch:

```csharp
public event Action<int, string, string, string>? OnUpdateClan;
public event Action<int>? OnDeleteClan;

private void SubmitClan()
{
    if (_editor.EditingClanId is { } clanId)
    {
        OnUpdateClan?.Invoke(clanId, _clanName.Text, _clanDescription.Text, _clanColor.Text);
        return;
    }

    OnCreateClan?.Invoke(_clanName.Text, _clanDescription.Text, _clanColor.Text);
}
```

The cancel button calls `_editor.Cancel()` followed by `SyncEditorControls()`.

- [ ] **Step 5: Add row actions and confirmation**

Replace the label-only row with a horizontal `BoxContainer` containing the expanding label and localized edit/delete buttons. Edit calls `BeginEdit(clan)` and synchronizes the controls.

Delete uses `Content.Client._RMC14.UserInterface.ConfirmationWindow`. Keep a nullable `_deleteConfirmation`, close any prior instance, call `Setup` with localized title/text/accept/deny strings, close on deny, and on accept invoke `OnDeleteClan(clan.Id)` before closing. Close the confirmation window from `Dispose(bool disposing)`.

Implement the confirmation method as:

```csharp
private void OpenDeleteConfirmation(YautjaClanAdminClanState clan)
{
    _deleteConfirmation?.Close();

    var confirmation = new ConfirmationWindow();
    _deleteConfirmation = confirmation;
    confirmation.Setup(
        Loc.GetString("cmu-yautja-clan-admin-delete-title"),
        Loc.GetString("cmu-yautja-clan-admin-delete-text", ("name", clan.Name)),
        Loc.GetString("cmu-yautja-clan-admin-delete-accept"),
        Loc.GetString("cmu-yautja-clan-admin-delete-deny"));
    confirmation.OnClose += () =>
    {
        if (_deleteConfirmation == confirmation)
            _deleteConfirmation = null;
    };
    confirmation.DenyButton.OnPressed += _ => confirmation.Close();
    confirmation.AcceptButton.OnPressed += _ =>
    {
        OnDeleteClan?.Invoke(clan.Id);
        confirmation.Close();
    };
    confirmation.OpenCentered();
}
```

- [ ] **Step 6: Wire the client EUI messages**

Subscribe and unsubscribe:

```csharp
_window.OnUpdateClan += OnUpdateClan;
_window.OnDeleteClan += OnDeleteClan;
```

Send:

```csharp
private void OnUpdateClan(int clanId, string name, string description, string color)
{
    SendMessage(new YautjaClanAdminUpdateClanMessage(clanId, name, description, color));
}

private void OnDeleteClan(int clanId)
{
    SendMessage(new YautjaClanAdminDeleteClanMessage(clanId));
}
```

- [ ] **Step 7: Add client localization**

Add to the English locale:

```text
cmu-yautja-clan-admin-edit-header = Edit clan
cmu-yautja-clan-admin-save = Save changes
cmu-yautja-clan-admin-cancel = Cancel
cmu-yautja-clan-admin-edit = Edit
cmu-yautja-clan-admin-delete = Delete
cmu-yautja-clan-admin-delete-title = Delete clan
cmu-yautja-clan-admin-delete-text = Delete clan "{$name}" and detach all of its members?
cmu-yautja-clan-admin-delete-accept = Delete
cmu-yautja-clan-admin-delete-deny = Cancel
```

Add to the Russian locale:

```text
cmu-yautja-clan-admin-edit-header = Редактирование клана
cmu-yautja-clan-admin-save = Сохранить изменения
cmu-yautja-clan-admin-cancel = Отмена
cmu-yautja-clan-admin-edit = Редактировать
cmu-yautja-clan-admin-delete = Удалить
cmu-yautja-clan-admin-delete-title = Удаление клана
cmu-yautja-clan-admin-delete-text = Удалить клан «{$name}» и отвязать всех его участников?
cmu-yautja-clan-admin-delete-accept = Удалить
cmu-yautja-clan-admin-delete-deny = Отмена
```

- [ ] **Step 8: Run client tests**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminEditorStateTest|FullyQualifiedName~YautjaClanAdminValidationTest"
```

Expected: all editor-state and validation tests pass.

- [ ] **Step 9: Commit the client slice**

```powershell
git add -- Content.Client/_CMU14/Yautja/YautjaClanAdminEditorState.cs Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminEditorStateTest.cs Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs Content.Client/_CMU14/Yautja/YautjaClanAdminEui.cs Resources/Locale/en-US/_CMU14/yautja/admin.ftl Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl
git diff --cached --check
git diff --cached --stat
git commit -m "feat: add Yautja clan edit and delete controls"
```

### Task 5: Full verification and review

**Files:**

- Review only: every file listed in Tasks 1-4.

**Interfaces:**

- Consumes: complete database, server, shared, and client slices.
- Produces: evidence that the approved acceptance criteria are satisfied without regressions.

- [ ] **Step 1: Run focused unit tests**

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdmin"
```

Expected: all validation and editor-state tests pass.

- [ ] **Step 2: Run focused integration tests**

```powershell
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanMutationPersistenceTest|FullyQualifiedName~YautjaClanAdminStateStoreTest|FullyQualifiedName~YautjaClanWorkflowTest|FullyQualifiedName~YautjaClanPersistenceTest"
```

Expected: all clan persistence, workflow, mutation, and cached-state tests pass.

- [ ] **Step 3: Build server and client**

```powershell
dotnet build Content.Server/Content.Server.csproj --no-restore
dotnet build Content.Client/Content.Client.csproj --no-restore
```

Expected: both builds exit with code `0`; existing unrelated warnings may remain, but no new warning is accepted from the changed files.

- [ ] **Step 4: Inspect the final diff**

```powershell
git status --short
git diff --check HEAD
git diff HEAD -- Content.Server.Database/YautjaClanModel.cs Content.Server/Database/ServerDbBase.YautjaClan.cs Content.Server/Database/ServerDbManager.cs Content.Shared/_CMU14/Yautja/YautjaClanAdminValidation.cs Content.Shared/_CMU14/Yautja/YautjaClanAdminEuiState.cs Content.Server/_CMU14/Yautja/YautjaClanAdminStateStore.cs Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs Content.Client/_CMU14/Yautja/YautjaClanAdminEditorState.cs Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs Content.Client/_CMU14/Yautja/YautjaClanAdminEui.cs Resources/Locale/en-US/_CMU14/yautja/admin.ftl Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl
```

Verify explicitly:

- only editable clan fields change during update;
- delete is soft and transactional;
- detached memberships preserve all other fields;
- inactive clan IDs are rejected by membership assignment;
- mutation metadata increments only on success;
- client error refreshes preserve the draft;
- confirmation is required before sending delete;
- every new visible/status string exists in both locales;
- no synchronous database wait was added.

- [ ] **Step 5: Record verification evidence**

Capture the exact test counts, build exit codes, and any pre-existing warnings in the final handoff. If a test or build fails, do not claim completion; diagnose it with `superpowers:systematic-debugging`, fix it through a new red/green test cycle, and rerun all commands in this task.
