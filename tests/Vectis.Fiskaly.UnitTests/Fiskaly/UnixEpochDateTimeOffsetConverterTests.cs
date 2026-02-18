using System.Text.Json;
using Vectis.Fiskaly.SDK.Serialization;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class UnixEpochDateTimeOffsetConverterTests
{
    private readonly JsonSerializerOptions _options;

    public UnixEpochDateTimeOffsetConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new UnixEpochDateTimeOffsetConverterFactory() }
        };
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_DateTimeOffset_WritesUnixSeconds()
    {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeSeconds(1_704_276_000); // 2024-01-03 10:00:00 UTC

        string json = JsonSerializer.Serialize(timestamp, _options);

        Assert.Equal("1704276000", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_Number_ReturnsDateTimeOffset()
    {
        string json = "1704276000";

        DateTimeOffset timestamp = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        Assert.Equal(1_704_276_000, timestamp.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_StringNumber_ReturnsDateTimeOffset()
    {
        string json = "\"1704276000\"";

        DateTimeOffset timestamp = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        Assert.Equal(1_704_276_000, timestamp.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_Iso8601String_ReturnsDateTimeOffset()
    {
        string json = "\"2024-01-03T10:00:00Z\"";

        DateTimeOffset timestamp = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        Assert.Equal(1_704_276_000, timestamp.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NullableNull_ReturnsNull()
    {
        string json = "null";

        DateTimeOffset? timestamp = JsonSerializer.Deserialize<DateTimeOffset?>(json, _options);

        Assert.Null(timestamp);
    }

    // ========================================
    // Unix Epoch Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_UnixEpoch_ReturnsZero()
    {
        // Arrange
        DateTimeOffset epoch = DateTimeOffset.UnixEpoch; // 1970-01-01 00:00:00 UTC

        // Act
        string json = JsonSerializer.Serialize(epoch, _options);

        // Assert
        Assert.Equal("0", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_Zero_ReturnsUnixEpoch()
    {
        // Arrange
        string json = "0";

        // Act
        DateTimeOffset timestamp = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        // Assert
        Assert.Equal(DateTimeOffset.UnixEpoch, timestamp);
        Assert.Equal(0, timestamp.ToUnixTimeSeconds());
    }

    // ========================================
    // Before Unix Epoch (Negative Timestamps)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_DateBeforeEpoch_ReturnsNegativeSeconds()
    {
        // Arrange
        DateTimeOffset beforeEpoch = new DateTimeOffset(1960, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        string json = JsonSerializer.Serialize(beforeEpoch, _options);

        // Assert
        long seconds = long.Parse(json);
        Assert.True(seconds < 0, "Date before Unix epoch should have negative seconds");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NegativeSeconds_ReturnsDateBeforeEpoch()
    {
        // Arrange
        string json = "-315619200"; // 1960-01-01 00:00:00 UTC

        // Act
        DateTimeOffset timestamp = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        // Assert
        Assert.True(timestamp < DateTimeOffset.UnixEpoch);
        Assert.Equal(-315619200, timestamp.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NegativeStringSeconds_ReturnsDateBeforeEpoch()
    {
        // Arrange
        string json = "\"-315619200\""; // String format

        // Act
        DateTimeOffset timestamp = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        // Assert
        Assert.True(timestamp < DateTimeOffset.UnixEpoch);
        Assert.Equal(-315619200, timestamp.ToUnixTimeSeconds());
    }

    // ========================================
    // Current Date and Realistic Values
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_CurrentDate_ReturnsReasonableValue()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Act
        string json = JsonSerializer.Serialize(now, _options);

        // Assert
        long seconds = long.Parse(json);
        // Should be between 2020 and 2100 (reasonable bounds)
        Assert.True(seconds > 1577836800, "Should be after 2020-01-01"); // 2020-01-01
        Assert.True(seconds < 4102444800, "Should be before 2100-01-01"); // 2100-01-01
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_Year2025_ReturnsCorrectSeconds()
    {
        // Arrange
        DateTimeOffset date2025 = new DateTimeOffset(2025, 10, 23, 12, 0, 0, TimeSpan.Zero);

        // Act
        string json = JsonSerializer.Serialize(date2025, _options);

        // Assert
        Assert.NotNull(json);
        long seconds = long.Parse(json);
        Assert.True(seconds > 1729684800, "Should be in late 2024 or later");
    }

    // ========================================
    // Error Handling Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NonNullable_NullToken_ThrowsJsonException()
    {
        // Arrange
        string json = "null";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset>(json, _options));

        Assert.Contains("cannot be null", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_BoolToken_ThrowsJsonException()
    {
        // Arrange
        string json = "true";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset>(json, _options));

        Assert.Contains("Unexpected token", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ObjectToken_ThrowsJsonException()
    {
        // Arrange
        string json = "{}";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset>(json, _options));

        Assert.Contains("Unexpected token", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ArrayToken_ThrowsJsonException()
    {
        // Arrange
        string json = "[]";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset>(json, _options));

        Assert.Contains("Unexpected token", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_EmptyString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"\"";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset>(json, _options));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WhitespaceString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"   \"";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset>(json, _options));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_InvalidString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"invalid-timestamp\"";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<DateTimeOffset>(json, _options));

        Assert.Contains("not a valid timestamp", exception.Message);
    }

    // ========================================
    // Round-Trip Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_PreservesDateTimeOffset()
    {
        // Arrange
        DateTimeOffset original = new DateTimeOffset(2024, 5, 15, 14, 30, 0, TimeSpan.Zero);

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        DateTimeOffset deserialized = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        // Assert
        Assert.Equal(original.ToUnixTimeSeconds(), deserialized.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_IgnoresMilliseconds()
    {
        // Arrange
        DateTimeOffset withMilliseconds = new DateTimeOffset(2024, 5, 15, 14, 30, 45, 123, TimeSpan.Zero);

        // Act - Serialize and deserialize
        string json = JsonSerializer.Serialize(withMilliseconds, _options);
        DateTimeOffset deserialized = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        // Assert - Unix timestamp precision is seconds, so milliseconds are lost
        Assert.Equal(withMilliseconds.ToUnixTimeSeconds(), deserialized.ToUnixTimeSeconds());
        Assert.NotEqual(withMilliseconds.Millisecond, deserialized.Millisecond);
        Assert.Equal(0, deserialized.Millisecond); // Should be truncated to 0
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_NegativeTimestamp_PreservesValue()
    {
        // Arrange
        DateTimeOffset beforeEpoch = new DateTimeOffset(1960, 6, 15, 10, 0, 0, TimeSpan.Zero);

        // Act
        string json = JsonSerializer.Serialize(beforeEpoch, _options);
        DateTimeOffset deserialized = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        // Assert
        Assert.Equal(beforeEpoch.ToUnixTimeSeconds(), deserialized.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_UnixEpoch_PreservesValue()
    {
        // Arrange
        DateTimeOffset epoch = DateTimeOffset.UnixEpoch;

        // Act
        string json = JsonSerializer.Serialize(epoch, _options);
        DateTimeOffset deserialized = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

        // Assert
        Assert.Equal(epoch, deserialized);
        Assert.Equal(0, deserialized.ToUnixTimeSeconds());
    }

    // ========================================
    // Nullable Variant Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_NullableWithValue_ReturnsSeconds()
    {
        // Arrange
        DateTimeOffset? timestamp = DateTimeOffset.FromUnixTimeSeconds(1704276000);

        // Act
        string json = JsonSerializer.Serialize(timestamp, _options);

        // Assert
        Assert.Equal("1704276000", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_NullableNull_ReturnsNull()
    {
        // Arrange
        DateTimeOffset? timestamp = null;

        // Act
        string json = JsonSerializer.Serialize(timestamp, _options);

        // Assert
        Assert.Equal("null", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NullableNumber_ReturnsDateTimeOffset()
    {
        // Arrange
        string json = "1704276000";

        // Act
        DateTimeOffset? timestamp = JsonSerializer.Deserialize<DateTimeOffset?>(json, _options);

        // Assert
        Assert.NotNull(timestamp);
        Assert.Equal(1704276000, timestamp.Value.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NullableStringNumber_ReturnsDateTimeOffset()
    {
        // Arrange
        string json = "\"1704276000\"";

        // Act
        DateTimeOffset? timestamp = JsonSerializer.Deserialize<DateTimeOffset?>(json, _options);

        // Assert
        Assert.NotNull(timestamp);
        Assert.Equal(1704276000, timestamp.Value.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_Nullable_PreservesValue()
    {
        // Arrange
        DateTimeOffset? original = new DateTimeOffset(2024, 12, 1, 8, 0, 0, TimeSpan.Zero);

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        DateTimeOffset? deserialized = JsonSerializer.Deserialize<DateTimeOffset?>(json, _options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Value.ToUnixTimeSeconds(), deserialized.Value.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_NullableNull_PreservesNull()
    {
        // Arrange
        DateTimeOffset? original = null;

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        DateTimeOffset? deserialized = JsonSerializer.Deserialize<DateTimeOffset?>(json, _options);

        // Assert
        Assert.Null(deserialized);
    }

    // ========================================
    // Boundary Value Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_VeryLargeTimestamp_HandlesCorrectly()
    {
        // Arrange - Year 2100
        DateTimeOffset futureDate = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        string json = JsonSerializer.Serialize(futureDate, _options);

        // Assert
        Assert.NotNull(json);
        long seconds = long.Parse(json);
        Assert.True(seconds > 0);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_VeryOldTimestamp_HandlesCorrectly()
    {
        // Arrange - Year 1900
        DateTimeOffset oldDate = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        string json = JsonSerializer.Serialize(oldDate, _options);

        // Assert
        Assert.NotNull(json);
        long seconds = long.Parse(json);
        Assert.True(seconds < 0, "Dates before Unix epoch should be negative");
    }
}
