using Discord;
using Discord.WebSocket;

namespace Content.DiscordBot.Governance;

public sealed class ModerationTrustCoordinator(
    DiscordSocketClient client,
    ModerationTrustService trust,
    ModerationQualificationService qualifications,
    CommunityCourtService court,
    Config config)
{
    private HashSet<ulong>? _guildMembers;
    private DateTime _guildMembersRefreshedAt;

    public async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Clamp(config.ModerationReviewSchedulerSeconds, 10, 3600));
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (client.ConnectionState == ConnectionState.Connected)
                    await ProcessOnceAsync();
            }
            catch (Exception exception)
            {
                await Logger.Error("Moderation Trust scheduler iteration failed", exception);
            }

            await Task.Delay(delay, cancellationToken);
        }
    }

    public async Task ProcessOnceAsync()
    {
        await trust.ProcessDeadlinesAsync();
        var available = await GuildMembersAsync();
        await trust.EnsureAutomaticReviewsAsync(available);
        await qualifications.ReconcileAsync();
        await NotifyReviewersAsync();
    }

    private async Task<IReadOnlySet<ulong>> GuildMembersAsync()
    {
        if (_guildMembers != null && DateTime.UtcNow - _guildMembersRefreshedAt < TimeSpan.FromMinutes(10))
            return _guildMembers;

        var members = new HashSet<ulong>();
        foreach (var discordId in await court.LinkedDiscordIdsAsync())
        {
            try
            {
                if (await client.Rest.GetGuildUserAsync(config.Guild, discordId) != null)
                    members.Add(discordId);
            }
            catch (Discord.Net.HttpException exception) when (exception.HttpCode == System.Net.HttpStatusCode.NotFound)
            {
                // Linked account is no longer present in the configured guild.
            }
        }

        _guildMembers = members;
        _guildMembersRefreshedAt = DateTime.UtcNow;
        return members;
    }

    private async Task NotifyReviewersAsync()
    {
        foreach (var (invitation, user) in await trust.PendingReviewNotificationsAsync())
        {
            try
            {
                var discordId = (ulong) user.DiscordUserId;

                IUser? discordUser = client.GetUser(discordId);
                discordUser ??= await client.Rest.GetUserAsync(discordId);

                if (discordUser == null)
                    continue;

                var dm = await discordUser.CreateDMChannelAsync();
                await dm.SendMessageAsync(
                    $"RUCM выбрал вас для независимого аудита действия дежурного №{invitation.EntityId}. " +
                    $"До <t:{new DateTimeOffset(invitation.ExpiresAt).ToUnixTimeSeconds()}:F> ответьте через " +
                    "`/дежурство аудит-ответ`. После согласия используйте `/дежурство аудит-материалы`, " +
                    "а затем `/дежурство аудит`. Согласие +10 Civic Rating, отказ -15, самоотвод без штрафа.");
                await trust.MarkInvitationNotifiedAsync(invitation.Id);
            }
            catch (Exception exception)
            {
                await Logger.Error($"Could not notify moderation reviewer {user.DiscordUserId} for invitation {invitation.Id}", exception);
            }
        }
    }
}
