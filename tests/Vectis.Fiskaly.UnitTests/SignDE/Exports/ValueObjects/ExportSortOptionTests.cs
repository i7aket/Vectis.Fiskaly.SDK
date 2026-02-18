using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.ValueObjects;

public class ExportSortOptionTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesInstanceWithFieldAndDirection()
    {
        ExportSortOption option = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Ascending);

        Assert.Equal(ExportSortField.TimeStart, option.Field);
        Assert.Equal(SortDirection.Ascending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithDescending_CreatesInstance()
    {
        ExportSortOption option = new ExportSortOption(ExportSortField.TimeEnd, SortDirection.Descending);

        Assert.Equal(ExportSortField.TimeEnd, option.Field);
        Assert.Equal(SortDirection.Descending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameValues_AreEqual()
    {
        ExportSortOption option1 = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Ascending);
        ExportSortOption option2 = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Ascending);

        Assert.Equal(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentField_AreNotEqual()
    {
        ExportSortOption option1 = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Ascending);
        ExportSortOption option2 = new ExportSortOption(ExportSortField.TimeEnd, SortDirection.Ascending);

        Assert.NotEqual(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentDirection_AreNotEqual()
    {
        ExportSortOption option1 = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Ascending);
        ExportSortOption option2 = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Descending);

        Assert.NotEqual(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithInvalidField_ThrowsArgumentOutOfRangeException()
    {
        ExportSortField invalidField = (ExportSortField)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExportSortOption(invalidField, SortDirection.Ascending));

        Assert.Contains("Unsupported export sort field", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithInvalidDirection_ThrowsArgumentOutOfRangeException()
    {
        SortDirection invalidDirection = (SortDirection)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExportSortOption(ExportSortField.TimeStart, invalidDirection));

        Assert.Contains("Unsupported sort direction", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void By_WithDefaultDirection_CreatesAscendingOption()
    {
        ExportSortOption option = ExportSortOption.By(ExportSortField.TimeStart);

        Assert.Equal(ExportSortField.TimeStart, option.Field);
        Assert.Equal(SortDirection.Ascending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void By_WithExplicitDirection_CreatesDescendingOption()
    {
        ExportSortOption option = ExportSortOption.By(ExportSortField.TimeEnd, SortDirection.Descending);

        Assert.Equal(ExportSortField.TimeEnd, option.Field);
        Assert.Equal(SortDirection.Descending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsFieldAndDirection()
    {
        ExportSortOption option = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Ascending);

        string result = option.ToString();

        Assert.Contains("TimeStart", result);
        Assert.Contains("Ascending", result);
    }
}
