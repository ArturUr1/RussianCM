using System;
using System.Collections.Generic;
using System.Threading;
using Content.Shared._CMU14.Yautja;

namespace Content.Server._CMU14.Yautja;

/// <summary>
/// Holds the last completed state for the clan administration EUI.
/// Database work must never be performed from <c>GetNewState</c>, which is called
/// synchronously by the EUI manager on the server tick.
/// </summary>
public sealed class YautjaClanAdminStateStore
{
    private (int ClanId, YautjaClanAdminMutationKind Kind, string StatusMessage)? _pendingMutation;
    private YautjaClanAdminEuiState _state = new(
        [],
        "",
        "",
        "",
        0,
        null,
        YautjaClanAdminMutationKind.None);

    public bool CanStartMutation => _pendingMutation == null;

    public YautjaClanAdminEuiState Get()
    {
        return Volatile.Read(ref _state);
    }

    public void Set(YautjaClanAdminEuiState state)
    {
        Volatile.Write(ref _state, state);
    }

    public void StageMutation(int clanId, YautjaClanAdminMutationKind kind, string statusMessage)
    {
        if (kind == YautjaClanAdminMutationKind.None)
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (_pendingMutation != null)
            throw new InvalidOperationException("A clan mutation is already waiting for a fresh snapshot.");

        _pendingMutation = (clanId, kind, statusMessage);
    }

    public YautjaClanAdminEuiState PublishFreshSnapshot(
        List<YautjaClanAdminClanState> clans,
        string inspectedPlayer,
        string inspectedSummary,
        string statusMessage)
    {
        var previous = Get();
        var version = previous.ClanMutationVersion;
        var lastMutatedClanId = previous.LastMutatedClanId;
        var lastMutationKind = previous.LastMutationKind;

        if (_pendingMutation is { } pending)
        {
            version++;
            lastMutatedClanId = pending.ClanId;
            lastMutationKind = pending.Kind;
            statusMessage = pending.StatusMessage;
        }

        var state = new YautjaClanAdminEuiState(
            clans,
            inspectedPlayer,
            inspectedSummary,
            statusMessage,
            version,
            lastMutatedClanId,
            lastMutationKind);
        Set(state);
        _pendingMutation = null;
        return state;
    }

    public YautjaClanAdminEuiState PublishRefreshFailure(string statusMessage)
    {
        var previous = Get();
        var state = new YautjaClanAdminEuiState(
            previous.Clans,
            previous.InspectedPlayer,
            previous.InspectedSummary,
            statusMessage,
            previous.ClanMutationVersion,
            previous.LastMutatedClanId,
            previous.LastMutationKind);
        Set(state);
        return state;
    }
}
