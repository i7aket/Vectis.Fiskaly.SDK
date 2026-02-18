using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Requests;

public class FinishTransactionRequestTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static ClientId CreateClient() => ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");

    [Fact]
    public void Receipt_Factory_SetsStateAndSchema()
    {
        FinishTransactionRequest request = FinishTransactionRequest.CreateReceipt(
            CreateClient(),
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = [new VatRateAmount { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(12.34m, CurrencyCode.EUR) }],
                AmountsPerPaymentType = [new PaymentTypeAmount { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(12.34m, CurrencyCode.EUR) }]
            });

        Assert.Equal(TxState.Finished, request.State);
        bool hasReceipt = request.Schema.TryGetReceipt(out Receipt? receipt);
        Assert.True(hasReceipt);
        Assert.NotNull(receipt);
        Assert.Equal(ReceiptType.Receipt, receipt.ReceiptType);
        Assert.Single(receipt.AmountsPerVatRate);
    }

    [Fact]
    public void Receipt_WithNegativeAmount_Throws()
    {
        Receipt invalid = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-10m, CurrencyCode.EUR) }
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>()
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            FinishTransactionRequest.CreateReceipt(CreateClient(), invalid));

        Assert.Contains("StornoReceipt", ex.Message);
    }

    [Fact]
    public void StornoReceipt_DoesNotModifyMetadata_WhenNotProvided()
    {
        FinishTransactionRequest request = FinishTransactionRequest.CreateStornoReceipt(
            CreateClient(),
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-5m, CurrencyCode.EUR) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(-5m, CurrencyCode.EUR) }
                }
            });

        // SDK should not auto-add metadata - caller's responsibility
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void StornoReceipt_PreservesCustomMetadata()
    {
        TxId originalTxId = TxId.New();
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("return_reference", originalTxId.Value.ToString())
            .Add("custom_field", "custom_value");

        FinishTransactionRequest request = FinishTransactionRequest.CreateStornoReceipt(
            CreateClient(),
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-5m, CurrencyCode.EUR) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(-5m, CurrencyCode.EUR) }
                }
            },
            metadata);

        // SDK should preserve all metadata as-is
        Assert.NotNull(request.Metadata);
        Assert.Equal(originalTxId.Value.ToString(), request.Metadata["return_reference"]);
        Assert.Equal("custom_value", request.Metadata["custom_field"]);
    }

    [Fact]
    public void Order_Factory_ValidatesPositiveQuantities()
    {
        FinishTransactionRequest request = FinishTransactionRequest.CreateOrder(
            CreateClient(),
            new Order
            {
                LineItems = new List<LineItem>
                {
                    new() { Quantity = 2m, Text = "Item", PricePerUnit = MoneyAmount.Create(9.99m, CurrencyCode.EUR) }
                }
            });

        bool hasOrder = request.Schema.TryGetOrder(out Order? order);
        Assert.True(hasOrder);
        Assert.NotNull(order);
        Assert.Single(order.LineItems);
        Assert.Equal(2m, order.LineItems[0].Quantity);
    }

    [Fact]
    public void Order_WithNegativeQuantity_Throws()
    {
        Order invalid = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = -1m, Text = "Return", PricePerUnit = MoneyAmount.Create(5m, CurrencyCode.EUR) }
            }
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            FinishTransactionRequest.CreateOrder(CreateClient(), invalid));

        Assert.Contains("StornoOrder", ex.Message);
    }

    [Fact]
    public void StornoOrder_DoesNotModifyMetadata_WhenNotProvided()
    {
        FinishTransactionRequest request = FinishTransactionRequest.CreateStornoOrder(
            CreateClient(),
            new Order
            {
                LineItems = new List<LineItem>
                {
                    new() { Quantity = -1m, Text = "Return", PricePerUnit = MoneyAmount.Create(5m, CurrencyCode.EUR) }
                }
            });

        // SDK should not auto-add metadata - caller's responsibility
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void StornoOrder_PreservesCustomMetadata()
    {
        TxId originalTxId = TxId.New();
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("return_reference", originalTxId.Value.ToString())
            .Add("order_reference", "ORD-123");

        FinishTransactionRequest request = FinishTransactionRequest.CreateStornoOrder(
            CreateClient(),
            new Order
            {
                LineItems = new List<LineItem>
                {
                    new() { Quantity = -1m, Text = "Return", PricePerUnit = MoneyAmount.Create(5m, CurrencyCode.EUR) }
                }
            },
            metadata);

        // SDK should preserve all metadata as-is
        Assert.NotNull(request.Metadata);
        Assert.Equal(originalTxId.Value.ToString(), request.Metadata["return_reference"]);
        Assert.Equal("ORD-123", request.Metadata["order_reference"]);
    }

    [Fact]
    public void Other_Factory_SerializesCorrectly()
    {
        FinishTransactionRequest request = FinishTransactionRequest.CreateOther(
            CreateClient(),
            new Other
            {
                AdditionalData = new Dictionary<string, object>
                {
                    ["mode"] = "training"
                }
            });

        string json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"state\":\"FINISHED\"", json);
        Assert.Contains("\"other\"", json);
        Assert.DoesNotContain("\"receipt\"", json);
    }
}
