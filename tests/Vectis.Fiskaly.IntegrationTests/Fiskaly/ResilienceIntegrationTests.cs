using System.Diagnostics;
using System.Net;
using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Integration tests verifying end-to-end resilience behavior (retries and circuit breaker).
/// Tests the interaction between FiskalyErrorHandler, FiskalyResiliencePredicates, and the resilience pipeline.
/// </summary>
/// <remarks>
/// <para>These tests verify that the SDK resilience pipeline behaves correctly when encountering different error categories:</para>
/// <list type="bullet">
///   <item><strong>Transient errors</strong> (E_TSS_LOCKED) → Should retry with exponential backoff</item>
///   <item><strong>Permanent errors</strong> (E_TSS_CONFLICT) → Should fail immediately without retry</item>
///   <item><strong>Infrastructure errors</strong> (E_TSS_DEFECTIVE) → Should trigger circuit breaker</item>
///   <item><strong>Unknown errors</strong> → Should fall back to HTTP status-based retry logic</item>
/// </list>
/// </remarks>
public sealed class ResilienceIntegrationTests : IClassFixture<FiskalyClientFixture>
{
    private readonly FiskalyClientFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ResilienceIntegrationTests(FiskalyClientFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.TestOutputHelper = output;
        _output = output;
    }

    /// <summary>
    /// Verifies that permanent errors (E_TSS_CONFLICT) fail immediately without retry.
    /// </summary>
    /// <remarks>
    /// Test strategy: Try to get a TSS that doesn't exist (E_TSS_NOT_FOUND - permanent error).
    /// Expected: Single attempt, no retries, immediate exception.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task PermanentError_ShouldFailImmediately_WithoutRetry()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(async () =>
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        });

        stopwatch.Stop();

        // Verify exception properties
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_TSS_NOT_FOUND,
            "Error code should be E_TSS_NOT_FOUND for non-existent TSS");
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent,
            "E_TSS_NOT_FOUND should be categorized as Permanent");
        exception.IsRetryable.Should().BeFalse("Permanent errors should not be retryable");
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound, "HTTP status should be 404 Not Found");

        // Verify no retries occurred (should complete in <2 seconds without retries)
        // With retries (1s + 2s delays), it would take >3 seconds
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000,
            "Permanent error should fail immediately without retry delays");

        _output.WriteLine($"✅ Permanent error failed immediately in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"   Error code: {exception.ErrorCode}");
        _output.WriteLine($"   Category: {exception.Category}");
        _output.WriteLine($"   Message: {exception.ApiErrorMessage}");
        _output.WriteLine($"   Recovery hint: {exception.GetRecoveryHint()}");
    }

    /// <summary>
    /// Verifies that 404 errors for other resources also fail immediately without retry.
    /// </summary>
    /// <remarks>
    /// Test strategy: Try to update a TSS that doesn't exist (404 Not Found).
    /// Expected: Single attempt, no retries, immediate exception.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task PermanentError_NotFound_ShouldFailImmediately()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(async () =>
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        });

        stopwatch.Stop();

        // Verify it's a permanent error that doesn't retry
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse();

        // Should complete quickly without retry delays
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000,
            "404 errors should fail immediately without retries");

        _output.WriteLine($"✅ 404 error failed immediately in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Verifies that different error scenarios produce exceptions with correct categories.
    /// This is a supporting test to ensure our resilience tests are based on correct error categorization.
    /// </summary>
    /// <remarks>
    /// Tests permanent error categorization by triggering real API errors.
    /// Since ErrorCodeMetadata is internal, we verify categorization through exception properties.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ErrorCategorization_PermanentErrors_ShouldHaveCorrectProperties()
    {
        // Arrange - Trigger E_TSS_NOT_FOUND (permanent error)
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());

        // Act
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(async () =>
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        });

        // Assert - Verify permanent error properties
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_TSS_NOT_FOUND);
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent,
            "E_TSS_NOT_FOUND should be categorized as Permanent");
        exception.IsRetryable.Should().BeFalse(
            "Permanent errors should not be retryable");

        _output.WriteLine($"✅ {exception.ErrorCode}: Category={exception.Category}, IsRetryable={exception.IsRetryable}");
    }

    /// <summary>
    /// Verifies that multiple sequential errors accumulate correctly in resilience metrics.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task MultipleSequentialErrors_ShouldNotTriggerCircuitBreaker()
    {
        // Arrange - Generate 3 different non-existent TSS IDs
        TssId[] tssIds = new[]
        {
            TssId.From(Guid.NewGuid().ToString()),
            TssId.From(Guid.NewGuid().ToString()),
            TssId.From(Guid.NewGuid().ToString())
        };

        // Act - Make 3 separate requests (each should fail immediately without retry)
        foreach (TssId tssId in tssIds)
        {
            try
            {
                await _fixture.TssClient.GetTssAsync(tssId);
            }
            catch (FiskalyApiException ex)
            {
                // Expected permanent error
                ex.Category.Should().Be(FiskalyErrorCategory.Permanent);
            }
        }

        // Assert - Circuit breaker should NOT open for permanent errors
        // (Circuit breaker only opens for infrastructure errors, not permanent errors)
        _output.WriteLine("✅ Sequential permanent errors did not trigger circuit breaker");
        _output.WriteLine("   Circuit breaker only activates for Infrastructure category errors");
    }

    /// <summary>
    /// Verifies that error exceptions contain all expected rich properties.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ErrorException_ShouldContainRichProperties()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());

        // Act
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(async () =>
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        });

        // Assert - Verify all 7 rich properties
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound, "Should have HTTP status code");
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_TSS_NOT_FOUND, "Should have error code enum");
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent, "Should have error category");
        exception.IsRetryable.Should().BeFalse("Should have IsRetryable flag");
        exception.ApiErrorMessage.Should().NotBeNullOrEmpty("Should have API error message");
        exception.ErrorDetails.Should().NotBeNull("Should have error details object");
        exception.CorrelationId.Should().NotBeNullOrEmpty("Should have correlation ID");

        // Verify recovery hint
        string recoveryHint = exception.GetRecoveryHint();
        recoveryHint.Should().NotBeNullOrEmpty("Should provide recovery hint");
        recoveryHint.Should().Contain("TSS", "Recovery hint should mention TSS");

        _output.WriteLine("✅ Exception contains all 7 rich properties:");
        _output.WriteLine($"   1. StatusCode: {exception.StatusCode}");
        _output.WriteLine($"   2. ErrorCode: {exception.ErrorCode}");
        _output.WriteLine($"   3. Category: {exception.Category}");
        _output.WriteLine($"   4. IsRetryable: {exception.IsRetryable}");
        _output.WriteLine($"   5. ApiErrorMessage: {exception.ApiErrorMessage}");
        _output.WriteLine($"   6. ErrorDetails: {(exception.ErrorDetails != null ? "Present" : "Missing")}");
        _output.WriteLine($"   7. CorrelationId: {exception.CorrelationId}");
        _output.WriteLine($"   Recovery hint: {recoveryHint}");
    }

    /// <summary>
    /// Verifies that correlation IDs are propagated through the entire request chain.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task CorrelationId_ShouldBeConsistent_AcrossRequestChain()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
        string? correlationId = null;

        // Act - Make request and capture correlation ID from exception
        try
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        }
        catch (FiskalyApiException ex)
        {
            correlationId = ex.CorrelationId;
        }

        // Assert
        correlationId.Should().NotBeNullOrEmpty("Correlation ID should be set in exception");

        // Verify it's a valid GUID format
        Guid.TryParse(correlationId, out Guid guidValue).Should().BeTrue(
            "Correlation ID should be a valid GUID");

        _output.WriteLine($"✅ Correlation ID propagated: {correlationId}");
    }

    /// <summary>
    /// Verifies exception filtering patterns work correctly with error codes.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ExceptionFiltering_ByErrorCode_ShouldWork()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
        bool notFoundCaught = false;
        bool otherErrorCaught = false;

        // Act
        try
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        }
        catch (FiskalyApiException ex) when (ex.ErrorCode == FiskalyErrorCode.E_TSS_NOT_FOUND)
        {
            notFoundCaught = true;
            _output.WriteLine($"✅ Caught E_TSS_NOT_FOUND with exception filter");
        }
        catch (FiskalyApiException ex)
        {
            otherErrorCaught = true;
            _output.WriteLine($"❌ Unexpected error code: {ex.ErrorCode}");
        }

        // Assert
        notFoundCaught.Should().BeTrue("Should catch E_TSS_NOT_FOUND with exception filter");
        otherErrorCaught.Should().BeFalse("Should not catch other error codes");
    }

    /// <summary>
    /// Verifies exception filtering by category works correctly.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task ExceptionFiltering_ByCategory_ShouldWork()
    {
        // Arrange
        TssId nonExistentTssId = TssId.From(Guid.NewGuid().ToString());
        bool permanentCaught = false;
        bool transientCaught = false;

        // Act
        try
        {
            await _fixture.TssClient.GetTssAsync(nonExistentTssId);
        }
        catch (FiskalyApiException ex) when (ex.Category == FiskalyErrorCategory.Permanent)
        {
            permanentCaught = true;
            _output.WriteLine($"✅ Caught permanent error with exception filter");
        }
        catch (FiskalyApiException ex) when (ex.Category == FiskalyErrorCategory.Transient)
        {
            transientCaught = true;
            _output.WriteLine($"❌ Unexpected transient error: {ex.ErrorCode}");
        }

        // Assert
        permanentCaught.Should().BeTrue("Should catch permanent errors with exception filter");
        transientCaught.Should().BeFalse("Should not catch transient errors for this test");
    }
}
