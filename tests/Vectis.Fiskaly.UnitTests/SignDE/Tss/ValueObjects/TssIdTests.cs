using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Tss.ValueObjects;

public class TssIdTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void New_CreatesValidTssId()
    {
        TssId tssId = TssId.New();

        Assert.NotEqual(Guid.Empty, tssId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithValidUuid_CreatesTssId()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        TssId tssId = TssId.From(uuid);

        Assert.Equal(Guid.Parse(uuid), tssId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuid_ThrowsArgumentException()
    {
        string invalidUuid = "not-a-uuid";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => TssId.From(invalidUuid));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithNullOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => TssId.From(null!));
        Assert.Throws<ArgumentException>(() => TssId.From(""));
        Assert.Throws<ArgumentException>(() => TssId.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuidVersion_ThrowsArgumentException()
    {
        // UUID v1 (not v4)
        string uuidV1 = "12345678-1234-1abc-9def-123456789012";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => TssId.From(uuidV1));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidUuid_ReturnsTrue()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        bool success = TssId.TryParse(uuid, out TssId tssId);

        Assert.True(success);
        Assert.Equal(Guid.Parse(uuid), tssId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidUuid_ReturnsFalse()
    {
        string invalidUuid = "not-a-uuid";

        bool success = TssId.TryParse(invalidUuid, out TssId tssId);

        Assert.False(success);
        Assert.Equal(default, tssId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullOrWhitespace_ReturnsFalse()
    {
        Assert.False(TssId.TryParse(null, out _));
        Assert.False(TssId.TryParse("", out _));
        Assert.False(TssId.TryParse("   ", out _));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsGuidString()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        TssId tssId = TssId.From(uuid);

        string result = tssId.ToString();

        Assert.Equal(uuid, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameUuid_AreEqual()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        TssId tssId1 = TssId.From(uuid);
        TssId tssId2 = TssId.From(uuid);

        Assert.Equal(tssId1, tssId2);
        Assert.True(tssId1 == tssId2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentUuid_AreNotEqual()
    {
        TssId tssId1 = TssId.From("a1b2c3d4-1234-4abc-9def-123456789012");
        TssId tssId2 = TssId.From("b2c3d4e5-2345-4bcd-9ef0-234567890123");

        Assert.NotEqual(tssId1, tssId2);
        Assert.True(tssId1 != tssId2);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "a1b2c3d4-1234-4abc-9def-123456789012";

        // Act
        TssId result = TssId.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(validInput), result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TssId.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TssId.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TssId.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidUuidFormat_ThrowsArgumentException()
    {
        // Arrange
        string invalidInput = "not-a-uuid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TssId.Parse(invalidInput, null));
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
        bool success = TssId.TryParse(validInput, null, out TssId result);

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
        bool success = TssId.TryParse(null, null, out TssId result);

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
        bool success = TssId.TryParse(invalidInput, null, out TssId result);

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
        bool success = TssId.TryParse(invalidInput, null, out TssId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
