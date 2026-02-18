using Vectis.Fiskaly.SDK.Authentication.Models;

namespace Vectis.Fiskaly.SDK.Authentication.Credentials;

/// <summary>
/// Represents a credential strategy that can build the payload for POST /api/v2/auth.
/// </summary>
public interface IFiskalyCredentials
{
    /// <summary>
    /// Creates the AuthenticationPayload that the SDK posts to /api/v2/auth.
    /// </summary>
    AuthenticationPayload CreatePayload();
}
