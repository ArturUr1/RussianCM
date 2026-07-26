# Yautja Personalization UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Перестроить UI персонализации яутжа в лобби в двухколоночное рабочее место с закреплённым превью, боковой навигацией, сводкой комплекта и понятными locked-состояниями, сохранив все текущие настройки и поведение профиля.

**Architecture:** Оставить `YautjaCharacterProfile`, существующие `Mutate`, `ReloadPreview`, селекторы и `OnProfileChanged` без изменения контракта. В `YautjaProfileEditor` заменить горизонтальную группу вкладок на каталог из пяти UI-категорий и контейнер активной страницы; слева собрать превью, identity, rank и summary. Чистую модель порядка категорий и правила locked unique-вариантов вынести в маленький клиентский helper, чтобы проверить их unit-тестом без запуска полноценного лобби.

**Tech Stack:** C#, Robust Client UserInterface (`BoxContainer`, `PanelContainer`, `ScrollContainer`, `Button`, `GridContainer`), текущие `YautjaCharacterProfile`/`YautjaRankResolver`, Fluent localization, NUnit.

## Global Constraints

- Сохранить все текущие настройки, варианты, фильтры, ограничения ранга и формат профиля.
- Не менять `Content.Shared/_CMU14/Yautja`, серверную авторизацию, прототипы экипировки или родительский `HumanoidProfileEditor`.
- Все изменения профиля по-прежнему проходят через существующий `OnProfileChanged` и dirty-state родительского лобби.
- Сохранить текущую тёмную тему и бронзовые акценты лобби; новые bitmap/vector-ассеты не добавлять.
- Добавить одинаковые новые localization keys в `en-US` и `ru-RU`.
- Не включать в коммиты существующие изменения рабочей копии; перед каждым коммитом проверять staged diff.
- Критерий доступности unique-наборов для UI остаётся `YautjaRankResolver.CanUseUnique`; locked-карточка не должна отправлять callback выбора.

---

## Files and responsibilities

- Create: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditorLayout.cs` — чистый порядок пяти категорий и UI-only predicate для locked unique-вариантов.
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs` — двухколоночный layout, боковая навигация, активные страницы, preview column, summary, подписи карточек и locked rendering.
- Create: `Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs` — быстрые NUnit-регрессии порядка категорий и rank-gating predicate.
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl` — английские названия групп, summary и locked-состояния.
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl` — русские названия групп, summary и locked-состояния.

---

### Task 1: Add a testable UI category catalog

**Files:**
- Create: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditorLayout.cs`
- Create: `Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs`

**Interfaces:**
- Produces `YautjaProfileEditorCategory` with values `Appearance`, `Equipment`, `Sets`, `Technology`, `Description`.
- Produces `YautjaProfileEditorCategoryInfo(YautjaProfileEditorCategory Id, string LocalizationKey)`.
- Produces `YautjaProfileEditorLayout.Categories`, an ordered `IReadOnlyList<YautjaProfileEditorCategoryInfo>`.
- Produces `YautjaProfileEditorLayout.IsUniqueSetLocked(YautjaCharacterProfile profile, YautjaUniqueSet unique)`.

- [ ] **Step 1: Write the failing tests for category order and locked behavior**

```csharp
using System.Linq;
using Content.Client._CMU14.Yautja.Lobby;
using Content.Shared._CMU14.Yautja;
using NUnit.Framework;

namespace Content.Tests.Client._CMU14.Yautja;

[TestFixture]
public sealed class YautjaProfileEditorLayoutTest
{
    [Test]
    public void CategoriesExposeAllNavigationGroupsInDesignOrder()
    {
        Assert.That(
            YautjaProfileEditorLayout.Categories,
            Has.Exactly(5).Items);
        Assert.That(
            YautjaProfileEditorLayout.Categories.Select(info => info.Id),
            Is.EqualTo(new[]
            {
                YautjaProfileEditorCategory.Appearance,
                YautjaProfileEditorCategory.Equipment,
                YautjaProfileEditorCategory.Sets,
                YautjaProfileEditorCategory.Technology,
                YautjaProfileEditorCategory.Description,
            }));
    }

    [TestCase(YautjaRank.Unblooded, true)]
    [TestCase(YautjaRank.YoungBlood, true)]
    [TestCase(YautjaRank.Blooded, true)]
    [TestCase(YautjaRank.Elite, false)]
    [TestCase(YautjaRank.Elder, false)]
    [TestCase(YautjaRank.Leader, false)]
    [TestCase(YautjaRank.Ancient, false)]
    public void UniqueSetsAreLockedUntilElite(YautjaRank rank, bool locked)
    {
        var profile = YautjaCharacterProfile.Default.WithRank(rank);

        Assert.That(
            YautjaProfileEditorLayout.IsUniqueSetLocked(profile, YautjaUniqueSet.Anubys),
            Is.EqualTo(locked));
    }

    [Test]
    public void NoneOptionIsNeverLocked()
    {
        var profile = YautjaCharacterProfile.Default.WithRank(YautjaRank.Blooded);

        Assert.That(
            YautjaProfileEditorLayout.IsUniqueSetLocked(profile, YautjaUniqueSet.None),
            Is.False);
    }
}
```

- [ ] **Step 2: Run the focused test to verify it fails for the missing catalog**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaProfileEditorLayoutTest`

Expected: FAIL because the new `YautjaProfileEditorLayout` and `YautjaProfileEditorCategory` symbols are not yet implemented; `YautjaCharacterProfile` and `YautjaUniqueSet` already come from the shared project.

- [ ] **Step 3: Implement the minimal catalog and predicate**

Create the client helper with the exact public-to-tests surface used above:

```csharp
using System.Collections.Generic;
using Content.Shared._CMU14.Yautja;

namespace Content.Client._CMU14.Yautja.Lobby;

public enum YautjaProfileEditorCategory
{
    Appearance,
    Equipment,
    Sets,
    Technology,
    Description,
}

public sealed record YautjaProfileEditorCategoryInfo(
    YautjaProfileEditorCategory Id,
    string LocalizationKey);

public static class YautjaProfileEditorLayout
{
    public static IReadOnlyList<YautjaProfileEditorCategoryInfo> Categories { get; } =
    [
        new(YautjaProfileEditorCategory.Appearance, "cmu-yautja-lobby-category-appearance"),
        new(YautjaProfileEditorCategory.Equipment, "cmu-yautja-lobby-category-equipment"),
        new(YautjaProfileEditorCategory.Sets, "cmu-yautja-lobby-category-sets"),
        new(YautjaProfileEditorCategory.Technology, "cmu-yautja-lobby-category-technology"),
        new(YautjaProfileEditorCategory.Description, "cmu-yautja-lobby-category-description"),
    ];

    public static bool IsUniqueSetLocked(YautjaCharacterProfile profile, YautjaUniqueSet unique)
    {
        return unique != YautjaUniqueSet.None && !YautjaRankResolver.CanUseUnique(profile);
    }
}
```

- [ ] **Step 4: Run the focused test to verify the catalog passes**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaProfileEditorLayoutTest`

Expected: PASS with all category-order and locked-state cases green.

- [ ] **Step 5: Commit the isolated catalog/test change**

```powershell
git add -- Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditorLayout.cs Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs
git diff --cached --check
git diff --cached --stat
git commit -m "test: define Yautja profile editor layout catalog"
```

---

### Task 2: Replace tabs with the two-column workbench

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs:56-230` for fields and constructor layout.
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs:1144-1160` for category-page helpers.

**Interfaces:**
- Consumes `YautjaProfileEditorLayout.Categories` from Task 1.
- Produces `AddCategory(YautjaProfileEditorCategory category, Control content)`, `SelectCategory(YautjaProfileEditorCategory category)`, and a stable active-category state used by the constructor.

- [ ] **Step 1: Write the failing active-page visibility test**

Extend `YautjaProfileEditorLayoutTest` before changing the editor layout:

```csharp
[TestCase(YautjaProfileEditorCategory.Appearance, YautjaProfileEditorCategory.Appearance, true)]
[TestCase(YautjaProfileEditorCategory.Appearance, YautjaProfileEditorCategory.Equipment, false)]
public void OnlyTheActiveCategoryPageIsVisible(
    YautjaProfileEditorCategory active,
    YautjaProfileEditorCategory candidate,
    bool expected)
{
    Assert.That(YautjaProfileEditorLayout.IsCategoryActive(active, candidate), Is.EqualTo(expected));
}
```

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaProfileEditorLayoutTest -m:1`

Expected: FAIL because `IsCategoryActive` does not exist yet. If the environment cannot build for the same OOM/disk reason recorded in Task 1, capture that exact output and continue with the code change; do not alter the test to hide the missing API.

- [ ] **Step 2: Replace the `TabContainer` field with navigation/page hosts**

Add these fields and remove `_categoryTabs`:

```csharp
private readonly ButtonGroup _categoryButtonGroup = new();
private readonly BoxContainer _categoryNavigation = new()
{
    Orientation = BoxContainer.LayoutOrientation.Vertical,
    SeparationOverride = 4,
};
private readonly BoxContainer _categoryPages = new()
{
    Orientation = BoxContainer.LayoutOrientation.Vertical,
    HorizontalExpand = true,
    VerticalExpand = true,
};
private readonly Dictionary<YautjaProfileEditorCategory, Control> _categoryPageControls = new();
private readonly Dictionary<YautjaProfileEditorCategory, Button> _categoryButtons = new();
private YautjaProfileEditorCategory _activeCategory = YautjaProfileEditorCategory.Appearance;
```

- [ ] **Step 3: Implement the minimal active-category predicate and registration/selection methods**

Add this pure helper to `YautjaProfileEditorLayout` and make `SelectCategory` use it when assigning `page.Visible`:

```csharp
public static bool IsCategoryActive(
    YautjaProfileEditorCategory active,
    YautjaProfileEditorCategory candidate)
{
    return active == candidate;
}
```

Replace `AddTab` with methods that keep every page alive, but make only the active `ScrollContainer` visible:

```csharp
private void AddCategory(YautjaProfileEditorCategory category, Control content)
{
    var page = CategoryScroll(content);
    page.Visible = _categoryPageControls.Count == 0;
    _categoryPages.AddChild(page);
    _categoryPageControls[category] = page;

    var definition = YautjaProfileEditorLayout.Categories.Single(info => info.Id == category);
    var button = new Button
    {
        Text = Loc.GetString(definition.LocalizationKey),
        ToggleMode = true,
        Group = _categoryButtonGroup,
        HorizontalExpand = true,
        Pressed = _categoryPageControls.Count == 1,
    };
    button.OnPressed += _ => SelectCategory(category);
    _categoryNavigation.AddChild(button);
    _categoryButtons[category] = button;
}

private void SelectCategory(YautjaProfileEditorCategory category)
{
    _activeCategory = category;
    foreach (var (id, page) in _categoryPageControls)
        page.Visible = YautjaProfileEditorLayout.IsCategoryActive(category, id);

    foreach (var (id, button) in _categoryButtons)
        button.Pressed = YautjaProfileEditorLayout.IsCategoryActive(category, id);
}
```

Use `CategoryScroll` for each page so only the right content area scrolls. Run the focused test again and expect GREEN (subject to the documented environment resource limit).

- [ ] **Step 4: Build the right-side workspace and register all five categories**

Replace the existing `workArea.AddChild(_categoryTabs)` and `AddTab` calls with a horizontal workspace containing a fixed navigation panel and a content panel:

```csharp
var categoryWorkspace = new BoxContainer
{
    Orientation = BoxContainer.LayoutOrientation.Horizontal,
    HorizontalExpand = true,
    VerticalExpand = true,
    SeparationOverride = 8,
};
categoryWorkspace.AddChild(new PanelContainer
{
    MinWidth = 176,
    Children = { _categoryNavigation },
});
categoryWorkspace.AddChild(new PanelContainer
{
    HorizontalExpand = true,
    VerticalExpand = true,
    Children = { _categoryPages },
});
workArea.AddChild(categoryWorkspace);

AddCategory(YautjaProfileEditorCategory.Appearance, new BoxContainer
{
    Orientation = BoxContainer.LayoutOrientation.Vertical,
    HorizontalExpand = true,
    Children =
    {
        VisualBlock("cmu-yautja-lobby-skin-color", _skinGrid),
        VisualBlock("cmu-yautja-lobby-eyes", _eyeGrid),
        VisualBlock("cmu-yautja-lobby-quills", _quillGrid),
    },
});
AddCategory(YautjaProfileEditorCategory.Equipment, BuildEquipmentPage());
AddCategory(YautjaProfileEditorCategory.Sets, BuildSetsPage());
AddCategory(YautjaProfileEditorCategory.Technology, BuildTechnologyPage());
AddCategory(YautjaProfileEditorCategory.Description, FlavorBlock());
```

Keep the existing equipment selector fields and rebuild methods. Add these layout-only helper signatures and move the current `AddTab` child trees into them without changing the contained controls:

```csharp
private Control BuildEquipmentPage();
private Control BuildSetsPage();
private Control BuildTechnologyPage();
```

- [ ] **Step 5: Run the focused catalog test and compile the client project**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaProfileEditorLayoutTest`

Then run: `dotnet build Content.Client/Content.Client.csproj --no-restore`

Expected: the catalog test remains green and the client project compiles without references to the removed `_categoryTabs` or `AddTab`.

- [ ] **Step 6: Commit the workbench navigation change**

```powershell
git add -- Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs
git diff --cached --check
git diff --cached --stat
git commit -m "feat: add Yautja profile workbench navigation"
```

---

### Task 3: Move identity controls beside the preview and add the live summary

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs:39-160` for fields and constructor placement.
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs:255-276` for profile binding.

**Interfaces:**
- Consumes the existing `YautjaCharacterProfile` display-name helpers and rank metadata.
- Produces `YautjaProfileEditorLayout.BuildSummary(YautjaCharacterProfile profile)` and `UpdateSelectionSummary(YautjaCharacterProfile yautja)` called after `SetProfile` and after every successful `Mutate`.

- [ ] **Step 1: Write the failing summary test**

Extend `YautjaProfileEditorLayoutTest` before adding the summary implementation:

```csharp
[Test]
public void BuildSummaryUsesUniqueSetAndCurrentGearNames()
{
    var profile = YautjaCharacterProfile.Default
        .WithRank(YautjaRank.Elite)
        .WithUnique(YautjaUniqueSet.Anubys)
        .WithArmor(YautjaGearMaterial.Silver, 2)
        .WithMask(YautjaGearMaterial.Bronze, 3)
        .WithGreaves(YautjaGearMaterial.Bone, 1)
        .WithCapeStyle(YautjaCapeStyle.Full)
        .WithBracer(YautjaBracerMaterial.Crimson)
        .WithCaster(YautjaBracerMaterial.Silver);

    var summary = YautjaProfileEditorLayout.BuildSummary(profile);

    Assert.That(summary.Set, Is.EqualTo(YautjaCharacterProfile.GetUniqueDisplayName(YautjaUniqueSet.Anubys)));
    Assert.That(summary.Armor, Is.EqualTo(YautjaCharacterProfile.GetArmorStyleDisplayName(YautjaGearMaterial.Silver, 2)));
    Assert.That(summary.Mask, Is.EqualTo(YautjaCharacterProfile.GetMaskStyleDisplayName(YautjaGearMaterial.Bronze, 3)));
    Assert.That(summary.Greaves, Is.EqualTo(YautjaCharacterProfile.GetGreavesStyleDisplayName(YautjaGearMaterial.Bone, 1)));
    Assert.That(summary.Cape, Is.EqualTo(YautjaCharacterProfile.GetCapeDisplayName(YautjaCapeStyle.Full)));
    Assert.That(summary.Bracer, Is.EqualTo(YautjaCharacterProfile.GetBracerDisplayName(YautjaBracerMaterial.Crimson)));
    Assert.That(summary.Caster, Is.EqualTo(YautjaCharacterProfile.GetCasterDisplayName(YautjaBracerMaterial.Silver)));
}
```

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaProfileEditorLayoutTest -m:1`

Expected: FAIL because `YautjaProfileEditorLayout.BuildSummary` and its return type do not exist yet.

- [ ] **Step 2: Implement the pure summary helper and make the test green**

Add a `YautjaProfileEditorSummary` record with `Set`, `Armor`, `Mask`, `Greaves`, `Cape`, `Bracer`, and `Caster` string properties in `YautjaProfileEditorLayout.cs`. Add:

```csharp
public static YautjaProfileEditorSummary BuildSummary(YautjaCharacterProfile profile)
{
    var set = profile.Unique != YautjaUniqueSet.None
        ? YautjaCharacterProfile.GetUniqueDisplayName(profile.Unique)
        : profile.Legacy != YautjaLegacySet.None
            ? YautjaCharacterProfile.GetLegacyDisplayName(profile.Legacy)
            : "—";

    return new YautjaProfileEditorSummary(
        set,
        YautjaCharacterProfile.GetArmorStyleDisplayName(profile.ArmorMaterial, profile.ArmorStyle),
        YautjaCharacterProfile.GetMaskStyleDisplayName(profile.MaskMaterial, profile.MaskStyle),
        YautjaCharacterProfile.GetGreavesStyleDisplayName(profile.GreavesMaterial, profile.GreavesStyle),
        YautjaCharacterProfile.GetCapeDisplayName(profile.CapeStyle),
        YautjaCharacterProfile.GetBracerDisplayName(profile.BracerMaterial),
        YautjaCharacterProfile.GetCasterDisplayName(profile.CasterMaterial));
}
```

Run the same focused test command and expect GREEN (subject to the documented environment resource limit).

- [ ] **Step 3: Add summary labels and move the identity/rank controls into the preview column**

Add labels for set, armor, mask, greaves, cape, bracer, and caster. Build the preview column as a vertical container with the existing preview panel, name/age rows, rank row, rotation controls, `_previewWithoutGear`, and a compact summary panel. Remove the old top-level identity, rank, and color rows; keep `_skinGrid` and `_eyeGrid` in the Appearance page from Task 2.

- [ ] **Step 4: Implement the summary binding with the pure helper and existing profile display names**

Use the existing helpers rather than duplicating display-name logic:

```csharp
private void UpdateSelectionSummary(YautjaCharacterProfile yautja)
{
    var summary = YautjaProfileEditorLayout.BuildSummary(yautja);
    _summarySet.Text = Loc.GetString("cmu-yautja-lobby-summary-set", ("value", summary.Set == "—"
        ? Loc.GetString("cmu-yautja-lobby-summary-custom")
        : summary.Set));
    _summaryArmor.Text = Loc.GetString(
        "cmu-yautja-lobby-summary-armor",
        ("value", summary.Armor));
    _summaryMask.Text = Loc.GetString(
        "cmu-yautja-lobby-summary-mask",
        ("value", summary.Mask));
    _summaryGreaves.Text = Loc.GetString(
        "cmu-yautja-lobby-summary-greaves",
        ("value", summary.Greaves));
    _summaryCape.Text = Loc.GetString(
        "cmu-yautja-lobby-summary-cape",
        ("value", summary.Cape));
    _summaryBracer.Text = Loc.GetString(
        "cmu-yautja-lobby-summary-bracer",
        ("value", summary.Bracer));
    _summaryCaster.Text = Loc.GetString(
        "cmu-yautja-lobby-summary-caster",
        ("value", summary.Caster));
}
```

- [ ] **Step 5: Update summary from both profile entry points**

Call `UpdateSelectionSummary(yautja)` in `SetProfile` after `RebuildVisualSelectors(yautja)` and in `Mutate` after `_profile` receives the updated profile. Do not create a second profile-change event and do not change `ReloadPreview`.

- [ ] **Step 6: Build the client and verify the summary binding compiles**

Run: `dotnet build Content.Client/Content.Client.csproj --no-restore`

Expected: PASS, with all existing `Mutate` callbacks still compiling and the preview column containing the identity controls.

- [ ] **Step 7: Commit preview column and summary**

```powershell
git add -- Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs
git diff --cached --check
git diff --cached --stat
git commit -m "feat: show Yautja loadout summary beside preview"
```

---

### Task 4: Make selector cards informative and expose locked unique sets

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs:421-450` for unique-set rendering.
- Modify: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs:774-865` for selector labels and disabled state.

**Interfaces:**
- Consumes `YautjaProfileEditorLayout.IsUniqueSetLocked` from Task 1.
- Keeps all existing selector callbacks and `ButtonGroup` selection behavior.

- [ ] **Step 1: Change the selector helper so visual cards show their tooltip text by default**

In `AddEntitySelector`, set `label ??= tooltip` before calculating the labeled button size. Keep the optional explicit label for caster/bracer cards. This gives armor, masks, greaves, capes, legacy, and unique cards visible labels without changing any selection callback.

- [ ] **Step 2: Extend `AddEntitySelector` with a disabled parameter**

Use this exact signature shape:

```csharp
private void AddEntitySelector(
    GridContainer grid,
    ButtonGroup group,
    string prototype,
    bool selected,
    string tooltip,
    Action onPressed,
    string? label = null,
    bool disabled = false)
```

After `BuildSelectorButton`, set `button.Disabled = disabled`. Keep the callback attached; Robust UI will not invoke it while disabled, and the button remains in the grid so the user can understand why it cannot be selected.

- [ ] **Step 3: Render every unique option and lock it below Elite**

Replace the current `continue` in `RebuildUniqueSelector` with a locked path. Use `YautjaRank.Elite` as the minimum rank because `YautjaRankMetadata.For` marks Elite and higher as `UniqueSetsAllowed`:

```csharp
var locked = YautjaProfileEditorLayout.IsUniqueSetLocked(yautja, unique);
var tooltip = locked
    ? Loc.GetString(
        "cmu-yautja-lobby-locked-rank",
        ("rank", Loc.GetString(YautjaRankMetadata.For(YautjaRank.Elite).LocalizedName)))
    : YautjaCharacterProfile.GetUniqueDisplayName(unique);

AddEntitySelector(
    _uniqueGrid,
    group,
    preview,
    selected,
    tooltip,
    () => Mutate(profile => profile.WithUnique(unique).WithLegacy(YautjaLegacySet.None), true),
    YautjaCharacterProfile.GetUniqueDisplayName(unique),
    locked);
```

Keep the `None` option active and keep the existing mutual exclusion behavior with legacy sets. The disabled button must never call `Mutate`.

- [ ] **Step 4: Build and run the focused layout tests**

Run: `dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaProfileEditorLayoutTest`

Then run: `dotnet build Content.Client/Content.Client.csproj --no-restore`

Expected: PASS, with all unique options present in the rendered grid and locked below Elite.

- [ ] **Step 5: Commit card labels and locked-state rendering**

```powershell
git add -- Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs
git diff --cached --check
git diff --cached --stat
git commit -m "feat: clarify Yautja selector cards and rank locks"
```

---

### Task 5: Add the remaining English and Russian localization

**Files:**
- Modify: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`

- [ ] **Step 1: Add the remaining summary and locked-state keys to both locale files**

Task 1 already added the five `cmu-yautja-lobby-category-*` keys required by the catalog. Do not duplicate those existing entries. Add the remaining keys below once in each locale file:

Add these English keys:

```ftl
cmu-yautja-lobby-summary-set = Set: {$value}
cmu-yautja-lobby-summary-custom = Custom
cmu-yautja-lobby-summary-armor = Armor: {$value}
cmu-yautja-lobby-summary-mask = Mask: {$value}
cmu-yautja-lobby-summary-greaves = Greaves: {$value}
cmu-yautja-lobby-summary-cape = Cape: {$value}
cmu-yautja-lobby-summary-bracer = Bracer: {$value}
cmu-yautja-lobby-summary-caster = Caster: {$value}
cmu-yautja-lobby-locked-rank = Requires rank: {$rank}
```

Add the Russian equivalents with the same identifiers and variables:

```ftl
cmu-yautja-lobby-summary-set = Набор: {$value}
cmu-yautja-lobby-summary-custom = Настроенный комплект
cmu-yautja-lobby-summary-armor = Броня: {$value}
cmu-yautja-lobby-summary-mask = Маска: {$value}
cmu-yautja-lobby-summary-greaves = Поножи: {$value}
cmu-yautja-lobby-summary-cape = Плащ: {$value}
cmu-yautja-lobby-summary-bracer = Браслет: {$value}
cmu-yautja-lobby-summary-caster = Кастер: {$value}
cmu-yautja-lobby-locked-rank = Требуется ранг: {$rank}
```

- [ ] **Step 2: Verify key parity and localization compilation**

Run: `rg -n "cmu-yautja-lobby-(category|summary|locked-rank)" Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`

Expected: every key listed above appears once in each locale with the same variable names.

Then run: `dotnet build Content.Client/Content.Client.csproj --no-restore`

- [ ] **Step 3: Commit localization separately**

```powershell
git add -- Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl
git diff --cached --check
git diff --cached --stat
git commit -m "loc: add Yautja personalization UI labels"
```

---

### Task 6: Run regression tests and perform visual QA

**Files:**
- Inspect only: `Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs`
- Inspect only: `Content.Client/Lobby/UI/HumanoidProfileEditor.xaml.cs`
- Inspect only: `Resources/Locale/en-US/_CMU14/yautja/yautja.ftl`
- Inspect only: `Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl`

- [ ] **Step 1: Run focused unit tests and client build**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~YautjaProfileEditorLayoutTest
dotnet build Content.Client/Content.Client.csproj --no-restore
```

Expected: both commands exit 0.

- [ ] **Step 2: Run the existing Yautja integration slice if the checkout supports it**

Run: `dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Yautja"`

Expected: existing Yautja behavior remains green. If unrelated pre-existing changes cause failures, record the exact failing test and do not alter unrelated files to make it pass.

- [ ] **Step 3: Manually verify the lobbiescreen at normal and narrow widths**

Launch the client with the repository's normal lobby command (`.\runclient.bat` or the configured development launcher) and verify:

1. Preview, name, age, rank icon/name, rotation buttons, and gear toggle stay visible on the left.
2. Appearance contains skin, eyes, and quills; Equipment contains armor, mask, mask accessory, greaves, bracer, caster, and cape; Sets contains both legacy and unique; Technology contains both options; Description contains flavor text.
3. Clicking each navigation button changes only the right page and preserves previous choices.
4. Changing a selector updates the preview and summary immediately.
5. Unique cards are visible but disabled below Elite and show the required-rank tooltip; Elite and above can select them.
6. Bracer/caster filters still filter their own grids and preserve selected material.
7. Narrowing the window reduces grid columns without horizontal overflow; only the active right page scrolls.
8. Editing any field still marks the parent profile dirty and the profile remains intact after switching away and back to the Yautja tab.

- [ ] **Step 4: Review the final diff and working tree**

Run:

```powershell
git status --short
git diff --check HEAD
git diff HEAD -- Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditor.cs Content.Client/_CMU14/Yautja/Lobby/YautjaProfileEditorLayout.cs Content.Tests/Client/_CMU14/Yautja/YautjaProfileEditorLayoutTest.cs Resources/Locale/en-US/_CMU14/yautja/yautja.ftl Resources/Locale/ru-RU/_CMU14/yautja/yautja.ftl
```

Expected: only the planned UI/helper/test/localization changes are shown for this task; unrelated pre-existing worktree changes remain untouched.

- [ ] **Step 5: Commit any final verified fix as a focused change**

Use a specific commit message matching the actual fix, for example:

```powershell
git add -- <only-the-verified-files>
git diff --cached --check
git commit -m "fix: polish Yautja personalization layout"
```

---

## Self-review checklist

- Spec coverage: the plan covers the two-column layout, five navigation groups, live summary, labels, locked unique cards, localization parity, dirty-state preservation, narrow-width behavior, build, tests, and manual visual QA.
- Placeholder scan: no `TBD`, `TODO`, `FIXME`, or vague "add appropriate" steps are used; every implementation step names files, APIs, or concrete UI behavior.
- Type consistency: the catalog API used by the tests and editor is defined in Task 1; `AddCategory`, `SelectCategory`, and `UpdateSelectionSummary` are introduced before later tasks rely on them; `YautjaProfileEditorLayout.IsUniqueSetLocked` is the single locked predicate.
- Scope: the plan changes only the client editor, one small client layout helper, tests, and two locale files; it does not change shared/server contracts or unrelated dirty files.
