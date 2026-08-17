namespace Content.DiscordBot.Governance;

public sealed class GovernanceUser
{
    public Guid Id { get; set; }
    public Guid Ss14UserId { get; set; }
    public long DiscordUserId { get; set; }
    public int CivicRatingCache { get; set; }
    public bool IsGovernanceSuspended { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class GovernanceQualification
{
    public Guid UserId { get; set; }
    public string Track { get; set; } = string.Empty;
    public short Level { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class GovernanceRatingEntry
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedByType { get; set; } = string.Empty;
    public string? CreatedById { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Metadata { get; set; } = "{}";
}

public sealed class GovernanceConflict
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? RelatedUserId { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string CreatedByType { get; set; } = string.Empty;
    public string? CreatedById { get; set; }
}

public sealed class GovernanceInvitation
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RecusalReason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime? DiscordNotifiedAt { get; set; }
}

public sealed class GovernanceCourtCase
{
    public long Id { get; set; }
    public Guid ClaimantUserId { get; set; }
    public Guid DefendantUserId { get; set; }
    public int RoundId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime FiledAt { get; set; }
    public DateTime DefenseDeadline { get; set; }
    public DateTime? GuiltStartedAt { get; set; }
    public DateTime? GuiltDeadline { get; set; }
    public DateTime? SentencingStartedAt { get; set; }
    public DateTime? SentencingDeadline { get; set; }
    public string? Verdict { get; set; }
    public string? SanctionType { get; set; }
    public short? SanctionDays { get; set; }
    public string? SanctionRole { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public string? ExecutionReference { get; set; }
    public int Version { get; set; }
    public long? DiscordThreadId { get; set; }
    public long? VerdictMessageId { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public sealed class GovernanceCourtStatement
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? EvidenceReference { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class GovernanceJuror
{
    public long CaseId { get; set; }
    public Guid UserId { get; set; }
    public long InvitationId { get; set; }
    public bool Active { get; set; }
    public DateTime AssignedAt { get; set; }
}

public sealed class GovernanceGuiltVote
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public Guid JurorUserId { get; set; }
    public string Verdict { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class GovernanceSentencingVote
{
    public long Id { get; set; }
    public long CaseId { get; set; }
    public Guid JurorUserId { get; set; }
    public string SanctionType { get; set; } = string.Empty;
    public short? SanctionDays { get; set; }
    public string? SanctionRole { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class GovernanceAuditEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Payload { get; set; } = "{}";
}
