using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.SDK.Authentication.Models;

public abstract record AuthenticationPayload
{
    [JsonIgnore]
    public abstract string Kind { get; }
}

public sealed record ApiKeyAuthenticationPayload(
    [property: JsonPropertyName("api_key")] ApiKey ApiKey,
    [property: JsonPropertyName("api_secret")] ApiSecret ApiSecret) : AuthenticationPayload
{
    public override string Kind => "api_key";
}

public sealed record RefreshTokenAuthenticationPayload(
    [property: JsonPropertyName("refresh_token")] RefreshToken RefreshToken) : AuthenticationPayload
{
    public override string Kind => "refresh_token";
}
