using System.Net;
using Vectis.Fiskaly.SDK.Configuration;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.Resilience;

namespace Vectis.Fiskaly.UnitTests.Resilience;

public class FiskalyDelayCalculatorTests
{
    private readonly CategoryRetryDelays _defaultDelays = new()
    {
        TransientDelaySeconds = 1,
        InfrastructureDelaySeconds = 5,
        AuthenticationDelaySeconds = 2
    };

    // ============================================================================
    // Exponential Backoff Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_AttemptZero_ReturnsBaseDelayWithJitter()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS is locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, _defaultDelays);

        // Assert
        // With attemptNumber=0: delay = 1s * 2^0 * (1 ± 0.25) = 0.75s - 1.25s
        Assert.InRange(delay.TotalSeconds, 0.75, 1.25);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_AttemptOne_ReturnsDoubledDelayWithJitter()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS is locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 1, _defaultDelays);

        // Assert
        // With attemptNumber=1: delay = 1s * 2^1 * (1 ± 0.25) = 1.5s - 2.5s
        Assert.InRange(delay.TotalSeconds, 1.5, 2.5);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_AttemptTwo_ReturnsQuadrupledDelayWithJitter()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS is locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 2, _defaultDelays);

        // Assert
        // With attemptNumber=2: delay = 1s * 2^2 * (1 ± 0.25) = 3.0s - 5.0s
        Assert.InRange(delay.TotalSeconds, 3.0, 5.0);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_MultipleAttempts_FollowsExponentialPattern()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS is locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act - Calculate delays for attempts 0-5
        List<TimeSpan> delays = Enumerable.Range(0, 6)
            .Select(attempt => FiskalyDelayCalculator.CalculateDelay(exception, attempt, _defaultDelays))
            .ToList();

        // Assert - Each delay should be approximately double the previous (accounting for jitter)
        for (int i = 1; i < delays.Count; i++)
        {
            // Verify exponential growth: delay[i] should be roughly 2x delay[i-1]
            // With ±25% jitter, the theoretical ratio range is [1.2, 3.33]:
            // Worst case min: delay[i-1] gets +25% jitter (1.25x), delay[i] gets -25% jitter (0.75x)
            //   → Ratio min: (1 * 2 * 0.75) / (1 * 1.25) = 1.5 / 1.25 = 1.2
            // Worst case max: delay[i-1] gets -25% jitter (0.75x), delay[i] gets +25% jitter (1.25x)
            //   → Ratio max: (1 * 2 * 1.25) / (1 * 0.75) = 2.5 / 0.75 = 3.33
            // We use [1.15, 3.4] to add small buffer for floating point precision
            double ratio = delays[i].TotalSeconds / delays[i - 1].TotalSeconds;
            Assert.InRange(ratio, 1.15, 3.4);
        }
    }

    // ============================================================================
    // Category-Specific Delays
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_TransientCategory_UsesTransientDelay()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED, // Transient category
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS is locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, _defaultDelays);

        // Assert
        // Base delay should be TransientDelaySeconds (1.0s) with jitter
        Assert.InRange(delay.TotalSeconds, 0.75, 1.25);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_InfrastructureCategory_UsesInfrastructureDelay()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.ServiceUnavailable,
            FiskalyErrorCode.E_TSS_EVICTED, // Infrastructure category
            FiskalyErrorCategory.Infrastructure,
            isRetryable: true,
            "TSS evicted from cache",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, _defaultDelays);

        // Assert
        // Base delay should be InfrastructureDelaySeconds (5.0s) with jitter
        Assert.InRange(delay.TotalSeconds, 3.75, 6.25);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_AuthenticationCategory_UsesAuthenticationDelay()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Unauthorized,
            FiskalyErrorCode.E_UNAUTHORIZED, // Authentication category
            FiskalyErrorCategory.Authentication,
            isRetryable: true,
            "Unauthorized - token expired",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, _defaultDelays);

        // Assert
        // Base delay should be AuthenticationDelaySeconds (2.0s) with jitter
        Assert.InRange(delay.TotalSeconds, 1.5, 2.5);
    }

    // ============================================================================
    // Jitter Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_AppliesJitter_WithinExpectedRange()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS is locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act - Calculate delay 100 times to verify jitter distribution
        List<double> delays = Enumerable.Range(0, 100)
            .Select(_ => FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, _defaultDelays).TotalSeconds)
            .ToList();

        // Assert
        // All delays should be within ±25% of base delay (1.0s)
        Assert.All(delays, delay => Assert.InRange(delay, 0.75, 1.25));

        // Verify we got some variety (jitter is random)
        int uniqueDelays = delays.Distinct().Count();
        Assert.True(uniqueDelays > 50, $"Expected jitter to produce variety, got {uniqueDelays} unique values out of 100");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_WithHttpException_UsesTransientDelay()
    {
        // Arrange
        HttpRequestException exception = new HttpRequestException("Network connection failed");

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, _defaultDelays);

        // Assert
        // Non-FiskalyApiException should default to TransientDelaySeconds
        Assert.InRange(delay.TotalSeconds, 0.75, 1.25);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_WithNullException_UsesTransientDelay()
    {
        // Arrange
        Exception? exception = null;

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, _defaultDelays);

        // Assert
        // Null exception should default to TransientDelaySeconds
        Assert.InRange(delay.TotalSeconds, 0.75, 1.25);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_WithHighAttemptNumber_GrowsExponentially()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS is locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act
        TimeSpan delayAttempt10 = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 10, _defaultDelays);

        // Assert
        // With attemptNumber=10: delay = 1s * 2^10 * (1 ± 0.25) = 1024s * (0.75-1.25) = 768s - 1280s
        Assert.InRange(delayAttempt10.TotalSeconds, 768, 1280);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CalculateDelay_WithCustomDelays_UsesProvidedValues()
    {
        // Arrange
        CategoryRetryDelays customDelays = new CategoryRetryDelays
        {
            TransientDelaySeconds = 10, // Custom base delay
            InfrastructureDelaySeconds = 30,
            AuthenticationDelaySeconds = 15
        };

        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS is locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, customDelays);

        // Assert
        // Base delay should be custom TransientDelaySeconds (10.0s) with jitter
        Assert.InRange(delay.TotalSeconds, 7.5, 12.5);
    }
}
