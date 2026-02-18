using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vectis.Fiskaly.SDK;
using Vectis.Fiskaly.SDK.Configuration;

namespace Vectis.Fiskaly.UnitTests.Configuration;

/// <summary>
/// Tests for enhanced credential validation (Recommendation #2 from Mews analysis).
/// </summary>
/// <remarks>
/// Tests validation improvements from vectis-sdk-deep-analysis-from-mews.md:
/// - ApiSecret: Exact 43 alphanumeric characters (Mews gold standard)
/// - ApiKey: Length 1-512, at least one non-whitespace
/// - BaseUrl: Must end with trailing slash
/// </remarks>
public class FiskalyConfigurationValidatorEnhancedTests
{
    private readonly FiskalyConfigurationValidator _validator = new();

    /// <summary>
    /// Creates a valid FiskalyConfiguration with all required nested client configurations.
    /// Default client configs use the same defaults as FiskalyConfiguration class.
    /// </summary>
    private static FiskalyConfiguration CreateValidConfiguration()
    {
        return new FiskalyConfiguration
        {
            ApiKey = "test_key_valid",
            ApiSecret = "abcdefghijklmnopqrstuvwxyz01234567890ABCDEF", // Exactly 43 chars: 26+11+6=43
            // Use default nested client configurations (they're initialized automatically)
        };
    }

    #region ApiSecret Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ValidApiSecret_Succeeds()
    {
        // Arrange: Valid 43-char alphanumeric secret
        FiskalyConfiguration config = CreateValidConfiguration();

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        if (result.Failed)
        {
            string failureMessages = string.Join("; ", result.Failures);
            Assert.Fail($"Validation failed unexpectedly. Errors: {failureMessages}");
        }
        Assert.True(result.Succeeded);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("too_short", "Current length: 9")]
    [InlineData("this_is_44_characters_long_and_should_fail!!", "Current length: 44")]
    [InlineData("invalid-characters-here-with-dashes!!!", "43 alphanumeric characters")]
    [InlineData("has spaces in the middle of secret key!!", "43 alphanumeric characters")]
    [InlineData("special_chars_not_allowed_!!!!!!!!!!!!!", "43 alphanumeric characters")]
    public void Validate_InvalidApiSecret_Fails(string invalidSecret, string expectedErrorPart)
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = invalidSecret;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains(expectedErrorPart));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_EmptyApiSecret_ShowsRequiredError()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = "";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("Fiskaly API Secret is required"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiSecret42Chars_Fails()
    {
        // Arrange: One character short
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = new string('a', 42);

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("exactly 43 alphanumeric characters"));
        Assert.Contains(result.Failures, f => f.Contains("Current length: 42"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiSecret44Chars_Fails()
    {
        // Arrange: One character too long
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = new string('a', 44);

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("exactly 43 alphanumeric characters"));
        Assert.Contains(result.Failures, f => f.Contains("Current length: 44"));
    }

    #endregion

    #region ApiKey Validation Tests

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("test_k")] // Min length 6 (MinimumLength constant)
    [InlineData("a")] // Length 1 (validator allows 1-512)
    [InlineData("test_key_normal_length")]
    public void Validate_ValidApiKey_Succeeds(string validKey)
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = validKey;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyMaxLength_Succeeds()
    {
        // Arrange: Exactly 512 characters
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = new string('x', 512);

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("", "API Key is required")]
    [InlineData("   ", "API Key is required")] // Whitespace-only triggers IsNullOrWhiteSpace check
    public void Validate_InvalidApiKey_Fails(string invalidKey, string expectedErrorPart)
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = invalidKey;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains(expectedErrorPart));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyTooLong_Fails()
    {
        // Arrange: 513 characters (over max 512)
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = new string('x', 513);

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("must be between 1 and 512"));
        Assert.Contains(result.Failures, f => f.Contains("Current length: 513"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyOnlyWhitespace_Fails()
    {
        // Arrange: Only spaces (10 characters)
        // Note: IsNullOrWhiteSpace check happens first in validator
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = new string(' ', 10);

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("API Key is required"));
    }

    #endregion

    #region BaseUrl Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlWithTrailingSlash_Succeeds()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "https://kassensichv-middleware.fiskaly.com/api/v2/";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlWithoutTrailingSlash_Fails()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "https://kassensichv-middleware.fiskaly.com/api/v2"; // Missing trailing slash

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("must end with trailing slash"));
        Assert.Contains(result.Failures, f => f.Contains("/api/v2"));
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("https://example.com/", true, null)]  // Valid HTTPS with trailing slash
    [InlineData("http://localhost:8080/", true, null)]  // HTTP loopback allowed for testing
    [InlineData("https://example.com", false, "trailing slash")]  // HTTPS without trailing slash
    [InlineData("http://localhost:8080", false, "trailing slash")]  // Loopback HTTP allowed, but must still end with slash
    public void Validate_BaseUrlTrailingSlash_ValidatesCorrectly(string baseUrl, bool shouldSucceed, string? expectedErrorPart)
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = baseUrl;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        if (shouldSucceed)
        {
            Assert.True(result.Succeeded, $"Expected success for BaseUrl: {baseUrl}");
        }
        else
        {
            Assert.True(result.Failed, $"Expected failure for BaseUrl: {baseUrl}");
            Assert.Contains(result.Failures, f => f.Contains(expectedErrorPart!));
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_InvalidBaseUrl_FailsBeforeTrailingSlashCheck()
    {
        // Arrange: Invalid URL (not HTTP/HTTPS)
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "ftp://invalid.com/";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("valid HTTPS URL"));
        Assert.DoesNotContain(result.Failures, f => f.Contains("trailing slash"));
    }

    #endregion

    #region Integration Test: Startup Validation

    [Trait("Category", "Unit")]
    [Fact]
    public void Startup_InvalidApiSecret_ThrowsOptionsValidationException()
    {
        // Arrange: Simulate invalid appsettings.json
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["Fiskaly:ApiKey"] = "test_key",
            ["Fiskaly:ApiSecret"] = "INVALID_TOO_SHORT" // Not 43 chars
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddFiskaly(configuration);

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act & Assert: Accessing configuration should throw
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value
        );

        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Startup_ApiKeyTooLong_ThrowsOptionsValidationException()
    {
        // Arrange: API Key over 512 chars
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["Fiskaly:ApiKey"] = new string('x', 513),
            ["Fiskaly:ApiSecret"] = "abcdefghijklmnopqrstuvwxyz01234567890ABCDEF"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddFiskaly(configuration);

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value
        );

        Assert.Contains("must be between 1 and 512", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Startup_BaseUrlWithoutTrailingSlash_ThrowsOptionsValidationException()
    {
        // Arrange
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            ["Fiskaly:ApiKey"] = "test_key_valid",
            ["Fiskaly:ApiSecret"] = "abcdefghijklmnopqrstuvwxyz01234567890ABCDEF",
            ["Fiskaly:BaseUrl"] = "https://kassensichv-middleware.fiskaly.com/api/v2" // Missing slash
        };

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddFiskaly(configuration);

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider.GetRequiredService<IOptions<FiskalyConfiguration>>().Value
        );

        Assert.Contains("must end with trailing slash", exception.Message);
    }

    #endregion
}
