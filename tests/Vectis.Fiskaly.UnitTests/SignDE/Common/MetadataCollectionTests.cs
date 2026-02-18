namespace Vectis.Fiskaly.UnitTests.SignDE.Common;

public class MetadataCollectionTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Empty_ReturnsEmptyCollection()
    {
        MetadataCollection empty = MetadataCollection.Empty;

        Assert.Empty(empty);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidEntries_CreatesCollection()
    {
        Dictionary<string, string> entries = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        MetadataCollection collection = MetadataCollection.From(entries);

        Assert.Equal(2, collection.Count);
        Assert.Equal("value1", collection["key1"]);
        Assert.Equal("value2", collection["key2"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_NewEntry_ReturnsNewCollection()
    {
        MetadataCollection original = MetadataCollection.Empty;

        MetadataCollection updated = original.Add("test", "value");

        Assert.Empty(original);
        Assert.Single(updated);
        Assert.Equal("value", updated["test"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_ExceedsMaxEntries_ThrowsArgumentException()
    {
        MetadataCollection collection = MetadataCollection.Empty;
        for (int i = 0; i < MetadataCollection.MaxEntries; i++)
        {
            collection = collection.Add($"key{i}", "value");
        }

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            collection.Add("overflow", "value"));

        Assert.Contains($"cannot contain more than {MetadataCollection.MaxEntries}", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_KeyTooLong_ThrowsArgumentException()
    {
        string longKey = new string('a', MetadataCollection.MaxKeyLength + 1);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            MetadataCollection.Empty.Add(longKey, "value"));

        Assert.Contains($"exceeds maximum length of {MetadataCollection.MaxKeyLength}", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_ValueTooLong_ThrowsArgumentException()
    {
        string longValue = new string('a', MetadataCollection.MaxValueLength + 1);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            MetadataCollection.Empty.Add("key", longValue));

        Assert.Contains($"exceeds maximum length of {MetadataCollection.MaxValueLength}", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryAdd_ValidEntry_ReturnsTrue()
    {
        bool result = MetadataCollection.Empty.TryAdd("key", "value", out MetadataCollection? collection, out string? error);

        Assert.True(result);
        Assert.NotNull(collection);
        Assert.Null(error);
        Assert.Equal("value", collection["key"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryAdd_ExceedsMaxEntries_ReturnsFalse()
    {
        MetadataCollection collection = MetadataCollection.Empty;
        for (int i = 0; i < MetadataCollection.MaxEntries; i++)
        {
            collection = collection.Add($"key{i}", "value");
        }

        bool result = collection.TryAdd("overflow", "value", out MetadataCollection? newCollection, out string? error);

        Assert.False(result);
        Assert.Null(newCollection);
        Assert.NotNull(error);
        Assert.Contains($"cannot contain more than {MetadataCollection.MaxEntries}", error);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_SameContents_ReturnsTrue()
    {
        MetadataCollection collection1 = MetadataCollection.From(new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        });

        MetadataCollection collection2 = MetadataCollection.From(new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        });

        Assert.True(collection1.Equals(collection2));
        Assert.Equal(collection1.GetHashCode(), collection2.GetHashCode());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_DifferentContents_ReturnsFalse()
    {
        MetadataCollection collection1 = MetadataCollection.From(new Dictionary<string, string>
        {
            ["key1"] = "value1"
        });

        MetadataCollection collection2 = MetadataCollection.From(new Dictionary<string, string>
        {
            ["key1"] = "different"
        });

        Assert.False(collection1.Equals(collection2));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToDictionary_CreatesNewDictionary()
    {
        MetadataCollection collection = MetadataCollection.From(new Dictionary<string, string>
        {
            ["key1"] = "value1"
        });

        Dictionary<string, string> dict = collection.ToDictionary();

        Assert.Equal("value1", dict["key1"]);
        dict["key1"] = "modified";
        Assert.Equal("value1", collection["key1"]);
    }

    // ============================================================================
    // Edge Case Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void From_EmptyDictionary_CreatesEmptyCollection()
    {
        MetadataCollection collection = MetadataCollection.From(new Dictionary<string, string>());

        Assert.Empty(collection);
        Assert.Equal(0, collection.Count);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_BoundaryKeyLength_Succeeds()
    {
        string maxLengthKey = new string('k', MetadataCollection.MaxKeyLength);

        MetadataCollection collection = MetadataCollection.Empty.Add(maxLengthKey, "value");

        Assert.Single(collection);
        Assert.Equal("value", collection[maxLengthKey]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_BoundaryValueLength_Succeeds()
    {
        string maxLengthValue = new string('v', MetadataCollection.MaxValueLength);

        MetadataCollection collection = MetadataCollection.Empty.Add("key", maxLengthValue);

        Assert.Single(collection);
        Assert.Equal(maxLengthValue, collection["key"]);
    }
}
