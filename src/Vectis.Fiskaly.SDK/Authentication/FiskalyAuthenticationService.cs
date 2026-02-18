using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Vectis.Fiskaly.SDK.Authentication.Credentials;
using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Configuration;
using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.SDK.Authentication;

public class FiskalyAuthenticationService : IFiskalyAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<FiskalyAuthenticationService> _logger;
    private readonly IFiskalyCredentials _defaultCredentials;
    private readonly TimeProvider _timeProvider;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TokenCacheEntry> _tokenCache = new(StringComparer.Ordinal);
    private bool _disposed;

    public FiskalyAuthenticationService(
        IHttpClientFactory httpClientFactory,
        IOptions<FiskalyConfiguration> options,
        JsonSerializerOptions serializerOptions,
        ILogger<FiskalyAuthenticationService> logger,
        TimeProvider timeProvider)
    {
        _httpClient = httpClientFactory.CreateClient("FiskalyAuth");
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        FiskalyConfiguration config = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _defaultCredentials = new ApiKeyCredentials(
            ApiKey.From(config.ApiKey),
            ApiSecret.From(config.ApiSecret));
    }

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        GetAccessTokenAsync(_defaultCredentials, cancellationToken);

    public async Task<string> GetAccessTokenAsync(
        IFiskalyCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        AuthenticationResponse response = await AuthenticateAsync(credentials, cancellationToken).ConfigureAwait(false);
        return response.AccessToken.Value;
    }

    public async Task<AuthenticationResponse> AuthenticateAsync(
        IFiskalyCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(credentials);

        AuthenticationPayload payload = credentials.CreatePayload();
        string cacheKey = BuildCacheKey(payload);

        SemaphoreSlim lockForThisKey = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        bool lockAcquired = false;
        try
        {
            await lockForThisKey.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockAcquired = true; // Only set after successful acquisition to prevent corruption on cancellation

            if (_tokenCache.TryGetValue(cacheKey, out TokenCacheEntry? entry) && entry.IsValid(_timeProvider))
            {
                _logger.LogCacheHit(payload.Kind, (entry.ExpiresAtUtc - _timeProvider.GetUtcNow()).TotalSeconds);
                return entry.Response;
            }

            _logger.LogTokenMissingOrExpired(payload.Kind);

            AuthenticationResponse response = await AuthenticateAsync(payload, cancellationToken).ConfigureAwait(false);
            DateTimeOffset expiry = CalculateExpiry(response.ExpiresIn, _timeProvider);
            TokenCacheEntry cacheEntry = new(response, expiry);

            _tokenCache[cacheKey] = cacheEntry;

            _logger.LogTokenObtained(payload.Kind, expiry);

            return cacheEntry.Response;
        }
        finally
        {
            if (lockAcquired)
            {
                lockForThisKey.Release();
            }
        }
    }

    private async Task<AuthenticationResponse> AuthenticateAsync(
        AuthenticationPayload payload,
        CancellationToken cancellationToken)
    {
        _logger.LogAuthenticationRequest(payload.Kind);

        using JsonContent content = JsonContent.Create(payload, payload.GetType(), options: _serializerOptions);

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "auth")
        {
            Content = content
        };

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogAuthenticationFailed(payload.Kind, response.StatusCode, errorBody);

            throw new FiskalyApiException(
                response.StatusCode,
                "Failed to authenticate with fiskaly API.",
                errorBody);
        }

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        AuthenticationResponse authResponse = await JsonSerializer.DeserializeAsync<AuthenticationResponse>(
                                                      responseStream,
                                                      _serializerOptions,
                                                      cancellationToken)
                                                  .ConfigureAwait(false)
                                              ?? throw new FiskalyException("Failed to deserialize authentication response.");

        _logger.LogAuthenticationSucceeded(payload.Kind, authResponse.ExpiresIn);

        return authResponse;
    }

    private static string BuildCacheKey(AuthenticationPayload payload) =>
        payload switch
        {
            ApiKeyAuthenticationPayload apiKey => $"api_key::{apiKey.ApiKey.Value}",
            RefreshTokenAuthenticationPayload refresh => $"refresh::{refresh.RefreshToken.Value}",
            _ => payload.Kind
        };

    private static DateTimeOffset CalculateExpiry(int expiresInSeconds, TimeProvider timeProvider)
    {
        int duration = Math.Max(expiresInSeconds, 60) - 60;
        return timeProvider.GetUtcNow().AddSeconds(duration);
    }

    private sealed record TokenCacheEntry(AuthenticationResponse Response, DateTimeOffset ExpiresAtUtc)
    {
        public bool IsValid(TimeProvider timeProvider) => timeProvider.GetUtcNow() < ExpiresAtUtc;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (SemaphoreSlim semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }

        _locks.Clear();
        _tokenCache.Clear();
    }
}
