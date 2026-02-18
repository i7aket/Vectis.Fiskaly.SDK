using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Admin.Requests;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Admin.Requests;

public class ChangeAdminPinRequestTests
{
    private readonly JsonSerializerOptions _options;

    public ChangeAdminPinRequestTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // ============================================================================
    // Serialization Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_ValidRequest_ContainsAdminPukAndNewPin()
    {
        ChangeAdminPinRequest request = new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From("1234567890"),
            NewAdminPin = AdminPin.From("newpin123")
        };

        string json = JsonSerializer.Serialize(request, _options);

        Assert.Contains("\"admin_puk\"", json);
        Assert.Contains("\"new_admin_pin\"", json);
        Assert.Contains("\"1234567890\"", json);
        Assert.Contains("\"newpin123\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_Request_UsesSnakeCasePropertyNames()
    {
        ChangeAdminPinRequest request = new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From("1234567890"),
            NewAdminPin = AdminPin.From("test123")
        };

        string json = JsonSerializer.Serialize(request, _options);

        // Should use snake_case per Fiskaly API spec
        Assert.Contains("admin_puk", json);
        Assert.Contains("new_admin_pin", json);
        // Should NOT contain PascalCase
        Assert.DoesNotContain("AdminPuk", json);
        Assert.DoesNotContain("NewAdminPin", json);
    }

    // ============================================================================
    // Deserialization Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidJson_ReturnsRequest()
    {
        string json = """{"admin_puk":"1234567890","new_admin_pin":"newpin"}""";

        ChangeAdminPinRequest? request = JsonSerializer.Deserialize<ChangeAdminPinRequest>(json, _options);

        Assert.NotNull(request);
        Assert.Equal("1234567890", request.AdminPuk.Value);
        Assert.Equal("newpin", request.NewAdminPin.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_CaseInsensitiveProperties_ReturnsRequest()
    {
        // Test with PascalCase (should work due to PropertyNameCaseInsensitive)
        string json = """{"Admin_Puk":"1234567890","New_Admin_Pin":"testpin"}""";

        ChangeAdminPinRequest? request = JsonSerializer.Deserialize<ChangeAdminPinRequest>(json, _options);

        Assert.NotNull(request);
        Assert.Equal("1234567890", request.AdminPuk.Value);
        Assert.Equal("testpin", request.NewAdminPin.Value);
    }

    // ============================================================================
    // Round-trip Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_SerializeAndDeserialize_PreservesValues()
    {
        ChangeAdminPinRequest original = new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From("9876543210"),
            NewAdminPin = AdminPin.From("secure123")
        };

        string json = JsonSerializer.Serialize(original, _options);
        ChangeAdminPinRequest? deserialized = JsonSerializer.Deserialize<ChangeAdminPinRequest>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.AdminPuk.Value, deserialized.AdminPuk.Value);
        Assert.Equal(original.NewAdminPin.Value, deserialized.NewAdminPin.Value);
    }
}
