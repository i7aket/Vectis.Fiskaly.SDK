using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Admin.Responses;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Admin.Responses;

public class AdminAuthenticationResponseTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_EmptyObject_ReturnsInstance()
    {
        // Fiskaly API returns empty object {} per OpenAPI spec v2.1.33
        string json = "{}";

        AdminAuthenticationResponse? response = JsonSerializer.Deserialize<AdminAuthenticationResponse>(json);

        Assert.NotNull(response);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PropertySetters_CanSetTssId()
    {
        // TssId is populated client-side by SDK (has [JsonIgnore])
        TssId tssId = TssId.From("a1b2c3d4-1234-4abc-9def-123456789012");

        AdminAuthenticationResponse response = new AdminAuthenticationResponse
        {
            TssId = tssId
        };

        Assert.Equal(tssId, response.TssId);
        Assert.Equal("a1b2c3d4-1234-4abc-9def-123456789012", response.TssId.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_DoesNotIncludeTssId()
    {
        // TssId has [JsonIgnore] so it should not be serialized
        TssId tssId = TssId.From("a1b2c3d4-1234-4abc-9def-123456789012");
        AdminAuthenticationResponse response = new AdminAuthenticationResponse
        {
            TssId = tssId
        };

        string json = JsonSerializer.Serialize(response);

        // Should serialize as empty object because TssId is ignored
        Assert.Equal("{}", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void DefaultConstructor_InitializesWithDefaultTssId()
    {
        AdminAuthenticationResponse response = new AdminAuthenticationResponse();

        Assert.Equal(default, response.TssId);
    }
}
