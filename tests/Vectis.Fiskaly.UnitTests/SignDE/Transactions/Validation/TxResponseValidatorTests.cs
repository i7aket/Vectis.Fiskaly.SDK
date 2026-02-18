using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Validation;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Validation;

public class TxResponseValidatorTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_ValidFinishedTransaction_DoesNotThrow()
    {
        // Arrange
        TxResponse response = CreateValidFinishedTransaction();

        // Act & Assert
        Exception? exception = Record.Exception(() => TxResponseValidator.EnsureFinished(response));
        Assert.Null(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        TxResponse? response = null;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            TxResponseValidator.EnsureFinished(response!));
        Assert.Contains("response", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_ActiveState_ThrowsInvalidOperationException()
    {
        // Arrange
        TxResponse response = CreateTransactionWithState(TxState.Active);

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            TxResponseValidator.EnsureFinished(response));
        Assert.Contains("FINISHED state", exception.Message);
        Assert.Contains("Active", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_CancelledState_ThrowsInvalidOperationException()
    {
        // Arrange
        TxResponse response = CreateTransactionWithState(TxState.Cancelled);

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            TxResponseValidator.EnsureFinished(response));
        Assert.Contains("FINISHED state", exception.Message);
        Assert.Contains("Cancelled", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_MissingSignature_ThrowsComplianceException()
    {
        // Arrange
        TxResponse response = CreateTransactionWithSignature(null);

        // Act & Assert
        ComplianceException exception = Assert.Throws<ComplianceException>(() =>
            TxResponseValidator.EnsureFinished(response));
        Assert.Contains("Missing transaction signature", exception.Message);
        Assert.Contains("§146a AO", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_EmptySignatureValue_ThrowsComplianceException()
    {
        // Arrange
        TxSignature signature = new TxSignature
        {
            Value = "",
            Counter = 1,
            Algorithm = Algorithm.EcdsaPlainSha256
        };
        TxResponse response = CreateTransactionWithSignature(signature);

        // Act & Assert
        ComplianceException exception = Assert.Throws<ComplianceException>(() =>
            TxResponseValidator.EnsureFinished(response));
        Assert.Contains("signature value is empty", exception.Message);
        Assert.Contains("KassenSichV", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_WhitespaceSignatureValue_ThrowsComplianceException()
    {
        // Arrange
        TxSignature signature = new TxSignature
        {
            Value = "   ",
            Counter = 1,
            Algorithm = Algorithm.EcdsaPlainSha256
        };
        TxResponse response = CreateTransactionWithSignature(signature);

        // Act & Assert
        ComplianceException exception = Assert.Throws<ComplianceException>(() =>
            TxResponseValidator.EnsureFinished(response));
        Assert.Contains("signature value is empty", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_NullSignatureCounter_ThrowsComplianceException()
    {
        // Arrange
        TxSignature signature = new TxSignature
        {
            Value = "VGhpcyBpcyBhIHNhbXBsZSBzaWduYXR1cmU=",
            Counter = null,
            Algorithm = Algorithm.EcdsaPlainSha256
        };
        TxResponse response = CreateTransactionWithSignature(signature);

        // Act & Assert
        ComplianceException exception = Assert.Throws<ComplianceException>(() =>
            TxResponseValidator.EnsureFinished(response));
        Assert.Contains("Invalid signature counter", exception.Message);
        Assert.Contains("audit trail compliance", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_ZeroSignatureCounter_ThrowsComplianceException()
    {
        // Arrange
        TxSignature signature = new TxSignature
        {
            Value = "VGhpcyBpcyBhIHNhbXBsZSBzaWduYXR1cmU=",
            Counter = 0,
            Algorithm = Algorithm.EcdsaPlainSha256
        };
        TxResponse response = CreateTransactionWithSignature(signature);

        // Act & Assert
        ComplianceException exception = Assert.Throws<ComplianceException>(() =>
            TxResponseValidator.EnsureFinished(response));
        Assert.Contains("Invalid signature counter: 0", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureFinished_NegativeSignatureCounter_ThrowsComplianceException()
    {
        // Arrange
        TxSignature signature = new TxSignature
        {
            Value = "VGhpcyBpcyBhIHNhbXBsZSBzaWduYXR1cmU=",
            Counter = -5,
            Algorithm = Algorithm.EcdsaPlainSha256
        };
        TxResponse response = CreateTransactionWithSignature(signature);

        // Act & Assert
        ComplianceException exception = Assert.Throws<ComplianceException>(() =>
            TxResponseValidator.EnsureFinished(response));
        Assert.Contains("Invalid signature counter: -5", exception.Message);
    }

    // NOTE: Test removed - Algorithm is now a required enum type, cannot be empty/whitespace
    // [Trait("Category", "Unit")]
    // [Fact]
    // public void EnsureFinished_EmptySignatureAlgorithm_ThrowsComplianceException()
    // {
    //     // Arrange
    //     var signature = new TxSignature
    //     {
    //         Value = "VGhpcyBpcyBhIHNhbXBsZSBzaWduYXR1cmU=",
    //         Counter = 1,
    //         Algorithm = Algorithm.EcdsaPlainSha256 // Algorithm is now required enum
    //     };
    //     var response = CreateTransactionWithSignature(signature);
    //
    //     // Act & Assert
    //     var exception = Assert.Throws<ComplianceException>(() =>
    //         TxResponseValidator.EnsureFinished(response));
    //     Assert.Contains("Missing signature algorithm", exception.Message);
    //     Assert.Contains("signature verification", exception.Message);
    // }

    // NOTE: Test removed - Algorithm is now a required enum type, cannot be empty/whitespace
    // [Trait("Category", "Unit")]
    // [Fact]
    // public void EnsureFinished_WhitespaceSignatureAlgorithm_ThrowsComplianceException()
    // {
    //     // Arrange
    //     var signature = new TxSignature
    //     {
    //         Value = "VGhpcyBpcyBhIHNhbXBsZSBzaWduYXR1cmU=",
    //         Counter = 1,
    //         Algorithm = Algorithm.EcdsaPlainSha256 // Algorithm is now required enum
    //     };
    //     var response = CreateTransactionWithSignature(signature);
    //
    //     // Act & Assert
    //     var exception = Assert.Throws<ComplianceException>(() =>
    //         TxResponseValidator.EnsureFinished(response));
    //     Assert.Contains("Missing signature algorithm", exception.Message);
    // }

    [Trait("Category", "Unit")]
    [Fact]
    public void ComplianceException_Constructor_CreatesExceptionWithMessage()
    {
        // Arrange
        const string message = "Test compliance violation";

        // Act
        ComplianceException exception = new ComplianceException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ComplianceException_ConstructorWithInner_CreatesExceptionWithMessageAndInner()
    {
        // Arrange
        const string message = "Test compliance violation";
        InvalidOperationException innerException = new InvalidOperationException("Inner error");

        // Act
        ComplianceException exception = new ComplianceException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    #region Test Helpers

    /// <summary>
    /// Creates a valid finished transaction for testing.
    /// </summary>
    private static TxResponse CreateValidFinishedTransaction()
    {
        return new TxResponse
        {
            Id = TxId.From("a1b2c3d4-1234-4abc-9def-123456789012"),
            State = TxState.Finished,
            ClientId = ClientId.From("b2c3d4e5-2345-4bcd-9ef0-234567890012"),
            Number = 123,
            Signature = new TxSignature
            {
                Value = "VGhpcyBpcyBhIHNhbXBsZSBzaWduYXR1cmU=",
                Counter = 123,
                Algorithm = Algorithm.EcdsaPlainSha256,
                PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE..."
            },
            TssSerialNumber = TssSerialNumber.From("fiskaly-12345678"),
            Log = new TxLog
            {
                Operation = TxOperation.Finish,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(1704276000),
                TimestampFormat = TimestampFormat.UnixTime
            },
            QrCodeData = "https://verify.fiskaly.com/tx/abc123",
            TimeStart = DateTimeOffset.FromUnixTimeSeconds(1704276000),
            TimeEnd = DateTimeOffset.FromUnixTimeSeconds(1704276030),
            Env = Env.Test,
            Type = ResourceType.Transaction,
            Version = "2.1.35",
            ClientSerialNumber = ClientSerialNumber.From("POS-001"),
            LatestRevision = 2,
            Revision = 2,
            TssId = TssId.From("c3d4e5f6-3456-4cde-9ef0-345678900012")
        };
    }

    /// <summary>
    /// Creates a transaction with a specific state.
    /// </summary>
    private static TxResponse CreateTransactionWithState(TxState state)
    {
        return new TxResponse
        {
            Id = TxId.From("a1b2c3d4-1234-4abc-9def-123456789012"),
            State = state,
            ClientId = ClientId.From("b2c3d4e5-2345-4bcd-9ef0-234567890012"),
            Number = 123,
            Signature = new TxSignature
            {
                Value = "VGhpcyBpcyBhIHNhbXBsZSBzaWduYXR1cmU=",
                Counter = 123,
                Algorithm = Algorithm.EcdsaPlainSha256,
                PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE..."
            },
            TssSerialNumber = TssSerialNumber.From("fiskaly-12345678"),
            Log = new TxLog
            {
                Operation = TxOperation.Finish,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(1704276000),
                TimestampFormat = TimestampFormat.UnixTime
            },
            QrCodeData = "https://verify.fiskaly.com/tx/abc123",
            TimeStart = DateTimeOffset.FromUnixTimeSeconds(1704276000),
            TimeEnd = DateTimeOffset.FromUnixTimeSeconds(1704276030),
            Env = Env.Test,
            Type = ResourceType.Transaction,
            Version = "2.1.35",
            ClientSerialNumber = ClientSerialNumber.From("POS-001"),
            LatestRevision = 2,
            Revision = 2,
            TssId = TssId.From("c3d4e5f6-3456-4cde-9ef0-345678900012")
        };
    }

    /// <summary>
    /// Creates a transaction with a specific signature.
    /// </summary>
    private static TxResponse CreateTransactionWithSignature(TxSignature? signature)
    {
        return new TxResponse
        {
            Id = TxId.From("a1b2c3d4-1234-4abc-9def-123456789012"),
            State = TxState.Finished,
            ClientId = ClientId.From("b2c3d4e5-2345-4bcd-9ef0-234567890012"),
            Number = 123,
            Signature = signature,
            TssSerialNumber = TssSerialNumber.From("fiskaly-12345678"),
            Log = new TxLog
            {
                Operation = TxOperation.Finish,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(1704276000),
                TimestampFormat = TimestampFormat.UnixTime
            },
            QrCodeData = "https://verify.fiskaly.com/tx/abc123",
            TimeStart = DateTimeOffset.FromUnixTimeSeconds(1704276000),
            TimeEnd = DateTimeOffset.FromUnixTimeSeconds(1704276030),
            Env = Env.Test,
            Type = ResourceType.Transaction,
            Version = "2.1.35",
            ClientSerialNumber = ClientSerialNumber.From("POS-001"),
            LatestRevision = 2,
            Revision = 2,
            TssId = TssId.From("c3d4e5f6-3456-4cde-9ef0-345678900012")
        };
    }

    #endregion
}
