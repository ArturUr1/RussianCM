using System.Linq;
using Content.Server.Database;
using Content.Shared._CMU14.Yautja;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaClanMutationPersistenceTest
{
    [Test]
    public async Task UpdateChangesEditableFieldsOnly()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = pair.Server.ResolveDependency<IServerDbManager>();
        var clanId = await db.CreateYautjaClanAsync("Old", "Old description", 42, "#111111");

        var updated = await db.UpdateYautjaClanAsync(clanId, "New", "New description", "#AABBCC");
        var clan = await db.GetYautjaClanAsync(clanId);

        Assert.Multiple(() =>
        {
            Assert.That(updated, Is.True);
            Assert.That(clan, Is.Not.Null);
            Assert.That(clan!.Name, Is.EqualTo("New"));
            Assert.That(clan.Description, Is.EqualTo("New description"));
            Assert.That(clan.Color, Is.EqualTo("#AABBCC"));
            Assert.That(clan.Honor, Is.EqualTo(42));
            Assert.That(clan.Active, Is.True);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task UpdateRejectsInactiveAndMissingClans()
    {
        await using var pair = await PoolManager.GetServerClient();
        var db = pair.Server.ResolveDependency<IServerDbManager>();
        var inactiveId = await db.CreateYautjaClanAsync(
            "Inactive",
            "Inactive description",
            0,
            "#111111",
            active: false);

        var inactiveUpdated =
            await db.UpdateYautjaClanAsync(inactiveId, "Changed", "Changed", "#222222");
        var missingUpdated =
            await db.UpdateYautjaClanAsync(int.MaxValue, "Missing", "Missing", "#333333");

        Assert.Multiple(() =>
        {
            Assert.That(inactiveUpdated, Is.False);
            Assert.That(missingUpdated, Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DeleteDeactivatesClanAndDetachesMemberWithoutChangingPersistentData()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var db = pair.Server.ResolveDependency<IServerDbManager>();
        var playerId = pair.Player!.UserId.UserId;
        var clanId = await db.CreateYautjaClanAsync("Delete me", "Deletion test", 7, "#123456");
        await db.UpsertYautjaClanMemberAsync(new YautjaClanMemberRecord(
            playerId,
            clanId,
            (int) YautjaRank.Elder,
            (int) (YautjaClanPermission.UserModify | YautjaClanPermission.UserView),
            13,
            true));

        var first = await db.DeactivateYautjaClanAsync(clanId);
        var second = await db.DeactivateYautjaClanAsync(clanId);
        var clan = await db.GetYautjaClanAsync(clanId);
        var member = await db.GetYautjaClanMemberAsync(playerId);
        var activeClans = await db.GetYautjaClansAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.DetachedPlayers, Is.EqualTo(new[] { playerId }));
            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.DetachedPlayers, Is.Empty);
            Assert.That(clan!.Active, Is.False);
            Assert.That(activeClans.All(entry => entry.Id != clanId), Is.True);
            Assert.That(member!.ClanId, Is.Null);
            Assert.That(member.Rank, Is.EqualTo((int) YautjaRank.Elder));
            Assert.That(member.Permissions,
                Is.EqualTo((int) (YautjaClanPermission.UserModify | YautjaClanPermission.UserView)));
            Assert.That(member.Honor, Is.EqualTo(13));
            Assert.That(member.IsLegacy, Is.True);
        });

        await pair.CleanReturnAsync();
    }
}
