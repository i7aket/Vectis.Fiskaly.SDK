using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResourceType
{
    [JsonStringEnumMemberName("TRANSACTION")]
    Transaction,
    [JsonStringEnumMemberName("TSS")]
    Tss,
    [JsonStringEnumMemberName("CLIENT")]
    Client,
    [JsonStringEnumMemberName("EXPORT")]
    Export,
    [JsonStringEnumMemberName("TRANSACTION_LIST")]
    TransactionList,
    [JsonStringEnumMemberName("TSS_LIST")]
    TssList,
    [JsonStringEnumMemberName("CLIENT_LIST")]
    ClientList,
    [JsonStringEnumMemberName("EXPORT_LIST")]
    ExportList
}
