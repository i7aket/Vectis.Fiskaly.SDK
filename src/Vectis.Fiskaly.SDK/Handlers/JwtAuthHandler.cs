using Vectis.Fiskaly.SDK.Authentication;

namespace Vectis.Fiskaly.SDK.Handlers;

internal sealed class JwtAuthHandler(
    IFiskalyAuthenticationService authService,
    ILogger<JwtAuthHandler> logger)
    : DelegatingHandler
{
    private readonly IFiskalyAuthenticationService _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    private readonly ILogger<JwtAuthHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string token = await _authService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        _logger.LogJwtTokenAdded(request.Method, request.RequestUri);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
