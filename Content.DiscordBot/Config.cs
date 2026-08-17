namespace Content.DiscordBot;

public sealed class Config
{
    public string Token { get; set; } = string.Empty;

    public string DatabaseString { get; set; } = string.Empty;

    public ulong Guild { get; set; } = 1168210010233376858UL;

    public bool CourtEnabled { get; set; }

    public ulong CourtChannel { get; set; }

    public int CourtSchedulerSeconds { get; set; } = 30;

    public int CourtComplaintWindowHours { get; set; } = 72;

    public int CourtDefenseHours { get; set; } = 48;

    public int CourtVoteHours { get; set; } = 48;

    public int CourtInvitationHours { get; set; } = 24;

    public int CourtJurySize { get; set; } = 3;

    public int CourtDecisionThreshold { get; set; } = 2;

    public int CourtAcceptReward { get; set; } = 10;

    public int CourtDeclinePenalty { get; set; } = 15;

    public int CourtExpiryPenalty { get; set; } = 20;

    public int CourtJuryReward { get; set; } = 15;

    public int CourtFailurePenalty { get; set; } = 30;

    public int CourtFalseReportPenalty { get; set; } = 50;

    public int CourtSelectionCooldownHours { get; set; } = 24;

    public ulong CourtLeadershipRole { get; set; }

    public ulong GovernanceChannel { get; set; }

    public int EventReviewHours { get; set; } = 48;

    public int EventReviewers { get; set; } = 3;

    public int EventApprovalThreshold { get; set; } = 2;
}
