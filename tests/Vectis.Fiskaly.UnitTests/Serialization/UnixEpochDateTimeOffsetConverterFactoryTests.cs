using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;

namespace Vectis.Fiskaly.UnitTests.Serialization;

public class UnixEpochDateTimeOffsetConverterFactoryTests
{
    private readonly UnixEpochDateTimeOffsetConverterFactory _factory;
    private readonly JsonSerializerOptions _options;

    public UnixEpochDateTimeOffsetConverterFactoryTests()
    {
        _factory = new UnixEpochDateTimeOffsetConverterFactory();
        _options = new JsonSerializerOptions();
    }

    // ========================================
    // CanConvert Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_DateTimeOffset_ReturnsTrue()
    {
        // Act
        bool result = _factory.CanConvert(typeof(DateTimeOffset));

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CanConvert_NullableDateTimeOffset_ReturnsTrue()
    {
        // Act
        bool result = _factory.CanConvert(typeof(DateTimeOffset?));

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
    public void CanConvert_DateTime_ReturnsFalse()
    {
        // Act
        bool result = _factory.CanConvert(typeof(DateTime));

        // Assert
        Assert.False(result);
    }

    // ========================================
    // CreateConverter Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateConverter_DateTimeOffset_ReturnsConverter()
    {
        // Act
        JsonConverter? converter = _factory.CreateConverter(typeof(DateTimeOffset), _options);

        // Assert
        Assert.NotNull(converter);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateConverter_NullableDateTimeOffset_ReturnsConverter()
    {
        // Act
        JsonConverter? converter = _factory.CreateConverter(typeof(DateTimeOffset?), _options);

        // Assert
        Assert.NotNull(converter);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateConverter_UnsupportedType_ThrowsNotSupportedException()
    {
        // Act & Assert
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            _factory.CreateConverter(typeof(string), _options));

        Assert.Contains("System.String", exception.Message);
        Assert.Contains("UnixEpochDateTimeOffsetConverterFactory", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateConverter_DateTime_ThrowsNotSupportedException()
    {
        // Act & Assert
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            _factory.CreateConverter(typeof(DateTime), _options));

        Assert.Contains("DateTime", exception.Message);
    }
}
