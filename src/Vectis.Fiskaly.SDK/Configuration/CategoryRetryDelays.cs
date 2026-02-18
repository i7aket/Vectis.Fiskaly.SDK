namespace Vectis.Fiskaly.SDK.Configuration;

public sealed class CategoryRetryDelays
{
    public int TransientDelaySeconds { get; set; } = 1;

    public int InfrastructureDelaySeconds { get; set; } = 5;

    public int AuthenticationDelaySeconds { get; set; } = 2;
}
