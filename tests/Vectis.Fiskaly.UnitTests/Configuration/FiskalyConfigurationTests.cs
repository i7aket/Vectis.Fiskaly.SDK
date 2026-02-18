using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vectis.Fiskaly.SDK;
using Vectis.Fiskaly.SDK.Configuration;
using Vectis.Fiskaly.SDK.SignDE.Admin;
using Vectis.Fiskaly.SDK.SignDE.Clients;
using Vectis.Fiskaly.SDK.SignDE.Exports;
using Vectis.Fiskaly.SDK.SignDE.Transactions;
using Vectis.Fiskaly.SDK.SignDE.Tss;

namespace Vectis.Fiskaly.UnitTests.Configuration;

/// <summary>
/// Valid 43-character API secret for testing (matches validator regex: ^[0-9A-Za-z]{43}$)
/// </summary>
file static class TestConstants
{
    public const string ValidApiSecret = "1234567890123456789012345678901234567890123";  // Exactly 43 chars
}

/// <summary>
/// Unit tests for Fiskaly SDK configuration system.
/// Tests code-only configuration, appsettings.json binding, programmatic overrides,
/// extension methods, validation, and per-client configuration independence.
/// </summary>
/// <remarks>
/// These tests verify Phase 6 requirements from fiskaly-resilience-api-refactoring-plan.md:
/// - Task 6.1: SDK works without appsettings.json
/// - Task 6.2: Programmatic override works
/// - Task 6.3: appsettings.json + programmatic override (configuration priority)
/// - Task 6.4: DisableResilience() extension method works
/// - Task 6.5: UseTestDefaults() extension method works
/// - Additional: UseProductionDefaults() and UseHighResilience() work
/// - Validation requires RetryCount between 1 and 10
/// - Per-client configuration independence
/// </remarks>
public class FiskalyConfigurationTests
{
    #region Task 6.1: Code-Only Configuration (Works without appsettings.json)

    [Trait("Category", "Unit")]
    [Fact]
    public void CodeOnly_Configuration_Works()
    {
        // Arrange: Empty configuration (no appsettings.json)
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Register Fiskaly with only programmatic configuration
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Assert: Configuration is resolved and has programmatic values
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;
        Assert.Equal("test_key", config.ApiKey);
        Assert.Equal(TestConstants.ValidApiSecret, config.ApiSecret);
        Assert.Equal("https://kassensichv-middleware.fiskaly.com/api/v2/", config.BaseUrl); // Default
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CodeOnly_CanResolveAllClients()
    {
        // Arrange: No appsettings.json, only programmatic configuration
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act & Assert: All 5 clients can be resolved
        Assert.NotNull(serviceProvider.GetRequiredService<IAdminClient>());
        Assert.NotNull(serviceProvider.GetRequiredService<ITssClient>());
        Assert.NotNull(serviceProvider.GetRequiredService<IClientManagementClient>());
        Assert.NotNull(serviceProvider.GetRequiredService<ITransactionClient>());
        Assert.NotNull(serviceProvider.GetRequiredService<IExportClient>());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CodeOnly_UsesDefaultBaseUrl()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Don't specify BaseUrl
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert: Uses production default
        Assert.Equal("https://kassensichv-middleware.fiskaly.com/api/v2/", config.BaseUrl);
    }

    #endregion

    #region Task 6.2: Programmatic Override Works

    [Trait("Category", "Unit")]
    [Fact]
    public void ProgrammaticOverride_OverridesDefaults()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Override default TransactionClient timeout (default is 45s)
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.TransactionClient.TimeoutSeconds = 99;
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert: Override took effect
        Assert.Equal(99, config.TransactionClient.TimeoutSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ProgrammaticOverride_CanSetBaseUrl()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.BaseUrl = "https://custom.fiskaly.com/api/v2/";
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert
        Assert.Equal("https://custom.fiskaly.com/api/v2/", config.BaseUrl);
    }

    #endregion

    #region Task 6.3: Configuration Priority (appsettings.json + Programmatic Override)

    [Trait("Category", "Unit")]
    [Fact]
    public void ConfigurationPriority_ProgrammaticWins()
    {
        // Arrange: Simulate appsettings.json with TimeoutSeconds = 10
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["Fiskaly:ApiKey"] = "appsettings_key",
            ["Fiskaly:ApiSecret"] = TestConstants.ValidApiSecret,
            ["Fiskaly:TransactionClient:TimeoutSeconds"] = "10"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Programmatic configuration overrides appsettings.json
        services.AddFiskaly(configuration, configure: options =>
        {
            options.TransactionClient.TimeoutSeconds = 20; // Override appsettings value (10)
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert: Programmatic wins over appsettings.json
        Assert.Equal("appsettings_key", config.ApiKey); // From appsettings
        Assert.Equal(TestConstants.ValidApiSecret, config.ApiSecret); // From appsettings
        Assert.Equal(20, config.TransactionClient.TimeoutSeconds); // Programmatic override
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ConfigurationPriority_AppsettingsWinsOverDefaults()
    {
        // Arrange: Simulate appsettings.json
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["Fiskaly:ApiKey"] = "appsettings_key",
            ["Fiskaly:ApiSecret"] = TestConstants.ValidApiSecret,
            ["Fiskaly:TransactionClient:TimeoutSeconds"] = "99"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Don't provide programmatic override
        services.AddFiskaly(configuration);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert: appsettings.json wins over defaults
        Assert.Equal(99, config.TransactionClient.TimeoutSeconds); // appsettings value (not default 45)
    }

    #endregion

    #region Task 6.4: DisableResilience() Extension Method

    [Trait("Category", "Unit")]
    [Fact]
    public void DisableResilience_SetsRetryAndCircuitBreakerToZero()
    {
        // Arrange: Start with default resilience settings
        FiskalyClientConfiguration config = new FiskalyClientConfiguration
        {
            RetryCount = 3,
            CircuitBreakerThreshold = 5
        };

        // Act
        config.DisableResilience();

        // Assert: Resilience disabled while preserving retry count
        Assert.False(config.ResilienceEnabled);
        Assert.Equal(3, config.RetryCount);
        Assert.Equal(0, config.CircuitBreakerThreshold);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void DisableResilience_WorksInProgrammaticConfiguration()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.TransactionClient.DisableResilience();
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert
        Assert.False(config.TransactionClient.ResilienceEnabled);
        Assert.Equal(5, config.TransactionClient.RetryCount);
        Assert.Equal(0, config.TransactionClient.CircuitBreakerThreshold);
    }

    #endregion

    #region Task 6.5: UseTestDefaults() Extension Method

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_AppliesTestSettings()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseTestDefaults();

        // Assert: Test settings (fast-fail, low resilience)
        Assert.Equal(5, config.TimeoutSeconds);
        Assert.Equal(1, config.RetryCount);
        // CategoryDelays uses defaults (Transient=1s, Infrastructure=5s, Auth=2s)
        Assert.Equal(0, config.CircuitBreakerThreshold);
        Assert.Equal(10, config.CircuitBreakerDurationSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_WorksInProgrammaticConfiguration()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.AdminClient.UseTestDefaults();
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert
        Assert.Equal(5, config.AdminClient.TimeoutSeconds);
        Assert.Equal(1, config.AdminClient.RetryCount);
    }

    #endregion

    #region Extension Method Tests: UseProductionDefaults() and UseHighResilience()

    [Trait("Category", "Unit")]
    [Fact]
    public void UseProductionDefaults_AppliesProductionSettings()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseProductionDefaults();

        // Assert: Production settings (balanced)
        Assert.Equal(30, config.TimeoutSeconds);
        Assert.Equal(3, config.RetryCount);
        // CategoryDelays uses defaults (Transient=1s, Infrastructure=5s, Auth=2s)
        Assert.Equal(5, config.CircuitBreakerThreshold);
        Assert.Equal(30, config.CircuitBreakerDurationSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_AppliesHighResilienceSettings()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseHighResilience();

        // Assert: High resilience settings (aggressive)
        Assert.Equal(60, config.TimeoutSeconds);
        Assert.Equal(10, config.RetryCount);
        Assert.Equal(15, config.CircuitBreakerThreshold);
        Assert.Equal(60, config.CircuitBreakerDurationSeconds);
        // CategoryDelays with longer delays for high resilience
        Assert.Equal(2, config.CategoryDelays.TransientDelaySeconds);
        Assert.Equal(10, config.CategoryDelays.InfrastructureDelaySeconds);
        Assert.Equal(5, config.CategoryDelays.AuthenticationDelaySeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_WorksForMissionCriticalClients()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Apply high resilience to TransactionClient
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.TransactionClient.UseHighResilience();
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert: TransactionClient has aggressive retry settings
        Assert.Equal(10, config.TransactionClient.RetryCount);
        Assert.Equal(15, config.TransactionClient.CircuitBreakerThreshold);
    }

    #endregion

    #region Validation Tests: Resilience Enabled / Disabled

    [Trait("Category", "Unit")]
    [Fact]
    public void Validation_RejectsRetryCountZero()
    {
        // Arrange: Configuration with RetryCount=0
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.TransactionClient.RetryCount = 0;
        });

        // Act & Assert: Validation should fail
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value
        );

        Assert.Contains("Retry count must be between 1 and 10", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validation_AllowsCircuitBreakerThresholdZero()
    {
        // Arrange: Configuration with CircuitBreakerThreshold=0 (resilience disabled)
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.TransactionClient.CircuitBreakerThreshold = 0; // Disabled
        });

        // Act & Assert: No validation exception should be thrown
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;
        Assert.Equal(0, config.TransactionClient.CircuitBreakerThreshold);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validation_RejectsInvalidRetryCount()
    {
        // Arrange: Configuration with invalid RetryCount
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.TransactionClient.RetryCount = -1; // Invalid
        });

        // Act & Assert: Validation should fail on BuildServiceProvider
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value
        );

        Assert.Contains("Retry count must be between 1 and 10", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validation_RejectsInvalidCircuitBreakerThreshold()
    {
        // Arrange: Configuration with invalid CircuitBreakerThreshold
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
            options.TransactionClient.CircuitBreakerThreshold = -1; // Invalid
        });

        // Act & Assert: Validation should fail
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value
        );

        Assert.Contains("Circuit breaker threshold must be >= 0", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validation_RejectsMissingApiKey()
    {
        // Arrange: Configuration with empty ApiKey
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = ""; // Missing
            options.ApiSecret = TestConstants.ValidApiSecret;
        });

        // Act & Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value
        );

        Assert.Contains("Fiskaly API Key is required", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validation_RejectsMissingApiSecret()
    {
        // Arrange: Configuration with empty ApiSecret
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = ""; // Missing
        });

        // Act & Assert
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value
        );

        Assert.Contains("Fiskaly API Secret is required", exception.Message);
    }

    #endregion

    #region Per-Client Configuration Independence Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void PerClient_Configuration_Independent()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Configure different settings for different clients
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;

            options.AdminClient.TimeoutSeconds = 10;
            options.TssClient.TimeoutSeconds = 20;
            options.TransactionClient.TimeoutSeconds = 30;
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert: Each client has independent settings
        Assert.Equal(10, config.AdminClient.TimeoutSeconds);
        Assert.Equal(20, config.TssClient.TimeoutSeconds);
        Assert.Equal(30, config.TransactionClient.TimeoutSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PerClient_TransactionClient_HasHigherDefaults()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Use defaults (no overrides)
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert: TransactionClient has more aggressive defaults (mission-critical)
        Assert.Equal(45, config.TransactionClient.TimeoutSeconds); // Higher than default 30s
        Assert.Equal(5, config.TransactionClient.RetryCount); // Higher than default 3
        Assert.Equal(10, config.TransactionClient.CircuitBreakerThreshold); // Higher than default 5

        // Compare with AdminClient (standard defaults)
        Assert.Equal(10, config.AdminClient.TimeoutSeconds);
        Assert.Equal(1, config.AdminClient.RetryCount);
        Assert.Equal(5, config.AdminClient.CircuitBreakerThreshold);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PerClient_CanMixExtensionMethods()
    {
        // Arrange
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();

        // Act: Apply different extension methods to different clients
        services.AddFiskaly(configuration, configure: options =>
        {
            options.ApiKey = "test_key";
            options.ApiSecret = TestConstants.ValidApiSecret;

            options.AdminClient.UseTestDefaults(); // Fast-fail for admin ops
            options.TransactionClient.UseHighResilience(); // Aggressive for transactions
            options.ExportClient.UseProductionDefaults(); // Balanced for exports
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyConfiguration config = serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value;

        // Assert: Each client has correct preset
        Assert.Equal(5, config.AdminClient.TimeoutSeconds); // Test defaults
        Assert.Equal(60, config.TransactionClient.TimeoutSeconds); // High resilience
        Assert.Equal(30, config.ExportClient.TimeoutSeconds); // Production defaults
    }

    #endregion
}
