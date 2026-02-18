using AwesomeAssertions;
using Vectis.Fiskaly.SDK.Exceptions;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Fiskaly;

/// <summary>
/// Tests verifying that all 45 Fiskaly error codes have complete and correct metadata.
/// Uses InternalsVisibleTo to access ErrorCodeMetadata for comprehensive validation.
/// </summary>
/// <remarks>
/// These tests ensure:
/// - All error codes (except Unknown) have metadata entries
/// - Metadata is consistent (Category matches IsRetryable expectations)
/// - All error codes have valid HTTP status codes and recovery hints
/// - Metadata count matches enum count
/// </remarks>
public sealed class ErrorCodeCoverageTests
{
    private readonly ITestOutputHelper _output;

    public ErrorCodeCoverageTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Verifies that all error codes (except Unknown) have metadata entries in ErrorCodeMetadata.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void AllErrorCodes_ExceptUnknown_ShouldHaveMetadata()
    {
        // Arrange - Get all error code enum values except Unknown
        List<FiskalyErrorCode> allErrorCodes = Enum.GetValues<FiskalyErrorCode>()
            .Where(code => code != FiskalyErrorCode.Unknown)
            .ToList();

        _output.WriteLine($"Testing {allErrorCodes.Count} error codes for metadata presence...");

        // Act & Assert - Verify each error code has metadata
        List<FiskalyErrorCode> missingMetadata = new List<FiskalyErrorCode>();

        foreach (FiskalyErrorCode errorCode in allErrorCodes)
        {
            Metadata metadata = ErrorCodeMetadata.Get(errorCode);

            // Verify metadata exists and has valid values
            // Note: Category can be any enum value (Permanent is 0, which is valid)
            // Note: HttpStatusCode should be >= 400 for client/server errors
            int statusCodeValue = (int)metadata.HttpStatusCode;

            if (statusCodeValue < 400 || statusCodeValue > 599 ||
                string.IsNullOrWhiteSpace(metadata.RecoveryHint))
            {
                missingMetadata.Add(errorCode);
                _output.WriteLine($"❌ {errorCode}: Missing or incomplete metadata (Status={statusCodeValue}, Hint={metadata.RecoveryHint?.Length ?? 0} chars)");
            }
        }

        // Assert
        missingMetadata.Should().BeEmpty(
            $"All {allErrorCodes.Count} error codes should have complete metadata. " +
            $"Missing: {string.Join(", ", missingMetadata)}");

        _output.WriteLine($"✅ All {allErrorCodes.Count} error codes have complete metadata");
    }

    /// <summary>
    /// Verifies that all Permanent error codes have IsRetryable = false.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void AllPermanentErrors_ShouldNotBeRetryable()
    {
        // Arrange - Get all error codes
        List<FiskalyErrorCode> allErrorCodes = Enum.GetValues<FiskalyErrorCode>()
            .Where(code => code != FiskalyErrorCode.Unknown)
            .ToList();

        // Act - Find all permanent errors
        List<FiskalyErrorCode> permanentErrors = allErrorCodes
            .Where(code => ErrorCodeMetadata.Get(code).Category == FiskalyErrorCategory.Permanent)
            .ToList();

        _output.WriteLine($"Found {permanentErrors.Count} permanent error codes");

        // Assert - All permanent errors should NOT be retryable
        List<FiskalyErrorCode> retryablePermanentErrors = permanentErrors
            .Where(code => ErrorCodeMetadata.Get(code).IsRetryable)
            .ToList();

        retryablePermanentErrors.Should().BeEmpty(
            $"Permanent errors should never be retryable. " +
            $"Retryable permanent errors: {string.Join(", ", retryablePermanentErrors)}");

        _output.WriteLine($"✅ All {permanentErrors.Count} permanent errors have IsRetryable = false");
    }

    /// <summary>
    /// Verifies that all Transient error codes have IsRetryable = true.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void AllTransientErrors_ShouldBeRetryable()
    {
        // Arrange - Get all error codes
        List<FiskalyErrorCode> allErrorCodes = Enum.GetValues<FiskalyErrorCode>()
            .Where(code => code != FiskalyErrorCode.Unknown)
            .ToList();

        // Act - Find all transient errors
        List<FiskalyErrorCode> transientErrors = allErrorCodes
            .Where(code => ErrorCodeMetadata.Get(code).Category == FiskalyErrorCategory.Transient)
            .ToList();

        _output.WriteLine($"Found {transientErrors.Count} transient error codes");

        // Assert - All transient errors should be retryable
        List<FiskalyErrorCode> nonRetryableTransientErrors = transientErrors
            .Where(code => !ErrorCodeMetadata.Get(code).IsRetryable)
            .ToList();

        nonRetryableTransientErrors.Should().BeEmpty(
            $"Transient errors should always be retryable. " +
            $"Non-retryable transient errors: {string.Join(", ", nonRetryableTransientErrors)}");

        _output.WriteLine($"✅ All {transientErrors.Count} transient errors have IsRetryable = true");
    }

    /// <summary>
    /// Verifies that all Infrastructure error codes have IsRetryable = true.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void AllInfrastructureErrors_ShouldBeRetryable()
    {
        // Arrange - Get all error codes
        List<FiskalyErrorCode> allErrorCodes = Enum.GetValues<FiskalyErrorCode>()
            .Where(code => code != FiskalyErrorCode.Unknown)
            .ToList();

        // Act - Find all infrastructure errors
        List<FiskalyErrorCode> infrastructureErrors = allErrorCodes
            .Where(code => ErrorCodeMetadata.Get(code).Category == FiskalyErrorCategory.Infrastructure)
            .ToList();

        _output.WriteLine($"Found {infrastructureErrors.Count} infrastructure error codes");

        // Assert - All infrastructure errors should be retryable
        List<FiskalyErrorCode> nonRetryableInfrastructureErrors = infrastructureErrors
            .Where(code => !ErrorCodeMetadata.Get(code).IsRetryable)
            .ToList();

        nonRetryableInfrastructureErrors.Should().BeEmpty(
            $"Infrastructure errors should always be retryable. " +
            $"Non-retryable infrastructure errors: {string.Join(", ", nonRetryableInfrastructureErrors)}");

        _output.WriteLine($"✅ All {infrastructureErrors.Count} infrastructure errors have IsRetryable = true");
    }

    /// <summary>
    /// Verifies that all Authentication error codes have IsRetryable = true.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void AllAuthenticationErrors_ShouldBeRetryable()
    {
        // Arrange - Get all error codes
        List<FiskalyErrorCode> allErrorCodes = Enum.GetValues<FiskalyErrorCode>()
            .Where(code => code != FiskalyErrorCode.Unknown)
            .ToList();

        // Act - Find all authentication errors
        List<FiskalyErrorCode> authenticationErrors = allErrorCodes
            .Where(code => ErrorCodeMetadata.Get(code).Category == FiskalyErrorCategory.Authentication)
            .ToList();

        _output.WriteLine($"Found {authenticationErrors.Count} authentication error codes");

        // Assert - All authentication errors should be retryable
        List<FiskalyErrorCode> nonRetryableAuthErrors = authenticationErrors
            .Where(code => !ErrorCodeMetadata.Get(code).IsRetryable)
            .ToList();

        nonRetryableAuthErrors.Should().BeEmpty(
            $"Authentication errors should always be retryable. " +
            $"Non-retryable authentication errors: {string.Join(", ", nonRetryableAuthErrors)}");

        _output.WriteLine($"✅ All {authenticationErrors.Count} authentication errors have IsRetryable = true");
    }

    /// <summary>
    /// Verifies that all error codes have valid HTTP status codes (400-599 range).
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void AllErrorCodes_ShouldHaveValidHttpStatusCode()
    {
        // Arrange - Get all error codes except Unknown
        List<FiskalyErrorCode> allErrorCodes = Enum.GetValues<FiskalyErrorCode>()
            .Where(code => code != FiskalyErrorCode.Unknown)
            .ToList();

        _output.WriteLine($"Testing {allErrorCodes.Count} error codes for valid HTTP status codes...");

        // Act & Assert - Verify each error code has valid HTTP status code
        List<(FiskalyErrorCode Code, int StatusCode)> invalidStatusCodes = new List<(FiskalyErrorCode Code, int StatusCode)>();

        foreach (FiskalyErrorCode errorCode in allErrorCodes)
        {
            Metadata metadata = ErrorCodeMetadata.Get(errorCode);
            int statusCodeValue = (int)metadata.HttpStatusCode;

            // HTTP error codes should be in 400-599 range
            if (statusCodeValue < 400 || statusCodeValue > 599)
            {
                invalidStatusCodes.Add((errorCode, statusCodeValue));
                _output.WriteLine($"❌ {errorCode}: Invalid HTTP status code {statusCodeValue}");
            }
        }

        // Assert
        invalidStatusCodes.Should().BeEmpty(
            $"All error codes should have HTTP status codes in 400-599 range. " +
            $"Invalid: {string.Join(", ", invalidStatusCodes.Select(x => $"{x.Code}={x.StatusCode}"))}");

        _output.WriteLine($"✅ All {allErrorCodes.Count} error codes have valid HTTP status codes (400-599)");
    }

    /// <summary>
    /// Verifies that all error codes have non-empty recovery hints.
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void AllErrorCodes_ShouldHaveNonEmptyRecoveryHint()
    {
        // Arrange - Get all error codes except Unknown
        List<FiskalyErrorCode> allErrorCodes = Enum.GetValues<FiskalyErrorCode>()
            .Where(code => code != FiskalyErrorCode.Unknown)
            .ToList();

        _output.WriteLine($"Testing {allErrorCodes.Count} error codes for recovery hints...");

        // Act & Assert - Verify each error code has non-empty recovery hint
        List<FiskalyErrorCode> missingRecoveryHints = new List<FiskalyErrorCode>();

        foreach (FiskalyErrorCode errorCode in allErrorCodes)
        {
            Metadata metadata = ErrorCodeMetadata.Get(errorCode);

            if (string.IsNullOrWhiteSpace(metadata.RecoveryHint))
            {
                missingRecoveryHints.Add(errorCode);
                _output.WriteLine($"❌ {errorCode}: Missing recovery hint");
            }
        }

        // Assert
        missingRecoveryHints.Should().BeEmpty(
            $"All error codes should have recovery hints. " +
            $"Missing: {string.Join(", ", missingRecoveryHints)}");

        _output.WriteLine($"✅ All {allErrorCodes.Count} error codes have non-empty recovery hints");
    }

    /// <summary>
    /// Verifies that the metadata dictionary contains exactly 45 entries (one for each error code except Unknown).
    /// </summary>
    [Trait("Category", "Integration")]
    [Fact]
    public void ErrorCodeMetadata_Count_ShouldMatch_EnumCount()
    {
        // Arrange - Count enum values (excluding Unknown)
        int enumCount = Enum.GetValues<FiskalyErrorCode>()
            .Count(code => code != FiskalyErrorCode.Unknown);

        _output.WriteLine($"Expected metadata count: {enumCount} (total enum values minus Unknown)");

        // Act - Get metadata count by checking all error codes
        int errorCodesWithMetadata = Enum.GetValues<FiskalyErrorCode>()
            .Where(code => code != FiskalyErrorCode.Unknown)
            .Count(code =>
            {
                Metadata metadata = ErrorCodeMetadata.Get(code);
                // Note: Category can be any enum value (Permanent is 0, which is valid)
                // Note: HttpStatusCode should be >= 400 for client/server errors
                int statusCodeValue = (int)metadata.HttpStatusCode;
                return statusCodeValue >= 400 && statusCodeValue <= 599 &&
                       !string.IsNullOrWhiteSpace(metadata.RecoveryHint);
            });

        // Assert
        errorCodesWithMetadata.Should().Be(enumCount,
            $"ErrorCodeMetadata should have exactly {enumCount} complete entries");

        _output.WriteLine($"✅ ErrorCodeMetadata has {errorCodesWithMetadata} complete entries (matches enum count)");
    }
}
