using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Tss.Enums;

public class TssStateTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void AllStates_HaveDefinedValues()
    {
        // Arrange & Act
        TssState[] states = Enum.GetValues<TssState>();

        // Assert
        Assert.NotEmpty(states);
        Assert.Contains(TssState.Created, states);
        Assert.Contains(TssState.Uninitialized, states);
        Assert.Contains(TssState.Initialized, states);
        Assert.Contains(TssState.Disabled, states);
        Assert.Contains(TssState.Deleted, states);
        Assert.Contains(TssState.Defective, states);
        Assert.Contains(TssState.Evicted, states);
        Assert.Equal(7, states.Length); // Should have 7 states
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Uninitialized_HasCorrectValue()
    {
        // Arrange
        TssState state = TssState.Uninitialized;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Uninitialized", stringValue);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Initialized_HasCorrectValue()
    {
        // Arrange
        TssState state = TssState.Initialized;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Initialized", stringValue);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Created_HasCorrectValue()
    {
        // Arrange
        TssState state = TssState.Created;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Created", stringValue);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Disabled_HasCorrectValue()
    {
        // Arrange
        TssState state = TssState.Disabled;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Disabled", stringValue);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(TssState.Created, "CREATED")]
    [InlineData(TssState.Uninitialized, "UNINITIALIZED")]
    [InlineData(TssState.Initialized, "INITIALIZED")]
    [InlineData(TssState.Disabled, "DISABLED")]
    [InlineData(TssState.Deleted, "DELETED")]
    [InlineData(TssState.Defective, "DEFECTIVE")]
    [InlineData(TssState.Evicted, "EVICTED")]
    public void Serialize_AllStates_SerializeAsUppercaseString(TssState state, string expectedJson)
    {
        // Act
        string json = JsonSerializer.Serialize(state);

        // Assert
        Assert.Equal($"\"{expectedJson}\"", json);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("CREATED", TssState.Created)]
    [InlineData("UNINITIALIZED", TssState.Uninitialized)]
    [InlineData("INITIALIZED", TssState.Initialized)]
    [InlineData("DISABLED", TssState.Disabled)]
    [InlineData("DELETED", TssState.Deleted)]
    [InlineData("DEFECTIVE", TssState.Defective)]
    [InlineData("EVICTED", TssState.Evicted)]
    public void Deserialize_AllStates_DeserializeCorrectly(string json, TssState expected)
    {
        // Arrange
        string jsonString = $"\"{json}\"";

        // Act
        TssState state = JsonSerializer.Deserialize<TssState>(jsonString);

        // Assert
        Assert.Equal(expected, state);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(TssState.Created)]
    [InlineData(TssState.Uninitialized)]
    [InlineData(TssState.Initialized)]
    [InlineData(TssState.Disabled)]
    [InlineData(TssState.Deleted)]
    [InlineData(TssState.Defective)]
    [InlineData(TssState.Evicted)]
    public void RoundTrip_AllStates_PreserveValue(TssState original)
    {
        // Act
        string json = JsonSerializer.Serialize(original);
        TssState deserialized = JsonSerializer.Deserialize<TssState>(json);

        // Assert
        Assert.Equal(original, deserialized);
    }
}
