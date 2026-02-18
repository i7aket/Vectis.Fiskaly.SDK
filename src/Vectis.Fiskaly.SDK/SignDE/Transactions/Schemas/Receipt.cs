using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

public class Receipt : StandardV1SchemaPayload
{
    [JsonPropertyName("receipt_type")]
    public required ReceiptType ReceiptType { get; init; } = ReceiptType.Receipt;
    [JsonPropertyName("amounts_per_vat_rate")]
    public required List<VatRateAmount> AmountsPerVatRate { get; init; }
    [JsonPropertyName("amounts_per_payment_type")]
    public List<PaymentTypeAmount> AmountsPerPaymentType { get; init; } = new();
}
