using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Requests;

public class CancelTransactionRequestTests
{
    private readonly JsonSerializerOptions _options;

    public CancelTransactionRequestTests()
    {
        _options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_SetsStateToCancelled()
    {
        CancelTransactionRequest request = new CancelTransactionRequest
            {
                ClientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012")
            };

        Assert.Equal(TxState.Cancelled, request.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_IncludesState()
    {
        CancelTransactionRequest request = new CancelTransactionRequest
        {
            ClientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012")
        };

        string json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"state\":\"CANCELLED\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithMetadata_IncludesMetadata()
    {
        CancelTransactionRequest request = new CancelTransactionRequest
        {
            ClientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012"),
            Metadata = MetadataCollection.Empty.Add("cancellation_reason", "Customer request")
        };

        string json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"state\":\"CANCELLED\"", json);
        Assert.Contains("\"metadata\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_IncludesSchema()
    {
        CancelTransactionRequest request = new CancelTransactionRequest
        {
            ClientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012"),
            Schema = TransactionSchema.ForReceipt(new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>(),
                AmountsPerPaymentType = new List<PaymentTypeAmount>()
            })
        };

        string json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"schema\"", json);
    }
}
