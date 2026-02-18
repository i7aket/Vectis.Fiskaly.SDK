namespace Vectis.Fiskaly.SDK.Configuration;

public class FiskalyClientConfiguration
{
    public int TimeoutSeconds { get; set; } = 30;

    public bool ResilienceEnabled { get; set; } = true;

    public int RetryCount { get; set; } = 3;

    public CategoryRetryDelays CategoryDelays { get; set; } = new();

    public int CircuitBreakerThreshold { get; set; } = 5;

    public int CircuitBreakerDurationSeconds { get; set; } = 30;
}
