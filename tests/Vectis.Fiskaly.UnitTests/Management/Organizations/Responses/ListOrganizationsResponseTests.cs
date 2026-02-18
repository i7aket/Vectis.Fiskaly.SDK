using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Management.Common.Enums;
using Vectis.Fiskaly.SDK.Management.Organizations.Responses;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;

namespace Vectis.Fiskaly.UnitTests.Management.Organizations.Responses;

public class ListOrganizationsResponseTests
{
    private readonly JsonSerializerOptions _options;

    public ListOrganizationsResponseTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters =
            {
                new UnixEpochDateTimeOffsetConverterFactory(),
                new JsonStringEnumConverter()
            }
        };
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_EmptyList_ReturnsZeroCount()
    {
        // Arrange - Empty organization list
        string json = """
                      {
                          "_type": "ORGANIZATION_LIST",
                          "_envs": ["TEST"],
                          "_version": "0.12.0",
                          "data": [],
                          "count": 0
                      }
                      """;

        // Act
        ListOrganizationsResponse? response = JsonSerializer.Deserialize<ListOrganizationsResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("ORGANIZATION_LIST", response.Type);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
        Assert.Equal(0, response.Count);
        Assert.NotNull(response.Envs);
        Assert.Single(response.Envs!);
        Assert.Contains(Env.Test, response.Envs);
        Assert.Equal("0.12.0", response.Version);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_SingleOrganization_ReturnsOneItem()
    {
        // Arrange - List with one organization
        string json = """
                      {
                          "_type": "ORGANIZATION_LIST",
                          "_envs": ["TEST", "LIVE"],
                          "_version": "0.12.0",
                          "data": [
                              {
                                  "_id": "12345678-1234-4234-8234-123456789012",
                                  "_type": "ORGANIZATION",
                                  "_envs": ["TEST"],
                                  "name": "Test Company",
                                  "address_line1": "Main Street 1",
                                  "zip": "12345",
                                  "town": "Berlin",
                                  "country_code": "DEU"
                              }
                          ],
                          "count": 1
                      }
                      """;

        // Act
        ListOrganizationsResponse? response = JsonSerializer.Deserialize<ListOrganizationsResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("ORGANIZATION_LIST", response.Type);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal(1, response.Count);

        // Verify first organization
        OrganizationResponse org = response.Data[0];
        Assert.NotNull(org.Id);
        Assert.Equal("Test Company", org.Name);
        Assert.Equal(OrganizationType.Organization, org.Type);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MultipleOrganizations_ReturnsAllItems()
    {
        // Arrange - List with multiple organizations
        string json = """
                      {
                          "_type": "ORGANIZATION_LIST",
                          "_envs": ["TEST", "LIVE"],
                          "_version": "0.12.0",
                          "data": [
                              {
                                  "_id": "11111111-1111-4111-8111-111111111111",
                                  "_type": "ORGANIZATION",
                                  "_envs": ["TEST"],
                                  "name": "First Company",
                                  "address_line1": "Street 1",
                                  "zip": "11111",
                                  "town": "Munich",
                                  "country_code": "DEU"
                              },
                              {
                                  "_id": "22222222-2222-4222-8222-222222222222",
                                  "_type": "ORGANIZATION",
                                  "_envs": ["LIVE"],
                                  "name": "Second Company",
                                  "address_line1": "Street 2",
                                  "zip": "22222",
                                  "town": "Hamburg",
                                  "country_code": "DEU"
                              },
                              {
                                  "_id": "33333333-3333-4333-8333-333333333333",
                                  "_type": "MANAGED_ORGANIZATION",
                                  "_envs": ["TEST"],
                                  "name": "Third Company (Managed)",
                                  "address_line1": "Street 3",
                                  "zip": "33333",
                                  "town": "Frankfurt",
                                  "country_code": "DEU",
                                  "managed_by_organization_id": "11111111-1111-4111-8111-111111111111"
                              }
                          ],
                          "count": 3
                      }
                      """;

        // Act
        ListOrganizationsResponse? response = JsonSerializer.Deserialize<ListOrganizationsResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("ORGANIZATION_LIST", response.Type);
        Assert.NotNull(response.Data);
        Assert.Equal(3, response.Data.Count);
        Assert.Equal(3, response.Count);

        // Verify organizations
        Assert.Equal("First Company", response.Data[0].Name);
        Assert.Equal(OrganizationType.Organization, response.Data[0].Type);

        Assert.Equal("Second Company", response.Data[1].Name);
        Assert.Equal(OrganizationType.Organization, response.Data[1].Type);

        Assert.Equal("Third Company (Managed)", response.Data[2].Name);
        Assert.Equal(OrganizationType.ManagedOrganization, response.Data[2].Type);
        Assert.NotNull(response.Data[2].ManagedByOrganizationId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_PaginatedResponse_CountHigherThanDataLength()
    {
        // Arrange - Paginated response where count > data.length
        // Simulates scenario where total count is 100 but only 10 items returned (limit=10)
        string json = """
                      {
                          "_type": "ORGANIZATION_LIST",
                          "_envs": ["TEST"],
                          "_version": "0.12.0",
                          "data": [
                              {
                                  "_id": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
                                  "_type": "ORGANIZATION",
                                  "_envs": ["TEST"],
                                  "name": "Org 1",
                                  "address_line1": "Street 1",
                                  "zip": "11111",
                                  "town": "City",
                                  "country_code": "DEU"
                              }
                          ],
                          "count": 100
                      }
                      """;

        // Act
        ListOrganizationsResponse? response = JsonSerializer.Deserialize<ListOrganizationsResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);  // Only 1 item in this page
        Assert.Equal(100, response.Count);  // But total count is 100
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MultipleEnvironments_ParsesCorrectly()
    {
        // Arrange - Response with both TEST and LIVE environments
        string json = """
                      {
                          "_type": "ORGANIZATION_LIST",
                          "_envs": ["TEST", "LIVE"],
                          "_version": "0.12.0",
                          "data": [],
                          "count": 0
                      }
                      """;

        // Act
        ListOrganizationsResponse? response = JsonSerializer.Deserialize<ListOrganizationsResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Envs);
        Assert.Equal(2, response.Envs.Count);
        Assert.Contains(Env.Test, response.Envs);
        Assert.Contains(Env.Live, response.Envs);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MissingOptionalFields_ReturnsNullForOptionalProperties()
    {
        // Arrange - Minimal response without optional _version field
        string json = """
                      {
                          "_type": "ORGANIZATION_LIST",
                          "data": [],
                          "count": 0
                      }
                      """;

        // Act
        ListOrganizationsResponse? response = JsonSerializer.Deserialize<ListOrganizationsResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("ORGANIZATION_LIST", response.Type);
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
        Assert.Equal(0, response.Count);
        // Optional fields can be null
        Assert.Null(response.Version);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PropertySetters_CanSetAllProperties()
    {
        // Arrange
        List<OrganizationResponse> data = new()
        {
            new OrganizationResponse
            {
                Id = Vectis.Fiskaly.SDK.Authentication.ValueObjects.OrganizationId.From("12345678-1234-4234-8234-123456789012"),
                Name = "Test Org",
                Type = OrganizationType.Organization,
                Envs = new List<Env> { Env.Test },
                AddressLine1 = "Street 1",
                Zip = "12345",
                Town = "City",
                CountryCode = Vectis.Fiskaly.SDK.Management.Common.Enums.CountryCode.DEU
            }
        };

        List<Env> envs = new() { Env.Test, Env.Live };

        // Act
        ListOrganizationsResponse response = new()
        {
            Type = "ORGANIZATION_LIST",
            Envs = envs,
            Version = "0.12.0",
            Data = data,
            Count = 1
        };

        // Assert
        Assert.Equal("ORGANIZATION_LIST", response.Type);
        Assert.Equal(envs, response.Envs);
        Assert.Equal("0.12.0", response.Version);
        Assert.Equal(data, response.Data);
        Assert.Equal(1, response.Count);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ExpectedResourceType_HasCorrectValue()
    {
        // Assert - Verify the const value matches expected API value
        Assert.Equal("ORGANIZATION_LIST", ListOrganizationsResponse.ExpectedResourceType);
    }
}
