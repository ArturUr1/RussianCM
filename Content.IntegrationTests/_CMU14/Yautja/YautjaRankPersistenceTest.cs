using Content.Server.Database;
using Content.Server._CMU14.Yautja;
using Content.Shared._CMU14.Yautja;
using Robust.Shared.Network;

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

    [TestCase(0, 0, true)]
    [TestCase(1, 1, true)]
    [TestCase(1, 2, false)]
    public void StaleDatabaseResultsCannotUpdateNewerCacheVersion(
        long requestVersion,
        long currentVersion,
        bool expectedCurrent)
    {
        Assert.That(
            YautjaRankManager.IsCacheVersionCurrent(requestVersion, currentVersion),
            Is.EqualTo(expectedCurrent));
    }

    [Test]
    public void InvalidatedClanResolutionRejectsStaleInFlightCompletion()
    {
        var versions = new YautjaClanCacheVersions();
        var userId = new NetUserId(Guid.NewGuid());
        var inFlightVersion = versions.Capture(userId);

        versions.Increment(userId);

        Assert.That(versions.IsCurrent(userId, inFlightVersion), Is.False);
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

    [Test]
    public async Task ResolveCachedLoadsAuthoritativeRankOnCacheMiss()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = pair.Server.ResolveDependency<IServerDbManager>();
        var manager = pair.Server.ResolveDependency<YautjaRankManager>();
        var userId = pair.Player!.UserId;

        await db.SetYautjaRank(userId.UserId, YautjaRank.Elder);

        Assert.That(manager.ResolveCached(userId), Is.EqualTo(YautjaRank.Elder));
        await pair.CleanReturnAsync();
    }
}
