using System.Net;
using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.UnitTests.Exceptions;

public class FiskalyApiExceptionTests
{
    private const string DefaultResponseBody = "<no-response-body>";
    private const string DefaultApiMessage = "Fiskaly API error message not provided.";

    // ============================================================================
    // Constructor Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithStatusCodeMessageAndBody_SetsProperties()
    {
        HttpStatusCode statusCode = HttpStatusCode.NotFound;
        string message = "Resource not found";
        string responseBody = "{\"error\":\"Not found\"}";

        FiskalyApiException exception = new FiskalyApiException(statusCode, message, responseBody);

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Contains(message, exception.Message);
        Assert.Equal(responseBody, exception.ResponseBody);
        Assert.Equal(FiskalyErrorCode.Unknown, exception.ErrorCode);
        Assert.Contains("404", exception.Message);
        Assert.Contains("NotFound", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithFullDetails_SetsAllProperties()
    {
        HttpStatusCode statusCode = HttpStatusCode.Conflict;
        FiskalyErrorCode errorCode = FiskalyErrorCode.E_TSS_LOCKED;
        FiskalyErrorCategory category = FiskalyErrorCategory.Transient;
        string apiMessage = "TSS is locked";
        string correlationId = Guid.NewGuid().ToString();
        string responseBody = "{\"code\":\"E_TSS_LOCKED\"}";

        FiskalyApiException exception = new FiskalyApiException(
            statusCode,
            errorCode,
            category,
            isRetryable: true,
            apiMessage,
            errorDetails: null,
            correlationId,
            responseBody);

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(errorCode, exception.ErrorCode);
        Assert.Equal(category, exception.Category);
        Assert.True(exception.IsRetryable);
        Assert.Equal(apiMessage, exception.ApiErrorMessage);
        Assert.Equal(correlationId, exception.CorrelationId);
        Assert.Equal(responseBody, exception.ResponseBody);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_DefaultValues_SetsDefaults()
    {
        FiskalyApiException exception = new FiskalyApiException();

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.Unknown, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
        Assert.Null(exception.CorrelationId);
        Assert.Equal(DefaultResponseBody, exception.ResponseBody);
        Assert.Equal(DefaultApiMessage, exception.ApiErrorMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMessage_SetsMessageAndDefaults()
    {
        string message = "Custom error message";

        FiskalyApiException exception = new FiskalyApiException(message);

        Assert.Equal(message, exception.Message);
        Assert.Equal(message, exception.ApiErrorMessage);
        Assert.Equal(DefaultResponseBody, exception.ResponseBody);
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.Unknown, exception.ErrorCode);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        string message = "Outer error";
        InvalidOperationException inner = new InvalidOperationException("Inner");

        FiskalyApiException exception = new FiskalyApiException(message, inner);

        Assert.Equal(message, exception.Message);
        Assert.Same(inner, exception.InnerException);
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal(message, exception.ApiErrorMessage);
        Assert.Equal(DefaultResponseBody, exception.ResponseBody);
    }

    // ============================================================================
    // Error Code and Category Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithPermanentError_SetsCorrectProperties()
    {
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.NotFound,
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "TSS not found",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithTransientError_SetsCorrectProperties()
    {
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Conflict,
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCategory.Transient,
            isRetryable: true,
            "TSS locked",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Assert.Equal(FiskalyErrorCategory.Transient, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithAuthenticationError_SetsCorrectProperties()
    {
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.Unauthorized,
            FiskalyErrorCode.E_UNAUTHORIZED,
            FiskalyErrorCategory.Authentication,
            isRetryable: true,
            "Unauthorized",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Assert.Equal(FiskalyErrorCategory.Authentication, exception.Category);
        Assert.True(exception.IsRetryable);
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    // ============================================================================
    // Message Format Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Message_IncludesErrorCodeAndStatus()
    {
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.NotFound,
            FiskalyErrorCode.E_CLIENT_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "Client not found",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Assert.Contains("E_CLIENT_NOT_FOUND", exception.Message);
        Assert.Contains("404", exception.Message);
        Assert.Contains("Client not found", exception.Message);
    }

    // ============================================================================
    // Recovery Hint Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void GetRecoveryHint_WithTssNotFound_ReturnsAppropriateHint()
    {
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.NotFound,
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "TSS not found",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        string hint = exception.GetRecoveryHint();

        Assert.NotNull(hint);
        Assert.NotEmpty(hint);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetRecoveryHint_WithUnknownError_ReturnsGenericHint()
    {
        FiskalyApiException exception = new FiskalyApiException();

        string hint = exception.GetRecoveryHint();

        Assert.NotNull(hint);
        Assert.NotEmpty(hint);
    }

    // ============================================================================
    // ErrorDetails Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithErrorDetails_StoresDetails()
    {
        FiskalyErrorResponse errorDetails = new FiskalyErrorResponse
        {
            Code = "E_TSS_NOT_FOUND",
            Message = "TSS not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.NotFound,
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "TSS not found",
            errorDetails,
            correlationId: null,
            responseBody: "{}");

        Assert.NotNull(exception.ErrorDetails);
        Assert.Equal("E_TSS_NOT_FOUND", exception.ErrorDetails.Code);
        Assert.Equal(404, exception.ErrorDetails.StatusCode);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullErrorDetails_AcceptsNull()
    {
        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.InternalServerError,
            FiskalyErrorCode.Unknown,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "Unknown error",
            errorDetails: null,
            correlationId: null,
            responseBody: "{}");

        Assert.Null(exception.ErrorDetails);
    }

    // ============================================================================
    // Correlation ID Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CorrelationId_WhenProvided_IsStored()
    {
        string correlationId = Guid.NewGuid().ToString();

        FiskalyApiException exception = new FiskalyApiException(
            HttpStatusCode.InternalServerError,
            FiskalyErrorCode.Unknown,
            FiskalyErrorCategory.Permanent,
            isRetryable: false,
            "Error",
            errorDetails: null,
            correlationId,
            responseBody: "{}");

        Assert.Equal(correlationId, exception.CorrelationId);
    }

    // ============================================================================
    // Inheritance Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void FiskalyApiException_InheritsFromFiskalyException()
    {
        FiskalyApiException exception = new FiskalyApiException();

        Assert.IsAssignableFrom<FiskalyException>(exception);
    }
}
