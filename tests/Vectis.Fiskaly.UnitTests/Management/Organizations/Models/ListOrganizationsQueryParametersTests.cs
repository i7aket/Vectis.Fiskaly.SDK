using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Management.Common.Enums;
using Vectis.Fiskaly.SDK.Management.Organizations.Models;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;

namespace Vectis.Fiskaly.UnitTests.Management.Organizations.Models;

public class ListOrganizationsQueryParametersTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesEmptyParameters()
    {
        // Act
        ListOrganizationsQueryParameters parameters = new();

        // Assert - All properties should be null (no defaults per spec)
        Assert.Null(parameters.Limit);
        Assert.Null(parameters.Offset);
        Assert.Null(parameters.OrderBy);
        Assert.Null(parameters.Order);
        Assert.Null(parameters.Env);
        Assert.Null(parameters.Type);
        Assert.Null(parameters.ManagedByOrganizationId);
    }

    #region Limit Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_CanBeSet_WithinValidRange()
    {
        // Arrange & Act
        ListOrganizationsQueryParameters parameters = new()
        {
            Limit = 100
        };

        // Assert
        Assert.Equal(100, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_AcceptsMinimumValue()
    {
        // Arrange & Act
        ListOrganizationsQueryParameters parameters = new()
        {
            Limit = 1
        };

        // Assert
        Assert.Equal(1, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_AcceptsMaximumValue()
    {
        // Arrange & Act
        ListOrganizationsQueryParameters parameters = new()
        {
            Limit = 5000
        };

        // Assert
        Assert.Equal(5000, parameters.Limit);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_ThrowsArgumentOutOfRangeException_WhenLessThan1()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new();

        // Act & Assert
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            parameters.Limit = 0;
        });

        Assert.Contains("Limit must be between 1 and 5000", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_ThrowsArgumentOutOfRangeException_WhenGreaterThan5000()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new();

        // Act & Assert
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            parameters.Limit = 5001;
        });

        Assert.Contains("Limit must be between 1 and 5000", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Limit_CanBeSetToNull()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            Limit = 100
        };

        // Act - Reset to null
        parameters.Limit = null;

        // Assert
        Assert.Null(parameters.Limit);
    }

    #endregion

    #region Property Setter Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void OrderBy_CanBeSet()
    {
        // Act
        ListOrganizationsQueryParameters parameters = new()
        {
            OrderBy = OrganizationSortField.Name
        };

        // Assert
        Assert.Equal(OrganizationSortField.Name, parameters.OrderBy);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Order_CanBeSet()
    {
        // Act
        ListOrganizationsQueryParameters parameters = new()
        {
            Order = SortDirection.Descending
        };

        // Assert
        Assert.Equal(SortDirection.Descending, parameters.Order);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Offset_CanBeSet()
    {
        // Act
        ListOrganizationsQueryParameters parameters = new()
        {
            Offset = 50
        };

        // Assert
        Assert.Equal(50, parameters.Offset);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Env_CanBeSet()
    {
        // Act
        ListOrganizationsQueryParameters parameters = new()
        {
            Env = Env.Live
        };

        // Assert
        Assert.Equal(Env.Live, parameters.Env);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Type_CanBeSet()
    {
        // Act
        ListOrganizationsQueryParameters parameters = new()
        {
            Type = OrganizationType.ManagedOrganization
        };

        // Assert
        Assert.Equal(OrganizationType.ManagedOrganization, parameters.Type);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ManagedByOrganizationId_CanBeSet()
    {
        // Arrange
        OrganizationId orgId = OrganizationId.From("12345678-1234-4234-8234-123456789012");

        // Act
        ListOrganizationsQueryParameters parameters = new()
        {
            ManagedByOrganizationId = orgId
        };

        // Assert
        Assert.Equal(orgId, parameters.ManagedByOrganizationId);
    }

    #endregion

    #region Query Parameter Serialization Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_EmptyParameters_ReturnsEmptyList()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new();

        // Act
        IEnumerable<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters();

        // Assert
        Assert.Empty(queryParams);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithLimit_ReturnsLimitParameter()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            Limit = 50
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("limit", queryParams[0].Key);
        Assert.Equal("50", queryParams[0].Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithOffset_ReturnsOffsetParameter()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            Offset = 100
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("offset", queryParams[0].Key);
        Assert.Equal("100", queryParams[0].Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithOrderBy_ReturnsOrderByParameter()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            OrderBy = OrganizationSortField.Zip
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("order_by", queryParams[0].Key);
        Assert.Equal("zip", queryParams[0].Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithOrder_ReturnsOrderParameter()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            Order = SortDirection.Ascending
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("order", queryParams[0].Key);
        Assert.Equal("asc", queryParams[0].Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithEnv_ReturnsEnvParameter()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            Env = Env.Test
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("env", queryParams[0].Key);
        Assert.Equal("TEST", queryParams[0].Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithType_ReturnsTypeParameter()
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            Type = OrganizationType.Organization
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("type", queryParams[0].Key);
        Assert.Equal("ORGANIZATION", queryParams[0].Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithManagedByOrganizationId_ReturnsManagedByParameter()
    {
        // Arrange
        OrganizationId orgId = OrganizationId.From("12345678-1234-4234-8234-123456789012");
        ListOrganizationsQueryParameters parameters = new()
        {
            ManagedByOrganizationId = orgId
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("managed_by_organization_id", queryParams[0].Key);
        Assert.Equal("12345678-1234-4234-8234-123456789012", queryParams[0].Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithAllParameters_ReturnsAllQueryParameters()
    {
        // Arrange
        OrganizationId orgId = OrganizationId.From("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        ListOrganizationsQueryParameters parameters = new()
        {
            Limit = 25,
            Offset = 50,
            OrderBy = OrganizationSortField.Name,
            Order = SortDirection.Descending,
            Env = Env.Live,
            Type = OrganizationType.ManagedOrganization,
            ManagedByOrganizationId = orgId
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Equal(7, queryParams.Count);
        Assert.Contains(queryParams, p => p.Key == "order_by" && p.Value == "name");
        Assert.Contains(queryParams, p => p.Key == "order" && p.Value == "desc");
        Assert.Contains(queryParams, p => p.Key == "limit" && p.Value == "25");
        Assert.Contains(queryParams, p => p.Key == "offset" && p.Value == "50");
        Assert.Contains(queryParams, p => p.Key == "env" && p.Value == "LIVE");
        Assert.Contains(queryParams, p => p.Key == "type" && p.Value == "MANAGED_ORGANIZATION");
        Assert.Contains(queryParams, p => p.Key == "managed_by_organization_id" && p.Value == "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(OrganizationSortField.Name, "name")]
    [InlineData(OrganizationSortField.Id, "id")]
    [InlineData(OrganizationSortField.AddressLine1, "address_line1")]
    [InlineData(OrganizationSortField.Zip, "zip")]
    [InlineData(OrganizationSortField.Town, "town")]
    public void ToQueryParameters_OrderByField_ProducesCorrectApiValue(OrganizationSortField field, string expectedValue)
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            OrderBy = field
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("order_by", queryParams[0].Key);
        Assert.Equal(expectedValue, queryParams[0].Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(SortDirection.Ascending, "asc")]
    [InlineData(SortDirection.Descending, "desc")]
    public void ToQueryParameters_OrderDirection_ProducesCorrectApiValue(SortDirection direction, string expectedValue)
    {
        // Arrange
        ListOrganizationsQueryParameters parameters = new()
        {
            Order = direction
        };

        // Act
        List<KeyValuePair<string, string?>> queryParams = parameters.ToQueryParameters().ToList();

        // Assert
        Assert.Single(queryParams);
        Assert.Equal("order", queryParams[0].Key);
        Assert.Equal(expectedValue, queryParams[0].Value);
    }

    #endregion
}
