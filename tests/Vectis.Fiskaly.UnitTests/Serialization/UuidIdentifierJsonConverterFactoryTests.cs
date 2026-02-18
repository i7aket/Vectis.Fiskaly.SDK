using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Serialization;

public class UuidIdentifierJsonConverterFactoryTests
{
    private readonly UuidIdentifierJsonConverterFactory _factory;
    private readonly JsonSerializerOptions _options;

    public UuidIdentifierJsonConverterFactoryTests()
    {
        _factory = new UuidIdentifierJsonConverterFactory();
        _options = new JsonSerializerOptions();
        _options.Converters.Add(_factory);
    }

    // ========================================
    // CanConvert Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_TssId_ReturnsTrue()
    {
        // Act
        bool result = _factory.CanConvert(typeof(TssId));

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_ClientId_ReturnsTrue()
    {
        // Act
        bool result = _factory.CanConvert(typeof(ClientId));

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_TransactionId_ReturnsTrue()
    {
        // Act
        bool result = _factory.CanConvert(typeof(TxId));

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_ExportId_ReturnsTrue()
    {
        // Act
        bool result = _factory.CanConvert(typeof(ExportId));

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_String_ReturnsFalse()
    {
        // Act
        bool result = _factory.CanConvert(typeof(string));

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_Int_ReturnsFalse()
    {
        // Act
        bool result = _factory.CanConvert(typeof(int));

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_Object_ReturnsFalse()
    {
        // Act
        bool result = _factory.CanConvert(typeof(object));

        // Assert
        Assert.False(result);
    }

    // ========================================
    // CreateConverter Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateConverter_TssId_ReturnsConverter()
    {
        // Act
        JsonConverter? converter = _factory.CreateConverter(typeof(TssId), _options);

        // Assert
        Assert.NotNull(converter);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateConverter_SameType_ReturnsSameInstance()
    {
        // Act
        JsonConverter? converter1 = _factory.CreateConverter(typeof(TssId), _options);
        JsonConverter? converter2 = _factory.CreateConverter(typeof(TssId), _options);

        // Assert
        Assert.Same(converter1, converter2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateConverter_DifferentTypes_ReturnsDifferentInstances()
    {
        // Act
        JsonConverter? tssConverter = _factory.CreateConverter(typeof(TssId), _options);
        JsonConverter? clientConverter = _factory.CreateConverter(typeof(ClientId), _options);

        // Assert
        Assert.NotSame(tssConverter, clientConverter);
    }

    // ========================================
    // Serialization Tests (TssId)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_ValidTssId_ReturnsLowercaseUuidString()
    {
        // Arrange
        string uuid = "7b3e4f8a-1234-4abc-9def-123456789012";
        TssId tssId = TssId.From(uuid);

        // Act
        string json = JsonSerializer.Serialize(tssId, _options);

        // Assert
        Assert.Equal($"\"{uuid}\"", json);
        Assert.DoesNotContain(uuid.ToUpperInvariant(), json); // Ensure lowercase
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_MultipleTssIds_SerializesAll()
    {
        // Arrange
        TssId tssId1 = TssId.From("7b3e4f8a-1234-4abc-9def-123456789012");
        TssId tssId2 = TssId.From("8c4f5a9b-2345-4bcd-9ef0-234567890123");
        var data = new { Tss1 = tssId1, Tss2 = tssId2 };

        // Act
        string json = JsonSerializer.Serialize(data, _options);

        // Assert
        Assert.Contains("7b3e4f8a-1234-4abc-9def-123456789012", json);
        Assert.Contains("8c4f5a9b-2345-4bcd-9ef0-234567890123", json);
    }

    // ========================================
    // Deserialization Tests (TssId)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidUuidString_ReturnsTssId()
    {
        // Arrange
        string uuid = "7b3e4f8a-1234-4abc-9def-123456789012";
        string json = $"\"{uuid}\"";

        // Act
        TssId result = JsonSerializer.Deserialize<TssId>(json, _options);

        // Assert
        Assert.Equal(uuid, result.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_UppercaseUuid_ReturnsTssId()
    {
        // Arrange
        string uuid = "7B3E4F8A-1234-4ABC-9DEF-123456789012";
        string json = $"\"{uuid}\"";

        // Act
        TssId result = JsonSerializer.Deserialize<TssId>(json, _options);

        // Assert
        // Should work because From() method trims and validates
        Assert.Equal("7b3e4f8a-1234-4abc-9def-123456789012", result.ToString().ToLowerInvariant());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_UuidWithWhitespace_ReturnsTssId()
    {
        // Arrange
        string uuid = "  7b3e4f8a-1234-4abc-9def-123456789012  ";
        string json = $"\"{uuid}\"";

        // Act
        TssId result = JsonSerializer.Deserialize<TssId>(json, _options);

        // Assert
        Assert.Equal("7b3e4f8a-1234-4abc-9def-123456789012", result.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NullString_ThrowsJsonException()
    {
        // Arrange
        string json = "null";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TssId>(json, _options));

        Assert.Contains("Expected string token", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_EmptyString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"\"";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TssId>(json, _options));

        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WhitespaceString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"   \"";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TssId>(json, _options));

        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_InvalidUuidFormat_ThrowsJsonException()
    {
        // Arrange
        string json = "\"not-a-uuid\"";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TssId>(json, _options));

        Assert.Contains("Invalid", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NumberToken_ThrowsJsonException()
    {
        // Arrange
        string json = "123";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TssId>(json, _options));

        Assert.Contains("Expected string token", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_BooleanToken_ThrowsJsonException()
    {
        // Arrange
        string json = "true";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TssId>(json, _options));

        Assert.Contains("Expected string token", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ObjectToken_ThrowsJsonException()
    {
        // Arrange
        string json = "{}";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TssId>(json, _options));

        Assert.Contains("Expected string token", exception.Message);
    }

    // ========================================
    // Round-Trip Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_TssId_PreservesValue()
    {
        // Arrange
        TssId original = TssId.From("7b3e4f8a-1234-4abc-9def-123456789012");

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        TssId deserialized = JsonSerializer.Deserialize<TssId>(json, _options);

        // Assert
        Assert.Equal(original, deserialized);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_ClientId_PreservesValue()
    {
        // Arrange
        ClientId original = ClientId.From("8c4f5a9b-2345-4bcd-9ef0-234567890123");

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        ClientId deserialized = JsonSerializer.Deserialize<ClientId>(json, _options);

        // Assert
        Assert.Equal(original, deserialized);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_TransactionId_PreservesValue()
    {
        // Arrange
        TxId original = TxId.From("9d5a6b7c-3456-4cde-9f12-345678901234");

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        TxId deserialized = JsonSerializer.Deserialize<TxId>(json, _options);

        // Assert
        Assert.Equal(original, deserialized);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_ExportId_PreservesValue()
    {
        // Arrange
        ExportId original = ExportId.From("ae6b7c8d-4567-4def-9a23-456789012345");

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        ExportId deserialized = JsonSerializer.Deserialize<ExportId>(json, _options);

        // Assert
        Assert.Equal(original, deserialized);
    }

    // ========================================
    // Complex Object Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ComplexObject_WithMultipleUuidTypes_WorksCorrectly()
    {
        // Arrange
        string json = "{\"tssId\":\"7b3e4f8a-1234-4abc-9def-123456789012\",\"clientId\":\"8c4f5a9b-2345-4bcd-9ef0-234567890123\"}";
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(_factory);

        // Act
        TestClass? result = JsonSerializer.Deserialize<TestClass>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("7b3e4f8a-1234-4abc-9def-123456789012", result.TssId.ToString());
        Assert.Equal("8c4f5a9b-2345-4bcd-9ef0-234567890123", result.ClientId.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_ComplexObject_WithMultipleUuidTypes_WorksCorrectly()
    {
        // Arrange
        TestClass obj = new TestClass
        {
            TssId = TssId.From("7b3e4f8a-1234-4abc-9def-123456789012"),
            ClientId = ClientId.From("8c4f5a9b-2345-4bcd-9ef0-234567890123")
        };

        // Act
        string json = JsonSerializer.Serialize(obj, _options);

        // Assert
        Assert.Contains("7b3e4f8a-1234-4abc-9def-123456789012", json);
        Assert.Contains("8c4f5a9b-2345-4bcd-9ef0-234567890123", json);
    }

    private class TestClass
    {
        public TssId TssId { get; set; }
        public ClientId ClientId { get; set; }
    }
}
