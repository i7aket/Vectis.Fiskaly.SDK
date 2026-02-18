using System.Text.Json;

namespace Vectis.Fiskaly.UnitTests.SignDE.Common;

public class MetadataCollectionJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public MetadataCollectionJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new MetadataCollectionJsonConverter());
    }

    // ============================================================================
    // Deserialize JSON Object Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidObject_ReturnsMetadataCollection()
    {
        string json = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        MetadataCollection? result = JsonSerializer.Deserialize<MetadataCollection>(json, _options);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("value1", result["key1"]);
        Assert.Equal("value2", result["key2"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_EmptyObject_ReturnsEmptyCollection()
    {
        string json = "{}";

        MetadataCollection? result = JsonSerializer.Deserialize<MetadataCollection>(json, _options);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NullValueInJson_ThrowsJsonException()
    {
        string json = "{\"key\":null}";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MetadataCollection>(json, _options));

        Assert.Contains("cannot be null", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MultipleEntries_PreservesAll()
    {
        string json = "{\"k1\":\"v1\",\"k2\":\"v2\",\"k3\":\"v3\"}";

        MetadataCollection? result = JsonSerializer.Deserialize<MetadataCollection>(json, _options);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("v1", result["k1"]);
        Assert.Equal("v2", result["k2"]);
        Assert.Equal("v3", result["k3"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NumberValue_ThrowsJsonException()
    {
        string json = "{\"count\":42}";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MetadataCollection>(json, _options));

        Assert.Contains("must be strings or null", exception.Message);
        Assert.Contains("Number", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_BooleanValue_ThrowsJsonException()
    {
        string json = "{\"enabled\":true}";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MetadataCollection>(json, _options));

        Assert.Contains("must be strings or null", exception.Message);
        Assert.Contains("True", exception.Message);
    }

    // ============================================================================
    // Deserialize Invalid Token Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_Null_ReturnsNull()
    {
        string json = "null";

        MetadataCollection? result = JsonSerializer.Deserialize<MetadataCollection>(json, _options);

        Assert.Null(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ArrayToken_ThrowsJsonException()
    {
        string json = "[\"key\",\"value\"]";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MetadataCollection>(json, _options));

        Assert.Contains("must be a JSON object", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_StringToken_ThrowsJsonException()
    {
        string json = "\"metadata\"";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MetadataCollection>(json, _options));

        Assert.Contains("must be a JSON object", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NumberToken_ThrowsJsonException()
    {
        string json = "123";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MetadataCollection>(json, _options));

        Assert.Contains("must be a JSON object", exception.Message);
    }

    // ============================================================================
    // Serialize MetadataCollection Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_MetadataCollection_ReturnsJsonObject()
    {
        MetadataCollection collection = MetadataCollection.From(new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        });

        string json = JsonSerializer.Serialize(collection, _options);

        Assert.Contains("\"key1\":\"value1\"", json);
        Assert.Contains("\"key2\":\"value2\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_EmptyCollection_ReturnsEmptyObject()
    {
        MetadataCollection collection = MetadataCollection.Empty;

        string json = JsonSerializer.Serialize(collection, _options);

        Assert.Equal("{}", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_SingleEntry_ReturnsCorrectJson()
    {
        MetadataCollection collection = MetadataCollection.From(new Dictionary<string, string>
        {
            ["test"] = "value"
        });

        string json = JsonSerializer.Serialize(collection, _options);

        Assert.Equal("{\"test\":\"value\"}", json);
    }

    // ============================================================================
    // Round-trip Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_PreservesKeyValuePairs()
    {
        string original = "{\"k1\":\"v1\",\"k2\":\"v2\",\"k3\":\"v3\"}";

        MetadataCollection? deserialized = JsonSerializer.Deserialize<MetadataCollection>(original, _options);
        string serialized = JsonSerializer.Serialize(deserialized, _options);
        MetadataCollection? roundTrip = JsonSerializer.Deserialize<MetadataCollection>(serialized, _options);

        Assert.NotNull(deserialized);
        Assert.NotNull(roundTrip);
        Assert.Equal(deserialized.Count, roundTrip.Count);
        Assert.Equal("v1", roundTrip["k1"]);
        Assert.Equal("v2", roundTrip["k2"]);
        Assert.Equal("v3", roundTrip["k3"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_EmptyCollection_PreservesEmpty()
    {
        string original = "{}";

        MetadataCollection? deserialized = JsonSerializer.Deserialize<MetadataCollection>(original, _options);
        string serialized = JsonSerializer.Serialize(deserialized, _options);

        Assert.Equal(original, serialized);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NullValue_ThrowsJsonException()
    {
        string original = "{\"key\":null}";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MetadataCollection>(original, _options));

        Assert.Contains("cannot be null", exception.Message);
    }

    private class TestObject
    {
        public MetadataCollection? Metadata { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
