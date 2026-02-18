using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class TransactionIdJsonConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        // TxId has [JsonConverter] attribute, so default options will use it
        return new JsonSerializerOptions();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_StringUuid_ReturnsIdentifier()
    {
        string uuid = "550e8400-e29b-41d4-a716-446655440000";
        string json = $"\"{uuid}\"";

        TxId identifier = JsonSerializer.Deserialize<TxId>(json, CreateOptions());

        Assert.Equal(Guid.Parse(uuid), identifier.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_InvalidString_ThrowsJsonException()
    {
        string json = "\"invalid\"";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TxId>(json, CreateOptions()));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_Uuid_WritesString()
    {
        TxId identifier = TxId.From("550e8400-e29b-41d4-a716-446655440000");
        string json = JsonSerializer.Serialize(identifier, CreateOptions());

        Assert.Equal("\"550e8400-e29b-41d4-a716-446655440000\"", json);
    }

}
