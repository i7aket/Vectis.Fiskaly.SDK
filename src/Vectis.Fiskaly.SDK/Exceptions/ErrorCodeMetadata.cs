#nullable enable

using System.Net;

namespace Vectis.Fiskaly.SDK.Exceptions;

internal sealed record Metadata(
    FiskalyErrorCategory Category,
    bool IsRetryable,
    HttpStatusCode HttpStatusCode,
    string RecoveryHint);

internal static class ErrorCodeMetadata
{
    public static Metadata Get(FiskalyErrorCode errorCode) => errorCode switch
    {
        FiskalyErrorCode.E_TSS_NOT_FOUND => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.NotFound,
            "Verify TSS ID or create new TSS"),

        FiskalyErrorCode.E_TSS_CONFLICT => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Conflict,
            "Use different TSS ID or reuse existing TSS"),

        FiskalyErrorCode.E_TSS_DISABLED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Contact administrator to re-enable TSS"),

        FiskalyErrorCode.E_TSS_NOT_INITIALIZED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Initialize TSS with state=INITIALIZED before use"),

        FiskalyErrorCode.E_TSS_DEFECTIVE => new(
            FiskalyErrorCategory.Infrastructure,
            true,
            (HttpStatusCode)423, // 423 Locked
            "Contact Fiskaly support for TSS replacement"),

        FiskalyErrorCode.E_TSS_DELETED => new(
            FiskalyErrorCategory.Permanent,
            false,
            (HttpStatusCode)423, // 423 Locked
            "Create new TSS with different ID"),

        FiskalyErrorCode.E_TSS_EVICTED => new(
            FiskalyErrorCategory.Infrastructure,
            true,
            (HttpStatusCode)423, // 423 Locked
            "SDK retries automatically, TSS will be reloaded"),

        FiskalyErrorCode.E_TSS_LOCKED => new(
            FiskalyErrorCategory.Transient,
            true,
            HttpStatusCode.ServiceUnavailable,
            "SDK retries automatically with exponential backoff"),

        FiskalyErrorCode.E_TSS_LIMIT_REACHED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Forbidden,
            "Upgrade plan or delete unused TSS"),

        FiskalyErrorCode.E_TSS_LIMIT_PER_DAY_REACHED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Forbidden,
            "Wait until next day (UTC) or upgrade plan"),

        FiskalyErrorCode.E_ILLEGAL_TSS_STATE_CHANGE => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Check TSS state machine (UNINITIALIZED → INITIALIZED → DISABLED only)"),

        FiskalyErrorCode.E_TSS_ILLEGAL_STATE_TO_PERFORM_EXPORT => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Conflict,
            "Initialize TSS before attempting export"),

        FiskalyErrorCode.E_TX_NOT_FOUND => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.NotFound,
            "Verify transaction ID or create new transaction"),

        FiskalyErrorCode.E_TX_UPSERT => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Use POST for create, PUT for update"),

        FiskalyErrorCode.E_TX_NO_TYPE_DEFINED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Conflict,
            "Set transaction type before calling finish"),

        FiskalyErrorCode.E_TX_ILLEGAL_TYPE_CHANGE => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Conflict,
            "Create new transaction with correct type"),

        FiskalyErrorCode.E_TX_LIMIT_REACHED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Upgrade plan or archive old transactions"),

        FiskalyErrorCode.E_TX_REVISION_NOT_FOUND => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Verify revision number or use latest revision"),

        FiskalyErrorCode.E_PENDING_TX_CONFLICT => new(
            FiskalyErrorCategory.Transient,
            true,
            HttpStatusCode.Conflict,
            "SDK retries automatically, pending operation will complete"),

        FiskalyErrorCode.E_CLIENT_NOT_FOUND => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.NotFound,
            "Verify client ID or register new client"),

        FiskalyErrorCode.E_CLIENT_CONFLICT => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Conflict,
            "Use different client ID or reuse existing client"),

        FiskalyErrorCode.E_CLIENT_DEREGISTERED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Register new client with different ID"),

        FiskalyErrorCode.E_CLIENT_LIMIT_REACHED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Forbidden,
            "Upgrade plan or deregister unused clients"),

        FiskalyErrorCode.E_ILLEGAL_CLIENT_SERIAL => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Use valid alphanumeric serial number format"),

        FiskalyErrorCode.E_LATEST_TX_REVISION_NOT_FOUND => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.NotFound,
            "Create transaction first before querying revisions"),

        FiskalyErrorCode.E_EXPORT_NOT_FOUND => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.NotFound,
            "Verify export ID or create new export"),

        FiskalyErrorCode.E_EXPORT_NOT_COMPLETED => new(
            FiskalyErrorCategory.Transient,
            true,
            (HttpStatusCode)425, // 425 Too Early
            "SDK retries automatically, export will complete soon"),

        FiskalyErrorCode.E_EXPORT_FORBIDDEN => new(
            FiskalyErrorCategory.Permanent,
            false,
            (HttpStatusCode)423, // 423 Locked
            "Check TSS permissions or initialize TSS"),

        FiskalyErrorCode.E_EXPORT_IN_PROGRESS => new(
            FiskalyErrorCategory.Transient,
            true,
            HttpStatusCode.Conflict,
            "Wait for current export to complete - trigger exports during off-peak hours"),

        FiskalyErrorCode.E_EXPORT_TEMPORARILY_UNAVAILABLE => new(
            FiskalyErrorCategory.Transient,
            true,
            HttpStatusCode.ServiceUnavailable,
            "SDK retries automatically, service will recover"),

        FiskalyErrorCode.E_DUPLICATE_EXPORT => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Use existing export or change export parameters"),

        FiskalyErrorCode.E_EXPORT_DUPLICATE_RATE_LIMITED => new(
            FiskalyErrorCategory.Transient,
            true,
            HttpStatusCode.Conflict,
            "SDK retries after delay, reuse existing exports"),

        FiskalyErrorCode.E_TOO_MANY_EXPORTS => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Conflict,
            "Wait for existing exports to complete"),

        FiskalyErrorCode.E_CANCEL_EXPORT => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Check export state, only cancel PENDING/IN_PROGRESS exports"),

        FiskalyErrorCode.E_UNAUTHORIZED => new(
            FiskalyErrorCategory.Authentication,
            true,
            HttpStatusCode.Unauthorized,
            "SDK refreshes token automatically, verify credentials if still fails"),

        FiskalyErrorCode.E_ADMIN_LOGIN_FAILED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Unauthorized,
            "Verify admin username and password, reset if forgotten"),

        FiskalyErrorCode.E_ADMIN_PIN_BLOCKED => new(
            FiskalyErrorCategory.Permanent,
            false,
            (HttpStatusCode)423, // 423 Locked
            "Contact Fiskaly support, may require TSS replacement"),

        FiskalyErrorCode.E_CHANGE_ADMIN_PIN_FAILED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Verify current PIN is correct, contact support if blocked"),

        FiskalyErrorCode.E_ACCESS_DENIED => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.Forbidden,
            "Verify API key permissions, use correct environment (test/prod)"),

        FiskalyErrorCode.E_SMAERS_GATEWAY_CAPACITIES_DEPLETED => new(
            FiskalyErrorCategory.Infrastructure,
            true,
            HttpStatusCode.ServiceUnavailable,
            "SDK applies circuit breaker, wait for capacity recovery"),

        FiskalyErrorCode.E_BOOTSTRAP_FILE_NOT_AVAILABLE => new(
            FiskalyErrorCategory.Infrastructure,
            true,
            HttpStatusCode.ServiceUnavailable,
            "SDK retries automatically, contact support if persists"),

        FiskalyErrorCode.E_USE_MIDDLEWARE => new(
            FiskalyErrorCategory.Infrastructure,
            true,
            (HttpStatusCode)432, // 432 Custom Code
            "Update configuration to use middleware endpoint"),

        FiskalyErrorCode.E_MIDDLEWARE_PENDING_REQUEST => new(
            FiskalyErrorCategory.Transient,
            true,
            HttpStatusCode.Conflict,
            "SDK retries automatically, pending request will complete"),

        FiskalyErrorCode.E_AVAILABLE_ON_TEST_ONLY => new(
            FiskalyErrorCategory.Permanent,
            false,
            (HttpStatusCode)423, // 423 Locked
            "Feature only available in TEST environment - use TEST API key"),

        FiskalyErrorCode.E_CSPL_NOT_FOUND => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.NotFound,
            "Update SDK to latest version, check API compatibility"),

        FiskalyErrorCode.E_FAILED_SCHEMA_VALIDATION => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Fix request structure to match OpenAPI schema - check message field for details"),

        FiskalyErrorCode.E_LOG_NOT_FOUND => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.NotFound,
            "Verify log ID or adjust date range for log queries"),

        FiskalyErrorCode.E_PARAMETER_MISMATCH => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.BadRequest,
            "Validate request parameters, check API documentation"),

        _ => new(
            FiskalyErrorCategory.Permanent,
            false,
            HttpStatusCode.InternalServerError,
            "Unknown error code - update SDK or contact support")
    };
}
