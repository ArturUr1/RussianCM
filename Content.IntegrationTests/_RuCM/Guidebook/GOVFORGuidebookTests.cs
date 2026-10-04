using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Content.Client._RuCM.Guidebook;
using Content.Client.Guidebook.Controls;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Guidebook;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._RuCM.Guidebook;

[TestFixture, NonParallelizable]
public sealed class GOVFORGuidebookTests : GameTest
{
    private static int _ttsOverrideIndex;

    public override PoolSettings PoolSettings => new() { InLobby = true, Fresh = true, Destructive = true };

    [OneTimeSetUp]
    public static void ConfigureLobbyFixture()
    {
        // The full lobby announces joins before per-test overrides. This fixture has no external TTS provider.
        _ttsOverrideIndex = PoolManager.Instance.DefaultCvars.Count;
        PoolManager.Instance.DefaultCvars.Add((CCCVars.TTSEnabled.Name, "false"));
    }

    [OneTimeTearDown]
    public static void RestoreLobbyFixture()
    {
        PoolManager.Instance.DefaultCvars.RemoveAt(_ttsOverrideIndex);
    }

    [TestCase("ru-RU")]
    [TestCase("en-US")]
    public async Task NormalGuidebookShowsCharterButIndividualBooksRemainScoped(string culture)
    {
        await Client.WaitAssertion(() =>
        {
            var loc = Client.ResolveDependency<ILocalizationManager>();
            loc.DefaultCulture = CultureInfo.GetCultureInfo(culture, predefinedOnly: false);
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var controller = ui.GetUIController<GuidebookUIController>();
            controller.OpenGuidebook();
            var window = ui.WindowRoot.Children.OfType<GuidebookWindow>().Single();
            var drill = window.Tree.Items.Single(i => i.Metadata is GuideEntry e &&
                e.Id == GOVFORGuidebookSystem.DrillRegulations);
            Assert.That(drill.Label.Text, Is.EqualTo(loc.GetString("rucm-guide-entry-govfor-drill-regulations")));
            Assert.That(window.Tree.Items.Any(i => i.Metadata is GuideEntry e && e.Id == "AU14SOP"), Is.True);
            Assert.That(window.Tree.Items.Any(i => i.Metadata is GuideEntry e && e.Id == "AU14UCMJ"), Is.True);
            window.Tree.SetSelectedIndex(drill.Index);
            Assert.That(window.Selected?.Id, Is.EqualTo(GOVFORGuidebookSystem.DrillRegulations));
            Assert.That(Descendants(window).OfType<GuidebookError>(), Is.Empty);
            Assert.That(Descendants(window).OfType<Label>().Select(i => i.Text), Does.Contain("Строевая подготовка"));
            Assert.That(Descendants(window).OfType<Label>().Select(i => i.Text), Does.Contain("Дух Устава"));

            window.Close();
            controller.OpenGuidebook();
            Assert.That(window.Tree.Items.Count(i => i.Metadata is GuideEntry e &&
                e.Id == GOVFORGuidebookSystem.DrillRegulations), Is.EqualTo(1));
            // The upstream controller resets an unknown root to its default when reopening.
            Assert.That(window.Selected?.Id, Is.EqualTo("CMUGuidebook"));

            window.Close();
            controller.OpenGuidebook(new List<ProtoId<GuideEntryPrototype>> { "AU14UCMJ" }, selected: "AU14UCMJ");
            Assert.That(window.Tree.Items.Any(i => i.Metadata is GuideEntry e &&
                e.Id == GOVFORGuidebookSystem.DrillRegulations), Is.False);
            Assert.That(window.Selected?.Id, Is.EqualTo("AU14UCMJ"));

            window.Close();
            controller.OpenGuidebook();
            Assert.That(window.Tree.Items.Count(i => i.Metadata is GuideEntry e &&
                e.Id == GOVFORGuidebookSystem.DrillRegulations), Is.EqualTo(1));
            Assert.That(window.Selected?.Id, Is.EqualTo("AU14UCMJ"));
            window.Close();
        });
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}
