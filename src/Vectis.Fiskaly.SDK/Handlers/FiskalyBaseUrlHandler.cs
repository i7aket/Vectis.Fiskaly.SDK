using Vectis.Fiskaly.SDK.Configuration;

namespace Vectis.Fiskaly.SDK.Handlers;

public sealed class FiskalyBaseUrlHandler : DelegatingHandler
{
    private readonly IOptionsMonitor<FiskalyConfiguration> _options;

    public FiskalyBaseUrlHandler(IOptionsMonitor<FiskalyConfiguration> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ApplyBaseUrl(request, _options.CurrentValue.BaseUrl);
        return base.SendAsync(request, cancellationToken);
    }

    private static void ApplyBaseUrl(HttpRequestMessage request, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        string normalized = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? baseUri))
        {
            return;
        }

        if (request.RequestUri == null)
        {
            request.RequestUri = baseUri;
            return;
        }

        if (!request.RequestUri.IsAbsoluteUri)
        {
            request.RequestUri = new Uri(baseUri, request.RequestUri);
            return;
        }

        string pathAndQuery = request.RequestUri.PathAndQuery;
        request.RequestUri = new Uri(baseUri, pathAndQuery);
    }
}
