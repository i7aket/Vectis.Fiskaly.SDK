using Microsoft.Extensions.Options;
using Vectis.Fiskaly.SDK.Configuration;

namespace Vectis.Fiskaly.UnitTests.Configuration;

/// <summary>
/// Unit tests for <see cref="FiskalyConfigurationValidator"/>.
/// Validates configuration settings for Fiskaly Cloud TSE integration.
/// </summary>
public class FiskalyConfigurationValidatorTests
{
    private readonly FiskalyConfigurationValidator _validator = new();

    #region ApiKey Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiKey is null");
        Assert.Contains("Fiskaly API Key is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyIsEmpty_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = string.Empty;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiKey is empty");
        Assert.Contains("Fiskaly API Key is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyIsWhitespace_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = "   ";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiKey is only whitespace");
        Assert.Contains("Fiskaly API Key is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyTooLong_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = new string('a', 513); // 513 characters (max is 512)

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiKey exceeds 512 characters");
        Assert.Contains("API Key length must be between 1 and 512 characters", result.FailureMessage);
        Assert.Contains("Current length: 513", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyValid_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = "test_valid_api_key_12345";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed with valid ApiKey. Errors: {result.FailureMessage}");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiKeyMaxLength_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = new string('a', 512); // Exactly 512 characters

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed with 512-character ApiKey. Errors: {result.FailureMessage}");
    }

    #endregion

    #region ApiSecret Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiSecretIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiSecret is null");
        Assert.Contains("Fiskaly API Secret is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiSecretIsEmpty_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = string.Empty;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiSecret is empty");
        Assert.Contains("Fiskaly API Secret is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiSecretTooShort_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = "test12345678901234567890123456789012345678"; // 42 characters (test=4 + 38 digits)

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiSecret is not exactly 43 characters");
        Assert.Contains("API Secret must be exactly 43 alphanumeric characters", result.FailureMessage);
        Assert.Contains("Current length: 42", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiSecretTooLong_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = "test1234567890123456789012345678901234567890"; // 44 characters (test=4 + 40 digits)

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiSecret is not exactly 43 characters");
        Assert.Contains("API Secret must be exactly 43 alphanumeric characters", result.FailureMessage);
        Assert.Contains("Current length: 44", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiSecretWithSpecialCharacters_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = "test_1234567890123456789012345678901234567"; // 43 chars but contains underscore

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ApiSecret contains non-alphanumeric characters");
        Assert.Contains("API Secret must be exactly 43 alphanumeric characters", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ApiSecretValid_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiSecret = "test123456789012345678901234567890123456789"; // Exactly 43 alphanumeric

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed with valid ApiSecret. Errors: {result.FailureMessage}");
    }

    #endregion

    #region BaseUrl Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when BaseUrl is null");
        Assert.Contains("Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlIsEmpty_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = string.Empty;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when BaseUrl is empty");
        Assert.Contains("Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlInvalidFormat_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "not-a-valid-url";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when BaseUrl is not a valid URL");
        Assert.Contains("Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlWithFtpProtocol_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "ftp://example.com/";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when BaseUrl uses FTP protocol");
        Assert.Contains("Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlWithoutTrailingSlash_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "https://api.example.com";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when BaseUrl does not end with trailing slash");
        Assert.Contains("Base URL must end with trailing slash", result.FailureMessage);
        Assert.Contains("Add '/' to the end", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlHttpWithTrailingSlash_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "http://api.example.com/";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when BaseUrl uses HTTP (only HTTPS allowed for production endpoints)");
        Assert.Contains("Base URL must be a valid HTTPS URL", result.FailureMessage);
        Assert.Contains("HTTP allowed only for localhost/testing", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlPrivateLanHttpWithoutFlag_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "http://192.168.1.10:5020/api/v2/";
        config.AllowHttpForPrivateNetworks = false;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when private LAN HTTP is disabled.");
        Assert.Contains("Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlPrivateLanHttpWithFlag_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "http://192.168.1.10:5020/api/v2/";
        config.AllowHttpForPrivateNetworks = true;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed when private LAN HTTP is enabled. Errors: {result.FailureMessage}");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_BaseUrlHttpsWithTrailingSlash_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.BaseUrl = "https://api.example.com/v2/";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed with valid HTTPS BaseUrl. Errors: {result.FailureMessage}");
    }

    #endregion

    #region ManagementBaseUrl Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ManagementBaseUrlIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ManagementBaseUrl = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ManagementBaseUrl is null");
        Assert.Contains("Management Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ManagementBaseUrlIsEmpty_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ManagementBaseUrl = string.Empty;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ManagementBaseUrl is empty");
        Assert.Contains("Management Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ManagementBaseUrlInvalidFormat_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ManagementBaseUrl = "not-a-valid-url";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ManagementBaseUrl is not a valid URL");
        Assert.Contains("Management Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ManagementBaseUrlWithFtpProtocol_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ManagementBaseUrl = "ftp://example.com/";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ManagementBaseUrl uses FTP protocol");
        Assert.Contains("Management Base URL must be a valid HTTPS URL", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ManagementBaseUrlWithoutTrailingSlash_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ManagementBaseUrl = "https://dashboard.example.com";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ManagementBaseUrl does not end with trailing slash");
        Assert.Contains("Management Base URL must end with trailing slash", result.FailureMessage);
        Assert.Contains("Add '/' to the end", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ManagementBaseUrlHttpWithTrailingSlash_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ManagementBaseUrl = "http://dashboard.example.com/";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ManagementBaseUrl uses HTTP (only HTTPS allowed for production endpoints)");
        Assert.Contains("Management Base URL must be a valid HTTPS URL", result.FailureMessage);
        Assert.Contains("HTTP allowed only for localhost/testing", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ManagementBaseUrlPrivateLanHttpWithFlag_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ManagementBaseUrl = "http://192.168.1.10:5020/api/v0/";
        config.AllowHttpForPrivateNetworks = true;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed when private LAN HTTP is enabled. Errors: {result.FailureMessage}");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ManagementBaseUrlHttpsWithTrailingSlash_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ManagementBaseUrl = "https://dashboard.fiskaly.com/api/v0/";

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed with valid HTTPS ManagementBaseUrl. Errors: {result.FailureMessage}");
    }

    #endregion

    #region Client Configuration Validation Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_AdminClientIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.AdminClient = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when AdminClient is null");
        Assert.Contains("AdminClient configuration is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_TssClientIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.TssClient = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when TssClient is null");
        Assert.Contains("TssClient configuration is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_TransactionClientIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.TransactionClient = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when TransactionClient is null");
        Assert.Contains("TransactionClient configuration is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ExportClientIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ExportClient = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ExportClient is null");
        Assert.Contains("ExportClient configuration is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientManagementClientIsNull_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ClientManagementClient = null!;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when ClientManagementClient is null");
        Assert.Contains("ClientManagementClient configuration is required", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientTimeoutSecondsZero_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.AdminClient.TimeoutSeconds = 0;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when TimeoutSeconds is zero");
        Assert.Contains("AdminClient: Timeout must be greater than 0 seconds", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientTimeoutSecondsNegative_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.TssClient.TimeoutSeconds = -5;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when TimeoutSeconds is negative");
        Assert.Contains("TssClient: Timeout must be greater than 0 seconds", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientRetryCountNegative_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.TransactionClient.RetryCount = -1;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when RetryCount is negative");
        Assert.Contains("TransactionClient: Retry count must be between 1 and 10", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientRetryCountTooHigh_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ExportClient.RetryCount = 11;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when RetryCount exceeds 10");
        Assert.Contains("ExportClient: Retry count must be between 1 and 10", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientRetryCountZero_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.AdminClient.RetryCount = 0;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when RetryCount is zero");
        Assert.Contains("AdminClient: Retry count must be between 1 and 10", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientTransientDelayZero_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.TssClient.CategoryDelays.TransientDelaySeconds = 0;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when TransientDelaySeconds is zero");
        Assert.Contains("TssClient: CategoryDelays.TransientDelaySeconds must be greater than 0 seconds", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientInfrastructureDelayNegative_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ClientManagementClient.CategoryDelays.InfrastructureDelaySeconds = -2;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when InfrastructureDelaySeconds is negative");
        Assert.Contains("ClientManagementClient: CategoryDelays.InfrastructureDelaySeconds must be greater than 0 seconds", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientAuthenticationDelayZero_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.AdminClient.CategoryDelays.AuthenticationDelaySeconds = 0;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when AuthenticationDelaySeconds is zero");
        Assert.Contains("AdminClient: CategoryDelays.AuthenticationDelaySeconds must be greater than 0 seconds", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientCircuitBreakerThresholdNegative_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.TransactionClient.CircuitBreakerThreshold = -1;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when CircuitBreakerThreshold is negative");
        Assert.Contains("TransactionClient: Circuit breaker threshold must be >= 0", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientCircuitBreakerThresholdZero_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.TssClient.CircuitBreakerThreshold = 0; // Valid: 0 = disabled

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed when CircuitBreakerThreshold is 0 (disabled). Errors: {result.FailureMessage}");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientCircuitBreakerDurationZero_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ExportClient.CircuitBreakerDurationSeconds = 0;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when CircuitBreakerDurationSeconds is zero");
        Assert.Contains("ExportClient: Circuit breaker duration must be greater than 0 seconds", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_ClientCircuitBreakerDurationNegative_ReturnsFailure()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.AdminClient.CircuitBreakerDurationSeconds = -10;

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail when CircuitBreakerDurationSeconds is negative");
        Assert.Contains("AdminClient: Circuit breaker duration must be greater than 0 seconds", result.FailureMessage);
    }

    #endregion

    #region Integration Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_AllValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed with all valid configuration. Errors: {result.FailureMessage}");
        Assert.True(result.Succeeded, "Validation result should be marked as succeeded");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.ApiKey = string.Empty; // Error 1
        config.ApiSecret = "too_short"; // Error 2
        config.BaseUrl = "invalid-url"; // Error 3
        config.AdminClient.TimeoutSeconds = 0; // Error 4
        config.TssClient.RetryCount = -1; // Error 5

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.True(result.Failed, "Validation should fail with multiple errors");
        Assert.Contains("API Key is required", result.FailureMessage);
        Assert.Contains("API Secret must be exactly 43 alphanumeric characters", result.FailureMessage);
        Assert.Contains("Base URL must be a valid HTTPS URL", result.FailureMessage);
        Assert.Contains("AdminClient: Timeout must be greater than 0 seconds", result.FailureMessage);
        Assert.Contains("TssClient: Retry count must be between 1 and 10", result.FailureMessage);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Validate_FallbackDirectoryEmpty_ReturnsSuccess()
    {
        // Arrange
        FiskalyConfiguration config = CreateValidConfiguration();
        config.FallbackDirectory = string.Empty; // FallbackDirectory is optional

        // Act
        ValidateOptionsResult result = _validator.Validate(null, config);

        // Assert
        Assert.False(result.Failed, $"Validation should succeed when FallbackDirectory is empty (optional field). Errors: {result.FailureMessage}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a valid configuration for testing.
    /// Individual tests modify specific properties to test validation failures.
    /// </summary>
    private static FiskalyConfiguration CreateValidConfiguration()
    {
        return new FiskalyConfiguration
        {
            ApiKey = "test_valid_api_key_1234567890",
            ApiSecret = "test123456789012345678901234567890123456789", // Exactly 43 alphanumeric (test + 39 digits)
            BaseUrl = "https://kassensichv-middleware.fiskaly.com/api/v2/",
            FallbackDirectory = "/tmp/fiskaly-fallback",
            AdminClient = new FiskalyClientConfiguration
            {
                TimeoutSeconds = 10,
                RetryCount = 1,
                CircuitBreakerThreshold = 5,
                CircuitBreakerDurationSeconds = 30,
                CategoryDelays = new CategoryRetryDelays
                {
                    TransientDelaySeconds = 1,
                    InfrastructureDelaySeconds = 5,
                    AuthenticationDelaySeconds = 2
                }
            },
            TssClient = new FiskalyClientConfiguration
            {
                TimeoutSeconds = 30,
                RetryCount = 3,
                CircuitBreakerThreshold = 5,
                CircuitBreakerDurationSeconds = 60,
                CategoryDelays = new CategoryRetryDelays
                {
                    TransientDelaySeconds = 1,
                    InfrastructureDelaySeconds = 5,
                    AuthenticationDelaySeconds = 2
                }
            },
            TransactionClient = new FiskalyClientConfiguration
            {
                TimeoutSeconds = 45,
                RetryCount = 5,
                CircuitBreakerThreshold = 10,
                CircuitBreakerDurationSeconds = 90,
                CategoryDelays = new CategoryRetryDelays
                {
                    TransientDelaySeconds = 1,
                    InfrastructureDelaySeconds = 5,
                    AuthenticationDelaySeconds = 2
                }
            },
            ExportClient = new FiskalyClientConfiguration
            {
                TimeoutSeconds = 120,
                RetryCount = 2,
                CircuitBreakerThreshold = 3,
                CircuitBreakerDurationSeconds = 240,
                CategoryDelays = new CategoryRetryDelays
                {
                    TransientDelaySeconds = 5,
                    InfrastructureDelaySeconds = 10,
                    AuthenticationDelaySeconds = 5
                }
            },
            ClientManagementClient = new FiskalyClientConfiguration
            {
                TimeoutSeconds = 30,
                RetryCount = 3,
                CircuitBreakerThreshold = 5,
                CircuitBreakerDurationSeconds = 60,
                CategoryDelays = new CategoryRetryDelays
                {
                    TransientDelaySeconds = 1,
                    InfrastructureDelaySeconds = 5,
                    AuthenticationDelaySeconds = 2
                }
            }
        };
    }

    #endregion
}
