using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.ValueObjects;

public class ClientIdTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void New_CreatesValidClientId()
    {
        ClientId clientId = ClientId.New();

        Assert.NotEqual(Guid.Empty, clientId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithValidUuid_CreatesClientId()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        ClientId clientId = ClientId.From(uuid);

        Assert.Equal(Guid.Parse(uuid), clientId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuid_ThrowsArgumentException()
    {
        string invalidUuid = "not-a-uuid";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ClientId.From(invalidUuid));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithNullOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => ClientId.From(null!));
        Assert.Throws<ArgumentException>(() => ClientId.From(""));
        Assert.Throws<ArgumentException>(() => ClientId.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuidVersion_ThrowsArgumentException()
    {
        // UUID v1 (not v4)
        string uuidV1 = "12345678-1234-1abc-9def-123456789012";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ClientId.From(uuidV1));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidUuid_ReturnsTrue()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        bool success = ClientId.TryParse(uuid, out ClientId clientId);

        Assert.True(success);
        Assert.Equal(Guid.Parse(uuid), clientId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidUuid_ReturnsFalse()
    {
        string invalidUuid = "not-a-uuid";

        bool success = ClientId.TryParse(invalidUuid, out ClientId clientId);

        Assert.False(success);
        Assert.Equal(default, clientId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullOrWhitespace_ReturnsFalse()
    {
        Assert.False(ClientId.TryParse(null, out _));
        Assert.False(ClientId.TryParse("", out _));
        Assert.False(ClientId.TryParse("   ", out _));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsGuidString()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        ClientId clientId = ClientId.From(uuid);

        string result = clientId.ToString();

        Assert.Equal(uuid, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameUuid_AreEqual()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        ClientId clientId1 = ClientId.From(uuid);
        ClientId clientId2 = ClientId.From(uuid);

        Assert.Equal(clientId1, clientId2);
        Assert.True(clientId1 == clientId2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentUuid_AreNotEqual()
    {
        ClientId clientId1 = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");
        ClientId clientId2 = ClientId.From("b2c3d4e5-2345-4bcd-9ef0-234567890123");

        Assert.NotEqual(clientId1, clientId2);
        Assert.True(clientId1 != clientId2);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "a1b2c3d4-1234-4abc-9def-123456789012";

        // Act
        ClientId result = ClientId.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(validInput), result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClientId.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ClientId.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ClientId.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidUuidFormat_ThrowsArgumentException()
    {
        // Arrange
        string invalidInput = "not-a-uuid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ClientId.Parse(invalidInput, null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "a1b2c3d4-1234-4abc-9def-123456789012";

        // Act
        bool success = ClientId.TryParse(validInput, null, out ClientId result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(validInput), result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullInput_ReturnsFalse()
    {
        // Act
        bool success = ClientId.TryParse(null, null, out ClientId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidInput_ReturnsFalse()
    {
        // Arrange
        string invalidInput = "";

        // Act
        bool success = ClientId.TryParse(invalidInput, null, out ClientId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidUuidFormat_ReturnsFalse()
    {
        // Arrange
        string invalidInput = "not-a-uuid";

        // Act
        bool success = ClientId.TryParse(invalidInput, null, out ClientId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
