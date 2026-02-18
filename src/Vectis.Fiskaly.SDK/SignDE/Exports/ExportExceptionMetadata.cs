using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Exports;

internal static class ExportExceptionMetadata
{
    public static ExportExceptionInfo Get(ExportExceptionCode exceptionCode) => exceptionCode switch
    {
        ExportExceptionCode.AlreadyProcessing => new(
            FiskalyErrorCategory.Transient,
            true,
            "Wait for current export to complete - retry after 30-60 seconds. Trigger exports during off-peak hours to avoid conflicts."),

        ExportExceptionCode.BadRequest => new(
            FiskalyErrorCategory.Permanent,
            false,
            "Fix export parameters (date range, transaction IDs, etc.) before creating new export."),

        ExportExceptionCode.ExportProcessingTimeout => new(
            FiskalyErrorCategory.Transient,
            true,
            "Export exceeded 24-hour timeout - reduce date range or split into smaller exports, then retry."),

        ExportExceptionCode.IdNotFound => new(
            FiskalyErrorCategory.Permanent,
            false,
            "Verify referenced IDs (transaction, client, etc.) exist in TSS before creating export."),

        ExportExceptionCode.Internal => new(
            FiskalyErrorCategory.Infrastructure,
            true,
            "Internal server error - retry after 60-120 seconds with exponential backoff. Contact support if persists."),

        ExportExceptionCode.LogsNotDeleted => new(
            FiskalyErrorCategory.Permanent,
            false,
            "Export succeeded but log deletion failed - export file is available for download. Manually delete logs if needed."),

        ExportExceptionCode.NoDataAvailable => new(
            FiskalyErrorCategory.Permanent,
            false,
            "No data found for specified export parameters - adjust date range, client filter, or verify TSS has data."),

        ExportExceptionCode.TooManyRecords => new(
            FiskalyErrorCategory.Permanent,
            false,
            "Export would exceed 1,000,000 signature limit - reduce date range or split into multiple exports."),

        ExportExceptionCode.TransactionIdNotFound => new(
            FiskalyErrorCategory.Permanent,
            false,
            "Specified start_transaction_number not found - verify transaction number exists and format is correct."),

        ExportExceptionCode.Unexpected => new(
            FiskalyErrorCategory.Infrastructure,
            true,
            "Unexpected error during export processing - retry after 60-120 seconds. Contact support if persists."),

        _ => throw new ArgumentOutOfRangeException(nameof(exceptionCode), exceptionCode, "Unknown export exception code")
    };

    public static FiskalyErrorCategory GetCategory(ExportExceptionCode exceptionCode)
        => Get(exceptionCode).Category;

    public static bool IsRetryable(ExportExceptionCode exceptionCode)
        => Get(exceptionCode).IsRetryable;

    public static string GetRecoveryHint(ExportExceptionCode exceptionCode)
        => Get(exceptionCode).RecoveryHint;
}
