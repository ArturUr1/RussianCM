using Content.Server._CMU14.Yautja;
using Content.Shared._CMU14.Yautja;
using NUnit.Framework;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaClanAdminStateStoreTest
{
    [Test]
    public void ReturnsCachedStateWithoutLoadingDatabase()
    {
        var store = new YautjaClanAdminStateStore();
        var state = new YautjaClanAdminEuiState(
            [],
            "player",
            "summary",
            "status",
            4,
            12,
            YautjaClanAdminMutationKind.Updated);

        store.Set(state);

        Assert.That(store.Get(), Is.SameAs(state));
    }
}
