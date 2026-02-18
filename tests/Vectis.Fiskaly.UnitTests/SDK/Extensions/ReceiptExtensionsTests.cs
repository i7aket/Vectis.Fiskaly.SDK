using AwesomeAssertions;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Extensions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Receipt = Vectis.Fiskaly.SDK.SignDE.Transactions.Aggregates.Receipt;

namespace Vectis.Fiskaly.UnitTests.SDK.Extensions;

public class ReceiptExtensionsTests
{
    private readonly ClientId _testClientId = ClientId.New();

    #region Basic Conversion Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithSingleItemAndPayment_ShouldConvertCorrectly()
    {
        // Arrange
        Receipt receipt = Receipt.CreateSale(
            items: [new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)],
            payments: [new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceipt = AssertReceiptSchema(request);

        // Assert
        request.ClientId.Should().Be(_testClientId);
        schemaReceipt.ReceiptType.Should().Be(ReceiptType.Receipt);
        schemaReceipt.AmountsPerVatRate.Should().HaveCount(1);
        schemaReceipt.AmountsPerVatRate[0].VatRate.Should().Be(VatRate.Normal);
        schemaReceipt.AmountsPerVatRate[0].Amount.Value.Should().Be(17.00m);
        schemaReceipt.AmountsPerPaymentType.Should().HaveCount(1);
        schemaReceipt.AmountsPerPaymentType[0].PaymentType.Should().Be(PaymentType.Cash);
        schemaReceipt.AmountsPerPaymentType[0].Amount.Value.Should().Be(17.00m);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithNullReceipt_ShouldThrowException()
    {
        // Arrange
        Receipt? receipt = null;

        // Act
        Func<FinishTransactionRequest> act = () => receipt!.ToFiskalyRequest(_testClientId);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Auto-Grouping Tests - Items by VAT Rate

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithMultipleItemsSameVatRate_ShouldGroupAndSum()
    {
        // Arrange
        Receipt receipt = Receipt.CreateSale(
            items: [
                new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal),
                new ReceiptItem(MoneyAmount.Create(7.00m, CurrencyCode.EUR), VatRate.Normal) // Same VAT rate
            ],
            payments: [new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceiptGroup = AssertReceiptSchema(request);

        // Assert
        schemaReceiptGroup.AmountsPerVatRate.Should().HaveCount(1);
        schemaReceiptGroup.AmountsPerVatRate[0].VatRate.Should().Be(VatRate.Normal);
        schemaReceiptGroup.AmountsPerVatRate[0].Amount.Value.Should().Be(17.00m); // Grouped sum
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithMultipleItemsDifferentVatRates_ShouldNotGroup()
    {
        // Arrange
        Receipt receipt = Receipt.CreateSale(
            items: [
                new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal),
                new ReceiptItem(MoneyAmount.Create(7.00m, CurrencyCode.EUR), VatRate.Reduced1) // Different VAT rate
            ],
            payments: [new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceiptRates = AssertReceiptSchema(request);

        // Assert
        schemaReceiptRates.AmountsPerVatRate.Should().HaveCount(2);

        VatRateAmount normalRate = schemaReceiptRates.AmountsPerVatRate.Single(x => x.VatRate == VatRate.Normal);
        normalRate.Amount.Value.Should().Be(10.00m);

        VatRateAmount reducedRate = schemaReceiptRates.AmountsPerVatRate.Single(x => x.VatRate == VatRate.Reduced1);
        reducedRate.Amount.Value.Should().Be(7.00m);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithThreeItemsSameVatRate_ShouldGroupAll()
    {
        // Arrange
        Receipt receipt = Receipt.CreateSale(
            items: [
                new ReceiptItem(MoneyAmount.Create(5.00m, CurrencyCode.EUR), VatRate.Normal),
                new ReceiptItem(MoneyAmount.Create(7.00m, CurrencyCode.EUR), VatRate.Normal),
                new ReceiptItem(MoneyAmount.Create(8.00m, CurrencyCode.EUR), VatRate.Normal)
            ],
            payments: [new Payment(MoneyAmount.Create(20.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceiptThree = AssertReceiptSchema(request);

        // Assert
        schemaReceiptThree.AmountsPerVatRate.Should().HaveCount(1);
        schemaReceiptThree.AmountsPerVatRate[0].Amount.Value.Should().Be(20.00m);
    }

    #endregion

    #region Auto-Grouping Tests - Payments by Type

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithMultiplePaymentsSameType_ShouldGroupAndSum()
    {
        // Arrange
        Receipt receipt = Receipt.CreateSale(
            items: [new ReceiptItem(MoneyAmount.Create(20.00m, CurrencyCode.EUR), VatRate.Normal)],
            payments: [
                new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash),
                new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash) // Same payment type
            ]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceiptSamePayment = AssertReceiptSchema(request);

        // Assert
        schemaReceiptSamePayment.AmountsPerPaymentType.Should().HaveCount(1);
        schemaReceiptSamePayment.AmountsPerPaymentType[0].PaymentType.Should().Be(PaymentType.Cash);
        schemaReceiptSamePayment.AmountsPerPaymentType[0].Amount.Value.Should().Be(20.00m); // Grouped sum
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithMultiplePaymentsDifferentTypes_ShouldNotGroup()
    {
        // Arrange
        Receipt receipt = Receipt.CreateSale(
            items: [new ReceiptItem(MoneyAmount.Create(20.00m, CurrencyCode.EUR), VatRate.Normal)],
            payments: [
                new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash),
                new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.NonCash) // Different payment type
            ]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceiptDifferentPayment = AssertReceiptSchema(request);

        // Assert
        schemaReceiptDifferentPayment.AmountsPerPaymentType.Should().HaveCount(2);

        PaymentTypeAmount cashPayment = schemaReceiptDifferentPayment.AmountsPerPaymentType.Single(x => x.PaymentType == PaymentType.Cash);
        cashPayment.Amount.Value.Should().Be(10.00m);

        PaymentTypeAmount nonCashPayment = schemaReceiptDifferentPayment.AmountsPerPaymentType.Single(x => x.PaymentType == PaymentType.NonCash);
        nonCashPayment.Amount.Value.Should().Be(10.00m);
    }

    #endregion

    #region Complex Scenario Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithComplexReceipt_ShouldGroupCorrectly()
    {
        // Arrange - Pizza shop scenario:
        // - 2 pizzas at 19% VAT (€8.50 each = €17.00)
        // - 1 book at 7% VAT (€10.00)
        // - Total: €27.00
        // - Payment: €20 cash + €7 card
        Receipt receipt = Receipt.CreateSale(
            items: [
                new ReceiptItem(MoneyAmount.Create(8.50m, CurrencyCode.EUR), VatRate.Normal),  // Pizza 1
                new ReceiptItem(MoneyAmount.Create(8.50m, CurrencyCode.EUR), VatRate.Normal),  // Pizza 2
                new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Reduced1) // Book
            ],
            payments: [
                new Payment(MoneyAmount.Create(20.00m, CurrencyCode.EUR), PaymentType.Cash),
                new Payment(MoneyAmount.Create(7.00m, CurrencyCode.EUR), PaymentType.NonCash)
            ]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceiptComplex = AssertReceiptSchema(request);

        // Assert - Items grouped by VAT rate
        schemaReceiptComplex.AmountsPerVatRate.Should().HaveCount(2);

        VatRateAmount normalVat = schemaReceiptComplex.AmountsPerVatRate.Single(x => x.VatRate == VatRate.Normal);
        normalVat.Amount.Value.Should().Be(17.00m); // 2 pizzas grouped

        VatRateAmount reducedVat = schemaReceiptComplex.AmountsPerVatRate.Single(x => x.VatRate == VatRate.Reduced1);
        reducedVat.Amount.Value.Should().Be(10.00m);

        // Assert - Payments NOT grouped (different types)
        schemaReceiptComplex.AmountsPerPaymentType.Should().HaveCount(2);

        PaymentTypeAmount cashPayment = schemaReceiptComplex.AmountsPerPaymentType.Single(x => x.PaymentType == PaymentType.Cash);
        cashPayment.Amount.Value.Should().Be(20.00m);

        PaymentTypeAmount cardPayment = schemaReceiptComplex.AmountsPerPaymentType.Single(x => x.PaymentType == PaymentType.NonCash);
        cardPayment.Amount.Value.Should().Be(7.00m);
    }

    #endregion

    #region Receipt Type Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithTrainingReceipt_ShouldPreserveType()
    {
        // Arrange
        Receipt receipt = Receipt.CreateTraining(
            items: [new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal)],
            payments: [new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        TransactionSchema? schema = request.Schema;
        Assert.NotNull(schema);

        // Assert
        bool hasReceipt = schema!.TryGetReceipt(out Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt? receiptData);
        hasReceipt.Should().BeTrue();
        receiptData.Should().NotBeNull();
        receiptData!.ReceiptType.Should().Be(ReceiptType.Training);
    }

    // NOTE: ToFiskalyRequest() extension method only supports normal receipts (positive amounts).
    // For storno receipts with negative amounts, use FinishStornoReceiptTransactionRequest directly.
    // This design enforces type safety - storno receipts require OriginalTransactionId which
    // Receipt aggregate doesn't have.

    #endregion

    #region Multi-Currency Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithUSDReceipt_ShouldPreserveCurrency()
    {
        // Arrange
        Receipt receipt = Receipt.CreateSale(
            items: [new ReceiptItem(MoneyAmount.Create(20.00m, CurrencyCode.USD), VatRate.Normal)],
            payments: [new Payment(MoneyAmount.Create(20.00m, CurrencyCode.USD), PaymentType.Cash)]
        );

        // Act
        FinishTransactionRequest request = receipt.ToFiskalyRequest(_testClientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceiptUsd = AssertReceiptSchema(request);

        // Assert
        schemaReceiptUsd.AmountsPerVatRate[0].Amount.Currency.Should().Be(CurrencyCode.USD);
        schemaReceiptUsd.AmountsPerPaymentType[0].Amount.Currency.Should().Be(CurrencyCode.USD);
    }

    #endregion
    private static Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt AssertReceiptSchema(FinishTransactionRequest request)
    {
        TransactionSchema? schema = request.Schema;
        Assert.NotNull(schema);
        StandardV1Schema? standard = schema!.StandardV1;
        Assert.NotNull(standard);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt? receipt = standard!.Receipt;
        Assert.NotNull(receipt);
        return receipt!;
    }
}
