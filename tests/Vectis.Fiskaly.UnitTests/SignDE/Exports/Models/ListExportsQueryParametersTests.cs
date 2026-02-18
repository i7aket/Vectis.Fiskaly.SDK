using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Models;

public class ListExportsQueryParametersTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesEmptyParameters()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters();

        Assert.Null(parameters.Sort);
        Assert.Null(parameters.States);
        Assert.Null(parameters.Limit);
        Assert.Null(parameters.Offset);
        Assert.Null(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Sort_CanBeSet()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Sort = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Descending)
        };

        Assert.NotNull(parameters.Sort);
        Assert.Equal(ExportSortField.TimeStart, parameters.Sort.Value.Field);
        Assert.Equal(SortDirection.Descending, parameters.Sort.Value.Direction);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void States_CanBeSet_WithSingleState()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            States = new[] { ExportState.Completed }
        };

        Assert.NotNull(parameters.States);
        Assert.Single(parameters.States);
        Assert.Contains(ExportState.Completed, parameters.States);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void States_CanBeSet_WithMultipleStates()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            States = new[] { ExportState.Pending, ExportState.Working, ExportState.Completed }
        };

        Assert.NotNull(parameters.States);
        Assert.Equal(3, parameters.States.Count);
        Assert.Contains(ExportState.Pending, parameters.States);
        Assert.Contains(ExportState.Working, parameters.States);
        Assert.Contains(ExportState.Completed, parameters.States);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_CanBeSet_WithinValidRange()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Limit = 50
        };

        Assert.Equal(50, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_ThrowsArgumentOutOfRangeException_WhenLessThan1()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 0);

        Assert.Contains("Limit must be between 1 and 100", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_ThrowsArgumentOutOfRangeException_WhenGreaterThan100()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Limit = 101);

        Assert.Contains("Limit must be between 1 and 100", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_AcceptsBoundaryValues()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters();

        parameters.Limit = 1;
        Assert.Equal(1, parameters.Limit);

        parameters.Limit = 100;
        Assert.Equal(100, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_CanBeSet()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Offset = 150
        };

        Assert.Equal(150, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_ThrowsArgumentOutOfRangeException_WhenNegative()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Offset = -1);

        Assert.Contains("Offset must be zero or positive", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_AcceptsZero()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Offset = 0
        };

        Assert.Equal(0, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShowDeleted_CanBeSet()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            ShowDeleted = true
        };

        Assert.True(parameters.ShowDeleted);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Sort = new ExportSortOption(ExportSortField.TimeEnd, SortDirection.Ascending),
            States = new[] { ExportState.Completed, ExportState.Error },
            Limit = 75,
            Offset = 150,
            ShowDeleted = false
        };

        Assert.NotNull(parameters.Sort);
        Assert.NotNull(parameters.States);
        Assert.Equal(2, parameters.States.Count);
        Assert.Equal(75, parameters.Limit);
        Assert.Equal(150, parameters.Offset);
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
        ListExportsQueryParameters parameters = new ListExportsQueryParameters();

        // Act
        IEnumerable<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs();

        // Assert
        Assert.Empty(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithSort_AddsOrderByAndOrder()
    {
        // Arrange
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Sort = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Descending)
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, kvp => kvp.Key == "order_by" && kvp.Value == "time_start");
        Assert.Contains(result, kvp => kvp.Key == "order" && kvp.Value == "desc");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithStates_AddsIndexedStateParameters()
    {
        // Arrange
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            States = new[] { ExportState.Pending, ExportState.Working, ExportState.Completed }
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, kvp => kvp.Key == "states[0]" && kvp.Value == "PENDING");
        Assert.Contains(result, kvp => kvp.Key == "states[1]" && kvp.Value == "WORKING");
        Assert.Contains(result, kvp => kvp.Key == "states[2]" && kvp.Value == "COMPLETED");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithLimit_AddsLimitParameter()
    {
        // Arrange
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Limit = 50
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, kvp => kvp.Key == "limit" && kvp.Value == "50");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithOffset_AddsOffsetParameter()
    {
        // Arrange
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Offset = 150
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert
        Assert.Single(result);
        Assert.Contains(result, kvp => kvp.Key == "offset" && kvp.Value == "150");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToKeyValuePairs_WithShowDeleted_AddsShowDeletedParameter()
    {
        // Arrange
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
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
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Sort = new ExportSortOption(ExportSortField.TimeEnd, SortDirection.Ascending),
            States = new[] { ExportState.Completed, ExportState.Error },
            Limit = 75,
            Offset = 150,
            ShowDeleted = false
        };

        // Act
        List<KeyValuePair<string, string?>> result = parameters.ToKeyValuePairs().ToList();

        // Assert
        Assert.Equal(7, result.Count); // 2 (sort) + 2 (states) + 1 (limit) + 1 (offset) + 1 (show_deleted)
        Assert.Contains(result, kvp => kvp.Key == "order_by" && kvp.Value == "time_end");
        Assert.Contains(result, kvp => kvp.Key == "order" && kvp.Value == "asc");
        Assert.Contains(result, kvp => kvp.Key == "states[0]" && kvp.Value == "COMPLETED");
        Assert.Contains(result, kvp => kvp.Key == "states[1]" && kvp.Value == "ERROR");
        Assert.Contains(result, kvp => kvp.Key == "limit" && kvp.Value == "75");
        Assert.Contains(result, kvp => kvp.Key == "offset" && kvp.Value == "150");
        Assert.Contains(result, kvp => kvp.Key == "show_deleted" && kvp.Value == "false");
    }

    // ============================================================================
    // BuildUrl Tests (Internal Method Coverage)
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithEmptyParameters_ReturnsBasePath()
    {
        // Arrange
        ListExportsQueryParameters parameters = new ListExportsQueryParameters();
        string basePath = "export";

        // Act
        string result = parameters.BuildUrl(basePath);

        // Assert
        Assert.Equal("export", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithSingleParameter_ReturnsCorrectQueryString()
    {
        // Arrange
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Limit = 25
        };
        string basePath = "export";

        // Act
        string result = parameters.BuildUrl(basePath);

        // Assert
        Assert.Equal("export?limit=25", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithAllParameters_ReturnsCorrectQueryString()
    {
        // Arrange
        ListExportsQueryParameters parameters = new ListExportsQueryParameters
        {
            Sort = new ExportSortOption(ExportSortField.TimeStart, SortDirection.Descending),
            States = new[] { ExportState.Completed },
            Limit = 10,
            Offset = 5,
            ShowDeleted = true
        };
        string basePath = "export";

        // Act
        string result = parameters.BuildUrl(basePath);

        // Assert
        Assert.StartsWith("export?", result);
        Assert.Contains("order_by=time_start", result);
        Assert.Contains("order=desc", result);
        Assert.Contains("states%5B0%5D=COMPLETED", result); // URL encoded states[0]
        Assert.Contains("limit=10", result);
        Assert.Contains("offset=5", result);
        Assert.Contains("show_deleted=true", result);
    }
}
