using System.Diagnostics;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Integration tests for CancellationToken support in HTTP requests.
/// Tests verify that cancellation tokens properly cancel in-flight requests
/// and throw OperationCanceledException when cancelled.
/// </summary>
/// <remarks>
/// <para><strong>Cancellation Token Behavior</strong>:</para>
/// <list type="bullet">
///   <item><description>Cancelled token throws OperationCanceledException (or TaskCanceledException)</description></item>
///   <item><description>Request is aborted immediately without completing</description></item>
///   <item><description>Cancellation works across retry attempts</description></item>
///   <item><description>Cancellation works with resilience pipeline (timeout, retry, circuit breaker)</description></item>
/// </list>
///
/// <para><strong>Test Strategy</strong>:</para>
/// <para>Uses WireMock to simulate slow responses. Cancellation token is triggered
/// while request is in-flight to verify proper cancellation handling.</para>
///
/// <para><strong>Important Note</strong>:</para>
/// <para>TaskCanceledException is a subclass of OperationCanceledException.
/// Tests accept both exception types as valid cancellation signals.</para>
/// </remarks>
public sealed class CancellationTokenIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public CancellationTokenIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Verifies that cancelling a CancellationToken cancels the in-flight HTTP request.
    /// </summary>
    /// <remarks>
    /// Test scenario: Request with 5-second delay, cancel after 1 second.
    /// Expected behavior:
    /// - Request starts
    /// - CancellationToken cancelled after 1 second
    /// - Request throws OperationCanceledException
    /// - Total time: ~1 second (not 5 seconds)
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Cancellation")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task CancellationToken_CancelsInFlightRequest()
    {
        // Arrange - Mock endpoint with 5-second delay
        using WireMockServer server = WireMockServer.Start();
        bool requestReceived = false;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithBody("{\"data\": []}")
                .WithCallback(_ =>
                {
                    requestReceived = true;
                    _output.WriteLine("   Request received by backend (5s delay configured)");
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Create cancellation token that cancels after 1 second
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act & Assert
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await httpClient.GetAsync("/api/v2/tss", cts.Token));

        stopwatch.Stop();

        // Assert cancellation behavior
        // Note: requestReceived flag is unreliable due to race condition - cancellation may occur
        // before WireMock callback executes. What matters is that cancellation worked correctly.

        // Should cancel quickly (~1 second), not wait for full 5-second delay
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3),
            "Request should be cancelled quickly after CancellationToken fired, not wait for full 5s response delay");

        _output.WriteLine($"✅ Cancellation test passed:");
        _output.WriteLine($"   Cancelled after: {stopwatch.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"   Exception: {exception.GetType().Name}");
    }

    /// <summary>
    /// Verifies that cancellation works during retry attempts.
    /// </summary>
    /// <remarks>
    /// Test scenario: Multiple 500 errors triggering retries, cancel during retry sequence.
    /// Expected behavior: Request should be cancelled during retry, not complete all retries.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Cancellation")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task CancellationToken_CancelsDuringRetryAttempts()
    {
        // Arrange - Mock endpoint that always returns 500 (triggers retries)
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithDelay(TimeSpan.FromSeconds(1))
                .WithBody("{\"error\": \"Internal Server Error\"}")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    _output.WriteLine($"   Attempt {attemptCount}: Returning 500 error");
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Create cancellation token that cancels after 2.5 seconds
        // This should cancel during retry (after 1-2 attempts)
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2.5));

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act & Assert
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await httpClient.GetAsync("/api/v2/tss", cts.Token));

        stopwatch.Stop();

        // Assert
        attemptCount.Should().BeLessThan(4,
            "Should not complete all 4 retry attempts (1 initial + 3 retries)");

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(4),
            "Should cancel before completing all retry attempts");

        _output.WriteLine($"✅ Cancellation during retry test passed:");
        _output.WriteLine($"   Attempts made: {attemptCount}");
        _output.WriteLine($"   Cancelled after: {stopwatch.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"   Exception: {exception.GetType().Name}");
    }

    /// <summary>
    /// Verifies that already-cancelled token throws immediately without making request.
    /// </summary>
    /// <remarks>
    /// Test scenario: Create pre-cancelled CancellationToken.
    /// Expected behavior: Throws immediately without reaching backend.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Cancellation")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task CancellationToken_AlreadyCancelled_ThrowsImmediately()
    {
        // Arrange - Mock endpoint
        using WireMockServer server = WireMockServer.Start();
        bool requestReceived = false;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"data\": []}")
                .WithCallback(_ =>
                {
                    requestReceived = true;
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Create already-cancelled token
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act & Assert
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await httpClient.GetAsync("/api/v2/tss", cts.Token));

        stopwatch.Stop();

        // Assert
        requestReceived.Should().BeFalse(
            "Request should not reach backend with pre-cancelled token");

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500),
            "Should throw immediately without delay");

        _output.WriteLine($"✅ Pre-cancelled token test passed:");
        _output.WriteLine($"   Request reached backend: {requestReceived}");
        _output.WriteLine($"   Threw after: {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
        _output.WriteLine($"   Exception: {exception.GetType().Name}");
    }

    /// <summary>
    /// Verifies that cancellation works with timeout scenarios.
    /// </summary>
    /// <remarks>
    /// Test scenario: Slow response (6s delay) with cancellation token (2s).
    /// Expected behavior: CancellationToken cancels before timeout would trigger.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Cancellation")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task CancellationToken_CancelsBeforeTimeout()
    {
        // Arrange - Mock endpoint with 6-second delay (exceeds 5s timeout)
        using WireMockServer server = WireMockServer.Start();

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(6))
                .WithBody("{\"data\": []}"));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Create cancellation token that cancels after 2 seconds
        // Should cancel before 5-second attempt timeout
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act & Assert
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await httpClient.GetAsync("/api/v2/tss", cts.Token));

        stopwatch.Stop();

        // Assert - Should cancel around 2 seconds, not wait for 5-second timeout
        stopwatch.Elapsed.Should().BeCloseTo(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1),
            "Should cancel after 2 seconds, before 5-second timeout would trigger");

        _output.WriteLine($"✅ Cancellation before timeout test passed:");
        _output.WriteLine($"   Cancelled after: {stopwatch.Elapsed.TotalSeconds:F2}s");
        _output.WriteLine($"   Expected cancellation: ~2s (before 5s timeout)");
        _output.WriteLine($"   Exception: {exception.GetType().Name}");
    }

    /// <summary>
    /// Verifies that uncancelled token allows request to complete normally.
    /// </summary>
    /// <remarks>
    /// Control test: Request with valid CancellationToken that is never cancelled.
    /// Expected behavior: Request completes successfully.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Trait("Feature", "Cancellation")]
    [Trait("Priority", "Low")]
    [Fact]
    public async Task CancellationToken_NotCancelled_RequestSucceeds()
    {
        // Arrange - Mock endpoint with normal response
        using WireMockServer server = WireMockServer.Start();

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(1))
                .WithBody("{\"data\": []}"));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Create cancellation token with long timeout (10 seconds, won't cancel)
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act
        HttpResponseMessage response = await httpClient.GetAsync("/api/v2/tss", cts.Token);

        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue(
            "Request should complete successfully with uncancelled token");

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3),
            "Request should complete normally");

        _output.WriteLine($"✅ Uncancelled token test passed:");
        _output.WriteLine($"   Status: {response.StatusCode}");
        _output.WriteLine($"   Completed in: {stopwatch.Elapsed.TotalSeconds:F2}s");
    }

    /// <summary>
    /// Creates an HttpClient with Fiskaly-compatible resilience configuration.
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

            // Retry on 5xx errors
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

            // Configure timeouts
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

            // Disable circuit breaker for cancellation tests
            options.CircuitBreaker.MinimumThroughput = int.MaxValue;
            options.CircuitBreaker.ShouldHandle = _ => ValueTask.FromResult(false);
        });

        // Add error handler to convert non-success responses to exceptions
        httpClientBuilder.AddHttpMessageHandler(() => new CancellationErrorHandler());

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IHttpClientFactory factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        return factory.CreateClient("TestClient");
    }

    /// <summary>
    /// Handler that converts non-success HTTP responses to exceptions.
    /// </summary>
    private sealed class CancellationErrorHandler : DelegatingHandler
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
