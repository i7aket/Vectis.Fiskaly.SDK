using Vectis.Fiskaly.SDK.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Common;

/// <summary>
/// Unit tests for the Guard class validation methods.
/// </summary>
/// <remarks>
/// <para><strong>Test Coverage</strong>:</para>
/// Tests all 4 Guard.Against methods with:
/// <list type="bullet">
///   <item><description>Null argument checks</description></item>
///   <item><description>Valid data (should not throw)</description></item>
///   <item><description>Invalid data (should throw with descriptive message)</description></item>
///   <item><description>Edge cases (zero amounts/quantities)</description></item>
/// </list>
///
/// <para><strong>Test Naming Convention</strong>:</para>
/// <c>MethodName_Scenario_ExpectedBehavior</c>
/// </remarks>
public class GuardTests
{
    #region PositiveAmountsInStornoReceipt Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveAmountsInStornoReceipt_WithNullReceipt_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Guard.Against.PositiveAmountsInStornoReceipt(null!, "receipt"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveAmountsInStornoReceipt_WithAllNegativeAmounts_DoesNotThrow()
    {
        // Arrange
        Receipt receipt = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-100.00m, CurrencyCode.EUR) },
                new() { VatRate = VatRate.Reduced1, Amount = MoneyAmount.Create(-50.00m, CurrencyCode.EUR) }
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>
            {
                new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(-100.00m, CurrencyCode.EUR) },
                new() { PaymentType = PaymentType.NonCash, Amount = MoneyAmount.Create(-50.00m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert - Should not throw
        Exception? exception = Record.Exception(() =>
            Guard.Against.PositiveAmountsInStornoReceipt(receipt, nameof(receipt)));

        Assert.Null(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveAmountsInStornoReceipt_WithPositiveVatAmount_ThrowsArgumentException()
    {
        // Arrange
        Receipt receipt = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(100.00m, CurrencyCode.EUR) } // Positive!
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>
            {
                new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(-100.00m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.PositiveAmountsInStornoReceipt(receipt, "receipt", "FinishReceiptTransactionRequest"));

        Assert.Contains("Negative amounts required for storno receipt", exception.Message);
        Assert.Contains("VAT rate", exception.Message);
        Assert.Contains("FinishReceiptTransactionRequest", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveAmountsInStornoReceipt_WithZeroVatAmount_ThrowsArgumentException()
    {
        // Arrange
        Receipt receipt = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(0.00m, CurrencyCode.EUR) } // Zero!
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>
            {
                new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(-100.00m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.PositiveAmountsInStornoReceipt(receipt, "receipt"));

        Assert.Contains("Negative amounts required", exception.Message);
        Assert.Contains("positive/zero amount", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveAmountsInStornoReceipt_WithPositivePaymentAmount_ThrowsArgumentException()
    {
        // Arrange
        Receipt receipt = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-100.00m, CurrencyCode.EUR) }
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>
            {
                new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(50.00m, CurrencyCode.EUR) } // Positive!
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.PositiveAmountsInStornoReceipt(receipt, "receipt", "FinishReceiptTransactionRequest"));

        Assert.Contains("Negative amounts required for storno receipt", exception.Message);
        Assert.Contains("payment type", exception.Message);
        Assert.Contains("FinishReceiptTransactionRequest", exception.Message);
    }

    #endregion

    #region PositiveQuantitiesInStornoOrder Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveQuantitiesInStornoOrder_WithNullOrder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Guard.Against.PositiveQuantitiesInStornoOrder(null!, "order"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveQuantitiesInStornoOrder_WithAllNegativeQuantities_DoesNotThrow()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = -2m, Text = "Pizza Margherita", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) },
                new() { Quantity = -1m, Text = "Cola 0.5L", PricePerUnit = MoneyAmount.Create(2.50m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert - Should not throw
        Exception? exception = Record.Exception(() =>
            Guard.Against.PositiveQuantitiesInStornoOrder(order, nameof(order)));

        Assert.Null(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveQuantitiesInStornoOrder_WithPositiveQuantity_ThrowsArgumentException()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 2m, Text = "Pizza", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) } // Positive!
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.PositiveQuantitiesInStornoOrder(order, "order", "FinishOrderTransactionRequest"));

        Assert.Contains("Negative quantities required for storno order", exception.Message);
        Assert.Contains("positive/zero quantity 2", exception.Message);
        Assert.Contains("Pizza", exception.Message);
        Assert.Contains("FinishOrderTransactionRequest", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveQuantitiesInStornoOrder_WithZeroQuantity_ThrowsArgumentException()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 0m, Text = "Test Item", PricePerUnit = MoneyAmount.Create(5.00m, CurrencyCode.EUR) } // Zero!
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.PositiveQuantitiesInStornoOrder(order, "order"));

        Assert.Contains("Negative quantities required", exception.Message);
        Assert.Contains("positive/zero quantity 0", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PositiveQuantitiesInStornoOrder_WithMixedQuantities_ThrowsOnFirstPositive()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = -1m, Text = "Valid Item", PricePerUnit = MoneyAmount.Create(5.00m, CurrencyCode.EUR) },
                new() { Quantity = 2m, Text = "Invalid Item", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) } // Positive!
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.PositiveQuantitiesInStornoOrder(order, "order"));

        Assert.Contains("Invalid Item", exception.Message); // Should fail on the positive one
    }

    #endregion

    #region NegativeAmountsInNormalReceipt Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeAmountsInNormalReceipt_WithNullReceipt_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Guard.Against.NegativeAmountsInNormalReceipt(null!, "receipt"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeAmountsInNormalReceipt_WithAllPositiveAmounts_DoesNotThrow()
    {
        // Arrange
        Receipt receipt = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(100.00m, CurrencyCode.EUR) },
                new() { VatRate = VatRate.Reduced1, Amount = MoneyAmount.Create(50.00m, CurrencyCode.EUR) }
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>
            {
                new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(100.00m, CurrencyCode.EUR) },
                new() { PaymentType = PaymentType.NonCash, Amount = MoneyAmount.Create(50.00m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert - Should not throw
        Exception? exception = Record.Exception(() =>
            Guard.Against.NegativeAmountsInNormalReceipt(receipt, nameof(receipt)));

        Assert.Null(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeAmountsInNormalReceipt_WithNegativeVatAmount_ThrowsArgumentException()
    {
        // Arrange
        Receipt receipt = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-100.00m, CurrencyCode.EUR) } // Negative!
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>
            {
                new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(100.00m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.NegativeAmountsInNormalReceipt(receipt, "receipt", "FinishStornoReceiptTransactionRequest"));

        Assert.Contains("Positive amounts required for normal receipt", exception.Message);
        Assert.Contains("VAT rate", exception.Message);
        Assert.Contains("FinishStornoReceiptTransactionRequest", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeAmountsInNormalReceipt_WithNegativePaymentAmount_ThrowsArgumentException()
    {
        // Arrange
        Receipt receipt = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(100.00m, CurrencyCode.EUR) }
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>
            {
                new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(-50.00m, CurrencyCode.EUR) } // Negative!
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.NegativeAmountsInNormalReceipt(receipt, "receipt", "FinishStornoReceiptTransactionRequest"));

        Assert.Contains("Positive amounts required for normal receipt", exception.Message);
        Assert.Contains("payment type", exception.Message);
        Assert.Contains("FinishStornoReceiptTransactionRequest", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeAmountsInNormalReceipt_WithZeroAmounts_DoesNotThrow()
    {
        // Arrange - Zero is allowed for normal receipts (not negative)
        Receipt receipt = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(0.00m, CurrencyCode.EUR) }
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>
            {
                new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(0.00m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert - Should not throw (zero >= 0, only negative fails)
        Exception? exception = Record.Exception(() =>
            Guard.Against.NegativeAmountsInNormalReceipt(receipt, nameof(receipt)));

        Assert.Null(exception);
    }

    #endregion

    #region NegativeQuantitiesInNormalOrder Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeQuantitiesInNormalOrder_WithNullOrder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            Guard.Against.NegativeQuantitiesInNormalOrder(null!, "order"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeQuantitiesInNormalOrder_WithAllPositiveQuantities_DoesNotThrow()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 2m, Text = "Pizza Margherita", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) },
                new() { Quantity = 1m, Text = "Cola 0.5L", PricePerUnit = MoneyAmount.Create(2.50m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert - Should not throw
        Exception? exception = Record.Exception(() =>
            Guard.Against.NegativeQuantitiesInNormalOrder(order, nameof(order)));

        Assert.Null(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeQuantitiesInNormalOrder_WithNegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = -2m, Text = "Pizza", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) } // Negative!
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.NegativeQuantitiesInNormalOrder(order, "order", "FinishStornoOrderTransactionRequest"));

        Assert.Contains("Positive quantities required for normal order", exception.Message);
        Assert.Contains("negative/zero quantity -2", exception.Message);
        Assert.Contains("Pizza", exception.Message);
        Assert.Contains("FinishStornoOrderTransactionRequest", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeQuantitiesInNormalOrder_WithZeroQuantity_ThrowsArgumentException()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 0m, Text = "Test Item", PricePerUnit = MoneyAmount.Create(5.00m, CurrencyCode.EUR) } // Zero!
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.NegativeQuantitiesInNormalOrder(order, "order"));

        Assert.Contains("Positive quantities required", exception.Message);
        Assert.Contains("negative/zero quantity 0", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeQuantitiesInNormalOrder_WithMixedQuantities_ThrowsOnFirstInvalid()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 2m, Text = "Valid Item", PricePerUnit = MoneyAmount.Create(5.00m, CurrencyCode.EUR) },
                new() { Quantity = -1m, Text = "Invalid Item", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) } // Negative!
            }
        };

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            Guard.Against.NegativeQuantitiesInNormalOrder(order, "order"));

        Assert.Contains("Invalid Item", exception.Message); // Should fail on the negative one
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NegativeQuantitiesInNormalOrder_WithFractionalPositiveQuantities_DoesNotThrow()
    {
        // Arrange
        Order order = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = 0.5m, Text = "Beer 0.5L", PricePerUnit = MoneyAmount.Create(3.50m, CurrencyCode.EUR) },
                new() { Quantity = 1.5m, Text = "Pizza", PricePerUnit = MoneyAmount.Create(8.90m, CurrencyCode.EUR) }
            }
        };

        // Act & Assert - Should not throw
        Exception? exception = Record.Exception(() =>
            Guard.Against.NegativeQuantitiesInNormalOrder(order, nameof(order)));

        Assert.Null(exception);
    }

    #endregion
}
