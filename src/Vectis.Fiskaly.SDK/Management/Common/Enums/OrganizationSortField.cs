using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Management.Common.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrganizationSortField
{
    [JsonStringEnumMemberName("name")]
    Name,
    [JsonStringEnumMemberName("id")]
    Id,
    [JsonStringEnumMemberName("address_line1")]
    AddressLine1,
    [JsonStringEnumMemberName("zip")]
    Zip,
    [JsonStringEnumMemberName("town")]
    Town,
    [JsonStringEnumMemberName("display_name")]
    DisplayName,
    [JsonStringEnumMemberName("vat_id")]
    VatId,
    [JsonStringEnumMemberName("address_line2")]
    AddressLine2,
    [JsonStringEnumMemberName("state")]
    State,
    [JsonStringEnumMemberName("tax_number")]
    TaxNumber,
    [JsonStringEnumMemberName("economy_id")]
    EconomyId
}
