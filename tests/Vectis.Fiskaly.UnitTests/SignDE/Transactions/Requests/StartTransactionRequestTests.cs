using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Requests;

public class StartTransactionRequestTests
{
    private readonly JsonSerializerOptions _options;

    public StartTransactionRequestTests()
    {
        _options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_SetsStateToActive()
    {
        StartTransactionRequest request = new StartTransactionRequest
        {
            ClientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012")
        };

        Assert.Equal(TxState.Active, request.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithClientId_IncludesClientId()
    {
        StartTransactionRequest request = new StartTransactionRequest
        {
            ClientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012")
        };

        string json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"state\":\"ACTIVE\"", json);
        Assert.Contains("\"client_id\":\"a1b2c3d4-1234-4abc-9def-123456789012\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithMetadata_IncludesMetadata()
    {
        StartTransactionRequest request = new StartTransactionRequest
        {
            ClientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012"),
            Metadata = MetadataCollection.Empty.Add("order_id", "ORD-12345")
        };

        string json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"state\":\"ACTIVE\"", json);
        Assert.Contains("\"metadata\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithoutMetadata_OmitsMetadata()
    {
        StartTransactionRequest request = new StartTransactionRequest
        {
            ClientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012")
        };

        string json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"state\":\"ACTIVE\"", json);
        Assert.DoesNotContain("\"metadata\"", json);
    }
}
