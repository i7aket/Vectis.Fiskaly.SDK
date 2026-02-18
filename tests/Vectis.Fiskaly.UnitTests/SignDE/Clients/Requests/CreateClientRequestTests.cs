using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Clients.Requests;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.Requests;

public class CreateClientRequestTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithSerialNumberOnly_ContainsSerialNumber()
    {
        CreateClientRequest request = new CreateClientRequest
        {
            SerialNumber = ClientSerialNumber.From("KASSE-001")
        };

        string json = JsonSerializer.Serialize(request);

        Assert.Contains("\"serial_number\"", json);
        Assert.Contains("\"KASSE-001\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithMetadata_ContainsMetadata()
    {
        CreateClientRequest request = new CreateClientRequest
        {
            SerialNumber = ClientSerialNumber.From("POS-123"),
            Metadata = MetadataCollection.From(new Dictionary<string, string>
            {
                ["location"] = "Store-5",
                ["operator"] = "employee-123"
            })
        };

        string json = JsonSerializer.Serialize(request);

        Assert.Contains("\"metadata\"", json);
        Assert.Contains("\"location\"", json);
        Assert.Contains("\"Store-5\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithNullMetadata_OmitsMetadata()
    {
        CreateClientRequest request = new CreateClientRequest
        {
            SerialNumber = ClientSerialNumber.From("TERMINAL-001"),
            Metadata = null
        };

        string json = JsonSerializer.Serialize(request);

        Assert.DoesNotContain("\"metadata\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidJson_ReturnsRequest()
    {
        string json = """
                      {
                          "serial_number": "KASSE-001",
                          "metadata": {
                              "location": "Store-5"
                          }
                      }
                      """;

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.Converters.Add(new Vectis.Fiskaly.SDK.SignDE.Common.MetadataCollectionJsonConverter());
        CreateClientRequest? request = JsonSerializer.Deserialize<CreateClientRequest>(json, options);

        Assert.NotNull(request);
        Assert.Equal("KASSE-001", request.SerialNumber.Value);
        Assert.NotNull(request.Metadata);
        Assert.Equal("Store-5", request.Metadata["location"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WithoutMetadata_HasNullMetadata()
    {
        string json = """
                      {
                          "serial_number": "POS-999"
                      }
                      """;

        CreateClientRequest? request = JsonSerializer.Deserialize<CreateClientRequest>(json);

        Assert.NotNull(request);
        Assert.Equal("POS-999", request.SerialNumber.Value);
        Assert.Null(request.Metadata);
    }
}
