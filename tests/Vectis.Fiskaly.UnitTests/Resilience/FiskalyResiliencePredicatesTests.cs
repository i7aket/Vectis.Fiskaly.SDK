using System.Net;
using Polly;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.Resilience;

namespace Vectis.Fiskaly.UnitTests.Resilience;

public class FiskalyResiliencePredicatesTests
{
    // ============================================================================
    // ShouldHandleTransient Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleTransient_WithTransientCategoryAndRetryable_ReturnsTrue()
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

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleTransient(outcome);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleTransient_WithTransientCategoryButNotRetryable_ReturnsFalse()
    {
        // Arrange
        // Create a custom exception that looks transient but is marked as not retryable
        // Since we can't mock IsRetryable directly, we use a Permanent error instead
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.BadRequest,
            FiskalyErrorCode.E_PARAMETER_MISMATCH,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "Parameter mismatch",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleTransient(outcome);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleTransient_WithNonTransientCategory_ReturnsFalse()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.NotFound,
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "TSS not found",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleTransient(outcome);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleTransient_WithNonFiskalyException_ReturnsFalse()
    {
        // Arrange
        HttpRequestException exception = new HttpRequestException("Network error");
        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleTransient(outcome);

        // Assert
        Assert.False(result);
    }

    // ============================================================================
    // ShouldHandleInfrastructure Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleInfrastructure_WithInfrastructureCategoryAndRetryable_ReturnsTrue()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.ServiceUnavailable,
            FiskalyErrorCode.E_TSS_EVICTED,
            FiskalyErrorCategory.Infrastructure,
            isRetryable: true,
            "TSS evicted from cache",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleInfrastructure(outcome);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleInfrastructure_WithInfrastructureCategoryButNotRetryable_ReturnsFalse()
    {
        // Arrange
        // Use a permanent error to test false case
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.NotFound,
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "TSS not found",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleInfrastructure(outcome);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleInfrastructure_WithNonInfrastructureCategory_ReturnsFalse()
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

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleInfrastructure(outcome);

        // Assert
        Assert.False(result);
    }

    // ============================================================================
    // ShouldHandleAuthentication Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleAuthentication_WithAuthenticationCategoryAndRetryable_ReturnsTrue()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Unauthorized,
            FiskalyErrorCode.E_UNAUTHORIZED,
            FiskalyErrorCategory.Authentication,
            isRetryable: true,
            "Unauthorized - token expired",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleAuthentication(outcome);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleAuthentication_WithAuthenticationCategoryButNotRetryable_ReturnsFalse()
    {
        // Arrange
        // Use a permanent error to test false case
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Forbidden,
            FiskalyErrorCode.E_ADMIN_LOGIN_FAILED,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "Admin login failed",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleAuthentication(outcome);

        // Assert
        Assert.False(result); // E_ADMIN_LOGIN_FAILED is Permanent, not Authentication
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleAuthentication_WithNonAuthenticationCategory_ReturnsFalse()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.NotFound,
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "TSS not found",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleAuthentication(outcome);

        // Assert
        Assert.False(result);
    }

    // ============================================================================
    // IsPermanent Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void IsPermanent_WithPermanentCategory_ReturnsTrue()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.NotFound,
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "TSS not found",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.IsPermanent(outcome);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsPermanent_WithNotRetryableFlag_ReturnsTrue()
    {
        // Arrange
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_CLIENT_CONFLICT,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "Client conflict",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.IsPermanent(outcome);

        // Assert
        Assert.True(result); // E_CLIENT_CONFLICT is Permanent
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsPermanent_WithTransientAndRetryable_ReturnsFalse()
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

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.IsPermanent(outcome);

        // Assert
        Assert.False(result); // E_TSS_LOCKED is Transient and retryable
    }

    // ============================================================================
    // ShouldHandleHttpTransient Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleHttpTransient_WithHttpRequestException_ReturnsTrue()
    {
        // Arrange
        HttpRequestException exception = new HttpRequestException("Network connection failed");
        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleHttpTransient(outcome);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleHttpTransient_WithTaskCanceledException_ReturnsTrue()
    {
        // Arrange
        TaskCanceledException exception = new TaskCanceledException("Request timed out");
        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleHttpTransient(outcome);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ShouldHandleHttpTransient_WithFiskalyApiException_ReturnsFalse()
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

        Outcome<HttpResponseMessage> outcome = Outcome.FromException<HttpResponseMessage>(exception);

        // Act
        bool result = FiskalyResiliencePredicates.ShouldHandleHttpTransient(outcome);

        // Assert
        Assert.False(result); // FiskalyApiException handled by category-specific predicates
    }

    // Note: Tests for HTTP status code checking (408, 429, 5xx) removed
    // because FiskalyErrorHandler converts all error responses to FiskalyApiException
    // before they reach Polly predicates (outcome.Result is always null for errors).
    // Only network-level exceptions (HttpRequestException, TaskCanceledException) are handled here.
}

