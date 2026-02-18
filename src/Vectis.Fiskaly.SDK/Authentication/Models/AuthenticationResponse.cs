using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Common;

namespace Vectis.Fiskaly.SDK.Authentication.Models;

public record AuthenticationResponse
{
    [JsonPropertyName("access_token")]
    public required AccessToken AccessToken { get; init; }
    [JsonPropertyName("refresh_token")]
    public RefreshToken? RefreshToken { get; init; }
    [JsonPropertyName("access_token_expires_in")]
    public int ExpiresIn { get; init; }
    [JsonPropertyName("access_token_expires_at")]
    public DateTimeOffset? AccessTokenExpiresAt { get; init; }
    [JsonPropertyName("refresh_token_expires_in")]
    public int? RefreshTokenExpiresIn { get; init; }
    [JsonPropertyName("refresh_token_expires_at")]
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }
    [JsonPropertyName("access_token_claims")]
    public AccessTokenClaims? Claims { get; init; }
}

public class AccessTokenClaims
{
    [JsonPropertyName("env")]
    public required string Environment { get; init; }
    [JsonPropertyName("organization_id")]
    public OrganizationId? OrganizationId { get; init; }

    [OnDeserialized]
    internal void ValidateRequiredFields(StreamingContext _)
    {
        Guard.Json.NotNullOrWhiteSpace(Environment, nameof(Environment));
    }
}
