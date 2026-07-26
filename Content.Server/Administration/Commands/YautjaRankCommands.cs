using Content.Server._CMU14.Yautja;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared._CMU14.Yautja;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class YautjaRankCommand : LocalizedCommands
{
    [Dependency] private IPlayerLocator _playerLocator = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private YautjaRankManager _rankManager = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;

    public override string Command => "yautjarank";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError("Usage: yautjarank <player> <unblooded|blooded|elite|elder|leader|ancient>");
            return;
        }

        if (!TryParsePersistentRank(args[1], out var rank))
        {
            shell.WriteError("Invalid Yautja rank. Young Blood is reserved for the special hunt role.");
            return;
        }

        var player = await _playerLocator.LookupIdByNameOrIdAsync(args[0]);
        if (player == null)
        {
            shell.WriteError($"Player '{args[0]}' was not found.");
            return;
        }

        var previous = await _db.GetYautjaRank(player.UserId.UserId);
        await _rankManager.Set(player.UserId, rank);

        _adminLog.Add(
            LogType.AdminCommands,
            LogImpact.Medium,
            $"{shell.Player?.Name ?? "Console"} set Yautja rank for {player.Username} ({player.UserId}) from {previous?.ToString() ?? "unset"} to {rank}.");
        shell.WriteLine($"Set {player.Username}'s Yautja rank to {rank}.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), "player");

        if (args.Length == 2)
            return CompletionResult.FromHintOptions(PersistentRankNames, "Yautja rank");

        return CompletionResult.Empty;
    }

    internal static readonly string[] PersistentRankNames =
    [
        "unblooded",
        "blooded",
        "elite",
        "elder",
        "leader",
        "ancient",
    ];

    internal static bool TryParsePersistentRank(string value, out YautjaRank rank)
    {
        rank = value.Trim().ToLowerInvariant() switch
        {
            "unblooded" => YautjaRank.Unblooded,
            "blooded" => YautjaRank.Blooded,
            "elite" => YautjaRank.Elite,
            "elder" => YautjaRank.Elder,
            "leader" => YautjaRank.Leader,
            "ancient" => YautjaRank.Ancient,
            _ => default,
        };

        return value.Trim().ToLowerInvariant() is "unblooded" or "blooded" or "elite" or "elder" or "leader" or "ancient";
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed partial class YautjaGetRankCommand : LocalizedCommands
{
    [Dependency] private IPlayerLocator _playerLocator = default!;
    [Dependency] private IServerDbManager _db = default!;

    public override string Command => "yautjaget";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Usage: yautjaget <player>");
            return;
        }

        var player = await _playerLocator.LookupIdByNameOrIdAsync(args[0]);
        if (player == null)
        {
            shell.WriteError($"Player '{args[0]}' was not found.");
            return;
        }

        var rank = await _db.GetYautjaRank(player.UserId.UserId);
        shell.WriteLine($"{player.Username}'s stored Yautja rank: {rank?.ToString() ?? "unset (defaults to Blooded)"}.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), "player")
            : CompletionResult.Empty;
    }
}
