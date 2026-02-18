namespace Vectis.Fiskaly.SDK.Configuration;

public static class FiskalyClientConfigurationExtensions
{
    public static void DisableResilience(this FiskalyClientConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.ResilienceEnabled = false;
        config.CircuitBreakerThreshold = 0;
    }

    public static void UseTestDefaults(this FiskalyClientConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.ResilienceEnabled = true;
        config.TimeoutSeconds = 5;
        config.RetryCount = 1;
        config.CircuitBreakerThreshold = 0;
        config.CircuitBreakerDurationSeconds = 10;
    }

    public static void UseProductionDefaults(this FiskalyClientConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.ResilienceEnabled = true;
        config.TimeoutSeconds = 30;
        config.RetryCount = 3;
        config.CircuitBreakerThreshold = 5;
        config.CircuitBreakerDurationSeconds = 30;
    }

    public static void UseHighResilience(this FiskalyClientConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.ResilienceEnabled = true;
        config.TimeoutSeconds = 60;
        config.RetryCount = 10;
        config.CircuitBreakerThreshold = 15;
        config.CircuitBreakerDurationSeconds = 60;

        config.CategoryDelays = new CategoryRetryDelays
        {
            TransientDelaySeconds = 2,
            InfrastructureDelaySeconds = 10,
            AuthenticationDelaySeconds = 5
        };
    }
}
