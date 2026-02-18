using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Management.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrganizationType
{
    [JsonStringEnumMemberName("ORGANIZATION")]
    Organization,
    [JsonStringEnumMemberName("MANAGED_ORGANIZATION")]
    ManagedOrganization
}
