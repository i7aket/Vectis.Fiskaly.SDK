using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using Vectis.Fiskaly.SDK.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.ValueObjects;

/// <summary>
/// Tests for IUuidIdentifier static validation methods.
/// Uses generic helper to test default interface implementation.
/// </summary>
public class IUuidIdentifierTests
{
    // Helper method to call static interface member via generic constraint
    private static bool CallIsValidUuidV4<T>(string value) where T : IUuidIdentifier<T>
    {
        return T.IsValidUuidV4(value);
    }

    // ========================================
    // IsValidUuidV4 Tests (via TssId)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithValidUuidV4_ReturnsTrue()
    {
        // Arrange
        string validUuid = "550e8400-e29b-41d4-a716-446655440000";

        // Act
        bool result = CallIsValidUuidV4<TssId>(validUuid);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithUppercaseUuidV4_ReturnsTrue()
    {
        // Arrange - case-insensitive validation
        string validUuid = "550E8400-E29B-41D4-A716-446655440000";

        // Act
        bool result = CallIsValidUuidV4<TssId>(validUuid);

        // Assert
        Assert.True(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithNullValue_ReturnsFalse()
    {
        // Act
        bool result = CallIsValidUuidV4<TssId>(null!);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithEmptyString_ReturnsFalse()
    {
        // Act
        bool result = CallIsValidUuidV4<TssId>(string.Empty);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithWhitespace_ReturnsFalse()
    {
        // Act
        bool result = CallIsValidUuidV4<TssId>("   ");

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithInvalidFormat_ReturnsFalse()
    {
        // Arrange - not a UUID at all
        string invalidUuid = "not-a-uuid";

        // Act
        bool result = CallIsValidUuidV4<TssId>(invalidUuid);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithWrongVersion_ReturnsFalse()
    {
        // Arrange - version nibble is '3' instead of '4' (UUIDv3, not v4)
        string uuidV3 = "550e8400-e29b-31d4-a716-446655440000";

        // Act
        bool result = CallIsValidUuidV4<TssId>(uuidV3);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithWrongVariant_ReturnsFalse()
    {
        // Arrange - variant nibble is 'c' instead of 8/9/a/b
        string invalidVariant = "550e8400-e29b-41d4-c716-446655440000";

        // Act
        bool result = CallIsValidUuidV4<TssId>(invalidVariant);

        // Assert
        Assert.False(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void IsValidUuidV4_WithMissingHyphens_ReturnsFalse()
    {
        // Arrange - valid digits but no hyphens
        string noHyphens = "550e8400e29b41d4a716446655440000";

        // Act
        bool result = CallIsValidUuidV4<TssId>(noHyphens);

        // Assert
        Assert.False(result);
    }
}
