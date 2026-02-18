using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

public sealed class TxStateFilter
{
    private readonly TxState[] _states;

    private TxStateFilter(TxState[] states)
    {
        if (states.Length == 0)
        {
            throw new ArgumentException("At least one state must be provided.", nameof(states));
        }

        _states = [..states.Distinct()];
    }

    public IReadOnlyList<TxState> States => _states;

    public static TxStateFilter FromStates(params TxState[] states)
    {
        if (states is null)
        {
            throw new ArgumentNullException(nameof(states));
        }

        return new TxStateFilter(states);
    }

    public static TxStateFilter FromStates(IEnumerable<TxState> states)
    {
        if (states is null)
        {
            throw new ArgumentNullException(nameof(states));
        }

        return new TxStateFilter([..states]);
    }

    public IReadOnlyList<string> ToApiValues() => [.._states.Select(state => state.ToApiString())];

    public override string ToString() => string.Join(", ", _states.Select(state => state.ToApiString()));
}
