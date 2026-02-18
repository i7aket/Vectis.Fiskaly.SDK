using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Validation;

public static class TxResponseValidator
{
    public static void EnsureFinished(TxResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.State != TxState.Finished)
        {
            throw new InvalidOperationException(
                $"Transaction must be in FINISHED state. Current state: {response.State}");
        }

        TxSignature? signature = response.Signature;
        if (signature == null)
        {
            throw new ComplianceException(
                "Missing transaction signature for finished transaction. " +
                "This violates §146a AO (German tax code). " +
                "Transactions must be cryptographically signed before receipt printing.");
        }

        if (string.IsNullOrWhiteSpace(signature.Value))
        {
            throw new ComplianceException(
                "Transaction signature value is empty. " +
                "A valid cryptographic signature is required for compliance with KassenSichV.");
        }

        if (signature.Counter is null || signature.Counter <= 0)
        {
            throw new ComplianceException(
                $"Invalid signature counter: {signature.Counter?.ToString() ?? "null"}. " +
                "Counter must be a positive integer for audit trail compliance.");
        }

        if (signature.Algorithm is null)
        {
            throw new ComplianceException(
                "Signature algorithm is missing. " +
                "Transactions must include algorithm information for verification.");
        }
    }
}

public class ComplianceException : Exception
{
    public ComplianceException(string message) : base(message)
    {
    }

    public ComplianceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
