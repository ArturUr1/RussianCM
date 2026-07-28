using Content.Client._CMU14.Yautja;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.UnitTesting;

namespace Content.Tests.Client._CMU14.Yautja;

[TestFixture]
public sealed class YautjaClanAdminWindowTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    [OneTimeSetUp]
    public void Setup()
    {
        IoCManager.Resolve<IUserInterfaceManager>().InitializeTesting();
    }

    [Test]
    public void SelectorSelectionUpdatesSelectedIdUsedByAdminAction()
    {
        var option = new OptionButton();
        option.AddItem("Blooded", 1);
        option.AddItem("Ancient", 6);

        YautjaClanAdminWindow.ApplySelectorSelection(
            option,
            new OptionButton.ItemSelectedEventArgs(6, option));

        Assert.That(option.SelectedId, Is.EqualTo(6));
    }

    [Test]
    public void ContextualTooltipIsAppliedToControl()
    {
        var field = new LineEdit();

        YautjaClanAdminWindow.ApplyTooltip(field, "cmu-yautja-clan-admin-name-tooltip");

        Assert.That(field.ToolTip, Is.EqualTo(Loc.GetString("cmu-yautja-clan-admin-name-tooltip")));
    }

    [Test]
    public void DefaultWindowSizeIsCompact()
    {
        Assert.That(YautjaClanAdminWindow.DefaultWindowSize.X, Is.LessThanOrEqualTo(760));
        Assert.That(YautjaClanAdminWindow.DefaultWindowSize.Y, Is.LessThanOrEqualTo(560));
    }
}
