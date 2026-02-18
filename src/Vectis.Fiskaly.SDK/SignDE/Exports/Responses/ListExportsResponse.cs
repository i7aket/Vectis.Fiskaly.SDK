using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Common;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Responses;

public class ListExportsResponse
{
    internal const string ExpectedResourceType = "EXPORT_LIST";
    [JsonPropertyName("data")]
    public List<ExportJob> Data { get; init; } = null!;
    [JsonPropertyName("count")]
    public int Count { get; init; }
    [JsonPropertyName("_type")]
    public required ResourceType Type { get; init; }
    [JsonPropertyName("_env")]
    public required Env Env { get; init; }
    [JsonPropertyName("_version")]
    public string Version { get; init; } = null!;

    [OnDeserialized]
    internal void ValidateRequiredFields(StreamingContext _)
    {
        Guard.Json.NotNull(Data, nameof(Data));
        Guard.Json.NotNullOrWhiteSpace(Version, nameof(Version));
    }
}
