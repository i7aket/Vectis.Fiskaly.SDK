using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.ValueObjects;

public class OrganizationIdTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void New_CreatesValidOrganizationId()
    {
        OrganizationId organizationId = OrganizationId.New();

        Assert.NotEqual(Guid.Empty, organizationId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithValidUuid_CreatesOrganizationId()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        OrganizationId organizationId = OrganizationId.From(uuid);

        Assert.Equal(Guid.Parse(uuid), organizationId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuid_ThrowsArgumentException()
    {
        string invalidUuid = "not-a-uuid";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => OrganizationId.From(invalidUuid));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithNullOrWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => OrganizationId.From(null!));
        Assert.Throws<ArgumentException>(() => OrganizationId.From(""));
        Assert.Throws<ArgumentException>(() => OrganizationId.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidUuidVersion_ThrowsArgumentException()
    {
        // UUID v1 (not v4)
        string uuidV1 = "12345678-1234-1abc-9def-123456789012";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => OrganizationId.From(uuidV1));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidUuid_ReturnsTrue()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";

        bool success = OrganizationId.TryParse(uuid, out OrganizationId organizationId);

        Assert.True(success);
        Assert.Equal(Guid.Parse(uuid), organizationId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidUuid_ReturnsFalse()
    {
        string invalidUuid = "not-a-uuid";

        bool success = OrganizationId.TryParse(invalidUuid, out OrganizationId organizationId);

        Assert.False(success);
        Assert.Equal(default, organizationId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullOrWhitespace_ReturnsFalse()
    {
        Assert.False(OrganizationId.TryParse(null, out _));
        Assert.False(OrganizationId.TryParse("", out _));
        Assert.False(OrganizationId.TryParse("   ", out _));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsGuidString()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        OrganizationId organizationId = OrganizationId.From(uuid);

        string result = organizationId.ToString();

        Assert.Equal(uuid, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameUuid_AreEqual()
    {
        string uuid = "a1b2c3d4-1234-4abc-9def-123456789012";
        OrganizationId organizationId1 = OrganizationId.From(uuid);
        OrganizationId organizationId2 = OrganizationId.From(uuid);

        Assert.Equal(organizationId1, organizationId2);
        Assert.True(organizationId1 == organizationId2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentUuid_AreNotEqual()
    {
        OrganizationId organizationId1 = OrganizationId.From("a1b2c3d4-1234-4abc-9def-123456789012");
        OrganizationId organizationId2 = OrganizationId.From("b2c3d4e5-2345-4bcd-9ef0-234567890123");

        Assert.NotEqual(organizationId1, organizationId2);
        Assert.True(organizationId1 != organizationId2);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "a1b2c3d4-1234-4abc-9def-123456789012";

        // Act
        OrganizationId result = OrganizationId.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(validInput), result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OrganizationId.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => OrganizationId.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => OrganizationId.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidUuidFormat_ThrowsArgumentException()
    {
        // Arrange
        string invalidInput = "not-a-uuid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => OrganizationId.Parse(invalidInput, null));
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
        bool success = OrganizationId.TryParse(validInput, null, out OrganizationId result);

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
        bool success = OrganizationId.TryParse(null, null, out OrganizationId result);

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
        bool success = OrganizationId.TryParse(invalidInput, null, out OrganizationId result);

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
        bool success = OrganizationId.TryParse(invalidInput, null, out OrganizationId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
