using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vectis.Fiskaly.SDK;
using Vectis.Fiskaly.SDK.SignDE.Admin;
using Vectis.Fiskaly.SDK.SignDE.Clients;
using Vectis.Fiskaly.SDK.SignDE.Exports;
using Vectis.Fiskaly.SDK.SignDE.Transactions;
using Vectis.Fiskaly.SDK.SignDE.Tss;

namespace Vectis.Fiskaly.IntegrationTests.Base;

/// <summary>
/// Test fixture for base integration tests with dedicated API credentials.
/// </summary>
/// <remarks>
/// <para><strong>Purpose</strong>:</para>
/// Provides SDK clients with dedicated API credentials for base integration tests,
/// isolated from legacy tests to prevent TSS limit conflicts and resource sharing issues.
///
/// <para><strong>Key Differences from FiskalyClientFixture</strong>:</para>
/// <list type="bullet">
///   <item><description>Uses separate credentials from "FiskalyBase" section in appsettings.test.json</description></item>
///   <item><description>NO auto-setup of TSS or Client (handled by FiskalyIntegrationTestBase)</description></item>
///   <item><description>Minimal fixture - only provides SDK clients</description></item>
///   <item><description>No shared resources - each test class creates its own TSS + Client</description></item>
/// </list>
///
/// <para><strong>Configuration Example</strong> (appsettings.test.json):</para>
/// <code>
/// {
///   "FiskalyBase": {
///     "ApiKey": "test_xxxxx_test",
///     "ApiSecret": "your-secret-key",
///     "BaseUrl": "https://kassensichv-middleware.fiskaly.com/api/v2/"
///   }
/// }
/// </code>
///
/// <para><strong>Usage Pattern</strong>:</para>
/// <code>
/// public class MyTests : FiskalyIntegrationTestBase
/// {
///     public MyTests(FiskalyBaseTestFixture fixture) : base(fixture) { }
///
///     // TssId and ClientId automatically created by base class
///     // Automatic cleanup after all tests complete
/// }
/// </code>
/// </remarks>
public class FiskalyBaseTestFixture : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the AdminClient instance for authentication operations.
    /// </summary>
    public IAdminClient AdminClient { get; private set; } = null!;

    /// <summary>
    /// Gets the TssClient instance for TSS lifecycle operations.
    /// </summary>
    public ITssClient TssClient { get; private set; } = null!;

    /// <summary>
    /// Gets the ClientManagementClient instance for POS client registration.
    /// </summary>
    public IClientManagementClient ClientManagementClient { get; private set; } = null!;

    /// <summary>
    /// Gets the TransactionClient instance for transaction operations.
    /// </summary>
    public ITransactionClient TransactionClient { get; private set; } = null!;

    /// <summary>
    /// Gets the ExportClient instance for export operations.
    /// </summary>
    public IExportClient ExportClient { get; private set; } = null!;

    /// <summary>
    /// Initializes the fixture by loading "FiskalyBase" configuration and creating SDK clients.
    /// </summary>
    /// <remarks>
    /// Uses dedicated API credentials from "FiskalyBase" section in appsettings.test.json,
    /// isolated from other test fixtures to prevent resource conflicts.
    /// SDK automatically validates configuration and fails fast if invalid.
    /// </remarks>
    public Task InitializeAsync()
    {
        // Load configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.test.local.json", optional: true, reloadOnChange: false)
            .Build();

        // Set up DI container
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Register Fiskaly SDK with "FiskalyBase" section
        // SDK will automatically validate configuration and fail-fast if section is missing or invalid
        services.AddFiskaly(configuration, "FiskalyBase");

        _serviceProvider = services.BuildServiceProvider();

        // Resolve all SDK clients
        AdminClient = _serviceProvider.GetRequiredService<IAdminClient>();
        TssClient = _serviceProvider.GetRequiredService<ITssClient>();
        ClientManagementClient = _serviceProvider.GetRequiredService<IClientManagementClient>();
        TransactionClient = _serviceProvider.GetRequiredService<ITransactionClient>();
        ExportClient = _serviceProvider.GetRequiredService<IExportClient>();

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
}
