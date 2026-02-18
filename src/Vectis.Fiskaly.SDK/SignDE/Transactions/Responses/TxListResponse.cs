using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;

public class TxListResponse
{
    [JsonPropertyName("data")]
    public List<TxResponse>? Data { get; init; }
    [JsonPropertyName("count")]
    public int? Count { get; init; }
    [JsonPropertyName("_type")]
    public ResourceType? Type { get; init; }
    [JsonPropertyName("_env")]
    public Env? Env { get; init; }
    [JsonPropertyName("_version")]
    public string? Version { get; init; }
}
