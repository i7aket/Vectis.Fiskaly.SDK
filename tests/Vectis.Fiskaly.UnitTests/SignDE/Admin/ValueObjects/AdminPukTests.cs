using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Admin.ValueObjects;

public class AdminPukTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidTenCharacterPuk_ReturnsAdminPuk()
    {
        // Arrange
        string pukValue = "1234567890"; // Minimum 10 characters per SDK spec

        // Act
        AdminPuk puk = AdminPuk.From(pukValue);

        // Assert
        Assert.Equal(pukValue, puk.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("123456789")]  // Too short (9 chars, needs 10+)
    [InlineData("12345678")]   // Too short
    [InlineData("1234567")]    // Too short
    [InlineData("123456")]     // Too short
    [InlineData("12345")]      // Too short
    [InlineData("1234")]       // Too short
    public void From_InvalidPuk_TooShort_ThrowsArgumentException(string shortPuk)
    {
        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => AdminPuk.From(shortPuk));
        Assert.Contains("at least 10 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_NullPuk_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AdminPuk.From(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_EmptyPuk_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPuk.From(string.Empty));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WhitespacePuk_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPuk.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_SamePuk_ReturnsTrue()
    {
        // Arrange
        AdminPuk puk1 = AdminPuk.From("1234567890");
        AdminPuk puk2 = AdminPuk.From("1234567890");

        // Act & Assert
        Assert.Equal(puk1, puk2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetHashCode_SamePuk_ReturnsSameHash()
    {
        // Arrange
        AdminPuk puk1 = AdminPuk.From("1234567890");
        AdminPuk puk2 = AdminPuk.From("1234567890");

        // Act
        int hash1 = puk1.GetHashCode();
        int hash2 = puk2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_MasksPuk_ReturnsStars()
    {
        // Arrange
        AdminPuk puk = AdminPuk.From("1234567890");

        // Act
        string result = puk.ToString();

        // Assert
        Assert.Equal("****", result); // SDK implementation returns 4 stars for privacy
        Assert.DoesNotContain("1234567890", result); // Ensures actual PUK not exposed
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidPuk_ReturnsTrue()
    {
        // Arrange
        string validPuk = "1234567890";

        // Act
        bool success = AdminPuk.TryFrom(validPuk, out AdminPuk puk);

        // Assert
        Assert.True(success);
        Assert.Equal(validPuk, puk.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_InvalidPuk_ReturnsFalse()
    {
        // Arrange
        string invalidPuk = "12345"; // Too short

        // Act
        bool success = AdminPuk.TryFrom(invalidPuk, out AdminPuk puk);

        // Assert
        Assert.False(success);
        Assert.Equal(default, puk);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_NullPuk_ReturnsFalse()
    {
        // Act
        bool success = AdminPuk.TryFrom(null, out AdminPuk puk);

        // Assert
        Assert.False(success);
        Assert.Equal(default, puk);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_PukWithWhitespace_TrimsWhitespace()
    {
        // Arrange
        string pukWithSpaces = "  1234567890  ";

        // Act
        AdminPuk puk = AdminPuk.From(pukWithSpaces);

        // Assert
        Assert.Equal("1234567890", puk.Value); // Should be trimmed
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_LongerPuk_Accepted()
    {
        // Arrange - PUKs can be longer than minimum
        string longPuk = "12345678901234567890";

        // Act
        AdminPuk puk = AdminPuk.From(longPuk);

        // Assert
        Assert.Equal(longPuk, puk.Value);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "1234567890";

        // Act
        AdminPuk result = AdminPuk.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validInput, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AdminPuk.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPuk.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPuk.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithTooShortInput_ThrowsArgumentException()
    {
        // Arrange: Only 9 characters (minimum is 10)
        string invalidInput = "123456789";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPuk.Parse(invalidInput, null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "1234567890";

        // Act
        bool success = AdminPuk.TryParse(validInput, null, out AdminPuk result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(validInput, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithNullInput_ReturnsFalse()
    {
        // Act
        bool success = AdminPuk.TryParse(null, null, out AdminPuk result);

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
        bool success = AdminPuk.TryParse(invalidInput, null, out AdminPuk result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithTooShortInput_ReturnsFalse()
    {
        // Arrange: Only 9 characters (minimum is 10)
        string invalidInput = "123456789";

        // Act
        bool success = AdminPuk.TryParse(invalidInput, null, out AdminPuk result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
