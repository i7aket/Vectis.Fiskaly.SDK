using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Requests;

public class OrderTests
{
    private readonly JsonSerializerOptions _options;

    public OrderTests()
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
    public void Constructor_WithSingleLineItem_Succeeds()
    {
        // Arrange & Act
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 2m, Text = "Pizza", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) }
            }
        };

        // Assert
        Assert.NotNull(order.LineItems);
        Assert.Single(order.LineItems);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMultipleLineItems_Succeeds()
    {
        // Arrange & Act
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 2m, Text = "Pizza Margherita", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) },
                new() { Quantity = 1m, Text = "Cola 0.5L", PricePerUnit = MoneyAmount.Create(2.50m, CurrencyCode.EUR) },
                new() { Quantity = 0.5m, Text = "Beer 0.5L", PricePerUnit = MoneyAmount.Create(3.50m, CurrencyCode.EUR) }
            }
        };

        // Assert
        Assert.Equal(3, order.LineItems.Count);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithSingleLineItem_ProducesCorrectJson()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 2m, Text = "Pizza", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) }
            }
        };

        // Act
        string json = JsonSerializer.Serialize(order, _options);

        // Assert
        Assert.Contains("\"line_items\"", json);
        Assert.Contains("\"quantity\":\"2\"", json);
        Assert.Contains("\"text\":\"Pizza\"", json);
        Assert.Contains("\"price_per_unit\":\"8.90\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithMultipleLineItems_ProducesCorrectJson()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 2m, Text = "Pizza", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) },
                new() { Quantity = 1m, Text = "Cola", PricePerUnit = MoneyAmount.Create(2.50m, CurrencyCode.EUR) }
            }
        };

        // Act
        string json = JsonSerializer.Serialize(order, _options);

        // Assert
        Assert.Contains("\"Pizza\"", json);
        Assert.Contains("\"Cola\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithFractionalQuantities_PreservesDecimals()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 0.5m, Text = "Half portion", PricePerUnit = MoneyAmount.Create(4.50m, CurrencyCode.EUR) },
                new() { Quantity = 2.75m, Text = "Ice cream scoops", PricePerUnit = MoneyAmount.Create(1.20m, CurrencyCode.EUR) }
            }
        };

        // Act
        string json = JsonSerializer.Serialize(order, _options);

        // Assert
        Assert.Contains("\"quantity\":\"0.5\"", json);
        Assert.Contains("\"quantity\":\"2.75\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithNegativeQuantities_IncludesMinusSign()
    {
        // Arrange (Storno order)
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = -2m, Text = "Return: Pizza", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) }
            }
        };

        // Act
        string json = JsonSerializer.Serialize(order, _options);

        // Assert
        Assert.Contains("\"quantity\":\"-2\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_FromValidJson_Succeeds()
    {
        // Arrange
        string json = """
                      {
                          "line_items": [
                              {
                                  "quantity": "10.98",
                                  "text": "Pizza Margherita",
                                  "price_per_unit": "8.90"
                              },
                              {
                                  "quantity": "1",
                                  "text": "Cola 0.5L",
                                  "price_per_unit": "2.50"
                              }
                          ]
                      }
                      """;

        // Act
        Order? order = JsonSerializer.Deserialize<Order>(json, _options);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(2, order.LineItems.Count);
        Assert.Equal(10.98m, order.LineItems[0].Quantity);
        Assert.Equal("Pizza Margherita", order.LineItems[0].Text);
        Assert.Equal(1m, order.LineItems[1].Quantity);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_EmptyLineItems_Succeeds()
    {
        // Arrange
        string json = """
                      {
                          "line_items": []
                      }
                      """;

        // Act
        Order? order = JsonSerializer.Deserialize<Order>(json, _options);

        // Assert
        Assert.NotNull(order);
        Assert.Empty(order.LineItems);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_Deserialize_RoundTrip_Succeeds()
    {
        // Arrange
        Order originalOrder = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 2.5m, Text = "Test Item", PricePerUnit = MoneyAmount.Create(10.99m, CurrencyCode.EUR) }
            }
        };

        // Act
        string json = JsonSerializer.Serialize(originalOrder, _options);
        Order? deserializedOrder = JsonSerializer.Deserialize<Order>(json, _options);

        // Assert
        Assert.NotNull(deserializedOrder);
        Assert.Equal(originalOrder.LineItems.Count, deserializedOrder.LineItems.Count);
        Assert.Equal(originalOrder.LineItems[0].Quantity, deserializedOrder.LineItems[0].Quantity);
        Assert.Equal(originalOrder.LineItems[0].Text, deserializedOrder.LineItems[0].Text);
    }
}
