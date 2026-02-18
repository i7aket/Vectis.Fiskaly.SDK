using System.Net;
using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.UnitTests.Exceptions;

public class ErrorCodeMetadataTests
{
    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(FiskalyErrorCode.E_TSS_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.NotFound)]
    [InlineData(FiskalyErrorCode.E_TSS_LOCKED, FiskalyErrorCategory.Transient, true, HttpStatusCode.ServiceUnavailable)]
    [InlineData(FiskalyErrorCode.E_UNAUTHORIZED, FiskalyErrorCategory.Authentication, true, HttpStatusCode.Unauthorized)]
    [InlineData(FiskalyErrorCode.E_TSS_EVICTED, FiskalyErrorCategory.Infrastructure, true, (HttpStatusCode)423)]
    public void Get_KnownErrorCode_ReturnsCorrectMetadata(
        FiskalyErrorCode errorCode,
        FiskalyErrorCategory expectedCategory,
        bool expectedRetryable,
        HttpStatusCode expectedStatusCode)
    {
        Metadata metadata = ErrorCodeMetadata.Get(errorCode);

        Assert.Equal(expectedCategory, metadata.Category);
        Assert.Equal(expectedRetryable, metadata.IsRetryable);
        Assert.Equal(expectedStatusCode, metadata.HttpStatusCode);
        Assert.NotEmpty(metadata.RecoveryHint);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Get_UnknownErrorCode_ReturnsDefaultMetadata()
    {
        Metadata metadata = ErrorCodeMetadata.Get((FiskalyErrorCode)99999);

        Assert.Equal(FiskalyErrorCategory.Permanent, metadata.Category);
        Assert.False(metadata.IsRetryable);
        Assert.Equal(HttpStatusCode.InternalServerError, metadata.HttpStatusCode);
        Assert.Contains("Unknown error code", metadata.RecoveryHint);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(FiskalyErrorCode.E_TSS_LOCKED, "SDK retries automatically")]
    [InlineData(FiskalyErrorCode.E_TSS_NOT_FOUND, "Verify TSS ID")]
    [InlineData(FiskalyErrorCode.E_CLIENT_LIMIT_REACHED, "Upgrade plan")]
    public void Get_ValidErrorCode_ReturnsActionableRecoveryHint(FiskalyErrorCode errorCode, string expectedHintFragment)
    {
        Metadata metadata = ErrorCodeMetadata.Get(errorCode);

        Assert.Contains(expectedHintFragment, metadata.RecoveryHint);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Get_AllPermanentErrors_AreNotRetryable()
    {
        FiskalyErrorCode[] permanentCodes = new[]
        {
            FiskalyErrorCode.E_TSS_NOT_FOUND,
            FiskalyErrorCode.E_TSS_CONFLICT,
            FiskalyErrorCode.E_TSS_DISABLED,
            FiskalyErrorCode.E_CLIENT_NOT_FOUND,
            FiskalyErrorCode.E_EXPORT_NOT_FOUND
        };

        foreach (FiskalyErrorCode code in permanentCodes)
        {
            Metadata metadata = ErrorCodeMetadata.Get(code);
            Assert.Equal(FiskalyErrorCategory.Permanent, metadata.Category);
            Assert.False(metadata.IsRetryable);
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Get_AllTransientErrors_AreRetryable()
    {
        FiskalyErrorCode[] transientCodes = new[]
        {
            FiskalyErrorCode.E_TSS_LOCKED,
            FiskalyErrorCode.E_PENDING_TX_CONFLICT,
            FiskalyErrorCode.E_EXPORT_NOT_COMPLETED
        };

        foreach (FiskalyErrorCode code in transientCodes)
        {
            Metadata metadata = ErrorCodeMetadata.Get(code);
            Assert.Equal(FiskalyErrorCategory.Transient, metadata.Category);
            Assert.True(metadata.IsRetryable);
        }
    }

    // ============================================================================
    // Comprehensive tests for ALL 49 error codes (45 from v2.1.34 + 4 legacy from v2.1.33)
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    // TSS Errors (12 codes) - Updated to match OpenAPI v2.1.34
    [InlineData(FiskalyErrorCode.E_TSS_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.NotFound)]
    [InlineData(FiskalyErrorCode.E_TSS_CONFLICT, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Conflict)]
    [InlineData(FiskalyErrorCode.E_TSS_DISABLED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_TSS_NOT_INITIALIZED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_TSS_DEFECTIVE, FiskalyErrorCategory.Infrastructure, true, (HttpStatusCode)423)]
    [InlineData(FiskalyErrorCode.E_TSS_DELETED, FiskalyErrorCategory.Permanent, false, (HttpStatusCode)423)]
    [InlineData(FiskalyErrorCode.E_TSS_EVICTED, FiskalyErrorCategory.Infrastructure, true, (HttpStatusCode)423)]
    [InlineData(FiskalyErrorCode.E_TSS_LOCKED, FiskalyErrorCategory.Transient, true, HttpStatusCode.ServiceUnavailable)]
    [InlineData(FiskalyErrorCode.E_TSS_LIMIT_REACHED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Forbidden)]
    [InlineData(FiskalyErrorCode.E_TSS_LIMIT_PER_DAY_REACHED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Forbidden)]
    [InlineData(FiskalyErrorCode.E_ILLEGAL_TSS_STATE_CHANGE, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_TSS_ILLEGAL_STATE_TO_PERFORM_EXPORT, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Conflict)]
    // Transaction Errors (7 codes) - Updated to match OpenAPI v2.1.34
    [InlineData(FiskalyErrorCode.E_TX_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.NotFound)]
    [InlineData(FiskalyErrorCode.E_TX_UPSERT, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_TX_NO_TYPE_DEFINED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Conflict)]
    [InlineData(FiskalyErrorCode.E_TX_ILLEGAL_TYPE_CHANGE, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Conflict)]
    [InlineData(FiskalyErrorCode.E_TX_LIMIT_REACHED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_TX_REVISION_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_PENDING_TX_CONFLICT, FiskalyErrorCategory.Transient, true, HttpStatusCode.Conflict)]
    // Client Errors (6 codes) - Updated to match OpenAPI v2.1.34
    [InlineData(FiskalyErrorCode.E_CLIENT_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.NotFound)]
    [InlineData(FiskalyErrorCode.E_CLIENT_CONFLICT, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Conflict)]
    [InlineData(FiskalyErrorCode.E_CLIENT_DEREGISTERED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_CLIENT_LIMIT_REACHED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Forbidden)]
    [InlineData(FiskalyErrorCode.E_ILLEGAL_CLIENT_SERIAL, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_LATEST_TX_REVISION_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.NotFound)]
    // Export Errors (8 codes) - Updated to match OpenAPI v2.1.34
    [InlineData(FiskalyErrorCode.E_EXPORT_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.NotFound)]
    [InlineData(FiskalyErrorCode.E_EXPORT_NOT_COMPLETED, FiskalyErrorCategory.Transient, true, (HttpStatusCode)425)]
    [InlineData(FiskalyErrorCode.E_EXPORT_FORBIDDEN, FiskalyErrorCategory.Permanent, false, (HttpStatusCode)423)]
    [InlineData(FiskalyErrorCode.E_EXPORT_TEMPORARILY_UNAVAILABLE, FiskalyErrorCategory.Transient, true, HttpStatusCode.ServiceUnavailable)]
    [InlineData(FiskalyErrorCode.E_DUPLICATE_EXPORT, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_EXPORT_DUPLICATE_RATE_LIMITED, FiskalyErrorCategory.Transient, true, HttpStatusCode.Conflict)]
    [InlineData(FiskalyErrorCode.E_TOO_MANY_EXPORTS, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Conflict)]
    [InlineData(FiskalyErrorCode.E_CANCEL_EXPORT, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    // Authentication Errors (5 codes) - Updated to match OpenAPI v2.1.34
    [InlineData(FiskalyErrorCode.E_UNAUTHORIZED, FiskalyErrorCategory.Authentication, true, HttpStatusCode.Unauthorized)]
    [InlineData(FiskalyErrorCode.E_ADMIN_LOGIN_FAILED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Unauthorized)]
    [InlineData(FiskalyErrorCode.E_ADMIN_PIN_BLOCKED, FiskalyErrorCategory.Permanent, false, (HttpStatusCode)423)]
    [InlineData(FiskalyErrorCode.E_CHANGE_ADMIN_PIN_FAILED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_ACCESS_DENIED, FiskalyErrorCategory.Permanent, false, HttpStatusCode.Forbidden)]
    // Infrastructure Errors (4 codes) - Updated to match OpenAPI v2.1.34
    [InlineData(FiskalyErrorCode.E_SMAERS_GATEWAY_CAPACITIES_DEPLETED, FiskalyErrorCategory.Infrastructure, true, HttpStatusCode.ServiceUnavailable)]
    [InlineData(FiskalyErrorCode.E_BOOTSTRAP_FILE_NOT_AVAILABLE, FiskalyErrorCategory.Infrastructure, true, HttpStatusCode.ServiceUnavailable)]
    [InlineData(FiskalyErrorCode.E_USE_MIDDLEWARE, FiskalyErrorCategory.Infrastructure, true, (HttpStatusCode)432)]
    [InlineData(FiskalyErrorCode.E_MIDDLEWARE_PENDING_REQUEST, FiskalyErrorCategory.Transient, true, HttpStatusCode.Conflict)]
    // General Errors (3 codes) - OpenAPI v2.1.34
    [InlineData(FiskalyErrorCode.E_PARAMETER_MISMATCH, FiskalyErrorCategory.Permanent, false, HttpStatusCode.BadRequest)]
    [InlineData(FiskalyErrorCode.E_LOG_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.NotFound)]
    [InlineData(FiskalyErrorCode.E_CSPL_NOT_FOUND, FiskalyErrorCategory.Permanent, false, HttpStatusCode.NotFound)]
    public void Get_AllErrorCodes_ReturnCorrectMetadata(
        FiskalyErrorCode errorCode,
        FiskalyErrorCategory expectedCategory,
        bool expectedRetryable,
        HttpStatusCode expectedStatusCode)
    {
        Metadata metadata = ErrorCodeMetadata.Get(errorCode);

        Assert.Equal(expectedCategory, metadata.Category);
        Assert.Equal(expectedRetryable, metadata.IsRetryable);
        Assert.Equal(expectedStatusCode, metadata.HttpStatusCode);
        Assert.NotNull(metadata.RecoveryHint);
        Assert.NotEmpty(metadata.RecoveryHint);
    }

    // ============================================================================
    // API Version Compatibility Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(FiskalyErrorCode.E_EXPORT_NOT_COMPLETED)]
    [InlineData(FiskalyErrorCode.E_MIDDLEWARE_PENDING_REQUEST)]
    [InlineData(FiskalyErrorCode.E_LOG_NOT_FOUND)]
    [InlineData(FiskalyErrorCode.E_CSPL_NOT_FOUND)]
    public void Get_LegacyErrorCodes_FromV2133_StillSupported(FiskalyErrorCode errorCode)
    {
        // These error codes exist in API v2.1.33 but were removed in v2.1.34
        // SDK preserves them for backward compatibility
        Metadata metadata = ErrorCodeMetadata.Get(errorCode);

        Assert.NotNull(metadata);
        Assert.NotNull(metadata.RecoveryHint);
        Assert.NotEmpty(metadata.RecoveryHint);
    }
}
