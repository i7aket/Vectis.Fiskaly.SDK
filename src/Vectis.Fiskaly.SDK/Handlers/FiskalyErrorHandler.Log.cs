using System.Net;
using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.SDK.Handlers;

internal static partial class FiskalyErrorHandlerLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "Fiskaly API error {ErrorCode} ({Category}) for {Operation}. Status: {StatusCode}, Retryable: {IsRetryable}, CorrelationId: {CorrelationId}, Message: {ApiErrorMessage}")]
    internal static partial void LogFiskalyApiError(this ILogger logger, FiskalyErrorCode errorCode, FiskalyErrorCategory category, string operation, HttpStatusCode statusCode, bool isRetryable, string? correlationId, string apiErrorMessage);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Unknown Fiskaly error code: {Code}. HTTP Status: {StatusCode}")]
    internal static partial void LogUnknownErrorCode(this ILogger logger, string code, HttpStatusCode statusCode);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Failed to parse Fiskaly error response. HTTP Status: {StatusCode}")]
    internal static partial void LogJsonParsingFailed(this ILogger logger, Exception exception, HttpStatusCode statusCode);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Fiskaly error response missing code. HTTP Status: {StatusCode}")]
    internal static partial void LogMissingErrorCode(this ILogger logger, HttpStatusCode statusCode);
}
