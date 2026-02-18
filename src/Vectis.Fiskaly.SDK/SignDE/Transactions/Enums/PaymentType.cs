using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentType
{
    [JsonStringEnumMemberName("CASH")]
    Cash = 0,
    [JsonStringEnumMemberName("NON_CASH")]
    NonCash
}
