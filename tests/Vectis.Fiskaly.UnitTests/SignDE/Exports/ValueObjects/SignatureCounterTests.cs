using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.ValueObjects;

public class SignatureCounterTests
{
    // ============================================================================
    // Factory Method Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1_000_000)]
    [InlineData(9_007_199_254_740_991)]
    public void From_ValidValue_ReturnsCounter(long value)
    {
        SignatureCounter counter = SignatureCounter.From(value);

        Assert.Equal(value, counter.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(9_007_199_254_740_992)]
    [InlineData(long.MaxValue)]
    public void From_InvalidValue_ThrowsArgumentOutOfRangeException(long value)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SignatureCounter.From(value));

        Assert.Contains($"must be between {SignatureCounter.Min} and {SignatureCounter.Max}", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0, true)]
    [InlineData(9_007_199_254_740_991, true)]
    [InlineData(-1, false)]
    [InlineData(9_007_199_254_740_992, false)]
    public void TryFrom_VariousValues_ReturnsExpected(long value, bool expected)
    {
        bool result = SignatureCounter.TryFrom(value, out SignatureCounter counter);

        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.Equal(value, counter.Value);
        }
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1000)]
    public void TryFrom_NegativeValues_ReturnsFalse(long value)
    {
        bool result = SignatureCounter.TryFrom(value, out SignatureCounter counter);

        Assert.False(result);
        Assert.Equal(0, counter.Value);
    }

    // ============================================================================
    // Boundary Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void From_MinimumValue_ReturnsCounter()
    {
        SignatureCounter counter = SignatureCounter.From(SignatureCounter.Min);

        Assert.Equal(SignatureCounter.Min, counter.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_MaximumValue_ReturnsCounter()
    {
        SignatureCounter counter = SignatureCounter.From(SignatureCounter.Max);

        Assert.Equal(SignatureCounter.Max, counter.Value);
        Assert.Equal(9_007_199_254_740_991, counter.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_BelowMinimum_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SignatureCounter.From(SignatureCounter.Min - 1));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_AboveMaximum_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SignatureCounter.From(SignatureCounter.Max + 1));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MaxValue_IsJavaScriptSafeInteger()
    {
        // Verify that max value is Number.MAX_SAFE_INTEGER from JavaScript
        Assert.Equal(9_007_199_254_740_991, SignatureCounter.Max);
    }

    // ============================================================================
    // Conversion Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueProperty_ReturnsValue()
    {
        SignatureCounter counter = SignatureCounter.From(12345);

        long value = counter.Value;

        Assert.Equal(12345, value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueProperty_WithMaxValue_WorksCorrectly()
    {
        SignatureCounter counter = SignatureCounter.From(SignatureCounter.Max);

        long value = counter.Value;

        Assert.Equal(SignatureCounter.Max, value);
    }

    // ============================================================================
    // ToString Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0, "0")]
    [InlineData(12345, "12345")]
    [InlineData(9_007_199_254_740_991, "9007199254740991")]
    public void ToString_ReturnsValueAsString(long value, string expected)
    {
        SignatureCounter counter = SignatureCounter.From(value);

        Assert.Equal(expected, counter.ToString());
    }

    // ============================================================================
    // Equality Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        SignatureCounter counter1 = SignatureCounter.From(12345);
        SignatureCounter counter2 = SignatureCounter.From(12345);

        Assert.Equal(counter1, counter2);
        Assert.True(counter1.Equals(counter2));
        Assert.Equal(counter1.GetHashCode(), counter2.GetHashCode());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        SignatureCounter counter1 = SignatureCounter.From(12345);
        SignatureCounter counter2 = SignatureCounter.From(67890);

        Assert.NotEqual(counter1, counter2);
        Assert.False(counter1.Equals(counter2));
    }

    // ============================================================================
    // IParsable Tests
    // ============================================================================

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "12345";

        // Act
        SignatureCounter result = SignatureCounter.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12345, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SignatureCounter.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => SignatureCounter.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => SignatureCounter.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNonNumericInput_ThrowsFormatException()
    {
        // Arrange
        string invalidInput = "abc";

        // Act & Assert
        Assert.Throws<FormatException>(() => SignatureCounter.Parse(invalidInput, null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "12345";

        // Act
        bool success = SignatureCounter.TryParse(validInput, null, out SignatureCounter result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(12345, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullInput_ReturnsFalse()
    {
        // Act
        bool success = SignatureCounter.TryParse(null, null, out SignatureCounter result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidInput_ReturnsFalse()
    {
        // Arrange
        string invalidInput = "";

        // Act
        bool success = SignatureCounter.TryParse(invalidInput, null, out SignatureCounter result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNonNumericInput_ReturnsFalse()
    {
        // Arrange
        string invalidInput = "abc";

        // Act
        bool success = SignatureCounter.TryParse(invalidInput, null, out SignatureCounter result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion

    // ============================================================================
    // JSON Serialization Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void JsonSerialization_Roundtrip_PreservesValue()
    {
        // Arrange
        SignatureCounter original = SignatureCounter.From(999999);

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(original);
        SignatureCounter deserialized = System.Text.Json.JsonSerializer.Deserialize<SignatureCounter>(json);

        // Assert
        Assert.Equal(original, deserialized);
        Assert.Equal(original.Value, deserialized.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12345)]
    [InlineData(9_007_199_254_740_991)]
    public void JsonSerialization_VariousValues_RoundtripCorrectly(long value)
    {
        // Arrange
        SignatureCounter original = SignatureCounter.From(value);

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(original);
        SignatureCounter deserialized = System.Text.Json.JsonSerializer.Deserialize<SignatureCounter>(json);

        // Assert
        Assert.Equal(original, deserialized);
        Assert.Equal(value, deserialized.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void JsonSerialization_MaxValue_RoundtripCorrectly()
    {
        // Arrange
        SignatureCounter original = SignatureCounter.From(SignatureCounter.Max);

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(original);
        SignatureCounter deserialized = System.Text.Json.JsonSerializer.Deserialize<SignatureCounter>(json);

        // Assert
        Assert.Equal(original, deserialized);
        Assert.Equal(SignatureCounter.Max, deserialized.Value);
    }
}
