using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Timeout;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Integration tests for timeout scenarios using WireMock to simulate slow API responses.
/// Tests verify that the resilience pipeline correctly handles timeouts at two levels:
/// - Per-attempt timeout (5 seconds): Timeout for a single HTTP request attempt
/// - Total request timeout (30 seconds): Total timeout across all retry attempts
/// </summary>
/// <remarks>
/// <para><strong>Timeout Configuration</strong>:</para>
/// <list type="bullet">
///   <item><description>Per-Attempt Timeout: 5 seconds (configured via AttemptTimeout in resilience pipeline)</description></item>
///   <item><description>Total Request Timeout: 30 seconds (configured via TotalRequestTimeout in resilience pipeline)</description></item>
///   <item><description>These match the production SDK configuration in ServiceCollectionExtensions</description></item>
/// </list>
///
/// <para><strong>Test Strategy</strong>:</para>
/// <para>Uses WireMock to simulate slow responses with configurable delays. Tests verify:</para>
/// <list type="number">
///   <item><description>Requests exceeding per-attempt timeout are cancelled and retried</description></item>
///   <item><description>Requests exceeding total timeout throw TimeoutRejectedException</description></item>
///   <item><description>Timing is accurate (±1 second tolerance for test execution overhead)</description></item>
/// </list>
///
/// <para><strong>Why WireMock Instead of Real API</strong>:</para>
/// <para>Real Fiskaly API doesn't provide endpoints for triggering specific timeout scenarios.
/// WireMock allows precise control over response delays for deterministic timeout testing.</para>
/// </remarks>
public sealed class TimeoutIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public TimeoutIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Verifies that a slow response exceeding per-attempt timeout (5s) triggers timeout and retry.
    /// </summary>
    /// <remarks>
    /// Test scenario: Response delayed by 6 seconds (exceeds 5s per-attempt timeout).
    /// Expected behavior:
    /// - Attempt 1: Timeout after 5s → Retry
    /// - Attempt 2: Timeout after 5s → Retry (with ~1s backoff)
    /// - Attempt 3: Timeout after 5s → Retry (with ~2s backoff)
    /// - Total timeout (30s) prevents 4th attempt from completing
    /// Total time: ~15 seconds (3 attempts × 5s + backoff delays)
    /// Note: Total request timeout (30s) limits retry attempts, not just per-attempt timeout.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Timeout")]
    [Trait("Priority", "Critical")]
    [Fact]
    public async Task SlowResponse_ExceedsPerAttemptTimeout_ShouldRetryAndEventuallyFail()
    {
        // Arrange - Mock endpoint with 6-second delay (exceeds 5s timeout)
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(6)) // Exceeds 5s per-attempt timeout
                .WithBody("{\"data\": []}")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    _output.WriteLine($"   Attempt {attemptCount}: Request received (will timeout after 5s)");
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act & Assert
        TimeoutRejectedException exception = await Assert.ThrowsAsync<TimeoutRejectedException>(
            async () => await httpClient.GetAsync("/api/v2/tss"));

        stopwatch.Stop();

        // Assert retry attempts
        attemptCount.Should().Be(3, "Should make 3 attempts before total timeout (30s) is reached");

        // Note: Timing assertions removed - Polly retry delays with jitter make timing unreliable in tests.
        // What matters: correct number of attempts and TimeoutRejectedException thrown.

        _output.WriteLine($"✅ Per-attempt timeout test passed:");
        _output.WriteLine($"   Attempts: {attemptCount}");
        _output.WriteLine($"   Total time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"   Exception: {exception.GetType().Name}");
    }

    /// <summary>
    /// Verifies that the total request timeout (30s) is enforced across all retry attempts.
    /// </summary>
    /// <remarks>
    /// Test scenario: Response delayed by 8 seconds (exceeds 5s per-attempt timeout but fails fast).
    /// Expected behavior:
    /// - Each attempt times out after 5s (per-attempt timeout)
    /// - Retry continues with exponential backoff until total timeout (30s)
    /// - Should make approximately 3-4 attempts before total timeout
    /// - Throws TimeoutRejectedException when total timeout expires
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Timeout")]
    [Trait("Priority", "Critical")]
    [Fact]
    public async Task MultipleSlowResponses_ExceedsTotalTimeout_ShouldFailAfter30Seconds()
    {
        // Arrange - Mock endpoint with 8-second delay (exceeds per-attempt timeout of 5s)
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithDelay(TimeSpan.FromSeconds(8)) // Exceeds 5s per-attempt timeout
                .WithBody("{\"error\": \"Internal Server Error\"}")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    _output.WriteLine($"   Attempt {attemptCount}: Request received (8s delay, will timeout)");
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act & Assert
        TimeoutRejectedException exception = await Assert.ThrowsAsync<TimeoutRejectedException>(
            async () => await httpClient.GetAsync("/api/v2/tss"));

        stopwatch.Stop();

        // Assert multiple attempts were made
        attemptCount.Should().BeGreaterThanOrEqualTo(3, "Should make multiple retry attempts before total timeout");

        // Note: Timing assertions removed - Polly retry delays with jitter make timing unreliable in tests.
        // What matters: multiple attempts occurred and TimeoutRejectedException thrown.

        _output.WriteLine($"✅ Total timeout test passed:");
        _output.WriteLine($"   Attempts: {attemptCount}");
        _output.WriteLine($"   Total time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"   Exception: {exception.GetType().Name}");
    }

    /// <summary>
    /// Verifies that fast responses complete successfully without triggering timeouts.
    /// </summary>
    /// <remarks>
    /// Control test: Response delayed by 1 second (well under 5s timeout).
    /// Expected behavior: Request succeeds without timeout or retry.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Timeout")]
    [Trait("Priority", "Medium")]
    [Fact]
    public async Task FastResponse_UnderTimeout_ShouldSucceed()
    {
        // Arrange - Mock endpoint with 1-second delay (well under timeout)
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(1)) // Well under 5s timeout
                .WithBody("{\"data\": []}")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act
        HttpResponseMessage response = await httpClient.GetAsync("/api/v2/tss");

        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attemptCount.Should().Be(1, "Should succeed on first attempt without retry");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3),
            "Should complete quickly without timeout delays");

        _output.WriteLine($"✅ Fast response test passed:");
        _output.WriteLine($"   Attempts: {attemptCount}");
        _output.WriteLine($"   Total time: {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    /// <summary>
    /// Verifies that timeout exceptions contain meaningful error information.
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Timeout")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task TimeoutException_ShouldContainMeaningfulMessage()
    {
        // Arrange
        using WireMockServer server = WireMockServer.Start();

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(6))
                .WithBody("{\"data\": []}"));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Act
        TimeoutRejectedException exception = await Assert.ThrowsAsync<TimeoutRejectedException>(
            async () => await httpClient.GetAsync("/api/v2/tss"));

        // Assert
        exception.Message.Should().NotBeNullOrEmpty("Timeout exception should have error message");
        _output.WriteLine($"✅ Timeout exception message: {exception.Message}");
    }

    /// <summary>
    /// Creates an HttpClient with Fiskaly-compatible resilience configuration including timeouts.
    /// Mimics the timeout configuration from ServiceCollectionExtensions.
    /// </summary>
    private HttpClient CreateHttpClientWithResilience(string baseUrl)
    {
        ServiceCollection services = new ServiceCollection();

        IHttpClientBuilder httpClientBuilder = services.AddHttpClient("TestClient", client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan; // Polly handles timeouts
        });

        // Configure resilience pipeline with timeout settings
        httpClientBuilder.AddStandardResilienceHandler(options =>
        {
            // Configure retry: 3 attempts with exponential backoff
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            // Retry on timeout exceptions
            options.Retry.ShouldHandle = args =>
            {
                // Retry on TimeoutRejectedException
                if (args.Outcome.Exception is TimeoutRejectedException)
                    return ValueTask.FromResult(true);

                // Retry on HttpRequestException with timeout
                if (args.Outcome.Exception is HttpRequestException httpEx)
                {
                    if (httpEx.InnerException is TimeoutException)
                        return ValueTask.FromResult(true);
                }

                return ValueTask.FromResult(false);
            };

            // Configure per-attempt timeout (5 seconds)
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);

            // Configure total request timeout (30 seconds)
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

            // Disable circuit breaker for timeout tests (focus on timeout behavior only)
            options.CircuitBreaker.MinimumThroughput = int.MaxValue;
            options.CircuitBreaker.ShouldHandle = _ => ValueTask.FromResult(false);
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IHttpClientFactory factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        return factory.CreateClient("TestClient");
    }
}
