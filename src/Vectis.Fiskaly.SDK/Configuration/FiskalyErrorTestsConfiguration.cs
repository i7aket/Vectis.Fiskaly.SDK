namespace Vectis.Fiskaly.SDK.Configuration;

public class FiskalyErrorTestsConfiguration
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://kassensichv-middleware.fiskaly.com/api/v2/";

    public ErrorTestTssConfiguration DedicatedTss { get; set; } = new();

    public ErrorTestClientConfiguration DedicatedClient { get; set; } = new();
}

public class ErrorTestTssConfiguration
{
    public string? Id { get; set; }

    public string? AdminPuk { get; set; }

    public string AdminPin { get; set; } = "error-test-pin-123";

    public string ExpectedState { get; set; } = "INITIALIZED";
}

public class ErrorTestClientConfiguration
{
    public string? Id { get; set; }

    public string SerialNumber { get; set; } = "ERROR-CLIENT-001";
}
