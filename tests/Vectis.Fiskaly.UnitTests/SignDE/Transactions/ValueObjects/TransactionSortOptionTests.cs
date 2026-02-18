using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.ValueObjects;

public class TransactionSortOptionTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesInstanceWithFieldAndDirection()
    {
        TransactionSortOption option = new TransactionSortOption(TransactionSortField.TimeStart, SortDirection.Ascending);

        Assert.Equal(TransactionSortField.TimeStart, option.Field);
        Assert.Equal(SortDirection.Ascending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithDescending_CreatesInstance()
    {
        TransactionSortOption option = new TransactionSortOption(TransactionSortField.Number, SortDirection.Descending);

        Assert.Equal(TransactionSortField.Number, option.Field);
        Assert.Equal(SortDirection.Descending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithInvalidField_ThrowsArgumentOutOfRangeException()
    {
        TransactionSortField invalidField = (TransactionSortField)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TransactionSortOption(invalidField, SortDirection.Ascending));

        Assert.Contains("Unsupported transaction sort field", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithInvalidDirection_ThrowsArgumentOutOfRangeException()
    {
        SortDirection invalidDirection = (SortDirection)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TransactionSortOption(TransactionSortField.TimeStart, invalidDirection));

        Assert.Contains("Unsupported sort direction", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void By_WithDefaultDirection_CreatesAscendingOption()
    {
        TransactionSortOption option = TransactionSortOption.By(TransactionSortField.TimeStart);

        Assert.Equal(TransactionSortField.TimeStart, option.Field);
        Assert.Equal(SortDirection.Ascending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void By_WithExplicitDirection_CreatesDescendingOption()
    {
        TransactionSortOption option = TransactionSortOption.By(TransactionSortField.Number, SortDirection.Descending);

        Assert.Equal(TransactionSortField.Number, option.Field);
        Assert.Equal(SortDirection.Descending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryPair_ReturnsApiFormattedPair()
    {
        TransactionSortOption option = new TransactionSortOption(TransactionSortField.TimeStart, SortDirection.Ascending);

        (string orderBy, string order) = option.ToQueryPair();

        Assert.NotEmpty(orderBy);
        Assert.NotEmpty(order);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryPair_WithDescending_ReturnsDescOrder()
    {
        TransactionSortOption option = new TransactionSortOption(TransactionSortField.Number, SortDirection.Descending);

        (string orderBy, string order) = option.ToQueryPair();

        Assert.NotEmpty(orderBy);
        Assert.NotEmpty(order);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsFieldAndDirection()
    {
        TransactionSortOption option = new TransactionSortOption(TransactionSortField.TimeStart, SortDirection.Ascending);

        string result = option.ToString();

        Assert.Contains("TimeStart", result);
        Assert.Contains("Ascending", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameValues_AreEqual()
    {
        TransactionSortOption option1 = new TransactionSortOption(TransactionSortField.TimeStart, SortDirection.Ascending);
        TransactionSortOption option2 = new TransactionSortOption(TransactionSortField.TimeStart, SortDirection.Ascending);

        Assert.Equal(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentField_AreNotEqual()
    {
        TransactionSortOption option1 = new TransactionSortOption(TransactionSortField.TimeStart, SortDirection.Ascending);
        TransactionSortOption option2 = new TransactionSortOption(TransactionSortField.Number, SortDirection.Ascending);

        Assert.NotEqual(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentDirection_AreNotEqual()
    {
        TransactionSortOption option1 = new TransactionSortOption(TransactionSortField.TimeStart, SortDirection.Ascending);
        TransactionSortOption option2 = new TransactionSortOption(TransactionSortField.TimeStart, SortDirection.Descending);

        Assert.NotEqual(option1, option2);
    }
}
