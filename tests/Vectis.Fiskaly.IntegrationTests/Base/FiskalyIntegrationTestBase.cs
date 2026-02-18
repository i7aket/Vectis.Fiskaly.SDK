using Vectis.Fiskaly.SDK.SignDE.Admin.Requests;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Clients.Requests;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Requests;
using Vectis.Fiskaly.SDK.SignDE.Tss.Responses;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.IntegrationTests.Base;

/// <summary>
/// Abstract base class for Fiskaly integration tests with automatic TSS and Client setup/cleanup.
/// </summary>
/// <remarks>
/// <para><strong>Architecture Pattern</strong>: Minimal State + API as Source of Truth</para>
///
/// <para><strong>What Gets Created (per test class)</strong>:</para>
/// <list type="bullet">
///   <item><description>1 TSS (INITIALIZED state)</description></item>
///   <item><description>1 Client (REGISTERED state)</description></item>
///   <item><description>Admin PIN configured</description></item>
/// </list>
///
/// <para><strong>What Gets Cleaned Up (after all tests)</strong>:</para>
/// <list type="bullet">
///   <item><description>All active transactions cancelled</description></item>
///   <item><description>Client deregistered</description></item>
///   <item><description>TSS disabled (permanent operation)</description></item>
/// </list>
///
/// <para><strong>Minimal State Stored</strong>:</para>
/// <list type="bullet">
///   <item><description>TssId - TSS identifier</description></item>
///   <item><description>ClientId - Client identifier</description></item>
///   <item><description>AdminPuk - returned ONLY ONCE on TSS creation</description></item>
///   <item><description>AdminPin - for admin authentication</description></item>
/// </list>
///
/// <para><strong>Usage Example</strong>:</para>
/// <code>
/// public class MyTests : FiskalyIntegrationTestBase
/// {
///     public MyTests(FiskalyClientFixture fixture) : base(fixture) { }
///
///     [Fact]
///     public async Task CreateTransaction_ShouldSucceed()
///     {
///         // TssId and ClientId already available!
///         var tx = await Fixture.TransactionClient.StartTransactionAsync(
///             TssId,
///             TxId.New(),
///             new StartTransactionRequest { ClientId = ClientId });
///     }
/// }
/// </code>
/// </remarks>
public abstract class FiskalyIntegrationTestBase : IClassFixture<FiskalyBaseTestFixture>, IAsyncLifetime
{
    /// <summary>
    /// Gets the Fiskaly base test fixture providing access to all API clients with dedicated credentials.
    /// </summary>
    protected FiskalyBaseTestFixture Fixture { get; }

    /// <summary>
    /// Gets the TSS identifier for this test class.
    /// </summary>
    /// <remarks>
    /// Created during <see cref="InitializeAsync"/> and available in all test methods.
    /// Automatically cleaned up (disabled) during <see cref="DisposeAsync"/>.
    /// </remarks>
    protected TssId TssId { get; private set; } = default;

    /// <summary>
    /// Gets the Client identifier for this test class.
    /// </summary>
    /// <remarks>
    /// Created during <see cref="InitializeAsync"/> and available in all test methods.
    /// Automatically deregistered during <see cref="DisposeAsync"/>.
    /// </remarks>
    protected ClientId ClientId { get; private set; } = default;

    /// <summary>
    /// Gets the Admin PUK (Personal Unblocking Key) for TSS admin operations.
    /// </summary>
    /// <remarks>
    /// ⚠️ CRITICAL: This value is only returned ONCE when creating the TSS.
    /// It cannot be retrieved later. Used for admin PIN operations.
    /// </remarks>
    protected string AdminPukValue { get; private set; } = null!;

    /// <summary>
    /// Gets the Admin PIN used for admin authentication.
    /// </summary>
    /// <remarks>
    /// Default value: "base-test-pin-12345"
    /// Set during <see cref="InitializeAsync"/> via <see cref="IAdminClient.ChangeAdminPinAsync"/>.
    /// </remarks>
    protected string AdminPinValue { get; } = "base-test-pin-12345";

    /// <summary>
    /// Initializes a new instance of <see cref="FiskalyIntegrationTestBase"/>.
    /// </summary>
    /// <param name="fixture">The Fiskaly base test fixture.</param>
    protected FiskalyIntegrationTestBase(FiskalyBaseTestFixture fixture)
    {
        Fixture = fixture;
    }

    /// <summary>
    /// Initializes test resources (TSS and Client).
    /// </summary>
    /// <remarks>
    /// <para>Called ONCE per test class before any test methods run (xUnit IAsyncLifetime).</para>
    ///
    /// <para><strong>Setup Steps</strong>:</para>
    /// <list type="number">
    ///   <item><description>Create TSS → CREATED state</description></item>
    ///   <item><description>Capture AdminPuk (only returned once!)</description></item>
    ///   <item><description>Set Admin PIN</description></item>
    ///   <item><description>Authenticate Admin</description></item>
    ///   <item><description>Initialize TSS → INITIALIZED state</description></item>
    ///   <item><description>Register Client → REGISTERED state</description></item>
    ///   <item><description>Logout Admin</description></item>
    /// </list>
    /// </remarks>
    public async Task InitializeAsync()
    {
        Console.WriteLine($"\n🚀 [SETUP START] {GetType().Name}");
        Console.WriteLine($"   Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");

        // ============================================
        // PRE-CHECK: TSS Limit Check with Retry Logic
        // ============================================
        // Fiskaly TEST environment has a limit of 5 TSS instances max.
        // If 6+ test classes run in parallel, we hit the limit.
        // This retry logic waits for other tests to finish cleanup.
        const int maxRetries = 10;
        const int retryDelaySeconds = 5;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            ListTssResponse allTss = await Fixture.TssClient.ListTssAsync();
            int activeCount = allTss.Data?.Count(t => t.State != TssState.Disabled) ?? 0;

            if (activeCount < 5)
            {
                Console.WriteLine($"   ✅ TSS limit check passed: {activeCount}/5 active TSS");
                break;
            }

            if (attempt == maxRetries)
            {
                throw new InvalidOperationException(
                    $"❌ TSS limit reached: {activeCount}/5 active TSS. " +
                    $"Cannot create new TSS for test. Waited {maxRetries * retryDelaySeconds}s for cleanup. " +
                    $"Please manually disable old TSS instances or wait for other tests to complete.");
            }

            Console.WriteLine($"   ⏳ TSS limit at capacity ({activeCount}/5), waiting {retryDelaySeconds}s " +
                             $"for other tests to finish cleanup (attempt {attempt}/{maxRetries})...");

            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
        }

        // Step 1: Create TSS
        Console.WriteLine($"   STEP 1: Creating TSS...");
        TssId = TssId.New();
        TssResponse tssResponse = await Fixture.TssClient.CreateTssAsync(TssId);
        Console.WriteLine($"   ✅ STEP 1: TSS created: {TssId}");

        // Step 2: Capture AdminPuk (CRITICAL - only returned once!)
        Console.WriteLine($"   STEP 2: Capturing AdminPuk...");
        AdminPukValue = tssResponse.AdminPuk?.Value ?? throw new InvalidOperationException("AdminPuk is null");
        Console.WriteLine($"   ✅ STEP 2: AdminPuk captured");

        // Step 3: Transition to UNINITIALIZED state (required for PIN change)
        Console.WriteLine($"   STEP 3: Uninitializing TSS...");
        UpdateTssRequest uninitializeRequest = UpdateTssRequest.Uninitialize();
        await Fixture.TssClient.UpdateTssAsync(TssId, uninitializeRequest);
        Console.WriteLine($"   ✅ STEP 3: TSS uninitialized");

        // Step 4: Set Admin PIN
        Console.WriteLine($"   STEP 4: Setting Admin PIN...");
        await Fixture.AdminClient.ChangeAdminPinAsync(TssId, new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From(AdminPukValue),
            NewAdminPin = AdminPin.From(AdminPinValue)
        });
        Console.WriteLine($"   ✅ STEP 4: Admin PIN set");

        // Step 5: Authenticate Admin
        Console.WriteLine($"   STEP 5: Authenticating Admin...");
        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });
        Console.WriteLine($"   ✅ STEP 5: Admin authenticated");

        // Step 6: Initialize TSS (UNINITIALIZED → INITIALIZED)
        Console.WriteLine($"   STEP 6: Initializing TSS...");
        MetadataCollection initMetadata = MetadataCollection.Empty
            .Add("purpose", "integration-test")
            .Add("test_class", GetType().Name)
            .Add("created_at", DateTime.UtcNow.ToString("o"));

        string description = $"Test: {GetType().Name} - Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

        UpdateTssRequest initializeRequest = UpdateTssRequest.Initialize(description, initMetadata);
        await Fixture.TssClient.UpdateTssAsync(TssId, initializeRequest);
        Console.WriteLine($"   ✅ STEP 6: TSS initialized with metadata");

        // Step 7: Register Client
        Console.WriteLine($"   STEP 7: Registering Client...");
        ClientId = ClientId.New();
        await Fixture.ClientManagementClient.CreateClientAsync(TssId, ClientId, new CreateClientRequest
        {
            SerialNumber = ClientSerialNumber.From($"TEST-{ClientId.ToString()[..8]}")
        });
        Console.WriteLine($"   ✅ STEP 7: Client registered: {ClientId}");

        // Step 8: Logout Admin (cleanup admin session)
        Console.WriteLine($"   STEP 8: Logging out Admin...");
        await Fixture.AdminClient.LogoutAdminAsync(TssId);
        Console.WriteLine($"   ✅ STEP 8: Admin logged out");

        Console.WriteLine($"✅ [SETUP COMPLETE] {GetType().Name}");
        Console.WriteLine($"   TSS: {TssId}");
        Console.WriteLine($"   Client: {ClientId}\n");
    }

    /// <summary>
    /// Cleans up test resources (transactions, client, TSS).
    /// </summary>
    /// <remarks>
    /// <para>Called ONCE per test class after all test methods complete (xUnit IAsyncLifetime).</para>
    ///
    /// <para><strong>Cleanup Steps</strong>:</para>
    /// <list type="number">
    ///   <item><description>Authenticate Admin (required for disable operations)</description></item>
    ///   <item><description>Cancel all active transactions (API-based via ListTransactionsAsync)</description></item>
    ///   <item><description>Deregister Client</description></item>
    ///   <item><description>Disable TSS (permanent operation!)</description></item>
    ///   <item><description>Logout Admin</description></item>
    /// </list>
    ///
    /// <para><strong>Error Handling</strong>:</para>
    /// All cleanup operations are wrapped in try-catch to prevent cleanup failures from masking test results.
    /// Individual failures are ignored as tests have already completed.
    /// </remarks>
    public async Task DisposeAsync()
    {
        Console.WriteLine($"\n🧹 [CLEANUP START] {GetType().Name}");
        Console.WriteLine($"   Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
        Console.WriteLine($"   TSS: {TssId}");
        Console.WriteLine($"   Client: {ClientId}");

        try
        {
            // Step 1: Authenticate Admin (required for disable operations)
            Console.WriteLine($"   STEP 1: Authenticating Admin...");
            await Fixture.AdminClient.AuthenticateAdminAsync(
                TssId,
                new AdminAuthenticationRequest
                {
                    AdminPin = AdminPin.From(AdminPinValue)
                });
            Console.WriteLine($"   ✅ STEP 1: Admin authenticated");

            // Step 2: Cancel all active transactions (API-based cleanup)
            Console.WriteLine($"   STEP 2: Listing active transactions...");
            TxListResponse transactions = await Fixture.TransactionClient.ListTransactionsAsync(TssId);
            IEnumerable<TxResponse> transactionItems = transactions.Data?.AsEnumerable() ?? Enumerable.Empty<TxResponse>();
            List<(TxResponse Response, TxId TxId, ClientId ClientId)> activeTransactions = new();

            foreach (TxResponse candidate in transactionItems)
            {
                if (candidate.State != TxState.Active)
                {
                    continue;
                }

                if (candidate.Id is not TxId txId || candidate.ClientId is not ClientId clientId)
                {
                    Console.WriteLine($"      ⚠️ Skipping transaction without identifiers (State: {candidate.State?.ToApiString() ?? "unknown"}).");
                    continue;
                }

                activeTransactions.Add((candidate, txId, clientId));
            }

            Console.WriteLine($"   📋 STEP 2: Found {activeTransactions.Count} active transaction(s)");

            foreach ((TxResponse transaction, TxId txId, ClientId clientId) in activeTransactions)
            {
                try
                {
                    Console.WriteLine($"      Cancelling transaction: {txId}...");
                    await Fixture.TransactionClient.CancelTransactionAsync(
                        TssId,
                        txId,
                        new CancelTransactionRequest
                        {
                            ClientId = clientId,  // Use transaction's ClientId, not base class
                            Schema = TransactionSchema.ForReceipt(new Receipt
                            {
                                ReceiptType = ReceiptType.Receipt,
                                AmountsPerVatRate = new List<VatRateAmount>()
                            })
                        });
                    Console.WriteLine($"      ✅ Cancelled transaction: {txId} (Client: {clientId})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      ⚠️ Failed to cancel transaction {txId}: {ex.GetType().Name} - {ex.Message}");
                }
            }
            Console.WriteLine($"   ✅ STEP 2: Transaction cleanup complete");

            // Step 3: Deregister Client (ALWAYS)
            try
            {
                Console.WriteLine($"   STEP 3: Deregistering Client {ClientId}...");
                await Fixture.ClientManagementClient.UpdateClientAsync(
                    TssId,
                    ClientId,
                    UpdateClientRequest.Deregister());
                Console.WriteLine($"   ✅ STEP 3: Client deregistered: {ClientId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ STEP 3 FAILED: Deregister client {ClientId}: {ex.GetType().Name} - {ex.Message}");
            }

            // Step 4: Disable TSS (ALWAYS - permanent operation!)
            try
            {
                Console.WriteLine($"   STEP 4: Disabling TSS {TssId}...");
                UpdateTssRequest disableRequest = UpdateTssRequest.Disable();
                await Fixture.TssClient.UpdateTssAsync(TssId, disableRequest);
                Console.WriteLine($"   ✅ STEP 4: TSS disabled: {TssId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ STEP 4 FAILED: Disable TSS {TssId}: {ex.GetType().Name} - {ex.Message}");
            }

            Console.WriteLine($"✅ [CLEANUP COMPLETE] {GetType().Name}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [CLEANUP FAILED] Fatal error during cleanup: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            // Suppress all cleanup exceptions - tests have already completed
            // Cleanup failures should not mask test results
        }
    }
}
