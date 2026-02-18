using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Models;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.Models;

public class ListClientsQueryParametersTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesEmptyParameters()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters();

        Assert.Null(parameters.Sort);
        Assert.Null(parameters.SerialNumber);
        Assert.Null(parameters.State);
        Assert.Null(parameters.Limit);
        Assert.Null(parameters.Offset);
        // Per OpenAPI spec, ShowDeleted defaults to true
        Assert.True(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Sort_CanBeSet()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            Sort = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Descending)
        };

        Assert.NotNull(parameters.Sort);
        Assert.Equal(ClientSortField.TimeCreation, parameters.Sort.Value.Field);
        Assert.Equal(SortDirection.Descending, parameters.Sort.Value.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SerialNumber_CanBeSet()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            SerialNumber = ClientSerialNumber.From("ERS 1234567890")
        };

        Assert.NotNull(parameters.SerialNumber);
        Assert.Equal("ERS 1234567890", parameters.SerialNumber.Value.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void State_CanBeSet()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            State = ClientState.Registered
        };

        Assert.NotNull(parameters.State);
        Assert.Equal(ClientState.Registered, parameters.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_CanBeSet_WithinValidRange()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            Limit = 50
        };

        Assert.Equal(50, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_ThrowsArgumentOutOfRangeException_WhenLessThan1()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 0);

        Assert.Contains("Limit must be between 1 and 100", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_ThrowsArgumentOutOfRangeException_WhenGreaterThan100()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 101);

        Assert.Contains("Limit must be between 1 and 100", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_AcceptsBoundaryValues()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters();

        parameters.Limit = 1;
        Assert.Equal(1, parameters.Limit);

        parameters.Limit = 100;
        Assert.Equal(100, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_CanBeSet()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            Offset = 100
        };

        Assert.Equal(100, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_ThrowsArgumentOutOfRangeException_WhenNegative()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Offset = -1);

        Assert.Contains("Offset must be zero or positive", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_AcceptsZero()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            Offset = 0
        };

        Assert.Equal(0, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShowDeleted_CanBeSet()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            ShowDeleted = true
        };

        Assert.True(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            Sort = new ClientSortOption(ClientSortField.SerialNumber, SortDirection.Ascending),
            SerialNumber = ClientSerialNumber.From("ERS 9999999999"),
            State = ClientState.Deregistered,
            Limit = 25,
            Offset = 50,
            ShowDeleted = false
        };

        Assert.NotNull(parameters.Sort);
        Assert.NotNull(parameters.SerialNumber);
        Assert.Equal(ClientState.Deregistered, parameters.State);
        Assert.Equal(25, parameters.Limit);
        Assert.Equal(50, parameters.Offset);
        Assert.False(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithNoParametersSet_ReturnsEmptyCollection()
    {
        // Arrange
        ListClientsQueryParameters parameters = new ListClientsQueryParameters();

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToQueryParameters().ToList();

        // Assert - ShowDeleted defaults to true per OpenAPI spec
        Assert.Single(result);
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "true");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithSort_ReturnsOrderByAndOrder()
    {
        // Arrange
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            Sort = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Descending)
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "order_by" && kvp.Value == "time_creation");
        Assert.Contains(result, kvp => kvp.Key == "order" && kvp.Value == "desc");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithSerialNumber_ReturnsSerialNumberParameter()
    {
        // Arrange
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            SerialNumber = ClientSerialNumber.From("ERS 1234567890")
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "serial_number" && kvp.Value == "ERS 1234567890");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithState_ReturnsStateParameter()
    {
        // Arrange
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            State = ClientState.Registered
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "state" && kvp.Value == "REGISTERED");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithLimitAndOffset_ReturnsCorrectParameters()
    {
        // Arrange
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            Limit = 50,
            Offset = 100
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "limit" && kvp.Value == "50");
        Assert.Contains(result, kvp => kvp.Key == "offset" && kvp.Value == "100");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithShowDeletedTrue_ReturnsTrue()
    {
        // Arrange
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            ShowDeleted = true
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "true");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithShowDeletedFalse_ReturnsFalse()
    {
        // Arrange
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            ShowDeleted = false
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "false");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithAllParametersSet_ReturnsAllParameters()
    {
        // Arrange
        ListClientsQueryParameters parameters = new ListClientsQueryParameters
        {
            Sort = new ClientSortOption(ClientSortField.SerialNumber, SortDirection.Ascending),
            SerialNumber = ClientSerialNumber.From("ERS 9999999999"),
            State = ClientState.Deregistered,
            Limit = 25,
            Offset = 50,
            ShowDeleted = false
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Equal(7, result.Count); // 2 for sort + 5 other parameters
        Assert.Contains(result, kvp => kvp.Key == "order_by");
        Assert.Contains(result, kvp => kvp.Key == "order");
        Assert.Contains(result, kvp => kvp.Key == "serial_number");
        Assert.Contains(result, kvp => kvp.Key == "state");
        Assert.Contains(result, kvp => kvp.Key == "limit");
        Assert.Contains(result, kvp => kvp.Key == "offset");
        Assert.Contains(result, kvp => kvp.Key == "show_deleted");
    }
}
