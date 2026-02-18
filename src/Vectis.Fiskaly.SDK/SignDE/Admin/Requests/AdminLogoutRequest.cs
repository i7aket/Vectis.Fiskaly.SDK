using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Admin.Requests;

public sealed class AdminLogoutRequest
{
    [JsonIgnore]
    public static AdminLogoutRequest Empty { get; } = new();
}
