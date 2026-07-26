using System.Linq;
using Content.Client._CMU14.Yautja.Lobby;
using Content.Shared._CMU14.Yautja;
using NUnit.Framework;

namespace Content.Tests.Client._CMU14.Yautja;

[TestFixture]
public sealed class YautjaProfileEditorLayoutTest
{
    [Test]
    public void CategoriesExposeAllNavigationGroupsInDesignOrder()
    {
        Assert.That(
            YautjaProfileEditorLayout.Categories,
            Has.Exactly(5).Items);
        Assert.That(
            YautjaProfileEditorLayout.Categories.Select(info => info.Id),
            Is.EqualTo(new[]
            {
                YautjaProfileEditorCategory.Appearance,
                YautjaProfileEditorCategory.Equipment,
                YautjaProfileEditorCategory.Sets,
                YautjaProfileEditorCategory.Technology,
                YautjaProfileEditorCategory.Description,
            }));
    }

    [TestCase(YautjaProfileEditorCategory.Appearance, YautjaProfileEditorCategory.Appearance, true)]
    [TestCase(YautjaProfileEditorCategory.Appearance, YautjaProfileEditorCategory.Equipment, false)]
    public void OnlyTheActiveCategoryPageIsVisible(
        YautjaProfileEditorCategory active,
        YautjaProfileEditorCategory candidate,
        bool expected)
    {
        Assert.That(YautjaProfileEditorLayout.IsCategoryActive(active, candidate), Is.EqualTo(expected));
    }

    [TestCase(YautjaRank.Unblooded, true)]
    [TestCase(YautjaRank.YoungBlood, true)]
    [TestCase(YautjaRank.Blooded, true)]
    [TestCase(YautjaRank.Elite, false)]
    [TestCase(YautjaRank.Elder, false)]
    [TestCase(YautjaRank.Leader, false)]
    [TestCase(YautjaRank.Ancient, false)]
    public void UniqueSetsAreLockedUntilElite(YautjaRank rank, bool locked)
    {
        var profile = YautjaCharacterProfile.Default.WithRank(rank);

        Assert.That(
            YautjaProfileEditorLayout.IsUniqueSetLocked(profile, YautjaUniqueSet.Anubys),
            Is.EqualTo(locked));
    }

    [Test]
    public void NoneOptionIsNeverLocked()
    {
        var profile = YautjaCharacterProfile.Default.WithRank(YautjaRank.Blooded);

        Assert.That(
            YautjaProfileEditorLayout.IsUniqueSetLocked(profile, YautjaUniqueSet.None),
            Is.False);
    }

    [Test]
    public void BuildSummaryUsesUniqueSetAndCurrentGearNames()
    {
        var profile = YautjaCharacterProfile.Default
            .WithRank(YautjaRank.Elite)
            .WithUnique(YautjaUniqueSet.Anubys)
            .WithArmor(YautjaGearMaterial.Silver, 2)
            .WithMask(YautjaGearMaterial.Bronze, 3)
            .WithGreaves(YautjaGearMaterial.Bone, 1)
            .WithCapeStyle(YautjaCapeStyle.Full)
            .WithBracer(YautjaBracerMaterial.Crimson)
            .WithCaster(YautjaBracerMaterial.Silver);

        var summary = YautjaProfileEditorLayout.BuildSummary(profile);

        Assert.That(summary.Set, Is.EqualTo(YautjaCharacterProfile.GetUniqueDisplayName(YautjaUniqueSet.Anubys)));
        Assert.That(summary.Armor, Is.EqualTo(YautjaCharacterProfile.GetArmorStyleDisplayName(YautjaGearMaterial.Silver, 2)));
        Assert.That(summary.Mask, Is.EqualTo(YautjaCharacterProfile.GetMaskStyleDisplayName(YautjaGearMaterial.Bronze, 3)));
        Assert.That(summary.Greaves, Is.EqualTo(YautjaCharacterProfile.GetGreavesStyleDisplayName(YautjaGearMaterial.Bone, 1)));
        Assert.That(summary.Cape, Is.EqualTo(YautjaCharacterProfile.GetCapeDisplayName(YautjaCapeStyle.Full)));
        Assert.That(summary.Bracer, Is.EqualTo(YautjaCharacterProfile.GetBracerDisplayName(YautjaBracerMaterial.Crimson)));
        Assert.That(summary.Caster, Is.EqualTo(YautjaCharacterProfile.GetCasterDisplayName(YautjaBracerMaterial.Silver)));
    }

    [TestCase(760, 6, 6)]
    [TestCase(340, 6, 3)]
    [TestCase(220, 4, 1)]
    public void ResponsiveColumnsFitTheAvailableWidth(float availableWidth, int preferredColumns, int expected)
    {
        Assert.That(
            YautjaProfileEditorLayout.GetResponsiveColumnCount(availableWidth, preferredColumns),
            Is.EqualTo(expected));
    }
}
