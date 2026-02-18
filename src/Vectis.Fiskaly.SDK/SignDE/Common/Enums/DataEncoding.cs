using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataEncoding
{
    [JsonStringEnumMemberName("UTF-8")]
    Utf8
}
