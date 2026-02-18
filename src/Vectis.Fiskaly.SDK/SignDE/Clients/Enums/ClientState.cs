using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClientState
{
    [JsonStringEnumMemberName("REGISTERED")]
    Registered,
    [JsonStringEnumMemberName("DEREGISTERED")]
    Deregistered
}
