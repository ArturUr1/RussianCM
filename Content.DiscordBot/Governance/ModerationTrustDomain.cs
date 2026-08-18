namespace Content.DiscordBot.Governance;

public static class ModerationReviewOutcomes
{
    public const string Correct = "correct";
    public const string ReasonableButWrong = "reasonable_but_wrong";
    public const string ProceduralError = "procedural_error";
    public const string Negligent = "negligent";
    public const string Abuse = "abuse";

    public static bool IsValid(string outcome) => outcome is
        Correct or ReasonableButWrong or ProceduralError or Negligent or Abuse;

    public static int AccuracyWeight(string outcome) => outcome switch
    {
        Correct => 100,
        ReasonableButWrong => 85,
        ProceduralError => 60,
        Negligent => 25,
        Abuse => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };

    public static int ProcedureWeight(string outcome) => outcome switch
    {
        Correct or ReasonableButWrong => 100,
        ProceduralError => 35,
        Negligent => 20,
        Abuse => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
    };
}

public sealed class GovernanceModerationReview
{
    public long Id { get; set; }
    public long ActionId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed record ModerationTrustProfile(
    Guid UserId,
    int TrustScore,
    int DecisionAccuracy,
    int ProceduralScore,
    int ReliabilityScore,
    int Confidence,
    int ReviewedActions,
    int ReviewSamples,
    int CompletedDuties,
    int FailedDuties,
    int SeriousInterventions);

public sealed record ModerationReviewAssignment(
    long ActionId,
    long InvitationId,
    Guid ReviewerUserId,
    long ReviewerDiscordId,
    DateTime ExpiresAt);
