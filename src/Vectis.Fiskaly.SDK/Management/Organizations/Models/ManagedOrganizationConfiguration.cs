using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Management.Organizations.Models;

public class ManagedOrganizationConfiguration
{
    [Obsolete("Use BillingOptions.WithholdBilling instead. This will be removed in API v1.0.0.")]
    [JsonPropertyName("withhold_billing")]
    public bool? WithholdBilling { get; init; }
}
