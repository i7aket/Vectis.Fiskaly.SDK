using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.ValueObjects;

public class ClientSortOptionTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesInstanceWithFieldAndDirection()
    {
        ClientSortOption option = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Ascending);

        Assert.Equal(ClientSortField.TimeCreation, option.Field);
        Assert.Equal(SortDirection.Ascending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithDescending_CreatesInstance()
    {
        ClientSortOption option = new ClientSortOption(ClientSortField.SerialNumber, SortDirection.Descending);

        Assert.Equal(ClientSortField.SerialNumber, option.Field);
        Assert.Equal(SortDirection.Descending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameValues_AreEqual()
    {
        ClientSortOption option1 = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Ascending);
        ClientSortOption option2 = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Ascending);

        Assert.Equal(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentField_AreNotEqual()
    {
        ClientSortOption option1 = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Ascending);
        ClientSortOption option2 = new ClientSortOption(ClientSortField.SerialNumber, SortDirection.Ascending);

        Assert.NotEqual(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentDirection_AreNotEqual()
    {
        ClientSortOption option1 = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Ascending);
        ClientSortOption option2 = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Descending);

        Assert.NotEqual(option1, option2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithInvalidField_ThrowsArgumentOutOfRangeException()
    {
        ClientSortField invalidField = (ClientSortField)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ClientSortOption(invalidField, SortDirection.Ascending));

        Assert.Contains("Unsupported client sort field", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithInvalidDirection_ThrowsArgumentOutOfRangeException()
    {
        SortDirection invalidDirection = (SortDirection)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ClientSortOption(ClientSortField.TimeCreation, invalidDirection));

        Assert.Contains("Unsupported sort direction", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void By_WithDefaultDirection_CreatesAscendingOption()
    {
        ClientSortOption option = ClientSortOption.By(ClientSortField.TimeCreation);

        Assert.Equal(ClientSortField.TimeCreation, option.Field);
        Assert.Equal(SortDirection.Ascending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void By_WithExplicitDirection_CreatesDescendingOption()
    {
        ClientSortOption option = ClientSortOption.By(ClientSortField.SerialNumber, SortDirection.Descending);

        Assert.Equal(ClientSortField.SerialNumber, option.Field);
        Assert.Equal(SortDirection.Descending, option.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsFieldAndDirection()
    {
        ClientSortOption option = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Ascending);

        string result = option.ToString();

        Assert.Contains("TimeCreation", result);
        Assert.Contains("Ascending", result);
    }
}
