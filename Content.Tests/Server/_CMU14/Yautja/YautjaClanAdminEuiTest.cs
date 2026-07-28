using System;
using Content.Server.Database;
using Content.Server._CMU14.Yautja;
using Content.Shared._CMU14.Yautja;
using NUnit.Framework;
using Robust.Shared.Network;

namespace Content.Tests.Server._CMU14.Yautja;

[TestFixture]
public sealed class YautjaClanAdminEuiTest
{
    [Test]
    public void ToMemberStateSanitizesRankAndRetainsDisplayData()
    {
        var playerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var record = new YautjaClanMemberRecord(playerId, 7, 255, 0, 0, false);

        var state = YautjaClanAdminEui.ToMemberState(record, "Unknown hunter", false);

        Assert.Multiple(() =>
        {
            Assert.That(state.PlayerId, Is.EqualTo(new NetUserId(playerId)));
            Assert.That(state.Name, Is.EqualTo("Unknown hunter"));
            Assert.That(state.Rank, Is.EqualTo(YautjaRank.Blooded));
            Assert.That(state.Online, Is.False);
        });
    }
}
