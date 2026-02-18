using System.Net;

namespace Vectis.Fiskaly.SDK.Authentication;

internal static partial class FiskalyAuthenticationServiceLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Debug,
        Message = "Using cached {Kind} access token (expires in {Seconds:F0}s)")]
    internal static partial void LogCacheHit(this ILogger logger, string kind, double seconds);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Access token for {Kind} credentials missing or expired. Authenticating with fiskaly API…")]
    internal static partial void LogTokenMissingOrExpired(this ILogger logger, string kind);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Debug,
        Message = "Sending authentication request using {Kind} credentials.")]
    internal static partial void LogAuthenticationRequest(this ILogger logger, string kind);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Debug,
        Message = "Authentication succeeded using {Kind} credentials. Token expires in {ExpiresIn}s.")]
    internal static partial void LogAuthenticationSucceeded(this ILogger logger, string kind, int expiresIn);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Access token obtained for {Kind} credentials. Expires at {Expiry:O} (UTC).")]
    internal static partial void LogTokenObtained(this ILogger logger, string kind, DateTimeOffset expiry);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Error,
        Message = "Authentication failed for {Kind} credentials. Status: {StatusCode}, Response: {Response}")]
    internal static partial void LogAuthenticationFailed(this ILogger logger, string kind, HttpStatusCode statusCode, string response);
}
