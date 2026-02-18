using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Models;

public class DsfinvkClientExportRequestTests
{
    // ============================================================================
    // Constructor Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithClientId_StoresClientId()
    {
        ClientId clientId = ClientId.New();

        DsfinvkClientExportRequest request = new DsfinvkClientExportRequest(clientId);

        Assert.Equal(clientId, request.ClientId);
    }

    // ============================================================================
    // Query Parameter Generation Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_GeneratesClientIdParameter()
    {
        ClientId clientId = ClientId.From("12345678-1234-4234-9234-123456789012");

        DsfinvkClientExportRequest request = new DsfinvkClientExportRequest(clientId);

        IEnumerable<KeyValuePair<string, string?>> parameters = request.ToQueryParameters();

        Assert.Single(parameters);
        Assert.Contains(parameters, p => p.Key == "client_id" && p.Value == clientId.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToQueryParameters_WithDifferentClientIds_GeneratesDifferentParameters()
    {
        ClientId clientId1 = ClientId.New();
        ClientId clientId2 = ClientId.New();

        DsfinvkClientExportRequest request1 = new DsfinvkClientExportRequest(clientId1);
        DsfinvkClientExportRequest request2 = new DsfinvkClientExportRequest(clientId2);

        IEnumerable<KeyValuePair<string, string?>> parameters1 = request1.ToQueryParameters();
        IEnumerable<KeyValuePair<string, string?>> parameters2 = request2.ToQueryParameters();

        Assert.NotEqual(
            parameters1.Single().Value,
            parameters2.Single().Value
        );
    }
}
