using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Models;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Tss.Models;

public class ListTssQueryParametersTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesEmptyParameters()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters();

        Assert.Null(parameters.Sort);
        Assert.Null(parameters.States);
        Assert.Null(parameters.Limit);
        Assert.Null(parameters.Offset);
        // Per OpenAPI spec, ShowDeleted defaults to true
        Assert.True(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Sort_CanBeSet()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Sort = new TssSortOption(TssSortField.TimeCreation, SortDirection.Descending)
        };

        Assert.NotNull(parameters.Sort);
        Assert.Equal(TssSortField.TimeCreation, parameters.Sort.Value.Field);
        Assert.Equal(SortDirection.Descending, parameters.Sort.Value.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void States_CanBeSet_WithSingleState()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            States = new[] { TssState.Initialized }
        };

        Assert.NotNull(parameters.States);
        Assert.Single(parameters.States);
        Assert.Contains(TssState.Initialized, parameters.States);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void States_CanBeSet_WithMultipleStates()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            States = new[] { TssState.Created, TssState.Initialized, TssState.Disabled }
        };

        Assert.NotNull(parameters.States);
        Assert.Equal(3, parameters.States.Count);
        Assert.Contains(TssState.Created, parameters.States);
        Assert.Contains(TssState.Initialized, parameters.States);
        Assert.Contains(TssState.Disabled, parameters.States);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_CanBeSet_WithinValidRange()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Limit = 75
        };

        Assert.Equal(75, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_ThrowsArgumentOutOfRangeException_WhenLessThan1()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 0);

        Assert.Contains("Limit must be between 1 and 100", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_ThrowsArgumentOutOfRangeException_WhenGreaterThan100()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 101);

        Assert.Contains("Limit must be between 1 and 100", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_AcceptsBoundaryValues()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters();

        parameters.Limit = 1;
        Assert.Equal(1, parameters.Limit);

        parameters.Limit = 100;
        Assert.Equal(100, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_CanBeSet()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Offset = 200
        };

        Assert.Equal(200, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_ThrowsArgumentOutOfRangeException_WhenNegative()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Offset = -1);

        Assert.Contains("Offset must be zero or positive", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_AcceptsZero()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Offset = 0
        };

        Assert.Equal(0, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShowDeleted_CanBeSet()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            ShowDeleted = true
        };

        Assert.True(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Sort = new TssSortOption(TssSortField.State, SortDirection.Ascending),
            States = new[] { TssState.Initialized, TssState.Disabled },
            Limit = 50,
            Offset = 100,
            ShowDeleted = false
        };

        Assert.NotNull(parameters.Sort);
        Assert.NotNull(parameters.States);
        Assert.Equal(2, parameters.States.Count);
        Assert.Equal(50, parameters.Limit);
        Assert.Equal(100, parameters.Offset);
        Assert.False(parameters.ShowDeleted);
    }

    // ============================================================================
    // ToKeyValuePairs Tests (Internal Method Coverage)
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithEmptyParameters_ReturnsEmptyCollection()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters();

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert - ShowDeleted defaults to true per OpenAPI spec
        Assert.Single(result);
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "true");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithSort_AddsOrderByAndOrder()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Sort = new TssSortOption(TssSortField.TimeCreation, SortDirection.Descending)
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert - 3 items: order_by, order, and ShowDeleted default
        Assert.Equal(3, result.Count);
        Assert.Contains(result, kvp => kvp.Key == "order_by" && kvp.Value == "time_creation");
        Assert.Contains(result, kvp => kvp.Key == "order" && kvp.Value == "desc");
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "true");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithStates_AddsIndexedStateParameters()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            States = new[] { TssState.Created, TssState.Initialized, TssState.Disabled }
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert - 4 items: states[0-2] and ShowDeleted default
        Assert.Equal(4, result.Count);
        Assert.Contains(result, kvp => kvp.Key == "states[0]" && kvp.Value == "CREATED");
        Assert.Contains(result, kvp => kvp.Key == "states[1]" && kvp.Value == "INITIALIZED");
        Assert.Contains(result, kvp => kvp.Key == "states[2]" && kvp.Value == "DISABLED");
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "true");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithLimit_AddsLimitParameter()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Limit = 75
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert - 2 items: limit and ShowDeleted default
        Assert.Equal(2, result.Count);
        Assert.Contains(result, kvp => kvp.Key == "limit" && kvp.Value == "75");
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "true");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithOffset_AddsOffsetParameter()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Offset = 200
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert - 2 items: offset and ShowDeleted default
        Assert.Equal(2, result.Count);
        Assert.Contains(result, kvp => kvp.Key == "offset" && kvp.Value == "200");
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "true");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithShowDeleted_AddsShowDeletedParameter()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            ShowDeleted = true
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "true");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithAllParameters_ReturnsAllKeyValuePairs()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Sort = new TssSortOption(TssSortField.State, SortDirection.Ascending),
            States = new[] { TssState.Initialized, TssState.Disabled },
            Limit = 50,
            Offset = 100,
            ShowDeleted = false
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert
        Assert.Equal(7, result.Count); // 2 (sort) + 2 (states) + 1 (limit) + 1 (offset) + 1 (show_deleted)
        Assert.Contains(result, kvp => kvp.Key == "order_by" && kvp.Value == "state");
        Assert.Contains(result, kvp => kvp.Key == "order" && kvp.Value == "asc");
        Assert.Contains(result, kvp => kvp.Key == "states[0]" && kvp.Value == "INITIALIZED");
        Assert.Contains(result, kvp => kvp.Key == "states[1]" && kvp.Value == "DISABLED");
        Assert.Contains(result, kvp => kvp.Key == "limit" && kvp.Value == "50");
        Assert.Contains(result, kvp => kvp.Key == "offset" && kvp.Value == "100");
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "false");
    }

    // ============================================================================
    // BuildUrl Tests (Extension Method Coverage)
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithEmptyParameters_ReturnsTss()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters();

        // Act
        string result = parameters.BuildUrl("tss");

        // Assert - ShowDeleted defaults to true per OpenAPI spec
        Assert.Equal("tss?show_deleted=true", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithSingleParameter_ReturnsCorrectQueryString()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Limit = 25
        };

        // Act
        string result = parameters.BuildUrl("tss");

        // Assert - Includes ShowDeleted default value
        Assert.Equal("tss?limit=25&show_deleted=true", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithAllParameters_ReturnsCorrectQueryString()
    {
        // Arrange
        ListTssQueryParameters parameters = new ListTssQueryParameters
        {
            Sort = new TssSortOption(TssSortField.TimeCreation, SortDirection.Descending),
            States = new[] { TssState.Initialized },
            Limit = 10,
            Offset = 5,
            ShowDeleted = true
        };

        // Act
        string result = parameters.BuildUrl("tss");

        // Assert
        Assert.StartsWith("tss?", result);
        Assert.Contains("order_by=time_creation", result);
        Assert.Contains("order=desc", result);
        Assert.Contains("states%5B0%5D=INITIALIZED", result); // URL encoded states[0]
        Assert.Contains("limit=10", result);
        Assert.Contains("offset=5", result);
        Assert.Contains("show_deleted=true", result);
    }
}
