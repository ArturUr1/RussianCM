# Прокручиваемые меню выбора Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ограничить высоту списка вариантов в общем RMC-диалоге и обеспечить прокрутку длинных меню выбора.

**Architecture:** Серверные `DialogOption` и сетевые сообщения не меняются. Клиентский `RMCDialogOptionsContainer` оставляет сообщение и поиск вне области прокрутки, а `ScrollContainer` с фиксированным лимитом высоты содержит только вертикальный список кнопок.

**Tech Stack:** C#, NUnit, Robust UI/XAML, .NET 8.

## Global Constraints

- Изменять только общий клиентский контейнер вариантов и его тест.
- Не менять порядок, индексы и обработку `DialogOption`.
- Сохранять горизонтальную прокрутку выключенной и вертикальную включённой.
- Не затрагивать существующие незакоммиченные пользовательские изменения.

---

### Task 1: Зафиксировать контракт разметки тестом

**Files:**
- Create: `Content.Tests/Client/_RMC14/Dialog/RMCDialogOptionsContainerTest.cs`

**Interfaces:**
- Consumes: `Content.Client._RMC14.Dialog.RMCDialogOptionsContainer`.
- Produces: regression test that requires a vertical-only scroll container with `MaxHeight == 300` around `Options`.

- [x] **Step 1: Write the failing test**

```csharp
using Content.Client._RMC14.Dialog;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.UnitTesting;

namespace Content.Tests.Client._RMC14.Dialog;

[TestFixture]
[TestOf(typeof(RMCDialogOptionsContainer))]
public sealed class RMCDialogOptionsContainerTest : ContentUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<IUserInterfaceManager>().InitializeTesting();
    }

    [Test]
    public void OptionsUseBoundedVerticalScrollContainer()
    {
        var container = new RMCDialogOptionsContainer();

        Assert.That(container.Options.Parent, Is.TypeOf<ScrollContainer>());

        var scroll = (ScrollContainer) container.Options.Parent!;
        Assert.That(scroll.HScrollEnabled, Is.False);
        Assert.That(scroll.VScrollEnabled, Is.True);
        Assert.That(scroll.MaxHeight, Is.EqualTo(300));
    }
}
```

- [x] **Step 2: Run the test to verify it fails**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~RMCDialogOptionsContainerTest
```

Expected: the test fails because the current XAML caps the options scroll container at `500`, not `300`.

### Task 2: Apply the shared scroll layout

**Files:**
- Modify: `Content.Client/_RMC14/Dialog/RMCDialogOptionsContainer.xaml:9-11`

**Interfaces:**
- Consumes: the existing `Options` button container used by `DialogBui.UpdateOptions`.
- Produces: a vertical-only scroll area capped at `300` pixels; no server or message protocol changes.

- [x] **Step 1: Change only the options scroll bounds**

```xml
<ScrollContainer HScrollEnabled="False" VScrollEnabled="True" HorizontalExpand="True" VerticalExpand="True" MinHeight="200" MaxHeight="300">
    <BoxContainer Name="Options" Access="Public" Orientation="Vertical" />
</ScrollContainer>
```

- [x] **Step 2: Run the focused test to verify it passes**

Run:

```powershell
dotnet test Content.Tests/Content.Tests.csproj --no-restore --filter FullyQualifiedName~RMCDialogOptionsContainerTest
```

Expected: PASS; the existing button generation and selection index path remain unchanged.

### Task 3: Verify the deliverable

**Files:**
- Test: `Content.Tests/Client/_RMC14/Dialog/RMCDialogOptionsContainerTest.cs`
- Modify: `Content.Client/_RMC14/Dialog/RMCDialogOptionsContainer.xaml`

- [x] **Step 1: Build the client and server**

Run:

```powershell
dotnet build Content.Client/Content.Client.csproj --no-restore
dotnet build Content.Server/Content.Server.csproj --no-restore
```

Expected: both builds succeed without errors.

- [x] **Step 2: Review the diff and working tree**

Run:

```powershell
git diff --check
git status --short
```

Expected: only the new focused test and the shared dialog XAML are part of this feature change; unrelated pre-existing changes remain untouched.

- [x] **Step 3: Commit the feature files**

```powershell
git add -- Content.Tests/Client/_RMC14/Dialog/RMCDialogOptionsContainerTest.cs Content.Client/_RMC14/Dialog/RMCDialogOptionsContainer.xaml
git commit -m "fix: scroll long option dialogs"
```
