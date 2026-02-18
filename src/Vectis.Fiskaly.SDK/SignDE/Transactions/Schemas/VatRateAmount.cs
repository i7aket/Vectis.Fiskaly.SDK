using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

public class VatRateAmount
{
    [JsonPropertyName("vat_rate")]
    public VatRate VatRate { get; init; }
    [JsonPropertyName("amount")]
    public MoneyAmount Amount { get; init; } = MoneyAmount.Zero(CurrencyCode.EUR);
}
