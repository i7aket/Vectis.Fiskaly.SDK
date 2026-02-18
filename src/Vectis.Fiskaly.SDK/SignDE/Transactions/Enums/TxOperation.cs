using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TxOperation
{
    [JsonStringEnumMemberName("Start")]
    Start,
    [JsonStringEnumMemberName("Update")]
    Update,
    [JsonStringEnumMemberName("Finish")]
    Finish
}
