#nullable enable

using Polly;
using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.SDK.Resilience;

internal static class FiskalyResiliencePredicates
{
    public static bool ShouldHandleTransient(Outcome<HttpResponseMessage> outcome)
    {
        FiskalyApiException? exception = outcome.Exception as FiskalyApiException;

        if (exception == null)
            return false;

        return exception.Category == FiskalyErrorCategory.Transient
               && exception.IsRetryable;
    }

    public static bool ShouldHandleInfrastructure(Outcome<HttpResponseMessage> outcome)
    {
        FiskalyApiException? exception = outcome.Exception as FiskalyApiException;

        if (exception == null)
            return false;

        return exception.Category == FiskalyErrorCategory.Infrastructure
               && exception.IsRetryable;
    }

    public static bool ShouldHandleAuthentication(Outcome<HttpResponseMessage> outcome)
    {
        FiskalyApiException? exception = outcome.Exception as FiskalyApiException;

        if (exception == null)
            return false;

        return exception.Category == FiskalyErrorCategory.Authentication
               && exception.IsRetryable;
    }

    public static bool IsPermanent(Outcome<HttpResponseMessage> outcome)
    {
        FiskalyApiException? exception = outcome.Exception as FiskalyApiException;

        if (exception == null)
            return false;

        return exception.Category == FiskalyErrorCategory.Permanent
               || !exception.IsRetryable;
    }

    public static bool ShouldHandleHttpTransient(Outcome<HttpResponseMessage> outcome)
    {
        return outcome.Exception is HttpRequestException or TaskCanceledException;
    }
}
