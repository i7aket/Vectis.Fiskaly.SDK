using System.Diagnostics.Metrics;
using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Integration tests for error metrics recording.
/// Verifies that FiskalyErrorHandler records metrics correctly when processing API errors.
/// </summary>
public sealed class ErrorMetricsTests : IClassFixture<FiskalyClientFixture>, IDisposable
{
    private readonly FiskalyClientFixture _fixture;
    private readonly MetricsCollector _metricsCollector;

    public ErrorMetricsTests(FiskalyClientFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.TestOutputHelper = output;
        _metricsCollector = new MetricsCollector();
    }

    public void Dispose()
    {
        _metricsCollector.Dispose();
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task TssClient_GetNonExistentTss_ShouldRecordErrorMetrics()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
        _metricsCollector.StartCollecting();

        // Act - Trigger 404 Not Found error
        try
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        }
        catch (FiskalyApiException)
        {
            // Expected exception
        }

        await Task.Delay(100); // Give metrics time to be recorded

        // Assert - Verify error metrics were recorded
        long errorsTotalCount = _metricsCollector.GetCounterValue("fiskaly.errors.total");
        errorsTotalCount.Should().BeGreaterThan(0, "ErrorsTotal counter should be incremented for API errors");

        long errorsByOperationCount = _metricsCollector.GetCounterValue("fiskaly.errors.by_operation");
        errorsByOperationCount.Should().BeGreaterThan(0, "ErrorsByOperation counter should be incremented");

        long errorsByStatusCount = _metricsCollector.GetCounterValue("fiskaly.errors.by_status");
        errorsByStatusCount.Should().BeGreaterThan(0, "ErrorsByStatus counter should be incremented");

        int errorHandlingDurationCount = _metricsCollector.GetHistogramCount("fiskaly.error_handling.duration");
        errorHandlingDurationCount.Should().BeGreaterThan(0, "ErrorHandlingDuration histogram should record measurements");

        _fixture.TestOutputHelper?.WriteLine($"✅ Error metrics recorded: Total={errorsTotalCount}, ByOperation={errorsByOperationCount}, ByStatus={errorsByStatusCount}, Duration count={errorHandlingDurationCount}");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task TssClient_GetNonExistentTss_ShouldRecordCorrectTags()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
        _metricsCollector.StartCollecting();

        // Act - Trigger 404 Not Found error
        try
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        }
        catch (FiskalyApiException)
        {
            // Expected exception
        }

        await Task.Delay(100); // Give metrics time to be recorded

        // Assert - Verify tags are recorded correctly
        List<KeyValuePair<string, object?>> errorsTotalTags = _metricsCollector.GetCounterTags("fiskaly.errors.total");
        errorsTotalTags.Should().NotBeEmpty("ErrorsTotal should have tags");

        // Verify error.code tag exists and has a value
        errorsTotalTags.Should().Contain(kvp => kvp.Key == "error.code",
            "ErrorsTotal should have error.code tag");

        // Verify error.category tag exists
        errorsTotalTags.Should().Contain(kvp => kvp.Key == "error.category",
            "ErrorsTotal should have error.category tag");

        List<KeyValuePair<string, object?>> errorsByOperationTags = _metricsCollector.GetCounterTags("fiskaly.errors.by_operation");
        errorsByOperationTags.Should().Contain(kvp => kvp.Key == "operation",
            "ErrorsByOperation should have operation tag");

        List<KeyValuePair<string, object?>> errorsByStatusTags = _metricsCollector.GetCounterTags("fiskaly.errors.by_status");
        errorsByStatusTags.Should().Contain(kvp => kvp.Key == "http.status_code",
            "ErrorsByStatus should have http.status_code tag");

        _fixture.TestOutputHelper?.WriteLine($"✅ Tags recorded correctly:");
        _fixture.TestOutputHelper?.WriteLine($"   ErrorsTotal tags: {string.Join(", ", errorsTotalTags.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        _fixture.TestOutputHelper?.WriteLine($"   ErrorsByOperation tags: {string.Join(", ", errorsByOperationTags.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        _fixture.TestOutputHelper?.WriteLine($"   ErrorsByStatus tags: {string.Join(", ", errorsByStatusTags.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task TssClient_GetNonExistentTss_ShouldRecordErrorHandlingDuration()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
        _metricsCollector.StartCollecting();

        // Act - Trigger 404 Not Found error
        try
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        }
        catch (FiskalyApiException)
        {
            // Expected exception
        }

        await Task.Delay(100); // Give metrics time to be recorded

        // Assert - Verify error handling duration was recorded
        List<double> durationMeasurements = _metricsCollector.GetHistogramMeasurements("fiskaly.error_handling.duration");
        durationMeasurements.Should().NotBeEmpty("ErrorHandlingDuration should record measurements");

        // Verify duration is reasonable (0.001ms - 10s)
        foreach (double measurement in durationMeasurements)
        {
            measurement.Should().BeGreaterThan(0, "Duration should be positive");
            measurement.Should().BeLessThan(10.0, "Duration should be less than 10 seconds");
        }

        _fixture.TestOutputHelper?.WriteLine($"✅ Error handling duration recorded: {durationMeasurements.Count} measurements");
        _fixture.TestOutputHelper?.WriteLine($"   Duration range: {durationMeasurements.Min():F6}s - {durationMeasurements.Max():F6}s");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task TssClient_MultipleErrors_ShouldAccumulateCounters()
    {
        // Arrange
        _metricsCollector.StartCollecting();
        long initialCount = _metricsCollector.GetCounterValue("fiskaly.errors.total");

        // Act - Trigger multiple errors
        int errorCount = 3;
        for (int i = 0; i < errorCount; i++)
        {
            try
            {
                TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
                await _fixture.TssClient.GetTssAsync(nonExistentTssId);
            }
            catch (FiskalyApiException)
            {
                // Expected exception
            }
        }

        await Task.Delay(100); // Give metrics time to be recorded

        // Assert - Verify counters accumulated
        long finalCount = _metricsCollector.GetCounterValue("fiskaly.errors.total");
        long incrementCount = finalCount - initialCount;

        incrementCount.Should().BeGreaterThanOrEqualTo(errorCount,
            $"ErrorsTotal should increment by at least {errorCount} for {errorCount} errors");

        _fixture.TestOutputHelper?.WriteLine($"✅ Counters accumulated correctly:");
        _fixture.TestOutputHelper?.WriteLine($"   Initial count: {initialCount}");
        _fixture.TestOutputHelper?.WriteLine($"   Final count: {finalCount}");
        _fixture.TestOutputHelper?.WriteLine($"   Increment: {incrementCount} (expected: >={errorCount})");
    }

    /// <summary>
    /// Helper class to collect metrics using MeterListener.
    /// </summary>
    private sealed class MetricsCollector : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly Dictionary<string, long> _counterValues = new();
        private readonly Dictionary<string, List<KeyValuePair<string, object?>>> _counterTags = new();
        private readonly Dictionary<string, List<double>> _histogramMeasurements = new();

        public MetricsCollector()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "Vectis.Fiskaly.SDK")
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };

            _listener.SetMeasurementEventCallback<long>(OnCounterMeasurement);
            _listener.SetMeasurementEventCallback<double>(OnHistogramMeasurement);
        }

        public void StartCollecting()
        {
            _listener.Start();
        }

        public long GetCounterValue(string instrumentName)
        {
            return _counterValues.TryGetValue(instrumentName, out long value) ? value : 0;
        }

        public List<KeyValuePair<string, object?>> GetCounterTags(string instrumentName)
        {
            return _counterTags.TryGetValue(instrumentName, out List<KeyValuePair<string, object?>>? tags) ? tags : new List<KeyValuePair<string, object?>>();
        }

        public int GetHistogramCount(string instrumentName)
        {
            return _histogramMeasurements.TryGetValue(instrumentName, out List<double>? measurements) ? measurements.Count : 0;
        }

        public List<double> GetHistogramMeasurements(string instrumentName)
        {
            return _histogramMeasurements.TryGetValue(instrumentName, out List<double>? measurements) ? measurements : new List<double>();
        }

        private void OnCounterMeasurement(Instrument instrument, long measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            string instrumentName = instrument.Name;

            // Accumulate counter value
            if (!_counterValues.ContainsKey(instrumentName))
                _counterValues[instrumentName] = 0;

            _counterValues[instrumentName] += measurement;

            // Store tags (latest)
            _counterTags[instrumentName] = tags.ToArray().ToList();
        }

        private void OnHistogramMeasurement(Instrument instrument, double measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            string instrumentName = instrument.Name;

            // Store histogram measurements
            if (!_histogramMeasurements.ContainsKey(instrumentName))
                _histogramMeasurements[instrumentName] = new List<double>();

            _histogramMeasurements[instrumentName].Add(measurement);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
