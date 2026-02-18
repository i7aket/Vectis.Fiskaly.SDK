using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Admin.Requests;

public class ChangeAdminPinRequest
{
    [JsonPropertyName("admin_puk")]
    public required AdminPuk AdminPuk { get; init; }
    [JsonPropertyName("new_admin_pin")]
    public required AdminPin NewAdminPin { get; init; }
}
