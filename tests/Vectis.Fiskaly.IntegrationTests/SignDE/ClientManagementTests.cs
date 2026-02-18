using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.SignDE.Admin.Requests;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Models;
using Vectis.Fiskaly.SDK.SignDE.Clients.Requests;
using Vectis.Fiskaly.SDK.SignDE.Clients.Responses;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.IntegrationTests.SignDE;

/// <summary>
/// Integration tests for client management operations.
/// </summary>
/// <remarks>
/// <para><strong>Scope</strong>: Client lifecycle and metadata management</para>
///
/// <para><strong>Endpoints Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>PUT /tss/{tss_id}/client/{client_id} - CreateClientAsync</description></item>
///   <item><description>GET /tss/{tss_id}/client/{client_id} - GetClientAsync</description></item>
///   <item><description>PATCH /tss/{tss_id}/client/{client_id} - UpdateClientAsync</description></item>
///   <item><description>GET /tss/{tss_id}/client - ListClientsAsync</description></item>
///   <item><description>GET /client - ListAllClientsAsync</description></item>
///   <item><description>PUT /tss/{tss_id}/client/{client_id}/metadata/{key} - UpdateClientMetadataAsync</description></item>
///   <item><description>GET /tss/{tss_id}/client/{client_id}/metadata/{key} - GetClientMetadataAsync</description></item>
/// </list>
///
/// <para><strong>Note</strong>: Base class provides a Client in REGISTERED state.
/// Tests that modify client state create additional clients for isolation.</para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "ClientManagement")]
[Trait("Priority", "High")]
public class ClientManagementTests : FiskalyIntegrationTestBase
{
    public ClientManagementTests(FiskalyBaseTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task RegisterClient_WithSerialNumber_ShouldReturnRegisteredState()
    {
        // Arrange
        ClientId newClientId = ClientId.New();
        ClientSerialNumber serialNumber = ClientSerialNumber.From($"TEST-{newClientId.ToString()[..8]}");

        Console.WriteLine($"Registering new client: {newClientId}");
        Console.WriteLine($"   Serial Number: {serialNumber}");

        try
        {
            // Authenticate admin for client registration
            await Fixture.AdminClient.AuthenticateAdminAsync(
                TssId,
                new AdminAuthenticationRequest
                {
                    AdminPin = AdminPin.From(AdminPinValue)
                });

            // Act
            ClientResponse response = await Fixture.ClientManagementClient.CreateClientAsync(
                TssId,
                newClientId,
                new CreateClientRequest { SerialNumber = serialNumber });

            // Assert
            response.Should().NotBeNull();
            response.Id.Should().NotBeNull();
            response.Id.Should().Be(newClientId);
            response.State.Should().Be(ClientState.Registered);
            response.SerialNumber.Should().Be(serialNumber);

            Console.WriteLine($"✅ Client registered successfully!");
            Console.WriteLine($"   State: {response.State}");
            Console.WriteLine($"   Serial Number: {response.SerialNumber}");

            // Cleanup
            try
            {
                await Fixture.ClientManagementClient.UpdateClientAsync(
                    TssId,
                    newClientId,
                    UpdateClientRequest.Deregister());
                Console.WriteLine($"   Cleanup: Client {newClientId} deregistered");

                await Fixture.AdminClient.LogoutAdminAsync(TssId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Cleanup failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Registration failed: {ex.Message}");
            throw;
        }
    }

    [Fact]
    public async Task GetClient_AfterRegistration_ShouldReturnDetails()
    {
        // Arrange
        Console.WriteLine($"Getting client details: {ClientId}");

        // Act
        ClientResponse response = await Fixture.ClientManagementClient.GetClientAsync(TssId, ClientId);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeNull();
        response.Id.Should().Be(ClientId);
        response.State.Should().Be(ClientState.Registered);
        response.SerialNumber.Should().NotBeNull();

        Console.WriteLine($"✅ Client retrieved successfully!");
        Console.WriteLine($"   State: {response.State}");
        Console.WriteLine($"   Serial Number: {response.SerialNumber}");
    }

    [Fact]
    public async Task DeregisterClient_ShouldTransitionToDeregistered()
    {
        // Arrange - Create a new client for this test
        ClientId testClientId = ClientId.New();
        ClientSerialNumber serialNumber = ClientSerialNumber.From($"TEST-{testClientId.ToString()[..8]}");

        // Authenticate admin for client state changes
        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        await Fixture.ClientManagementClient.CreateClientAsync(
            TssId,
            testClientId,
            new CreateClientRequest { SerialNumber = serialNumber });

        Console.WriteLine($"Client {testClientId} registered, testing DEREGISTER...");

        // Act
        ClientResponse response = await Fixture.ClientManagementClient.UpdateClientAsync(
            TssId,
            testClientId,
            UpdateClientRequest.Deregister());

        // Assert
        response.Should().NotBeNull();
        response.State.Should().Be(ClientState.Deregistered);

        Console.WriteLine($"✅ Client transitioned to DEREGISTERED");
        Console.WriteLine($"   Final State: {response.State}");

        // Cleanup
        await Fixture.AdminClient.LogoutAdminAsync(TssId);
    }

    [Fact]
    public async Task ReregisterClient_AfterDeregistration_ShouldTransitionToRegistered()
    {
        // Arrange - Create and deregister a client
        ClientId testClientId = ClientId.New();
        ClientSerialNumber serialNumber = ClientSerialNumber.From($"TEST-{testClientId.ToString()[..8]}");

        // Authenticate admin for client state changes
        await Fixture.AdminClient.AuthenticateAdminAsync(
            TssId,
            new AdminAuthenticationRequest
            {
                AdminPin = AdminPin.From(AdminPinValue)
            });

        await Fixture.ClientManagementClient.CreateClientAsync(
            TssId,
            testClientId,
            new CreateClientRequest { SerialNumber = serialNumber });

        await Fixture.ClientManagementClient.UpdateClientAsync(
            TssId,
            testClientId,
            UpdateClientRequest.Deregister());

        Console.WriteLine($"Client {testClientId} is DEREGISTERED, testing REREGISTER...");

        // Act
        ClientResponse response = await Fixture.ClientManagementClient.UpdateClientAsync(
            TssId,
            testClientId,
            UpdateClientRequest.Register());

        // Assert
        response.Should().NotBeNull();
        response.State.Should().Be(ClientState.Registered);

        Console.WriteLine($"✅ Client transitioned back to REGISTERED");
        Console.WriteLine($"   Final State: {response.State}");

        // Cleanup
        try
        {
            await Fixture.ClientManagementClient.UpdateClientAsync(
                TssId,
                testClientId,
                UpdateClientRequest.Deregister());

            await Fixture.AdminClient.LogoutAdminAsync(TssId);
        }
        catch { /* Ignore cleanup errors */ }
    }

    [Fact]
    public async Task ListClients_ShouldContainRegisteredClient()
    {
        // Arrange
        Console.WriteLine($"Listing clients for TSS: {TssId}");

        // Act
        ClientListResponse response = await Fixture.ClientManagementClient.ListClientsAsync(TssId);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().NotBeEmpty();
        response.Data.Should().Contain(c => c.Id == ClientId);

        ClientResponse ourClient = response.Data.First(c => c.Id == ClientId);
        ourClient.State.Should().Be(ClientState.Registered);

        Console.WriteLine($"✅ Listed {response.Data.Count} client(s)");
        Console.WriteLine($"   Our client {ClientId} found with state: {ourClient.State}");
    }

    [Fact]
    public async Task ListAllClients_ShouldContainClientAcrossAllTss()
    {
        // Arrange
        Console.WriteLine($"Listing all clients across all TSS...");

        // Act - Sort by TimeCreation descending to get newest clients first
        ListClientsQueryParameters queryParams = new ListClientsQueryParameters
        {
            Sort = new ClientSortOption(ClientSortField.TimeCreation, SortDirection.Descending),
            Limit = 100 // Limit to first page to avoid loading too many old clients
        };
        ClientListResponse response = await Fixture.ClientManagementClient.ListAllClientsAsync(queryParams);

        // Assert
        response.Should().NotBeNull();
        response.Data.Should().NotBeEmpty();
        response.Data.Should().Contain(c => c.Id == ClientId, "Our client should be in global list");

        Console.WriteLine($"✅ Listed {response.Data.Count} client(s) across all TSS (sorted by TimeCreation DESC)");
        Console.WriteLine($"   Our client {ClientId} found in global list");
    }

    [Fact]
    public async Task UpdateClientMetadata_ShouldPersist()
    {
        // Arrange
        MetadataCollection metadata = MetadataCollection.Empty
            .Add("environment", "integration-test")
            .Add("test-class", GetType().Name)
            .Add("timestamp", DateTime.UtcNow.ToString("o"));

        Console.WriteLine($"Updating client metadata: {metadata.Count} keys");

        // Act
        await Fixture.ClientManagementClient.UpdateClientMetadataAsync(TssId, ClientId, metadata);

        // Assert - Retrieve and verify
        MetadataCollection retrievedMetadata = await Fixture.ClientManagementClient.GetClientMetadataAsync(TssId, ClientId);
        retrievedMetadata.Should().NotBeNull();
        retrievedMetadata.Should().ContainKeys(metadata.Keys);
        retrievedMetadata["environment"].Should().Be("integration-test");
        retrievedMetadata["test-class"].Should().Be(GetType().Name);

        Console.WriteLine($"✅ Client metadata updated: {retrievedMetadata.Count} keys");
        foreach (KeyValuePair<string, string> kvp in retrievedMetadata)
        {
            Console.WriteLine($"   {kvp.Key} = {kvp.Value}");
        }
    }

    [Fact]
    public async Task GetClientMetadata_AfterUpdate_ShouldReturnNewMetadata()
    {
        // Arrange
        string key1 = "client-test-key";
        string value1 = "client-test-value";
        MetadataCollection metadata = MetadataCollection.Empty.Add(key1, value1);

        await Fixture.ClientManagementClient.UpdateClientMetadataAsync(TssId, ClientId, metadata);
        Console.WriteLine($"Metadata set: {key1} = {value1}");

        // Act
        MetadataCollection retrievedMetadata = await Fixture.ClientManagementClient.GetClientMetadataAsync(TssId, ClientId);

        // Assert
        retrievedMetadata.Should().NotBeNull();
        retrievedMetadata.Should().ContainKey(key1);
        retrievedMetadata[key1].Should().Be(value1);

        Console.WriteLine($"✅ Client metadata retrieved: {retrievedMetadata.Count} keys");
        Console.WriteLine($"   {key1} = {retrievedMetadata[key1]}");
    }
}
