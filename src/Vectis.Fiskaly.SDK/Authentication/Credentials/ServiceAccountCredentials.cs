using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.SDK.Authentication.Credentials;

public sealed class ServiceAccountCredentials : IFiskalyCredentials
{
    public ServiceAccountCredentials(
        ApiKey apiKey,
        ApiSecret apiSecret,
        OrganizationId organizationId,
        UserId userId)
    {
        ApiKey = apiKey;
        ApiSecret = apiSecret;
        OrganizationId = organizationId;
        UserId = userId;
    }

    public ApiKey ApiKey { get; }

    public ApiSecret ApiSecret { get; }

    public OrganizationId OrganizationId { get; }

    public UserId UserId { get; }

    public AuthenticationPayload CreatePayload() => new ApiKeyAuthenticationPayload(ApiKey, ApiSecret);
}
