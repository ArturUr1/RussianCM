using Content.DiscordBot.Governance;
using NUnit.Framework;

namespace Content.DiscordBot.Tests;

[TestFixture]
public sealed class ReputationMathTests
{
    private static readonly DateTime Now = new(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public void NeutralPriorStartsAtFiveHundred()
    {
        var posterior = ReputationMath.Posterior(ReputationTracks.General, [], Now);
        Assert.That(posterior.Score, Is.EqualTo(500));
        Assert.That(posterior.Mean, Is.EqualTo(0.5).Within(1e-9));
        Assert.That(posterior.LowerBound, Is.LessThan(posterior.Mean));
    }

    [Test]
    public void PositiveEvidenceRaisesMeanAndCredibleBound()
    {
        var neutral = ReputationMath.Posterior(ReputationTracks.Jury, [], Now);
        var positive = ReputationMath.Posterior(ReputationTracks.Jury,
        [
            new ReputationObservationValue(Now.AddDays(-1), ReputationReasons.JuryCompleted, 4, 0, false),
            new ReputationObservationValue(Now.AddDays(-2), ReputationReasons.JuryCompleted, 4, 0, false),
        ], Now);
        Assert.That(positive.Mean, Is.GreaterThan(neutral.Mean));
        Assert.That(positive.LowerBound, Is.GreaterThan(neutral.LowerBound));
    }

    [Test]
    public void SustainedPositiveBehaviorRehabilitatesSeriousError()
    {
        var withoutRehabilitation = ReputationMath.Posterior(ReputationTracks.Moderation,
        [
            new ReputationObservationValue(Now.AddDays(-30), ReputationReasons.ModerationActionWrong, 0, 3, true),
        ], Now);
        var rehabilitated = ReputationMath.Posterior(ReputationTracks.Moderation,
        [
            new ReputationObservationValue(Now.AddDays(-30), ReputationReasons.ModerationActionWrong, 0, 3, true),
            new ReputationObservationValue(Now.AddDays(-20), ReputationReasons.DutyCompleted, 3, 0, false),
            new ReputationObservationValue(Now.AddDays(-10), ReputationReasons.ModerationActionCorrect, 3, 0, false),
            new ReputationObservationValue(Now.AddDays(-2), ReputationReasons.DutyCompleted, 3, 0, false),
        ], Now);
        Assert.That(rehabilitated.Mean, Is.GreaterThan(withoutRehabilitation.Mean));
        Assert.That(rehabilitated.LowerBound, Is.GreaterThan(withoutRehabilitation.LowerBound));
    }

    [Test]
    public void RecidivismMakesRepeatedSeriousErrorsWorse()
    {
        var once = ReputationMath.Posterior(ReputationTracks.Moderation,
        [
            new ReputationObservationValue(Now.AddDays(-10), "serious", 0, 2, true),
        ], Now);
        var twice = ReputationMath.Posterior(ReputationTracks.Moderation,
        [
            new ReputationObservationValue(Now.AddDays(-20), "serious", 0, 2, true),
            new ReputationObservationValue(Now.AddDays(-10), "serious", 0, 2, true),
        ], Now);
        Assert.That(twice.Mean, Is.LessThan(once.Mean));
    }

    [Test]
    public void OldEvidenceHasLessInfluence()
    {
        var recent = ReputationMath.Posterior(ReputationTracks.Jury,
        [
            new ReputationObservationValue(Now.AddDays(-1), "positive", 4, 0, false),
        ], Now);
        var old = ReputationMath.Posterior(ReputationTracks.Jury,
        [
            new ReputationObservationValue(Now.AddDays(-720), "positive", 4, 0, false),
        ], Now);
        Assert.That(recent.Mean, Is.GreaterThan(old.Mean));
    }

    [Test]
    public void GameActivitySaturates()
    {
        var early = ReputationMath.Activity(100, 20, 180);
        var veteran = ReputationMath.Activity(1000, 100, 1500);
        var extreme = ReputationMath.Activity(10000, 1000, 10000);
        Assert.That(veteran.ActivityIndex, Is.GreaterThan(early.ActivityIndex));
        Assert.That(extreme.ActivityIndex, Is.LessThanOrEqualTo(1));
        Assert.That(extreme.EvidenceWeight, Is.LessThanOrEqualTo(ReputationPolicy.GameActivityMaxEvidence));
        Assert.That(extreme.ActivityIndex - veteran.ActivityIndex, Is.LessThan(veteran.ActivityIndex - early.ActivityIndex));
    }

    [Test]
    public void ContributionWeightUsesSaturation()
    {
        var ordinary = ReputationPolicy.ContributionSuccessWeight(1, 1, 1);
        var large = ReputationPolicy.ContributionSuccessWeight(3, 1.5, 1.5);
        Assert.That(large, Is.GreaterThan(ordinary));
        Assert.That(large, Is.LessThanOrEqualTo(2.5));
    }

    [Test]
    public void QualificationNeedsConservativeEvidence()
    {
        var optimisticButUncertain = new ReputationPosterior("jury", 9, 1, 0.9, 0.60, 4, 900);
        var established = new ReputationPosterior("jury", 40, 6, 40d / 46d, 0.78, 20, 870);
        Assert.That(ReputationPolicy.EligibleQualificationLevel("jury", optimisticButUncertain, 20), Is.LessThan(3));
        Assert.That(ReputationPolicy.EligibleQualificationLevel("jury", established, 20), Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void ThompsonSampleStaysInProbabilityRange()
    {
        var random = new Random(12345);
        for (var i = 0; i < 1000; i++)
            Assert.That(ReputationMath.SampleBeta(8, 3, random), Is.InRange(0.0, 1.0));
    }
}
