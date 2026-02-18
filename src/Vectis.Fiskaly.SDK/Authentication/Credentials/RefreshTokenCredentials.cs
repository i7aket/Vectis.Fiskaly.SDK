using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.SDK.Authentication.Credentials;

public sealed class RefreshTokenCredentials : IFiskalyCredentials
{
    public RefreshTokenCredentials(RefreshToken refreshToken)
    {
        RefreshToken = refreshToken;
    }

    public RefreshToken RefreshToken { get; }

    public AuthenticationPayload CreatePayload() => new RefreshTokenAuthenticationPayload(RefreshToken);
}
