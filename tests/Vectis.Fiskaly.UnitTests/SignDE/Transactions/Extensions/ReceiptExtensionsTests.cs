using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Extensions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Receipt = Vectis.Fiskaly.SDK.SignDE.Transactions.Aggregates.Receipt;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Extensions;

public class ReceiptExtensionsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithSingleItem_CreatesRequestWithCorrectAmounts()
    {
        Receipt receipt = Receipt.CreateSale(
            items: [new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)],
            payments: [new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );
        ClientId clientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");

        FinishTransactionRequest request = receipt.ToFiskalyRequest(clientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceipt = AssertReceiptSchema(request);

        Assert.NotNull(request);
        Assert.Equal(clientId, request.ClientId);
        Assert.Equal(ReceiptType.Receipt, schemaReceipt.ReceiptType);
        Assert.Single(schemaReceipt.AmountsPerVatRate);
        Assert.Single(schemaReceipt.AmountsPerPaymentType);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithMultipleItemsSameVatRate_GroupsAmounts()
    {
        Receipt receipt = Receipt.CreateSale(
            items:
            [
                new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal),
                new ReceiptItem(MoneyAmount.Create(7.00m, CurrencyCode.EUR), VatRate.Normal)
            ],
            payments: [new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );
        ClientId clientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");

        FinishTransactionRequest request = receipt.ToFiskalyRequest(clientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceipt = AssertReceiptSchema(request);

        Assert.Single(schemaReceipt.AmountsPerVatRate);
        VatRateAmount vatRateAmount = schemaReceipt.AmountsPerVatRate[0];
        Assert.Equal(VatRate.Normal, vatRateAmount.VatRate);
        Assert.Equal(17.00m, vatRateAmount.Amount.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithMultipleItemsDifferentVatRates_CreatesMultipleEntries()
    {
        Receipt receipt = Receipt.CreateSale(
            items:
            [
                new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal),
                new ReceiptItem(MoneyAmount.Create(7.00m, CurrencyCode.EUR), VatRate.Reduced1)
            ],
            payments: [new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );
        ClientId clientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");

        FinishTransactionRequest request = receipt.ToFiskalyRequest(clientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceipt = AssertReceiptSchema(request);

        Assert.Equal(2, schemaReceipt.AmountsPerVatRate.Count);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithMultiplePaymentsSameType_GroupsAmounts()
    {
        Receipt receipt = Receipt.CreateSale(
            items: [new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)],
            payments:
            [
                new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash),
                new Payment(MoneyAmount.Create(7.00m, CurrencyCode.EUR), PaymentType.Cash)
            ]
        );
        ClientId clientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");

        FinishTransactionRequest request = receipt.ToFiskalyRequest(clientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceipt = AssertReceiptSchema(request);

        Assert.Single(schemaReceipt.AmountsPerPaymentType);
        PaymentTypeAmount paymentTypeAmount = schemaReceipt.AmountsPerPaymentType[0];
        Assert.Equal(PaymentType.Cash, paymentTypeAmount.PaymentType);
        Assert.Equal(17.00m, paymentTypeAmount.Amount.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithMultiplePaymentsDifferentTypes_CreatesMultipleEntries()
    {
        Receipt receipt = Receipt.CreateSale(
            items: [new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)],
            payments:
            [
                new Payment(MoneyAmount.Create(10.00m, CurrencyCode.EUR), PaymentType.Cash),
                new Payment(MoneyAmount.Create(7.00m, CurrencyCode.EUR), PaymentType.NonCash)
            ]
        );
        ClientId clientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");

        FinishTransactionRequest request = receipt.ToFiskalyRequest(clientId);
        Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt schemaReceipt = AssertReceiptSchema(request);

        Assert.Equal(2, schemaReceipt.AmountsPerPaymentType.Count);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_WithNullReceipt_ThrowsArgumentNullException()
    {
        Receipt? receipt = null;
        ClientId clientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");

        Assert.Throws<ArgumentNullException>(() => receipt!.ToFiskalyRequest(clientId));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToFiskalyRequest_PreservesReceiptType()
    {
        Receipt receipt = Receipt.CreateTraining(
            items: [new ReceiptItem(MoneyAmount.Create(17.00m, CurrencyCode.EUR), VatRate.Normal)],
            payments: [new Payment(MoneyAmount.Create(17.00m, CurrencyCode.EUR), PaymentType.Cash)]
        );
        ClientId clientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");

        FinishTransactionRequest request = receipt.ToFiskalyRequest(clientId);
        TransactionSchema? schema = request.Schema;
        Assert.NotNull(schema);
        bool hasReceipt = schema!.TryGetReceipt(out Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt? receiptData);
        Assert.True(hasReceipt);
        Assert.NotNull(receiptData);
        Assert.Equal(ReceiptType.Training, receiptData.ReceiptType);
    }

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
