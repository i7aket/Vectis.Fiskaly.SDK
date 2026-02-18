namespace Vectis.Fiskaly.SDK.Handlers;

internal static partial class JwtAuthHandlerLog
{
    [LoggerMessage(
        EventId = 2100,
        Level = LogLevel.Debug,
        Message = "Added JWT Bearer token to request: {Method} {Uri}")]
    internal static partial void LogJwtTokenAdded(this ILogger logger, HttpMethod method, Uri? uri);
}
