using Content.DiscordBot.Governance;
using NUnit.Framework;

namespace Content.DiscordBot.Tests;

[TestFixture]
public sealed class FullGovernancePolicyTests
{
    [TestCase("freeze", 1)]
    [TestCase("request_explanation", 1)]
    [TestCase("view_logs", 1)]
    [TestCase("round_remove", 2)]
    public void ModerationQuorumMatchesRisk(string action, short expected)
    {
        Assert.That(ModerationQuorum.RequiredApprovals(action), Is.EqualTo(expected));
    }

    [Test]
    public void EventManifestRequiresScopedEventCapabilityAndBoundedLimit()
    {
        var result = EventGovernanceService.ParseManifest("event.spawn:CMXenoDrone:3, event.weather:ash:1");
        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(new EventManifestRequest("event.spawn", "CMXenoDrone", 3)));
        Assert.Throws<CourtRuleException>(() => EventGovernanceService.ParseManifest("admin.spawn:anything:1"));
        Assert.Throws<CourtRuleException>(() => EventGovernanceService.ParseManifest("event.spawn:anything:0"));
    }

    [Test]
    public void CourtDefendantNicknameIsTrimmedAndBounded()
    {
        Assert.That(CommunityCourtService.NormalizeGameNickname("  MarinePlayer  "), Is.EqualTo("MarinePlayer"));
        Assert.Throws<CourtRuleException>(() => CommunityCourtService.NormalizeGameNickname("   "));
        Assert.Throws<CourtRuleException>(() => CommunityCourtService.NormalizeGameNickname(new string('x', 65)));
    }
}
