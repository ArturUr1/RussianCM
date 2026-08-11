using Content.Client._RMC14.DonorCapes;
using NUnit.Framework;

namespace Content.Tests.Shared._RMC14.DonorCapes;

[TestFixture]
public sealed class DonorCapePreviewAnimationTest
{
    [Test]
    public void BackViewRangeSkipsTransitionFrames()
    {
        var range = DonorCapePreviewAnimation.GetBackViewFrameRange(15);

        Assert.That(range.Start, Is.EqualTo(2));
        Assert.That(range.Count, Is.EqualTo(11));
        Assert.That(range.Start + range.Count - 1, Is.EqualTo(12));
    }

    [Test]
    public void ShortAnimationsKeepAllFrames()
    {
        var range = DonorCapePreviewAnimation.GetBackViewFrameRange(4);

        Assert.That(range.Start, Is.EqualTo(0));
        Assert.That(range.Count, Is.EqualTo(4));
    }
}
