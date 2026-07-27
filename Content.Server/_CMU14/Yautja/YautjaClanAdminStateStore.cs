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
    private YautjaClanAdminEuiState _state = new(
        [],
        "",
        "",
        "",
        0,
        null,
        YautjaClanAdminMutationKind.None);

    public YautjaClanAdminEuiState Get()
    {
        return Volatile.Read(ref _state);
    }

    public void Set(YautjaClanAdminEuiState state)
    {
        Volatile.Write(ref _state, state);
    }
}
