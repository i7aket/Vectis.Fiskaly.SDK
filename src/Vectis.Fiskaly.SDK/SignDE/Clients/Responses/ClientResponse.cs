using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.Responses;

public class ClientResponse
{
    internal const string ExpectedResourceType = "CLIENT";
    [JsonPropertyName("_id")]
    public ClientId? Id { get; init; }
    [JsonPropertyName("serial_number")]
    public ClientSerialNumber? SerialNumber { get; init; }
    [JsonPropertyName("_env")]
    public Env? Env { get; init; }
    [JsonPropertyName("_type")]
    public ResourceType? Type { get; init; }
    [JsonPropertyName("_version")]
    public string? Version { get; init; }
    [JsonPropertyName("state")]
    public ClientState? State { get; init; }
    [JsonPropertyName("metadata")]
    public MetadataCollection? Metadata { get; init; }
    [JsonPropertyName("time_creation")]
    public DateTimeOffset? TimeCreation { get; init; }
    [JsonPropertyName("time_update")]
    public DateTimeOffset? TimeUpdate { get; init; }
    [JsonPropertyName("tss_id")]
    public TssId? TssId { get; init; }
}
