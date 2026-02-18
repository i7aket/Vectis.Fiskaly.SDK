using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Enums;

public class ExportStateTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void AllStates_HaveDefinedValues()
    {
        // Arrange & Act
        ExportState[] states = Enum.GetValues<ExportState>();

        // Assert
        Assert.NotEmpty(states);
        Assert.Contains(ExportState.Pending, states);
        Assert.Contains(ExportState.Working, states);
        Assert.Contains(ExportState.Completed, states);
        Assert.Contains(ExportState.Error, states);
        Assert.Contains(ExportState.Cancelled, states);
        Assert.Equal(5, states.Length); // Should have 5 states
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Pending_HasCorrectValue()
    {
        // Arrange
        ExportState state = ExportState.Pending;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Pending", stringValue);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Completed_HasCorrectValue()
    {
        // Arrange
        ExportState state = ExportState.Completed;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Completed", stringValue);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Working_HasCorrectValue()
    {
        // Arrange
        ExportState state = ExportState.Working;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Working", stringValue);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Error_HasCorrectValue()
    {
        // Arrange
        ExportState state = ExportState.Error;

        // Act
        string stringValue = state.ToString();

        // Assert
        Assert.Equal("Error", stringValue);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(ExportState.Pending, "PENDING")]
    [InlineData(ExportState.Working, "WORKING")]
    [InlineData(ExportState.Completed, "COMPLETED")]
    [InlineData(ExportState.Error, "ERROR")]
    [InlineData(ExportState.Cancelled, "CANCELLED")]
    public void Serialize_AllStates_SerializeAsUppercaseString(ExportState state, string expectedJson)
    {
        // Act
        string json = JsonSerializer.Serialize(state);

        // Assert
        Assert.Equal($"\"{expectedJson}\"", json);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("PENDING", ExportState.Pending)]
    [InlineData("WORKING", ExportState.Working)]
    [InlineData("COMPLETED", ExportState.Completed)]
    [InlineData("ERROR", ExportState.Error)]
    [InlineData("CANCELLED", ExportState.Cancelled)]
    public void Deserialize_AllStates_DeserializeCorrectly(string json, ExportState expected)
    {
        // Arrange
        string jsonString = $"\"{json}\"";

        // Act
        ExportState state = JsonSerializer.Deserialize<ExportState>(jsonString);

        // Assert
        Assert.Equal(expected, state);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(ExportState.Pending)]
    [InlineData(ExportState.Working)]
    [InlineData(ExportState.Completed)]
    [InlineData(ExportState.Error)]
    [InlineData(ExportState.Cancelled)]
    public void RoundTrip_AllStates_PreserveValue(ExportState original)
    {
        // Act
        string json = JsonSerializer.Serialize(original);
        ExportState deserialized = JsonSerializer.Deserialize<ExportState>(json);

        // Assert
        Assert.Equal(original, deserialized);
    }
}
