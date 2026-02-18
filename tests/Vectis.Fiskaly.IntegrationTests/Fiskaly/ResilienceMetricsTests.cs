using System.Diagnostics.Metrics;
using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.Metrics;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Integration tests for resilience metrics (retries and circuit breaker).
/// Verifies that resilience metrics infrastructure is in place and can record measurements.
/// </summary>
/// <remarks>
/// <para><strong>Note:</strong> These tests verify the metrics infrastructure exists and can record measurements.
/// For full end-to-end resilience testing (triggering actual retries and circuit breaker opens),
/// see ResilienceIntegrationTests.cs.</para>
/// </remarks>
public sealed class ResilienceMetricsTests : IClassFixture<FiskalyClientFixture>, IDisposable
{
    private readonly FiskalyClientFixture _fixture;
    private readonly MetricsCollector _metricsCollector;
    private readonly TestMeterFactory _meterFactory;
    private readonly FiskalyMetrics _metrics;

    public ResilienceMetricsTests(FiskalyClientFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.TestOutputHelper = output;
        _metricsCollector = new MetricsCollector();
        _meterFactory = new TestMeterFactory();
        _metrics = new FiskalyMetrics(_meterFactory);
    }

    public void Dispose()
    {
        _metricsCollector.Dispose();
        _meterFactory.Dispose();
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void ResilienceRetries_Instrument_Should_Exist()
    {
        // Assert
        _metrics.ResilienceRetries.Should().NotBeNull("ResilienceRetries counter should be registered");
        _metrics.ResilienceRetries.Name.Should().Be("fiskaly.resilience.retries");
        _metrics.ResilienceRetries.Unit.Should().Be("{retry}");
        _metrics.ResilienceRetries.Description.Should().Be("Count of retry attempts by resilience pipeline and error category");

        _fixture.TestOutputHelper?.WriteLine("✅ ResilienceRetries instrument exists and has correct metadata");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public void ResilienceCircuitBreakerOpened_Instrument_Should_Exist()
    {
        // Assert
        _metrics.ResilienceCircuitBreakerOpened.Should().NotBeNull("ResilienceCircuitBreakerOpened counter should be registered");
        _metrics.ResilienceCircuitBreakerOpened.Name.Should().Be("fiskaly.resilience.circuit_breaker_opened");
        _metrics.ResilienceCircuitBreakerOpened.Unit.Should().Be("{event}");
        _metrics.ResilienceCircuitBreakerOpened.Description.Should().Be("Count of circuit breaker open events by resilience pipeline");

        _fixture.TestOutputHelper?.WriteLine("✅ ResilienceCircuitBreakerOpened instrument exists and has correct metadata");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task ResilienceRetries_Should_Record_Without_Exception()
    {
        // Arrange
        _metricsCollector.StartCollecting();
        long initialCount = _metricsCollector.GetCounterValue("fiskaly.resilience.retries");

        // Act - Manually record a retry metric
        _metrics.ResilienceRetries.Add(1,
            new KeyValuePair<string, object?>("resilience.pipeline", "TssClient"),
            new KeyValuePair<string, object?>("error.category", "Transient"));

        await Task.Delay(100); // Give metrics time to be recorded

        // Assert
        long finalCount = _metricsCollector.GetCounterValue("fiskaly.resilience.retries");
        finalCount.Should().BeGreaterThan(initialCount, "ResilienceRetries counter should increment");

        List<KeyValuePair<string, object?>> tags = _metricsCollector.GetCounterTags("fiskaly.resilience.retries");
        tags.Should().Contain(kvp => kvp.Key == "resilience.pipeline", "Should have resilience.pipeline tag");
        tags.Should().Contain(kvp => kvp.Key == "error.category", "Should have error.category tag");

        _fixture.TestOutputHelper?.WriteLine($"✅ ResilienceRetries recorded successfully:");
        _fixture.TestOutputHelper?.WriteLine($"   Initial count: {initialCount}");
        _fixture.TestOutputHelper?.WriteLine($"   Final count: {finalCount}");
        _fixture.TestOutputHelper?.WriteLine($"   Tags: {string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task ResilienceCircuitBreakerOpened_Should_Record_Without_Exception()
    {
        // Arrange
        _metricsCollector.StartCollecting();
        long initialCount = _metricsCollector.GetCounterValue("fiskaly.resilience.circuit_breaker_opened");

        // Act - Manually record a circuit breaker opened metric
        _metrics.ResilienceCircuitBreakerOpened.Add(1,
            new KeyValuePair<string, object?>("resilience.pipeline", "TssClient"));

        await Task.Delay(100); // Give metrics time to be recorded

        // Assert
        long finalCount = _metricsCollector.GetCounterValue("fiskaly.resilience.circuit_breaker_opened");
        finalCount.Should().BeGreaterThan(initialCount, "ResilienceCircuitBreakerOpened counter should increment");

        List<KeyValuePair<string, object?>> tags = _metricsCollector.GetCounterTags("fiskaly.resilience.circuit_breaker_opened");
        tags.Should().Contain(kvp => kvp.Key == "resilience.pipeline", "Should have resilience.pipeline tag");

        _fixture.TestOutputHelper?.WriteLine($"✅ ResilienceCircuitBreakerOpened recorded successfully:");
        _fixture.TestOutputHelper?.WriteLine($"   Initial count: {initialCount}");
        _fixture.TestOutputHelper?.WriteLine($"   Final count: {finalCount}");
        _fixture.TestOutputHelper?.WriteLine($"   Tags: {string.Join(", ", tags.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task ResilienceRetries_Should_Support_All_Pipeline_Names()
    {
        // Arrange
        _metricsCollector.StartCollecting();
        string[] pipelineNames = new[] { "AdminClient", "TssClient", "ClientManagementClient", "TransactionClient", "ExportClient" };

        // Act - Record retry for each pipeline
        foreach (string pipelineName in pipelineNames)
        {
            _metrics.ResilienceRetries.Add(1,
                new KeyValuePair<string, object?>("resilience.pipeline", pipelineName),
                new KeyValuePair<string, object?>("error.category", "Transient"));
        }

        await Task.Delay(100); // Give metrics time to be recorded

        // Assert
        long count = _metricsCollector.GetCounterValue("fiskaly.resilience.retries");
        count.Should().BeGreaterThanOrEqualTo(pipelineNames.Length,
            $"Should record retries for all {pipelineNames.Length} pipelines");

        _fixture.TestOutputHelper?.WriteLine($"✅ ResilienceRetries supports all {pipelineNames.Length} pipeline names");
        _fixture.TestOutputHelper?.WriteLine($"   Pipelines: {string.Join(", ", pipelineNames)}");
        _fixture.TestOutputHelper?.WriteLine($"   Total retry count: {count}");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task ResilienceRetries_Should_Support_All_Error_Categories()
    {
        // Arrange
        _metricsCollector.StartCollecting();
        string[] errorCategories = new[] { "Transient", "Infrastructure", "Authentication" };

        // Act - Record retry for each error category
        foreach (string category in errorCategories)
        {
            _metrics.ResilienceRetries.Add(1,
                new KeyValuePair<string, object?>("resilience.pipeline", "TssClient"),
                new KeyValuePair<string, object?>("error.category", category));
        }

        await Task.Delay(100); // Give metrics time to be recorded

        // Assert
        long count = _metricsCollector.GetCounterValue("fiskaly.resilience.retries");
        count.Should().BeGreaterThanOrEqualTo(errorCategories.Length,
            $"Should record retries for all {errorCategories.Length} error categories");

        _fixture.TestOutputHelper?.WriteLine($"✅ ResilienceRetries supports all {errorCategories.Length} retryable error categories");
        _fixture.TestOutputHelper?.WriteLine($"   Categories: {string.Join(", ", errorCategories)}");
        _fixture.TestOutputHelper?.WriteLine($"   Total retry count: {count}");
    }

    /// <summary>
    /// Test implementation of IMeterFactory that captures the created meter.
    /// </summary>
    private sealed class TestMeterFactory : IMeterFactory, IDisposable
    {
        public Meter? CreatedMeter { get; private set; }

        public Meter Create(MeterOptions options)
        {
            CreatedMeter = new Meter(options.Name, options.Version);
            return CreatedMeter;
        }

        public void Dispose()
        {
            CreatedMeter?.Dispose();
        }
    }

    /// <summary>
    /// Helper class to collect metrics using MeterListener.
    /// </summary>
    private sealed class MetricsCollector : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly Dictionary<string, long> _counterValues = new();
        private readonly Dictionary<string, List<KeyValuePair<string, object?>>> _counterTags = new();

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

        public void Dispose()
        {
            _listener.Dispose();
        }
    }
}
