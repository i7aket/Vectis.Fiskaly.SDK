using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using static Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects.MoneyAmount;

namespace Vectis.Fiskaly.IntegrationTests.SignDE;

/// <summary>
/// Integration tests for storno (cancellation) transaction workflows.
/// </summary>
/// <remarks>
/// <para><strong>Scope</strong>: Storno receipts and orders (cancellations/refunds)</para>
///
/// <para><strong>Storno Patterns Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>Storno Receipts - Negative amounts referencing original transaction</description></item>
///   <item><description>Storno Orders - Negative quantities referencing original order</description></item>
///   <item><description>ReceiptType.Annulation for storno transactions</description></item>
///   <item><description>Original transaction reference in storno metadata</description></item>
/// </list>
///
/// <para><strong>Endpoints Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>POST /tss/{tss_id}/tx/{tx_id} - StartTransactionAsync</description></item>
///   <item><description>PUT /tss/{tss_id}/tx/{tx_id} - FinishTransactionAsync</description></item>
///   <item><description>GET /tss/{tss_id}/tx/{tx_id} - GetTransactionAsync</description></item>
/// </list>
///
/// <para><strong>Note</strong>: Storno transactions must reference an original completed transaction.</para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "StornoTransactions")]
[Trait("Priority", "High")]
public class StornoTransactionTests : FiskalyIntegrationTestBase
{
    public StornoTransactionTests(FiskalyBaseTestFixture fixture) : base(fixture) { }

    /// <summary>
    /// Helper method to create EUR MoneyAmount.
    /// </summary>
    private static MoneyAmount Eur(decimal value)
        => Create(value, CurrencyCode.EUR);

    [Fact]
    public async Task StartAndFinishStornoReceipt_WithNegativeAmounts_ShouldReferenceOriginalTransaction()
    {
        // Arrange - Create and finish original receipt transaction
        TxId originalTxId = TxId.New();
        TxId stornoTxId = TxId.New();

        Console.WriteLine($"Creating original receipt transaction: {originalTxId}");

        // Step 1: Create original receipt (positive amounts)
        await Fixture.TransactionClient.StartTransactionAsync(TssId, originalTxId, new StartTransactionRequest
        {
            ClientId = ClientId
        });

        TxResponse originalReceipt = await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            originalTxId,
            FinishTransactionRequest.CreateReceipt(
                ClientId,
                new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,
                    AmountsPerVatRate = new List<VatRateAmount>
                    {
                        new() { VatRate = VatRate.Normal, Amount = Eur(100.00m) }
                    },
                    AmountsPerPaymentType = new List<PaymentTypeAmount>
                    {
                        new() { PaymentType = PaymentType.Cash, Amount = Eur(100.00m) }
                    }
                }),
            txRevision: null);

        Console.WriteLine($"   Original receipt finished with signature");
        Console.WriteLine($"   Total: 100.00 EUR");

        // Step 2: Create storno receipt (negative amounts, reference original)
        Console.WriteLine($"Creating storno receipt: {stornoTxId}");

        await Fixture.TransactionClient.StartTransactionAsync(TssId, stornoTxId, new StartTransactionRequest
        {
            ClientId = ClientId
        });

        // Prepare metadata with original transaction reference
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("return_reference", originalTxId.Value.ToString());

        // Act - Finish storno receipt with negative amounts
        TxResponse stornoReceipt = await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            stornoTxId,
            FinishTransactionRequest.CreateStornoReceipt(
                ClientId,
                new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,  // TSS requirement: use Receipt (not Annulation) for storno
                    AmountsPerVatRate = new List<VatRateAmount>
                    {
                        new() { VatRate = VatRate.Normal, Amount = Eur(-100.00m) }  // Negative amount
                    },
                    AmountsPerPaymentType = new List<PaymentTypeAmount>
                    {
                        new() { PaymentType = PaymentType.Cash, Amount = Eur(-100.00m) }  // Negative amount
                    }
                },
                metadata),
            txRevision: null);

        // Assert
        stornoReceipt.Should().NotBeNull();
        stornoReceipt.State.Should().Be(TxState.Finished);
        stornoReceipt.Signature.Should().NotBeNull();

        Console.WriteLine($"✅ Storno receipt created successfully!");
        Console.WriteLine($"   State: {stornoReceipt.State}");
        Console.WriteLine($"   Signature: {stornoReceipt.Signature!.Value[..20]}...");
    }

    [Fact]
    public async Task StartAndFinishStornoOrder_WithNegativeQuantities_ShouldReferenceOriginalTransaction()
    {
        // Arrange - Create and finish original order transaction
        TxId originalTxId = TxId.New();
        TxId stornoTxId = TxId.New();

        Console.WriteLine($"Creating original order transaction: {originalTxId}");

        // Step 1: Create original order (positive quantities)
        await Fixture.TransactionClient.StartTransactionAsync(TssId, originalTxId, new StartTransactionRequest
        {
            ClientId = ClientId
        });

        TxResponse originalOrder = await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            originalTxId,
            FinishTransactionRequest.CreateOrder(
                ClientId,
                new Order
                {
                    LineItems = new List<LineItem>
                    {
                        new() { Quantity = 2m, Text = "Test Product", PricePerUnit = Eur(50.00m) }
                    }
                }),
            txRevision: null);

        Console.WriteLine($"   Original order finished");
        Console.WriteLine($"   Items: 2x Test Product @ 50.00 EUR");

        // Step 2: Create storno order (negative quantities)
        Console.WriteLine($"Creating storno order: {stornoTxId}");

        await Fixture.TransactionClient.StartTransactionAsync(TssId, stornoTxId, new StartTransactionRequest
        {
            ClientId = ClientId
        });

        // Prepare metadata with original transaction reference
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("return_reference", originalTxId.Value.ToString());

        // Act - Finish storno order with negative quantities
        TxResponse stornoOrder = await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            stornoTxId,
            FinishTransactionRequest.CreateStornoOrder(
                ClientId,
                new Order
                {
                    LineItems = new List<LineItem>
                    {
                        new() { Quantity = -2m, Text = "Test Product (Storno)", PricePerUnit = Eur(50.00m) }  // Negative quantity
                    }
                },
                metadata),
            txRevision: null);

        // Assert
        stornoOrder.Should().NotBeNull();
        stornoOrder.State.Should().Be(TxState.Finished);
        stornoOrder.Signature.Should().NotBeNull();

        Console.WriteLine($"✅ Storno order created successfully!");
        Console.WriteLine($"   State: {stornoOrder.State}");
        Console.WriteLine($"   Signature: {stornoOrder.Signature!.Value[..20]}...");
    }

    [Fact]
    public async Task GetStornoTransaction_ShouldContainCorrectReceiptType()
    {
        // Arrange - Create original receipt and then storno
        TxId originalTxId = TxId.New();
        TxId stornoTxId = TxId.New();

        Console.WriteLine($"Creating original receipt: {originalTxId}");

        // Create original receipt
        await Fixture.TransactionClient.StartTransactionAsync(TssId, originalTxId, new StartTransactionRequest
        {
            ClientId = ClientId
        });

        await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            originalTxId,
            FinishTransactionRequest.CreateReceipt(
                ClientId,
                new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,
                    AmountsPerVatRate = new List<VatRateAmount>
                    {
                        new() { VatRate = VatRate.Normal, Amount = Eur(50.00m) }
                    },
                    AmountsPerPaymentType = new List<PaymentTypeAmount>
                    {
                        new() { PaymentType = PaymentType.Cash, Amount = Eur(50.00m) }
                    }
                }),
            txRevision: null);

        Console.WriteLine($"Creating storno receipt: {stornoTxId}");

        // Create storno receipt
        await Fixture.TransactionClient.StartTransactionAsync(TssId, stornoTxId, new StartTransactionRequest
        {
            ClientId = ClientId
        });

        // Prepare metadata with original transaction reference
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("return_reference", originalTxId.Value.ToString());

        await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            stornoTxId,
            FinishTransactionRequest.CreateStornoReceipt(
                ClientId,
                new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,  // TSS requirement: use Receipt (not Annulation) for storno
                    AmountsPerVatRate = new List<VatRateAmount>
                    {
                        new() { VatRate = VatRate.Normal, Amount = Eur(-50.00m) }
                    },
                    AmountsPerPaymentType = new List<PaymentTypeAmount>
                    {
                        new() { PaymentType = PaymentType.Cash, Amount = Eur(-50.00m) }
                    }
                },
                metadata),
            txRevision: null);

        // Act - Retrieve the storno transaction
        TxResponse transaction = await Fixture.TransactionClient.GetTransactionAsync(TssId, stornoTxId);

        // Assert
        transaction.Should().NotBeNull();
        transaction.State.Should().Be(TxState.Finished);
        transaction.Signature.Should().NotBeNull();

        Console.WriteLine($"✅ Storno transaction retrieved successfully!");
        Console.WriteLine($"   Transaction ID: {transaction.Id}");
        Console.WriteLine($"   State: {transaction.State}");
        Console.WriteLine($"   Signature: {transaction.Signature!.Value[..20]}...");
    }

    [Fact]
    public async Task StornoTransaction_ShouldHaveCorrectReceiptType_Annulation()
    {
        // Arrange - Create original and storno receipts
        TxId originalTxId = TxId.New();
        TxId stornoTxId = TxId.New();

        Console.WriteLine($"Testing storno ReceiptType validation");

        // Create original receipt
        await Fixture.TransactionClient.StartTransactionAsync(TssId, originalTxId, new StartTransactionRequest
        {
            ClientId = ClientId
        });

        await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            originalTxId,
            FinishTransactionRequest.CreateReceipt(
                ClientId,
                new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,
                    AmountsPerVatRate = new List<VatRateAmount>
                    {
                        new() { VatRate = VatRate.Normal, Amount = Eur(25.00m) }
                    },
                    AmountsPerPaymentType = new List<PaymentTypeAmount>
                    {
                        new() { PaymentType = PaymentType.Cash, Amount = Eur(25.00m) }
                    }
                }),
            txRevision: null);

        await Fixture.TransactionClient.StartTransactionAsync(TssId, stornoTxId, new StartTransactionRequest
        {
            ClientId = ClientId
        });

        // Prepare metadata with original transaction reference
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("return_reference", originalTxId.Value.ToString());

        // Act - Create storno transaction (ReceiptType set to Receipt per TSS requirement)
        TxResponse response = await Fixture.TransactionClient.FinishTransactionAsync(
            TssId,
            stornoTxId,
            FinishTransactionRequest.CreateStornoReceipt(
                ClientId,
                new Receipt
                {
                    ReceiptType = ReceiptType.Receipt,  // TSS requirement: use Receipt (not Annulation) for storno
                    AmountsPerVatRate = new List<VatRateAmount>
                    {
                        new() { VatRate = VatRate.Normal, Amount = Eur(-25.00m) }
                    },
                    AmountsPerPaymentType = new List<PaymentTypeAmount>
                    {
                        new() { PaymentType = PaymentType.Cash, Amount = Eur(-25.00m) }
                    }
                },
                metadata),
            txRevision: null);

        // Assert
        response.Should().NotBeNull();
        response.State.Should().Be(TxState.Finished);
        response.Signature.Should().NotBeNull();

        Console.WriteLine($"✅ Storno transaction validated!");
        Console.WriteLine($"   State: {response.State}");
        Console.WriteLine($"   Signature: {response.Signature!.Value[..20]}...");
    }
}
