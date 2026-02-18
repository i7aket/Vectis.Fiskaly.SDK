using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.ValueObjects;

public class ExportLimitTests
{
    // ============================================================================
    // Factory Method Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(1_000_000)]
    public void From_ValidValue_ReturnsLimit(int value)
    {
        ExportLimit limit = ExportLimit.From(value);

        Assert.Equal(value, limit.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    [InlineData(int.MaxValue)]
    public void From_InvalidValue_ThrowsArgumentOutOfRangeException(int value)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExportLimit.From(value));

        Assert.Contains($"must be between {ExportLimit.Min} and {ExportLimit.Max}", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(1, true)]
    [InlineData(1_000_000, true)]
    [InlineData(0, false)]
    [InlineData(1_000_001, false)]
    public void TryFrom_VariousValues_ReturnsExpected(int value, bool expected)
    {
        bool result = ExportLimit.TryFrom(value, out ExportLimit limit);

        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.Equal(value, limit.Value);
        }
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void TryFrom_NegativeValues_ReturnsFalse(int value)
    {
        bool result = ExportLimit.TryFrom(value, out ExportLimit limit);

        Assert.False(result);
        Assert.Equal(0, limit.Value);
    }

    // ============================================================================
    // Boundary Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void From_MinimumValue_ReturnsLimit()
    {
        ExportLimit limit = ExportLimit.From(ExportLimit.Min);

        Assert.Equal(ExportLimit.Min, limit.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_MaximumValue_ReturnsLimit()
    {
        ExportLimit limit = ExportLimit.From(ExportLimit.Max);

        Assert.Equal(ExportLimit.Max, limit.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_BelowMinimum_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExportLimit.From(ExportLimit.Min - 1));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_AboveMaximum_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExportLimit.From(ExportLimit.Max + 1));
    }

    // ============================================================================
    // Conversion Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueProperty_ReturnsValue()
    {
        ExportLimit limit = ExportLimit.From(500);

        int value = limit.Value;

        Assert.Equal(500, value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueProperty_WithMaxValue_WorksCorrectly()
    {
        ExportLimit limit = ExportLimit.From(ExportLimit.Max);

        int value = limit.Value;

        Assert.Equal(ExportLimit.Max, value);
    }

    // ============================================================================
    // ToString Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(1, "1")]
    [InlineData(1000, "1000")]
    [InlineData(1_000_000, "1000000")]
    public void ToString_ReturnsValueAsString(int value, string expected)
    {
        ExportLimit limit = ExportLimit.From(value);

        Assert.Equal(expected, limit.ToString());
    }

    // ============================================================================
    // Equality Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        ExportLimit limit1 = ExportLimit.From(1000);
        ExportLimit limit2 = ExportLimit.From(1000);

        Assert.Equal(limit1, limit2);
        Assert.True(limit1.Equals(limit2));
        Assert.Equal(limit1.GetHashCode(), limit2.GetHashCode());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        ExportLimit limit1 = ExportLimit.From(1000);
        ExportLimit limit2 = ExportLimit.From(2000);

        Assert.NotEqual(limit1, limit2);
        Assert.False(limit1.Equals(limit2));
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
        string validInput = "1000";

        // Act
        ExportLimit result = ExportLimit.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ExportLimit.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => ExportLimit.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => ExportLimit.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNonNumericInput_ThrowsFormatException()
    {
        // Arrange
        string invalidInput = "abc";

        // Act & Assert
        Assert.Throws<FormatException>(() => ExportLimit.Parse(invalidInput, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithOutOfRangeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        string invalidInput = "2000000"; // Exceeds max

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ExportLimit.Parse(invalidInput, null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "1000";

        // Act
        bool success = ExportLimit.TryParse(validInput, null, out ExportLimit result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(1000, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullInput_ReturnsFalse()
    {
        // Act
        bool success = ExportLimit.TryParse(null, null, out ExportLimit result);

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
        bool success = ExportLimit.TryParse(invalidInput, null, out ExportLimit result);

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
        bool success = ExportLimit.TryParse(invalidInput, null, out ExportLimit result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithOutOfRangeValue_ReturnsFalse()
    {
        // Arrange
        string invalidInput = "2000000"; // Exceeds max

        // Act
        bool success = ExportLimit.TryParse(invalidInput, null, out ExportLimit result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
