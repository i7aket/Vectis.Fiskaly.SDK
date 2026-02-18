using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Tss.Requests;

namespace Vectis.Fiskaly.UnitTests.SignDE.Tss.Requests;

public class CreateTssRequestTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithMetadata_ContainsMetadata()
    {
        CreateTssRequest request = new CreateTssRequest
        {
            Metadata = MetadataCollection.From(new Dictionary<string, string>
            {
                ["location"] = "Hamburg Store #5",
                ["store_id"] = "STORE-001"
            })
        };

        string json = JsonSerializer.Serialize(request);

        Assert.Contains("\"metadata\"", json);
        Assert.Contains("\"location\"", json);
        Assert.Contains("\"Hamburg Store #5\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithNullMetadata_OmitsMetadata()
    {
        CreateTssRequest request = new CreateTssRequest
        {
            Metadata = null
        };

        string json = JsonSerializer.Serialize(request);

        Assert.DoesNotContain("\"metadata\"", json);
        Assert.Equal("{}", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidJson_ReturnsRequest()
    {
        string json = """
                      {
                          "metadata": {
                              "environment": "production"
                          }
                      }
                      """;

        JsonSerializerOptions options = new JsonSerializerOptions();
        options.Converters.Add(new Vectis.Fiskaly.SDK.SignDE.Common.MetadataCollectionJsonConverter());
        CreateTssRequest? request = JsonSerializer.Deserialize<CreateTssRequest>(json, options);

        Assert.NotNull(request);
        Assert.NotNull(request.Metadata);
        Assert.Equal("production", request.Metadata["environment"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_EmptyJson_HasNullMetadata()
    {
        string json = "{}";

        CreateTssRequest? request = JsonSerializer.Deserialize<CreateTssRequest>(json);

        Assert.NotNull(request);
        Assert.Null(request.Metadata);
    }
}
