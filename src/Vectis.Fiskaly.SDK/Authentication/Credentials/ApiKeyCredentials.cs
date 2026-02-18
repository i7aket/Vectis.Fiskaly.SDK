using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.SDK.Authentication.Credentials;

public sealed class ApiKeyCredentials : IFiskalyCredentials
{
    public ApiKeyCredentials(ApiKey apiKey, ApiSecret apiSecret)
    {
        ApiKey = apiKey;
        ApiSecret = apiSecret;
    }

    public ApiKey ApiKey { get; }

    public ApiSecret ApiSecret { get; }

    public AuthenticationPayload CreatePayload()
    {
        return new ApiKeyAuthenticationPayload(ApiKey, ApiSecret);
    }
}
