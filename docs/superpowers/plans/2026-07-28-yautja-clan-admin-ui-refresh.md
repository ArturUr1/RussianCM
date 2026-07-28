# Yautja Clan Admin UI Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize the Yautja clan administration window into clear clan, player, and existing-clan sections with localized contextual hints while preserving all current actions and server contracts.

**Architecture:** Keep `YautjaClanAdminWindow` as the single client view and keep its existing events unchanged. Build the visual hierarchy with `YautjaBracerUiStyle.Section`, small labeled field rows, wrapped clan cards, and localized tooltips. Extend only the client localization resources and the existing client test; no server or network protocol changes are needed.

**Tech Stack:** C# / RobustToolbox UI controls, Fluent localization (`.ftl`), NUnit client unit tests, `dotnet test`, `dotnet build`.

## Global Constraints

- Preserve the existing `YautjaClanAdminWindow` events and all server-side validation/permission behavior.
- Use `YautjaBracerUiStyle.Section`, muted labels, accent borders, and compact actions; do not introduce a new theme.
- Add every user-facing string to both Russian and English localization resources; do not add hard-coded English UI text.
- Keep the window usable at its current minimum size and keep the clan list independently scrollable.
- Every `OptionButton` used by the clan workflows must update `SelectedId` on `OnItemSelected`.
- Preserve unsent editor drafts when state refreshes and keep delete behind explicit confirmation.

---

### Task 1: Add a failing client UI contract test

**Files:**
- Modify: `Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminWindowTest.cs`

**Interfaces:**
- Consumes: `YautjaClanAdminWindow`, `RobustUnitTest`, `IUserInterfaceManager.InitializeTesting()`.
- Produces: a regression test that fails until the redesigned window exposes localized tooltips on its fields and actions.

- [ ] **Step 1: Write the failing test**

Add a focused tooltip helper test like:

```csharp
[Test]
public void ContextualTooltipIsAppliedToControl()
{
    var field = new LineEdit();
    YautjaClanAdminWindow.ApplyTooltip(field, "cmu-yautja-clan-admin-name-tooltip");
    Assert.That(field.ToolTip, Is.EqualTo(Loc.GetString("cmu-yautja-clan-admin-name-tooltip")));
}
```

Add the required `Robust.Client.UserInterface`, `Robust.Client.UserInterface.Controls`, and `Robust.Shared.Localization` imports if they are not already present.

- [ ] **Step 2: Run the test to verify RED**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminWindowTest.ContextualTooltipIsAppliedToControl"
```

Expected result: compilation fails because `YautjaClanAdminWindow.ApplyTooltip` is not implemented yet.

- [ ] **Step 3: Commit the failing test**

```powershell
git add Content.Tests/Client/_CMU14/Yautja/YautjaClanAdminWindowTest.cs
git commit -m "test: define Yautja clan admin tooltip contract"
```

### Task 2: Add localized section hints, tooltips, and row copy

**Files:**
- Modify: `Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl`
- Modify: `Resources/Locale/en-US/_CMU14/yautja/admin.ftl`

**Interfaces:**
- Consumes: existing `cmu-yautja-clan-admin-*` localization ids.
- Produces: the exact ids referenced by the window implementation and the failing test.

- [ ] **Step 1: Add Russian strings**

Add localized ids for the three section titles, section hints, field tooltips, selector tooltips, and action tooltips. The Russian values must communicate:

```text
cmu-yautja-clan-admin-section-clan = Клан
cmu-yautja-clan-admin-section-player = Операции с игроком
cmu-yautja-clan-admin-section-existing = Существующие кланы
cmu-yautja-clan-admin-name-tooltip = Обязательное короткое отображаемое название клана.
cmu-yautja-clan-admin-description-tooltip = Обязательное описание, которое увидят участники клана.
cmu-yautja-clan-admin-color-tooltip = Цвет в формате #RRGGBB, например #C62D2D.
cmu-yautja-clan-admin-player-tooltip = Введите имя игрока или UserId.
cmu-yautja-clan-admin-clan-id-tooltip = Укажите существующий числовой ID или none, чтобы отвязать игрока.
cmu-yautja-clan-admin-membership-rank-tooltip = Ранг, который будет установлен вместе с членством.
cmu-yautja-clan-admin-rank-tooltip = Постоянный ранг, который будет назначен игроку.
cmu-yautja-clan-admin-whitelist-tooltip = Группа доступа whitelist для игрока.
cmu-yautja-clan-admin-inspect-tooltip = Обновить сводку по указанному игроку.
cmu-yautja-clan-admin-edit-tooltip = Загрузить этот клан в форму редактирования.
cmu-yautja-clan-admin-delete-tooltip = Удалить клан после подтверждения и отвязать всех участников.
cmu-yautja-clan-admin-refresh-tooltip = Загрузить актуальное состояние кланов с сервера.
cmu-yautja-clan-admin-clan-section-hint = Создайте клан или загрузите существующий для изменения названия, описания и цвета.
cmu-yautja-clan-admin-player-section-hint = Сначала укажите игрока, затем выберите нужную операцию и её параметры.
cmu-yautja-clan-admin-existing-section-hint = Редактирование загружает клан в форму; удаление отвязывает участников после подтверждения.
```

- [ ] **Step 2: Add matching English strings**

Add the same ids to the English resource with concise equivalents, including `#RRGGBB` and `none` examples, so the C# code uses identical keys in both locales.

- [ ] **Step 3: Validate localization keys**

Run:

```powershell
rg -n "cmu-yautja-clan-admin-(section|.*tooltip|.*section-hint)" Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl Resources/Locale/en-US/_CMU14/yautja/admin.ftl
```

Expected result: every id used by the planned window appears once in each locale.

- [ ] **Step 4: Commit localization changes**

```powershell
git add Resources/Locale/ru-RU/_CMU14/yautja/admin_clan.ftl Resources/Locale/en-US/_CMU14/yautja/admin.ftl
git commit -m "feat: add Yautja clan admin UI hints"
```

### Task 3: Rebuild the window hierarchy around the three workflows

**Files:**
- Modify: `Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs`

**Interfaces:**
- Consumes: localization ids from Task 2 and existing public events/state types.
- Produces: the redesigned window without changing event signatures or server messages.

- [ ] **Step 1: Build the clan editor section**

Replace the direct children currently added to `root` with a `YautjaBracerUiStyle.Section` titled by `cmu-yautja-clan-admin-section-clan`. Add the section hint, keep `_clanFormHeader` dynamic for create/edit mode, put name/description/color in labeled rows, set each field tooltip, and place `_submitClan` plus `_cancelClan` in one action row. Keep `SyncEditorControls()` unchanged in behavior.

- [ ] **Step 2: Build the player operations section**

Use a second `YautjaBracerUiStyle.Section` titled by `cmu-yautja-clan-admin-section-player`. Add the section hint, put `_player` and `_clanId` in a labeled identity row, keep membership assignment in its own row, and put rank/whitelist/inspect actions in a separate row. Apply the player, clan id, membership-rank, rank, whitelist, and inspect tooltips to the corresponding controls/buttons. Keep all existing event invocations exactly as they are.

- [ ] **Step 3: Build the existing clans section**

Use a third `YautjaBracerUiStyle.Section` titled by `cmu-yautja-clan-admin-section-existing`. Add the section hint, keep the `ScrollContainer` as the independently expanding list area, and render each clan as a wrapped card. The card must show a primary title (`#id name`), secondary metadata (members, honor, color), a neutral edit button, and a destructive delete button. Apply the edit/delete tooltips and preserve the current delete confirmation callback.

- [ ] **Step 4: Keep status and refresh discoverable**

Place `_status` and the refresh button in a compact footer row inside the existing-clans section. Set the refresh tooltip and retain `OnRefresh`. Keep empty inspection text and status text readable with muted Yautja styling; do not change their state semantics.

- [ ] **Step 5: Preserve selector correctness**

Keep `CreateRankOption()` and `AddWhitelistOptions()` wired to `ApplySelectorSelection`. If any new selector is introduced while reorganizing rows, attach:

```csharp
selector.OnItemSelected += args => selector.SelectId(args.Id);
```

- [ ] **Step 6: Run the focused test to verify GREEN**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdminWindowTest"
```

Expected result: all Yautja clan admin window tests pass, including the tooltip contract.

- [ ] **Step 7: Commit the UI implementation**

```powershell
git add Content.Client/_CMU14/Yautja/YautjaClanAdminWindow.cs
git commit -m "feat: reorganize Yautja clan admin window"
```

### Task 4: Verify build and runtime workflow

**Files:**
- No additional source files; verification covers the files from Tasks 1–3.

**Interfaces:**
- Consumes: the completed client UI and localized resources.
- Produces: verified build/test evidence and a running local client/server pair.

- [ ] **Step 1: Build the client**

Run:

```powershell
dotnet build Content.Client/Content.Client.csproj --no-restore
```

Expected result: build succeeds with zero errors.

- [ ] **Step 2: Run focused clan tests**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter "FullyQualifiedName~YautjaClanAdmin"
```

Expected result: all matching tests pass.

- [ ] **Step 3: Smoke-test the running workflow**

With the local server and client connected, check: create mode and hints, edit/cancel transition, rank and whitelist selection followed by action buttons, inspect status, refresh, edit loading, and delete confirmation. Confirm that the list scrolls without hiding the action sections.

- [ ] **Step 4: Review the final diff**

Run:

```powershell
git diff --check HEAD~3..HEAD
git status --short
```

Expected result: no whitespace errors; unrelated pre-existing worktree changes remain untouched.
