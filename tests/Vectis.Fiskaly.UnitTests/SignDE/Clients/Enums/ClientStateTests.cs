using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.Enums;

public class ClientStateTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void AllStates_HaveDefinedValues()
    {
        // Arrange & Act
        ClientState[] states = Enum.GetValues<ClientState>();

        // Assert
        Assert.NotEmpty(states);
        Assert.Contains(ClientState.Registered, states);
        Assert.Contains(ClientState.Deregistered, states);
        Assert.Equal(2, states.Length); // Should only have 2 states
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Registered_HasCorrectValue()
    {
        // Arrange
        ClientState state = ClientState.Registered;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Registered", stringValue);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deregistered_HasCorrectValue()
    {
        // Arrange
        ClientState state = ClientState.Deregistered;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Deregistered", stringValue);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_Registered_SerializesAsUppercaseString()
    {
        // Arrange
        ClientState state = ClientState.Registered;

        // Act
        string json = JsonSerializer.Serialize(state);

        // Assert
        Assert.Equal("\"REGISTERED\"", json); // Per JsonStringEnumMemberName attribute
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_Deregistered_SerializesAsUppercaseString()
    {
        // Arrange
        ClientState state = ClientState.Deregistered;

        // Act
        string json = JsonSerializer.Serialize(state);

        // Assert
        Assert.Equal("\"DEREGISTERED\"", json); // Per JsonStringEnumMemberName attribute
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_RegisteredString_ReturnsRegisteredEnum()
    {
        // Arrange
        string json = "\"REGISTERED\"";

        // Act
        ClientState state = JsonSerializer.Deserialize<ClientState>(json);

        // Assert
        Assert.Equal(ClientState.Registered, state);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_DeregisteredString_ReturnsDeregisteredEnum()
    {
        // Arrange
        string json = "\"DEREGISTERED\"";

        // Act
        ClientState state = JsonSerializer.Deserialize<ClientState>(json);

        // Assert
        Assert.Equal(ClientState.Deregistered, state);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_Registered_PreservesValue()
    {
        // Arrange
        ClientState original = ClientState.Registered;

        // Act
        string json = JsonSerializer.Serialize(original);
        ClientState deserialized = JsonSerializer.Deserialize<ClientState>(json);

        // Assert
        Assert.Equal(original, deserialized);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_Deregistered_PreservesValue()
    {
        // Arrange
        ClientState original = ClientState.Deregistered;

        // Act
        string json = JsonSerializer.Serialize(original);
        ClientState deserialized = JsonSerializer.Deserialize<ClientState>(json);

        // Assert
        Assert.Equal(original, deserialized);
    }
}
