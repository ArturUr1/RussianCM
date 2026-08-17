using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Content.DiscordBot.Governance;

public sealed class GovernanceDbContext(DbContextOptions<GovernanceDbContext> options) : DbContext(options)
{
    public DbSet<GovernanceUser> Users => Set<GovernanceUser>();
    public DbSet<GovernanceQualification> Qualifications => Set<GovernanceQualification>();
    public DbSet<GovernanceRatingEntry> RatingEntries => Set<GovernanceRatingEntry>();
    public DbSet<GovernanceConflict> Conflicts => Set<GovernanceConflict>();
    public DbSet<GovernanceInvitation> Invitations => Set<GovernanceInvitation>();
    public DbSet<GovernanceCourtCase> CourtCases => Set<GovernanceCourtCase>();
    public DbSet<GovernanceCourtStatement> CourtStatements => Set<GovernanceCourtStatement>();
    public DbSet<GovernanceJuror> Jurors => Set<GovernanceJuror>();
    public DbSet<GovernanceGuiltVote> GuiltVotes => Set<GovernanceGuiltVote>();
    public DbSet<GovernanceSentencingVote> SentencingVotes => Set<GovernanceSentencingVote>();
    public DbSet<GovernanceAuditEvent> AuditEvents => Set<GovernanceAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Configure<GovernanceUser>(modelBuilder, "users");
        Configure<GovernanceRatingEntry>(modelBuilder, "rating_entries");
        Configure<GovernanceConflict>(modelBuilder, "conflicts");
        Configure<GovernanceInvitation>(modelBuilder, "invitations");
        Configure<GovernanceCourtCase>(modelBuilder, "court_cases");
        Configure<GovernanceCourtStatement>(modelBuilder, "court_statements");
        Configure<GovernanceGuiltVote>(modelBuilder, "guilt_votes");
        Configure<GovernanceSentencingVote>(modelBuilder, "sentencing_votes");
        Configure<GovernanceAuditEvent>(modelBuilder, "audit_events");

        var qualification = modelBuilder.Entity<GovernanceQualification>();
        qualification.ToTable("qualifications", "governance");
        qualification.HasKey(value => new { value.UserId, value.Track });
        SnakeCaseProperties(qualification);

        var juror = modelBuilder.Entity<GovernanceJuror>();
        juror.ToTable("jurors", "governance");
        juror.HasKey(value => new { value.CaseId, value.UserId });
        SnakeCaseProperties(juror);

        modelBuilder.Entity<GovernanceUser>().HasIndex(value => value.Ss14UserId).IsUnique();
        modelBuilder.Entity<GovernanceUser>().HasIndex(value => value.DiscordUserId).IsUnique();
        modelBuilder.Entity<GovernanceInvitation>().HasIndex(value => value.IdempotencyKey).IsUnique();
        modelBuilder.Entity<GovernanceRatingEntry>().HasIndex(value => value.IdempotencyKey).IsUnique();
        modelBuilder.Entity<GovernanceGuiltVote>().HasIndex(value => new { value.CaseId, value.JurorUserId }).IsUnique();
        modelBuilder.Entity<GovernanceSentencingVote>().HasIndex(value => new { value.CaseId, value.JurorUserId }).IsUnique();
        modelBuilder.Entity<GovernanceCourtCase>().Property(value => value.Version).IsConcurrencyToken();
        modelBuilder.Entity<GovernanceInvitation>().Property(value => value.Version).IsConcurrencyToken();
        modelBuilder.Entity<GovernanceRatingEntry>().Property(value => value.Metadata).HasColumnType("jsonb");
        modelBuilder.Entity<GovernanceAuditEvent>().Property(value => value.Payload).HasColumnType("jsonb");
    }

    private static void Configure<TEntity>(ModelBuilder modelBuilder, string table)
        where TEntity : class
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.ToTable(table, "governance");
        entity.HasKey("Id");
        SnakeCaseProperties(entity);
    }

    private static void SnakeCaseProperties<TEntity>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity)
        where TEntity : class
    {
        foreach (var property in entity.Metadata.GetProperties())
            property.SetColumnName(ToSnakeCase(property.Name));
    }

    private static string ToSnakeCase(string value)
    {
        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $"_{char.ToLowerInvariant(character)}" : char.ToLowerInvariant(character).ToString()));
    }
}

public sealed class GovernanceDesignTimeContextFactory : IDesignTimeDbContextFactory<GovernanceDbContext>
{
    public GovernanceDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GovernanceDbContext>()
            .UseNpgsql("Host=localhost;Database=ss14;Username=postgres")
            .Options;
        return new GovernanceDbContext(options);
    }
}
