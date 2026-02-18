using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExportState
{
    [JsonStringEnumMemberName("PENDING")]
    Pending,
    [JsonStringEnumMemberName("WORKING")]
    Working,
    [JsonStringEnumMemberName("COMPLETED")]
    Completed,
    [JsonStringEnumMemberName("ERROR")]
    Error,
    [JsonStringEnumMemberName("CANCELLED")]
    Cancelled
}
