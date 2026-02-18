using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.Requests;

public sealed class UpdateClientRequest
{
    [JsonPropertyName("state")]
    public required ClientState State { get; init; }
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetadataCollection? Metadata { get; init; }

    public static UpdateClientRequest Register(MetadataCollection? metadata = null)
    {
        return new UpdateClientRequest { State = ClientState.Registered, Metadata = metadata };
    }

    public static UpdateClientRequest Deregister(MetadataCollection? metadata = null)
    {
        return new UpdateClientRequest { State = ClientState.Deregistered, Metadata = metadata };
    }
}
