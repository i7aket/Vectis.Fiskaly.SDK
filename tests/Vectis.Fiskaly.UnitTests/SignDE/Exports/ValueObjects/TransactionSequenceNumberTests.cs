using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.ValueObjects;

public class TransactionSequenceNumberTests
{
    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9_007_199_254_740_991)]
    public void From_ValidValue_ReturnsNumber(long value)
    {
        TransactionSequenceNumber number = TransactionSequenceNumber.From(value);

        Assert.Equal(value, number.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(-1)]
    [InlineData(9_007_199_254_740_992)]
    [InlineData(long.MaxValue)]
    public void From_InvalidValue_ThrowsArgumentOutOfRangeException(long value)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TransactionSequenceNumber.From(value));

        Assert.Contains($"must be between {TransactionSequenceNumber.Min} and {TransactionSequenceNumber.Max}", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0, true)]
    [InlineData(9_007_199_254_740_991, true)]
    [InlineData(-1, false)]
    [InlineData(9_007_199_254_740_992, false)]
    public void TryFrom_VariousValues_ReturnsExpected(long value, bool expected)
    {
        bool result = TransactionSequenceNumber.TryFrom(value, out TransactionSequenceNumber number);

        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.Equal(value, number.Value);
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueProperty_ReturnsValue()
    {
        TransactionSequenceNumber number = TransactionSequenceNumber.From(12345);

        long value = number.Value;

        Assert.Equal(12345, value);
    }

    // ============================================================================
    // Boundary Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void From_MinimumValue_ReturnsNumber()
    {
        TransactionSequenceNumber number = TransactionSequenceNumber.From(TransactionSequenceNumber.Min);

        Assert.Equal(TransactionSequenceNumber.Min, number.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_MaximumValue_ReturnsNumber()
    {
        TransactionSequenceNumber number = TransactionSequenceNumber.From(TransactionSequenceNumber.Max);

        Assert.Equal(TransactionSequenceNumber.Max, number.Value);
        Assert.Equal(9_007_199_254_740_991, number.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_BelowMinimum_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TransactionSequenceNumber.From(TransactionSequenceNumber.Min - 1));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_AboveMaximum_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TransactionSequenceNumber.From(TransactionSequenceNumber.Max + 1));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MaxValue_IsJavaScriptSafeInteger()
    {
        // Verify that max value is Number.MAX_SAFE_INTEGER from JavaScript
        Assert.Equal(9_007_199_254_740_991, TransactionSequenceNumber.Max);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueProperty_WithMaxValue_WorksCorrectly()
    {
        TransactionSequenceNumber number = TransactionSequenceNumber.From(TransactionSequenceNumber.Max);

        long value = number.Value;

        Assert.Equal(TransactionSequenceNumber.Max, value);
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
        TransactionSequenceNumber number = TransactionSequenceNumber.From(value);

        Assert.Equal(expected, number.ToString());
    }

    // ============================================================================
    // Equality Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        TransactionSequenceNumber number1 = TransactionSequenceNumber.From(12345);
        TransactionSequenceNumber number2 = TransactionSequenceNumber.From(12345);

        Assert.Equal(number1, number2);
        Assert.True(number1.Equals(number2));
        Assert.Equal(number1.GetHashCode(), number2.GetHashCode());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        TransactionSequenceNumber number1 = TransactionSequenceNumber.From(12345);
        TransactionSequenceNumber number2 = TransactionSequenceNumber.From(67890);

        Assert.NotEqual(number1, number2);
        Assert.False(number1.Equals(number2));
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(-1000)]
    public void TryFrom_NegativeValues_ReturnsFalse(long value)
    {
        bool result = TransactionSequenceNumber.TryFrom(value, out TransactionSequenceNumber number);

        Assert.False(result);
        Assert.Equal(0, number.Value);
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
        TransactionSequenceNumber result = TransactionSequenceNumber.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(12345, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TransactionSequenceNumber.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => TransactionSequenceNumber.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => TransactionSequenceNumber.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNonNumericInput_ThrowsFormatException()
    {
        // Arrange
        string invalidInput = "abc";

        // Act & Assert
        Assert.Throws<FormatException>(() => TransactionSequenceNumber.Parse(invalidInput, null));
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
        bool success = TransactionSequenceNumber.TryParse(validInput, null, out TransactionSequenceNumber result);

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
        bool success = TransactionSequenceNumber.TryParse(null, null, out TransactionSequenceNumber result);

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
        bool success = TransactionSequenceNumber.TryParse(invalidInput, null, out TransactionSequenceNumber result);

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
        bool success = TransactionSequenceNumber.TryParse(invalidInput, null, out TransactionSequenceNumber result);

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
        TransactionSequenceNumber original = TransactionSequenceNumber.From(888888);

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(original);
        TransactionSequenceNumber deserialized = System.Text.Json.JsonSerializer.Deserialize<TransactionSequenceNumber>(json);

        // Assert
        Assert.Equal(original, deserialized);
        Assert.Equal(original.Value, deserialized.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(54321)]
    [InlineData(9_007_199_254_740_991)]
    public void JsonSerialization_VariousValues_RoundtripCorrectly(long value)
    {
        // Arrange
        TransactionSequenceNumber original = TransactionSequenceNumber.From(value);

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(original);
        TransactionSequenceNumber deserialized = System.Text.Json.JsonSerializer.Deserialize<TransactionSequenceNumber>(json);

        // Assert
        Assert.Equal(original, deserialized);
        Assert.Equal(value, deserialized.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void JsonSerialization_MaxValue_RoundtripCorrectly()
    {
        // Arrange
        TransactionSequenceNumber original = TransactionSequenceNumber.From(TransactionSequenceNumber.Max);

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(original);
        TransactionSequenceNumber deserialized = System.Text.Json.JsonSerializer.Deserialize<TransactionSequenceNumber>(json);

        // Assert
        Assert.Equal(original, deserialized);
        Assert.Equal(TransactionSequenceNumber.Max, deserialized.Value);
    }
}
