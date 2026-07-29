using System.Collections.Generic;
using System.Linq;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.Administration.UI.Tabs.AdminTab;
using Robust.Client.UserInterface;
using Robust.Shared.Localization;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaClanAdminEntryTest
{
    [Test]
    public async Task AdminTabProvidesLocalizedClanAdministrationCommand()
    {
        await using var pair = await PoolManager.GetServerClient();

        await pair.Client.WaitAssertion(() =>
        {
            var localization = pair.Client.ResolveDependency<ILocalizationManager>();
            var tab = new AdminTab();
            try
            {
                var button = Descendants(tab)
                    .OfType<CommandButton>()
                    .SingleOrDefault(entry => entry.Command == "yautja_clan_admin");

                Assert.That(button, Is.Not.Null);
                Assert.That(button!.Text, Is.EqualTo(localization.GetString("cmu-yautja-clan-admin-open")));
            }
            finally
            {
                tab.DisposeAllChildren();
            }
        });

        await pair.CleanReturnAsync();
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children)
        {
            yield return child;

            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
