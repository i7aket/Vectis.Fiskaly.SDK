using System.Diagnostics.Metrics;
using AwesomeAssertions;
using Vectis.Fiskaly.SDK.Metrics;

namespace Vectis.Fiskaly.UnitTests.Metrics;

/// <summary>
/// Unit tests for FiskalyMetrics instrumentation.
/// Verifies that all metrics instruments are created correctly and can record measurements.
/// </summary>
public sealed class FiskalyMetricsTests : IDisposable
{
    private readonly TestMeterFactory _meterFactory;
    private readonly FiskalyMetrics _metrics;

    public FiskalyMetricsTests()
    {
        _meterFactory = new TestMeterFactory();
        _metrics = new FiskalyMetrics(_meterFactory);
    }

    public void Dispose()
    {
        _meterFactory.Dispose();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_Should_Create_Meter_With_Correct_Name_And_Version()
    {
        // Assert
        _meterFactory.CreatedMeter.Should().NotBeNull();
        _meterFactory.CreatedMeter!.Name.Should().Be("Vectis.Fiskaly.SDK");
        _meterFactory.CreatedMeter.Version.Should().Be("1.0");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_Should_Create_ErrorsTotal_Counter()
    {
        // Assert
        _metrics.ErrorsTotal.Should().NotBeNull();
        _metrics.ErrorsTotal.Name.Should().Be("fiskaly.errors.total");
        _metrics.ErrorsTotal.Unit.Should().Be("{error}");
        _metrics.ErrorsTotal.Description.Should().Be("Total count of Fiskaly API errors by error code and category");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_Should_Create_ErrorsByOperation_Counter()
    {
        // Assert
        _metrics.ErrorsByOperation.Should().NotBeNull();
        _metrics.ErrorsByOperation.Name.Should().Be("fiskaly.errors.by_operation");
        _metrics.ErrorsByOperation.Unit.Should().Be("{error}");
        _metrics.ErrorsByOperation.Description.Should().Be("Count of Fiskaly API errors by operation (API endpoint) and category");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_Should_Create_ErrorsByStatus_Counter()
    {
        // Assert
        _metrics.ErrorsByStatus.Should().NotBeNull();
        _metrics.ErrorsByStatus.Name.Should().Be("fiskaly.errors.by_status");
        _metrics.ErrorsByStatus.Unit.Should().Be("{error}");
        _metrics.ErrorsByStatus.Description.Should().Be("Count of Fiskaly API errors by HTTP status code and category");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_Should_Create_ResilienceRetries_Counter()
    {
        // Assert
        _metrics.ResilienceRetries.Should().NotBeNull();
        _metrics.ResilienceRetries.Name.Should().Be("fiskaly.resilience.retries");
        _metrics.ResilienceRetries.Unit.Should().Be("{retry}");
        _metrics.ResilienceRetries.Description.Should().Be("Count of retry attempts by resilience pipeline and error category");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_Should_Create_ResilienceCircuitBreakerOpened_Counter()
    {
        // Assert
        _metrics.ResilienceCircuitBreakerOpened.Should().NotBeNull();
        _metrics.ResilienceCircuitBreakerOpened.Name.Should().Be("fiskaly.resilience.circuit_breaker_opened");
        _metrics.ResilienceCircuitBreakerOpened.Unit.Should().Be("{event}");
        _metrics.ResilienceCircuitBreakerOpened.Description.Should().Be("Count of circuit breaker open events by resilience pipeline");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_Should_Create_ErrorHandlingDuration_Histogram()
    {
        // Assert
        _metrics.ErrorHandlingDuration.Should().NotBeNull();
        _metrics.ErrorHandlingDuration.Name.Should().Be("fiskaly.error_handling.duration");
        _metrics.ErrorHandlingDuration.Unit.Should().Be("s");
        _metrics.ErrorHandlingDuration.Description.Should().Be("Duration of error handling operations (parsing, metadata lookup, exception creation)");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ErrorsTotal_Should_Record_Without_Exception()
    {
        // Act
        Action act = () => _metrics.ErrorsTotal.Add(1,
            new KeyValuePair<string, object?>("error.code", "E_TSS_NOT_FOUND"),
            new KeyValuePair<string, object?>("error.category", "Permanent"));

        // Assert
        act.Should().NotThrow();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ErrorsByOperation_Should_Record_Without_Exception()
    {
        // Act
        Action act = () => _metrics.ErrorsByOperation.Add(1,
            new KeyValuePair<string, object?>("operation", "GET /tss/{id}"),
            new KeyValuePair<string, object?>("error.category", "Permanent"));

        // Assert
        act.Should().NotThrow();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ErrorsByStatus_Should_Record_Without_Exception()
    {
        // Act
        Action act = () => _metrics.ErrorsByStatus.Add(1,
            new KeyValuePair<string, object?>("http.status_code", "404"),
            new KeyValuePair<string, object?>("error.category", "Permanent"));

        // Assert
        act.Should().NotThrow();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ResilienceRetries_Should_Record_Without_Exception()
    {
        // Act
        Action act = () => _metrics.ResilienceRetries.Add(1,
            new KeyValuePair<string, object?>("resilience.pipeline", "TssClient"),
            new KeyValuePair<string, object?>("error.category", "Transient"));

        // Assert
        act.Should().NotThrow();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ResilienceCircuitBreakerOpened_Should_Record_Without_Exception()
    {
        // Act
        Action act = () => _metrics.ResilienceCircuitBreakerOpened.Add(1,
            new KeyValuePair<string, object?>("resilience.pipeline", "TssClient"));

        // Assert
        act.Should().NotThrow();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ErrorHandlingDuration_Should_Record_Without_Exception()
    {
        // Act
        Action act = () => _metrics.ErrorHandlingDuration.Record(0.123,
            new KeyValuePair<string, object?>("error.code", "E_TSS_NOT_FOUND"),
            new KeyValuePair<string, object?>("error.category", "Permanent"));

        // Assert
        act.Should().NotThrow();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_Should_Throw_When_MeterFactory_Is_Null()
    {
        // Act
        Action act = () => new FiskalyMetrics(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("meterFactory");
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
}
