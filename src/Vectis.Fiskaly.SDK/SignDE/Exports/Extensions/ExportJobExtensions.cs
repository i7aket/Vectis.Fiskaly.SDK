using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Extensions;

public static class ExportJobExtensions
{
    public static bool IsCompleted(this ExportJob export)
        => export.State == ExportState.Completed;

    public static bool IsFailed(this ExportJob export)
        => export.State == ExportState.Error;

    public static bool IsPending(this ExportJob export)
        => export.State == ExportState.Pending || export.State == ExportState.Working;

    public static bool IsCancelled(this ExportJob export)
        => export.State == ExportState.Cancelled;

    public static void ThrowIfFailed(this ExportJob export)
    {
        if (export.State != ExportState.Error)
            return;

        ExportExceptionCode exceptionCode = export.ExceptionCode ?? ExportExceptionCode.Unexpected;
        ExportExceptionInfo metadata = ExportExceptionMetadata.Get(exceptionCode);

        throw new InvalidOperationException(
            $"Export {export.Id.Value} failed with {exceptionCode}: {metadata.RecoveryHint}");
    }

    public static ExportExceptionInfo? GetExceptionMetadata(this ExportJob export)
    {
        if (export.State != ExportState.Error || export.ExceptionCode == null)
            return null;

        return ExportExceptionMetadata.Get(export.ExceptionCode.Value);
    }
}
