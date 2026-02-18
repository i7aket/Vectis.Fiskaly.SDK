using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Requests;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.Requests;

public class UpdateClientRequestTests
{
    [Fact]
    public void RegisterFactory_ShouldCreateRequestWithRegisteredState()
    {
        // Act
        UpdateClientRequest request = UpdateClientRequest.Register();

        // Assert
        Assert.Equal(ClientState.Registered, request.State);
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void DeregisterFactory_ShouldCreateRequestWithDeregisteredState()
    {
        // Act
        UpdateClientRequest request = UpdateClientRequest.Deregister();

        // Assert
        Assert.Equal(ClientState.Deregistered, request.State);
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void RegisterFactory_WithMetadata_ShouldAssignMetadata()
    {
        // Arrange
        MetadataCollection metadata = MetadataCollection.From(new Dictionary<string, string>
        {
            ["terminal"] = "POS-01"
        });

        // Act
        UpdateClientRequest request = UpdateClientRequest.Register(metadata);

        // Assert
        Assert.Equal(ClientState.Registered, request.State);
        Assert.Same(metadata, request.Metadata);
    }

}
