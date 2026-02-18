using System.Net;

namespace Vectis.Fiskaly.SDK.Exceptions;

public class FiskalyApiException : FiskalyException
{
    private const string DefaultResponseBody = "<no-response-body>";
    private const string DefaultApiErrorMessage = "Fiskaly API error message not provided.";

    public HttpStatusCode StatusCode { get; }

    public string ResponseBody { get; }

    public FiskalyErrorCode ErrorCode { get; }

    public FiskalyErrorCategory Category { get; }

    public bool IsRetryable { get; }

    public string ApiErrorMessage { get; }

    public FiskalyErrorResponse? ErrorDetails { get; }

    public string? CorrelationId { get; }

    public TimeSpan? RetryAfter { get; }

    public FiskalyApiException()
        : base()
    {
        StatusCode = HttpStatusCode.InternalServerError;
        ResponseBody = DefaultResponseBody;
        ErrorCode = FiskalyErrorCode.Unknown;
        Category = FiskalyErrorCategory.Permanent;
        IsRetryable = false;
        ApiErrorMessage = DefaultApiErrorMessage;
        ErrorDetails = null;
        CorrelationId = null;
        RetryAfter = null;
    }

    public FiskalyApiException(string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        StatusCode = HttpStatusCode.InternalServerError;
        ResponseBody = DefaultResponseBody;
        ErrorCode = FiskalyErrorCode.Unknown;
        Category = FiskalyErrorCategory.Permanent;
        IsRetryable = false;
        ApiErrorMessage = message;
        ErrorDetails = null;
        CorrelationId = null;
        RetryAfter = null;
    }

    public FiskalyApiException(string message, Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        StatusCode = HttpStatusCode.InternalServerError;
        ResponseBody = DefaultResponseBody;
        ErrorCode = FiskalyErrorCode.Unknown;
        Category = FiskalyErrorCategory.Permanent;
        IsRetryable = false;
        ApiErrorMessage = message;
        ErrorDetails = null;
        CorrelationId = null;
        RetryAfter = null;
    }

    public FiskalyApiException(HttpStatusCode statusCode, string message, string responseBody)
        : base($"Fiskaly API error ({(int)statusCode} {statusCode}): {message}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        StatusCode = statusCode;
        ResponseBody = Normalize(responseBody, DefaultResponseBody);
        ErrorCode = FiskalyErrorCode.Unknown;
        Category = FiskalyErrorCategory.Permanent;
        IsRetryable = false;
        ApiErrorMessage = message;
        ErrorDetails = null;
        CorrelationId = null;
        RetryAfter = null;
    }

    public FiskalyApiException(
        HttpStatusCode statusCode,
        FiskalyErrorCode errorCode,
        FiskalyErrorCategory category,
        bool isRetryable,
        string apiErrorMessage,
        FiskalyErrorResponse? errorDetails,
        string? correlationId,
        string responseBody,
        TimeSpan? retryAfter = null)
        : base($"Fiskaly API error {errorCode} ({(int)statusCode} {statusCode}): {apiErrorMessage}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiErrorMessage);

        StatusCode = statusCode;
        ErrorCode = errorCode;
        Category = category;
        IsRetryable = isRetryable;
        ApiErrorMessage = apiErrorMessage;
        ErrorDetails = errorDetails;
        CorrelationId = correlationId;
        ResponseBody = Normalize(responseBody, DefaultResponseBody);
        RetryAfter = retryAfter;
    }

    private static string Normalize(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value;

    public string GetRecoveryHint()
    {
        Metadata metadata = ErrorCodeMetadata.Get(ErrorCode);
        return metadata.RecoveryHint;
    }
}
