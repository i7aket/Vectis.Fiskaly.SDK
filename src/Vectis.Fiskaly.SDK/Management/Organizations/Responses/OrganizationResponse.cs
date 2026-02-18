using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Management.Common.Enums;
using Vectis.Fiskaly.SDK.Management.Organizations.Models;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;

namespace Vectis.Fiskaly.SDK.Management.Organizations.Responses;

public class OrganizationResponse
{
    [JsonPropertyName("_id")]
    public OrganizationId? Id { get; init; }
    [JsonPropertyName("_type")]
    public OrganizationType? Type { get; init; }
    [JsonPropertyName("_envs")]
    public List<Env>? Envs { get; init; }
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    [JsonPropertyName("address_line1")]
    public string? AddressLine1 { get; init; }
    [JsonPropertyName("zip")]
    public string? Zip { get; init; }
    [JsonPropertyName("town")]
    public string? Town { get; init; }
    [JsonPropertyName("country_code")]
    public CountryCode? CountryCode { get; init; }
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }
    [JsonPropertyName("vat_id")]
    public string? VatId { get; init; }
    [JsonPropertyName("contact_person_id")]
    public Guid? ContactPersonId { get; init; }
    [JsonPropertyName("address_line2")]
    public string? AddressLine2 { get; init; }
    [JsonPropertyName("state")]
    public string? State { get; init; }
    [JsonPropertyName("tax_number")]
    public string? TaxNumber { get; init; }
    [JsonPropertyName("economy_id")]
    public string? EconomyId { get; init; }
    [JsonPropertyName("billing_options")]
    public BillingOptions? BillingOptions { get; init; }
    [JsonPropertyName("billing_address_id")]
    public Guid? BillingAddressId { get; init; }
    [JsonPropertyName("metadata")]
    public MetadataCollection? Metadata { get; init; }
    [JsonPropertyName("managed_by_organization_id")]
    public OrganizationId? ManagedByOrganizationId { get; init; }

    [Obsolete("Use BillingOptions instead. This will be removed in API v1.0.0.")]
    [JsonPropertyName("managed_configuration")]
    public ManagedOrganizationConfiguration? ManagedConfiguration { get; init; }
    [JsonPropertyName("created_by_user")]
    public Guid? CreatedByUser { get; init; }
}
