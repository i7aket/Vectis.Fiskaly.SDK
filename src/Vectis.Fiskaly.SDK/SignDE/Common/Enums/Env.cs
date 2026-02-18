using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Env
{
    [JsonStringEnumMemberName("TEST")]
    Test,
    [JsonStringEnumMemberName("LIVE")]
    Live
}
