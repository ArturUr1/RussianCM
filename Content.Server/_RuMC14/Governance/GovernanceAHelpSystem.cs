using System.Linq;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Shared._RuMC14.Governance;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._RuMC14.Governance;

/// <summary>
/// Owns the modern in-game Governance AHelp surface for both players and temporary responders.
/// This system does not depend on BwoinkSystem: messages, assignment and status live in PostgreSQL
/// and are presented directly through Governance EUIs.
/// </summary>
public sealed class GovernanceAHelpSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IServerDbManager _database = default!;
    [Dependency] private readonly EuiManager _euis = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly GovernanceManager _governance = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    private readonly HashSet<GovernanceAHelpQueueEui> _responderEuis = new();
    private readonly Dictionary<NetUserId, HashSet<GovernanceAHelpPlayerEui>> _playerEuis = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GovernanceAHelpOpenRequest>(OnOpenRequest);
    }

    private async void OnOpenRequest(GovernanceAHelpOpenRequest message, EntitySessionEventArgs args)
    {
        if (await CanUseResponderAsync(args.SenderSession))
        {
            EntityManager.System<GovernanceDutySystem>().OpenAHelpQueue(args.SenderSession);
            return;
        }

        OpenPlayerHelp(args.SenderSession);
    }

    public void OpenPlayerHelp(ICommonSession player)
    {
        if (!_governance.Enabled || _ticker.RoundId <= 0)
        {
            _chat.DispatchServerMessage(player, Loc.GetString("governance-ahelp-player-unavailable"));
            return;
        }

        _euis.OpenEui(new GovernanceAHelpPlayerEui(this), player);
    }

    public async Task<bool> CanUseResponderAsync(ICommonSession player)
    {
        if (!_governance.Enabled || _ticker.RoundId <= 0 ||
            player.AttachedEntity is not { } entity || !HasComp<GhostComponent>(entity))
            return false;

        return await _governance.AuthorizeAsync(player.UserId, _ticker.RoundId, "moderation.ahelp") != null;
    }

    public async Task<IReadOnlyList<GovernanceAHelpTicketInfo>> GetQueueAsync(ICommonSession player)
    {
        if (!await CanUseResponderAsync(player))
            return [];

        return await _database.GetGovernanceAHelpQueueAsync(player.UserId, _ticker.RoundId);
    }

    public async Task<bool> ClaimAsync(ICommonSession player, long ticketId)
    {
        if (!await CanUseResponderAsync(player))
            return false;

        var claimed = await _database.ClaimGovernanceAHelpAsync(ticketId, player.UserId, _ticker.RoundId);
        if (claimed)
        {
            await RefreshResponderEuisAsync();
            await RefreshTicketReporterAsync(ticketId, player);
        }

        return claimed;
    }

    public async Task<IReadOnlyList<GovernanceAHelpModernTranscriptLine>> GetResponderTranscriptAsync(
        ICommonSession player,
        long ticketId)
    {
        if (!await CanUseResponderAsync(player))
            return [];

        return await _database.GetGovernanceAHelpResponderTranscriptAsync(
            ticketId,
            player.UserId,
            _ticker.RoundId);
    }

    public async Task<bool> SendResponderMessageAsync(ICommonSession player, long ticketId, string text)
    {
        if (!await CanUseResponderAsync(player) || string.IsNullOrWhiteSpace(text))
            return false;

        var reporter = await _database.SendGovernanceAHelpResponderMessageAsync(
            ticketId,
            player.UserId,
            _ticker.RoundId,
            text);
        if (reporter == null)
            return false;

        await RefreshResponderEuisAsync();
        await RefreshPlayerEuisAsync(reporter.Value);

        if (_players.TryGetSessionById(reporter.Value, out var reporterSession))
        {
            var preview = text.Trim();
            if (preview.Length > 160)
                preview = preview[..160] + "…";
            RaiseNetworkEvent(new GovernanceAHelpPlayerReplyReceived(ticketId, preview), reporterSession);
        }

        return true;
    }

    public async Task<bool> SetStatusAsync(ICommonSession player, long ticketId, string status)
    {
        if (!await CanUseResponderAsync(player))
            return false;

        var queue = await _database.GetGovernanceAHelpQueueAsync(player.UserId, _ticker.RoundId);
        var ticket = queue.SingleOrDefault(value => value.Id == ticketId && value.ClaimedByMe);
        if (ticket == null)
            return false;

        var changed = await _database.SetGovernanceAHelpStatusAsync(
            ticketId,
            player.UserId,
            _ticker.RoundId,
            status);
        if (!changed)
            return false;

        await RefreshResponderEuisAsync();
        await RefreshPlayerEuisAsync(ticket.ReporterUserId);
        return true;
    }

    public Task<GovernanceAHelpPlayerTicketInfo?> GetPlayerTicketAsync(ICommonSession player)
    {
        return _database.GetGovernanceAHelpPlayerTicketAsync(player.UserId, _ticker.RoundId);
    }

    public Task<IReadOnlyList<GovernanceAHelpModernTranscriptLine>> GetPlayerTranscriptAsync(ICommonSession player)
    {
        return _database.GetGovernanceAHelpPlayerTranscriptAsync(player.UserId, _ticker.RoundId);
    }

    public async Task<bool> SendPlayerMessageAsync(ICommonSession player, string text)
    {
        if (!_governance.Enabled || _ticker.RoundId <= 0 || string.IsNullOrWhiteSpace(text))
            return false;

        var ticketId = await _database.SendGovernanceAHelpPlayerMessageAsync(
            player.UserId,
            _ticker.RoundId,
            text);
        if (ticketId == null)
            return false;

        await RefreshPlayerEuisAsync(player.UserId);
        await RefreshResponderEuisAsync();

        var responderId = await _database.GetGovernanceAHelpResponderAsync(player.UserId, _ticker.RoundId);
        if (responderId != null && _players.TryGetSessionById(responderId.Value, out var responderSession))
        {
            var preview = text.Trim();
            if (preview.Length > 160)
                preview = preview[..160] + "…";
            RaiseNetworkEvent(
                new GovernanceAHelpResponderReplyReceived(ticketId.Value, player.Name, preview),
                responderSession);
        }

        return true;
    }

    public async Task<bool> ResolveByPlayerAsync(ICommonSession player)
    {
        if (!_governance.Enabled || _ticker.RoundId <= 0)
            return false;

        var resolved = await _database.ResolveGovernanceAHelpByReporterAsync(player.UserId, _ticker.RoundId);
        if (resolved)
        {
            await RefreshPlayerEuisAsync(player.UserId);
            await RefreshResponderEuisAsync();
        }

        return resolved;
    }

    public void RegisterResponderEui(GovernanceAHelpQueueEui eui)
    {
        _responderEuis.Add(eui);
    }

    public void UnregisterResponderEui(GovernanceAHelpQueueEui eui)
    {
        _responderEuis.Remove(eui);
    }

    public void RegisterPlayerEui(NetUserId userId, GovernanceAHelpPlayerEui eui)
    {
        if (!_playerEuis.TryGetValue(userId, out var euis))
        {
            euis = new HashSet<GovernanceAHelpPlayerEui>();
            _playerEuis[userId] = euis;
        }

        euis.Add(eui);
    }

    public void UnregisterPlayerEui(NetUserId userId, GovernanceAHelpPlayerEui eui)
    {
        if (!_playerEuis.TryGetValue(userId, out var euis))
            return;

        euis.Remove(eui);
        if (euis.Count == 0)
            _playerEuis.Remove(userId);
    }

    public async Task RefreshResponderEuisAsync()
    {
        foreach (var eui in _responderEuis.ToArray())
            await eui.RefreshFromSystemAsync();
    }

    public async Task RefreshPlayerEuisAsync(NetUserId userId)
    {
        if (!_playerEuis.TryGetValue(userId, out var euis))
            return;

        foreach (var eui in euis.ToArray())
            await eui.RefreshFromSystemAsync();
    }

    private async Task RefreshTicketReporterAsync(long ticketId, ICommonSession responder)
    {
        var queue = await _database.GetGovernanceAHelpQueueAsync(responder.UserId, _ticker.RoundId);
        var ticket = queue.SingleOrDefault(value => value.Id == ticketId);
        if (ticket != null)
            await RefreshPlayerEuisAsync(ticket.ReporterUserId);
    }
}
