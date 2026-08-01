using Content.Shared._CMU14.Yautja;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Tests.Shared._CMU14.Yautja;

[TestFixture]
public sealed class YautjaWallVisionTargetingTest
{
    [Test]
    public void TargetMustBeDifferentVisibleMobOnTheSameMapAndOutsideContainers()
    {
        var viewer = new EntityUid(1);
        var target = new EntityUid(2);
        var map = new MapId(1);

        Assert.Multiple(() =>
        {
            Assert.That(YautjaWallVisionTargeting.IsEligible(
                viewer, target, map, map, true, true, false, true), Is.True);
            Assert.That(YautjaWallVisionTargeting.IsEligible(
                viewer, target, map, map, true, true, false, false), Is.False);
            Assert.That(YautjaWallVisionTargeting.IsEligible(
                viewer, viewer, map, map, true, true, false, true), Is.False);
            Assert.That(YautjaWallVisionTargeting.IsEligible(
                viewer, target, map, map, false, true, false, true), Is.False);
            Assert.That(YautjaWallVisionTargeting.IsEligible(
                viewer, target, map, map, true, false, false, true), Is.False);
            Assert.That(YautjaWallVisionTargeting.IsEligible(
                viewer, target, map, new MapId(2), true, true, false, true), Is.False);
            Assert.That(YautjaWallVisionTargeting.IsEligible(
                viewer, target, map, map, true, true, true, true), Is.False);
        });
    }
}
