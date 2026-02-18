using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.Requests;

public class CreateClientRequest
{
    [JsonPropertyName("serial_number")]
    public required ClientSerialNumber SerialNumber { get; init; }
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetadataCollection? Metadata { get; init; }
}
