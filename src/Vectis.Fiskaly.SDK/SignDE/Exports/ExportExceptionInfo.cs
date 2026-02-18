using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.SDK.SignDE.Exports;

public record ExportExceptionInfo(
    FiskalyErrorCategory Category,
    bool IsRetryable,
    string RecoveryHint);
