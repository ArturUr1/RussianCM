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
}
