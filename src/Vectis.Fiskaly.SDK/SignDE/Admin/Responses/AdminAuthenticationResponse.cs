using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Admin.Responses;

public class AdminAuthenticationResponse
{
    [JsonIgnore]
    public TssId TssId { get; init; }
}
