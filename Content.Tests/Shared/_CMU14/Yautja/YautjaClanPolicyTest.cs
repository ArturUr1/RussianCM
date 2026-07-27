using System;
using Content.Shared._CMU14.Yautja;
using NUnit.Framework;
using Robust.Shared.Network;

namespace Content.Tests.Shared._CMU14.Yautja;

[TestFixture]
public sealed class YautjaClanPolicyTest
{
    [TestCase(YautjaRank.Unblooded, YautjaClanPermission.AdminModify, null, null)]
    [TestCase(YautjaRank.Blooded, YautjaClanPermission.UserModify, null, null)]
    [TestCase(YautjaRank.Elite, YautjaClanPermission.UserModify, 5, null)]
    [TestCase(YautjaRank.Elder, YautjaClanPermission.UserModify, null, 12)]
    [TestCase(YautjaRank.Leader, YautjaClanPermission.UserAll | YautjaClanPermission.AdminModify, 1, null)]
    [TestCase(YautjaRank.Ancient, YautjaClanPermission.AdminAncient, null, null)]
    public void RankRulesMatchCmss13(
        YautjaRank rank,
        YautjaClanPermission permission,
        int? absoluteLimit,
        int? membersPerRankLimit)
    {
        var rule = YautjaClanPolicy.GetRule(rank);

        Assert.Multiple(() =>
        {
            Assert.That(rule.RequiredPermission, Is.EqualTo(permission));
            Assert.That(rule.AbsoluteLimit, Is.EqualTo(absoluteLimit));
            Assert.That(rule.MembersPerRankLimit, Is.EqualTo(membersPerRankLimit));
        });
    }

    [Test]
    public void ActorCannotTargetSelfOrEqualOrHigherRank()
    {
        var actor = Member(1, YautjaRank.Leader, YautjaClanPermission.UserAll | YautjaClanPermission.AdminModify);

        Assert.Multiple(() =>
        {
            Assert.That(YautjaClanPolicy.CanTarget(actor, actor), Is.False);
            Assert.That(YautjaClanPolicy.CanTarget(
                actor,
                Member(2, YautjaRank.Leader, YautjaClanPermission.UserAll)), Is.False);
            Assert.That(YautjaClanPolicy.CanTarget(
                actor,
                Member(3, YautjaRank.Ancient, YautjaClanPermission.AdminAncient)), Is.False);
        });
    }

    [Test]
    public void ManagerStillCannotTargetAncientAdministrator()
    {
        var actor = Member(1, YautjaRank.Ancient, YautjaClanPermission.All);
        var target = Member(2, YautjaRank.Leader, YautjaClanPermission.AdminAncient);

        Assert.That(YautjaClanPolicy.CanTarget(actor, target), Is.False);
    }

    [Test]
    public void NormalRankOptionsExcludeYoungBloodAndAncient()
    {
        var options = YautjaClanPolicy.GetNormalAssignableRanks();

        Assert.Multiple(() =>
        {
            Assert.That(options, Does.Not.Contain(YautjaRank.YoungBlood));
            Assert.That(options, Does.Not.Contain(YautjaRank.Ancient));
            Assert.That(options, Does.Contain(YautjaRank.Unblooded));
            Assert.That(options, Does.Contain(YautjaRank.Leader));
        });
    }

    [TestCase(YautjaRank.Elite, 5, 5, false)]
    [TestCase(YautjaRank.Elite, 4, 5, true)]
    [TestCase(YautjaRank.Elder, 1, 12, false)]
    [TestCase(YautjaRank.Elder, 1, 13, true)]
    [TestCase(YautjaRank.Leader, 1, 1, false)]
    public void RankLimitsUsePostChangeOccupancy(
        YautjaRank rank,
        int currentOccupancy,
        int clanSize,
        bool expectedAllowed)
    {
        var actor = Member(1, YautjaRank.Leader, YautjaClanPermission.UserAll | YautjaClanPermission.AdminModify);
        var target = Member(2, YautjaRank.Blooded, YautjaClanPermission.UserModify);

        Assert.That(
            YautjaClanPolicy.CanModifyRank(actor, target, rank, clanSize, currentOccupancy),
            Is.EqualTo(expectedAllowed));
    }

    private static YautjaClanMemberSnapshot Member(
        int id,
        YautjaRank rank,
        YautjaClanPermission permissions)
    {
        return new YautjaClanMemberSnapshot(
            new NetUserId(new Guid(id, 0, 0, new byte[8])),
            1,
            rank,
            permissions,
            false,
            0);
    }
}
