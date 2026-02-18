using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vectis.Fiskaly.SDK;
using Vectis.Fiskaly.SDK.Management.Organizations;
using Vectis.Fiskaly.SDK.SignDE.Admin;
using Vectis.Fiskaly.SDK.SignDE.Clients;
using Vectis.Fiskaly.SDK.SignDE.Exports;
using Vectis.Fiskaly.SDK.SignDE.Transactions;
using Vectis.Fiskaly.SDK.SignDE.Tss;
using Xunit.Abstractions;

namespace Vectis.Fiskaly.IntegrationTests.Base;

/// <summary>
/// Test fixture for Fiskaly SDK integration tests with segregated client architecture.
/// Implements IAsyncLifetime to set up and tear down the test environment.
/// </summary>
/// <remarks>
/// <para><strong>Fixture Responsibilities:</strong></para>
/// <list type="bullet">
///   <item>Load configuration from appsettings.test.json (FiskalyConfiguration + FiskalyTestConfiguration)</item>
///   <item>Register 5 segregated Fiskaly clients with automatic JWT authentication</item>
///   <item>Set up SharedTss in INITIALIZED state (CREATED → INITIALIZED)</item>
///   <item>Register SharedClient on SharedTss for use by tests</item>
///   <item>Provide test resources (TSS IDs, Admin PUKs, Admin PIN) to all tests</item>
/// </list>
///
/// <para><strong>Segregated Client Architecture:</strong></para>
/// <list type="bullet">
///   <item><strong>AdminClient</strong> - Admin authentication operations (login, logout, change PIN)</item>
///   <item><strong>TssClient</strong> - TSS lifecycle management (create, initialize, disable, update)</item>
///   <item><strong>ClientManagementClient</strong> - POS client registration and metadata</item>
///   <item><strong>TransactionClient</strong> - Transaction operations (start, update, finish, cancel)</item>
///   <item><strong>ExportClient</strong> - Export operations (create, trigger, retrieve)</item>
///   <item><strong>OrganizationClient</strong> - Management API organization operations (list, get)</item>
/// </list>
///
/// <para><strong>Initialization Flow:</strong></para>
/// <code>
/// InitializeAsync() runs ONCE before all tests:
///   1. Load FiskalyConfiguration + FiskalyTestConfiguration from appsettings.test.json
///   2. Register all Fiskaly services via AddFiskaly() extension method:
///      - IFiskalyAuthenticationService (JWT token management)
///      - JwtAuthHandler (automatic authentication for HTTP clients)
///      - 6 segregated clients (AdminClient, TssClient, ClientManagementClient, TransactionClient, ExportClient, OrganizationClient)
///   3. Resolve all 6 clients from DI container
///   4. Call InitializeSharedTssAsync():
///      a. Get current SharedTss state (via TssClient)
///      b. If CREATED → Set to UNINITIALIZED (via TssClient.SetTssStateAsync - internal method)
///      c. Set Admin PIN using Admin PUK (via AdminClient.ChangeAdminPinAsync)
///      d. Initialize TSS with Admin PIN (via TssClient.UpdateTssAsync + UpdateTssRequestFactory.Initialize) → INITIALIZED
///      e. Register SharedClient for tests (via ClientManagementClient.CreateClientAsync)
///      f. If INITIALIZED → Check if SharedClient exists, register if needed
/// </code>
///
/// <para><strong>Shared Test Resources:</strong></para>
/// <list type="bullet">
///   <item><strong>SharedTssId</strong> (from config) - INITIALIZED TSS for most tests (~25 tests use this)</item>
///   <item><strong>SharedClientId</strong> ("shared-client-001") - Pre-registered client</item>
///   <item><strong>TssForInitializeTest</strong> (from config) - UNINITIALIZED TSS for init tests</item>
///   <item><strong>TssForDuplicateTest</strong> (from config) - For error handling tests</item>
///   <item><strong>TssForCreateTest</strong> (from config) - CREATED state for state tests</item>
///   <item><strong>Tss1</strong> (from config) - General purpose TSS</item>
///   <item><strong>AdminPinValue</strong> ("1234567890") - Standard admin PIN</item>
/// </list>
///
/// <para><strong>⚠️ Fixture Initialization Failures:</strong></para>
/// <para>If SharedTss initialization fails, ~25 tests will fail with:</para>
/// <list type="bullet">
///   <item>"Expected clientsResponse.Data not to be empty" - SharedClient not registered</item>
///   <item>403 Forbidden - Cannot register clients on uninitialized TSS</item>
/// </list>
/// <para><strong>Solution:</strong> Verify SharedTss in appsettings.test.json:</para>
/// <code>
/// "SharedTss": {
///   "Id": "86e16afe-8776-4a9b-8e95-61024a57fa29",  // Must belong to API credentials
///   "AdminPuk": "1378377070",                      // Must be correct PUK
///   "State": "CREATED"                             // Preferred: CREATED or UNINITIALIZED
/// }
/// </code>
///
/// <para><strong>📖 Documentation:</strong></para>
/// <para>See Base/README.md for setup details and lifecycle notes.</para>
/// </remarks>
public class FiskalyClientFixture : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the AdminClient instance for testing authentication operations.
    /// </summary>
    public IAdminClient AdminClient { get; private set; } = null!;

    /// <summary>
    /// Gets the TssClient instance for testing TSS lifecycle operations.
    /// </summary>
    public ITssClient TssClient { get; private set; } = null!;

    /// <summary>
    /// Gets the ClientManagementClient instance for testing POS client registration.
    /// </summary>
    public IClientManagementClient ClientManagementClient { get; private set; } = null!;

    /// <summary>
    /// Gets the TransactionClient instance for testing transaction operations.
    /// </summary>
    public ITransactionClient TransactionClient { get; private set; } = null!;

    /// <summary>
    /// Gets the ExportClient instance for testing export operations.
    /// </summary>
    public IExportClient ExportClient { get; private set; } = null!;

    /// <summary>
    /// Gets the OrganizationClient instance for testing Management API organization operations.
    /// </summary>
    public IOrganizationClient OrganizationClient { get; private set; } = null!;

    /// <summary>
    /// Gets or sets the test output helper for logging test results.
    /// </summary>
    public ITestOutputHelper? TestOutputHelper { get; set; }

    /// <summary>
    /// Initializes the test fixture by loading "Fiskaly" configuration and creating SDK clients.
    /// </summary>
    /// <remarks>
    /// Uses "Fiskaly" section from appsettings.test.json for infrastructure tests.
    /// SDK automatically validates configuration and fails fast if invalid.
    /// </remarks>
    public Task InitializeAsync()
    {
        // Build configuration from appsettings.test.json
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.test.local.json", optional: true, reloadOnChange: false)
            .Build();

        // Set up DI container
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Register Fiskaly SDK with "Fiskaly" section (default section, explicit for clarity)
        // SDK will automatically validate configuration and fail-fast if section is missing or invalid
        services.AddFiskaly(configuration, "Fiskaly");

        _serviceProvider = services.BuildServiceProvider();

        // Resolve all SDK clients
        AdminClient = _serviceProvider.GetRequiredService<IAdminClient>();
        TssClient = _serviceProvider.GetRequiredService<ITssClient>();
        ClientManagementClient = _serviceProvider.GetRequiredService<IClientManagementClient>();
        TransactionClient = _serviceProvider.GetRequiredService<ITransactionClient>();
        ExportClient = _serviceProvider.GetRequiredService<IExportClient>();
        OrganizationClient = _serviceProvider.GetRequiredService<IOrganizationClient>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up resources after tests complete.
    /// </summary>
    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Generates a unique TSS ID for testing.
    /// </summary>
    public static string GenerateUniqueTssId() => Guid.NewGuid().ToString();

    /// <summary>
    /// Generates a unique client ID for testing.
    /// </summary>
    public static string GenerateUniqueClientId() => Guid.NewGuid().ToString();

    /// <summary>
    /// Generates a unique transaction ID for testing.
    /// </summary>
    public static string GenerateUniqueTransactionId() => Guid.NewGuid().ToString();
}
