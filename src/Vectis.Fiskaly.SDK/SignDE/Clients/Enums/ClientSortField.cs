using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClientSortField
{
    [JsonStringEnumMemberName("serial_number")]
    SerialNumber,
    [JsonStringEnumMemberName("time_creation")]
    TimeCreation
}
