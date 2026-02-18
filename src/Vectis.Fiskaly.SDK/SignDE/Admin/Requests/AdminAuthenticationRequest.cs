using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Admin.Requests;

public sealed class AdminAuthenticationRequest
{
    [JsonPropertyName("admin_pin")]
    public required AdminPin AdminPin { get; init; }
}
