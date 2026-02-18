using System.Diagnostics.Metrics;

namespace Vectis.Fiskaly.SDK.Metrics;

public sealed class FiskalyMetrics
{
    private readonly Meter _meter;

    public Counter<long> ErrorsTotal { get; }

    public Counter<long> ErrorsByOperation { get; }

    public Counter<long> ErrorsByStatus { get; }

    public Counter<long> ResilienceRetries { get; }

    public Counter<long> ResilienceCircuitBreakerOpened { get; }

    public Histogram<double> ErrorHandlingDuration { get; }

    public FiskalyMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        _meter = meterFactory.Create(new MeterOptions("Vectis.Fiskaly.SDK")
        {
            Version = "1.0"
        });

        ErrorsTotal = _meter.CreateCounter<long>(
            name: "fiskaly.errors.total",
            unit: "{error}",
            description: "Total count of Fiskaly API errors by error code and category");

        ErrorsByOperation = _meter.CreateCounter<long>(
            name: "fiskaly.errors.by_operation",
            unit: "{error}",
            description: "Count of Fiskaly API errors by operation (API endpoint) and category");

        ErrorsByStatus = _meter.CreateCounter<long>(
            name: "fiskaly.errors.by_status",
            unit: "{error}",
            description: "Count of Fiskaly API errors by HTTP status code and category");

        ResilienceRetries = _meter.CreateCounter<long>(
            name: "fiskaly.resilience.retries",
            unit: "{retry}",
            description: "Count of retry attempts by resilience pipeline and error category");

        ResilienceCircuitBreakerOpened = _meter.CreateCounter<long>(
            name: "fiskaly.resilience.circuit_breaker_opened",
            unit: "{event}",
            description: "Count of circuit breaker open events by resilience pipeline");

        ErrorHandlingDuration = _meter.CreateHistogram<double>(
            name: "fiskaly.error_handling.duration",
            unit: "s",
            description: "Duration of error handling operations (parsing, metadata lookup, exception creation)",
            advice: new InstrumentAdvice<double>
            {
                HistogramBucketBoundaries = [0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1.0, 5.0, 10.0]
            });
    }
}
