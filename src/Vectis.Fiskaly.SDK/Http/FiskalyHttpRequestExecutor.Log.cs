namespace Vectis.Fiskaly.SDK.Http;

internal static partial class FiskalyHttpRequestExecutorLog
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Debug,
        Message = "Executing HTTP GET: {Url}")]
    internal static partial void LogExecutingGet(this ILogger logger, string url);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Executing HTTP PUT (no request body): {Url}")]
    internal static partial void LogExecutingPutNoBody(this ILogger logger, string url);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Executing HTTP {Method}: {Url}")]
    internal static partial void LogExecutingRequest(this ILogger logger, HttpMethod method, string url);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Executing HTTP POST (no response): {Url}")]
    internal static partial void LogExecutingPostNoResponse(this ILogger logger, string url);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Debug,
        Message = "Executing HTTP DELETE: {Url}")]
    internal static partial void LogExecutingDelete(this ILogger logger, string url);
}
