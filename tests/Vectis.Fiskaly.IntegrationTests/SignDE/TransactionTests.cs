using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Xunit.Abstractions;

using static Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects.MoneyAmount;

namespace Vectis.Fiskaly.IntegrationTests.SignDE;

/// <summary>
/// Integration tests for transaction operations (create, update, finish, cancel, list, metadata).
/// </summary>
/// <remarks>
/// <para><strong>Coverage</strong>:</para>
/// <list type="bullet">
///   <item><description>Transaction lifecycle: Start → Update → Finish</description></item>
///   <item><description>Receipt transactions (Kassenbeleg-V1 with aggregated amounts)</description></item>
///   <item><description>Order transactions (Bestellung-V1 with line items)</description></item>
///   <item><description>Transaction cancellation</description></item>
///   <item><description>Transaction listing (TSS, Client, All)</description></item>
///   <item><description>Transaction metadata operations</description></item>
/// </list>
///
/// <para><strong>Test Architecture</strong>:</para>
/// Inherits from <see cref="FiskalyIntegrationTestBase"/> which provides:
/// <list type="bullet">
///   <item><description>Automatic TSS creation and cleanup</description></item>
///   <item><description>Automatic Client registration and cleanup</description></item>
///   <item><description>TSS limit retry logic (handles 5 TSS max limit)</description></item>
/// </list>
///
/// <para><strong>Related Tests</strong>:</para>
/// <list type="bullet">
///   <item><description><see cref="StornoTransactionTests"/> - Reversal transactions (Storno)</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "Transactions")]
[Trait("Priority", "Critical")]
public class TransactionTests(
    FiskalyBaseTestFixture fixture,
    ITestOutputHelper output)
    : FiskalyIntegrationTestBase(fixture)
{
    /// <summary>
    /// Helper method to create EUR MoneyAmount.
    /// </summary>
    private static MoneyAmount Eur(decimal value)
        => Create(value, CurrencyCode.EUR);

    [Fact]
    public async Task StartTransaction_WithValidClient_ShouldCreateActiveTransaction()
    {
        // Arrange
        TxId transactionId = TxId.New();
        output.WriteLine($"Using TSS: {TssId}");
        output.WriteLine($"Using Client: {ClientId}");
        output.WriteLine($"Starting transaction: {transactionId}");

        // Act
        TxResponse response = await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest
            {
                ClientId = ClientId
            });

        // Assert
        response.Should().NotBeNull();
        response.State.Should().Be(TxState.Active);
        response.ClientId.Should().Be(ClientId);
        response.TssId.Should().Be(TssId);
        response.Id.Should().Be(transactionId);
        response.Number.Should().BeGreaterThan(0);
        response.Revision.Should().Be(1, "Initial transaction revision should be 1");

        output.WriteLine($"✅ Transaction started successfully!");
        output.WriteLine($"   Transaction Number: {response.Number}");
        output.WriteLine($"   State: {response.State}");
        output.WriteLine($"   Revision: {response.Revision}");
    }

    [Fact]
    public async Task GetTransaction_AfterStart_ShouldReturnActiveState()
    {
        // Arrange - Start a transaction
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        // Act - Get transaction details
        TxResponse response = await Fixture.TransactionClient.GetTransactionAsync(TssId, transactionId);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be(transactionId);
        response.State.Should().Be(TxState.Active, "Transaction should be in ACTIVE state after START");
        response.Revision.Should().Be(1);
        response.ClientId.Should().Be(ClientId);
        response.TssId.Should().Be(TssId);

        output.WriteLine($"✅ Transaction retrieved successfully!");
        output.WriteLine($"   State: {response.State}");
        output.WriteLine($"   Revision: {response.Revision}");
    }

    [Fact]
    public async Task UpdateTransaction_WithReceipt_ShouldIncrementRevision()
    {
        // Arrange - Start a transaction
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        output.WriteLine($"Transaction {transactionId} started (revision 1)");

        // Act - Update with receipt data
        TxResponse updateResponse = await Fixture.TransactionClient.UpdateTransactionAsync(
            TssId,
            transactionId,
            UpdateTransactionRequest.CreateReceipt(
                ClientId,
                new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,
                    AmountsPerVatRate = new List<VatRateAmount>
                    {
                        new() { VatRate = VatRate.Normal, Amount = Eur(25.99m) }
                    },
                    AmountsPerPaymentType = new List<PaymentTypeAmount>
                    {
                        new() { PaymentType = PaymentType.Cash, Amount = Eur(25.99m) }
                    }
                }),
            txRevision: null); // Auto-fetch current revision

        // Assert
        updateResponse.Should().NotBeNull();
        updateResponse.Revision.Should().Be(2, "Revision should increment after UPDATE");
        updateResponse.State.Should().Be(TxState.Active, "Transaction should still be ACTIVE after UPDATE");

        output.WriteLine($"✅ Transaction updated successfully!");
        output.WriteLine($"   Revision: {updateResponse.Revision}");
        output.WriteLine($"   State: {updateResponse.State}");
    }

    [Fact]
    public async Task UpdateTransaction_WithOrder_ShouldIncrementRevision()
    {
        // Arrange - Start a transaction
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        output.WriteLine($"Order transaction {transactionId} started (revision 1)");

        // Act - Update with order data (line items)
        TxResponse updateResponse = await Fixture.TransactionClient.UpdateTransactionAsync(
            TssId,
            transactionId,
            UpdateTransactionRequest.CreateOrder(
                ClientId,
                new Order
                {
                    LineItems = new List<LineItem>
                    {
                        new() { Quantity = 2m, Text = "Pizza Margherita", PricePerUnit = Eur(8.90m) },
                        new() { Quantity = 1m, Text = "Cola 0.5L", PricePerUnit = Eur(2.50m) }
                    }
                }),
            txRevision: null); // Auto-fetch current revision

        // Assert
        updateResponse.Should().NotBeNull();
        updateResponse.Revision.Should().Be(2, "Revision should increment after UPDATE");
        updateResponse.State.Should().Be(TxState.Active, "Transaction should still be ACTIVE after UPDATE");

        output.WriteLine($"✅ Order transaction updated successfully!");
        output.WriteLine($"   Revision: {updateResponse.Revision}");
        output.WriteLine($"   State: {updateResponse.State}");
        output.WriteLine($"   Line Items: {updateResponse.Schema?.StandardV1?.Order?.LineItems?.Count ?? 0}");
    }

    [Fact]
    public async Task FinishTransaction_WithReceipt_ShouldGenerateSignature()
    {
        // Arrange - Start a transaction
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        // Act - Finish transaction with receipt data
        TxResponse finishResponse = await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            transactionId,
            FinishTransactionRequest.CreateReceipt(
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
                }));

        // Assert
        finishResponse.Should().NotBeNull();
        finishResponse.State.Should().Be(TxState.Finished);
        finishResponse.QrCodeData.Should().NotBeNullOrEmpty("QR code is required for KassenSichV compliance");
        finishResponse.Signature.Should().NotBeNull("Transaction must be cryptographically signed");
        finishResponse.Signature!.Value.Should().NotBeNullOrEmpty();
        finishResponse.Signature.Counter.Should().BeGreaterThan(0);

        output.WriteLine($"✅ Transaction finished successfully!");
        output.WriteLine($"   Transaction ID: {finishResponse.Id}");
        output.WriteLine($"   State: {finishResponse.State}");
        output.WriteLine($"   Signature Counter: {finishResponse.Signature.Counter}");
        output.WriteLine($"   QR Code: {finishResponse.QrCodeData![..50]}...");
    }

    [Fact]
    public async Task FinishOrderTransaction_WithLineItems_ShouldSucceed()
    {
        // Arrange - Start a transaction
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        output.WriteLine($"Order transaction {transactionId} started");

        // Act - Finish with Order data (type-safe DTO)
        TxResponse finishResponse = await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            transactionId,
            FinishTransactionRequest.CreateOrder(
                ClientId,
                new Order
                {
                    LineItems = new List<LineItem>
                    {
                        new() { Quantity = 2m, Text = "Pizza Margherita", PricePerUnit = Eur(8.90m) },
                        new() { Quantity = 1m, Text = "Cola 0.5L", PricePerUnit = Eur(2.50m) }
                    }
                }),
            txRevision: null);

        // Assert
        finishResponse.Should().NotBeNull();
        finishResponse.State.Should().Be(TxState.Finished);
        finishResponse.Signature.Should().NotBeNull("Order transaction must be signed");
        finishResponse.QrCodeData.Should().NotBeNullOrEmpty();

        output.WriteLine($"✅ Order transaction finished successfully!");
        output.WriteLine($"   Signature: {finishResponse.Signature!.Value[..16]}...");
        output.WriteLine($"   QR Code: {finishResponse.QrCodeData![..50]}...");
    }

    [Fact]
    public async Task CancelTransaction_FromActive_ShouldTransitionToCancelled()
    {
        // Arrange - Start a transaction
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        output.WriteLine($"Transaction {transactionId} started");

        // Act - Cancel the transaction
        TxResponse cancelResponse = await Fixture.TransactionClient.CancelTransactionAsync(
            TssId,
            transactionId,
            new CancelTransactionRequest
            {
                ClientId = ClientId,
                Schema = TransactionSchema.ForReceipt(new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,
                    AmountsPerVatRate = new List<VatRateAmount>()
                })
            });

        // Assert
        cancelResponse.Should().NotBeNull();
        cancelResponse.State.Should().Be(TxState.Cancelled);
        cancelResponse.Id.Should().Be(transactionId);

        output.WriteLine($"✅ Transaction cancelled successfully!");
        output.WriteLine($"   Final State: {cancelResponse.State}");
    }

    [Fact]
    public async Task CreateMultipleTransactions_ShouldAllSucceed()
    {
        // Arrange - Create 3 transactions
        TxId[] transactionIds = new[]
        {
            TxId.New(),
            TxId.New(),
            TxId.New()
        };

        output.WriteLine($"Creating {transactionIds.Length} transactions...");

        // Act - Start and finish all transactions
        foreach (TxId txId in transactionIds)
        {
            // Start
            await Fixture.TransactionClient.StartTransactionAsync(
                TssId,
                txId,
                new StartTransactionRequest { ClientId = ClientId });

            // Finish
            TxResponse response = await Fixture.TransactionClient.FinishTransactionAsync(
                TssId,
                txId,
                FinishTransactionRequest.CreateReceipt(
                    ClientId,
                    new Receipt
                    {
                        ReceiptType = ReceiptType.Receipt,
                        AmountsPerVatRate = new List<VatRateAmount>
                        {
                            new() { VatRate = VatRate.Normal, Amount = Eur(5.00m) }
                        },
                        AmountsPerPaymentType = new List<PaymentTypeAmount>
                        {
                            new() { PaymentType = PaymentType.Cash, Amount = Eur(5.00m) }
                        }
                    }));

            output.WriteLine($"   ✓ Transaction {txId} finished (counter: {response.Signature!.Counter})");
        }

        // Assert - Verify all transactions exist
        TxListResponse allTransactions = await Fixture.TransactionClient.ListTransactionsAsync(TssId);
        IEnumerable<TxResponse> allTransactionData = allTransactions.Data ?? Enumerable.Empty<TxResponse>();
        List<TxResponse> ourTransactions = allTransactionData
            .Where(t => t.Id is TxId id && transactionIds.Contains(id))
            .ToList();

        ourTransactions.Should().HaveCount(transactionIds.Length);
        ourTransactions.Should().OnlyContain(t => t.State == TxState.Finished);

        output.WriteLine($"✅ All {transactionIds.Length} transactions completed successfully!");
    }

    [Fact]
    public async Task ListTransactions_ShouldContainCreatedTransactions()
    {
        // Arrange - Create some transactions
        TxId tx1 = TxId.New();
        TxId tx2 = TxId.New();

        await Fixture.TransactionClient.StartTransactionAsync(TssId, tx1, new StartTransactionRequest { ClientId = ClientId });
        await Fixture.TransactionClient.FinishTransactionAsync(TssId, tx1, FinishTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount> { new() { VatRate = VatRate.Normal, Amount = Eur(1.00m) } },
                AmountsPerPaymentType = new List<PaymentTypeAmount> { new() { PaymentType = PaymentType.Cash, Amount = Eur(1.00m) } }
            }));

        await Fixture.TransactionClient.StartTransactionAsync(TssId, tx2, new StartTransactionRequest { ClientId = ClientId });

        // Act
        TxListResponse response = await Fixture.TransactionClient.ListTransactionsAsync(TssId);

        // Assert
        response.Should().NotBeNull();
        List<TxResponse> data = (response.Data ?? new List<TxResponse>()).ToList();
        data.Should().Contain(t => t.Id == tx1, "Finished transaction should be in list");
        data.Should().Contain(t => t.Id == tx2, "Active transaction should be in list");

        TxResponse tx1Data = data.First(t => t.Id == tx1);
        TxResponse tx2Data = data.First(t => t.Id == tx2);

        tx1Data.State.Should().Be(TxState.Finished);
        tx2Data.State.Should().Be(TxState.Active);

        output.WriteLine($"✅ Listed {data.Count} transactions");
        output.WriteLine($"   Transaction 1: {tx1} - {tx1Data.State}");
        output.WriteLine($"   Transaction 2: {tx2} - {tx2Data.State}");
    }

    [Fact]
    public async Task ListClientTransactions_ShouldContainOnlyClientTransactions()
    {
        // Arrange - Create transaction for this client
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        // Act
        TxListResponse response = await Fixture.TransactionClient.ListClientTransactionsAsync(TssId, ClientId);

        // Assert
        response.Should().NotBeNull();
        List<TxResponse> clientTransactions = (response.Data ?? new List<TxResponse>()).ToList();
        clientTransactions.Should().Contain(t => t.Id == transactionId, "Client's transaction should be in list");
        clientTransactions.Should().OnlyContain(t => t.ClientId == ClientId, "All transactions should belong to this client");

        output.WriteLine($"✅ Listed {clientTransactions.Count} transaction(s) for Client {ClientId}");
    }

    [Fact]
    public async Task ListAllTransactions_ShouldContainTransactionsAcrossAllTss()
    {
        // Arrange - Create transaction
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        // Act - Sort by TimeStart descending to get newest transactions first
        ListTransactionsQueryParameters queryParams = new ListTransactionsQueryParameters
        {
            Sort = TransactionSortOption.By(TransactionSortField.TimeStart, SortDirection.Descending),
            Limit = 100 // Limit to first page to avoid loading too many old transactions
        };
        TxListResponse response = await Fixture.TransactionClient.ListAllTransactionsAsync(queryParams);

        // Assert
        response.Should().NotBeNull();
        List<TxResponse> globalTransactions = (response.Data ?? new List<TxResponse>()).ToList();
        globalTransactions.Should().Contain(t => t.Id == transactionId, "Our transaction should be in global list");

        output.WriteLine($"✅ Listed {globalTransactions.Count} transaction(s) across all TSS (sorted by TimeStart DESC)");
        output.WriteLine($"   Our transaction {transactionId} found in global list");
    }

    [Fact]
    public async Task UpdateTransactionMetadata_ShouldPersist()
    {
        // Arrange - Create and finish transaction
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(TssId, transactionId, new StartTransactionRequest { ClientId = ClientId });
        await Fixture.TransactionClient.FinishTransactionAsync(TssId, transactionId, FinishTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount> { new() { VatRate = VatRate.Normal, Amount = Eur(1.00m) } },
                AmountsPerPaymentType = new List<PaymentTypeAmount> { new() { PaymentType = PaymentType.Cash, Amount = Eur(1.00m) } }
            }));

        // Act - Update metadata
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("test-tx", "metadata-value")
            .Add("tx-type", "finished-transaction")
            .Add("timestamp", DateTime.UtcNow.ToString("O"));

        MetadataCollection updatedMetadata = await Fixture.TransactionClient.UpdateTransactionMetadataAsync(TssId, transactionId, metadata);

        // Assert
        updatedMetadata.Should().NotBeNull();
        updatedMetadata.Should().HaveCount(3);
        updatedMetadata["test-tx"].Should().Be("metadata-value");
        updatedMetadata["tx-type"].Should().Be("finished-transaction");
        updatedMetadata.Should().ContainKey("timestamp");

        output.WriteLine($"✅ Transaction metadata updated: {updatedMetadata.Count} keys");
        output.WriteLine($"   Keys: {string.Join(", ", updatedMetadata.Keys)}");
    }

    [Fact]
    public async Task GetTransactionMetadata_AfterUpdate_ShouldReturnNewMetadata()
    {
        // Arrange - Create transaction and set metadata
        TxId transactionId = TxId.New();
        await Fixture.TransactionClient.StartTransactionAsync(TssId, transactionId, new StartTransactionRequest { ClientId = ClientId });
        await Fixture.TransactionClient.FinishTransactionAsync(TssId, transactionId, FinishTransactionRequest.CreateReceipt(
            ClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount> { new() { VatRate = VatRate.Normal, Amount = Eur(1.00m) } },
                AmountsPerPaymentType = new List<PaymentTypeAmount> { new() { PaymentType = PaymentType.Cash, Amount = Eur(1.00m) } }
            }));

        MetadataCollection metadata = MetadataCollection.Empty
            .Add("key1", "value1")
            .Add("key2", "value2");
        await Fixture.TransactionClient.UpdateTransactionMetadataAsync(TssId, transactionId, metadata);

        // Act
        MetadataCollection retrievedMetadata = await Fixture.TransactionClient.GetTransactionMetadataAsync(TssId, transactionId);

        // Assert
        retrievedMetadata.Should().NotBeNull();
        retrievedMetadata.Should().HaveCount(2);
        retrievedMetadata["key1"].Should().Be("value1");
        retrievedMetadata["key2"].Should().Be("value2");

        output.WriteLine($"✅ Transaction metadata retrieved: {retrievedMetadata.Count} keys");
        output.WriteLine($"   key1 = {retrievedMetadata["key1"]}");
        output.WriteLine($"   key2 = {retrievedMetadata["key2"]}");
    }

    [Fact]
    public async Task UpdateTransaction_WithManualRevision_ShouldUseProvidedRevision()
    {
        // Arrange - Start a transaction
        TxId transactionId = TxId.New();
        TxResponse startResponse = await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        output.WriteLine($"Transaction {transactionId} started (revision {startResponse.Revision})");

        // Act - Update with explicitly provided revision number (manual revision)
        // Transaction is at revision 1 (from START), so we pass 2 to create the next revision
        TxResponse updateResponse = await Fixture.TransactionClient.UpdateTransactionAsync(
            TssId,
            transactionId,
            UpdateTransactionRequest.CreateReceipt(
                ClientId,
                new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,
                    AmountsPerVatRate = new List<VatRateAmount>
                    {
                        new() { VatRate = VatRate.Normal, Amount = Eur(15.50m) }
                    },
                    AmountsPerPaymentType = new List<PaymentTypeAmount>
                    {
                        new() { PaymentType = PaymentType.Cash, Amount = Eur(15.50m) }
                    }
                }),
            txRevision: 2); // Explicitly provide next revision (current is 1, creating 2)

        // Assert
        updateResponse.Should().NotBeNull();
        updateResponse.Revision.Should().Be(2, "Revision should increment after UPDATE");
        updateResponse.State.Should().Be(TxState.Active, "Transaction should still be ACTIVE after UPDATE");

        output.WriteLine($"✅ Transaction updated with manual revision!");
        output.WriteLine($"   Provided revision: 1");
        output.WriteLine($"   New revision: {updateResponse.Revision}");
        output.WriteLine($"   State: {updateResponse.State}");
    }

    [Fact]
    public async Task OrderTransaction_CompleteWorkflow_WithMultipleLineItems_ShouldSucceed()
    {
        // Arrange
        TxId transactionId = TxId.New();

        output.WriteLine($"=== Complete Order Workflow Test ===");
        output.WriteLine($"Transaction ID: {transactionId}");

        // Act & Assert - STEP 1: Start transaction
        output.WriteLine("\nSTEP 1: Starting order transaction...");
        TxResponse startResponse = await Fixture.TransactionClient.StartTransactionAsync(
            TssId,
            transactionId,
            new StartTransactionRequest { ClientId = ClientId });

        startResponse.State.Should().Be(TxState.Active);
        startResponse.Revision.Should().Be(1);
        output.WriteLine($"✅ Transaction started - State: {startResponse.State}, Revision: {startResponse.Revision}");

        // STEP 2: Update with Order data (multiple line items)
        output.WriteLine("\nSTEP 2: Updating with Order data (multiple line items)...");
        TxResponse updateResponse = await Fixture.TransactionClient.UpdateTransactionAsync(
            TssId,
            transactionId,
            UpdateTransactionRequest.CreateOrder(
                ClientId,
                new Order
                {
                    LineItems = new List<LineItem>
                    {
                        new() { Quantity = 2m, Text = "Pizza Margherita", PricePerUnit = Eur(8.90m) },
                        new() { Quantity = 1m, Text = "Pizza Salami", PricePerUnit = Eur(9.50m) },
                        new() { Quantity = 3m, Text = "Cola 0.5L", PricePerUnit = Eur(2.50m) }
                    }
                }),
            txRevision: null); // Auto-fetch current revision

        updateResponse.Revision.Should().Be(2, "Revision should increment after UPDATE");
        updateResponse.State.Should().Be(TxState.Active);
        updateResponse.Schema?.StandardV1?.Order?.LineItems.Should().HaveCount(3);
        output.WriteLine($"✅ Order updated - Revision: {updateResponse.Revision}, Line Items: {updateResponse.Schema?.StandardV1?.Order?.LineItems?.Count}");

        // STEP 3: Finish transaction
        output.WriteLine("\nSTEP 3: Finishing order transaction...");
        TxResponse finishResponse = await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            transactionId,
            FinishTransactionRequest.CreateOrder(
                ClientId,
                new Order
                {
                    LineItems = new List<LineItem>
                    {
                        new() { Quantity = 2m, Text = "Pizza Margherita", PricePerUnit = Eur(8.90m) },
                        new() { Quantity = 1m, Text = "Pizza Salami", PricePerUnit = Eur(9.50m) },
                        new() { Quantity = 3m, Text = "Cola 0.5L", PricePerUnit = Eur(2.50m) }
                    }
                }),
            txRevision: null);

        finishResponse.State.Should().Be(TxState.Finished);
        finishResponse.Signature.Should().NotBeNull("Order transaction must be cryptographically signed");
        finishResponse.QrCodeData.Should().NotBeNullOrEmpty("QR code required for KassenSichV compliance");
        finishResponse.Signature!.Counter.Should().BeGreaterThan(0);
        output.WriteLine($"✅ Order transaction finished!");
        output.WriteLine($"   State: {finishResponse.State}");
        output.WriteLine($"   Signature Counter: {finishResponse.Signature.Counter}");
        output.WriteLine($"   QR Code: {finishResponse.QrCodeData![..50]}...");

        // STEP 4: Verify transaction persisted
        output.WriteLine("\nSTEP 4: Verifying persisted transaction...");
        TxResponse getResponse = await Fixture.TransactionClient.GetTransactionAsync(TssId, transactionId);

        getResponse.Id.Should().Be(transactionId);
        getResponse.State.Should().Be(TxState.Finished);
        getResponse.Schema?.StandardV1?.Order?.LineItems.Should().HaveCount(3, "All line items should be persisted");
        output.WriteLine($"✅ Transaction verified - All 3 line items persisted");

        output.WriteLine("\n✅ Complete order workflow test passed!");
    }
}
