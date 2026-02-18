using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

public class Order : StandardV1SchemaPayload
{
    [JsonPropertyName("line_items")]
    public required List<LineItem> LineItems { get; init; }
}
