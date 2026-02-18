using Vectis.Fiskaly.SDK.Authentication.Credentials;
using Vectis.Fiskaly.SDK.Authentication.Models;

namespace Vectis.Fiskaly.SDK.Authentication;

/// <summary>
/// Issues Fiskaly JWT tokens via POST /api/v2/auth.
/// </summary>
public interface IFiskalyAuthenticationService : IDisposable
{
    /// <summary>
    /// Returns a cached or freshly requested JWT for POST /api/v2/auth.
    /// </summary>
    /// <param name="cancellationToken">Forwarded to the underlying HTTP call.</param>
    /// <returns>Bearer token text for the Authorization header.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a JWT for the supplied credential strategy via POST /api/v2/auth.
    /// </summary>
    /// <param name="credentials">Strategy (API key, refresh token, service account).</param>
    /// <param name="cancellationToken">Forwarded to the HTTP call.</param>
    /// <returns>Bearer token text for the Authorization header.</returns>
    Task<string> GetAccessTokenAsync(IFiskalyCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes POST /api/v2/auth and returns the entire AuthenticationResponse payload.
    /// </summary>
    /// <param name="credentials">Strategy (API key, refresh token, service account).</param>
    /// <param name="cancellationToken">Forwarded to the HTTP call.</param>
    /// <returns>The parsed AuthenticationResponse from fiskaly.</returns>
    Task<AuthenticationResponse> AuthenticateAsync(IFiskalyCredentials credentials, CancellationToken cancellationToken = default);
}
