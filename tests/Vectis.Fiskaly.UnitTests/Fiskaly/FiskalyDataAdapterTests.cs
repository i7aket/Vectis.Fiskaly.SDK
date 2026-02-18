/*
using System.Collections.Generic;
using AwesomeAssertions;
using Radzen;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.UI.Services;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class FiskalyDataAdapterTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void MapTransactionQuery_WithSortDescriptorAndStateFilter_MapsCorrectly()
    {
        var adapter = new FiskalyDataAdapter();
        var args = new LoadDataArgs
        {
            Sorts = new List<SortDescriptor>
            {
                new()
                {
                    Property = "TimeEnd",
                    SortOrder = SortOrder.Descending
                }
            },
            Top = 25,
            Skip = 5,
            Filters = new List<FilterDescriptor>
            {
                new()
                {
                    Property = "State",
                    FilterOperator = FilterOperator.Equals,
                    FilterValue = TxState.Active
                }
            }
        };

        var result = adapter.MapTransactionQuery(args);

        result.Sort.Should().Be(TransactionSortOption.By(TransactionSortField.TimeEnd, SortDirection.Descending));
        result.StateFilter.Should().NotBeNull();
        result.StateFilter!.States.Should().ContainSingle()
            .Which.Should().Be(TxState.Active);
        result.Limit.Should().Be(25);
        result.Offset.Should().Be(5);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MapTransactionQuery_WithLegacyOrderBy_MapsSort()
    {
        var adapter = new FiskalyDataAdapter();
        var args = new LoadDataArgs
        {
            OrderBy = "TimeStart desc"
        };

        var result = adapter.MapTransactionQuery(args);

        result.Sort.Should().Be(TransactionSortOption.By(TransactionSortField.TimeStart, SortDirection.Descending));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MapTransactionQuery_WithUnsupportedSort_DoesNotSetSort()
    {
        var adapter = new FiskalyDataAdapter();
        var args = new LoadDataArgs
        {
            OrderBy = "Revision desc"
        };

        var result = adapter.MapTransactionQuery(args);

        result.Sort.Should().BeNull();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MapTransactionQuery_WithFilterStringFallback_ParsesStates()
    {
        var adapter = new FiskalyDataAdapter();
        var args = new LoadDataArgs
        {
            Filter = "State in ('ACTIVE', 'FINISHED')"
        };

        var result = adapter.MapTransactionQuery(args);

        result.StateFilter.Should().NotBeNull();
        result.StateFilter!.States.Should().Contain(new[] { TxState.Active, TxState.Finished });
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MapTransactionQuery_WithEnumFilterValue_ParsesState()
    {
        var adapter = new FiskalyDataAdapter();
        var args = new LoadDataArgs
        {
            Filters = new List<FilterDescriptor>
            {
                new()
                {
                    Property = "State",
                    FilterValue = TxState.Finished
                }
            }
        };

        var result = adapter.MapTransactionQuery(args);

        result.StateFilter.Should().NotBeNull();
        result.StateFilter!.States.Should().ContainSingle().Which.Should().Be(TxState.Finished);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MapTransactionQuery_WithNumericFilterValue_ParsesState()
    {
        var adapter = new FiskalyDataAdapter();
        var args = new LoadDataArgs
        {
            Filters = new List<FilterDescriptor>
            {
                new()
                {
                    Property = "State",
                    FilterValue = (int)TxState.Active
                }
            }
        };

        var result = adapter.MapTransactionQuery(args);

        result.StateFilter.Should().NotBeNull();
        result.StateFilter!.States.Should().ContainSingle().Which.Should().Be(TxState.Active);
    }

}
*/
