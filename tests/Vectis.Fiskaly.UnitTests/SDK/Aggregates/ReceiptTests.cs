using AwesomeAssertions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Aggregates;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SDK.Aggregates;

public class ReceiptTests
{
    #region CreateSale Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithValidData_ShouldCreateReceipt()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Assert
        receipt.Type.Should().Be(ReceiptType.Receipt);
        receipt.Items.Should().HaveCount(1);
        receipt.Payments.Should().HaveCount(1);
        receipt.TotalItemAmount.Value.Should().Be(17.00m);
        receipt.TotalPaymentAmount.Value.Should().Be(17.00m);
        receipt.TotalItemAmount.Currency.Should().Be(CurrencyCode.EUR);
        receipt.TotalPaymentAmount.Currency.Should().Be(CurrencyCode.EUR);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithMultipleItems_ShouldCalculateTotals()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal),
            new ReceiptItem(MoneyAmount.Create(7.00m, CurrencyCode.EUR), VatRate.Reduced1)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Assert
        receipt.TotalItemAmount.Value.Should().Be(17.00m);
        receipt.TotalPaymentAmount.Value.Should().Be(17.00m);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithMultiplePayments_ShouldCalculateTotals()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(20.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash),
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.NonCash)
        ];

        // Act
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Assert
        receipt.TotalPaymentAmount.Value.Should().Be(20.00m);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithNoItems_ShouldThrowException()
    {
        // Arrange
        ReceiptItem[] items = [];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Func<Receipt> act = () => Receipt.CreateSale(items, payments);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one item*");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithNoPayments_ShouldThrowException()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments = [];

        // Act
        Func<Receipt> act = () => Receipt.CreateSale(items, payments);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*at least one payment*");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithMismatchedAmounts_ShouldThrowException()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(20.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Func<Receipt> act = () => Receipt.CreateSale(items, payments);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must match*");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithMixedCurrenciesInItems_ShouldThrowException()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal),
            new ReceiptItem(MoneyAmount.Create(7.00m, CurrencyCode.USD), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Func<Receipt> act = () => Receipt.CreateSale(items, payments);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*same currency*EUR*");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithMixedCurrenciesInPayments_ShouldThrowException()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash),
            new Payment(MoneyAmount.Create(7.00m, CurrencyCode.USD), PaymentType.NonCash)
        ];

        // Act
        Func<Receipt> act = () => Receipt.CreateSale(items, payments);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*same currency*EUR*");
    }

    #endregion

    #region Alternative Factory Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateTraining_WithValidData_ShouldCreateTrainingReceipt()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Receipt receipt = Receipt.CreateTraining(items, payments);

        // Assert
        receipt.Type.Should().Be(ReceiptType.Training);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateCancellation_WithValidData_ShouldCreateCancellationReceipt()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Receipt receipt = Receipt.CreateCancellation(items, payments);

        // Assert
        receipt.Type.Should().Be(ReceiptType.Cancellation);
    }

    #endregion

    #region CreateStorno Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateStorno_WithNegativeAmounts_ShouldCreateStornoReceipt()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(-17.00m, CurrencyCode.EUR).EnsureNonPositive(), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(-17.00m, CurrencyCode.EUR).EnsureNonPositive(), PaymentType.Cash)
        ];

        // Act
        Receipt receipt = Receipt.CreateStorno(items, payments);

        // Assert
        receipt.Type.Should().Be(ReceiptType.Annulation);
        receipt.TotalItemAmount.IsNegative.Should().BeTrue();
        receipt.TotalPaymentAmount.IsNegative.Should().BeTrue();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateStorno_WithPositiveItemAmount_ShouldThrowException()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(-17.00m, CurrencyCode.EUR).EnsureNonPositive(), PaymentType.Cash)
        ];

        // Act
        Func<Receipt> act = () => Receipt.CreateStorno(items, payments);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be negative*");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateStorno_WithPositivePaymentAmount_ShouldThrowException()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(-17.00m, CurrencyCode.EUR).EnsureNonPositive(), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Func<Receipt> act = () => Receipt.CreateStorno(items, payments);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be negative*");
    }

    #endregion

    #region Defensive Copy Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Items_ShouldReturnDefensiveCopy()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Act
        IReadOnlyList<ReceiptItem> itemsCopy1 = receipt.Items;
        IReadOnlyList<ReceiptItem> itemsCopy2 = receipt.Items;

        // Assert
        itemsCopy1.Should().NotBeSameAs(itemsCopy2);
        itemsCopy1.Should().Equal(itemsCopy2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Payments_ShouldReturnDefensiveCopy()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Act
        IReadOnlyList<Payment> paymentsCopy1 = receipt.Payments;
        IReadOnlyList<Payment> paymentsCopy2 = receipt.Payments;

        // Assert
        paymentsCopy1.Should().NotBeSameAs(paymentsCopy2);
        paymentsCopy1.Should().Equal(paymentsCopy2);
    }

    #endregion

    #region ToString Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Act
        string result = receipt.ToString();

        // Assert
        result.Should().Contain(nameof(ReceiptType.Receipt));
        result.Should().Contain("17.00");
        result.Should().Contain(CurrencyCode.EUR.ToIsoString());
        result.Should().Contain("1 items");
        result.Should().Contain("1 payments");
    }

    #endregion

    #region Multi-Currency Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithUSD_ShouldPreserveCurrency()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(20.00m, CurrencyCode.USD), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(20.00m, CurrencyCode.USD), PaymentType.Cash)
        ];

        // Act
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Assert
        receipt.TotalItemAmount.Currency.Should().Be(CurrencyCode.USD);
        receipt.TotalPaymentAmount.Currency.Should().Be(CurrencyCode.USD);
    }

    #endregion

    #region Edge Case Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithZeroAmounts_ShouldCreateReceipt()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(0m, CurrencyCode.EUR), VatRate.Null)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(0m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Assert
        receipt.TotalItemAmount.IsZero.Should().BeTrue();
        receipt.TotalPaymentAmount.IsZero.Should().BeTrue();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreateSale_WithRoundingDifference_ShouldAcceptSmallTolerance()
    {
        // Arrange
        ReceiptItem[] items =
        [
            new ReceiptItem(MoneyAmount.Create(10.005m, CurrencyCode.EUR), VatRate.Normal)
        ];
        Payment[] payments =
        [
            new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash)
        ];

        // Act
        Receipt receipt = Receipt.CreateSale(items, payments);

        // Assert
        receipt.TotalItemAmount.Value.Should().BeApproximately(10.00m, 0.01m);
        receipt.TotalPaymentAmount.Value.Should().BeApproximately(10.00m, 0.01m);
    }

    #endregion
}
