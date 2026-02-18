using System.Net;
using AwesomeAssertions;
using Polly;
using Vectis.Fiskaly.SDK.Configuration;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.Resilience;

namespace Vectis.Fiskaly.UnitTests.SDK.Resilience;

/// <summary>
/// Unit tests for FiskalyResiliencePredicates that determine retry and circuit breaker behavior.
/// Tests predicate logic without making real API calls.
/// </summary>
/// <remarks>
/// <para>These tests verify the decision logic for category-based retry predicates:</para>
/// <list type="bullet">
///   <item><strong>ShouldHandleTransient</strong>: Transient errors (E_TSS_LOCKED) trigger exponential backoff retry</item>
///   <item><strong>ShouldHandleInfrastructure</strong>: Infrastructure errors (E_TSS_DEFECTIVE) trigger circuit breaker</item>
///   <item><strong>ShouldHandleAuthentication</strong>: Auth errors (E_UNAUTHORIZED) trigger token refresh + retry</item>
///   <item><strong>IsPermanent</strong>: Permanent errors (E_TSS_NOT_FOUND) fail immediately without retry</item>
///   <item><strong>ShouldHandleHttpTransient</strong>: Network errors (HttpRequestException, TaskCanceledException) trigger retry</item>
/// </list>
/// </remarks>
public sealed class CategoryRetryTests
{
    #region ShouldHandleTransient Tests (3 tests)

    /// <summary>
    /// Verifies that transient errors (E_TSS_LOCKED) are marked for retry.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleTransient_TransientError_ReturnsTrue()
    {
        // Arrange - Create exception for E_TSS_LOCKED (transient error)
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            HttpStatusCode.Conflict,
            isRetryable: true);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleTransient(outcome);

        // Assert
        shouldHandle.Should().BeTrue("Transient errors with IsRetryable=true should be handled");
    }

    /// <summary>
    /// Verifies that permanent errors are NOT marked for transient retry.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleTransient_PermanentError_ReturnsFalse()
    {
        // Arrange - Create exception for permanent error
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            HttpStatusCode.NotFound,
            isRetryable: false);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleTransient(outcome);

        // Assert
        shouldHandle.Should().BeFalse("Permanent errors should not be handled by transient retry");
    }

    /// <summary>
    /// Verifies that non-Fiskaly exceptions are NOT handled by transient predicate.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleTransient_NonFiskalyException_ReturnsFalse()
    {
        // Arrange - Create non-Fiskaly exception
        InvalidOperationException exception = new InvalidOperationException("Test exception");
        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleTransient(outcome);

        // Assert
        shouldHandle.Should().BeFalse("Non-Fiskaly exceptions should not be handled");
    }

    #endregion

    #region ShouldHandleInfrastructure Tests (3 tests)

    /// <summary>
    /// Verifies that infrastructure errors (E_TSS_DEFECTIVE) are marked for circuit breaker handling.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleInfrastructure_InfrastructureError_ReturnsTrue()
    {
        // Arrange - Create exception for infrastructure error
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_DEFECTIVE,
            FiskalyErrorCategory.Infrastructure,
            HttpStatusCode.InternalServerError,
            isRetryable: true);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleInfrastructure(outcome);

        // Assert
        shouldHandle.Should().BeTrue("Infrastructure errors with IsRetryable=true should be handled by circuit breaker");
    }

    /// <summary>
    /// Verifies that permanent errors are NOT marked for infrastructure handling.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleInfrastructure_PermanentError_ReturnsFalse()
    {
        // Arrange - Create exception for permanent error
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_CONFLICT,
            FiskalyErrorCategory.Permanent,
            HttpStatusCode.Conflict,
            isRetryable: false);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleInfrastructure(outcome);

        // Assert
        shouldHandle.Should().BeFalse("Permanent errors should not be handled by circuit breaker");
    }

    /// <summary>
    /// Verifies that transient errors are NOT handled by infrastructure predicate.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleInfrastructure_TransientError_ReturnsFalse()
    {
        // Arrange - Create transient error
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            HttpStatusCode.Conflict,
            isRetryable: true);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleInfrastructure(outcome);

        // Assert
        shouldHandle.Should().BeFalse("Transient errors should not be handled by circuit breaker");
    }

    #endregion

    #region ShouldHandleAuthentication Tests (2 tests)

    /// <summary>
    /// Verifies that authentication errors (E_UNAUTHORIZED) are marked for token refresh + retry.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleAuthentication_AuthError_ReturnsTrue()
    {
        // Arrange - Create authentication error
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_UNAUTHORIZED,
            FiskalyErrorCategory.Authentication,
            HttpStatusCode.Unauthorized,
            isRetryable: true);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleAuthentication(outcome);

        // Assert
        shouldHandle.Should().BeTrue("Authentication errors with IsRetryable=true should be handled");
    }

    /// <summary>
    /// Verifies that permanent errors are NOT handled by authentication predicate.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleAuthentication_PermanentError_ReturnsFalse()
    {
        // Arrange - Create permanent error
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_ADMIN_LOGIN_FAILED,
            FiskalyErrorCategory.Permanent,
            HttpStatusCode.Forbidden,
            isRetryable: false);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleAuthentication(outcome);

        // Assert
        shouldHandle.Should().BeFalse("Permanent errors (even 403) should not be handled by auth retry");
    }

    #endregion

    #region IsPermanent Tests (2 tests)

    /// <summary>
    /// Verifies that permanent errors are correctly identified.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void IsPermanent_PermanentError_ReturnsTrue()
    {
        // Arrange - Create permanent error
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_CLIENT_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            HttpStatusCode.NotFound,
            isRetryable: false);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool isPermanent = FiskalyResiliencePredicates.IsPermanent(outcome);

        // Assert
        isPermanent.Should().BeTrue("Permanent errors should be identified as permanent");
    }

    /// <summary>
    /// Verifies that transient/infrastructure errors are NOT identified as permanent.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void IsPermanent_RetryableError_ReturnsFalse()
    {
        // Arrange - Create retryable error
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_PENDING_TX_CONFLICT,
            FiskalyErrorCategory.Transient,
            HttpStatusCode.Conflict,
            isRetryable: true);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool isPermanent = FiskalyResiliencePredicates.IsPermanent(outcome);

        // Assert
        isPermanent.Should().BeFalse("Retryable errors should not be identified as permanent");
    }

    #endregion

    #region ShouldHandleHttpTransient Tests (1 test)

    // Note: Tests for HTTP status code checking (503, 500, 404) removed
    // because FiskalyErrorHandler converts all error responses to FiskalyApiException
    // before they reach Polly predicates (outcome.Result is always null for errors).
    // Only network-level exceptions (HttpRequestException, TaskCanceledException) are handled.
    // See FiskalyResiliencePredicatesTests for HttpRequestException and TaskCanceledException tests.

    /// <summary>
    /// Verifies that FiskalyApiException is NOT handled by HTTP transient predicate (handled by category-specific predicates).
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleHttpTransient_FiskalyApiException_ReturnsFalse()
    {
        // Arrange - Create Fiskaly exception (handled by other predicates)
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_EVICTED,
            FiskalyErrorCategory.Infrastructure,
            HttpStatusCode.ServiceUnavailable,
            isRetryable: true);

        Outcome<HttpResponseMessage> outcome = CreateExceptionOutcome(exception);

        // Act
        bool shouldHandle = FiskalyResiliencePredicates.ShouldHandleHttpTransient(outcome);

        // Assert
        shouldHandle.Should().BeFalse("FiskalyApiException should be handled by category-specific predicates, not HTTP predicate");
    }

    #endregion

    #region DelayGenerator Tests (5 tests)

    /// <summary>
    /// Verifies that FiskalyDelayCalculator uses TransientDelaySeconds for transient errors.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void DelayCalculator_TransientError_UsesTransientDelay()
    {
        // Arrange
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            HttpStatusCode.Conflict,
            isRetryable: true);

        CategoryRetryDelays delays = new CategoryRetryDelays { TransientDelaySeconds = 1 };

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, delays);

        // Assert - With jitter ±25%, expect 0.75s - 1.25s (1s * 2^0 * (0.75-1.25))
        delay.TotalSeconds.Should().BeInRange(0.75, 1.25,
            "First retry with TransientDelaySeconds=1 should be ~1s with ±25% jitter");
    }

    /// <summary>
    /// Verifies that FiskalyDelayCalculator uses InfrastructureDelaySeconds for infrastructure errors.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void DelayCalculator_InfrastructureError_UsesInfrastructureDelay()
    {
        // Arrange
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_DEFECTIVE,
            FiskalyErrorCategory.Infrastructure,
            HttpStatusCode.InternalServerError,
            isRetryable: true);

        CategoryRetryDelays delays = new CategoryRetryDelays { InfrastructureDelaySeconds = 5 };

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, delays);

        // Assert - With jitter ±25%, expect 3.75s - 6.25s (5s * 2^0 * (0.75-1.25))
        delay.TotalSeconds.Should().BeInRange(3.75, 6.25,
            "First retry with InfrastructureDelaySeconds=5 should be ~5s with ±25% jitter");
    }

    /// <summary>
    /// Verifies that FiskalyDelayCalculator uses AuthenticationDelaySeconds for authentication errors.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void DelayCalculator_AuthenticationError_UsesAuthenticationDelay()
    {
        // Arrange
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_UNAUTHORIZED,
            FiskalyErrorCategory.Authentication,
            HttpStatusCode.Unauthorized,
            isRetryable: true);

        CategoryRetryDelays delays = new CategoryRetryDelays { AuthenticationDelaySeconds = 2 };

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, delays);

        // Assert - With jitter ±25%, expect 1.5s - 2.5s (2s * 2^0 * (0.75-1.25))
        delay.TotalSeconds.Should().BeInRange(1.5, 2.5,
            "First retry with AuthenticationDelaySeconds=2 should be ~2s with ±25% jitter");
    }

    /// <summary>
    /// Verifies that FiskalyDelayCalculator uses TransientDelaySeconds for HTTP errors without Fiskaly code.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void DelayCalculator_HttpError_UsesTransientDelay()
    {
        // Arrange - Generic HTTP exception (not FiskalyApiException)
        HttpRequestException exception = new HttpRequestException("Service Unavailable");
        CategoryRetryDelays delays = new CategoryRetryDelays { TransientDelaySeconds = 1 };

        // Act
        TimeSpan delay = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, delays);

        // Assert - HTTP errors without Fiskaly code use Transient delay
        delay.TotalSeconds.Should().BeInRange(0.75, 1.25,
            "HTTP errors should use TransientDelaySeconds by default");
    }

    /// <summary>
    /// Verifies that FiskalyDelayCalculator applies exponential backoff correctly.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void DelayCalculator_ExponentialBackoff_CalculatesCorrectly()
    {
        // Arrange
        FiskalyApiException exception = CreateFiskalyException(
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            HttpStatusCode.Conflict,
            isRetryable: true);

        CategoryRetryDelays delays = new CategoryRetryDelays { TransientDelaySeconds = 1 };

        // Act - Calculate delays for attempts 0, 1, 2
        TimeSpan delay0 = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 0, delays);
        TimeSpan delay1 = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 1, delays);
        TimeSpan delay2 = FiskalyDelayCalculator.CalculateDelay(exception, attemptNumber: 2, delays);

        // Assert - Verify exponential backoff pattern with jitter
        // Attempt 0: 1s * 2^0 = 1s (with jitter: 0.75-1.25s)
        delay0.TotalSeconds.Should().BeInRange(0.75, 1.25);

        // Attempt 1: 1s * 2^1 = 2s (with jitter: 1.5-2.5s)
        delay1.TotalSeconds.Should().BeInRange(1.5, 2.5);

        // Attempt 2: 1s * 2^2 = 4s (with jitter: 3.0-5.0s)
        delay2.TotalSeconds.Should().BeInRange(3.0, 5.0);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Creates a FiskalyApiException for testing predicates.
    /// </summary>
    private static FiskalyApiException CreateFiskalyException(
        FiskalyErrorCode errorCode,
        FiskalyErrorCategory category,
        HttpStatusCode statusCode,
        bool isRetryable)
    {
        FiskalyErrorResponse errorDetails = new FiskalyErrorResponse
        {
            Code = errorCode.ToString(),
            Message = $"Test error message for {errorCode}",
            StatusCode = (int)statusCode,
            Error = statusCode.ToString()
        };

        return new FiskalyApiException(
            statusCode,
            errorCode,
            category,
            isRetryable,
            $"Test error message for {errorCode}",
            errorDetails,
            Guid.NewGuid().ToString(),
            $"{{\"code\":\"{errorCode}\",\"message\":\"Test error\"}}");
    }

    /// <summary>
    /// Creates a Polly Outcome for exception-based testing.
    /// </summary>
    private static Outcome<HttpResponseMessage> CreateExceptionOutcome(Exception exception)
    {
        return Outcome.FromException<HttpResponseMessage>(exception);
    }

    #endregion
}
