using System.Globalization;
using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Serialization;

public class DecimalToStringJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public DecimalToStringJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new DecimalToStringJsonConverter() }
        };
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(10.98, "10.98")]
    [InlineData(0.5, "0.5")]
    [InlineData(100, "100")]
    [InlineData(1.12345, "1.12345")]
    public void Write_PositiveDecimals_ProducesCorrectString(double input, string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal($"\"{expected}\"", json);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(-10.98, "-10.98")]
    [InlineData(-0.5, "-0.5")]
    [InlineData(-2.75, "-2.75")]
    public void Write_NegativeDecimals_ProducesCorrectString(double input, string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal($"\"{expected}\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_Zero_ProducesZeroString()
    {
        // Arrange
        decimal value = 0m;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal("\"0\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_VerySmallDecimal_PreservesDecimals()
    {
        // Arrange
        decimal value = 0.00001m;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal("\"0.00001\"", json);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("10.98", 10.98)]
    [InlineData("0.5", 0.5)]
    [InlineData("100", 100)]
    [InlineData("1.12345", 1.12345)]
    public void Read_StringValues_ParsesCorrectly(string json, double expected)
    {
        // Arrange
        string jsonWithQuotes = $"\"{json}\"";

        // Act
        decimal result = JsonSerializer.Deserialize<decimal>(jsonWithQuotes, _options);

        // Assert
        Assert.Equal((decimal)expected, result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("-10.98", -10.98)]
    [InlineData("-0.5", -0.5)]
    [InlineData("-2.75", -2.75)]
    public void Read_NegativeStringValues_ParsesCorrectly(string json, double expected)
    {
        // Arrange
        string jsonWithQuotes = $"\"{json}\"";

        // Act
        decimal result = JsonSerializer.Deserialize<decimal>(jsonWithQuotes, _options);

        // Assert
        Assert.Equal((decimal)expected, result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(10.98)]
    [InlineData(0.5)]
    [InlineData(100)]
    public void Read_NumericValues_ParsesCorrectly(double input)
    {
        // Arrange
        string json = input.ToString(CultureInfo.InvariantCulture);
        decimal expected = (decimal)input;

        // Act
        decimal result = JsonSerializer.Deserialize<decimal>(json, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Read_EmptyString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"\"";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<decimal>(json, _options));

        Assert.NotNull(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Read_InvalidString_ThrowsJsonException()
    {
        // Arrange
        string json = "\"invalid\"";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<decimal>(json, _options));

        Assert.Contains("not a valid decimal", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Read_NullToken_ThrowsJsonException()
    {
        // Arrange
        string json = "null";

        // Act & Assert
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<decimal>(json, _options));

        Assert.NotNull(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_PositiveDecimal_PreservesValue()
    {
        // Arrange
        decimal original = 10.98765m;

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        decimal result = JsonSerializer.Deserialize<decimal>(json, _options);

        // Assert
        Assert.Equal(original, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_NegativeDecimal_PreservesValue()
    {
        // Arrange
        decimal original = -2.75m;

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        decimal result = JsonSerializer.Deserialize<decimal>(json, _options);

        // Assert
        Assert.Equal(original, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_DecimalValue_UsesInvariantCultureWithPeriodDecimalSeparator()
    {
        // Arrange
        decimal value = 1234.56m;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        // Should use "." as decimal separator, not "," (InvariantCulture formatting)
        Assert.Contains("1234.56", json);
        Assert.DoesNotContain(",", json);
    }

    // ========================================
    // DSFinV-K Fiscal Compliance Tests
    // ========================================

    /// <summary>
    /// DSFinV-K compliance tests verify that decimal values are serialized according to
    /// German fiscal regulations (DSFinV-K v2.3 specification).
    /// Maximum 5 decimal places are allowed, with trailing zeros removed for efficiency.
    /// </summary>

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_OneDecimalPlace_PreservesFormat()
    {
        // Arrange
        decimal value = 10.5m; // Exactly 1 decimal

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal("\"10.5\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_TwoDecimalPlacesWithTrailingZero_RemovesTrailingZeros()
    {
        // Arrange
        decimal value = 10.50m; // Exactly 2 decimals with trailing zero

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        // "0.#####" format removes trailing zeros (DSFinV-K compliant and more efficient)
        Assert.Equal("\"10.5\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_ThreeDecimalPlaces_PreservesFormat()
    {
        // Arrange
        decimal value = 10.123m; // Exactly 3 decimals

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal("\"10.123\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_FourDecimalPlaces_PreservesFormat()
    {
        // Arrange
        decimal value = 10.1234m; // Exactly 4 decimals

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal("\"10.1234\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_FiveDecimalPlaces_PreservesFormat()
    {
        // Arrange
        decimal value = 10.12345m; // Exactly 5 decimals (DSFinV-K maximum)

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal("\"10.12345\"", json);

        // Verify DSFinV-K compliance: max 5 decimal places
        string withoutQuotes = json.Trim('"');
        string decimalPart = withoutQuotes.Split('.')[1];
        Assert.True(decimalPart.Length <= 5,
            $"DSFinV-K requires max 5 decimals, got {decimalPart.Length}");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_FiveDecimalPlacesWithTrailingZeros_RemovesTrailingZeros()
    {
        // Arrange
        decimal value = 10.12300m; // 5 decimals with trailing zeros

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal("\"10.123\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_SixDecimalPlaces_RoundsToFiveDecimals_DSFinVKCompliant()
    {
        // Arrange
        decimal value = 1.123456m; // 6 decimals

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert - DSFinV-K compliance: max 5 decimal places enforced by "0.#####" format
        Assert.Equal("\"1.12346\"", json); // Explicitly verify rounded value (banker's rounding)

        // Verify decimal places count
        string withoutQuotes = json.Trim('"');
        string decimalPart = withoutQuotes.Split('.')[1];
        Assert.Equal(5, decimalPart.Length); // Exactly 5 decimals after rounding
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_SevenDecimalPlaces_RoundsToFiveDecimals_DSFinVKCompliant()
    {
        // Arrange
        decimal value = 1.1234567m; // 7 decimals

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert - DSFinV-K compliance: max 5 decimal places enforced
        Assert.Equal("\"1.12346\"", json); // Explicitly verify rounded value

        // Verify decimal places count
        string withoutQuotes = json.Trim('"');
        string decimalPart = withoutQuotes.Split('.')[1];
        Assert.Equal(5, decimalPart.Length); // Exactly 5 decimals after rounding
    }

    // ========================================
    // DSFinV-K v2.3 Compliance Tests (Pattern Validation)
    // ========================================

    /// <summary>
    /// Pattern validation tests ensure all serialized decimal values match the DSFinV-K v2.3
    /// regex pattern: ^-?\d+(\.\d{1,5})?$ (optional minus sign, digits, optional decimal with 1-5 places)
    /// </summary>

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(10.98)]
    [InlineData(0.5)]
    [InlineData(-2.75)]
    [InlineData(1.12345)]
    [InlineData(100)]
    [InlineData(1.123456)] // 6 decimals → rounds to 5
    [InlineData(0.1234567)] // 7 decimals → rounds to 5
    public void Write_AllValues_MatchDSFinVK_Pattern(double input)
    {
        // Arrange
        // DSFinV-K MENGE field pattern: ^-?\d+(\.\d{1,5})?$
        decimal value = (decimal)input;
        string pattern = @"^-?\d+(\.\d{1,5})?$";

        // Act
        string json = JsonSerializer.Serialize(value, _options);
        string withoutQuotes = json.Trim('"');

        // Assert - Verify regex compliance with DSFinV-K v2.3
        Assert.True(
            System.Text.RegularExpressions.Regex.IsMatch(withoutQuotes, pattern),
            $"Serialized value '{withoutQuotes}' does not match DSFinV-K v2.3 pattern: {pattern}");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_RandomDecimals_NeverExceedFiveDecimalPlaces()
    {
        // Arrange
        Random random = new Random(42); // Seed for reproducibility

        for (int i = 0; i < 100; i++)
        {
            // Generate random decimal with various scales
            decimal value = (decimal)(random.NextDouble() * 1000);

            // Act
            string json = JsonSerializer.Serialize(value, _options);
            string withoutQuotes = json.Trim('"');

            // Assert - Verify max 5 decimal places
            if (withoutQuotes.Contains('.'))
            {
                string decimalPart = withoutQuotes.Split('.')[1];
                Assert.True(decimalPart.Length <= 5,
                    $"Value {value} produced {decimalPart.Length} decimals: {withoutQuotes}");
            }
        }
    }

    // ========================================
    // Explicit Rounding Tests
    // ========================================

    /// <summary>
    /// Rounding tests verify that decimal values with more than 5 decimal places are correctly
    /// rounded to 5 places using .NET's standard rounding (round-half-away-from-zero).
    /// This ensures DSFinV-K compliance while maintaining precision.
    /// </summary>

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(1.123456, "1.12346")]  // 6 decimals → round up
    [InlineData(1.123446, "1.12345")]  // 6 decimals → round down
    [InlineData(1.1234567, "1.12346")] // 7 decimals → round up
    [InlineData(10.123456, "10.12346")] // larger number, 6 decimals
    [InlineData(0.123456, "0.12346")]   // < 1, 6 decimals
    [InlineData(99.999999, "100")]      // rounding causes integer
    public void Write_VariousPrecisions_RoundsToFiveDecimalsCorrectly(double input, string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal($"\"{expected}\"", json);
    }

    /// <summary>
    /// Tests midpoint rounding behavior (values ending in .5).
    /// .NET's ToString() with format specifier uses standard rounding (round-half-away-from-zero),
    /// not banker's rounding (which is used by Math.Round() by default).
    /// </summary>
    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(1.123455, "1.12346")]  // .5 at 6th position → rounds up
    [InlineData(1.123445, "1.12345")]  // .45 at 6th position → rounds up to .5
    [InlineData(2.555555, "2.55556")]  // multiple .5 cases → rounds up
    [InlineData(0.000005, "0.00001")]  // very small → rounds up
    [InlineData(10.123454999, "10.12345")] // just below .5 → rounds down
    public void Write_MidpointValues_UsesStandardRoundingNotBankersRounding(double input, string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal($"\"{expected}\"", json);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(-1.123456, "-1.12346")]
    [InlineData(-10.9876543, "-10.98765")]
    [InlineData(-0.123456, "-0.12346")]
    public void Write_NegativeDecimals_RoundsCorrectly(double input, string expected)
    {
        // Arrange
        decimal value = (decimal)input;

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Equal($"\"{expected}\"", json);
    }

    // ========================================
    // Boundary Value Tests
    // ========================================

    /// <summary>
    /// Boundary value tests ensure the converter handles extreme decimal values correctly,
    /// including decimal.MaxValue, decimal.MinValue, and very large numbers with precision.
    /// </summary>

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_DecimalMaxValue_HandlesCorrectly()
    {
        // Arrange
        decimal value = decimal.MaxValue; // 79,228,162,514,264,337,593,543,950,335

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("79228162514264337593543950335", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_DecimalMinValue_HandlesCorrectly()
    {
        // Arrange
        decimal value = decimal.MinValue; // -79,228,162,514,264,337,593,543,950,335

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("-79228162514264337593543950335", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Write_VeryLargeDecimalWithDecimals_HandlesCorrectly()
    {
        // Arrange
        decimal value = 123456789012345.12345m; // Large number with 5 decimals

        // Act
        string json = JsonSerializer.Serialize(value, _options);

        // Assert
        Assert.Contains("123456789012345.12345", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Read_MaxValue_ParsesCorrectly()
    {
        // Arrange
        string json = "\"79228162514264337593543950335\"";

        // Act
        decimal result = JsonSerializer.Deserialize<decimal>(json, _options);

        // Assert
        Assert.Equal(decimal.MaxValue, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Read_MinValue_ParsesCorrectly()
    {
        // Arrange
        string json = "\"-79228162514264337593543950335\"";

        // Act
        decimal result = JsonSerializer.Deserialize<decimal>(json, _options);

        // Assert
        Assert.Equal(decimal.MinValue, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_FiveDecimals_PreservesValue()
    {
        // Arrange
        decimal original = 123.12345m; // DSFinV-K compliant (5 decimals)

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        decimal result = JsonSerializer.Deserialize<decimal>(json, _options);

        // Assert
        Assert.Equal(original, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_ZeroDecimals_PreservesValue()
    {
        // Arrange
        decimal original = 100m; // No decimals

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        decimal result = JsonSerializer.Deserialize<decimal>(json, _options);

        // Assert
        Assert.Equal(original, result);
    }
}
