using Content.Server.Database;
using Content.Server._CMU14.Yautja;
using Content.Shared._CMU14.Yautja;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaRankPersistenceTest
{
    [TestCase(null, YautjaRank.Blooded)]
    [TestCase(YautjaRank.YoungBlood, YautjaRank.Blooded)]
    [TestCase((YautjaRank) 99, YautjaRank.Blooded)]
    [TestCase(YautjaRank.Unblooded, YautjaRank.Unblooded)]
    [TestCase(YautjaRank.Ancient, YautjaRank.Ancient)]
    public void StoredRankSanitizationKeepsYoungbloodRoleSeparate(YautjaRank? stored, YautjaRank expected)
    {
        Assert.That(YautjaRankManager.Sanitize(stored), Is.EqualTo(expected));
    }

    [Test]
    public async Task RankRoundTripsThroughSqlite()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = pair.Server.ResolveDependency<IServerDbManager>();
        var userId = pair.Player!.UserId.UserId;

        await db.SetYautjaRank(userId, YautjaRank.Elder);

        Assert.That(await db.GetYautjaRank(userId), Is.EqualTo(YautjaRank.Elder));
        await pair.CleanReturnAsync();
    }
}
