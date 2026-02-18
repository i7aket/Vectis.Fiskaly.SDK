using System.Text.Json;
using System.Net.Http.Json;
using Vectis.Fiskaly.SDK.Extensions;

namespace Vectis.Fiskaly.SDK.Http;

public class FiskalyHttpRequestExecutor
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<FiskalyHttpRequestExecutor> _logger;

    public FiskalyHttpRequestExecutor(
        JsonSerializerOptions serializerOptions,
        ILogger<FiskalyHttpRequestExecutor> logger)
    {
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> ExecuteGetAsync<TResponse>(
        HttpClient httpClient,
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        _logger.LogExecutingGet(url);

        using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken)
            .ConfigureAwait(false);

        return await response.Content.ReadFiskalyJsonAsync<TResponse>(
            _serializerOptions,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> ExecutePutAsync<TResponse>(
        HttpClient httpClient,
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        _logger.LogExecutingPutNoBody(url);

        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Put, url);

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        return await response.Content.ReadFiskalyJsonAsync<TResponse>(
            _serializerOptions,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<TResponse> ExecutePutAsync<TRequest, TResponse>(
        HttpClient httpClient,
        string url,
        TRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteJsonRequestAsync<TRequest, TResponse>(httpClient, HttpMethod.Put, url, request, cancellationToken);

    public Task<TResponse> ExecutePatchAsync<TRequest, TResponse>(
        HttpClient httpClient,
        string url,
        TRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteJsonRequestAsync<TRequest, TResponse>(httpClient, HttpMethod.Patch, url, request, cancellationToken);

    public Task<TResponse> ExecutePostAsync<TRequest, TResponse>(
        HttpClient httpClient,
        string url,
        TRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteJsonRequestAsync<TRequest, TResponse>(httpClient, HttpMethod.Post, url, request, cancellationToken);

    public async Task ExecutePostAsync<TRequest>(
        HttpClient httpClient,
        string url,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogExecutingPostNoResponse(url);

        using JsonContent content = JsonContent.Create(request, options: _serializerOptions);

        using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TResponse> ExecuteDeleteAsync<TResponse>(
        HttpClient httpClient,
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        _logger.LogExecutingDelete(url);

        using HttpResponseMessage response = await httpClient.DeleteAsync(url, cancellationToken)
            .ConfigureAwait(false);

        return await response.Content.ReadFiskalyJsonAsync<TResponse>(
            _serializerOptions,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteDeleteAsync(
        HttpClient httpClient,
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        _logger.LogExecutingDelete(url);

        using HttpResponseMessage response = await httpClient.DeleteAsync(url, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<TResponse> ExecuteJsonRequestAsync<TRequest, TResponse>(
        HttpClient httpClient,
        HttpMethod method,
        string url,
        TRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogExecutingRequest(method, url);

        using JsonContent content = JsonContent.Create(request, options: _serializerOptions);

        using HttpRequestMessage httpRequest = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        return await response.Content.ReadFiskalyJsonAsync<TResponse>(
            _serializerOptions,
            cancellationToken).ConfigureAwait(false);
    }


}
