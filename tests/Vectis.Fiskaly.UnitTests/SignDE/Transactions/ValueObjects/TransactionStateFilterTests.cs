using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.ValueObjects;

public class TransactionStateFilterTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void FromStates_WithSingleState_CreatesFilter()
    {
        TxStateFilter filter = TxStateFilter.FromStates(TxState.Active);

        Assert.Single(filter.States);
        Assert.Equal(TxState.Active, filter.States[0]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromStates_WithMultipleStates_CreatesFilter()
    {
        TxStateFilter filter = TxStateFilter.FromStates(TxState.Active, TxState.Finished);

        Assert.Equal(2, filter.States.Count);
        Assert.Contains(TxState.Active, filter.States);
        Assert.Contains(TxState.Finished, filter.States);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromStates_WithDuplicates_RemovesDuplicates()
    {
        TxStateFilter filter = TxStateFilter.FromStates(
            TxState.Active,
            TxState.Finished,
            TxState.Active);

        Assert.Equal(2, filter.States.Count);
        Assert.Contains(TxState.Active, filter.States);
        Assert.Contains(TxState.Finished, filter.States);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromStates_WithNullArray_ThrowsArgumentNullException()
    {
        TxState[]? nullStates = null;

        Assert.Throws<ArgumentNullException>(() => TxStateFilter.FromStates(nullStates!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromStates_WithEmptyArray_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            TxStateFilter.FromStates(Array.Empty<TxState>()));

        Assert.Contains("At least one state must be provided", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromStates_WithEnumerable_CreatesFilter()
    {
        List<TxState> states = new List<TxState> { TxState.Active, TxState.Cancelled };

        TxStateFilter filter = TxStateFilter.FromStates(states);

        Assert.Equal(2, filter.States.Count);
        Assert.Contains(TxState.Active, filter.States);
        Assert.Contains(TxState.Cancelled, filter.States);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromStates_WithNullEnumerable_ThrowsArgumentNullException()
    {
        IEnumerable<TxState>? nullStates = null;

        Assert.Throws<ArgumentNullException>(() => TxStateFilter.FromStates(nullStates!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromStates_WithEmptyEnumerable_ThrowsArgumentException()
    {
        IEnumerable<TxState> emptyStates = Enumerable.Empty<TxState>();

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            TxStateFilter.FromStates(emptyStates));

        Assert.Contains("At least one state must be provided", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiValues_ReturnsApiStringValues()
    {
        TxStateFilter filter = TxStateFilter.FromStates(TxState.Active, TxState.Finished);

        IReadOnlyList<string> apiValues = filter.ToApiValues();

        Assert.Equal(2, apiValues.Count);
        Assert.All(apiValues, value => Assert.NotEmpty(value));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsCommaSeparatedStates()
    {
        TxStateFilter filter = TxStateFilter.FromStates(TxState.Active, TxState.Finished);

        string result = filter.ToString();

        Assert.NotEmpty(result);
        Assert.Contains(",", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void States_ReturnsReadOnlyList()
    {
        TxStateFilter filter = TxStateFilter.FromStates(TxState.Active);

        IReadOnlyList<TxState> states = filter.States;

        Assert.IsAssignableFrom<IReadOnlyList<TxState>>(states);
    }
}
