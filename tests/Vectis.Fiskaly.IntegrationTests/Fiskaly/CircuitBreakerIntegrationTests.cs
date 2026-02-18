using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Integration tests for circuit breaker behavior using WireMock.
/// Tests verify that the circuit breaker protects against cascading failures by:
/// - Opening after consecutive failures
/// - Rejecting requests while open
/// - Allowing test requests in half-open state
/// - Closing after successful test request
/// </summary>
/// <remarks>
/// <para><strong>Circuit Breaker States</strong>:</para>
/// <list type="bullet">
///   <item><description><strong>Closed</strong>: Normal operation, requests flow through</description></item>
///   <item><description><strong>Open</strong>: Too many failures, all requests rejected immediately</description></item>
///   <item><description><strong>Half-Open</strong>: Testing if service recovered, allows limited requests</description></item>
/// </list>
///
/// <para><strong>Circuit Breaker Configuration (matches SDK)</strong>:</para>
/// <list type="bullet">
///   <item><description>Failure Threshold: 50% error rate</description></item>
///   <item><description>Minimum Throughput: 10 requests</description></item>
///   <item><description>Break Duration: 5 seconds</description></item>
///   <item><description>Sampling Duration: 30 seconds</description></item>
/// </list>
///
/// <para><strong>Test Strategy</strong>:</para>
/// <para>Uses WireMock to simulate infrastructure errors (500 Internal Server Error).
/// Tests verify circuit breaker state transitions and timing.</para>
///
/// <para><strong>Why WireMock</strong>:</para>
/// <para>Real API infrastructure failures are unpredictable and rare.
/// WireMock allows controlled failure simulation for deterministic circuit breaker testing.</para>
/// </remarks>
public sealed class CircuitBreakerIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public CircuitBreakerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Verifies that circuit breaker opens after consecutive infrastructure failures.
    /// </summary>
    /// <remarks>
    /// Test scenario: Generate 15 consecutive 500 errors (exceeds 10 minimum throughput).
    /// Expected behavior:
    /// - First 10-12 requests: Circuit closed, requests reach backend (with retries)
    /// - After threshold: Circuit opens, requests rejected immediately with BrokenCircuitException
    /// Circuit opens because: 100% failure rate, >= 10 requests (minimum throughput)
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "CircuitBreaker")]
    [Trait("Priority", "Medium")]
    [Fact]
    public async Task CircuitBreaker_OpensAfterConsecutiveFailures()
    {
        // Arrange - Mock endpoint that always returns 500
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("{\"error\": \"Internal Server Error\"}")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    _output.WriteLine($"   Backend request {attemptCount}: Returning 500 error");
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Act - Make multiple requests to trip circuit breaker
        List<Exception> exceptions = new List<Exception>();
        int brokenCircuitExceptions = 0;
        int httpRequestExceptions = 0;

        for (int i = 1; i <= 15; i++)
        {
            try
            {
                _output.WriteLine($"\n▶ Request {i}:");
                await httpClient.GetAsync("/api/v2/tss");
            }
            catch (BrokenCircuitException ex)
            {
                brokenCircuitExceptions++;
                _output.WriteLine($"   ⚡ Circuit OPEN - Request rejected: {ex.Message}");
                exceptions.Add(ex);
            }
            catch (HttpRequestException ex)
            {
                httpRequestExceptions++;
                _output.WriteLine($"   ❌ Request failed (circuit closed): {ex.Message}");
                exceptions.Add(ex);
            }

            // Small delay between requests to avoid overwhelming the circuit breaker
            await Task.Delay(100);
        }

        // Assert
        _output.WriteLine($"\n📊 Results:");
        _output.WriteLine($"   Backend attempts: {attemptCount}");
        _output.WriteLine($"   HttpRequestExceptions: {httpRequestExceptions} (circuit closed)");
        _output.WriteLine($"   BrokenCircuitExceptions: {brokenCircuitExceptions} (circuit open)");

        // Circuit breaker should have opened (at least some requests rejected)
        brokenCircuitExceptions.Should().BeGreaterThan(0,
            "Circuit breaker should open and reject requests after consecutive failures");

        // Backend should not receive all 15 requests (some rejected by open circuit)
        attemptCount.Should().BeLessThan(60,
            "Circuit breaker should prevent some requests from reaching backend");

        _output.WriteLine($"\n✅ Circuit breaker opened after failures");
    }

    /// <summary>
    /// Verifies that circuit breaker resets to half-open state after break duration.
    /// </summary>
    /// <remarks>
    /// Test scenario:
    /// 1. Trip circuit breaker with failures
    /// 2. Wait for break duration (5 seconds)
    /// 3. Make successful request to close circuit
    /// Expected behavior: Circuit transitions from Open → Half-Open → Closed
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "CircuitBreaker")]
    [Trait("Priority", "Medium")]
    [Fact]
    public async Task CircuitBreaker_ResetsAfterBreakDuration()
    {
        // Arrange - Mock endpoint that fails initially, then succeeds
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;
        bool shouldSucceed = false;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithCallback(request =>
                {
                    attemptCount++;

                    if (shouldSucceed)
                    {
                        _output.WriteLine($"   Backend request {attemptCount}: Returning 200 OK");
                        return new WireMock.ResponseMessage
                        {
                            StatusCode = 200
                        };
                    }

                    _output.WriteLine($"   Backend request {attemptCount}: Returning 500 error");
                    return new WireMock.ResponseMessage
                    {
                        StatusCode = 500
                    };
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Step 1: Trip circuit breaker with failures
        _output.WriteLine("📍 STEP 1: Tripping circuit breaker...");
        for (int i = 1; i <= 12; i++)
        {
            try
            {
                await httpClient.GetAsync("/api/v2/tss");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"   Request {i}: {ex.GetType().Name}");
            }
            await Task.Delay(50);
        }

        // Verify circuit is open
        bool circuitOpen = false;
        try
        {
            await httpClient.GetAsync("/api/v2/tss");
        }
        catch (BrokenCircuitException)
        {
            circuitOpen = true;
            _output.WriteLine($"   ✅ Circuit confirmed OPEN");
        }

        circuitOpen.Should().BeTrue("Circuit should be open after consecutive failures");

        // Step 2: Wait for break duration (5 seconds + 1 second buffer)
        _output.WriteLine($"\n📍 STEP 2: Waiting for break duration (6 seconds)...");
        Stopwatch stopwatch = Stopwatch.StartNew();
        await Task.Delay(TimeSpan.FromSeconds(6));
        stopwatch.Stop();
        _output.WriteLine($"   Waited {stopwatch.Elapsed.TotalSeconds:F2}s");

        // Step 3: Switch to success mode and make request
        _output.WriteLine($"\n📍 STEP 3: Testing circuit recovery...");
        shouldSucceed = true;

        // Circuit should be half-open, allow test request
        HttpResponseMessage response = await httpClient.GetAsync("/api/v2/tss");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Request should succeed after break duration (circuit half-open/closed)");

        _output.WriteLine($"   ✅ Circuit recovered: {response.StatusCode}");
        _output.WriteLine($"\n✅ Circuit breaker reset after break duration");
    }

    /// <summary>
    /// Verifies that circuit breaker only applies to infrastructure errors, not permanent errors.
    /// </summary>
    /// <remarks>
    /// Test scenario: Make 15 requests that return 404 Not Found (permanent error).
    /// Expected behavior: Circuit should NOT open (permanent errors don't trip circuit breaker).
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "CircuitBreaker")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task CircuitBreaker_DoesNotOpen_ForPermanentErrors()
    {
        // Arrange - Mock endpoint that always returns 404 (permanent error)
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithBody("{\"error\": \"Not Found\"}")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Act - Make multiple requests (404 errors should not trip circuit breaker)
        int brokenCircuitCount = 0;
        int notFoundCount = 0;

        for (int i = 1; i <= 15; i++)
        {
            try
            {
                await httpClient.GetAsync("/api/v2/tss");
            }
            catch (BrokenCircuitException)
            {
                brokenCircuitCount++;
                _output.WriteLine($"   Request {i}: BrokenCircuitException");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                notFoundCount++;
                _output.WriteLine($"   Request {i}: 404 Not Found");
            }

            await Task.Delay(50);
        }

        // Assert
        brokenCircuitCount.Should().Be(0,
            "Circuit breaker should NOT open for permanent errors (404)");

        notFoundCount.Should().Be(15,
            "All 15 requests should receive 404 errors (no circuit breaker interference)");

        attemptCount.Should().Be(15,
            "All requests should reach backend (no circuit breaker blocking)");

        _output.WriteLine($"\n✅ Circuit breaker correctly ignores permanent errors:");
        _output.WriteLine($"   404 errors: {notFoundCount}");
        _output.WriteLine($"   Circuit never opened: {brokenCircuitCount} BrokenCircuitExceptions");
    }

    /// <summary>
    /// Verifies that circuit breaker remains closed with mixed success/failure results using permanent errors.
    /// </summary>
    /// <remarks>
    /// Test scenario: 50% success rate using permanent errors (404 Not Found).
    /// Expected behavior: Circuit remains closed because permanent errors don't trigger circuit breaker.
    ///
    /// Note: Uses 404 (permanent error) instead of 5xx to avoid retry amplification.
    /// Permanent errors are NOT retryable per SDK design (FiskalyResiliencePredicates.IsPermanent).
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "CircuitBreaker")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task CircuitBreaker_RemainsClosedWithMixedResults()
    {
        // Arrange - Mock endpoint that alternates success/failure
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithCallback(request =>
                {
                    attemptCount++;

                    // Alternate success and failure (50% success rate using permanent errors to avoid retry amplification)
                    if (attemptCount % 2 == 0)
                    {
                        _output.WriteLine($"   Request {attemptCount}: 200 OK");
                        return new WireMock.ResponseMessage
                        {
                            StatusCode = 200
                        };
                    }

                    _output.WriteLine($"   Request {attemptCount}: 404 Not Found (permanent error)");
                    return new WireMock.ResponseMessage
                    {
                        StatusCode = 404
                    };
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Act - Make 20 requests (50% failure rate, should not trip circuit)
        int successCount = 0;
        int failureCount = 0;
        int brokenCircuitCount = 0;

        for (int i = 1; i <= 20; i++)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync("/api/v2/tss");
                if (response.IsSuccessStatusCode)
                    successCount++;
            }
            catch (BrokenCircuitException)
            {
                brokenCircuitCount++;
            }
            catch (HttpRequestException)
            {
                failureCount++;
            }

            await Task.Delay(50);
        }

        // Assert
        brokenCircuitCount.Should().Be(0,
            "Circuit breaker should NOT open with 50% permanent errors (permanent errors don't trigger circuit breaker)");

        successCount.Should().BeGreaterThan(5,
            "Should have successful requests");

        _output.WriteLine($"\n✅ Circuit breaker remained closed with mixed results:");
        _output.WriteLine($"   Successes: {successCount}");
        _output.WriteLine($"   Failures: {failureCount}");
        _output.WriteLine($"   Circuit never opened: {brokenCircuitCount} BrokenCircuitExceptions");
    }

    /// <summary>
    /// Verifies that circuit breaker remains closed with low failure rate despite retry amplification.
    /// </summary>
    /// <remarks>
    /// Test scenario: 12.5% base failure rate with retryable errors (500 Internal Server Error).
    /// Expected behavior: Circuit remains closed because amplified failure rate stays below 50% threshold.
    ///
    /// Retry amplification calculation:
    /// - Base failures: 2-3 out of 24 requests (~12.5%)
    /// - Each failure retried 3 times (4 total attempts)
    /// - Amplified failures: 3 × 4 = 12 attempts
    /// - Total attempts: 21 successes + 12 failures = 33
    /// - Amplified failure rate: 12/33 = 36% (below 50% threshold)
    ///
    /// This test demonstrates that SDK retry policy works correctly with circuit breaker
    /// when failure rate stays below threshold even after retry amplification.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "CircuitBreaker")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task CircuitBreaker_RemainsClosedWithLowFailureRate()
    {
        // Arrange - Mock endpoint with 12.5% failure rate (retryable errors)
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithCallback(request =>
                {
                    attemptCount++;

                    // 12.5% failure rate: fail every 8th request
                    // This tests retry amplification: 3 base failures × 4 retries = 12 failures
                    // Expected total: 21 successes + 12 failures = 33 attempts
                    // Failure rate: 12/33 = 36% (below 50% threshold)
                    if (attemptCount % 8 == 1)
                    {
                        _output.WriteLine($"   Request {attemptCount}: 500 Error (will retry)");
                        return new WireMock.ResponseMessage
                        {
                            StatusCode = 500
                        };
                    }

                    _output.WriteLine($"   Request {attemptCount}: 200 OK");
                    return new WireMock.ResponseMessage
                    {
                        StatusCode = 200
                    };
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Act - Make 24 requests (3 will fail, 21 will succeed)
        int successCount = 0;
        int failureCount = 0;
        int brokenCircuitCount = 0;

        for (int i = 1; i <= 24; i++)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync("/api/v2/tss");
                if (response.IsSuccessStatusCode)
                    successCount++;
            }
            catch (BrokenCircuitException)
            {
                brokenCircuitCount++;
                _output.WriteLine($"   Request {i}: BrokenCircuitException");
            }
            catch (HttpRequestException)
            {
                failureCount++;
            }

            await Task.Delay(50);
        }

        // Assert
        brokenCircuitCount.Should().Be(0,
            "Circuit breaker should remain closed with ~36% failure rate (below 50% threshold after retry amplification)");

        successCount.Should().BeGreaterThan(15,
            "Should have majority of successful requests");

        _output.WriteLine($"\n✅ Circuit breaker remained closed with low failure rate:");
        _output.WriteLine($"   Successes: {successCount}");
        _output.WriteLine($"   Failures (exhausted retries): {failureCount}");
        _output.WriteLine($"   Backend attempts: {attemptCount}");
        _output.WriteLine($"   Expected backend attempts: ~33 (21 successes + 12 retry attempts)");
        _output.WriteLine($"   Circuit never opened: {brokenCircuitCount} BrokenCircuitExceptions");
    }

    /// <summary>
    /// Creates an HttpClient with Fiskaly-compatible resilience configuration including circuit breaker.
    /// </summary>
    private HttpClient CreateHttpClientWithResilience(string baseUrl)
    {
        ServiceCollection services = new ServiceCollection();

        IHttpClientBuilder httpClientBuilder = services.AddHttpClient("TestClient", client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan; // Polly handles timeouts
        });

        httpClientBuilder.AddStandardResilienceHandler(options =>
        {
            // Configure retry: 3 attempts with exponential backoff
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            // Retry on 5xx errors (infrastructure failures)
            options.Retry.ShouldHandle = args =>
            {
                if (args.Outcome.Result != null)
                {
                    int statusCode = (int)args.Outcome.Result.StatusCode;
                    return ValueTask.FromResult(statusCode >= 500 && statusCode <= 599);
                }

                if (args.Outcome.Exception is HttpRequestException httpEx)
                {
                    if (httpEx.StatusCode.HasValue)
                    {
                        int code = (int)httpEx.StatusCode.Value;
                        return ValueTask.FromResult(code >= 500 && code <= 599);
                    }
                }

                return ValueTask.FromResult(false);
            };

            // Configure circuit breaker (matches SDK configuration)
            options.CircuitBreaker.FailureRatio = 0.5; // 50% failure rate
            options.CircuitBreaker.MinimumThroughput = 10; // At least 10 requests
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5); // Open for 5 seconds
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30); // 30-second window

            // Circuit breaker handles same errors as retry (5xx errors)
            options.CircuitBreaker.ShouldHandle = args =>
            {
                if (args.Outcome.Result != null)
                {
                    int statusCode = (int)args.Outcome.Result.StatusCode;
                    return ValueTask.FromResult(statusCode >= 500 && statusCode <= 599);
                }

                if (args.Outcome.Exception is HttpRequestException httpEx)
                {
                    if (httpEx.StatusCode.HasValue)
                    {
                        int code = (int)httpEx.StatusCode.Value;
                        return ValueTask.FromResult(code >= 500 && code <= 599);
                    }
                }

                return ValueTask.FromResult(false);
            };

            // Configure timeouts
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        // Add error handler to convert non-success responses to exceptions
        httpClientBuilder.AddHttpMessageHandler(() => new CircuitBreakerErrorHandler());

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IHttpClientFactory factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        return factory.CreateClient("TestClient");
    }

    /// <summary>
    /// Handler that converts non-success HTTP responses to exceptions.
    /// </summary>
    private sealed class CircuitBreakerErrorHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Throw exception for non-success status codes
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"HTTP {(int)response.StatusCode} {response.StatusCode}",
                    null,
                    response.StatusCode);
            }

            return response;
        }
    }
}
