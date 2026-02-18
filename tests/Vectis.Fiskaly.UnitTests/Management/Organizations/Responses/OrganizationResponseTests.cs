using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Management.Common.Enums;
using Vectis.Fiskaly.SDK.Management.Organizations.Models;
using Vectis.Fiskaly.SDK.Management.Organizations.Responses;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;

namespace Vectis.Fiskaly.UnitTests.Management.Organizations.Responses;

public class OrganizationResponseTests
{
    private readonly JsonSerializerOptions _options;

    public OrganizationResponseTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters =
            {
                new UnixEpochDateTimeOffsetConverterFactory(),
                new JsonStringEnumConverter(),
                new MetadataCollectionJsonConverter()
            }
        };
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_RequiredFieldsOnly_ReturnsPopulatedObject()
    {
        // Arrange - Minimal organization with only required fields
        string json = """
                      {
                          "_id": "12345678-1234-4234-8234-123456789012",
                          "_type": "ORGANIZATION",
                          "_envs": ["TEST", "LIVE"],
                          "name": "Test Company GmbH",
                          "address_line1": "Teststrasse 123",
                          "zip": "12345",
                          "town": "Berlin",
                          "country_code": "DEU"
                      }
                      """;

        // Act
        OrganizationResponse? response = JsonSerializer.Deserialize<OrganizationResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        Assert.Equal("12345678-1234-4234-8234-123456789012", response.Id.Value.ToString());
        Assert.Equal(OrganizationType.Organization, response.Type);
        Assert.NotNull(response.Envs);
        Assert.Equal(2, response.Envs.Count);
        Assert.Contains(Env.Test, response.Envs);
        Assert.Contains(Env.Live, response.Envs);
        Assert.Equal("Test Company GmbH", response.Name);
        Assert.Equal("Teststrasse 123", response.AddressLine1);
        Assert.Equal("12345", response.Zip);
        Assert.Equal("Berlin", response.Town);
        Assert.Equal(CountryCode.DEU, response.CountryCode);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_AllFields_ReturnsCompleteObject()
    {
        // Arrange - Complete organization with all optional fields
        string json = """
                      {
                          "_id": "87654321-4321-4321-8321-210987654321",
                          "_type": "ORGANIZATION",
                          "_envs": ["LIVE"],
                          "name": "Complete Test Org",
                          "display_name": "Complete Test Organization",
                          "address_line1": "Main Street 1",
                          "address_line2": "Building A, Floor 3",
                          "zip": "10115",
                          "town": "Munich",
                          "state": "Bavaria",
                          "country_code": "DEU",
                          "vat_id": "DE123456789",
                          "tax_number": "12/345/67890",
                          "economy_id": "DE1234567890",
                          "contact_person_id": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
                          "billing_address_id": "bbbbbbbb-cccc-4ddd-8eee-ffffffffffff",
                          "billing_options": {
                              "gln": "9012345000004",
                              "bill_to_organization": "cccccccc-dddd-4eee-8fff-000000000000",
                              "withhold_billing": true
                          },
                          "metadata": {
                              "industry": "retail",
                              "customer_id": "CUST-12345"
                          },
                          "created_by_user": "dddddddd-eeee-4fff-8000-111111111111"
                      }
                      """;

        // Act
        OrganizationResponse? response = JsonSerializer.Deserialize<OrganizationResponse>(json, _options);

        // Assert - Required fields
        Assert.NotNull(response);
        Assert.NotNull(response.Id);
        Assert.Equal(OrganizationType.Organization, response.Type);
        Assert.NotNull(response.Envs);
        Assert.Single(response.Envs!);
        Assert.Contains(Env.Live, response.Envs);

        // Assert - Optional fields
        Assert.Equal("Complete Test Organization", response.DisplayName);
        Assert.Equal("Building A, Floor 3", response.AddressLine2);
        Assert.Equal("Bavaria", response.State);
        Assert.Equal("DE123456789", response.VatId);
        Assert.Equal("12/345/67890", response.TaxNumber);
        Assert.Equal("DE1234567890", response.EconomyId);
        Assert.NotNull(response.ContactPersonId);
        Assert.NotNull(response.BillingAddressId);

        // Assert - Billing options
        Assert.NotNull(response.BillingOptions);
        Assert.Equal("9012345000004", response.BillingOptions.Gln);
        Assert.NotNull(response.BillingOptions.BillToOrganization);
        Assert.True(response.BillingOptions.WithholdBilling);

        // Assert - Metadata
        Assert.NotNull(response.Metadata);
        Assert.Equal(2, response.Metadata.Count);
        Assert.Equal("retail", response.Metadata["industry"]);
        Assert.Equal("CUST-12345", response.Metadata["customer_id"]);

        // Assert - Created by user
        Assert.NotNull(response.CreatedByUser);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ManagedOrganization_HasCorrectType()
    {
        // Arrange - Managed organization
        string json = """
                      {
                          "_id": "11111111-2222-4333-8444-555555555555",
                          "_type": "MANAGED_ORGANIZATION",
                          "_envs": ["TEST"],
                          "name": "Managed Sub-Org",
                          "address_line1": "Sub Street 1",
                          "zip": "54321",
                          "town": "Hamburg",
                          "country_code": "DEU",
                          "managed_by_organization_id": "99999999-8888-4777-8666-555555555555"
                      }
                      """;

        // Act
        OrganizationResponse? response = JsonSerializer.Deserialize<OrganizationResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(OrganizationType.ManagedOrganization, response.Type);
        Assert.NotNull(response.ManagedByOrganizationId);
        Assert.Equal("99999999-8888-4777-8666-555555555555", response.ManagedByOrganizationId.Value.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MultipleEnvironments_ParsesCorrectly()
    {
        // Arrange - Organization with both TEST and LIVE environments
        string json = """
                      {
                          "_id": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
                          "_type": "ORGANIZATION",
                          "_envs": ["TEST", "LIVE"],
                          "name": "Multi-Env Org",
                          "address_line1": "Env Street 1",
                          "zip": "11111",
                          "town": "Frankfurt",
                          "country_code": "DEU"
                      }
                      """;

        // Act
        OrganizationResponse? response = JsonSerializer.Deserialize<OrganizationResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Envs);
        Assert.Equal(2, response.Envs.Count);
        Assert.Contains(Env.Test, response.Envs);
        Assert.Contains(Env.Live, response.Envs);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_DifferentCountryCodes_ParsesCorrectly()
    {
        // Arrange - Test various country codes
        (string Code, CountryCode Expected, string Name)[] testCases = new[]
        {
            (Code: "DEU", Expected: CountryCode.DEU, Name: "Germany"),
            (Code: "AUT", Expected: CountryCode.AUT, Name: "Austria"),
            (Code: "CHE", Expected: CountryCode.CHE, Name: "Switzerland"),
            (Code: "USA", Expected: CountryCode.USA, Name: "United States")
        };

        foreach ((string Code, CountryCode Expected, string Name) testCase in testCases)
        {
            string json = $$"""
                          {
                              "_id": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
                              "_type": "ORGANIZATION",
                              "_envs": ["TEST"],
                              "name": "Test Org",
                              "address_line1": "Street 1",
                              "zip": "12345",
                              "town": "City",
                              "country_code": "{{testCase.Code}}"
                          }
                          """;

            // Act
            OrganizationResponse? response = JsonSerializer.Deserialize<OrganizationResponse>(json, _options);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(testCase.Expected, response.CountryCode);
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_EmptyMetadata_ReturnsEmptyCollection()
    {
        // Arrange - Organization with empty metadata object
        string json = """
                      {
                          "_id": "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
                          "_type": "ORGANIZATION",
                          "_envs": ["TEST"],
                          "name": "No Metadata Org",
                          "address_line1": "Street 1",
                          "zip": "12345",
                          "town": "City",
                          "country_code": "DEU",
                          "metadata": {}
                      }
                      """;

        // Act
        OrganizationResponse? response = JsonSerializer.Deserialize<OrganizationResponse>(json, _options);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Metadata);
        Assert.Empty(response.Metadata);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PropertySetters_CanSetAllProperties()
    {
        // Arrange
        OrganizationId orgId = OrganizationId.From("12345678-1234-4234-8234-123456789012");
        List<Env> envs = new() { Env.Test, Env.Live };
        MetadataCollection metadata = MetadataCollection.Empty.Add("key1", "value1");
        BillingOptions billingOptions = new()
        {
            Gln = "9012345000004",
            BillToOrganization = OrganizationId.From("87654321-4321-4321-8321-210987654321"),
            WithholdBilling = true
        };

        // Act
        OrganizationResponse response = new()
        {
            Id = orgId,
            Type = OrganizationType.Organization,
            Envs = envs,
            Name = "Test Company",
            DisplayName = "Test Company Display",
            AddressLine1 = "Street 1",
            AddressLine2 = "Building A",
            Zip = "12345",
            Town = "Berlin",
            State = "Berlin",
            CountryCode = CountryCode.DEU,
            VatId = "DE123456789",
            TaxNumber = "12/345/67890",
            EconomyId = "DE1234567890",
            ContactPersonId = Guid.NewGuid(),
            BillingAddressId = Guid.NewGuid(),
            BillingOptions = billingOptions,
            Metadata = metadata,
            ManagedByOrganizationId = null,
            CreatedByUser = Guid.NewGuid()
        };

        // Assert
        Assert.Equal(orgId, response.Id);
        Assert.Equal(OrganizationType.Organization, response.Type);
        Assert.Equal(envs, response.Envs);
        Assert.Equal("Test Company", response.Name);
        Assert.Equal("Test Company Display", response.DisplayName);
        Assert.Equal("Street 1", response.AddressLine1);
        Assert.Equal("Building A", response.AddressLine2);
        Assert.Equal("12345", response.Zip);
        Assert.Equal("Berlin", response.Town);
        Assert.Equal("Berlin", response.State);
        Assert.Equal(CountryCode.DEU, response.CountryCode);
        Assert.Equal("DE123456789", response.VatId);
        Assert.Equal("12/345/67890", response.TaxNumber);
        Assert.Equal("DE1234567890", response.EconomyId);
        Assert.NotNull(response.ContactPersonId);
        Assert.NotNull(response.BillingAddressId);
        Assert.Equal(billingOptions, response.BillingOptions);
        Assert.Equal(metadata, response.Metadata);
        Assert.NotNull(response.CreatedByUser);
    }
}
