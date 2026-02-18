using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;

public abstract class TxRequest
{
    private readonly ClientId _clientId;
    private readonly TxState _state;
    [JsonPropertyName("client_id")]
    public required ClientId ClientId
    {
        get => _clientId;
        init => _clientId = value;
    }
    [JsonPropertyName("state")]
    public required TxState State
    {
        get => _state;
        init => _state = value;
    }
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MetadataCollection? Metadata { get; init; }
}
