# Yautja Console, Thermal Visor, and Xeno Health Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Require `AdminFlags.Clans` for every Yautja console command, verify the existing thermal wall-vision path, and hide health information for dead xenonids in the Yautja mask HUD.

**Architecture:** Keep command authorization in the existing `AdminCommandAttribute` registry and use one reflection-based test to cover the complete Yautja command inventory. Keep thermal vision server-authoritative through the linked mask visor glasses and client overlay. Make the xeno health-state resolver return no icon for `MobState.Dead`, which removes dead-xeno HP information from every HUD that consumes that resolver, including the Yautja mask.

**Tech Stack:** C#/.NET 10, RobustToolbox entity systems, NUnit unit/integration tests, PowerShell build and test commands.

## Global Constraints

- Use the existing `AdminFlags.Clans` value; do not introduce a second permission flag.
- Keep EUI permission checks and linked visor validation server-authoritative.
- Preserve alive and critical xeno health icon states.
- Do not add a shader pipeline or unrelated Yautja refactors.
- Preserve unrelated dirty-worktree changes; only stage files belonging to this task when committing.

---

### Task 1: Add failing authorization and dead-xeno tests

**Files:**
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminEntryTest.cs`
- Modify: `Content.Tests/Client/_RMC14/Medical/HUD/CMXenoHealthIconStateTest.cs`

**Interfaces:**
- Consumes: `AdminCommandAttribute`, the nine Yautja command types, `CMXenoHealthIconState.GetState`.
- Produces: regression coverage that fails before the production changes.

- [ ] **Step 1: Extend command coverage with the complete command inventory.**

Add a test that checks these command types: `YautjaClanAdminCommand`, `YautjaClanInfoCommand`, `YautjaPredatorAdminEditorCommand`, `YautjaYoungbloodCallCommand`, `YautjaClanSetMemberCommand`, `YautjaClanCreateCommand`, `YautjaClanWhitelistCommand`, `YautjaRankCommand`, and `YautjaGetRankCommand`. For each type, read its `AdminCommandAttribute` and assert `Flags == AdminFlags.Clans`.

```csharp
[Test]
public void EveryYautjaConsoleCommandRequiresClanPermission()
{
    var commandTypes = new[]
    {
        typeof(YautjaClanAdminCommand),
        typeof(YautjaClanInfoCommand),
        typeof(YautjaPredatorAdminEditorCommand),
        typeof(YautjaYoungbloodCallCommand),
        typeof(YautjaClanSetMemberCommand),
        typeof(YautjaClanCreateCommand),
        typeof(YautjaClanWhitelistCommand),
        typeof(YautjaRankCommand),
        typeof(YautjaGetRankCommand),
    };

    foreach (var commandType in commandTypes)
    {
        var attribute = commandType
            .GetCustomAttributes(typeof(AdminCommandAttribute), false)
            .Cast<AdminCommandAttribute>()
            .Single();

        Assert.That(attribute.Flags, Is.EqualTo(AdminFlags.Clans), commandType.FullName);
    }
}
```

- [ ] **Step 2: Change the dead-xeno health-state expectation.**

Rename `DeadXenoUsesTheZeroHealthState` to `DeadXenoDoesNotExposeAHealthState` and assert that the resolver returns `null`:

```csharp
[Test]
public void DeadXenoDoesNotExposeAHealthState()
{
    Assert.That(CMXenoHealthIconState.GetState(300, MobState.Dead, 200, 300), Is.Null);
}
```

- [ ] **Step 3: Run the new tests before production changes.**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminEntryTest.EveryYautjaConsoleCommandRequiresClanPermission|FullyQualifiedName~CMXenoHealthIconStateTest.DeadXenoDoesNotExposeAHealthState"
```

Expected: the authorization test fails because several commands do not require `Clans`, and the dead-xeno test fails because the resolver returns `xenohealth0`.

### Task 2: Require `Clans` on every Yautja console command

**Files:**
- Modify: `Content.Server/_CMU14/Yautja/YautjaClanInfoCommand.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaPredatorAdminEditorCommand.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaYoungbloodAdminCommand.cs`
- Modify: `Content.Server/Administration/Commands/YautjaClanCommands.cs`
- Modify: `Content.Server/Administration/Commands/YautjaRankCommands.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminEntryTest.cs`

**Interfaces:**
- Consumes: the failing reflection test from Task 1.
- Produces: command registration metadata requiring `AdminFlags.Clans` for all nine commands.

- [ ] **Step 1: Change each weaker command attribute to `AdminFlags.Clans`.**

Replace `[AdminCommand(AdminFlags.Host)]`, `[AdminCommand(AdminFlags.Admin)]`, and the missing attribute on `YautjaClanInfoCommand` with `[AdminCommand(AdminFlags.Clans)]`. Do not change command names, argument parsing, EUI behavior, database calls, or audit logging.

- [ ] **Step 2: Run the authorization regression test.**

Run the command from Task 1 Step 3.

Expected: the authorization test passes.

### Task 3: Suppress dead-xeno health icons

**Files:**
- Modify: `Content.Client/_RMC14/Medical/HUD/CMXenoHealthIconState.cs`
- Test: `Content.Tests/Client/_RMC14/Medical/HUD/CMXenoHealthIconStateTest.cs`

**Interfaces:**
- Consumes: `MobState.Dead` and the existing `CMXenoHealthIconState.GetState` callers.
- Produces: `null` for dead xenos and unchanged strings for alive/critical xenos.

- [ ] **Step 1: Return no health state for dead xenos.**

At the start of `CMXenoHealthIconState.GetState`, add:

```csharp
if (state == MobState.Dead)
    return null;
```

Leave the critical and alive calculations unchanged.

- [ ] **Step 2: Run all xeno health-state tests.**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~CMXenoHealthIconStateTest"
```

Expected: healthy, critical, and dead-xeno tests pass.

### Task 4: Verify thermal wall vision and final integration

**Files:**
- Inspect: `Content.Client/_CMU14/Yautja/YautjaWallVisionOverlay.cs`
- Inspect: `Content.Shared/_CMU14/Yautja/YautjaWallVisionTargeting.cs`
- Test: `Content.Tests/Shared/_CMU14/Yautja/YautjaWallVisionTargetingTest.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaWallVisionPrototypeTest.cs`

**Interfaces:**
- Consumes: linked visor state and `YautjaWallVisionTargeting.IsEligible`.
- Produces: evidence that wall vision remains restricted to an active linked thermal visor and eligible mobs.

- [ ] **Step 1: Run thermal visor unit and prototype tests.**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaWallVisionTargetingTest"
dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter "FullyQualifiedName~YautjaWallVisionPrototypeTest"
```

Expected: all thermal visor tests pass without changing the existing overlay.

- [ ] **Step 2: Build the integration test project.**

Run:

```powershell
dotnet build Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore -m:1 --verbosity quiet
```

Expected: exit code 0. Existing NU1900 vulnerability-feed warnings may remain if the configured NuGet source is unavailable; any compiler error fails the task.

- [ ] **Step 3: Run the focused Yautja regression set.**

Run:

```powershell
dotnet test bin/Content.IntegrationTests/Content.IntegrationTests.dll --filter "FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaClanAdminEntryTest|FullyQualifiedName~Content.IntegrationTests._CMU14.Yautja.YautjaWallVisionPrototypeTest" -- NUnit.ConsoleOut=0
```

Expected: all selected tests pass.

- [ ] **Step 4: Check the diff and runtime processes.**

Run:

```powershell
git diff --check
Get-CimInstance Win32_Process -Filter "Name='Content.Server.exe' OR Name='Content.Client.exe'" | Select-Object ProcessId,Name,ExecutablePath
```

Expected: no whitespace errors and both workspace binaries are running after any required restart.
