using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExportType
{
    [JsonStringEnumMemberName("EXPORT")]
    Export
}
