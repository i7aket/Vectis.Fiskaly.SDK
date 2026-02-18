using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RichardSzalay.MockHttp;
using Vectis.Fiskaly.SDK.Authentication;
using Vectis.Fiskaly.SDK.Authentication.Credentials;
using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Configuration;
using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.UnitTests.Authentication;

/// <summary>
/// Unit tests for <see cref="FiskalyAuthenticationService"/>.
/// Tests token caching, expiry handling, thread-safety, and time abstraction.
/// </summary>
public class FiskalyAuthenticationServiceTests
{
    private const string ValidApiKey = "test_api_key";
    private const string ValidApiSecret = "1234567890123456789012345678901234567890123";  // Exactly 43 chars
    private const string SampleAccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0IjoiMCJ9.SAMPLE0";
    private const string SampleRefreshToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyIjoiMCJ9.SAMPLE0";

    private static string BuildAccessToken(string marker) => $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{AccessPayload(marker)}.SIGN{marker}";
    private static string BuildRefreshToken(string marker) => $"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.{RefreshPayload(marker)}.REF{marker}";

    private static string AccessPayload(string marker) => marker switch
    {
        "0" => "eyJ0IjoiMCJ9",
        "1" => "eyJ0IjoiMSJ9",
        "2" => "eyJ0IjoiMiJ9",
        "3" => "eyJ0IjoiMyJ9",
        "4" => "eyJ0IjoiNCJ9",
        _ => "eyJ0IjoiMDAwIn0"
    };

    private static string RefreshPayload(string marker) => marker switch
    {
        "0" => "eyJyIjoiMCJ9",
        "1" => "eyJyIjoiMSJ9",
        "2" => "eyJyIjoiMiJ9",
        "3" => "eyJyIjoiMyJ9",
        "4" => "eyJyIjoiNCJ9",
        _ => "eyJyIjoiMDAwIn0"
    };

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        AuthenticationResponse authResponse = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(SampleAccessToken),
            RefreshToken = RefreshToken.From(SampleRefreshToken),
            ExpiresIn = 3600 // 1 hour
        };

        mockHttp.When("*/auth")
            .Respond("application/json", JsonSerializer.Serialize(authResponse));

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        // Act
        string token = await service.GetAccessTokenAsync();

        // Assert
        Assert.Equal(SampleAccessToken, token);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task AuthenticateAsync_ReturnsResponseAndUsesCache()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        AuthenticationResponse authResponse = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(SampleAccessToken),
            RefreshToken = RefreshToken.From(SampleRefreshToken),
            ExpiresIn = 3600,
            Claims = new AccessTokenClaims
            {
                Environment = "TEST",
                OrganizationId = OrganizationId.From("7b3e4f8a-1234-4abc-9def-123456789012")
            }
        };

        int callCount = 0;
        mockHttp.When("*/auth")
            .Respond(_ =>
            {
                callCount++;
                return new StringContent(JsonSerializer.Serialize(authResponse), System.Text.Encoding.UTF8, "application/json");
            });

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        ApiKeyCredentials credentials = new ApiKeyCredentials(
            ApiKey.From(ValidApiKey),
            ApiSecret.From(ValidApiSecret));

        // Act
        AuthenticationResponse first = await service.AuthenticateAsync(credentials);

        fakeTime.Advance(TimeSpan.FromMinutes(10));

        AuthenticationResponse second = await service.AuthenticateAsync(credentials);

        // Assert
        Assert.Equal(1, callCount); // Only one HTTP call thanks to caching
        Assert.True(object.ReferenceEquals(first, second));
        Assert.Equal(SampleAccessToken, first.AccessToken.Value);
        Assert.Equal(SampleRefreshToken, first.RefreshToken!.Value.Value);
        Assert.Equal("TEST", first.Claims!.Environment);
        Assert.Equal("7b3e4f8a-1234-4abc-9def-123456789012", first.Claims.OrganizationId!.Value.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_UsesCachedToken_WhenNotExpired()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        AuthenticationResponse authResponse = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(SampleAccessToken),
            RefreshToken = RefreshToken.From(SampleRefreshToken),
            ExpiresIn = 3600 // 1 hour
        };

        int callCount = 0;
        mockHttp.When("*/auth")
            .Respond(_ =>
            {
                callCount++;
                return new StringContent(JsonSerializer.Serialize(authResponse), System.Text.Encoding.UTF8, "application/json");
            });

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        // Act: First call - authenticates and caches
        string token1 = await service.GetAccessTokenAsync();

        // Advance time by 30 minutes (still valid - 30 minutes remaining)
        fakeTime.Advance(TimeSpan.FromMinutes(30));

        // Second call - should use cache (no HTTP request)
        string token2 = await service.GetAccessTokenAsync();

        // Assert
        Assert.Equal(SampleAccessToken, token1);
        Assert.Equal(token1, token2);
        Assert.Equal(1, callCount); // Only 1 HTTP call
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_RefreshesToken_WhenExpired()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        AuthenticationResponse authResponse1 = new AuthenticationResponse
        {
            AccessToken = AccessToken.From("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0IjoiMSJ9._-t7L4aln0"),
            RefreshToken = RefreshToken.From("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyIjoiMSJ9.DUyFQJvZ8rUY"),
            ExpiresIn = 3600 // 1 hour
        };

        AuthenticationResponse authResponse2 = new AuthenticationResponse
        {
            AccessToken = AccessToken.From("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0IjoiMiJ9.B0_j9H82H0"),
            RefreshToken = RefreshToken.From("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyIjoiMiJ9.eIx3n8xyzwQ"),
            ExpiresIn = 3600
        };

        int callCount = 0;
        mockHttp.When("*/auth")
            .Respond(_ =>
            {
                callCount++;
                AuthenticationResponse response = callCount == 1 ? authResponse1 : authResponse2;
                return new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json");
            });

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        // Act: First call
        string token1 = await service.GetAccessTokenAsync();

        // Advance time by 61 minutes (past expiry - 3600s - 60s buffer = 59 minutes)
        fakeTime.Advance(TimeSpan.FromMinutes(61));

        // Second call - should re-authenticate
        string token2 = await service.GetAccessTokenAsync();

        // Assert
        Assert.Equal(authResponse1.AccessToken.Value, token1);
        Assert.Equal(authResponse2.AccessToken.Value, token2);
        Assert.Equal(2, callCount); // 2 HTTP calls
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_ConcurrentRequests_ReturnsSameToken()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        AuthenticationResponse authResponse = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(SampleAccessToken),
            RefreshToken = RefreshToken.From(SampleRefreshToken),
            ExpiresIn = 3600
        };

        int callCount = 0;
        mockHttp.When("*/auth")
            .Respond(_ =>
            {
                callCount++;
                return new StringContent(JsonSerializer.Serialize(authResponse), System.Text.Encoding.UTF8, "application/json");
            });

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        // Act: 10 concurrent requests
        Task<string>[] tasks = Enumerable.Range(0, 10)
            .Select(_ => service.GetAccessTokenAsync())
            .ToArray();

        string[] tokens = await Task.WhenAll(tasks);

        // Assert: All tasks get the same token
        Assert.All(tokens, token => Assert.Equal(SampleAccessToken, token));

        // Only 1 HTTP request should be made (thread-safe caching via SemaphoreSlim)
        Assert.Equal(1, callCount);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_ThrowsFiskalyApiException_OnAuthFailure()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        mockHttp.When("*/auth")
            .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"error\": \"Invalid credentials\"}");

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => service.GetAccessTokenAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Contains("Failed to authenticate with fiskaly API", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_HandlesMultipleCredentials_Separately()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        AuthenticationResponse authResponse1 = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(BuildAccessToken("1")),
            RefreshToken = RefreshToken.From(BuildRefreshToken("1")),
            ExpiresIn = 3600
        };

        AuthenticationResponse authResponse2 = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(BuildAccessToken("2")),
            RefreshToken = RefreshToken.From(BuildRefreshToken("2")),
            ExpiresIn = 3600
        };

        int callCount = 0;
        mockHttp.When("*/auth")
            .Respond(_ =>
            {
                callCount++;
                AuthenticationResponse response = callCount == 1 ? authResponse1 : authResponse2;
                return new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json");
            });

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        ApiKeyCredentials credentials1 = new ApiKeyCredentials(
            ApiKey.From("key_1_test"),
            ApiSecret.From(ValidApiSecret));

        ApiKeyCredentials credentials2 = new ApiKeyCredentials(
            ApiKey.From("key_2_test"),
            ApiSecret.From(ValidApiSecret));

        // Act
        string token1 = await service.GetAccessTokenAsync(credentials1);
        string token2 = await service.GetAccessTokenAsync(credentials2);

        // Act: Request tokens again (should use cache)
        string token1Cached = await service.GetAccessTokenAsync(credentials1);
        string token2Cached = await service.GetAccessTokenAsync(credentials2);

        // Assert: Different credentials get different tokens
        Assert.Equal(authResponse1.AccessToken.Value, token1);
        Assert.Equal(authResponse2.AccessToken.Value, token2);

        // Cached tokens match original tokens
        Assert.Equal(token1, token1Cached);
        Assert.Equal(token2, token2Cached);

        // Only 2 HTTP calls (one per credential)
        Assert.Equal(2, callCount);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_WithNullCredentials_ThrowsArgumentNullException()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));
        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetAccessTokenAsync(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_CalculatesExpiry_WithSixtySecondBuffer()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        AuthenticationResponse authResponse1 = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(BuildAccessToken("3")),
            RefreshToken = RefreshToken.From(BuildRefreshToken("3")),
            ExpiresIn = 3600 // 1 hour
        };

        AuthenticationResponse authResponse2 = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(BuildAccessToken("4")),
            RefreshToken = RefreshToken.From(BuildRefreshToken("4")),
            ExpiresIn = 3600
        };

        int callCount = 0;
        mockHttp.When("*/auth")
            .Respond(_ =>
            {
                callCount++;
                AuthenticationResponse response = callCount == 1 ? authResponse1 : authResponse2;
                return new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json");
            });

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        // Act: First call
        string token1 = await service.GetAccessTokenAsync();

        // Advance to 58m 59s (still within the 59-minute window before expiry)
        // Token should still be valid (3600s - 60s buffer = 3540s = 59 minutes effective lifetime)
        fakeTime.Advance(TimeSpan.FromMinutes(58).Add(TimeSpan.FromSeconds(59)));
        string token2 = await service.GetAccessTokenAsync();

        // Assert: Same token (cached)
        Assert.Equal(token1, token2);
        Assert.Equal(1, callCount);

        // Advance 2 more seconds (total 59m 1s) - now expired
        fakeTime.Advance(TimeSpan.FromSeconds(2));

        string token3 = await service.GetAccessTokenAsync();

        // Assert: New token (re-authenticated)
        Assert.NotEqual(token1, token3);
        Assert.Equal(authResponse2.AccessToken.Value, token3);
        Assert.Equal(2, callCount);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task GetAccessTokenAsync_WithZeroExpiresIn_UsesMinimumSixtySeconds()
    {
        // Arrange
        MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
        FakeTimeProvider fakeTime = new FakeTimeProvider(new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero));

        AuthenticationResponse authResponse1 = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(BuildAccessToken("5")),
            RefreshToken = RefreshToken.From(BuildRefreshToken("5")),
            ExpiresIn = 0 // Invalid value - should default to 60s
        };

        AuthenticationResponse authResponse2 = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(BuildAccessToken("6")),
            RefreshToken = RefreshToken.From(BuildRefreshToken("6")),
            ExpiresIn = 3600
        };

        int callCount = 0;
        mockHttp.When("*/auth")
            .Respond(_ =>
            {
                callCount++;
                AuthenticationResponse response = callCount == 1 ? authResponse1 : authResponse2;
                return new StringContent(JsonSerializer.Serialize(response), System.Text.Encoding.UTF8, "application/json");
            });

        FiskalyAuthenticationService service = CreateService(mockHttp, fakeTime);

        // Act
        string token = await service.GetAccessTokenAsync();

        // Assert: Token is immediately expired (60s - 60s buffer = 0s)
        // So next call should re-authenticate
        fakeTime.Advance(TimeSpan.FromSeconds(1));

        string token2 = await service.GetAccessTokenAsync();

        Assert.NotEqual(token, token2);
        Assert.Equal(authResponse2.AccessToken.Value, token2);
        Assert.Equal(2, callCount);
    }

    #region Helper Methods

    private static FiskalyAuthenticationService CreateService(
        MockHttpMessageHandler mockHttp,
        FakeTimeProvider fakeTime)
    {
        MockHttpClientFactory httpClientFactory = new MockHttpClientFactory(mockHttp);

        FiskalyConfiguration config = new FiskalyConfiguration
        {
            ApiKey = ValidApiKey,
            ApiSecret = ValidApiSecret,
            BaseUrl = "https://kassensichv-middleware.fiskaly.com/api/v2/"
        };

        IOptions<FiskalyConfiguration> options = Options.Create(config);
        NullLogger<FiskalyAuthenticationService> logger = NullLogger<FiskalyAuthenticationService>.Instance;
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return new FiskalyAuthenticationService(
            httpClientFactory,
            options,
            jsonOptions,
            logger,
            fakeTime);
    }

    /// <summary>
    /// Mock IHttpClientFactory for testing.
    /// </summary>
    private class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly MockHttpMessageHandler _mockHttp;

        public MockHttpClientFactory(MockHttpMessageHandler mockHttp)
        {
            _mockHttp = mockHttp;
        }

        public HttpClient CreateClient(string name)
        {
            HttpClient client = _mockHttp.ToHttpClient();
            client.BaseAddress = new Uri("https://kassensichv-middleware.fiskaly.com/api/v2/");
            return client;
        }
    }

    #endregion
}
