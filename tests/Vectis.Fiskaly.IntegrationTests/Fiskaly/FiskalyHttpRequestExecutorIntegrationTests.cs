using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vectis.Fiskaly.SDK;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Tss;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Responses;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using WireMock;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Integration tests verifying FiskalyHttpRequestExecutor fix (commit bc31269).
/// Tests that the executor receives properly configured HttpClient with full pipeline:
/// - Base address configuration
/// - JWT authentication header
/// - Resilience policies (retry, circuit breaker)
/// - Error handling (FiskalyApiException parsing)
/// </summary>
/// <remarks>
/// <para>This test proves the critical bug fix where executor was receiving unconfigured HttpClient.</para>
/// <para>Fix: Executor now accepts HttpClient as parameter, clients explicitly pass configured instance.</para>
/// </remarks>
public sealed class FiskalyHttpRequestExecutorIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public FiskalyHttpRequestExecutorIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Verifies that TssClient uses configured HttpClient with full pipeline.
    /// Tests all aspects: base URL, JWT auth, retry policy, and error handling.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task TssClient_UsesConfiguredHttpClient_WithFullPipeline()
    {
        // Arrange - Setup WireMock server
        using WireMockServer server = WireMockServer.Start();

        // Mock authentication endpoint (required for JWT token acquisition)
        // Auth service uses relative URL "auth", so with BaseUrl="/api/v2/" it becomes "/api/v2/auth"
        server
            .Given(Request.Create()
                .WithPath("/api/v2/auth")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""access_token"": ""eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0In0.test"",
                    ""token_type"": ""Bearer"",
                    ""expires_in"": 3600
                }"));

        // First 2 attempts fail with 503 (should retry), 3rd succeeds
        // Using WireMock scenarios to handle stateful responses across multiple requests

        // Attempt 1: Return 503 with E_TSS_LOCKED (Transient error that triggers retry), move to state "FirstRetry"
        server
            .Given(Request.Create()
                .WithPath("/api/v2/tss/*")
                .UsingGet())
            .InScenario("retry-test")
            .WillSetStateTo("FirstRetry")
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""code"": ""E_TSS_LOCKED"",
                    ""message"": ""TSS temporarily locked"",
                    ""status_code"": 503,
                    ""error"": ""Service Unavailable""
                }"));

        // Attempt 2: When in "FirstRetry", return 503 with E_TSS_LOCKED (Transient error that triggers retry), move to state "SecondRetry"
        server
            .Given(Request.Create()
                .WithPath("/api/v2/tss/*")
                .UsingGet())
            .InScenario("retry-test")
            .WhenStateIs("FirstRetry")
            .WillSetStateTo("SecondRetry")
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""code"": ""E_TSS_LOCKED"",
                    ""message"": ""TSS temporarily locked"",
                    ""status_code"": 503,
                    ""error"": ""Service Unavailable""
                }"));

        // Attempt 3: When in "SecondRetry", return 200 success
        server
            .Given(Request.Create()
                .WithPath("/api/v2/tss/*")
                .UsingGet())
            .InScenario("retry-test")
            .WhenStateIs("SecondRetry")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""_id"": ""12345678-1234-4234-8234-123456789012"",
                    ""_type"": ""TSS"",
                    ""description"": ""Test TSS"",
                    ""state"": ""CREATED"",
                    ""admin_puk"": ""test-admin-puk"",
                    ""_env"": ""TEST""
                }"));

        // Setup real DI container with AddFiskaly
        ServiceCollection services = new ServiceCollection();

        // Build minimal configuration
        Dictionary<string, string?> configDict = new Dictionary<string, string?>
        {
            ["Fiskaly:ApiKey"] = "test_1234567890abcdefghijklmnopqr_test",
            ["Fiskaly:ApiSecret"] = "test1234567890abcdefghijklmnopqrstuvwxyz123",
            ["Fiskaly:BaseUrl"] = $"{server.Url}/api/v2/" // Point to WireMock
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Register Fiskaly SDK with REAL DI registration
        services.AddFiskaly(configuration, configure: options =>
        {
            // Override retry to be faster for testing
            options.TssClient.RetryCount = 2; // Will retry twice (3 total attempts)
            options.TssClient.CategoryDelays.TransientDelaySeconds = 1; // Minimum valid value
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        ITssClient tssClient = serviceProvider.GetRequiredService<ITssClient>();

        // Act - Call GetTssAsync (will use executor internally)
        TssId tssId = TssId.From("12345678-1234-4234-8234-123456789012");
        TssResponse result = await tssClient.GetTssAsync(tssId);

        // Assert - Verify full pipeline worked

        // 1. ✅ Base address was set correctly (WireMock received requests at correct URL)
        // Use WireMock's request log to count TSS endpoint calls (excluding auth endpoint)
        List<ILogEntry> tssRequests = server.LogEntries
            .Where(e => e.RequestMessage.Path.StartsWith("/api/v2/tss/"))
            .ToList();
        int attemptCount = tssRequests.Count;
        attemptCount.Should().Be(3, "Should make 1 initial + 2 retry attempts before success");
        _output.WriteLine($"✅ Base address verified: {attemptCount} requests received by WireMock");

        // 2. ✅ JWT Authorization header present (auth handler added token)
        IRequestMessage lastRequest = tssRequests.Last().RequestMessage;
        lastRequest.Headers.Should().ContainKey("Authorization", "Authorization header should be present");
        string authHeader = lastRequest.Headers["Authorization"].ToString();
        authHeader.Should().StartWith("Bearer ", "Should use Bearer token");
        _output.WriteLine($"✅ JWT Authorization header verified: {authHeader[..Math.Min(30, authHeader.Length)]}...");

        // 3. ✅ Retry policy worked (E_TSS_LOCKED 503 → retry → success)
        _output.WriteLine($"✅ Retry policy verified: Failed 2 times with E_TSS_LOCKED (503), succeeded on 3rd attempt");

        // 4. ✅ Response deserialized correctly (executor's JSON handling worked)
        result.Should().NotBeNull("Should deserialize response");
        result.Id.HasValue.Should().BeTrue("Response should include TSS identifier");
        result.Id!.Value.Should().Be(tssId, "Should have correct TSS ID");
        result.State.Should().Be(TssState.Created, "Should have correct state");
        _output.WriteLine($"✅ Response deserialization verified: TSS {result.Id} with state {result.State}");

        // 5. ✅ Full pipeline integration verified
        _output.WriteLine("✅ COMPLETE: Full pipeline verified (base URL + JWT auth + retry + deserialization)");
    }

    /// <summary>
    /// Verifies that error responses are parsed correctly into FiskalyApiException.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task TssClient_ParsesErrorResponse_IntoFiskalyApiException()
    {
        // Arrange - Setup WireMock to return Fiskaly error
        using WireMockServer server = WireMockServer.Start();

        // Mock authentication endpoint (required for JWT token acquisition)
        // Auth service uses relative URL "auth", so with BaseUrl="/api/v2/" it becomes "/api/v2/auth"
        server
            .Given(Request.Create()
                .WithPath("/api/v2/auth")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""access_token"": ""eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0In0.test"",
                    ""token_type"": ""Bearer"",
                    ""expires_in"": 3600
                }"));

        server
            .Given(Request.Create()
                .WithPath("/api/v2/tss/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/json")
                .WithHeader("X-Correlation-ID", "test-correlation-123")
                .WithBody(@"{
                    ""code"": ""E_TSS_NOT_FOUND"",
                    ""message"": ""TSS with id test-tss-nonexistent not found"",
                    ""status_code"": 404,
                    ""error"": ""Not Found""
                }"));

        // Setup DI with real AddFiskaly
        ServiceCollection services = new ServiceCollection();
        Dictionary<string, string?> configDict = new Dictionary<string, string?>
        {
            ["Fiskaly:ApiKey"] = "test_1234567890abcdefghijklmnopqr_test",
            ["Fiskaly:ApiSecret"] = "test1234567890abcdefghijklmnopqrstuvwxyz123",
            ["Fiskaly:BaseUrl"] = $"{server.Url}/api/v2/"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        services.AddFiskaly(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        ITssClient tssClient = serviceProvider.GetRequiredService<ITssClient>();

        // Act & Assert - Should throw FiskalyApiException with parsed error
        TssId tssId = TssId.From("99999999-9999-4999-9999-999999999999");
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await tssClient.GetTssAsync(tssId));

        // Verify error parsing worked
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound, "Should have 404 status");
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_TSS_NOT_FOUND, "Should parse error code");
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent, "Should categorize correctly");
        exception.IsRetryable.Should().BeFalse("Permanent errors should not be retryable");
        exception.CorrelationId.Should().NotBeNullOrEmpty("Should have correlation ID (auto-generated)");

        _output.WriteLine("✅ Error parsing verified: FiskalyApiException with all properties");
        _output.WriteLine($"   ErrorCode: {exception.ErrorCode}");
        _output.WriteLine($"   Category: {exception.Category}");
        _output.WriteLine($"   CorrelationId: {exception.CorrelationId}");
    }

    /// <summary>
    /// Verifies that circuit breaker opens after threshold failures.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task TssClient_CircuitBreaker_OpensAfterThreshold()
    {
        // Arrange - Setup WireMock to always fail
        using WireMockServer server = WireMockServer.Start();

        // Mock authentication endpoint (required for JWT token acquisition)
        server
            .Given(Request.Create()
                .WithPath("/api/v2/auth")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""access_token"": ""eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0In0.test"",
                    ""token_type"": ""Bearer"",
                    ""expires_in"": 3600
                }"));

        server
            .Given(Request.Create()
                .WithPath("/api/v2/tss/*")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""error"":""Internal error""}"));

        // Setup DI with circuit breaker enabled
        ServiceCollection services = new ServiceCollection();
        Dictionary<string, string?> configDict = new Dictionary<string, string?>
        {
            ["Fiskaly:ApiKey"] = "test_1234567890abcdefghijklmnopqr_test",
            ["Fiskaly:ApiSecret"] = "test1234567890abcdefghijklmnopqrstuvwxyz123",
            ["Fiskaly:BaseUrl"] = $"{server.Url}/api/v2/"
        };
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        services.AddFiskaly(configuration, configure: options =>
        {
            options.TssClient.CircuitBreakerThreshold = 3; // Open after 3 failures
            options.TssClient.RetryCount = 1; // Minimum valid value (1 attempt total, no retries)
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        ITssClient tssClient = serviceProvider.GetRequiredService<ITssClient>();

        // Act - Make 5 requests (should fail 3, then circuit opens)
        TssId tssId = TssId.From("88888888-8888-4888-8888-888888888888");
        List<Exception> exceptions = new List<Exception>();

        for (int i = 0; i < 5; i++)
        {
            try
            {
                await tssClient.GetTssAsync(tssId);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                _output.WriteLine($"Request {i + 1} failed: {ex.GetType().Name}");
            }
        }

        // Assert
        exceptions.Should().HaveCount(5, "All 5 requests should fail");

        // Circuit breaker behavior:
        // - First 3 requests hit the server (circuit closed)
        // - After 3 failures, circuit opens
        // - Remaining requests fail immediately without hitting server
        List<ILogEntry> tssRequests = server.LogEntries
            .Where(e => e.RequestMessage.Path.StartsWith("/api/v2/tss/"))
            .ToList();
        int attemptCount = tssRequests.Count;
        attemptCount.Should().BeLessThanOrEqualTo(3,
            "Circuit breaker should open after threshold, preventing further server calls");

        _output.WriteLine($"✅ Circuit breaker verified: Opened after {attemptCount} failures (threshold: 3)");
        _output.WriteLine($"   Total requests: 5, Server calls: {attemptCount}");
    }
}
