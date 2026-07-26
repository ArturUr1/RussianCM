using System.Linq;
using Content.Shared.Access;
using Content.Shared._CMU14.Yautja;
using Content.Shared.Preferences;

namespace Content.IntegrationTests._CMU14.Yautja;

[TestFixture]
public sealed class YautjaRankParityTest
{
    [TestCase(YautjaRank.Unblooded, "unblooded", false, false, new[] { "CMUAccessYautjaSecure" })]
    [TestCase(YautjaRank.YoungBlood, "youngblood", false, false, new[] { "CMUAccessYautjaSecure" })]
    [TestCase(YautjaRank.Blooded, "blooded", false, false, new[] { "CMUAccessYautjaSecure" })]
    [TestCase(YautjaRank.Elite, "elite", true, false, new[] { "CMUAccessYautjaSecure", "CMUAccessYautjaElite" })]
    [TestCase(YautjaRank.Elder, "elder", true, false, new[] { "CMUAccessYautjaSecure", "CMUAccessYautjaElite", "CMUAccessYautjaElder" })]
    [TestCase(YautjaRank.Leader, "leader", true, true, new[] { "CMUAccessYautjaSecure", "CMUAccessYautjaElite", "CMUAccessYautjaElder", "CMUAccessYautjaLeader" })]
    [TestCase(YautjaRank.Ancient, "ancient", true, true, new[] { "CMUAccessYautjaSecure", "CMUAccessYautjaElite", "CMUAccessYautjaElder", "CMUAccessYautjaLeader", "CMUAccessYautjaAncient" })]
    public void RankMetadataMatchesCmss13(
        YautjaRank rank,
        string icon,
        bool unique,
        bool bypassSlots,
        string[] accessTags)
    {
        var metadata = YautjaRankMetadata.For(rank);

        Assert.Multiple(() =>
        {
            Assert.That(metadata.IconState, Is.EqualTo(icon));
            Assert.That(metadata.UniqueSetsAllowed, Is.EqualTo(unique));
            Assert.That(metadata.BypassesPredatorSlotCap, Is.EqualTo(bypassSlots));
            Assert.That(metadata.AccessTags.Select(tag => tag.Id), Is.EqualTo(accessTags));
        });
    }

    [Test]
    public void MissingHunterRankFallsBackToBlooded()
    {
        Assert.Multiple(() =>
        {
            Assert.That(YautjaRankResolver.ResolveForHunter(null), Is.EqualTo(YautjaRank.Blooded));
            Assert.That(YautjaRankResolver.ResolveForHunter(YautjaCharacterProfile.Default), Is.EqualTo(YautjaRank.Blooded));
            Assert.That(
                YautjaRankResolver.ResolveForHunter(
                    YautjaCharacterProfile.Default.WithOwnerRank(YautjaBracerOwnerRank.Unblooded)),
                Is.EqualTo(YautjaRank.Blooded));
        });
    }

    [TestCase(YautjaBracerOwnerRank.Elite, YautjaRank.Elite)]
    [TestCase(YautjaBracerOwnerRank.Elder, YautjaRank.Elder)]
    [TestCase(YautjaBracerOwnerRank.Leader, YautjaRank.Leader)]
    [TestCase(YautjaBracerOwnerRank.Admin, YautjaRank.Ancient)]
    public void LegacySpecialOwnerRanksResolveToCanonicalRank(YautjaBracerOwnerRank ownerRank, YautjaRank expectedRank)
    {
        var profile = YautjaCharacterProfile.Default.WithOwnerRank(ownerRank);

        Assert.That(YautjaRankResolver.ResolveForHunter(profile), Is.EqualTo(expectedRank));
    }

    [TestCase(YautjaRank.Unblooded, YautjaBracerOwnerRank.Unblooded)]
    [TestCase(YautjaRank.YoungBlood, YautjaBracerOwnerRank.Unblooded)]
    [TestCase(YautjaRank.Blooded, YautjaBracerOwnerRank.Unblooded)]
    [TestCase(YautjaRank.Elite, YautjaBracerOwnerRank.Elite)]
    [TestCase(YautjaRank.Elder, YautjaBracerOwnerRank.Elder)]
    [TestCase(YautjaRank.Leader, YautjaBracerOwnerRank.Leader)]
    [TestCase(YautjaRank.Ancient, YautjaBracerOwnerRank.Admin)]
    public void CanonicalRankProjectsToLegacyBracerOwnerRank(YautjaRank rank, YautjaBracerOwnerRank expectedOwnerRank)
    {
        Assert.That(YautjaRankResolver.ToOwnerRank(rank), Is.EqualTo(expectedOwnerRank));
    }

    [TestCase(YautjaBracerOwnerRank.Unblooded, YautjaRank.Blooded)]
    [TestCase(YautjaBracerOwnerRank.Elite, YautjaRank.Elite)]
    [TestCase(YautjaBracerOwnerRank.Elder, YautjaRank.Elder)]
    [TestCase(YautjaBracerOwnerRank.Leader, YautjaRank.Leader)]
    [TestCase(YautjaBracerOwnerRank.Admin, YautjaRank.Ancient)]
    public void LegacyBracerOwnerRanksProjectToCanonicalHunterCompatibilityRank(
        YautjaBracerOwnerRank ownerRank,
        YautjaRank expectedRank)
    {
        Assert.That(YautjaRankResolver.FromOwnerRank(ownerRank), Is.EqualTo(expectedRank));
    }

    [Test]
    public void HumanoidProfileCloneAndEqualityKeepCanonicalClanRank()
    {
        var canonical = YautjaCharacterProfile.Default.WithClanRank(YautjaRank.Elder);
        var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human")
            .WithName("Kainde")
            .WithYautjaProfile(canonical);

        var clone = profile.Clone();
        var differentRank = profile.WithYautjaProfile(canonical.WithClanRank(YautjaRank.Leader));

        Assert.Multiple(() =>
        {
            Assert.That(clone.YautjaProfile.ClanRank, Is.EqualTo(YautjaRank.Elder));
            Assert.That(clone.MemberwiseEquals(profile), Is.True);
            Assert.That(profile.MemberwiseEquals(differentRank), Is.False);
        });
    }
}
