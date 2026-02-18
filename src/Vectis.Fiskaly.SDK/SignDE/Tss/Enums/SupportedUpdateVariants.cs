using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Tss.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupportedUpdateVariants
{
    [JsonStringEnumMemberName("SIGNED")]
    Signed
}
