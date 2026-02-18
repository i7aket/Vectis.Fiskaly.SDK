using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Requests;

public class ListTransactionsQueryParametersTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesEmptyParameters()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        Assert.Null(parameters.Sort);
        Assert.Null(parameters.StateFilter);
        Assert.Null(parameters.Limit);
        Assert.Null(parameters.Offset);
        Assert.Null(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Sort_CanBeSet()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters
        {
            Sort = TransactionSortOption.By(TransactionSortField.TimeStart, SortDirection.Descending)
        };

        Assert.NotNull(parameters.Sort);
        Assert.Equal(TransactionSortField.TimeStart, parameters.Sort.Value.Field);
        Assert.Equal(SortDirection.Descending, parameters.Sort.Value.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void StateFilter_CanBeSet()
    {
        TxStateFilter filter = TxStateFilter.FromStates(TxState.Finished);
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters
        {
            StateFilter = filter
        };

        Assert.NotNull(parameters.StateFilter);
        Assert.Single(parameters.StateFilter.States);
        Assert.Equal(TxState.Finished, parameters.StateFilter.States[0]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_CanBeSet()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters
        {
            Limit = 50
        };

        Assert.Equal(50, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_CanBeSet()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters
        {
            Offset = 100
        };

        Assert.Equal(100, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShowDeleted_CanBeSet()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters
        {
            ShowDeleted = true
        };

        Assert.True(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters
        {
            Sort = TransactionSortOption.By(TransactionSortField.Number),
            StateFilter = TxStateFilter.FromStates(TxState.Active, TxState.Finished),
            Limit = 100,
            Offset = 200,
            ShowDeleted = false
        };

        Assert.NotNull(parameters.Sort);
        Assert.NotNull(parameters.StateFilter);
        Assert.Equal(100, parameters.Limit);
        Assert.Equal(200, parameters.Offset);
        Assert.False(parameters.ShowDeleted);
    }

    #region Limit Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_MinBound_1_IsValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        // Should not throw
        parameters.Limit = 1;

        Assert.Equal(1, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_MaxBound_100_IsValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        // Should not throw
        parameters.Limit = 100;

        Assert.Equal(100, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_MiddleValue_50_IsValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        // Should not throw
        parameters.Limit = 50;

        Assert.Equal(50, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_BelowMinimum_0_ThrowsArgumentOutOfRangeException()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 0);
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("Limit must be between 1 and 100", ex.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_Negative_ThrowsArgumentOutOfRangeException()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = -1);
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("Limit must be between 1 and 100", ex.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_AboveMaximum_101_ThrowsArgumentOutOfRangeException()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 101);
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("Limit must be between 1 and 100", ex.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_WayAboveMaximum_1000_ThrowsArgumentOutOfRangeException()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 1000);
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("Limit must be between 1 and 100", ex.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_Null_IsValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        // Should not throw
        parameters.Limit = null;

        Assert.Null(parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_CanBeChangedFromValidToAnotherValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters { Limit = 50 };

        parameters.Limit = 75;

        Assert.Equal(75, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_CanBeSetToNullAfterHavingValue()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters { Limit = 50 };

        parameters.Limit = null;

        Assert.Null(parameters.Limit);
    }

    #endregion

    #region Offset Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_MinBound_0_IsValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        // Should not throw
        parameters.Offset = 0;

        Assert.Equal(0, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_PositiveValue_100_IsValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        // Should not throw
        parameters.Offset = 100;

        Assert.Equal(100, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_LargeValue_10000_IsValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        // Should not throw (no upper bound on Offset)
        parameters.Offset = 10000;

        Assert.Equal(10000, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_Negative_1_ThrowsArgumentOutOfRangeException()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Offset = -1);
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("Offset must be zero or positive", ex.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_VeryNegative_ThrowsArgumentOutOfRangeException()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Offset = -1000);
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("Offset must be zero or positive", ex.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_Null_IsValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        // Should not throw
        parameters.Offset = null;

        Assert.Null(parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_CanBeChangedFromValidToAnotherValid()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters { Offset = 0 };

        parameters.Offset = 500;

        Assert.Equal(500, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_CanBeSetToNullAfterHavingValue()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters { Offset = 100 };

        parameters.Offset = null;

        Assert.Null(parameters.Offset);
    }

    #endregion

    #region Combined Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void LimitAndOffset_BothValidTogether()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        parameters.Limit = 50;
        parameters.Offset = 150;

        Assert.Equal(50, parameters.Limit);
        Assert.Equal(150, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void LimitAndOffset_PaginationExample_Page1()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        parameters.Limit = 100;
        parameters.Offset = 0;

        Assert.Equal(100, parameters.Limit);
        Assert.Equal(0, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void LimitAndOffset_PaginationExample_Page2()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters();

        parameters.Limit = 100;
        parameters.Offset = 100;

        Assert.Equal(100, parameters.Limit);
        Assert.Equal(100, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void LimitAndOffset_InvalidLimitDoesNotAffectOffset()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters { Offset = 100 };

        Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 0);

        // Offset should still be valid
        Assert.Equal(100, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void LimitAndOffset_InvalidOffsetDoesNotAffectLimit()
    {
        ListTransactionsQueryParameters parameters = new ListTransactionsQueryParameters { Limit = 50 };

        Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Offset = -5);

        // Limit should still be valid
        Assert.Equal(50, parameters.Limit);
    }

    #endregion
}
