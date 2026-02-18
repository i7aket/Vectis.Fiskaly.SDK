using AwesomeAssertions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class TransactionQueryParametersTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithSortStatesAndPaging_ProducesExpectedPairs()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters
        {
            Sort = TransactionSortOption.By(TransactionSortField.TimeEnd, SortDirection.Descending),
            StateFilter = TxStateFilter.FromStates(TxState.Active, TxState.Finished),
            Limit = 50,
            Offset = 100,
            ShowDeleted = true
        };

        List<KeyValuePair<string, string?>> pairs = parameters.ToQueryParameters().ToList();

        pairs.Should().Contain(new KeyValuePair<string, string?>("order_by", "time_end"));
        pairs.Should().Contain(new KeyValuePair<string, string?>("order", "desc"));
        pairs.Should().Contain(new KeyValuePair<string, string?>("states[0]", "ACTIVE"));
        pairs.Should().Contain(new KeyValuePair<string, string?>("states[1]", "FINISHED"));
        pairs.Should().Contain(new KeyValuePair<string, string?>("limit", "50"));
        pairs.Should().Contain(new KeyValuePair<string, string?>("offset", "100"));
        pairs.Should().Contain(new KeyValuePair<string, string?>("show_deleted", "true"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithoutValues_ReturnsEmpty()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        parameters.ToQueryParameters().Should().BeEmpty();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TransactionSortOption_ToQueryPair_ReturnsFieldAndDirection()
    {
        TransactionSortOption option = TransactionSortOption.By(TransactionSortField.Number, SortDirection.Ascending);

        (string orderBy, string order) = option.ToQueryPair();

        orderBy.Should().Be("number");
        order.Should().Be("asc");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TransactionStateFilter_RemovesDuplicatesAndKeepsOrder()
    {
        TxStateFilter filter = TxStateFilter.FromStates(TxState.Active, TxState.Finished, TxState.Active);

        filter.States.Should().HaveCount(2);
        filter.ToApiValues().Should().ContainInOrder("ACTIVE", "FINISHED");
    }
}
