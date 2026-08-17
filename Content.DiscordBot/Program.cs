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

if (string.IsNullOrWhiteSpace(token))
    throw new ArgumentException("No token found.");

if (guild == 0)
    throw new ArgumentException("No Discord guild found.");

config.Guild = guild;
if (config.CourtEnabled && config.CourtChannel == 0)
    throw new ArgumentException("Community Court is enabled but CourtChannel is not configured.");

await using (var governance = CreateGovernanceDatabase())
    await governance.Database.MigrateAsync();

await using CourtInstanceLock? courtInstanceLock = config.CourtEnabled
    ? await CourtInstanceLock.AcquireAsync(connectionString)
    : null;

var court = new CommunityCourtService(CreateGovernanceDatabase, CreateConfiguredDatabase, CourtPolicy.FromConfig(config));
var coordinator = new CourtDiscordCoordinator(client, court, config);
var services = new ServiceCollection()
    .AddSingleton(client)
    .AddSingleton(config)
    .AddSingleton(court)
    .AddSingleton(coordinator)
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
    await scheduler;
}
catch (OperationCanceledException)
{
    // Normal scheduler shutdown.
}
