using System;
using System.Collections.Generic;
using Content.Shared._CMU14.Yautja;
using NUnit.Framework;
using Robust.Shared.Network;

namespace Content.Tests.Shared._CMU14.Yautja;

[TestFixture]
public sealed class YautjaClanAdminEuiStateTest
{
    [Test]
    public void ClanStateRetainsRosterMemberData()
    {
        var playerId = new NetUserId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var member = new YautjaClanAdminMemberState(playerId, "Huntmaster", YautjaRank.Leader, true);
        var clan = new YautjaClanAdminClanState(
            7,
            "Blood Moon",
            "A test clan",
            12,
            "#C62D2D",
            1,
            new List<YautjaClanAdminMemberState> { member });

        Assert.Multiple(() =>
        {
            Assert.That(clan.Members, Has.Count.EqualTo(1));
            Assert.That(clan.Members[0].PlayerId, Is.EqualTo(playerId));
            Assert.That(clan.Members[0].Name, Is.EqualTo("Huntmaster"));
            Assert.That(clan.Members[0].Rank, Is.EqualTo(YautjaRank.Leader));
            Assert.That(clan.Members[0].Online, Is.True);
        });
    }
}
