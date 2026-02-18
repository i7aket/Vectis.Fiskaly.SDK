using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Tss.ValueObjects;

public class TssSortOptionTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesInstanceWithFieldAndDirection()
    {
        TssSortOption option = new TssSortOption(TssSortField.TimeCreation, SortDirection.Ascending);

        Assert.Equal(TssSortField.TimeCreation, option.Field);
        Assert.Equal(SortDirection.Ascending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithDescending_CreatesInstance()
    {
        TssSortOption option = new TssSortOption(TssSortField.State, SortDirection.Descending);

        Assert.Equal(TssSortField.State, option.Field);
        Assert.Equal(SortDirection.Descending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameValues_AreEqual()
    {
        TssSortOption option1 = new TssSortOption(TssSortField.TimeCreation, SortDirection.Ascending);
        TssSortOption option2 = new TssSortOption(TssSortField.TimeCreation, SortDirection.Ascending);

        Assert.Equal(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentField_AreNotEqual()
    {
        TssSortOption option1 = new TssSortOption(TssSortField.TimeCreation, SortDirection.Ascending);
        TssSortOption option2 = new TssSortOption(TssSortField.State, SortDirection.Ascending);

        Assert.NotEqual(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentDirection_AreNotEqual()
    {
        TssSortOption option1 = new TssSortOption(TssSortField.TimeCreation, SortDirection.Ascending);
        TssSortOption option2 = new TssSortOption(TssSortField.TimeCreation, SortDirection.Descending);

        Assert.NotEqual(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithInvalidField_ThrowsArgumentOutOfRangeException()
    {
        TssSortField invalidField = (TssSortField)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TssSortOption(invalidField, SortDirection.Ascending));

        Assert.Contains("Unsupported TSS sort field", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithInvalidDirection_ThrowsArgumentOutOfRangeException()
    {
        SortDirection invalidDirection = (SortDirection)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TssSortOption(TssSortField.TimeCreation, invalidDirection));

        Assert.Contains("Unsupported sort direction", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void By_WithDefaultDirection_CreatesAscendingOption()
    {
        TssSortOption option = TssSortOption.By(TssSortField.TimeCreation);

        Assert.Equal(TssSortField.TimeCreation, option.Field);
        Assert.Equal(SortDirection.Ascending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void By_WithExplicitDirection_CreatesDescendingOption()
    {
        TssSortOption option = TssSortOption.By(TssSortField.State, SortDirection.Descending);

        Assert.Equal(TssSortField.State, option.Field);
        Assert.Equal(SortDirection.Descending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsFieldAndDirection()
    {
        TssSortOption option = new TssSortOption(TssSortField.TimeCreation, SortDirection.Ascending);

        string result = option.ToString();

        Assert.Contains("TimeCreation", result);
        Assert.Contains("Ascending", result);
    }
}
