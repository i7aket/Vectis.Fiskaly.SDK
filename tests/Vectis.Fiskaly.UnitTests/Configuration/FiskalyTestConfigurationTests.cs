using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vectis.Fiskaly.SDK.Configuration;

namespace Vectis.Fiskaly.UnitTests.Configuration;

/// <summary>
/// Unit tests for FiskalyTestConfiguration - the test infrastructure configuration class.
/// Tests configuration binding, default values, nested objects, and IOptions pattern integration.
/// </summary>
/// <remarks>
/// FiskalyTestConfiguration is a POCO used by integration tests to load test resources
/// (TSS IDs, Admin PUKs, shared credentials) from appsettings.test.json.
/// These tests ensure the configuration infrastructure works correctly for all test suites.
/// </remarks>
public class FiskalyTestConfigurationTests
{
    #region Configuration Binding Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void ConfigurationBinding_FromInMemory_BindsAllProperties()
    {
        // Arrange: Simulate appsettings.test.json structure
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "c3440422-7e08-48cb-95d9-1941938258a0",
            ["FiskalyTestResources:SharedTss:AdminPuk"] = "7593392581",
            ["FiskalyTestResources:SharedTss:State"] = "CREATED",
            ["FiskalyTestResources:SharedClientId"] = "54efc7cd-ccb8-467a-947b-9524f69cd5da",
            ["FiskalyTestResources:SharedClientSerialNumber"] = "SHARED-CLIENT-001",
            ["FiskalyTestResources:AdminPin"] = "1234567890"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert
        Assert.NotNull(testConfig);
        Assert.Equal("c3440422-7e08-48cb-95d9-1941938258a0", testConfig.SharedTss.Id);
        Assert.Equal("7593392581", testConfig.SharedTss.AdminPuk);
        Assert.Equal("CREATED", testConfig.SharedTss.State);
        Assert.Equal("54efc7cd-ccb8-467a-947b-9524f69cd5da", testConfig.SharedClientId);
        Assert.Equal("SHARED-CLIENT-001", testConfig.SharedClientSerialNumber);
        Assert.Equal("1234567890", testConfig.AdminPin);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ConfigurationBinding_MultipleTssConfigurations_BindsCorrectly()
    {
        // Arrange: All 5 TSS configurations
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "tss-shared-001",
            ["FiskalyTestResources:SharedTss:AdminPuk"] = "1111111111",
            ["FiskalyTestResources:SharedTss:State"] = "CREATED",

            ["FiskalyTestResources:TssForInitializeTest:Id"] = "tss-init-001",
            ["FiskalyTestResources:TssForInitializeTest:AdminPuk"] = "2222222222",
            ["FiskalyTestResources:TssForInitializeTest:State"] = "UNINITIALIZED",

            ["FiskalyTestResources:TssForDuplicateTest:Id"] = "tss-dup-001",
            ["FiskalyTestResources:TssForDuplicateTest:AdminPuk"] = "3333333333",
            ["FiskalyTestResources:TssForDuplicateTest:State"] = "CREATED",

            ["FiskalyTestResources:TssForCreateTest:Id"] = "tss-create-001",
            ["FiskalyTestResources:TssForCreateTest:AdminPuk"] = "4444444444",
            ["FiskalyTestResources:TssForCreateTest:State"] = "CREATED",

            ["FiskalyTestResources:Tss1:Id"] = "tss1-001",
            ["FiskalyTestResources:Tss1:AdminPuk"] = "5555555555",
            ["FiskalyTestResources:Tss1:State"] = "INITIALIZED"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: All 5 TSS configurations bound correctly
        Assert.NotNull(testConfig);

        Assert.Equal("tss-shared-001", testConfig.SharedTss.Id);
        Assert.Equal("1111111111", testConfig.SharedTss.AdminPuk);
        Assert.Equal("CREATED", testConfig.SharedTss.State);

        Assert.Equal("tss-init-001", testConfig.TssForInitializeTest.Id);
        Assert.Equal("2222222222", testConfig.TssForInitializeTest.AdminPuk);
        Assert.Equal("UNINITIALIZED", testConfig.TssForInitializeTest.State);

        Assert.Equal("tss-dup-001", testConfig.TssForDuplicateTest.Id);
        Assert.Equal("3333333333", testConfig.TssForDuplicateTest.AdminPuk);
        Assert.Equal("CREATED", testConfig.TssForDuplicateTest.State);

        Assert.Equal("tss-create-001", testConfig.TssForCreateTest.Id);
        Assert.Equal("4444444444", testConfig.TssForCreateTest.AdminPuk);
        Assert.Equal("CREATED", testConfig.TssForCreateTest.State);

        Assert.Equal("tss1-001", testConfig.Tss1.Id);
        Assert.Equal("5555555555", testConfig.Tss1.AdminPuk);
        Assert.Equal("INITIALIZED", testConfig.Tss1.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ConfigurationBinding_WithIOptionsPattern_Works()
    {
        // Arrange
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "test-tss-001",
            ["FiskalyTestResources:SharedClientId"] = "test-client-001"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.Configure<FiskalyTestConfiguration>(configuration.GetSection("FiskalyTestResources"));

        // Act
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyTestConfiguration testConfig = serviceProvider.GetRequiredService<IOptions<FiskalyTestConfiguration>>().Value;

        // Assert
        Assert.NotNull(testConfig);
        Assert.Equal("test-tss-001", testConfig.SharedTss.Id);
        Assert.Equal("test-client-001", testConfig.SharedClientId);
    }

    #endregion

    #region Default Value Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void DefaultValues_AllPropertiesInitialized()
    {
        // Arrange & Act: Create instance without configuration
        FiskalyTestConfiguration testConfig = new FiskalyTestConfiguration();

        // Assert: All properties have default values
        Assert.NotNull(testConfig.SharedTss);
        Assert.NotNull(testConfig.TssForInitializeTest);
        Assert.NotNull(testConfig.TssForDuplicateTest);
        Assert.NotNull(testConfig.TssForCreateTest);
        Assert.NotNull(testConfig.Tss1);

        Assert.Equal("shared-client-001", testConfig.SharedClientId);
        Assert.Equal("SHARED-CLIENT-001", testConfig.SharedClientSerialNumber);
        Assert.Equal("1234567890", testConfig.AdminPin);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void DefaultValues_TssConfiguration_HasExpectedDefaults()
    {
        // Arrange & Act
        TssConfiguration tssConfig = new TssConfiguration();

        // Assert
        Assert.Equal(string.Empty, tssConfig.Id);
        Assert.Equal(string.Empty, tssConfig.AdminPuk);
        Assert.Equal("UNINITIALIZED", tssConfig.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void DefaultValues_PartialConfiguration_FillsGaps()
    {
        // Arrange: Only provide SharedTss.Id, rest should use defaults
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "custom-tss-001"
            // AdminPin, SharedClientId, etc. not provided
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: Custom value used, rest are defaults
        Assert.NotNull(testConfig);
        Assert.Equal("custom-tss-001", testConfig.SharedTss.Id);
        Assert.Equal("shared-client-001", testConfig.SharedClientId); // Default
        Assert.Equal("SHARED-CLIENT-001", testConfig.SharedClientSerialNumber); // Default
        Assert.Equal("1234567890", testConfig.AdminPin); // Default
    }

    #endregion

    #region Nested Object Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void NestedObjects_TssConfiguration_BindsIndependently()
    {
        // Arrange: Each TSS has different values
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "shared-id",
            ["FiskalyTestResources:SharedTss:AdminPuk"] = "shared-puk",
            ["FiskalyTestResources:SharedTss:State"] = "CREATED",

            ["FiskalyTestResources:TssForInitializeTest:Id"] = "init-id",
            ["FiskalyTestResources:TssForInitializeTest:AdminPuk"] = "init-puk",
            ["FiskalyTestResources:TssForInitializeTest:State"] = "UNINITIALIZED"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: Each TSS configuration is independent
        Assert.NotNull(testConfig);
        Assert.Equal("shared-id", testConfig.SharedTss.Id);
        Assert.Equal("shared-puk", testConfig.SharedTss.AdminPuk);
        Assert.Equal("CREATED", testConfig.SharedTss.State);

        Assert.Equal("init-id", testConfig.TssForInitializeTest.Id);
        Assert.Equal("init-puk", testConfig.TssForInitializeTest.AdminPuk);
        Assert.Equal("UNINITIALIZED", testConfig.TssForInitializeTest.State);

        // Verify they're different objects
        Assert.NotSame(testConfig.SharedTss, testConfig.TssForInitializeTest);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NestedObjects_MissingTssConfiguration_UsesDefaultInstance()
    {
        // Arrange: Don't provide any TSS configuration
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedClientId"] = "test-client"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: TSS objects still created with defaults
        Assert.NotNull(testConfig);
        Assert.NotNull(testConfig.SharedTss);
        Assert.Equal(string.Empty, testConfig.SharedTss.Id); // Default empty
        Assert.Equal(string.Empty, testConfig.SharedTss.AdminPuk); // Default empty
        Assert.Equal("UNINITIALIZED", testConfig.SharedTss.State); // Default state
    }

    #endregion

    #region Missing Configuration Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void MissingConfiguration_EmptySection_ReturnsNull()
    {
        // Arrange: Configuration without FiskalyTestResources section
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: Returns null when section doesn't exist
        Assert.Null(testConfig);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MissingConfiguration_WrongSectionName_ReturnsNull()
    {
        // Arrange: Provide configuration but with wrong section name
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["WrongSectionName:SharedTss:Id"] = "test-id"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert
        Assert.Null(testConfig);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void MissingConfiguration_WithIOptions_ReturnsDefaultInstance()
    {
        // Arrange: Empty configuration with IOptions pattern
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.Configure<FiskalyTestConfiguration>(configuration.GetSection("FiskalyTestResources"));

        // Act
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FiskalyTestConfiguration testConfig = serviceProvider.GetRequiredService<IOptions<FiskalyTestConfiguration>>().Value;

        // Assert: IOptions returns default instance (not null)
        Assert.NotNull(testConfig);
        Assert.Equal("shared-client-001", testConfig.SharedClientId); // Has defaults
    }

    #endregion

    #region TSS State Value Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void TssState_AllValidStates_BindCorrectly()
    {
        // Arrange: Test all three valid states
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:State"] = "CREATED",
            ["FiskalyTestResources:TssForInitializeTest:State"] = "UNINITIALIZED",
            ["FiskalyTestResources:Tss1:State"] = "INITIALIZED"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: All valid states bind correctly
        Assert.NotNull(testConfig);
        Assert.Equal("CREATED", testConfig.SharedTss.State);
        Assert.Equal("UNINITIALIZED", testConfig.TssForInitializeTest.State);
        Assert.Equal("INITIALIZED", testConfig.Tss1.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TssState_InvalidState_BindsAsString()
    {
        // Arrange: Provide invalid state (configuration binding doesn't validate)
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:State"] = "INVALID_STATE"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: Invalid state binds as-is (validation happens at runtime, not binding)
        Assert.NotNull(testConfig);
        Assert.Equal("INVALID_STATE", testConfig.SharedTss.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TssState_CaseSensitive_BindsExactly()
    {
        // Arrange: Test case sensitivity
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:State"] = "created", // lowercase
            ["FiskalyTestResources:TssForInitializeTest:State"] = "CREATED" // uppercase
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: Case preserved exactly
        Assert.NotNull(testConfig);
        Assert.Equal("created", testConfig.SharedTss.State);
        Assert.Equal("CREATED", testConfig.TssForInitializeTest.State);
    }

    #endregion

    #region UUID Format Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void TssId_ValidUuid_BindsCorrectly()
    {
        // Arrange: Valid UUID format
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "c3440422-7e08-48cb-95d9-1941938258a0"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert
        Assert.NotNull(testConfig);
        Assert.Equal("c3440422-7e08-48cb-95d9-1941938258a0", testConfig.SharedTss.Id);

        // Verify it's a valid GUID format
        Assert.True(Guid.TryParse(testConfig.SharedTss.Id, out _));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TssId_InvalidUuid_BindsAsString()
    {
        // Arrange: Invalid UUID format (configuration doesn't validate format)
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "not-a-valid-uuid"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: Binds as-is, validation happens at API call time
        Assert.NotNull(testConfig);
        Assert.Equal("not-a-valid-uuid", testConfig.SharedTss.Id);
        Assert.False(Guid.TryParse(testConfig.SharedTss.Id, out _));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ClientId_ValidUuid_BindsCorrectly()
    {
        // Arrange: Valid UUID for SharedClientId
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedClientId"] = "54efc7cd-ccb8-467a-947b-9524f69cd5da"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert
        Assert.NotNull(testConfig);
        Assert.Equal("54efc7cd-ccb8-467a-947b-9524f69cd5da", testConfig.SharedClientId);
        Assert.True(Guid.TryParse(testConfig.SharedClientId, out _));
    }

    #endregion

    #region Empty and Null Value Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void EmptyValues_BindAsEmptyStrings()
    {
        // Arrange: Explicitly set empty values
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "",
            ["FiskalyTestResources:SharedTss:AdminPuk"] = "",
            ["FiskalyTestResources:SharedClientId"] = ""
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: Empty strings bind correctly
        Assert.NotNull(testConfig);
        Assert.Equal(string.Empty, testConfig.SharedTss.Id);
        Assert.Equal(string.Empty, testConfig.SharedTss.AdminPuk);
        Assert.Equal(string.Empty, testConfig.SharedClientId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void NullValues_OverrideDefaults()
    {
        // Arrange: Null values in configuration
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedClientId"] = null
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: Null values override defaults
        Assert.NotNull(testConfig);
        Assert.Null(testConfig.SharedClientId);
    }

    #endregion

    #region Real-World Configuration Test

    [Trait("Category", "Unit")]
    [Fact]
    public void RealWorld_FullConfiguration_MatchesActualUsage()
    {
        // Arrange: Simulate actual appsettings.test.json structure
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["FiskalyTestResources:SharedTss:Id"] = "c3440422-7e08-48cb-95d9-1941938258a0",
            ["FiskalyTestResources:SharedTss:AdminPuk"] = "7593392581",
            ["FiskalyTestResources:SharedTss:State"] = "CREATED",

            ["FiskalyTestResources:TssForInitializeTest:Id"] = "cb4620d2-50bf-4fdd-9414-9083e0ea139a",
            ["FiskalyTestResources:TssForInitializeTest:AdminPuk"] = "9692045775",
            ["FiskalyTestResources:TssForInitializeTest:State"] = "CREATED",

            ["FiskalyTestResources:TssForDuplicateTest:Id"] = "55f1e622-593f-4206-a238-e27842483344",
            ["FiskalyTestResources:TssForDuplicateTest:AdminPuk"] = "4550438114",
            ["FiskalyTestResources:TssForDuplicateTest:State"] = "CREATED",

            ["FiskalyTestResources:TssForCreateTest:Id"] = "22a36a24-8a33-4db4-9cef-7fc9d73235a0",
            ["FiskalyTestResources:TssForCreateTest:AdminPuk"] = "5358739292",
            ["FiskalyTestResources:TssForCreateTest:State"] = "CREATED",

            ["FiskalyTestResources:Tss1:Id"] = "0ef6005f-4a7a-456c-a26b-7f8697c8a128",
            ["FiskalyTestResources:Tss1:AdminPuk"] = "8632737286",
            ["FiskalyTestResources:Tss1:State"] = "CREATED",

            ["FiskalyTestResources:SharedClientId"] = "54efc7cd-ccb8-467a-947b-9524f69cd5da",
            ["FiskalyTestResources:SharedClientSerialNumber"] = "SHARED-CLIENT-001",
            ["FiskalyTestResources:AdminPin"] = "1234567890"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Act
        FiskalyTestConfiguration? testConfig = configuration.GetSection("FiskalyTestResources").Get<FiskalyTestConfiguration>();

        // Assert: All values match expected real-world configuration
        Assert.NotNull(testConfig);

        // Verify all 5 TSS configurations
        Assert.Equal("c3440422-7e08-48cb-95d9-1941938258a0", testConfig.SharedTss.Id);
        Assert.Equal("7593392581", testConfig.SharedTss.AdminPuk);
        Assert.Equal("CREATED", testConfig.SharedTss.State);

        Assert.Equal("cb4620d2-50bf-4fdd-9414-9083e0ea139a", testConfig.TssForInitializeTest.Id);
        Assert.Equal("9692045775", testConfig.TssForInitializeTest.AdminPuk);

        Assert.Equal("55f1e622-593f-4206-a238-e27842483344", testConfig.TssForDuplicateTest.Id);
        Assert.Equal("4550438114", testConfig.TssForDuplicateTest.AdminPuk);

        Assert.Equal("22a36a24-8a33-4db4-9cef-7fc9d73235a0", testConfig.TssForCreateTest.Id);
        Assert.Equal("5358739292", testConfig.TssForCreateTest.AdminPuk);

        Assert.Equal("0ef6005f-4a7a-456c-a26b-7f8697c8a128", testConfig.Tss1.Id);
        Assert.Equal("8632737286", testConfig.Tss1.AdminPuk);

        // Verify shared client configuration
        Assert.Equal("54efc7cd-ccb8-467a-947b-9524f69cd5da", testConfig.SharedClientId);
        Assert.Equal("SHARED-CLIENT-001", testConfig.SharedClientSerialNumber);
        Assert.Equal("1234567890", testConfig.AdminPin);

        // Verify all IDs are valid GUIDs
        Assert.True(Guid.TryParse(testConfig.SharedTss.Id, out _));
        Assert.True(Guid.TryParse(testConfig.TssForInitializeTest.Id, out _));
        Assert.True(Guid.TryParse(testConfig.TssForDuplicateTest.Id, out _));
        Assert.True(Guid.TryParse(testConfig.TssForCreateTest.Id, out _));
        Assert.True(Guid.TryParse(testConfig.Tss1.Id, out _));
        Assert.True(Guid.TryParse(testConfig.SharedClientId, out _));
    }

    #endregion

    #region Property Accessor Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void PropertyAccessors_AllTssConfigurations_AreIndependent()
    {
        // Arrange & Act
        FiskalyTestConfiguration testConfig = new FiskalyTestConfiguration();

        // Assert: All TSS configuration objects are independent instances
        Assert.NotSame(testConfig.SharedTss, testConfig.TssForInitializeTest);
        Assert.NotSame(testConfig.SharedTss, testConfig.TssForDuplicateTest);
        Assert.NotSame(testConfig.SharedTss, testConfig.TssForCreateTest);
        Assert.NotSame(testConfig.SharedTss, testConfig.Tss1);

        // Verify modifying one doesn't affect others
        testConfig.SharedTss.Id = "modified-id";
        Assert.NotEqual("modified-id", testConfig.TssForInitializeTest.Id);
        Assert.NotEqual("modified-id", testConfig.TssForDuplicateTest.Id);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PropertyAccessors_StringProperties_CanBeModified()
    {
        // Arrange
        FiskalyTestConfiguration testConfig = new FiskalyTestConfiguration();

        // Act: Modify properties
        testConfig.SharedClientId = "new-client-id";
        testConfig.SharedClientSerialNumber = "NEW-SERIAL";
        testConfig.AdminPin = "9876543210";

        // Assert: Properties are mutable
        Assert.Equal("new-client-id", testConfig.SharedClientId);
        Assert.Equal("NEW-SERIAL", testConfig.SharedClientSerialNumber);
        Assert.Equal("9876543210", testConfig.AdminPin);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PropertyAccessors_TssConfiguration_CanBeReplaced()
    {
        // Arrange
        FiskalyTestConfiguration testConfig = new FiskalyTestConfiguration();
        TssConfiguration newTssConfig = new TssConfiguration
        {
            Id = "replaced-id",
            AdminPuk = "replaced-puk",
            State = "INITIALIZED"
        };

        // Act: Replace entire TSS configuration object
        testConfig.SharedTss = newTssConfig;

        // Assert: Object replaced successfully
        Assert.Same(newTssConfig, testConfig.SharedTss);
        Assert.Equal("replaced-id", testConfig.SharedTss.Id);
        Assert.Equal("replaced-puk", testConfig.SharedTss.AdminPuk);
        Assert.Equal("INITIALIZED", testConfig.SharedTss.State);
    }

    #endregion

    #region TssConfiguration Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void TssConfiguration_CanBeCreatedDirectly()
    {
        // Arrange & Act
        TssConfiguration tssConfig = new TssConfiguration
        {
            Id = "direct-tss-id",
            AdminPuk = "1234567890",
            State = "CREATED"
        };

        // Assert
        Assert.Equal("direct-tss-id", tssConfig.Id);
        Assert.Equal("1234567890", tssConfig.AdminPuk);
        Assert.Equal("CREATED", tssConfig.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TssConfiguration_PropertiesAreMutable()
    {
        // Arrange
        TssConfiguration tssConfig = new TssConfiguration();

        // Act: Modify all properties
        tssConfig.Id = "modified-id";
        tssConfig.AdminPuk = "modified-puk";
        tssConfig.State = "MODIFIED";

        // Assert: All properties mutable
        Assert.Equal("modified-id", tssConfig.Id);
        Assert.Equal("modified-puk", tssConfig.AdminPuk);
        Assert.Equal("MODIFIED", tssConfig.State);
    }

    #endregion
}
