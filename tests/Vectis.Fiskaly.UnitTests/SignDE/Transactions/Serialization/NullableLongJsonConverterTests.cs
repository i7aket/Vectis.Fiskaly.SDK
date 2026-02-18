using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Serialization;

public class NullableLongJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public NullableLongJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _options.Converters.Add(new NullableLongJsonConverter());
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("123", 123L)]
    [InlineData("0", 0L)]
    [InlineData("-456", -456L)]
    [InlineData("9223372036854775807", long.MaxValue)] // long.MaxValue
    [InlineData("-9223372036854775808", long.MinValue)] // long.MinValue
    public void Deserialize_ValidNumber_ReturnsLong(string json, long expected)
    {
        // Act
        long? result = JsonSerializer.Deserialize<long?>(json, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"123\"", 123L)]
    [InlineData("\"0\"", 0L)]
    [InlineData("\"-456\"", -456L)]
    [InlineData("\"9223372036854775807\"", long.MaxValue)]
    [InlineData("\"-9223372036854775808\"", long.MinValue)]
    public void Deserialize_ValidStringNumber_ReturnsLong(string json, long expected)
    {
        // Act
        long? result = JsonSerializer.Deserialize<long?>(json, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_Null_ReturnsNull()
    {
        // Arrange
        string json = "null";

        // Act
        long? result = JsonSerializer.Deserialize<long?>(json, _options);

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("\"\\t\"")]
    public void Deserialize_EmptyOrWhitespaceString_ReturnsNull(string json)
    {
        // Act
        long? result = JsonSerializer.Deserialize<long?>(json, _options);

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"not a number\"")]
    [InlineData("\"abc123\"")]
    [InlineData("\"12.34\"")]
    [InlineData("\"1e10\"")]
    public void Deserialize_InvalidString_ReturnsNull(string json)
    {
        // Act
        long? result = JsonSerializer.Deserialize<long?>(json, _options);

        // Assert
        Assert.Null(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_BooleanToken_ThrowsJsonException()
    {
        // Arrange
        string json = "true";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<long?>(json, _options));

        Assert.Contains("Cannot convert", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ObjectToken_ThrowsJsonException()
    {
        // Arrange
        string json = "{}";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<long?>(json, _options));

        Assert.Contains("Cannot convert", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(123L, "123")]
    [InlineData(0L, "0")]
    [InlineData(-456L, "-456")]
    [InlineData(9223372036854775807L, "9223372036854775807")] // long.MaxValue
    [InlineData(-9223372036854775808L, "-9223372036854775808")] // long.MinValue
    public void Serialize_ValidLong_ReturnsNumber(long value, string expected)
    {
        // Arrange
        long? nullableValue = value;

        // Act
        string json = JsonSerializer.Serialize(nullableValue, _options);

        // Assert
        Assert.Equal(expected, json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_Null_ReturnsNull()
    {
        // Arrange
        long? value = null;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal("null", json);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(123L)]
    [InlineData(0L)]
    [InlineData(-456L)]
    [InlineData(9223372036854775807L)]
    [InlineData(-9223372036854775808L)]
    public void RoundTrip_Number_PreservesValue(long original)
    {
        // Act
        string json = JsonSerializer.Serialize(original, _options);
        long? deserialized = JsonSerializer.Deserialize<long?>(json, _options);

        // Assert
        Assert.Equal(original, deserialized);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"123\"")]
    [InlineData("\"0\"")]
    [InlineData("\"-456\"")]
    public void RoundTrip_StringNumber_ConvertsToNumber(string jsonInput)
    {
        // Act - deserialize string, serialize back
        long? deserialized = JsonSerializer.Deserialize<long?>(jsonInput, _options);
        string jsonOutput = JsonSerializer.Serialize(deserialized, _options);

        // Assert - output should be a number (no quotes)
        Assert.DoesNotContain("\"", jsonOutput);
        Assert.Equal(deserialized!.Value.ToString(), jsonOutput);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_Null_PreservesNull()
    {
        // Arrange
        long? original = null;

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        long? deserialized = JsonSerializer.Deserialize<long?>(json, _options);

        // Assert
        Assert.Null(deserialized);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ComplexObject_WithNullableLong_WorksCorrectly()
    {
        // Arrange
        string json = "{\"counter\":\"123\",\"name\":\"test\"}";

        // Act
        TestClass? result = JsonSerializer.Deserialize<TestClass>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123L, result.Counter);
        Assert.Equal("test", result.Name);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ComplexObject_WithNullCounter_WorksCorrectly()
    {
        // Arrange
        string json = "{\"counter\":null,\"name\":\"test\"}";

        // Act
        TestClass? result = JsonSerializer.Deserialize<TestClass>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Counter);
        Assert.Equal("test", result.Name);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ComplexObject_WithEmptyStringCounter_ReturnsNull()
    {
        // Arrange
        string json = "{\"counter\":\"\",\"name\":\"test\"}";

        // Act
        TestClass? result = JsonSerializer.Deserialize<TestClass>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Counter);
        Assert.Equal("test", result.Name);
    }

    private class TestClass
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(NullableLongJsonConverter))]
        public long? Counter { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
