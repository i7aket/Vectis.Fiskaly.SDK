using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Admin.ValueObjects;

public class AdminPinTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidSixCharacterPin_ReturnsAdminPin()
    {
        // Arrange
        string pinValue = "123456"; // Minimum 6 characters per SDK spec

        // Act
        AdminPin pin = AdminPin.From(pinValue);

        // Assert
        Assert.Equal(pinValue, pin.Value);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("12345")]     // Too short (5 chars, needs 6+)
    [InlineData("1234")]      // Too short
    [InlineData("123")]       // Too short
    [InlineData("12")]        // Too short
    [InlineData("1")]         // Too short
    public void From_InvalidPin_TooShort_ThrowsArgumentException(string shortPin)
    {
        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => AdminPin.From(shortPin));
        Assert.Contains("at least 6 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_NullPin_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AdminPin.From(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_EmptyPin_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPin.From(string.Empty));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WhitespacePin_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPin.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_SamePin_ReturnsTrue()
    {
        // Arrange
        AdminPin pin1 = AdminPin.From("123456");
        AdminPin pin2 = AdminPin.From("123456");

        // Act & Assert
        Assert.Equal(pin1, pin2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetHashCode_SamePin_ReturnsSameHash()
    {
        // Arrange
        AdminPin pin1 = AdminPin.From("123456");
        AdminPin pin2 = AdminPin.From("123456");

        // Act
        int hash1 = pin1.GetHashCode();
        int hash2 = pin2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_MasksPin_ReturnsStars()
    {
        // Arrange
        AdminPin pin = AdminPin.From("123456");

        // Act
        string result = pin.ToString();

        // Assert
        Assert.Equal("****", result); // SDK implementation returns 4 stars for privacy
        Assert.DoesNotContain("123456", result); // Ensures actual PIN not exposed
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidPin_ReturnsTrue()
    {
        // Arrange
        string validPin = "123456";

        // Act
        bool success = AdminPin.TryFrom(validPin, out AdminPin pin);

        // Assert
        Assert.True(success);
        Assert.Equal(validPin, pin.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_InvalidPin_ReturnsFalse()
    {
        // Arrange
        string invalidPin = "12345"; // Too short

        // Act
        bool success = AdminPin.TryFrom(invalidPin, out AdminPin pin);

        // Assert
        Assert.False(success);
        Assert.Equal(default, pin);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_NullPin_ReturnsFalse()
    {
        // Act
        bool success = AdminPin.TryFrom(null, out AdminPin pin);

        // Assert
        Assert.False(success);
        Assert.Equal(default, pin);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_PinWithWhitespace_TrimsWhitespace()
    {
        // Arrange
        string pinWithSpaces = "  123456  ";

        // Act
        AdminPin pin = AdminPin.From(pinWithSpaces);

        // Assert
        Assert.Equal("123456", pin.Value); // Should be trimmed
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "123456";

        // Act
        AdminPin result = AdminPin.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validInput, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AdminPin.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPin.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPin.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithTooShortInput_ThrowsArgumentException()
    {
        // Arrange: Only 5 characters (minimum is 6)
        string invalidInput = "12345";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => AdminPin.Parse(invalidInput, null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "123456";

        // Act
        bool success = AdminPin.TryParse(validInput, null, out AdminPin result);

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
        bool success = AdminPin.TryParse(null, null, out AdminPin result);

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
        bool success = AdminPin.TryParse(invalidInput, null, out AdminPin result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithTooShortInput_ReturnsFalse()
    {
        // Arrange: Only 5 characters (minimum is 6)
        string invalidInput = "12345";

        // Act
        bool success = AdminPin.TryParse(invalidInput, null, out AdminPin result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
