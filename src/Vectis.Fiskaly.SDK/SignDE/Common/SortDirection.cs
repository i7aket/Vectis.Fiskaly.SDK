using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection
{
    [JsonStringEnumMemberName("asc")]
    Ascending,
    [JsonStringEnumMemberName("desc")]
    Descending
}
