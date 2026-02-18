using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class MetadataCollectionTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void FromDictionary_WithValidEntries_CreatesImmutableCollection()
    {
        Dictionary<string, string> source = new Dictionary<string, string>
        {
            ["location"] = "Store-01",
            ["manager"] = "Alice"
        };

        MetadataCollection metadata = MetadataCollection.FromDictionary(source);

        Assert.Equal(2, metadata.Count);
        Assert.Equal("Store-01", metadata["location"]);
        Assert.Equal("Alice", metadata["manager"]);

        source["location"] = "Store-02";
        Assert.Equal("Store-01", metadata["location"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_WhenKeyExceedsLimit_ThrowsArgumentException()
    {
        MetadataCollection metadata = MetadataCollection.Empty;
        string longKey = new string('k', MetadataCollection.MaxKeyLength + 1);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => metadata.Add(longKey, "value"));
        Assert.Contains("exceeds maximum length", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryAdd_WithTooManyEntries_ReturnsFalseAndNoChange()
    {
        Dictionary<string, string> payload = Enumerable.Range(0, MetadataCollection.MaxEntries)
            .ToDictionary(i => $"key{i}", _ => "value");
        MetadataCollection metadata = MetadataCollection.FromDictionary(payload);

        bool success = metadata.TryAdd("extra", "value", out MetadataCollection? updated, out string? error);

        Assert.False(success);
        Assert.Null(updated);
        Assert.NotNull(error);
        Assert.Equal(MetadataCollection.MaxEntries, metadata.Count);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void JsonSerialization_RoundTripsCollection()
    {
        MetadataCollection metadata = MetadataCollection.FromDictionary(new Dictionary<string, string>
        {
            ["cashier_id"] = "123",
            ["shift"] = "morning"
        });

        JsonSerializerOptions options = new JsonSerializerOptions();
        Type converterType = typeof(MetadataCollection)
            .Assembly
            .GetType("Vectis.Fiskaly.SDK.SignDE.Common.MetadataCollectionJsonConverter", throwOnError: true)!;
        JsonConverter converter = (JsonConverter)Activator.CreateInstance(converterType)!;
        options.Converters.Add(converter);

        string json = JsonSerializer.Serialize(metadata, options);
        MetadataCollection? deserialized = JsonSerializer.Deserialize<MetadataCollection>(json, options);

        Assert.NotNull(deserialized);
        Assert.Equal(metadata.Count, deserialized!.Count);
        Assert.Equal("123", deserialized["cashier_id"]);
    }
}
