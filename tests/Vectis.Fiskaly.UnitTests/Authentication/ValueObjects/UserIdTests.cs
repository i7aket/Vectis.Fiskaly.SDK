using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.ValueObjects;

public class UserIdTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void FromGuid_WithValidGuid_CreatesUserId()
    {
        Guid guid = Guid.NewGuid();

        UserId userId = UserId.FromGuid(guid);

        Assert.Equal(guid, userId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FromGuid_WithEmptyGuid_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => UserId.FromGuid(Guid.Empty));

        Assert.Contains("User identifier cannot be empty", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithValidUuidV4String_CreatesUserId()
    {
        // Valid UUIDv4 (version=4 at position 14, variant=8/9/a/b at position 19)
        string validUuidV4 = "550e8400-e29b-41d4-a716-446655440000";

        UserId userId = UserId.From(validUuidV4);

        Assert.Equal(Guid.Parse(validUuidV4), userId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithUuidV1_ThrowsArgumentException()
    {
        // UUIDv1 contains MAC address and timestamp - security issue
        string uuidV1 = "550e8400-e29b-11d4-a716-446655440000"; // version=1

        ArgumentException exception = Assert.Throws<ArgumentException>(() => UserId.From(uuidV1));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
        Assert.Contains("version=4", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithUuidV3_ThrowsArgumentException()
    {
        // UUIDv3 (MD5 hash based)
        string uuidV3 = "550e8400-e29b-31d4-a716-446655440000"; // version=3

        ArgumentException exception = Assert.Throws<ArgumentException>(() => UserId.From(uuidV3));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithUuidV5_ThrowsArgumentException()
    {
        // UUIDv5 (SHA-1 hash based)
        string uuidV5 = "550e8400-e29b-51d4-a716-446655440000"; // version=5

        ArgumentException exception = Assert.Throws<ArgumentException>(() => UserId.From(uuidV5));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidVariant_ThrowsArgumentException()
    {
        // Invalid variant (should be 8/9/a/b, this has 'c')
        string invalidVariant = "550e8400-e29b-41d4-c716-446655440000";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => UserId.From(invalidVariant));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithNullString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => UserId.From(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithEmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => UserId.From(string.Empty));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithWhitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => UserId.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidUuidV4String_CreatesUserId()
    {
        // Valid UUIDv4
        string uuid = "a1b2c3d4-5678-4abc-9def-123456789012";

        UserId userId = UserId.Parse(uuid);

        Assert.Equal(Guid.Parse(uuid), userId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidUuidString_ThrowsArgumentException()
    {
        string invalidUuid = "not-a-uuid";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => UserId.Parse(invalidUuid));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithUuidV1_ThrowsArgumentException()
    {
        string uuidV1 = "a1b2c3d4-5678-1abc-9def-123456789012"; // version=1

        ArgumentException exception = Assert.Throws<ArgumentException>(() => UserId.Parse(uuidV1));

        Assert.Contains("Invalid UUIDv4 format", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidUuidV4String_ReturnsTrue()
    {
        string uuid = "a1b2c3d4-5678-4abc-9def-123456789012";

        bool success = UserId.TryParse(uuid, out UserId userId);

        Assert.True(success);
        Assert.Equal(Guid.Parse(uuid), userId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidUuidString_ReturnsFalse()
    {
        string invalidUuid = "not-a-uuid";

        bool success = UserId.TryParse(invalidUuid, out UserId userId);

        Assert.False(success);
        Assert.Equal(default, userId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithUuidV1_ReturnsFalse()
    {
        string uuidV1 = "a1b2c3d4-5678-1abc-9def-123456789012"; // version=1

        bool success = UserId.TryParse(uuidV1, out UserId userId);

        Assert.False(success);
        Assert.Equal(default, userId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithUuidV3_ReturnsFalse()
    {
        string uuidV3 = "a1b2c3d4-5678-3abc-9def-123456789012"; // version=3

        bool success = UserId.TryParse(uuidV3, out UserId userId);

        Assert.False(success);
        Assert.Equal(default, userId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithUuidV5_ReturnsFalse()
    {
        string uuidV5 = "a1b2c3d4-5678-5abc-9def-123456789012"; // version=5

        bool success = UserId.TryParse(uuidV5, out UserId userId);

        Assert.False(success);
        Assert.Equal(default, userId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidVariant_ReturnsFalse()
    {
        string invalidVariant = "a1b2c3d4-5678-4abc-cdef-123456789012"; // invalid variant

        bool success = UserId.TryParse(invalidVariant, out UserId userId);

        Assert.False(success);
        Assert.Equal(default, userId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullString_ReturnsFalse()
    {
        bool success = UserId.TryParse(null, out UserId userId);

        Assert.False(success);
        Assert.Equal(default, userId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithEmptyGuid_ReturnsFalse()
    {
        bool success = UserId.TryParse(Guid.Empty.ToString(), out UserId userId);

        Assert.False(success);
        Assert.Equal(default, userId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsGuidString()
    {
        Guid guid = Guid.NewGuid();
        UserId userId = UserId.FromGuid(guid);

        string result = userId.ToString();

        Assert.Equal(guid.ToString(), result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameGuid_AreEqual()
    {
        Guid guid = Guid.NewGuid();
        UserId userId1 = UserId.FromGuid(guid);
        UserId userId2 = UserId.FromGuid(guid);

        Assert.Equal(userId1, userId2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentGuid_AreNotEqual()
    {
        UserId userId1 = UserId.FromGuid(Guid.NewGuid());
        UserId userId2 = UserId.FromGuid(Guid.NewGuid());

        Assert.NotEqual(userId1, userId2);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "a1b2c3d4-1234-4abc-9def-123456789012";

        // Act
        UserId result = UserId.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Parse(validInput), result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => UserId.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => UserId.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => UserId.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidUuidFormat_ThrowsArgumentException()
    {
        // Arrange
        string invalidInput = "not-a-uuid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => UserId.Parse(invalidInput, null));
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
        bool success = UserId.TryParse(validInput, null, out UserId result);

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
        bool success = UserId.TryParse(null, null, out UserId result);

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
        bool success = UserId.TryParse(invalidInput, null, out UserId result);

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
        bool success = UserId.TryParse(invalidInput, null, out UserId result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
