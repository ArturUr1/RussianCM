using Content.Server._CMU14.Yautja;
using Content.Shared._CMU14.Yautja;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaRankSpawnTest
{
    [TestCase(YautjaRank.Unblooded, false)]
    [TestCase(YautjaRank.Blooded, false)]
    [TestCase(YautjaRank.Elite, false)]
    [TestCase(YautjaRank.Elder, false)]
    [TestCase(YautjaRank.Leader, true)]
    [TestCase(YautjaRank.Ancient, true)]
    public void NormalRanksUseHunterShipAndOnlySeniorRanksBypassSlots(YautjaRank rank, bool bypass)
    {
        var policy = YautjaPredatorRoundSystem.GetRankSpawnPolicy(rank);

        Assert.Multiple(() =>
        {
            Assert.That(policy.SpawnKind, Is.EqualTo(YautjaSpawnKind.HunterShipClan));
            Assert.That(policy.BypassSlotCap, Is.EqualTo(bypass));
        });
    }

    [Test]
    public void YoungbloodKeepsSpecialHuntingGroundSpawn()
    {
        var policy = YautjaPredatorRoundSystem.GetRankSpawnPolicy(YautjaRank.YoungBlood);

        Assert.Multiple(() =>
        {
            Assert.That(policy.SpawnKind, Is.EqualTo(YautjaSpawnKind.HuntingGroundsYoungblood));
            Assert.That(policy.BypassSlotCap, Is.False);
        });
    }

    [Test]
    public void InvalidRankFallsBackToBloodedHunterShipPolicy()
    {
        var policy = YautjaPredatorRoundSystem.GetRankSpawnPolicy((YautjaRank) 99);

        Assert.Multiple(() =>
        {
            Assert.That(policy.SpawnKind, Is.EqualTo(YautjaSpawnKind.HunterShipClan));
            Assert.That(policy.BypassSlotCap, Is.False);
        });
    }
}
