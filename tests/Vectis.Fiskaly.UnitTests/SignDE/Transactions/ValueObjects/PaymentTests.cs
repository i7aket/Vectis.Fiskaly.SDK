using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.ValueObjects;

public class PaymentTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_ValidPayment_SetsProperties()
    {
        MoneyAmount amount = MoneyAmount.Create(20.50m, CurrencyCode.EUR);
        Payment payment = new Payment(amount, PaymentType.Cash);

        Assert.Equal(amount, payment.Amount);
        Assert.Equal(PaymentType.Cash, payment.Type);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_NonCashPayment_SetsProperties()
    {
        MoneyAmount amount = MoneyAmount.Create(50.00m, CurrencyCode.EUR);
        Payment payment = new Payment(amount, PaymentType.NonCash);

        Assert.Equal(amount, payment.Amount);
        Assert.Equal(PaymentType.NonCash, payment.Type);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        MoneyAmount amount = MoneyAmount.Create(25.75m, CurrencyCode.EUR);
        Payment payment = new Payment(amount, PaymentType.Cash);

        string result = payment.ToString();

        Assert.Contains("25.75", result);
        Assert.Contains(CurrencyCode.EUR.ToIsoString(), result);
        Assert.Contains("Cash", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        MoneyAmount amount1 = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        Payment payment1 = new Payment(amount1, PaymentType.Cash);

        MoneyAmount amount2 = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        Payment payment2 = new Payment(amount2, PaymentType.Cash);

        Assert.Equal(payment1, payment2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_DifferentAmounts_ReturnsFalse()
    {
        Payment payment1 = new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash);
        Payment payment2 = new Payment(MoneyAmount.Create(20.00m, CurrencyCode.EUR), PaymentType.Cash);

        Assert.NotEqual(payment1, payment2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_DifferentTypes_ReturnsFalse()
    {
        MoneyAmount amount = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        Payment payment1 = new Payment(amount, PaymentType.Cash);
        Payment payment2 = new Payment(amount, PaymentType.NonCash);

        Assert.NotEqual(payment1, payment2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void InitSyntax_CanCreatePayment()
    {
        Payment payment = new Payment(MoneyAmount.Create(15.00m, CurrencyCode.EUR), PaymentType.Cash)
        {
            Amount = MoneyAmount.Create(16.00m, CurrencyCode.EUR),
            Type = PaymentType.NonCash
        };

        Assert.Equal("16.00", payment.Amount.ToStringInvariant());
        Assert.Equal(PaymentType.NonCash, payment.Type);
    }

    #region Amount Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNegativeAmount_CreatesPayment()
    {
        // Arrange
        MoneyAmount negativeAmount = MoneyAmount.Create(-10.50m, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(negativeAmount, PaymentType.Cash);

        // Assert
        Assert.Equal("-10.50", payment.Amount.ToStringInvariant());
        Assert.True(payment.Amount.IsNegative);
        Assert.Equal(PaymentType.Cash, payment.Type);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithAllowedNegativeAmount_CreatesPayment()
    {
        // Arrange
        MoneyAmount negativeAmount = MoneyAmount.Create(-50.00m, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(negativeAmount, PaymentType.NonCash);

        // Assert
        Assert.Equal("-50.00", payment.Amount.ToStringInvariant());
        Assert.True(payment.Amount.IsNegative);
        Assert.Equal(PaymentType.NonCash, payment.Type);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(-10.50, "-10.50")]
    [InlineData(-100, "-100.00")]
    public void Constructor_WithStornoAmount_CreatesPayment(decimal input, string expected)
    {
        // Arrange
        MoneyAmount stornoAmount = MoneyAmount.Create(input, CurrencyCode.EUR).EnsureNonPositive();

        // Act
        Payment payment = new Payment(stornoAmount, PaymentType.Cash);

        // Assert
        Assert.Equal(expected, payment.Amount.ToStringInvariant());
        Assert.True(payment.Amount.IsNegative);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithZeroAmount_CreatesPayment()
    {
        // Arrange
        MoneyAmount zeroAmount = MoneyAmount.Create(0m, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(zeroAmount, PaymentType.Cash);

        // Assert
        Assert.Equal("0.00", payment.Amount.ToStringInvariant());
        Assert.True(payment.Amount.IsZero);
        Assert.Equal(0m, payment.Amount.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMoneyAmountZero_CreatesPayment()
    {
        // Arrange & Act
        Payment payment = new Payment(MoneyAmount.Zero(CurrencyCode.EUR), PaymentType.NonCash);

        // Assert
        Assert.True(payment.Amount.IsZero);
        Assert.Equal("0.00", payment.Amount.ToStringInvariant());
    }

    #endregion

    #region Decimal Precision Tests

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(10.556, "10.556")]  // Preserve up to 5 decimals
    [InlineData(10.554, "10.554")]  // No forced rounding to two decimals
    [InlineData(10.555, "10.555")]  // Banker's rounding not applied (<=5 decimals retained)
    [InlineData(10.545, "10.545")]  // Preserve precision
    [InlineData(0.001, "0.001")]    // Very small amount retained
    [InlineData(0.005, "0.005")]    // Preserve precision
    [InlineData(0.015, "0.015")]    // Preserve precision
    [InlineData(0.999, "0.999")]    // No implicit rounding to whole euro
    public void Constructor_WithHighPrecisionAmount_RoundsToTwoDecimals(decimal input, string expected)
    {
        // Arrange
        MoneyAmount amount = MoneyAmount.Create(input, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(amount, PaymentType.Cash);

        // Assert
        Assert.Equal(expected, payment.Amount.ToStringInvariant());
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(0.01, "0.01")]     // Minimum meaningful amount
    [InlineData(0.10, "0.10")]     // 10 cents
    [InlineData(1.00, "1.00")]     // 1 euro
    [InlineData(10.00, "10.00")]   // 10 euros
    [InlineData(100.00, "100.00")] // 100 euros
    [InlineData(999.99, "999.99")] // Maximum common amount
    public void Constructor_WithTypicalEuroAmounts_NormalizesToTwoDecimals(decimal input, string expected)
    {
        // Arrange
        MoneyAmount amount = MoneyAmount.Create(input, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(amount, PaymentType.NonCash);

        // Assert
        Assert.Equal(expected, payment.Amount.ToStringInvariant());
        Assert.Equal(CurrencyCode.EUR, payment.Amount.Currency);
    }

    #endregion

    #region Large Amount Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithVeryLargeAmount_CreatesPayment()
    {
        // Arrange
        MoneyAmount largeAmount = MoneyAmount.Create(9999999999.99m, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(largeAmount, PaymentType.NonCash);

        // Assert
        Assert.Equal("9999999999.99", payment.Amount.ToStringInvariant());
        Assert.Equal(9999999999.99m, payment.Amount.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMaxDecimalValue_CreatesPayment()
    {
        // Arrange
        // decimal.MaxValue is 79,228,162,514,264,337,593,543,950,335
        // After rounding to 2 decimals, it remains the same
        MoneyAmount maxAmount = MoneyAmount.Create(decimal.MaxValue, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(maxAmount, PaymentType.Cash);

        // Assert
        Assert.Equal(decimal.MaxValue, payment.Amount.Value);
        Assert.False(payment.Amount.IsNegative);
    }

    #endregion

    #region Currency Validation Tests

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(CurrencyCode.EUR)]
    [InlineData(CurrencyCode.USD)]
    [InlineData(CurrencyCode.GBP)]
    [InlineData(CurrencyCode.CHF)]
    public void Constructor_WithValidCurrency_PreservesCurrency(CurrencyCode currency)
    {
        MoneyAmount amount = MoneyAmount.Create(25.50m, currency);
        Payment payment = new Payment(amount, PaymentType.Cash);

        Assert.Equal(currency, payment.Amount.Currency);
        Assert.Equal("25.50", payment.Amount.ToStringInvariant());
    }

    #endregion

    #region German Fiscal Compliance Scenarios

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CashPaymentWithSmallAmount_CreatesPayment()
    {
        // Arrange - Minimum meaningful Euro amount (1 cent)
        MoneyAmount amount = MoneyAmount.Create(0.01m, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(amount, PaymentType.Cash);

        // Assert
        Assert.Equal("0.01", payment.Amount.ToStringInvariant());
        Assert.Equal(PaymentType.Cash, payment.Type);
        Assert.Equal(CurrencyCode.EUR, payment.Amount.Currency);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_NonCashPaymentWithTypicalAmount_CreatesPayment()
    {
        // Arrange - Typical card payment amount
        MoneyAmount amount = MoneyAmount.Create(89.95m, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(amount, PaymentType.NonCash);

        // Assert
        Assert.Equal("89.95", payment.Amount.ToStringInvariant());
        Assert.Equal(PaymentType.NonCash, payment.Type);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(5.00, PaymentType.Cash)]      // Common cash amount
    [InlineData(19.99, PaymentType.NonCash)]  // Typical retail price
    [InlineData(50.00, PaymentType.Cash)]     // Common bill denomination
    [InlineData(123.45, PaymentType.NonCash)] // Typical invoice amount
    [InlineData(1000.00, PaymentType.NonCash)]// Large transaction
    public void Constructor_WithTypicalGermanTransactionAmounts_CreatesPayment(
        decimal amountValue, PaymentType paymentType)
    {
        // Arrange
        MoneyAmount amount = MoneyAmount.Create(amountValue, CurrencyCode.EUR);

        // Act
        Payment payment = new Payment(amount, paymentType);

        // Assert
        Assert.Equal(amount, payment.Amount);
        Assert.Equal(paymentType, payment.Type);
        Assert.Equal(CurrencyCode.EUR, payment.Amount.Currency);
        Assert.False(payment.Amount.IsNegative);
    }

    #endregion
}
