using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;

namespace Vectis.Fiskaly.SDK.Management.Organizations.Responses;

public class ListOrganizationsResponse
{
    internal const string ExpectedResourceType = "ORGANIZATION_LIST";
    [JsonPropertyName("data")]
    public List<OrganizationResponse>? Data { get; init; }
    [JsonPropertyName("count")]
    public int? Count { get; init; }
    [JsonPropertyName("_type")]
    public string? Type { get; init; }
    [JsonPropertyName("_envs")]
    public List<Env>? Envs { get; init; }
    [JsonPropertyName("_version")]
    public string? Version { get; init; }
}
