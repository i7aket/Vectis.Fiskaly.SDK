using System.Net;
using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using static Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects.MoneyAmount;

namespace Vectis.Fiskaly.IntegrationTests.SignDE;

/// <summary>
/// Integration tests for transaction state transition error scenarios.
/// </summary>
/// <remarks>
/// <para><strong>Scope</strong>: Invalid transaction state transitions and operations</para>
///
/// <para><strong>Error Scenarios Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>Starting transaction with unregistered client</description></item>
///   <item><description>Double-finish (finishing already finished transaction)</description></item>
///   <item><description>Update after finish (SDK client-side validation)</description></item>
///   <item><description>Double-cancel (SDK client-side validation)</description></item>
///   <item><description>Finish after cancel (invalid state transition)</description></item>
/// </list>
///
/// <para><strong>Error Types</strong>:</para>
/// <list type="bullet">
///   <item><description>FiskalyApiException - API-level errors (400 BadRequest, 409 Conflict)</description></item>
///   <item><description>InvalidOperationException - SDK client-side validation</description></item>
/// </list>
///
/// <para><strong>Fiscal Compliance</strong>:</para>
/// <para>Once a transaction is FINISHED or CANCELLED, it becomes immutable (fiscally sealed).
/// These tests verify that the SDK and API enforce fiscal integrity rules.</para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "ErrorScenarios")]
[Trait("Priority", "High")]
public class TransactionStateErrorTests : FiskalyIntegrationTestBase
{
    public TransactionStateErrorTests(FiskalyBaseTestFixture fixture) : base(fixture) { }

    /// <summary>
    /// Helper method to create EUR MoneyAmount.
    /// </summary>
    private static MoneyAmount Eur(decimal value)
        => Create(value, CurrencyCode.EUR).EnsureNonNegative();

    [Fact]
    public async Task StartTransaction_WithUnregisteredClient_ThrowsE_CLIENT_NOT_FOUND()
    {
        // Arrange
        ClientId nonExistentClientId = ClientId.New();
        TxId txId = TxId.New();

        StartTransactionRequest startRequest = new StartTransactionRequest
        {
            ClientId = nonExistentClientId
        };

        Console.WriteLine($"Testing transaction start with unregistered client: {nonExistentClientId}");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.TransactionClient.StartTransactionAsync(TssId, txId, startRequest)
        );

        // Assert exception details
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_CLIENT_NOT_FOUND);
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("E_CLIENT_NOT_FOUND is a permanent error");
        exception.CorrelationId.Should().NotBeNullOrEmpty("CorrelationId should be provided for tracing");

        Console.WriteLine($"✅ E_CLIENT_NOT_FOUND (via transaction start) verified");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Status Code: {exception.StatusCode}");
        Console.WriteLine($"   Correlation ID: {exception.CorrelationId}");
    }

    [Fact]
    public async Task FinishTransaction_AlreadyFinished_ThrowsBadRequest()
    {
        // Arrange - Create and finish a transaction
        TxId txId = TxId.New();

        Console.WriteLine($"Creating and finishing transaction to test double-finish error...");
        Console.WriteLine($"   Transaction ID: {txId}");

        // Start transaction
        StartTransactionRequest startRequest = new StartTransactionRequest
        {
            ClientId = ClientId
        };
        TxResponse startedTx = await Fixture.TransactionClient.StartTransactionAsync(TssId, txId, startRequest);
        Console.WriteLine($"✅ Transaction started: {startedTx.Number}");

        // Update transaction with receipt type (required before finish)
        UpdateTransactionRequest updateRequest = UpdateTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = [],  // Empty list initially
                AmountsPerPaymentType = []  // Empty list initially
            });
        await Fixture.TransactionClient.UpdateTransactionAsync(TssId, txId, updateRequest);
        Console.WriteLine($"✅ Transaction updated with receipt type");

        // Finish transaction (first time - should succeed)
        FinishTransactionRequest finishRequest = FinishTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = Eur(10.00m) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = Eur(10.00m) }
                }
            });
        TxResponse finishedTx = await Fixture.TransactionClient.FinishTransactionAsync(TssId, txId, finishRequest);
        Console.WriteLine($"✅ Transaction finished: {finishedTx.Number}");

        // Act & Assert - Try to finish the same transaction again
        Console.WriteLine($"Attempting to finish already-finished transaction...");
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.TransactionClient.FinishTransactionAsync(TssId, txId, finishRequest)
        );

        // Debug output to see actual error details
        Console.WriteLine($"📋 Actual API Error:");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Status Code: {exception.StatusCode} ({(int)exception.StatusCode})");
        Console.WriteLine($"   Category: {exception.Category}");
        Console.WriteLine($"   Message: {exception.Message}");

        // Assert exception details - API returns BadRequest for invalid operations on finished transactions
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "attempting to modify finished transaction returns BadRequest (invalid operation)");
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("transaction state conflicts are permanent errors");
        exception.CorrelationId.Should().NotBeNullOrEmpty("CorrelationId should be provided for tracing");

        Console.WriteLine($"✅ Double-finish error verified");
    }

    [Fact]
    public async Task UpdateTransaction_AlreadyFinished_ThrowsInvalidOperationException()
    {
        // Arrange - Create and finish a transaction
        TxId txId = TxId.New();

        Console.WriteLine($"Creating and finishing transaction to test update-after-finish error...");
        Console.WriteLine($"   Transaction ID: {txId}");

        // Start transaction
        StartTransactionRequest startRequest = new StartTransactionRequest
        {
            ClientId = ClientId
        };
        await Fixture.TransactionClient.StartTransactionAsync(TssId, txId, startRequest);
        Console.WriteLine($"✅ Transaction started");

        // Update transaction with receipt type (required before finish)
        UpdateTransactionRequest updateRequest1 = UpdateTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = [],  // Empty list initially
                AmountsPerPaymentType = []  // Empty list initially
            });
        await Fixture.TransactionClient.UpdateTransactionAsync(TssId, txId, updateRequest1);
        Console.WriteLine($"✅ Transaction updated with receipt type");

        // Finish transaction
        FinishTransactionRequest finishRequest = FinishTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = Eur(10.00m) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = Eur(10.00m) }
                }
            });
        await Fixture.TransactionClient.FinishTransactionAsync(TssId, txId, finishRequest);
        Console.WriteLine($"✅ Transaction finished");

        // Act & Assert - Try to update the finished transaction (SDK validates client-side)
        Console.WriteLine($"Attempting to update finished transaction...");
        UpdateTransactionRequest updateRequest = UpdateTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = [],
                AmountsPerPaymentType = []
            });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Fixture.TransactionClient.UpdateTransactionAsync(TssId, txId, updateRequest)
        );

        // Assert exception message
        exception.Message.Should().Contain("FINISHED",
            "SDK should validate transaction state and prevent updates to finished transactions");
        exception.Message.Should().Contain("ACTIVE",
            "SDK should provide clear validation message about state requirement");

        Console.WriteLine($"✅ SDK client-side validation verified");
        Console.WriteLine($"   Exception Type: InvalidOperationException");
        Console.WriteLine($"   Message: {exception.Message}");
    }

    [Fact]
    public async Task CancelTransaction_AlreadyCancelled_ThrowsInvalidOperationException()
    {
        // Arrange - Create and cancel a transaction
        TxId txId = TxId.New();

        Console.WriteLine($"Creating and cancelling transaction to test double-cancel error...");
        Console.WriteLine($"   Transaction ID: {txId}");

        // Start transaction
        StartTransactionRequest startRequest = new StartTransactionRequest
        {
            ClientId = ClientId
        };
        await Fixture.TransactionClient.StartTransactionAsync(TssId, txId, startRequest);
        Console.WriteLine($"✅ Transaction started");

        // Cancel transaction (first time - should succeed)
        CancelTransactionRequest cancelRequest = new CancelTransactionRequest
        {
            ClientId = ClientId,
            Schema = TransactionSchema.ForReceipt(new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>()
            })
        };
        await Fixture.TransactionClient.CancelTransactionAsync(TssId, txId, cancelRequest);
        Console.WriteLine($"✅ Transaction cancelled");

        // Act & Assert - Try to cancel the same transaction again (SDK validates client-side)
        Console.WriteLine($"Attempting to cancel already-cancelled transaction...");
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Fixture.TransactionClient.CancelTransactionAsync(TssId, txId, cancelRequest)
        );

        // Assert exception message
        exception.Message.Should().Contain("CANCELLED",
            "SDK should validate transaction state and prevent double-cancel");
        exception.Message.Should().Contain("ACTIVE",
            "SDK should provide clear validation message about state requirement");

        Console.WriteLine($"✅ SDK client-side validation verified");
        Console.WriteLine($"   Exception Type: InvalidOperationException");
        Console.WriteLine($"   Message: {exception.Message}");
    }

    [Fact]
    public async Task FinishTransaction_AfterCancelled_ThrowsE_TX_UPSERT()
    {
        // Arrange - Create and cancel a transaction
        TxId txId = TxId.New();

        Console.WriteLine($"Creating and cancelling transaction to test finish-after-cancel error...");
        Console.WriteLine($"   Transaction ID: {txId}");

        // Start transaction
        StartTransactionRequest startRequest = new StartTransactionRequest
        {
            ClientId = ClientId
        };
        await Fixture.TransactionClient.StartTransactionAsync(TssId, txId, startRequest);
        Console.WriteLine($"✅ Transaction started");

        // Cancel transaction
        CancelTransactionRequest cancelRequest = new CancelTransactionRequest
        {
            ClientId = ClientId,
            Schema = TransactionSchema.ForReceipt(new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>()
            })
        };
        await Fixture.TransactionClient.CancelTransactionAsync(TssId, txId, cancelRequest);
        Console.WriteLine($"✅ Transaction cancelled");

        // Act & Assert - Try to finish the cancelled transaction (SDK validates client-side)
        Console.WriteLine($"Attempting to finish cancelled transaction...");
        FinishTransactionRequest finishRequest = FinishTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = Eur(10.00m) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = Eur(10.00m) }
                }
            });

        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.TransactionClient.FinishTransactionAsync(TssId, txId, finishRequest)
        );

        // Assert exception details
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_TX_UPSERT,
            "API rejects finish on cancelled transaction with E_TX_UPSERT");
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("transaction state errors are permanent");
        exception.Message.Should().Contain("CANCELLED",
            "Error message should explain cancelled transactions cannot be finished");

        Console.WriteLine($"✅ Finish-after-cancel error verified");
        Console.WriteLine($"   Exception Type: FiskalyApiException");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Message: {exception.Message}");
    }

    [Fact]
    public async Task CancelTransaction_AlreadyFinished_ThrowsInvalidOperationException()
    {
        // Arrange - Create and finish a transaction
        TxId txId = TxId.New();

        Console.WriteLine($"Creating and finishing transaction to test cancel-after-finish error...");
        Console.WriteLine($"   Transaction ID: {txId}");

        // Start transaction
        StartTransactionRequest startRequest = new StartTransactionRequest
        {
            ClientId = ClientId
        };
        await Fixture.TransactionClient.StartTransactionAsync(TssId, txId, startRequest);
        Console.WriteLine($"✅ Transaction started");

        // Update transaction with receipt type (required before finish)
        UpdateTransactionRequest updateRequest = UpdateTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = [],
                AmountsPerPaymentType = []
            });
        await Fixture.TransactionClient.UpdateTransactionAsync(TssId, txId, updateRequest);
        Console.WriteLine($"✅ Transaction updated with receipt type");

        // Finish transaction
        FinishTransactionRequest finishRequest = FinishTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = Eur(10.00m) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = Eur(10.00m) }
                }
            });
        await Fixture.TransactionClient.FinishTransactionAsync(TssId, txId, finishRequest);
        Console.WriteLine($"✅ Transaction finished");

        // Act & Assert - Try to cancel the finished transaction (SDK validates client-side)
        Console.WriteLine($"Attempting to cancel finished transaction...");
        CancelTransactionRequest cancelRequest = new CancelTransactionRequest
        {
            ClientId = ClientId,
            Schema = TransactionSchema.ForReceipt(new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>()
            })
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Fixture.TransactionClient.CancelTransactionAsync(TssId, txId, cancelRequest)
        );

        // Assert exception message
        exception.Message.Should().Contain("FINISHED",
            "SDK should validate transaction state and prevent cancel on finished transactions");
        exception.Message.Should().Contain("ACTIVE",
            "SDK should provide clear validation message about state requirement");

        Console.WriteLine($"✅ SDK client-side validation verified");
        Console.WriteLine($"   Exception Type: InvalidOperationException");
        Console.WriteLine($"   Message: {exception.Message}");
    }

    [Fact]
    public async Task UpdateTransaction_WithWrongRevision_ThrowsBadRequest()
    {
        // Arrange - Start a transaction
        TxId txId = TxId.New();

        Console.WriteLine($"Creating transaction to test wrong-revision error...");
        Console.WriteLine($"   Transaction ID: {txId}");

        // Start transaction
        StartTransactionRequest startRequest = new StartTransactionRequest
        {
            ClientId = ClientId
        };
        TxResponse startedTx = await Fixture.TransactionClient.StartTransactionAsync(TssId, txId, startRequest);
        Console.WriteLine($"✅ Transaction started (revision {startedTx.Revision})");

        // Act & Assert - Try to update with wrong revision number (999 instead of 1)
        Console.WriteLine($"Attempting to update with wrong revision (999 instead of 1)...");

        UpdateTransactionRequest updateRequest = UpdateTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = [],
                AmountsPerPaymentType = []
            });

        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.TransactionClient.UpdateTransactionAsync(
                TssId,
                txId,
                updateRequest,
                txRevision: 999) // Wrong revision - should be 1
        );

        // Debug output to see actual error details
        Console.WriteLine($"📋 Actual API Error:");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Status Code: {exception.StatusCode} ({(int)exception.StatusCode})");
        Console.WriteLine($"   Category: {exception.Category}");
        Console.WriteLine($"   Message: {exception.Message}");

        // Assert exception details - API returns BadRequest for wrong revision
        exception.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "wrong revision number should return 400 BadRequest with E_TX_UPSERT error code");
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_TX_UPSERT,
            "Fiskaly API returns E_TX_UPSERT when revision is incorrect");
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("wrong revision is a permanent error - client must fetch current revision");
        exception.CorrelationId.Should().NotBeNullOrEmpty("CorrelationId should be provided for tracing");

        Console.WriteLine($"✅ Wrong revision error verified");
        Console.WriteLine($"   Expected revision: 1 (or 2)");
        Console.WriteLine($"   Provided revision: 999");
        Console.WriteLine($"   Result: 400 BadRequest (E_TX_UPSERT)");
    }
}
