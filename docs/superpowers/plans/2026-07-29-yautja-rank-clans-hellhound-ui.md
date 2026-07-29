# Yautja Rank, Clan Permission, Hellhound, and UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Yautja personalization use external rank entitlements, add a dedicated `Clans` admin permission, verify the complete hellhound raffle takeover, and prevent localized technology controls from clipping.

**Architecture:** Keep base `YautjaProfileCapabilities` as the authoritative personalization entitlement and derive status-specific capabilities only for the spawned rank presentation. Reuse the command permission pipeline for F7 visibility and enforce the same new flag in the clan EUI. Preserve the existing hellhound implementation when its end-to-end tests pass, and replace fixed-width technology rows with an always-safe vertical layout.

**Tech Stack:** C#/.NET 10, RobustToolbox UI/EUI and admin command framework, YAML prototypes, NUnit unit/integration tests, PowerShell.

## Global Constraints

- External `Ancient` plus profile status `Normal` keeps Ancient personalization entitlements but spawns with active rank `Blooded`.
- Every rank- or whitelist-gated personalization option uses base entitlement capabilities on both client and server.
- Generic `Admin` permission does not grant Yautja clan administration; only the new `Clans` flag does.
- Do not rewrite a passing hellhound workflow.
- Preserve all unrelated dirty-worktree changes and do not stage production files that already contain user changes.
- Run focused tests before complete client and server builds.

---

### Task 1: Separate personalization entitlements from active rank

**Files:**
- Modify: `Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs:591-634`
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs:392-410, 540-835`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaCharacterProfileTest.cs`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaRankParityTest.cs`
- Test: `Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs`

**Interfaces:**
- Consumes: `YautjaProfileCapabilities`, `YautjaProfileCapabilities.ForStatus(YautjaProfileStatus)`, and existing `CanUseLegacySet`, `CanUseCape`, and `CanUseBracer` policy methods.
- Produces: `YautjaCharacterProfile.SanitizeForCapabilities(YautjaProfileCapabilities)` that stores the active rank while validating equipment against the base capabilities.

- [ ] **Step 1: Add a failing server sanitizer regression test**

Add a test that captures the approved split:

```csharp
[Test]
public void ExternalAncientNormalStatusKeepsEntitledGearAndBloodedActiveRank()
{
    var capabilities = new YautjaProfileCapabilities(
        YautjaRank.Ancient,
        canUseUnique: true,
        canUseLegacy: true,
        canUseCouncilStatus: true,
        canUseLeaderStatus: true);
    var profile = YautjaCharacterProfile.Default
        .WithStatus(YautjaProfileStatus.Normal)
        .WithUnique(YautjaUniqueSet.Anubys)
        .WithLegacy(YautjaLegacySet.None)
        .WithCapeStyle(YautjaCapeStyle.Ceremonial)
        .WithBracer(YautjaBracerMaterial.Bone);

    var sanitized = profile.SanitizeForCapabilities(capabilities);

    Assert.Multiple(() =>
    {
        Assert.That(sanitized.Status, Is.EqualTo(YautjaProfileStatus.Normal));
        Assert.That(sanitized.ClanRank, Is.EqualTo(YautjaRank.Blooded));
        Assert.That(sanitized.Unique, Is.EqualTo(YautjaUniqueSet.Anubys));
        Assert.That(sanitized.CapeStyle, Is.EqualTo(YautjaCapeStyle.Ceremonial));
        Assert.That(sanitized.BracerMaterial, Is.EqualTo(YautjaBracerMaterial.Bone));
    });
}
```

- [ ] **Step 2: Run the new test and verify RED**

Run:

```powershell
dotnet test Content.IntegrationTests\Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~ExternalAncientNormalStatusKeepsEntitledGearAndBloodedActiveRank" -p:NuGetAudit=false -p:NoWarn=RA0002
```

Expected: the unique set, ceremonial cape, or advanced bracer is stripped because the sanitizer currently checks status-specific Blooded capabilities.

- [ ] **Step 3: Preserve selections while assigning the active rank**

Add a private helper that changes rank metadata without clearing unique equipment:

```csharp
private YautjaCharacterProfile WithActiveRank(YautjaRank rank)
{
    if (!Enum.IsDefined(rank))
        rank = YautjaRank.Blooded;

    return new YautjaCharacterProfile(this)
    {
        ClanRank = rank,
        OwnerRank = YautjaRankResolver.ToOwnerRank(rank),
    };
}
```

Update the sanitizer to derive active rank once but perform all equipment checks against `capabilities`:

```csharp
public YautjaCharacterProfile SanitizeForCapabilities(YautjaProfileCapabilities capabilities)
{
    var status = capabilities.SanitizeStatus(Status);
    var activeCapabilities = capabilities.ForStatus(status);
    var profile = WithStatus(status).WithActiveRank(activeCapabilities.Rank);

    if (!capabilities.CanUseLegacySet(profile.Legacy))
        profile = profile.WithLegacy(YautjaLegacySet.None);

    if (!capabilities.CanUseUnique || profile.Legacy != YautjaLegacySet.None)
        profile = profile.WithUnique(YautjaUniqueSet.None);

    if (!capabilities.CanUseCape(profile.CapeStyle))
        profile = profile.WithCapeStyle(YautjaCapeStyle.Full);

    if (!capabilities.CanUseBracer(profile.BracerMaterial))
        profile = profile.WithBracer(YautjaBracerMaterial.Ebony);

    return profile;
}
```

- [ ] **Step 4: Make client selectors use entitlement capabilities**

Keep `_effectiveCapabilities` for `UpdateRankPresentation()`. In the selector rebuild methods, replace the four equipment-policy calls that currently pass `_effectiveCapabilities`:

```csharp
YautjaProfileEditorLayout.IsLegacySetLocked(_capabilities, legacy);
YautjaProfileEditorLayout.IsUniqueSetLocked(_capabilities, unique);
YautjaProfileEditorLayout.IsBracerLocked(_capabilities, material);
YautjaProfileEditorLayout.IsCapeLocked(_capabilities, style);
```

Do not change status availability or the displayed active rank.

- [ ] **Step 5: Add a capability boundary assertion**

Extend `EffectiveCapabilitiesFollowSelectedSeniorStatus` to make the split explicit:

```csharp
Assert.That(capabilities.Rank, Is.EqualTo(YautjaRank.Ancient));
Assert.That(capabilities.CanUseUnique, Is.True);
Assert.That(capabilities.CanUseCape(YautjaCapeStyle.Ceremonial), Is.True);
Assert.That(capabilities.ForStatus(YautjaProfileStatus.Normal).Rank, Is.EqualTo(YautjaRank.Blooded));
```

- [ ] **Step 6: Run focused rank and profile tests**

Run:

```powershell
dotnet test Content.IntegrationTests\Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaRankParityTest|FullyQualifiedName~YautjaCharacterProfileTest" -p:NuGetAudit=false -p:NoWarn=RA0002
dotnet test Content.Tests\Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaProfileEditorLayoutTest" -p:NuGetAudit=false
```

Expected: all selected tests pass; active `Normal` rank remains `Blooded`.

- [ ] **Step 7: Record the task checkpoint**

Run `git diff --check` and inspect only the three touched production/test files. Do not stage them because each already contains pre-existing user changes.

---

### Task 2: Make technology personalization controls localization-safe

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs:1545-1592`
- Test: `Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs`

**Interfaces:**
- Consumes: existing `OptionButton`, `Label`, sound preview callback, and localization keys.
- Produces: `TechOptionBlock` controls with a full-width selector and no fixed-width title column.

- [ ] **Step 1: Add a pure layout contract**

Add constants to `YautjaProfileEditorLayout`:

```csharp
public const int TechOptionSpacing = 6;
public const int TechOptionBottomMargin = 12;
```

Add a test:

```csharp
[Test]
public void TechnologyOptionsUseVerticalLocalizationSafeLayout()
{
    Assert.Multiple(() =>
    {
        Assert.That(YautjaProfileEditorLayout.TechOptionSpacing, Is.GreaterThan(0));
        Assert.That(YautjaProfileEditorLayout.TechOptionBottomMargin, Is.GreaterThanOrEqualTo(10));
    });
}
```

- [ ] **Step 2: Replace the fixed horizontal title row**

Set `option.HorizontalExpand = true`. Build the block in this order:

```csharp
var block = new BoxContainer
{
    Orientation = BoxContainer.LayoutOrientation.Vertical,
    HorizontalExpand = true,
    SeparationOverride = YautjaProfileEditorLayout.TechOptionSpacing,
    Margin = new Thickness(0, 0, 0, YautjaProfileEditorLayout.TechOptionBottomMargin),
    Children =
    {
        new Label { Text = Loc.GetString(label), HorizontalExpand = true },
        option,
        help,
    },
};
```

When `previewButton` exists, add it after `option` and before `help`, with
`HorizontalExpand = true`. Remove the title `MinWidth = 160` and preview
`MinWidth = 92` assumptions.

- [ ] **Step 3: Run the client layout test**

Run:

```powershell
dotnet test Content.Tests\Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaProfileEditorLayoutTest" -p:NuGetAudit=false
```

Expected: all selected tests pass and client compilation accepts the new control tree.

- [ ] **Step 4: Record the task checkpoint**

Run `git diff --check` and inspect the `TechOptionBlock` diff for fixed-width technology labels. Do not stage the dirty production file.

---

### Task 3: Add and enforce the `Clans` admin permission

**Files:**
- Modify: `Content.Shared/Administration/AdminFlags.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaClanAdminCommand.cs`
- Modify: `Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs`
- Modify: `Content.Tests/Shared/Administration/AdminFlagsExtTest.cs`
- Modify: `Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminEntryTest.cs`

**Interfaces:**
- Produces: `AdminFlags.Clans = 1ul << 34`.
- Produces: `YautjaClanAdminEui.RequiredAdminFlag` as the single permission used by command advertisement and EUI authorization.

- [ ] **Step 1: Write failing flag and command tests**

Add `CLANS` cases to both conversion tests:

```csharp
[TestCase("CLANS", AdminFlags.Clans)]
[TestCase("ADMIN,CLANS", AdminFlags.Admin | AdminFlags.Clans)]
```

Extend the clan admin entry integration test:

```csharp
var attributes = typeof(YautjaClanAdminCommand)
    .GetCustomAttributes(typeof(AdminCommandAttribute), false)
    .Cast<AdminCommandAttribute>()
    .ToArray();

Assert.That(attributes, Has.Exactly(1).Items);
Assert.That(attributes[0].Flags, Is.EqualTo(AdminFlags.Clans));
Assert.That(YautjaClanAdminEui.RequiredAdminFlag, Is.EqualTo(AdminFlags.Clans));
```

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
dotnet test Content.Tests\Content.Tests.csproj --no-restore --filter "FullyQualifiedName~AdminFlagsExtTest" -p:NuGetAudit=false
dotnet test Content.IntegrationTests\Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminEntryTest" -p:NuGetAudit=false -p:NoWarn=RA0002
```

Expected: compilation fails because `AdminFlags.Clans` does not exist.

- [ ] **Step 3: Add the unique admin flag**

Append:

```csharp
/// <summary>
///     Yautja clan administration.
/// </summary>
Clans = 1ul << 34,
```

Do not reuse any existing bit.

- [ ] **Step 4: Centralize and enforce the EUI permission**

In `YautjaClanAdminEui`, add:

```csharp
public const AdminFlags RequiredAdminFlag = AdminFlags.Clans;
```

Replace every `HasAdminFlag(Player, AdminFlags.Admin)` check in `Opened`,
`HandleMessage`, the operation-gate recheck, and `OnAdminPermsChanged` with
`RequiredAdminFlag`.

Change the command attribute to:

```csharp
[AdminCommand(YautjaClanAdminEui.RequiredAdminFlag)]
```

The existing F7 `CommandButton` requires no special client logic because command advertisement controls its visibility.

- [ ] **Step 5: Run focused permission tests**

Run the two commands from Step 2 again. Expected: all selected tests pass.

- [ ] **Step 6: Record the task checkpoint**

Run `git diff --check`; inspect that no `AdminFlags.Admin` clan-EUI guard remains:

```powershell
rg -n "AdminFlags\.Admin" Content.Server\_CMU14\Yautja\YautjaClanAdminCommand.cs Content.Server\_CMU14\Yautja\YautjaClanAdminEui.cs
```

Expected: no matches.

---

### Task 4: Verify and repair the hellhound wake-to-raffle workflow

**Files:**
- Verify: `Content.Server/_CMU14/Yautja/YautjaSleepingHellhoundSystem.cs`
- Verify: `Resources/Prototypes/_CMU14/Threats/Yautja/Equipment/hellhound.yml`
- Verify: `Resources/Prototypes/_CMU14/Maps/huntership_support.yml`
- Test: `Content.IntegrationTests/_CMU14/Yautja/YautjaSmokeTest.cs`

**Interfaces:**
- Consumes: hand/world activation, `DialogSystem.OpenConfirmation`, `GhostRole` with raffle setting `default`, and `GhostRoleSystem`.
- Produces: confirmed wake, standard raffle enrollment, and winner transfer into `CMUMobYautjaHellhound`.

- [ ] **Step 1: Run the four end-to-end tests unchanged**

Run:

```powershell
dotnet test Content.IntegrationTests\Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~SleepingHellhoundRequiresConfirmationBeforeWaking|FullyQualifiedName~SleepingHellhoundLeftClickActivationOpensConfirmation|FullyQualifiedName~HellhoundGhostRoleUsesDefaultRaffleQueue|FullyQualifiedName~HellhoundRaffleWinnerIsTransferredIntoTheGhostRoleBody" -p:NuGetAudit=false -p:NoWarn=RA0002
```

Expected: all four tests pass.

- [ ] **Step 2: Fix only a demonstrated failing stage**

If a test fails, map it to exactly one stage:

- no dialog: repair event subscription/handled state in `YautjaSleepingHellhoundSystem`;
- no active hound: repair the confirmation event or spawn prototype;
- no raffle: restore `GhostRole`, `GhostTakeoverAvailable`, and `raffle.settings: default`;
- no takeover: repair the standard `GhostRoleSystem` integration without adding a parallel lottery.

Re-run only the failed test after each minimal change, then run all four. If all
four pass initially, make no production change.

- [ ] **Step 3: Record the verification checkpoint**

Record the passing test count and duration. Confirm that the sleeping and active prototype IDs remain `CMUHunterShipSleepingHellhound` and `CMUMobYautjaHellhound`.

---

### Task 5: Final validation and builds

**Files:**
- Verify all files changed in Tasks 1-4.

**Interfaces:**
- Produces: a clean focused test result and compilable client/server artifacts.

- [ ] **Step 1: Run the focused regression set**

Run the Task 1, Task 2, Task 3, and Task 4 test commands. Every selected test must pass.

- [ ] **Step 2: Build the server**

Run:

```powershell
dotnet build Content.Server\Content.Server.csproj --no-restore --nologo --verbosity:minimal -p:NuGetAudit=false
```

Expected: exit code 0. Report unrelated warnings separately.

- [ ] **Step 3: Build the client**

Run:

```powershell
dotnet build Content.Client\Content.Client.csproj --no-restore --nologo --verbosity:minimal -p:NuGetAudit=false
```

Expected: exit code 0. Report unrelated warnings separately.

- [ ] **Step 4: Check the final diff**

Run:

```powershell
git diff --check
git status --short
git diff -- Content.Shared/_CMU14/Yautja/YautjaCharacterProfile.cs Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs Content.Shared/Administration/AdminFlags.cs Content.Server/_CMU14/Yautja/YautjaClanAdminCommand.cs Content.Server/_CMU14/Yautja/YautjaClanAdminEui.cs Content.Tests/Shared/Administration/AdminFlagsExtTest.cs Content.IntegrationTests/_CMU14/Yautja/YautjaClanAdminEntryTest.cs
```

Expected: no whitespace errors; only intended hunks are attributable to this plan.

- [ ] **Step 5: Report completion**

Report the exact tests/builds run, note whether hellhound needed code changes,
and list the files changed. Do not claim unrelated dirty-worktree changes.
