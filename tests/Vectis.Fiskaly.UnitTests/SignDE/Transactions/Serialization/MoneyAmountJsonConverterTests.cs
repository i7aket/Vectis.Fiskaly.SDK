using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Serialization;

public class MoneyAmountJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public MoneyAmountJsonConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // Match production setup from ServiceCollectionExtensions
        };
        _options.Converters.Add(new MoneyAmountJsonConverter());
    }

    // ============================================================================
    // Deserialize String Input Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"12.34\"", 12.34)]
    [InlineData("\"0.00\"", 0.00)]
    [InlineData("\"999999.99\"", 999999.99)]
    [InlineData("\"-12.34\"", -12.34)]
    [InlineData("\"1.00\"", 1.00)]
    public void Deserialize_ValidString_ReturnsMoneyAmount(string json, decimal expectedValue)
    {
        MoneyAmount result = JsonSerializer.Deserialize<MoneyAmount>(json, _options);

        Assert.Equal(expectedValue, result.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public void Deserialize_EmptyOrWhitespaceString_ThrowsJsonException(string json)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MoneyAmount>(json, _options));

        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("\"12.34.56\"")]
    [InlineData("\"not a number\"")]
    public void Deserialize_InvalidFormatString_ThrowsJsonException(string json)
    {
        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MoneyAmount>(json, _options));

        Assert.Contains("not a valid", exception.Message);
    }

    // ============================================================================
    // Deserialize Number Input Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(12.34, 12.34)]
    [InlineData(0.00, 0.00)]
    [InlineData(100.50, 100.50)]
    [InlineData(-12.34, -12.34)]
    public void Deserialize_ValidNumber_ReturnsMoneyAmount(decimal value, decimal expectedValue)
    {
        string json = JsonSerializer.Serialize(value);

        MoneyAmount result = JsonSerializer.Deserialize<MoneyAmount>(json, _options);

        Assert.Equal(expectedValue, result.Value);
    }

    // ============================================================================
    // Deserialize Invalid Token Type Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_BooleanToken_ThrowsJsonException()
    {
        string json = "true";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MoneyAmount>(json, _options));

        Assert.Contains("Unexpected token", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ArrayToken_ThrowsJsonException()
    {
        string json = "[1, 2, 3]";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MoneyAmount>(json, _options));

        Assert.Contains("Unexpected token", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ObjectToken_ThrowsJsonException()
    {
        string json = "{}";

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<MoneyAmount>(json, _options));

        Assert.Contains("Unexpected token", exception.Message);
    }

    // ============================================================================
    // Serialize MoneyAmount Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(12.34, "\"12.34\"")]
    [InlineData(0.00, "\"0.00\"")]
    [InlineData(100.50, "\"100.50\"")]
    [InlineData(-12.34, "\"-12.34\"")]
    [InlineData(1.00, "\"1.00\"")]
    public void Serialize_MoneyAmount_ReturnsInvariantString(decimal value, string expected)
    {
        MoneyAmount amount = MoneyAmount.Create(value, CurrencyCode.EUR);

        string json = JsonSerializer.Serialize(amount, _options);

        Assert.Equal(expected, json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_ZeroAmount_ReturnsFormattedZero()
    {
        MoneyAmount amount = MoneyAmount.Create(0, CurrencyCode.EUR);

        string json = JsonSerializer.Serialize(amount, _options);

        Assert.Equal("\"0.00\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_NegativeAmount_ReturnsNegativeString()
    {
        MoneyAmount amount = MoneyAmount.Create(-50.00m, CurrencyCode.EUR);

        string json = JsonSerializer.Serialize(amount, _options);

        Assert.Equal("\"-50.00\"", json);
    }

    // ============================================================================
    // Round-trip Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"12.34\"")]
    [InlineData("\"0.00\"")]
    [InlineData("\"100.50\"")]
    [InlineData("\"-12.34\"")]
    public void RoundTrip_StringInput_PreservesValue(string originalJson)
    {
        MoneyAmount deserialized = JsonSerializer.Deserialize<MoneyAmount>(originalJson, _options);
        string serialized = JsonSerializer.Serialize(deserialized, _options);

        Assert.Equal(originalJson, serialized);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(12.34, "\"12.34\"")]
    [InlineData(0.00, "\"0.00\"")]
    [InlineData(100.50, "\"100.50\"")]
    [InlineData(-12.34, "\"-12.34\"")]
    public void RoundTrip_NumberInput_ConvertsToString(decimal numberInput, string expectedOutput)
    {
        string numberJson = JsonSerializer.Serialize(numberInput);
        MoneyAmount deserialized = JsonSerializer.Deserialize<MoneyAmount>(numberJson, _options);
        string serialized = JsonSerializer.Serialize(deserialized, _options);

        Assert.Equal(expectedOutput, serialized);
    }

    // ============================================================================
    // Nested Property Deserialization Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_ComplexObject_WorksCorrectly()
    {
        // Arrange - JSON uses lowercase property names (tests PropertyNameCaseInsensitive)
        string json = "{\"amount\":\"12.34\",\"description\":\"test transaction\"}";

        // Act - Deserialize
        TestObject? result = JsonSerializer.Deserialize<TestObject>(json, _options);

        // Assert - Nested MoneyAmount property should be deserialized correctly
        Assert.NotNull(result);
        Assert.Equal(12.34m, result.Amount.Value);
        Assert.Equal(CurrencyCode.EUR, result.Amount.Currency);
        Assert.Equal("test transaction", result.Description);

        // Act - Serialize back
        string roundTrip = JsonSerializer.Serialize(result, _options);

        // Assert - Should serialize MoneyAmount as string
        Assert.Contains("\"12.34\"", roundTrip);
        Assert.Contains("\"test transaction\"", roundTrip);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("{\"amount\":100.50,\"description\":\"number input\"}", 100.50)]
    [InlineData("{\"amount\":\"-25.00\",\"description\":\"negative\"}", -25.00)]
    [InlineData("{\"amount\":\"0.00\",\"description\":\"zero\"}", 0.00)]
    public void Deserialize_ComplexObjectWithDifferentAmounts_DeserializesCorrectly(string json, decimal expectedAmount)
    {
        TestObject? result = JsonSerializer.Deserialize<TestObject>(json, _options);

        Assert.NotNull(result);
        Assert.Equal(expectedAmount, result.Amount.Value);
    }

    // ============================================================================
    // Precision Preservation Tests (2-5 Decimals per OpenAPI spec)
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"12.34\"", 12.34)]     // 2 decimals - preserved
    [InlineData("\"12.345\"", 12.345)]   // 3 decimals - preserved
    [InlineData("\"12.3456\"", 12.3456)] // 4 decimals - preserved
    [InlineData("\"12.34567\"", 12.34567)] // 5 decimals - preserved (max)
    [InlineData("\"10.1\"", 10.10)]      // Pads to 2 decimals
    [InlineData("\"0.123\"", 0.123)]     // 3 decimals
    public void Deserialize_StringWithTwoToFiveDecimals_PreservesPrecision(string json, decimal expected)
    {
        MoneyAmount result = JsonSerializer.Deserialize<MoneyAmount>(json, _options);

        Assert.Equal(expected, result.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"12.345678\"", 12.34568)]   // 6 decimals → rounds to 5 (banker's rounding)
    [InlineData("\"12.345672\"", 12.34567)]   // 6 decimals → rounds to 5 (banker's rounding)
    [InlineData("\"99.999999\"", 100.00000)]  // 6 decimals → rounds to 5
    [InlineData("\"12.3456789012\"", 12.34568)] // 10 decimals → rounds to 5
    public void Deserialize_StringWithMoreThanFiveDecimals_RoundsToFive(string json, decimal expected)
    {
        MoneyAmount result = JsonSerializer.Deserialize<MoneyAmount>(json, _options);

        Assert.Equal(expected, result.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(12.34, 12.34)]       // 2 decimals
    [InlineData(12.345, 12.345)]     // 3 decimals
    [InlineData(12.3456, 12.3456)]   // 4 decimals
    [InlineData(12.34567, 12.34567)] // 5 decimals
    public void Deserialize_NumberWithTwoToFiveDecimals_PreservesPrecision(decimal input, decimal expected)
    {
        string json = JsonSerializer.Serialize(input);
        MoneyAmount result = JsonSerializer.Deserialize<MoneyAmount>(json, _options);

        Assert.Equal(expected, result.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"12.345\"", "\"12.345\"")]     // 3 decimals preserved in output
    [InlineData("\"12.34567\"", "\"12.34567\"")] // 5 decimals preserved in output
    [InlineData("\"10.1\"", "\"10.10\"")]        // Pads to minimum 2 decimals
    [InlineData("\"100.5\"", "\"100.50\"")]      // Pads to minimum 2 decimals
    public void Serialize_MoneyAmountWithVariableDecimals_PreservesDecimalCount(string inputJson, string expectedJson)
    {
        MoneyAmount amount = JsonSerializer.Deserialize<MoneyAmount>(inputJson, _options);
        string serialized = JsonSerializer.Serialize(amount, _options);

        Assert.Equal(expectedJson, serialized);
    }

    // ============================================================================
    // Large Number Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"999999.99\"", 999999.99)]
    [InlineData("\"1000000.00\"", 1000000.00)]
    [InlineData("\"9999999999.99\"", 9999999999.99)]
    [InlineData("\"-999999.99\"", -999999.99)]
    public void Deserialize_VeryLargeNumber_ReturnsMoneyAmount(string json, decimal expected)
    {
        MoneyAmount result = JsonSerializer.Deserialize<MoneyAmount>(json, _options);

        Assert.Equal(expected, result.Value);
        Assert.Equal(CurrencyCode.EUR, result.Currency);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MaxDecimalValue_HandlesCorrectly()
    {
        // Test with a very large but valid decimal
        decimal largeAmount = 79228162514264337593543950335m; // decimal.MaxValue
        string json = JsonSerializer.Serialize(largeAmount);

        MoneyAmount result = JsonSerializer.Deserialize<MoneyAmount>(json, _options);

        // Should round to 5 decimals (max precision per OpenAPI spec)
        Assert.Equal(decimal.Round(largeAmount, 5, MidpointRounding.ToEven), result.Value);
    }

    // ============================================================================
    // Test Helper Classes
    // ============================================================================

    private class TestObject
    {
        public MoneyAmount Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
