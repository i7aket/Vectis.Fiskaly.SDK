using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Requests;

public class LineItemTests
{
    private readonly JsonSerializerOptions _options;

    public LineItemTests()
    {
        _options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(),
                new DecimalToStringJsonConverter(),
                new MoneyAmountJsonConverter()
            }
        };
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithValidData_Succeeds()
    {
        // Arrange & Act
        LineItem lineItem = new LineItem
        {
            Quantity = 2.5m,
            Text = "Pizza Margherita",
            PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR)
        };

        // Assert
        Assert.Equal(2.5m, lineItem.Quantity);
        Assert.Equal("Pizza Margherita", lineItem.Text);
        Assert.Equal(8.90m, lineItem.PricePerUnit.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Text_WithValidLength_Succeeds()
    {
        // Arrange
        string text = new string('A', 255);

        // Act
        LineItem lineItem = new LineItem
        {
            Quantity = 1m,
            Text = text,
            PricePerUnit = MoneyAmount.Create(10.00m, CurrencyCode.EUR)
        };

        // Assert
        Assert.Equal(255, lineItem.Text.Length);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Text_ExceedsMaxLength_ThrowsArgumentException()
    {
        // Arrange
        string text = new string('A', 256);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new LineItem
        {
            Quantity = 1m,
            Text = text,
            PricePerUnit = MoneyAmount.Create(10.00m, CurrencyCode.EUR)
        });

        Assert.Contains("255 characters", exception.Message);
        Assert.Contains("256 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Text_EmptyString_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new LineItem
        {
            Quantity = 1m,
            Text = "",
            PricePerUnit = MoneyAmount.Create(10.00m, CurrencyCode.EUR)
        });

        Assert.Contains("null or whitespace", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Text_NullValue_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new LineItem
        {
            Quantity = 1m,
            Text = null!,
            PricePerUnit = MoneyAmount.Create(10.00m, CurrencyCode.EUR)
        });

        Assert.Contains("null or whitespace", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithValidData_ProducesCorrectJson()
    {
        // Arrange
        LineItem lineItem = new LineItem
        {
            Quantity = 10.98m,
            Text = "Eisbecher \"Himbeere\"",
            PricePerUnit = MoneyAmount.Create(20.25m, CurrencyCode.EUR)
        };

        // Act
        string json = JsonSerializer.Serialize(lineItem, _options);

        // Assert
        Assert.Contains("\"quantity\":\"10.98\"", json);
        Assert.Contains("\"text\":", json);
        Assert.Contains("Eisbecher", json);
        Assert.Contains("Himbeere", json);
        Assert.Contains("\"price_per_unit\":\"20.25\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithFractionalQuantity_PreservesDecimals()
    {
        // Arrange
        LineItem lineItem = new LineItem
        {
            Quantity = 0.5m,
            Text = "Half portion",
            PricePerUnit = MoneyAmount.Create(5.00m, CurrencyCode.EUR)
        };

        // Act
        string json = JsonSerializer.Serialize(lineItem, _options);

        // Assert
        Assert.Contains("\"quantity\":\"0.5\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithNegativeQuantity_IncludesMinusSign()
    {
        // Arrange
        LineItem lineItem = new LineItem
        {
            Quantity = -2.75m,
            Text = "Return item",
            PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR)
        };

        // Act
        string json = JsonSerializer.Serialize(lineItem, _options);

        // Assert
        Assert.Contains("\"quantity\":\"-2.75\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_FromValidJson_Succeeds()
    {
        // Arrange
        string json = """
                      {
                          "quantity": "10.98",
                          "text": "Pizza Margherita",
                          "price_per_unit": "8.90"
                      }
                      """;

        // Act
        LineItem? lineItem = JsonSerializer.Deserialize<LineItem>(json, _options);

        // Assert
        Assert.NotNull(lineItem);
        Assert.Equal(10.98m, lineItem.Quantity);
        Assert.Equal("Pizza Margherita", lineItem.Text);
        Assert.Equal(8.90m, lineItem.PricePerUnit.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_QuantityAsNumber_Succeeds()
    {
        // Arrange
        string json = """
                      {
                          "quantity": 10.98,
                          "text": "Test",
                          "price_per_unit": "5.00"
                      }
                      """;

        // Act
        LineItem? lineItem = JsonSerializer.Deserialize<LineItem>(json, _options);

        // Assert
        Assert.NotNull(lineItem);
        Assert.Equal(10.98m, lineItem.Quantity);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithMaxDecimals_PreservesAllDigits()
    {
        // Arrange
        LineItem lineItem = new LineItem
        {
            Quantity = 1.12345m, // 5 decimals (max allowed)
            Text = "Precision test",
            PricePerUnit = MoneyAmount.Create(9.99999m, CurrencyCode.EUR)
        };

        // Act
        string json = JsonSerializer.Serialize(lineItem, _options);

        // Assert
        Assert.Contains("\"quantity\":\"1.12345\"", json);
    }
}
