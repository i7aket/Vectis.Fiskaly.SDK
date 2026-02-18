using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Integration tests for HTTP-level retry logic using WireMock.
/// Tests fallback retry behavior when API returns generic HTTP errors without Fiskaly error codes.
/// </summary>
/// <remarks>
/// These tests verify that the resilience pipeline correctly handles HTTP errors without structured JSON:
/// - HTTP 503 Service Unavailable → Retry 3 times (1 initial + 3 retries = 4 total attempts)
/// - HTTP 500 Internal Server Error → Retry 3 times (1 initial + 3 retries = 4 total attempts)
/// - HTTP 404 Not Found → No retry (permanent error, only 1 attempt)
///
/// Uses WireMock.Net for in-process HTTP mocking without requiring real Fiskaly API.
/// </remarks>
public sealed class HttpRetryIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public HttpRetryIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Verifies that HTTP 503 Service Unavailable triggers retry with exponential backoff.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task HttpError_503_ShouldRetry_WithExponentialBackoff()
    {
        // Arrange - WireMock server returns HTTP 503 without JSON body
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/test"))
            .RespondWith(Response.Create()
                .WithStatusCode(503)
                .WithHeader("Content-Type", "text/plain")
                .WithBody("Service Unavailable")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Act - Make request and expect exception after retries
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await httpClient.GetAsync("/test"));

        // Assert - Should make 1 initial attempt + 3 retries = 4 total
        attemptCount.Should().Be(4, "Should try 1 initial + 3 retries for HTTP 503");

        _output.WriteLine($"✅ HTTP 503 triggered {attemptCount} retry attempts (1 initial + 3 retries)");
    }

    /// <summary>
    /// Verifies that HTTP 500 Internal Server Error triggers retry 3 times.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task HttpError_500_ShouldRetry_ThreeTimes()
    {
        // Arrange - Track request count
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss"))
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "text/plain")
                .WithBody("Internal Server Error")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Act
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await httpClient.GetAsync("/api/v2/tss"));

        // Assert - Should make 1 initial attempt + 3 retries = 4 total
        attemptCount.Should().Be(4, "Should make 1 initial + 3 retry attempts for HTTP 500");

        _output.WriteLine($"✅ HTTP 500 triggered {attemptCount} retry attempts (1 initial + 3 retries)");
    }

    /// <summary>
    /// Verifies that HTTP 404 Not Found does NOT trigger retry (permanent error).
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task HttpError_404_ShouldNotRetry_PermanentError()
    {
        // Arrange
        using WireMockServer server = WireMockServer.Start();
        int attemptCount = 0;

        server
            .Given(Request.Create().WithPath("/api/v2/tss/nonexistent"))
            .RespondWith(Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "text/plain")
                .WithBody("Not Found")
                .WithCallback(_ =>
                {
                    attemptCount++;
                    return new WireMock.ResponseMessage();
                }));

        HttpClient httpClient = CreateHttpClientWithResilience(server.Url!);

        // Act
        HttpRequestException exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await httpClient.GetAsync("/api/v2/tss/nonexistent"));

        // Assert - Should NOT retry 404 errors (only 1 attempt)
        attemptCount.Should().Be(1, "Should NOT retry 404 errors (permanent client error)");

        _output.WriteLine($"✅ HTTP 404 failed after {attemptCount} attempt (no retries)");
    }

    /// <summary>
    /// Creates an HttpClient with Fiskaly-compatible resilience configuration.
    /// Mimics the retry logic from ServiceCollectionExtensions but without full DI setup.
    /// </summary>
    private static HttpClient CreateHttpClientWithResilience(string baseUrl)
    {
        ServiceCollection services = new ServiceCollection();

        // Register simple error handler that converts HTTP errors to exceptions
        services.AddTransient<SimpleErrorHandler>();

        IHttpClientBuilder httpClientBuilder = services.AddHttpClient("TestClient", client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan; // Polly handles timeouts
        });

        // Add resilience pipeline (returns IHttpStandardResiliencePipelineBuilder, not IHttpClientBuilder)
        httpClientBuilder.AddStandardResilienceHandler(options =>
        {
            // Configure retry: 3 attempts with exponential backoff
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            // Custom retry predicate: Retry ONLY for 5xx errors (not 4xx)
            // This mimics FiskalyResiliencePredicates.ShouldHandleHttpTransient but for raw HTTP responses
            options.Retry.ShouldHandle = args =>
            {
                // Retry on response with 5xx status codes
                if (args.Outcome.Result != null)
                {
                    HttpStatusCode statusCode = args.Outcome.Result.StatusCode;
                    return ValueTask.FromResult(
                        (int)statusCode >= 500 && (int)statusCode <= 599);
                }

                // Retry on HttpRequestException ONLY if it's a 5xx error
                if (args.Outcome.Exception is HttpRequestException httpEx)
                {
                    // HttpRequestException.StatusCode is available in .NET 5+
                    if (httpEx.StatusCode.HasValue)
                    {
                        int code = (int)httpEx.StatusCode.Value;
                        return ValueTask.FromResult(code >= 500 && code <= 599);
                    }
                    // If no status code, assume it's a network error (retry)
                    return ValueTask.FromResult(true);
                }

                // Retry on timeout/cancellation (network errors)
                if (args.Outcome.Exception is TaskCanceledException)
                    return ValueTask.FromResult(true);

                return ValueTask.FromResult(false);
            };

            // Disable circuit breaker for these tests (focus on retry only)
            options.CircuitBreaker.MinimumThroughput = int.MaxValue;
            options.CircuitBreaker.ShouldHandle = _ => ValueTask.FromResult(false);

            // Set reasonable timeouts
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        // Add error handler AFTER resilience (mimics SDK order)
        httpClientBuilder.AddHttpMessageHandler<SimpleErrorHandler>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IHttpClientFactory factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        return factory.CreateClient("TestClient");
    }

    /// <summary>
    /// Simple test handler that converts non-success HTTP status codes to exceptions.
    /// Placed AFTER Resilience pipeline (mimics FiskalyErrorHandler placement in real SDK).
    /// </summary>
    private sealed class SimpleErrorHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Throw exception for all non-success status codes
            // Resilience pipeline already decided whether to retry based on StatusCode
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
