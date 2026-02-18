using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Requests;

public class OtherTests
{
    private readonly JsonSerializerOptions _options;

    public OtherTests()
    {
        _options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullAdditionalData_Succeeds()
    {
        // Arrange & Act
        Other other = new Other
        {
            AdditionalData = null
        };

        // Assert
        Assert.Null(other.AdditionalData);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithEmptyAdditionalData_Succeeds()
    {
        // Arrange & Act
        Other other = new Other
        {
            AdditionalData = new Dictionary<string, object>()
        };

        // Assert
        Assert.NotNull(other.AdditionalData);
        Assert.Empty(other.AdditionalData);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithCustomProperties_Succeeds()
    {
        // Arrange & Act
        Other other = new Other
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["mode"] = "training",
                ["employee_id"] = "EMP-12345",
                ["training_module"] = "POS-Basics"
            }
        };

        // Assert
        Assert.Equal(3, other.AdditionalData.Count);
        Assert.Equal("training", other.AdditionalData["mode"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithNullAdditionalData_ProducesEmptyObject()
    {
        // Arrange
        Other other = new Other
        {
            AdditionalData = null
        };

        // Act
        string json = JsonSerializer.Serialize(other, _options);

        // Assert
        Assert.Equal("{}", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithEmptyAdditionalData_ProducesEmptyObject()
    {
        // Arrange
        Other other = new Other
        {
            AdditionalData = new Dictionary<string, object>()
        };

        // Act
        string json = JsonSerializer.Serialize(other, _options);

        // Assert
        Assert.Equal("{}", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WithCustomProperties_ProducesCorrectJson()
    {
        // Arrange
        Other other = new Other
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["mode"] = "training",
                ["employee_id"] = "EMP-12345"
            }
        };

        // Act
        string json = JsonSerializer.Serialize(other, _options);

        // Assert
        Assert.Contains("\"mode\":\"training\"", json);
        Assert.Contains("\"employee_id\":\"EMP-12345\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_CustomPropertiesAtRootLevel()
    {
        // Arrange
        Other other = new Other
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["test_property"] = "test_value"
            }
        };

        // Act
        string json = JsonSerializer.Serialize(other, _options);

        // Assert
        // Properties should be at root level, not nested
        Assert.DoesNotContain("AdditionalData", json);
        Assert.Contains("\"test_property\":\"test_value\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_FromEmptyObject_Succeeds()
    {
        // Arrange
        string json = "{}";

        // Act
        Other? other = JsonSerializer.Deserialize<Other>(json, _options);

        // Assert
        Assert.NotNull(other);
        Assert.Null(other.AdditionalData);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WithCustomProperties_Succeeds()
    {
        // Arrange
        string json = """
                      {
                          "mode": "training",
                          "employee_id": "EMP-12345",
                          "training_module": "POS-Basics"
                      }
                      """;

        // Act
        Other? other = JsonSerializer.Deserialize<Other>(json, _options);

        // Assert
        Assert.NotNull(other);
        Assert.NotNull(other.AdditionalData);
        Assert.Equal(3, other.AdditionalData.Count);
        Assert.Equal("training", other.AdditionalData["mode"].ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_Deserialize_RoundTrip_Succeeds()
    {
        // Arrange
        Other original = new Other
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["mode"] = "test",
                ["test_case"] = "TC-001"
            }
        };

        // Act
        string json = JsonSerializer.Serialize(original, _options);
        Other? deserialized = JsonSerializer.Deserialize<Other>(json, _options);

        // Assert
        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.AdditionalData);
        Assert.Equal(2, deserialized.AdditionalData.Count);
    }
}
