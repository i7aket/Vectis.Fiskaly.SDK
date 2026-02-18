namespace Vectis.Fiskaly.SDK.Configuration;

public class FiskalyTestConfiguration
{
    public TssConfiguration SharedTss { get; set; } = new();

    public TssConfiguration TssForInitializeTest { get; set; } = new();

    public TssConfiguration TssForDuplicateTest { get; set; } = new();

    public TssConfiguration TssForCreateTest { get; set; } = new();

    public TssConfiguration Tss1 { get; set; } = new();

    public string SharedClientId { get; set; } = "shared-client-001";

    public string SharedClientSerialNumber { get; set; } = "SHARED-CLIENT-001";

    public string AdminPin { get; set; } = "1234567890";
}

public class TssConfiguration
{
    public string Id { get; set; } = string.Empty;

    public string AdminPuk { get; set; } = string.Empty;

    public string State { get; set; } = "UNINITIALIZED";
}
