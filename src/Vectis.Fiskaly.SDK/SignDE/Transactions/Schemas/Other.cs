using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

public class Other : StandardV1SchemaPayload
{
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; init; }
}
