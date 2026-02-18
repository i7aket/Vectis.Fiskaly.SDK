namespace Vectis.Fiskaly.SDK.Configuration;

public class FiskalyConfiguration
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://kassensichv-middleware.fiskaly.com/api/v2/";

    public string ManagementBaseUrl { get; set; } = "https://dashboard.fiskaly.com/api/v0/";

    public bool AllowHttpForPrivateNetworks { get; set; }

    public int TimeoutSeconds { get; set; } = 5;

    public int RetryAttempts { get; set; } = 3;

    public bool EnableCircuitBreaker { get; set; } = true;

    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    public string FallbackDirectory { get; set; } = string.Empty;

    public FiskalyClientConfiguration AdminClient { get; set; } = new()
    {
        TimeoutSeconds = 10,
        RetryCount = 1,
        CircuitBreakerThreshold = 5,
        CircuitBreakerDurationSeconds = 30
    };

    public FiskalyClientConfiguration AuthClient { get; set; } = new()
    {
        TimeoutSeconds = 10,
        RetryCount = 2,
        CircuitBreakerThreshold = 0,  // Disabled - auth failures should fail fast
        CircuitBreakerDurationSeconds = 10
    };

    public FiskalyClientConfiguration TssClient { get; set; } = new()
    {
        TimeoutSeconds = 30,
        RetryCount = 3,
        CircuitBreakerThreshold = 5,
        CircuitBreakerDurationSeconds = 60  // Must be >= 2 * TimeoutSeconds (Microsoft.Extensions.Http.Resilience requirement)
    };

    public FiskalyClientConfiguration TransactionClient { get; set; } = new()
    {
        TimeoutSeconds = 45,
        RetryCount = 5,
        CircuitBreakerThreshold = 10,
        CircuitBreakerDurationSeconds = 90  // Must be >= 2 * TimeoutSeconds (Microsoft.Extensions.Http.Resilience requirement)
    };

    public FiskalyClientConfiguration ExportClient { get; set; } = new()
    {
        TimeoutSeconds = 120,
        RetryCount = 2,
        CircuitBreakerThreshold = 3,
        CircuitBreakerDurationSeconds = 240,  // Must be >= 2 * TimeoutSeconds (Microsoft.Extensions.Http.Resilience requirement)
        CategoryDelays = new CategoryRetryDelays
        {
            TransientDelaySeconds = 5,  // Longer delays for export operations (5s → 10s → 20s)
            InfrastructureDelaySeconds = 10,
            AuthenticationDelaySeconds = 5
        }
    };

    public FiskalyClientConfiguration ClientManagementClient { get; set; } = new()
    {
        TimeoutSeconds = 30,
        RetryCount = 3,
        CircuitBreakerThreshold = 5,
        CircuitBreakerDurationSeconds = 60  // Must be >= 2 * TimeoutSeconds (Microsoft.Extensions.Http.Resilience requirement)
    };

    public FiskalyClientConfiguration OrganizationClient { get; set; } = new()
    {
        TimeoutSeconds = 30,
        RetryCount = 3,
        CircuitBreakerThreshold = 5,
        CircuitBreakerDurationSeconds = 60  // Must be >= 2 * TimeoutSeconds (Microsoft.Extensions.Http.Resilience requirement)
    };
}
