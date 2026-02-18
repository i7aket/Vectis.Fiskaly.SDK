using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Requests;

public class UpdateTransactionRequestTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static ClientId CreateClient() => ClientId.From("9f4c8ec5-1111-4f2d-aaaa-bbbbbbbbbbbb");

    [Fact]
    public void Receipt_Factory_SetsActiveState()
    {
        UpdateTransactionRequest request = UpdateTransactionRequest.CreateReceipt(
            CreateClient(),
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(9.99m, CurrencyCode.EUR) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(9.99m, CurrencyCode.EUR) }
                }
            });

        Assert.Equal(TxState.Active, request.State);
        Assert.NotNull(request.Schema.TryGetReceipt(out _));
    }

    [Fact]
    public void Receipt_WithPositiveAmounts_Serializes()
    {
        UpdateTransactionRequest request = UpdateTransactionRequest.CreateReceipt(
            CreateClient(),
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(5m, CurrencyCode.EUR) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(5m, CurrencyCode.EUR) }
                }
            });

        string json = JsonSerializer.Serialize(request, _options);
        Assert.Contains("\"state\":\"ACTIVE\"", json);
        Assert.Contains("\"receipt\"", json);
    }

    [Fact]
    public void Receipt_WithNegativeAmount_Throws()
    {
        Receipt invalid = new Receipt
        {
            ReceiptType = ReceiptType.Receipt,
            AmountsPerVatRate = new List<VatRateAmount>
            {
                new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-1m, CurrencyCode.EUR) }
            },
            AmountsPerPaymentType = new List<PaymentTypeAmount>()
        };

        Assert.Throws<ArgumentException>(() =>
            UpdateTransactionRequest.CreateReceipt(CreateClient(), invalid));
    }

    [Fact]
    public void Order_Factory_ReturnsSchema()
    {
        UpdateTransactionRequest request = UpdateTransactionRequest.CreateOrder(
            CreateClient(),
            new Order
            {
                LineItems = new List<LineItem>
                {
                    new() { Quantity = 3m, Text = "Sandwich", PricePerUnit = MoneyAmount.Create(4m, CurrencyCode.EUR) }
                }
            });

        Assert.NotNull(request.Schema.TryGetOrder(out _));
    }

    [Fact]
    public void Order_WithNegativeQuantity_Throws()
    {
        Order invalid = new Order
        {
            LineItems = new List<LineItem>
            {
                new() { Quantity = -3m, Text = "Return", PricePerUnit = MoneyAmount.Create(4m, CurrencyCode.EUR) }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            UpdateTransactionRequest.CreateOrder(CreateClient(), invalid));
    }

    [Fact]
    public void Other_Factory_AllowsAdditionalData()
    {
        UpdateTransactionRequest request = UpdateTransactionRequest.CreateOther(
            CreateClient(),
            new Other
            {
                AdditionalData = new Dictionary<string, object>
                {
                    ["mode"] = "training"
                }
            });

        string json = JsonSerializer.Serialize(request, _options);
        Assert.Contains("\"other\"", json);
        Assert.DoesNotContain("\"receipt\"", json);
    }

    [Fact]
    public void StornoReceipt_DoesNotModifyMetadata_WhenNotProvided()
    {
        UpdateTransactionRequest request = UpdateTransactionRequest.CreateStornoReceipt(
            CreateClient(),
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-10m, CurrencyCode.EUR) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(-10m, CurrencyCode.EUR) }
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
            .Add("reason", "customer_return");

        UpdateTransactionRequest request = UpdateTransactionRequest.CreateStornoReceipt(
            CreateClient(),
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(-10m, CurrencyCode.EUR) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(-10m, CurrencyCode.EUR) }
                }
            },
            metadata);

        // SDK should preserve all metadata as-is
        Assert.NotNull(request.Metadata);
        Assert.Equal(originalTxId.Value.ToString(), request.Metadata["return_reference"]);
        Assert.Equal("customer_return", request.Metadata["reason"]);
    }

    [Fact]
    public void StornoOrder_DoesNotModifyMetadata_WhenNotProvided()
    {
        UpdateTransactionRequest request = UpdateTransactionRequest.CreateStornoOrder(
            CreateClient(),
            new Order
            {
                LineItems = new List<LineItem>
                {
                    new() { Quantity = -2m, Text = "Returned item", PricePerUnit = MoneyAmount.Create(15m, CurrencyCode.EUR) }
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
            .Add("warehouse_id", "WH-42");

        UpdateTransactionRequest request = UpdateTransactionRequest.CreateStornoOrder(
            CreateClient(),
            new Order
            {
                LineItems = new List<LineItem>
                {
                    new() { Quantity = -2m, Text = "Returned item", PricePerUnit = MoneyAmount.Create(15m, CurrencyCode.EUR) }
                }
            },
            metadata);

        // SDK should preserve all metadata as-is
        Assert.NotNull(request.Metadata);
        Assert.Equal(originalTxId.Value.ToString(), request.Metadata["return_reference"]);
        Assert.Equal("WH-42", request.Metadata["warehouse_id"]);
    }
}
