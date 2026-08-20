using System.Text.Json;
using Content.DiscordBot;
using Content.DiscordBot.Governance;
using Content.Server.Database;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var client = new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents =
        GatewayIntents.Guilds |
        GatewayIntents.GuildMessages,
});
client.Log += Logger.Log;
var seedBoostyTiers = args.Contains("--seed-boosty-tiers");
var listBoostyTiers = args.Contains("--list-boosty-tiers");
var listTestPatrons = args.Contains("--list-test-patrons");
var grantTestTierIndex = Array.IndexOf(args, "--grant-test-tier");
var migrateOnly = args.Contains("--migrate-only");
var governanceDoctor = args.Contains("--governance-doctor");
var environmentFileIndex = Array.IndexOf(args, "--env-file");
if (environmentFileIndex >= 0)
{
    if (args.Length <= environmentFileIndex + 1)
        throw new ArgumentException("Usage: --env-file <path>");
    ConfigurationLoader.LoadEnvironmentFile(args[environmentFileIndex + 1]);
}

string? token = null;
string? connectionString = null;
var guild = 0UL;
var config = new Config();
if (File.Exists("config.json"))
{
    config = await JsonSerializer.DeserializeAsync<Config>(File.OpenRead("config.json")) ?? new Config();
    token = config.Token;
    connectionString = config.DatabaseString;
    guild = config.Guild;
}

ConfigurationLoader.ApplyEnvironment(config, ref token, ref connectionString, ref guild);

if (string.IsNullOrWhiteSpace(connectionString))
    throw new ArgumentException("No database connection string found.");

ServerDbContext CreateConfiguredDatabase()
{
    var postgresBuilder = new DbContextOptionsBuilder<PostgresServerDbContext>();
    postgresBuilder.UseNpgsql(connectionString);
    return new PostgresServerDbContext(postgresBuilder.Options);
}

GovernanceDbContext CreateGovernanceDatabase()
{
    var builder = new DbContextOptionsBuilder<GovernanceDbContext>();
    builder.UseNpgsql(connectionString);
    return new GovernanceDbContext(builder.Options);
}

async Task WithConfiguredDatabase(Func<ServerDbContext, Task> action)
{
    await using var db = CreateConfiguredDatabase();
    await action(db);
}

if (seedBoostyTiers)
{
    await WithConfiguredDatabase(BoostyTierSeeder.Seed);
    Console.WriteLine("Boosty sponsor tiers seeded.");
    return;
}

if (listBoostyTiers)
{
    await WithConfiguredDatabase(BoostyTierSeeder.PrintTiers);
    return;
}

if (listTestPatrons)
{
    await WithConfiguredDatabase(BoostyTierSeeder.PrintPatrons);
    return;
}

if (grantTestTierIndex >= 0)
{
    if (args.Length <= grantTestTierIndex + 2)
        throw new ArgumentException("Usage: --grant-test-tier <player-name-or-user-id> <tier-name>");

    var playerNameOrId = args[grantTestTierIndex + 1];
    var tierName = args[grantTestTierIndex + 2];
    await WithConfiguredDatabase(db => BoostyTierSeeder.GrantTestTier(db, playerNameOrId, tierName));
    Console.WriteLine($"Granted '{tierName}' to '{playerNameOrId}'.");
    return;
}

await using (var governance = CreateGovernanceDatabase())
    await governance.Database.MigrateAsync();

if (migrateOnly)
{
    Console.WriteLine("Governance migrations applied successfully.");
    return;
}

if (governanceDoctor)
{
    await using var governance = CreateGovernanceDatabase();
    var requiredTables = new HashSet<string>(StringComparer.Ordinal)
    {
        "users", "rating_entries", "qualifications", "conflicts", "invitations",
        "court_cases", "court_participants", "court_statements", "jurors", "guilt_votes",
        "sentencing_votes", "friendships", "service_assignments", "punishment_executions",
        "duty_sessions", "capability_grants", "ahelp_tickets", "ahelp_messages", "live_incidents",
        "moderation_actions", "moderation_approvals", "moderation_reviews", "event_proposals", "event_reviews",
        "event_sessions", "event_manifest_items", "event_actions", "leadership_overrides", "audit_events",
    };
    var existingTables = (await governance.Database.SqlQueryRaw<string>(
        "SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'governance'").ToListAsync()).ToHashSet();
    var missing = requiredTables.Except(existingTables).OrderBy(value => value).ToArray();
    if (missing.Length > 0)
        throw new InvalidOperationException($"Governance schema is incomplete: {string.Join(", ", missing)}");
    var applied = await governance.Database.GetAppliedMigrationsAsync();
    if (!applied.Contains("20260821000000_EventActionServerExecution"))
        throw new InvalidOperationException("EventActionServerExecution migration is not recorded as applied.");
    var ahelpColumns = (await governance.Database.SqlQueryRaw<string>("""
        SELECT column_name || ':' || is_nullable AS "Value"
        FROM information_schema.columns
        WHERE table_schema = 'governance' AND table_name = 'ahelp_tickets'
          AND column_name IN ('reporter_user_id', 'reporter_ss14_user_id')
        """).ToListAsync()).ToHashSet(StringComparer.Ordinal);
    if (!ahelpColumns.SetEquals(["reporter_user_id:YES", "reporter_ss14_user_id:NO"]))
        throw new InvalidOperationException("The in-game AHelp ticket identity contract is invalid.");
    var eventActionColumns = (await governance.Database.SqlQueryRaw<string>("""
        SELECT column_name || ':' || is_nullable AS "Value"
        FROM information_schema.columns
        WHERE table_schema = 'governance' AND table_name = 'event_actions'
          AND column_name IN ('server_status', 'server_executed_at', 'server_execution_error')
        """).ToListAsync()).ToHashSet(StringComparer.Ordinal);
    if (!eventActionColumns.SetEquals([
            "server_status:NO",
            "server_executed_at:YES",
            "server_execution_error:YES",
        ]))
        throw new InvalidOperationException("The event server execution contract is invalid.");
    var immutableTrigger = await governance.Database.SqlQueryRaw<int>("""
        SELECT count(*)::integer AS "Value"
        FROM pg_trigger
        WHERE tgrelid = 'governance.ahelp_messages'::regclass
          AND tgname = 'ahelp_messages_immutable' AND tgenabled <> 'D'
        """).SingleAsync();
    if (immutableTrigger != 1)
        throw new InvalidOperationException("The immutable AHelp transcript trigger is unavailable.");
    var moderationReviewTrigger = await governance.Database.SqlQueryRaw<int>("""
        SELECT count(*)::integer AS "Value"
        FROM pg_trigger
        WHERE tgrelid = 'governance.moderation_reviews'::regclass
          AND tgname = 'moderation_reviews_immutable' AND tgenabled <> 'D'
        """).SingleAsync();
    if (moderationReviewTrigger != 1)
        throw new InvalidOperationException("The immutable moderation review trigger is unavailable.");
    await using var game = CreateConfiguredDatabase();
    _ = await game.Player.AsNoTracking().CountAsync();
    _ = await game.RMCLinkedAccounts.AsNoTracking().CountAsync();
    var doctorSelection = new CandidateSelectionService(CreateGovernanceDatabase, CreateConfiguredDatabase);
    _ = await doctorSelection.SelectAsync("jury", 1, "doctor", "read-only", 1, [], null, TimeSpan.Zero);
    Console.WriteLine($"Governance doctor OK: {requiredTables.Count} workflow tables, AHelp, moderation review and event execution contracts, game identity tables, candidate query, latest migration.");
    return;
}

if (string.IsNullOrWhiteSpace(token))
    throw new ArgumentException("No token found.");

if (guild == 0)
    throw new ArgumentException("No Discord guild found.");

config.Guild = guild;
if (config.CourtEnabled && config.CourtChannel == 0)
    throw new ArgumentException("Community Court is enabled but CourtChannel is not configured.");

await using CourtInstanceLock? courtInstanceLock = config.CourtEnabled
    ? await CourtInstanceLock.AcquireAsync(connectionString)
    : null;

var selection = new CandidateSelectionService(CreateGovernanceDatabase, CreateConfiguredDatabase);
var court = new CommunityCourtService(
    CreateGovernanceDatabase,
    CreateConfiguredDatabase,
    CourtPolicy.FromConfig(config),
    selection);
var courtMaterials = new CourtSourceMaterialService(CreateGovernanceDatabase, CreateConfiguredDatabase);
var community = new GovernanceCommunityService(CreateGovernanceDatabase, CreateConfiguredDatabase, config);
var punishments = new CourtPunishmentService(CreateGovernanceDatabase, CreateConfiguredDatabase);
var moderation = new ModerationGovernanceService(CreateGovernanceDatabase, CreateConfiguredDatabase, community);
var moderationTrust = new ModerationTrustService(CreateGovernanceDatabase, community, selection, config);
var moderationQualifications = new ModerationQualificationService(CreateGovernanceDatabase, moderationTrust);
var events = new EventGovernanceService(CreateGovernanceDatabase, community, selection, config);
var coordinator = new CourtDiscordCoordinator(client, court, courtMaterials, punishments, events, moderation, config);
var moderationTrustCoordinator = new ModerationTrustCoordinator(client, moderationTrust, moderationQualifications, court, config);
var services = new ServiceCollection()
    .AddSingleton(client)
    .AddSingleton(config)
    .AddSingleton(selection)
    .AddSingleton(court)
    .AddSingleton(courtMaterials)
    .AddSingleton(community)
    .AddSingleton(punishments)
    .AddSingleton(moderation)
    .AddSingleton(moderationTrust)
    .AddSingleton(moderationQualifications)
    .AddSingleton(events)
    .AddSingleton(coordinator)
    .AddSingleton(moderationTrustCoordinator)
    .BuildServiceProvider();

await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

var interaction = new InteractionService(client);
var handler = new CommandHandler(
    client,
    new CommandService(),
    interaction,
    CreateConfiguredDatabase,
    court,
    services,
    guild);

using var shutdown = new CancellationTokenSource();

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Interlocked.Decrement(ref handler.Running);
    shutdown.Cancel();
};

await handler.InstallCommandsAsync();
var scheduler = Task.Run(() => coordinator.RunSchedulerAsync(shutdown.Token));
var moderationTrustScheduler = Task.Run(() => moderationTrustCoordinator.RunSchedulerAsync(shutdown.Token));

// Block this task until the program is closed.
try
{
    await Task.Delay(Timeout.Infinite, shutdown.Token);
}
catch (OperationCanceledException)
{
    // Normal process shutdown.
}

await client.StopAsync();
await services.DisposeAsync();
try
{
    await Task.WhenAll(scheduler, moderationTrustScheduler);
}
catch (OperationCanceledException)
{
    // Normal scheduler shutdown.
}
