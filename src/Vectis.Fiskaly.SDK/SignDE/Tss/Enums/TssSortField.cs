using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Tss.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TssSortField
{
    [JsonStringEnumMemberName("description")]
    Description,
    [JsonStringEnumMemberName("state")]
    State,
    [JsonStringEnumMemberName("time_creation")]
    TimeCreation,
    [JsonStringEnumMemberName("time_init")]
    TimeInit,
    [JsonStringEnumMemberName("time_disable")]
    TimeDisable
}
