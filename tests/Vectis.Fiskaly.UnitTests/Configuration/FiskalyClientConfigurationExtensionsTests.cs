using Vectis.Fiskaly.SDK.Configuration;

namespace Vectis.Fiskaly.UnitTests.Configuration;

public class FiskalyClientConfigurationExtensionsTests
{
    // ============================================================================
    // DisableResilience Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void DisableResilience_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        FiskalyClientConfiguration? config = null;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => config!.DisableResilience());
        Assert.Equal("config", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void DisableResilience_SetsResilienceEnabledFalse()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration
        {
            RetryCount = 5,
            ResilienceEnabled = true
        };

        // Act
        config.DisableResilience();

        // Assert
        Assert.False(config.ResilienceEnabled);
        Assert.Equal(5, config.RetryCount);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void DisableResilience_SetsCircuitBreakerThresholdToZero()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration
        {
            CircuitBreakerThreshold = 10
        };

        // Act
        config.DisableResilience();

        // Assert
        Assert.False(config.ResilienceEnabled);
        Assert.Equal(0, config.CircuitBreakerThreshold);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void DisableResilience_DisablesResilienceAndKeepsTimeout()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration
        {
            RetryCount = 3,
            CircuitBreakerThreshold = 5,
            TimeoutSeconds = 30
        };

        // Act
        config.DisableResilience();

        // Assert
        Assert.False(config.ResilienceEnabled);
        Assert.Equal(3, config.RetryCount);
        Assert.Equal(0, config.CircuitBreakerThreshold);
        Assert.Equal(30, config.TimeoutSeconds); // Other properties unchanged
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void DisableResilience_IsIdempotent()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration
        {
            RetryCount = 3,
            CircuitBreakerThreshold = 5
        };

        // Act - Call twice
        config.DisableResilience();
        config.DisableResilience();

        // Assert
        Assert.False(config.ResilienceEnabled);
        Assert.Equal(3, config.RetryCount);
        Assert.Equal(0, config.CircuitBreakerThreshold);
    }

    // ============================================================================
    // UseTestDefaults Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        FiskalyClientConfiguration? config = null;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => config!.UseTestDefaults());
        Assert.Equal("config", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_SetsTimeoutTo5Seconds()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseTestDefaults();

        // Assert
        Assert.Equal(5, config.TimeoutSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_SetsRetryCountToOne()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration { RetryCount = 10 };

        // Act
        config.UseTestDefaults();

        // Assert
        Assert.Equal(1, config.RetryCount);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_DisablesCircuitBreaker()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration { CircuitBreakerThreshold = 10 };

        // Act
        config.UseTestDefaults();

        // Assert
        Assert.Equal(0, config.CircuitBreakerThreshold);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_SetsCircuitBreakerDurationTo10Seconds()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseTestDefaults();

        // Assert
        Assert.Equal(10, config.CircuitBreakerDurationSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_SetsAllExpectedProperties()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration
        {
            TimeoutSeconds = 100,
            RetryCount = 10,
            CircuitBreakerThreshold = 20,
            CircuitBreakerDurationSeconds = 60
        };

        // Act
        config.UseTestDefaults();

        // Assert
        Assert.Equal(5, config.TimeoutSeconds);
        Assert.Equal(1, config.RetryCount);
        Assert.Equal(0, config.CircuitBreakerThreshold);
        Assert.Equal(10, config.CircuitBreakerDurationSeconds);
    }

    // ============================================================================
    // UseProductionDefaults Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void UseProductionDefaults_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        FiskalyClientConfiguration? config = null;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => config!.UseProductionDefaults());
        Assert.Equal("config", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseProductionDefaults_SetsTimeoutTo30Seconds()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseProductionDefaults();

        // Assert
        Assert.Equal(30, config.TimeoutSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseProductionDefaults_SetsRetryCountTo3()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseProductionDefaults();

        // Assert
        Assert.Equal(3, config.RetryCount);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseProductionDefaults_SetsCircuitBreakerThresholdTo5()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseProductionDefaults();

        // Assert
        Assert.Equal(5, config.CircuitBreakerThreshold);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseProductionDefaults_SetsCircuitBreakerDurationTo30Seconds()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseProductionDefaults();

        // Assert
        Assert.Equal(30, config.CircuitBreakerDurationSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseProductionDefaults_SetsAllExpectedProperties()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration
        {
            TimeoutSeconds = 5,
            RetryCount = 1,
            CircuitBreakerThreshold = 0,
            CircuitBreakerDurationSeconds = 10
        };

        // Act
        config.UseProductionDefaults();

        // Assert
        Assert.Equal(30, config.TimeoutSeconds);
        Assert.Equal(3, config.RetryCount);
        Assert.Equal(5, config.CircuitBreakerThreshold);
        Assert.Equal(30, config.CircuitBreakerDurationSeconds);
    }

    // ============================================================================
    // UseHighResilience Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange
        FiskalyClientConfiguration? config = null;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => config!.UseHighResilience());
        Assert.Equal("config", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_SetsTimeoutTo60Seconds()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseHighResilience();

        // Assert
        Assert.Equal(60, config.TimeoutSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_SetsRetryCountTo10()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseHighResilience();

        // Assert
        Assert.Equal(10, config.RetryCount);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_SetsCircuitBreakerThresholdTo15()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseHighResilience();

        // Assert
        Assert.Equal(15, config.CircuitBreakerThreshold);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_SetsCircuitBreakerDurationTo60Seconds()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseHighResilience();

        // Assert
        Assert.Equal(60, config.CircuitBreakerDurationSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_SetsConservativeCategoryDelays()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseHighResilience();

        // Assert
        Assert.NotNull(config.CategoryDelays);
        Assert.Equal(2, config.CategoryDelays.TransientDelaySeconds);
        Assert.Equal(10, config.CategoryDelays.InfrastructureDelaySeconds);
        Assert.Equal(5, config.CategoryDelays.AuthenticationDelaySeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_SetsAllExpectedProperties()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration
        {
            TimeoutSeconds = 30,
            RetryCount = 3,
            CircuitBreakerThreshold = 5,
            CircuitBreakerDurationSeconds = 30
        };

        // Act
        config.UseHighResilience();

        // Assert
        Assert.Equal(60, config.TimeoutSeconds);
        Assert.Equal(10, config.RetryCount);
        Assert.Equal(15, config.CircuitBreakerThreshold);
        Assert.Equal(60, config.CircuitBreakerDurationSeconds);
        Assert.NotNull(config.CategoryDelays);
        Assert.Equal(2, config.CategoryDelays.TransientDelaySeconds);
        Assert.Equal(10, config.CategoryDelays.InfrastructureDelaySeconds);
        Assert.Equal(5, config.CategoryDelays.AuthenticationDelaySeconds);
    }

    // ============================================================================
    // Cross-Method Integration Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ExtensionMethods_CanBeChained()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act - Chain multiple calls (last wins)
        config.UseProductionDefaults();
        config.DisableResilience();

        // Assert - DisableResilience keeps retry count but disables resilience
        Assert.False(config.ResilienceEnabled);
        Assert.Equal(3, config.RetryCount);
        Assert.Equal(0, config.CircuitBreakerThreshold);
        Assert.Equal(30, config.TimeoutSeconds); // From production defaults
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseTestDefaults_ThenUseProductionDefaults_OverridesAllSettings()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseTestDefaults();
        config.UseProductionDefaults();

        // Assert - Production defaults override test defaults
        Assert.Equal(30, config.TimeoutSeconds);
        Assert.Equal(3, config.RetryCount);
        Assert.Equal(5, config.CircuitBreakerThreshold);
        Assert.Equal(30, config.CircuitBreakerDurationSeconds);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void UseHighResilience_ThenDisableResilience_KeepsTimeoutButRemovesRetries()
    {
        // Arrange
        FiskalyClientConfiguration config = new FiskalyClientConfiguration();

        // Act
        config.UseHighResilience();
        config.DisableResilience();

        // Assert
        Assert.Equal(60, config.TimeoutSeconds); // From UseHighResilience
        Assert.False(config.ResilienceEnabled);
        Assert.Equal(10, config.RetryCount); // From UseHighResilience
        Assert.Equal(0, config.CircuitBreakerThreshold); // Disabled
        Assert.NotNull(config.CategoryDelays); // From UseHighResilience (not reset)
    }
}
