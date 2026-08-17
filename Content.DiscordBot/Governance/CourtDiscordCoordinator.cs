using Discord;
using Discord.WebSocket;

namespace Content.DiscordBot.Governance;

public sealed class CourtDiscordCoordinator(
    DiscordSocketClient client,
    CommunityCourtService court,
    Config config)
{
    private HashSet<ulong>? _guildMembers;
    private DateTime _guildMembersRefreshedAt;
    private bool _courtChannelValidated;

    public async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Clamp(config.CourtSchedulerSeconds, 10, 3600));
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync();
            }
            catch (Exception exception)
            {
                await Logger.Error("Community Court scheduler iteration failed", exception);
            }

            await Task.Delay(delay, cancellationToken);
        }
    }

    public async Task ProcessOnceAsync()
    {
        if (!config.CourtEnabled)
            return;
        if (client.ConnectionState != ConnectionState.Connected)
            return;
        var guild = client.GetGuild(config.Guild)
            ?? throw new InvalidOperationException($"Discord bot cannot access configured guild {config.Guild}.");
        await ValidateCourtChannelAsync();
        var available = await GuildMembersAsync();
        await court.ProcessDeadlinesAsync(available);
        foreach (var courtCase in await court.CasesWithoutThreadsAsync())
            await EnsureCaseThreadAsync(courtCase);
        await NotifyJurorsAsync();
        await PublishVerdictsAsync();
    }

    private async Task ValidateCourtChannelAsync()
    {
        if (_courtChannelValidated)
            return;
        if (config.CourtChannel == 0)
            throw new InvalidOperationException("CourtChannel is not configured.");

        var channel = client.GetChannel(config.CourtChannel)
            ?? throw new InvalidOperationException($"Court channel {config.CourtChannel} is unavailable.");
        if (channel is not SocketForumChannel && channel is not SocketTextChannel)
            throw new InvalidOperationException($"Court channel {config.CourtChannel} is not a forum or text channel.");

        _courtChannelValidated = true;
        var name = channel is SocketGuildChannel guildChannel ? guildChannel.Name : channel.Id.ToString();
        await Logger.Info($"Community Court channel '{name}' ({channel.Id}) is available as {channel.GetType().Name}.");
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
                // Linked SS14 account is no longer a member of the configured guild.
            }
        }
        _guildMembers = members;
        _guildMembersRefreshedAt = DateTime.UtcNow;
        return members;
    }

    public async Task<IThreadChannel> EnsureCaseThreadAsync(GovernanceCourtCase courtCase)
    {
        if (courtCase.DiscordThreadId is { } existing && client.GetChannel((ulong) existing) is SocketThreadChannel cached)
            return cached;
        if (config.CourtChannel == 0)
            throw new InvalidOperationException("CourtChannel is not configured.");
        var channel = client.GetChannel(config.CourtChannel)
            ?? throw new InvalidOperationException($"Court channel {config.CourtChannel} is unavailable.");
        var name = $"суд-{courtCase.Id:000000}";
        var embed = await BuildCaseEmbedAsync(courtCase);
        IThreadChannel thread;
        if (channel is SocketForumChannel forum)
        {
            thread = await forum.CreatePostAsync(
                name,
                ThreadArchiveDuration.OneWeek,
                null,
                string.Empty,
                embed);
        }
        else if (channel is SocketTextChannel text)
        {
            thread = await text.CreateThreadAsync(name, ThreadType.PublicThread, ThreadArchiveDuration.OneWeek);
            await thread.SendMessageAsync(embed: embed);
        }
        else
        {
            throw new InvalidOperationException($"Court channel {config.CourtChannel} is not a forum or text channel.");
        }
        await court.AttachThreadAsync(courtCase.Id, thread.Id);
        return thread;
    }

    public async Task PublishStatementAsync(long caseId, GovernanceCourtStatement statement)
    {
        var courtCase = await court.GetCaseAsync(caseId);
        var thread = await EnsureCaseThreadAsync(courtCase);
        var author = await court.GetAccountAsync(statement.AuthorUserId);
        var embed = new EmbedBuilder()
            .WithTitle(statement.Kind == "defense" ? $"Защита по делу №{caseId}" : $"Материал по делу №{caseId}")
            .WithDescription(statement.Body)
            .WithColor(statement.Kind == "defense" ? Color.Blue : Color.Orange)
            .WithFooter($"{author.Name} • SS14 {author.PlayerId}")
            .WithCurrentTimestamp();
        if (!string.IsNullOrWhiteSpace(statement.EvidenceReference))
            embed.AddField("Доказательство", statement.EvidenceReference);
        await thread.SendMessageAsync(embed: embed.Build());
    }

    public async Task<Embed> BuildStatusEmbedAsync(long caseId)
    {
        var courtCase = await court.GetCaseAsync(caseId);
        var embed = await BuildCaseEmbedAsync(courtCase);
        return embed.ToEmbedBuilder()
            .AddField("Вердикт", VerdictText(courtCase.Verdict), true)
            .AddField("Наказание", SanctionText(courtCase), true)
            .Build();
    }

    private async Task NotifyJurorsAsync()
    {
        foreach (var (invitation, user) in await court.PendingNotificationsAsync())
        {
            try
            {
                IUser discordUser = client.GetUser((ulong) user.DiscordUserId) ??
                    (IUser) await client.Rest.GetUserAsync((ulong) user.DiscordUserId);
                var dm = await discordUser.CreateDMChannelAsync();
                await dm.SendMessageAsync(
                    $"Вас пригласили в присяжные RUCM по делу №{invitation.EntityId}. " +
                    $"Ответьте командой `/суд присяжный` до <t:{new DateTimeOffset(invitation.ExpiresAt).ToUnixTimeSeconds()}:F>. " +
                    "То же приглашение доступно во внутриигровом EUI.");
                await court.MarkInvitationNotifiedAsync(invitation.Id);
            }
            catch (Exception exception)
            {
                await Logger.Error($"Could not notify juror {user.DiscordUserId} for invitation {invitation.Id}", exception);
            }
        }
    }

    private async Task PublishVerdictsAsync()
    {
        foreach (var courtCase in await court.UnpublishedVerdictsAsync())
        {
            var thread = await EnsureCaseThreadAsync(courtCase);
            var message = await thread.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle($"Решение Community Court по делу №{courtCase.Id}")
                .WithDescription(VerdictText(courtCase.Verdict))
                .AddField("Назначенная мера", SanctionText(courtCase))
                .WithColor(courtCase.Verdict == CourtVerdicts.Guilty ? Color.Red : Color.Green)
                .WithCurrentTimestamp()
                .Build());
            await court.MarkPublishedAsync(courtCase.Id, message.Id);
            await thread.ModifyAsync(properties =>
            {
                properties.Locked = true;
                properties.Archived = true;
            });
        }
    }

    private async Task<Embed> BuildCaseEmbedAsync(GovernanceCourtCase courtCase)
    {
        var claimant = await court.GetAccountAsync(courtCase.ClaimantUserId);
        var defendant = await court.GetAccountAsync(courtCase.DefendantUserId);
        var statements = await court.GetStatementsAsync(courtCase.Id);
        var complaint = statements.FirstOrDefault(value => value.Kind == "complaint");
        var embed = new EmbedBuilder()
            .WithTitle($"Community Court • дело №{courtCase.Id}")
            .WithDescription(courtCase.Summary)
            .WithColor(Color.DarkOrange)
            .AddField("Раунд", courtCase.RoundId, true)
            .AddField("Истец", $"<@{claimant.DiscordId}> ({claimant.Name})", true)
            .AddField("Ответчик", $"<@{defendant.DiscordId}> ({defendant.Name})", true)
            .AddField("Стадия", StatusText(courtCase.Status), true)
            .AddField("Срок защиты", $"<t:{new DateTimeOffset(courtCase.DefenseDeadline).ToUnixTimeSeconds()}:F>", true)
            .WithCurrentTimestamp();
        if (!string.IsNullOrWhiteSpace(complaint?.EvidenceReference))
            embed.AddField("Доказательство", complaint.EvidenceReference);
        return embed.Build();
    }

    private static string StatusText(string status) => status switch
    {
        CourtStatuses.Defense => "Защита",
        CourtStatuses.AwaitingJury => "Формирование коллегии",
        CourtStatuses.Jury => "Голосование о виновности",
        CourtStatuses.Sentencing => "Голосование о наказании",
        CourtStatuses.Verdict => "Решение вынесено",
        CourtStatuses.Executed => "Решение исполнено",
        CourtStatuses.Overturned => "Решение отменено",
        _ => status,
    };

    private static string VerdictText(string? verdict) => verdict switch
    {
        CourtVerdicts.Guilty => "Виновен",
        CourtVerdicts.NotGuilty => "Не виновен",
        CourtVerdicts.InsufficientEvidence => "Недостаточно доказательств",
        _ => "Ещё не вынесен",
    };

    private static string SanctionText(GovernanceCourtCase courtCase) => courtCase.SanctionType switch
    {
        CourtSanctions.Warning => "Предупреждение",
        CourtSanctions.GameBan => $"Блокировка игры на {courtCase.SanctionDays} дн.",
        CourtSanctions.JobBan => $"Блокировка роли `{courtCase.SanctionRole}` на {courtCase.SanctionDays} дн.",
        _ => "Не назначено",
    };
}
