using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.Metrics;

namespace Vectis.Fiskaly.SDK.Handlers;

public class FiskalyErrorHandler(
    ILogger<FiskalyErrorHandler> logger,
    JsonSerializerOptions jsonOptions,
    FiskalyMetrics metrics) : DelegatingHandler
{
    private readonly ILogger<FiskalyErrorHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
    private readonly FiskalyMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
            return response;

        Stopwatch stopwatch = Stopwatch.StartNew();

        string operation = $"{request.Method} {request.RequestUri?.PathAndQuery}";
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        (FiskalyErrorCode errorCode, FiskalyErrorResponse? errorDetails) = ParseErrorResponse(responseBody, response.StatusCode);

        Metadata metadata = ErrorCodeMetadata.Get(errorCode);

        string correlationId = ExtractCorrelationId(request);

        string apiErrorMessage = string.IsNullOrWhiteSpace(errorDetails?.Message)
            ? $"HTTP {(int)response.StatusCode} {response.StatusCode}"
            : errorDetails.Message!;

        _logger.LogFiskalyApiError(
            errorCode,
            metadata.Category,
            operation,
            response.StatusCode,
            metadata.IsRetryable,
            correlationId,
            apiErrorMessage);

        EnrichActivityWithErrorContext(errorCode, metadata.Category, metadata.IsRetryable, correlationId, operation);

        RecordErrorMetrics(errorCode, metadata.Category, operation, response.StatusCode, stopwatch.Elapsed.TotalSeconds);

        TimeSpan? retryAfter = null;
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            if (response.Headers.RetryAfter != null)
            {
                if (response.Headers.RetryAfter.Delta.HasValue)
                {
                    retryAfter = response.Headers.RetryAfter.Delta.Value;
                }
                else if (response.Headers.RetryAfter.Date.HasValue)
                {
                    retryAfter = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                    if (retryAfter < TimeSpan.Zero)
                    {
                        retryAfter = TimeSpan.Zero; // Past date → retry immediately
                    }
                }
            }
        }

        response.Dispose();

        throw new FiskalyApiException(
            statusCode: response.StatusCode,
            errorCode: errorCode,
            category: metadata.Category,
            isRetryable: metadata.IsRetryable,
            apiErrorMessage: apiErrorMessage,
            errorDetails: errorDetails,
            correlationId: correlationId,
            responseBody: responseBody,
            retryAfter: retryAfter);
    }

    private (FiskalyErrorCode ErrorCode, FiskalyErrorResponse? ErrorDetails) ParseErrorResponse(
        string responseBody,
        HttpStatusCode statusCode)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            _logger.LogMissingErrorCode(statusCode);
            return (FiskalyErrorCode.Unknown, null);
        }

        try
        {
            FiskalyErrorResponse? errorResponse = JsonSerializer.Deserialize<FiskalyErrorResponse>(
                responseBody,
                _jsonOptions);

            if (errorResponse == null || string.IsNullOrEmpty(errorResponse.Code))
            {
                _logger.LogMissingErrorCode(statusCode);
                return (FiskalyErrorCode.Unknown, errorResponse);
            }

            if (Enum.TryParse<FiskalyErrorCode>(errorResponse.Code, ignoreCase: false, out FiskalyErrorCode errorCode))
                return (errorCode, errorResponse);

            _logger.LogUnknownErrorCode(errorResponse.Code, statusCode);

            return (FiskalyErrorCode.Unknown, errorResponse);
        }
        catch (JsonException ex)
        {
            _logger.LogJsonParsingFailed(ex, statusCode);

            return (FiskalyErrorCode.Unknown, null);
        }
    }

    private static string ExtractCorrelationId(HttpRequestMessage request)
    {
        if (request.Headers.TryGetValues("X-Correlation-ID", out IEnumerable<string>? values))
        {
            string? correlationId = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(correlationId))
                return correlationId;
        }

        Activity? activity = System.Diagnostics.Activity.Current;
        if (activity != null)
            return activity.TraceId.ToString();

        return Guid.NewGuid().ToString();
    }

    private static void EnrichActivityWithErrorContext(
        FiskalyErrorCode errorCode,
        FiskalyErrorCategory category,
        bool isRetryable,
        string? correlationId,
        string operation)
    {
        Activity? activity = Activity.Current;
        if (activity == null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, $"Fiskaly API error: {errorCode}");

        activity.SetTag("error", true);
        activity.SetTag("error.type", "FiskalyApiException");
        activity.SetTag("fiskaly.error.code", errorCode.ToString());
        activity.SetTag("fiskaly.error.category", category.ToString());
        activity.SetTag("fiskaly.error.retryable", isRetryable);
        activity.SetTag("fiskaly.operation", operation);

        if (!string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag("fiskaly.correlation_id", correlationId);
        }

        activity.AddEvent(new ActivityEvent(
            "fiskaly.api.error",
            DateTimeOffset.UtcNow,
            new ActivityTagsCollection
            {
                { "error.code", errorCode.ToString() },
                { "error.category", category.ToString() }
            }));
    }

    private void RecordErrorMetrics(
        FiskalyErrorCode errorCode,
        FiskalyErrorCategory category,
        string operation,
        HttpStatusCode statusCode,
        double durationSeconds)
    {
        KeyValuePair<string, object?> errorCodeTag = new KeyValuePair<string, object?>("error.code", errorCode.ToString());
        KeyValuePair<string, object?> categoryTag = new KeyValuePair<string, object?>("error.category", category.ToString());
        KeyValuePair<string, object?> operationTag = new KeyValuePair<string, object?>("operation", operation);
        KeyValuePair<string, object?> statusCodeTag = new KeyValuePair<string, object?>("http.status_code", ((int)statusCode).ToString());

        _metrics.ErrorsTotal.Add(1, errorCodeTag, categoryTag);

        _metrics.ErrorsByOperation.Add(1, operationTag, categoryTag);

        _metrics.ErrorsByStatus.Add(1, statusCodeTag, categoryTag);

        _metrics.ErrorHandlingDuration.Record(durationSeconds, errorCodeTag, categoryTag);
    }

}
