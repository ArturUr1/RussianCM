using System;
using Content.Server.Commands;
using Content.Server.GameTicking;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server._RuMC14.Governance;

public sealed class GovernanceStatusCommand : IConsoleCommand
{
    public string Command => "governance_status";
    public string Description => Loc.GetString("cmd-governance-status-description");
    public string Help => Loc.GetString("cmd-governance-status-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("cmd-governance-player-only"));
            return;
        }

        var governance = IoCManager.Resolve<GovernanceManager>();
        var ticker = IoCManager.Resolve<IEntityManager>().System<GameTicker>();
        var duty = await governance.RefreshDutyAsync(player.UserId);
        if (duty == null || duty.RoundId != ticker.RoundId)
        {
            shell.WriteLine(Loc.GetString("cmd-governance-status-inactive"));
            return;
        }

        shell.WriteLine(Loc.GetString(
            "cmd-governance-status-active",
            ("session", duty.Id),
            ("round", duty.RoundId),
            ("expires", duty.ExpiresAt)));
    }
}

public sealed class GovernanceFreezeCommand : IConsoleCommand
{
    public string Command => "governance_freeze";
    public string Description => Loc.GetString("cmd-governance-freeze-description");
    public string Help => Loc.GetString("cmd-governance-freeze-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } actor)
        {
            shell.WriteError(Loc.GetString("cmd-governance-player-only"));
            return;
        }

        if (args.Length < 4 || !int.TryParse(args[1], out var seconds) || !long.TryParse(args[2], out var actionId))
        {
            shell.WriteError(Help);
            return;
        }

        if (!CommandUtils.TryGetSessionByUsernameOrId(shell, args[0], actor, out var target))
            return;

        var reason = string.Join(' ', args[3..]);
        var system = IoCManager.Resolve<IEntityManager>().System<GovernanceSystem>();
        var result = await system.TryFreezeAsync(actor, target, seconds, actionId, reason);
        if (!result.Allowed)
        {
            shell.WriteError(Loc.GetString("cmd-governance-freeze-denied", ("reason", DenialText(result.Denial))));
            return;
        }

        shell.WriteLine(Loc.GetString(
            "cmd-governance-freeze-success",
            ("target", target.Name),
            ("seconds", seconds),
            ("incident", actionId)));
    }

    private static string DenialText(GovernanceDenial denial)
    {
        var key = denial switch
        {
            GovernanceDenial.Disabled => "governance-denial-disabled",
            GovernanceDenial.DatabaseUnavailable => "governance-denial-invalid-input",
            GovernanceDenial.InvalidInput => "governance-denial-invalid-input",
            GovernanceDenial.NotOnDuty => "governance-denial-not-on-duty",
            GovernanceDenial.NotObserver => "governance-denial-not-observer",
            GovernanceDenial.SelfTarget => "governance-denial-self-target",
            GovernanceDenial.InvalidDuration => "governance-denial-invalid-duration",
            GovernanceDenial.TargetUnavailable => "governance-denial-target-unavailable",
            GovernanceDenial.AlreadyFrozen => "governance-denial-already-frozen",
            GovernanceDenial.ActionNotApproved => "governance-denial-action-not-approved",
            _ => "governance-denial-unknown",
        };
        return Loc.GetString(key);
    }
}

public sealed class GovernanceRoundRemoveCommand : IConsoleCommand
{
    public string Command => "governance_round_remove";
    public string Description => Loc.GetString("cmd-governance-round-remove-description");
    public string Help => Loc.GetString("cmd-governance-round-remove-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } actor)
        {
            shell.WriteError(Loc.GetString("cmd-governance-player-only"));
            return;
        }
        if (args.Length < 3 || !long.TryParse(args[1], out var actionId))
        {
            shell.WriteError(Help);
            return;
        }
        if (!CommandUtils.TryGetSessionByUsernameOrId(shell, args[0], actor, out var target))
            return;
        var reason = string.Join(' ', args[2..]);
        var system = IoCManager.Resolve<IEntityManager>().System<GovernanceSystem>();
        var result = await system.TryRoundRemoveAsync(actor, target, actionId, reason);
        if (!result.Allowed)
        {
            shell.WriteError(Loc.GetString("cmd-governance-freeze-denied", ("reason", result.Denial.ToString())));
            return;
        }
        shell.WriteLine(Loc.GetString("cmd-governance-round-remove-success", ("target", target.Name), ("action", actionId)));
    }
}
