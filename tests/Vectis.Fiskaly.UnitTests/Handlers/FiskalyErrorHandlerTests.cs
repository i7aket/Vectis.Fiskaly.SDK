using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.Handlers;
using Vectis.Fiskaly.SDK.Metrics;

namespace Vectis.Fiskaly.UnitTests.Handlers;

/// <summary>
/// Unit tests for <see cref="FiskalyErrorHandler"/>.
/// Tests HTTP status code handling, error response parsing, and exception properties.
/// </summary>
public class FiskalyErrorHandlerTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;
    private readonly FiskalyErrorHandler _handler;
    private readonly HttpMessageInvoker _invoker;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly FiskalyMetrics _metrics;
    private readonly Meter _meter;

    public FiskalyErrorHandlerTests()
    {
        _innerHandlerMock = new Mock<HttpMessageHandler>();
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Create a real Meter for metrics (no mocking needed for System.Diagnostics.Metrics)
        TestMeterFactory meterFactory = new TestMeterFactory();
        _metrics = new FiskalyMetrics(meterFactory);
        _meter = meterFactory.Meter;

        _handler = new FiskalyErrorHandler(
            NullLogger<FiskalyErrorHandler>.Instance,
            _jsonOptions,
            _metrics);

        _handler.InnerHandler = _innerHandlerMock.Object;
        _invoker = new HttpMessageInvoker(_handler);
    }

    public void Dispose()
    {
        _handler?.Dispose();
        _invoker?.Dispose();
        _meter?.Dispose();
    }

    // ============================================================================
    // HTTP Status Code Handling Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithBadRequest_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_PARAMETER_MISMATCH",
            Message = "Invalid parameter value",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_PARAMETER_MISMATCH, exception.ErrorCode);
        Assert.Contains("Invalid parameter value", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithUnauthorized_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_UNAUTHORIZED",
            Message = "JWT token expired",
            StatusCode = 401,
            Error = "Unauthorized"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.fiskaly.com/tss");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_UNAUTHORIZED, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Authentication, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithNotFound_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_NOT_FOUND",
            Message = "TSS with ID 'abc123' not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/tss/abc123");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_NOT_FOUND, exception.ErrorCode);
        Assert.Contains("abc123", exception.ApiErrorMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithInternalServerError_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_DEFECTIVE",
            Message = "Hardware security module is defective",
            StatusCode = 500,
            Error = "Internal Server Error"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "https://api.fiskaly.com/tss/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_DEFECTIVE, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Infrastructure, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithSuccessStatusCode_PassesThroughUnchanged()
    {
        // Arrange
        HttpResponseMessage expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":\"success\"}", Encoding.UTF8, "application/json")
        };

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/tss");

        // Act
        HttpResponseMessage actualResponse = await _invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Same(expectedResponse, actualResponse);
        Assert.Equal(HttpStatusCode.OK, actualResponse.StatusCode);
        Assert.True(actualResponse.IsSuccessStatusCode);
    }

    // ============================================================================
    // Error Response Parsing Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_ParsesJsonErrorResponse_WithAllFields()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_CLIENT_CONFLICT",
            Message = "Client with ID 'client-1' already exists",
            StatusCode = 409,
            Error = "Conflict"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.fiskaly.com/client");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.NotNull(exception.ErrorDetails);
        Assert.Equal("E_CLIENT_CONFLICT", exception.ErrorDetails.Code);
        Assert.Equal("Client with ID 'client-1' already exists", exception.ErrorDetails.Message);
        Assert.Equal(409, exception.ErrorDetails.StatusCode);
        Assert.Equal("Conflict", exception.ErrorDetails.Error);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithMalformedJson_ThrowsFiskalyApiExceptionWithUnknownCode()
    {
        // Arrange
        string malformedJson = "{invalid-json-content}";
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(malformedJson, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.Unknown, exception.ErrorCode);
        Assert.Null(exception.ErrorDetails);
        Assert.Contains("400", exception.ApiErrorMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithEmptyResponseBody_ThrowsFiskalyApiExceptionWithUnknownCode()
    {
        // Arrange
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "https://api.fiskaly.com/resource");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.Unknown, exception.ErrorCode);
        Assert.Null(exception.ErrorDetails);
        Assert.Equal("<no-response-body>", exception.ResponseBody);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_PreservesOriginalHttpStatusCode()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_LOCKED",
            Message = "TSS is locked by another operation",
            StatusCode = 409,
            Error = "Conflict"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, "https://api.fiskaly.com/tss/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        // Verify both the exception property and the error details match
        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.NotNull(exception.ErrorDetails);
        Assert.Equal(409, exception.ErrorDetails.StatusCode);
    }

    // ============================================================================
    // Exception Properties Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_FiskalyApiException_ContainsCorrectStatusCode()
    {
        // Arrange - Test various status codes
        (HttpStatusCode, string)[] testCases = new[]
        {
            (HttpStatusCode.BadRequest, "E_PARAMETER_MISMATCH"),
            (HttpStatusCode.NotFound, "E_CLIENT_NOT_FOUND"),
            (HttpStatusCode.Conflict, "E_TSS_CONFLICT"),
            ((HttpStatusCode)422, "E_TSS_NOT_INITIALIZED") // UnprocessableEntity
        };

        foreach ((HttpStatusCode statusCode, string errorCode) in testCases)
        {
            FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
            {
                Code = errorCode,
                Message = "Test error message",
                StatusCode = (int)statusCode,
                Error = statusCode.ToString()
            };

            string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
            _innerHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

            // Act & Assert
            FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
                () => _invoker.SendAsync(request, CancellationToken.None));

            Assert.Equal(statusCode, exception.StatusCode);
            Assert.Equal(json, exception.ResponseBody);
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_FiskalyApiException_ContainsErrorMessageFromResponse()
    {
        // Arrange
        string customMessage = "This is a very specific error message from the API";
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_EXPORT_NOT_COMPLETED",
            Message = customMessage,
            StatusCode = 425,
            Error = "Too Early"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)425)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/export/123");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(customMessage, exception.ApiErrorMessage);
        Assert.Contains(customMessage, exception.Message);
        Assert.Equal(FiskalyErrorCode.E_EXPORT_NOT_COMPLETED, exception.ErrorCode);
    }

    // ============================================================================
    // Additional Edge Cases
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithUnknownErrorCode_SetsUnknownAndLogsWarning()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_FUTURE_ERROR_NOT_IN_SDK",
            Message = "This error code doesn't exist in current SDK",
            StatusCode = 500,
            Error = "Internal Server Error"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(FiskalyErrorCode.Unknown, exception.ErrorCode);
        Assert.Equal("This error code doesn't exist in current SDK", exception.ApiErrorMessage);
        Assert.NotNull(exception.ErrorDetails);
        Assert.Equal("E_FUTURE_ERROR_NOT_IN_SDK", exception.ErrorDetails.Code);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithNullErrorCode_SetsUnknownErrorCode()
    {
        // Arrange
        string jsonWithNullCode = "{\"code\":null,\"message\":\"Error\",\"status_code\":500,\"error\":\"Internal Server Error\"}";
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(jsonWithNullCode, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(FiskalyErrorCode.Unknown, exception.ErrorCode);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithCorrelationIdHeader_UsesProvidedCorrelationId()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_NOT_FOUND",
            Message = "Test error",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");
        request.Headers.Add("X-Correlation-ID", "custom-correlation-id-12345");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal("custom-correlation-id-12345", exception.CorrelationId);
    }

    // ============================================================================
    // Additional Error Tests - Permanent Errors
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithAccessDenied_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_ACCESS_DENIED",
            Message = "",
            StatusCode = 403,
            Error = "Forbidden"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_ACCESS_DENIED, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithAdminPinBlocked_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_ADMIN_PIN_BLOCKED",
            Message = "",
            StatusCode = 423,
            Error = "Locked"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)423)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal((HttpStatusCode)423, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_ADMIN_PIN_BLOCKED, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithCancelExport_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_CANCEL_EXPORT",
            Message = "",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_CANCEL_EXPORT, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithChangeAdminPinFailed_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_CHANGE_ADMIN_PIN_FAILED",
            Message = "",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_CHANGE_ADMIN_PIN_FAILED, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithClientLimitReached_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_CLIENT_LIMIT_REACHED",
            Message = "",
            StatusCode = 403,
            Error = "Forbidden"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_CLIENT_LIMIT_REACHED, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithDuplicateExport_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_DUPLICATE_EXPORT",
            Message = "",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_DUPLICATE_EXPORT, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithExportNotFound_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_EXPORT_NOT_FOUND",
            Message = "",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_EXPORT_NOT_FOUND, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithIllegalClientSerial_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_ILLEGAL_CLIENT_SERIAL",
            Message = "",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_ILLEGAL_CLIENT_SERIAL, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithIllegalTssStateChange_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_ILLEGAL_TSS_STATE_CHANGE",
            Message = "",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_ILLEGAL_TSS_STATE_CHANGE, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTssConflict_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_CONFLICT",
            Message = "",
            StatusCode = 409,
            Error = "Conflict"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_CONFLICT, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTssDisabled_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_DISABLED",
            Message = "",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_DISABLED, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTssIllegalStateToPerformExport_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_ILLEGAL_STATE_TO_PERFORM_EXPORT",
            Message = "",
            StatusCode = 409,
            Error = "Conflict"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_ILLEGAL_STATE_TO_PERFORM_EXPORT, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTssLimitReached_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_LIMIT_REACHED",
            Message = "",
            StatusCode = 403,
            Error = "Forbidden"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_LIMIT_REACHED, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTxIllegalTypeChange_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TX_ILLEGAL_TYPE_CHANGE",
            Message = "",
            StatusCode = 409,
            Error = "Conflict"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TX_ILLEGAL_TYPE_CHANGE, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTxRevisionNotFound_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TX_REVISION_NOT_FOUND",
            Message = "",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TX_REVISION_NOT_FOUND, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTxUpsert_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TX_UPSERT",
            Message = "",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TX_UPSERT, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    // ============================================================================
    // Additional Error Tests - Transient Error
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithExportTemporarilyUnavailable_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_EXPORT_TEMPORARILY_UNAVAILABLE",
            Message = "",
            StatusCode = 503,
            Error = "Service Unavailable"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_EXPORT_TEMPORARILY_UNAVAILABLE, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Transient, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    // ============================================================================
    // Additional Error Tests - Infrastructure Errors
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithBootstrapFileNotAvailable_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_BOOTSTRAP_FILE_NOT_AVAILABLE",
            Message = "",
            StatusCode = 503,
            Error = "Service Unavailable"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_BOOTSTRAP_FILE_NOT_AVAILABLE, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Infrastructure, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithUseMiddleware_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_USE_MIDDLEWARE",
            Message = "",
            StatusCode = 432,
            Error = ""
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)432)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal((HttpStatusCode)432, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_USE_MIDDLEWARE, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Infrastructure, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    // ============================================================================
    // Retry-After Header Tests (Rate Limiting - HTTP 429)
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithRateLimitAndRetryAfterDelta_ParsesRetryAfterSeconds()
    {
        // Arrange - E_EXPORT_DUPLICATE_RATE_LIMITED (429)
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_EXPORT_DUPLICATE_RATE_LIMITED",
            Message = "Too many duplicate export requests",
            StatusCode = 429,
            Error = "Too Many Requests"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Add Retry-After: 120 (seconds)
        responseMessage.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(120));

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_EXPORT_DUPLICATE_RATE_LIMITED, exception.ErrorCode);
        Assert.Equal(FiskalyErrorCategory.Transient, exception.Category);
        Assert.True(exception.IsRetryable);

        // Verify Retry-After parsed correctly
        Assert.NotNull(exception.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(120), exception.RetryAfter.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithRateLimitAndRetryAfterDate_ParsesRetryAfterDate()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TOO_MANY_EXPORTS",
            Message = "Too many concurrent exports",
            StatusCode = 429,
            Error = "Too Many Requests"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Add Retry-After: [future date]
        DateTimeOffset futureDate = DateTimeOffset.UtcNow.AddMinutes(5);
        responseMessage.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(futureDate);

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        // Verify Retry-After parsed from date (approximately 5 minutes)
        Assert.NotNull(exception.RetryAfter);
        Assert.InRange(exception.RetryAfter.Value.TotalMinutes, 4.9, 5.1); // Allow 6s tolerance
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithRateLimitWithoutRetryAfter_RetryAfterIsNull()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_LIMIT_PER_DAY_REACHED",
            Message = "Daily TSS limit reached",
            StatusCode = 429,
            Error = "Too Many Requests"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);

        // No Retry-After header set
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);

        // Verify Retry-After is null when header not present
        Assert.Null(exception.RetryAfter);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithRetryAfterPastDate_ReturnsZeroTimeSpan()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TOO_MANY_EXPORTS",
            Message = "Too many exports",
            StatusCode = 429,
            Error = "Too Many Requests"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Set Retry-After to a past date (5 minutes ago)
        DateTimeOffset pastDate = DateTimeOffset.UtcNow.AddMinutes(-5);
        responseMessage.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(pastDate);

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        // Past date should return TimeSpan.Zero (retry immediately)
        Assert.NotNull(exception.RetryAfter);
        Assert.Equal(TimeSpan.Zero, exception.RetryAfter.Value);
    }

    // ============================================================================
    // Missing Error Code Tests - Phase 2 Addition
    // Source: ErrorCodeMetadata.cs (v2.1.35-aligned)
    // Added: 2025-10-24 to complete error code coverage (15/15 remaining)
    // ============================================================================

    // Priority 1 - Critical (5 tests)

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTssNotInitialized_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_NOT_INITIALIZED",
            Message = "TSS must be initialized before use",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_NOT_INITIALIZED, exception.ErrorCode);
        Assert.Contains("TSS must be initialized", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTxNotFound_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TX_NOT_FOUND",
            Message = "Transaction with ID 'abc123' not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TX_NOT_FOUND, exception.ErrorCode);
        Assert.Contains("Transaction", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithClientNotFound_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_CLIENT_NOT_FOUND",
            Message = "Client with ID 'def456' not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_CLIENT_NOT_FOUND, exception.ErrorCode);
        Assert.Contains("Client", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithPendingTxConflict_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_PENDING_TX_CONFLICT",
            Message = "Another transaction operation is pending",
            StatusCode = 409,
            Error = "Conflict"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_PENDING_TX_CONFLICT, exception.ErrorCode);
        Assert.Contains("pending", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Transient, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTxNoTypeDefined_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TX_NO_TYPE_DEFINED",
            Message = "Transaction type must be defined before finishing",
            StatusCode = 409,
            Error = "Conflict"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TX_NO_TYPE_DEFINED, exception.ErrorCode);
        Assert.Contains("type", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    // Priority 2 - Infrastructure (3 tests)

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithSmaersGatewayCapacitiesDepleted_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_SMAERS_GATEWAY_CAPACITIES_DEPLETED",
            Message = "SMAERS gateway capacity exhausted",
            StatusCode = 503,
            Error = "Service Unavailable"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_SMAERS_GATEWAY_CAPACITIES_DEPLETED, exception.ErrorCode);
        Assert.Contains("capacity", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Infrastructure, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTssEvicted_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_EVICTED",
            Message = "TSS has been evicted from cache",
            StatusCode = 423,
            Error = "Locked"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)423)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal((HttpStatusCode)423, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_EVICTED, exception.ErrorCode);
        Assert.Contains("evicted", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Infrastructure, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTssDeleted_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TSS_DELETED",
            Message = "TSS has been deleted",
            StatusCode = 423,
            Error = "Locked"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)423)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal((HttpStatusCode)423, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TSS_DELETED, exception.ErrorCode);
        Assert.Contains("deleted", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    // Priority 3 - Legacy v2.1.33 (3 tests)

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithCsplNotFound_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_CSPL_NOT_FOUND",
            Message = "CSPL not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_CSPL_NOT_FOUND, exception.ErrorCode);
        Assert.Contains("CSPL", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithLogNotFound_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_LOG_NOT_FOUND",
            Message = "Log entry not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_LOG_NOT_FOUND, exception.ErrorCode);
        Assert.Contains("Log", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithMiddlewarePendingRequest_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_MIDDLEWARE_PENDING_REQUEST",
            Message = "A pending request is already being processed",
            StatusCode = 409,
            Error = "Conflict"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_MIDDLEWARE_PENDING_REQUEST, exception.ErrorCode);
        Assert.Contains("pending", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Transient, exception.Category);
        Assert.True(exception.IsRetryable);
    }

    // Priority 4 - Additional (5 tests)

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithAdminLoginFailed_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_ADMIN_LOGIN_FAILED",
            Message = "Admin authentication failed",
            StatusCode = 401,
            Error = "Unauthorized"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_ADMIN_LOGIN_FAILED, exception.ErrorCode);
        Assert.Contains("Admin", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithClientDeregistered_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_CLIENT_DEREGISTERED",
            Message = "Client has been deregistered",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_CLIENT_DEREGISTERED, exception.ErrorCode);
        Assert.Contains("deregistered", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithExportForbidden_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_EXPORT_FORBIDDEN",
            Message = "Export operation is forbidden",
            StatusCode = 423,
            Error = "Locked"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)423)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal((HttpStatusCode)423, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_EXPORT_FORBIDDEN, exception.ErrorCode);
        Assert.Contains("forbidden", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithTxLimitReached_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_TX_LIMIT_REACHED",
            Message = "Transaction limit has been reached",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_TX_LIMIT_REACHED, exception.ErrorCode);
        Assert.Contains("limit", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithLatestTxRevisionNotFound_ThrowsFiskalyApiException()
    {
        // Arrange
        FiskalyErrorResponse errorResponse = new FiskalyErrorResponse
        {
            Code = "E_LATEST_TX_REVISION_NOT_FOUND",
            Message = "Latest transaction revision not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(errorResponse, _jsonOptions);
        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(FiskalyErrorCode.E_LATEST_TX_REVISION_NOT_FOUND, exception.ErrorCode);
        Assert.Contains("revision", exception.ApiErrorMessage);
        Assert.Equal(FiskalyErrorCategory.Permanent, exception.Category);
        Assert.False(exception.IsRetryable);
    }
}

/// <summary>
/// Test implementation of IMeterFactory for unit tests.
/// </summary>
internal class TestMeterFactory : IMeterFactory
{
    public Meter Meter { get; }

    public TestMeterFactory()
    {
        Meter = new Meter("Vectis.Fiskaly.SDK.Tests", "1.0");
    }

    public Meter Create(MeterOptions options)
    {
        return Meter;
    }

    public void Dispose()
    {
        Meter?.Dispose();
    }
}
