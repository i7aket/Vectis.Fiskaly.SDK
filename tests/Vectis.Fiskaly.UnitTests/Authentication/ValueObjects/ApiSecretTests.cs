using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.ValueObjects;

/// <summary>
/// Tests for ApiSecret value object validation (Recommendation #2 from Mews analysis).
/// </summary>
/// <remarks>
/// Tests validation improvements from vectis-sdk-deep-analysis-from-mews.md:
/// - ApiSecret: Exact 43 alphanumeric characters (Mews gold standard)
/// - Pattern: ^[0-9A-Za-z]{43}$ (no special characters, dashes, spaces)
/// - Fail-fast validation at construction time
/// </remarks>
public class ApiSecretTests
{
    #region From Method - Valid Inputs

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiSecret43Chars_Succeeds()
    {
        // Arrange: Valid 43-char alphanumeric secret
        string validSecret = "abcdefghijklmnopqrstuvwxyz01234567890ABCDEF"; // Exactly 43 chars

        // Act
        ApiSecret apiSecret = ApiSecret.From(validSecret);

        // Assert
        Assert.Equal(validSecret, apiSecret.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiSecretWithNumbers_Succeeds()
    {
        // Arrange: All numeric characters (43 digits)
        string validSecret = "0123456789012345678901234567890123456789012"; // Exactly 43 chars

        // Act
        ApiSecret apiSecret = ApiSecret.From(validSecret);

        // Assert
        Assert.Equal(validSecret, apiSecret.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiSecretMixedCase_Succeeds()
    {
        // Arrange: Mixed case alphanumeric (realistic Fiskaly format)
        string validSecret = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"; // Exactly 43 chars

        // Act
        ApiSecret apiSecret = ApiSecret.From(validSecret);

        // Assert
        Assert.Equal(validSecret, apiSecret.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiSecretTrimsWhitespace_Succeeds()
    {
        // Arrange: Valid secret with surrounding whitespace
        string secretWithWhitespace = "  abcdefghijklmnopqrstuvwxyz01234567890ABCDEF  ";
        string expectedTrimmed = "abcdefghijklmnopqrstuvwxyz01234567890ABCDEF";

        // Act
        ApiSecret apiSecret = ApiSecret.From(secretWithWhitespace);

        // Assert
        Assert.Equal(expectedTrimmed, apiSecret.Value);
    }

    #endregion

    #region From Method - Invalid Length

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecret42Chars_ThrowsFormatException()
    {
        // Arrange: One character short
        string invalidSecret = new string('a', 42);

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
        Assert.Contains("Current value: 42 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecret44Chars_ThrowsFormatException()
    {
        // Arrange: One character too long
        string invalidSecret = new string('a', 44);

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
        Assert.Contains("Current value: 44 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecret10Chars_ThrowsFormatException()
    {
        // Arrange: Far too short
        string invalidSecret = "tooshort12";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
        Assert.Contains("Current value: 10 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecret100Chars_ThrowsFormatException()
    {
        // Arrange: Far too long
        string invalidSecret = new string('x', 100);

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
        Assert.Contains("Current value: 100 characters", exception.Message);
    }

    #endregion

    #region From Method - Invalid Characters

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecretWithDashes_ThrowsFormatException()
    {
        // Arrange: 43 chars but contains dashes (common mistake)
        string invalidSecret = "test-api-secret-with-dashes-1234567890abc";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecretWithUnderscores_ThrowsFormatException()
    {
        // Arrange: 43 chars but contains underscores (common in API keys, not secrets)
        string invalidSecret = "test_api_secret_with_underscores_12345678";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecretWithSpaces_ThrowsFormatException()
    {
        // Arrange: 43 chars but contains spaces in middle
        string invalidSecret = "test api secret with spaces 1234567890abc";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecretWithSpecialChars_ThrowsFormatException()
    {
        // Arrange: 43 chars but contains special characters
        string invalidSecret = "test!@#$%^&*()secret1234567890abcdefghijk";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiSecretWithDots_ThrowsFormatException()
    {
        // Arrange: 43 chars but contains dots
        string invalidSecret = "test.api.secret.with.dots.1234567890abcdef";

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => ApiSecret.From(invalidSecret));
        Assert.Contains("exactly 43 alphanumeric characters", exception.Message);
    }

    #endregion

    #region From Method - Null/Empty/Whitespace

    [Trait("Category", "Unit")]
    [Fact]
    public void From_NullApiSecret_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ApiSecret.From(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_EmptyApiSecret_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiSecret.From(string.Empty));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WhitespaceOnlyApiSecret_ThrowsArgumentException()
    {
        // Arrange: Only spaces
        string whitespaceSecret = "     ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiSecret.From(whitespaceSecret));
    }

    #endregion

    #region TryFrom Method - Valid Inputs

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidApiSecret_ReturnsTrue()
    {
        // Arrange
        string validSecret = "abcdefghijklmnopqrstuvwxyz01234567890ABCDEF";

        // Act
        bool success = ApiSecret.TryFrom(validSecret, out ApiSecret apiSecret);

        // Assert
        Assert.True(success);
        Assert.Equal(validSecret, apiSecret.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidApiSecretWithWhitespace_ReturnsTrueAndTrims()
    {
        // Arrange
        string secretWithWhitespace = "  AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA  ";
        string expectedTrimmed = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        // Act
        bool success = ApiSecret.TryFrom(secretWithWhitespace, out ApiSecret apiSecret);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedTrimmed, apiSecret.Value);
    }

    #endregion

    #region TryFrom Method - Invalid Inputs

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryFrom_NullOrWhitespace_ReturnsFalse(string? invalidSecret)
    {
        // Act
        bool success = ApiSecret.TryFrom(invalidSecret, out ApiSecret apiSecret);

        // Assert
        Assert.False(success);
        Assert.Equal(default, apiSecret);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("tooshort")] // 8 chars
    [InlineData("exactly42charactersbutnotfortyThreeChars")] // 42 chars
    [InlineData("exactly44characterslongthatexceedstheLimit12")] // 44 chars
    public void TryFrom_InvalidLength_ReturnsFalse(string invalidSecret)
    {
        // Act
        bool success = ApiSecret.TryFrom(invalidSecret, out ApiSecret apiSecret);

        // Assert
        Assert.False(success);
        Assert.Equal(default, apiSecret);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("test-secret-with-dashes-1234567890abcdefg")] // Dashes
    [InlineData("test_secret_with_underscores_12345678901")] // Underscores
    [InlineData("test secret with spaces 1234567890abcdefg")] // Spaces
    [InlineData("test!@#secret$%^with&*()special1234567890")] // Special chars
    public void TryFrom_InvalidCharacters_ReturnsFalse(string invalidSecret)
    {
        // Act
        bool success = ApiSecret.TryFrom(invalidSecret, out ApiSecret apiSecret);

        // Assert
        Assert.False(success);
        Assert.Equal(default, apiSecret);
    }

    #endregion

    #region ToString Method (Security)

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_MasksSecretValue()
    {
        // Arrange
        string validSecret = "abcdefghijklmnopqrstuvwxyz01234567890ABCDEF";
        ApiSecret apiSecret = ApiSecret.From(validSecret);

        // Act
        string stringValue = apiSecret.ToString();

        // Assert
        Assert.DoesNotContain(validSecret, stringValue);
        Assert.Equal("********", stringValue); // Should be masked (8 asterisks)
    }

    #endregion

    #region Equality and Record Behavior

    [Trait("Category", "Unit")]
    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        ApiSecret secret1 = ApiSecret.From("abcdefghijklmnopqrstuvwxyz01234567890ABCDEF");
        ApiSecret secret2 = ApiSecret.From("abcdefghijklmnopqrstuvwxyz01234567890ABCDEF");

        // Act & Assert
        Assert.Equal(secret1, secret2);
        Assert.True(secret1 == secret2);
        Assert.False(secret1 != secret2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        ApiSecret secret1 = ApiSecret.From("abcdefghijklmnopqrstuvwxyz01234567890ABCDEF");
        ApiSecret secret2 = ApiSecret.From("differentSecretValue1234567890ABCDEFGHIJKLM");

        // Act & Assert
        Assert.NotEqual(secret1, secret2);
        Assert.False(secret1 == secret2);
        Assert.True(secret1 != secret2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetHashCode_SameValue_SameHashCode()
    {
        // Arrange
        ApiSecret secret1 = ApiSecret.From("abcdefghijklmnopqrstuvwxyz01234567890ABCDEF");
        ApiSecret secret2 = ApiSecret.From("abcdefghijklmnopqrstuvwxyz01234567890ABCDEF");

        // Act & Assert
        Assert.Equal(secret1.GetHashCode(), secret2.GetHashCode());
    }

    #endregion

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"; // 43 chars

        // Act
        ApiSecret result = ApiSecret.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validInput, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ApiSecret.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiSecret.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiSecret.Parse("   ", null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithInvalidLength_ThrowsFormatException()
    {
        // Arrange: 42 chars (one short of required 43)
        string invalidInput = new string('a', 42);

        // Act & Assert
        Assert.Throws<FormatException>(() => ApiSecret.Parse(invalidInput, null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        // Act
        bool success = ApiSecret.TryParse(validInput, null, out ApiSecret result);

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
        bool success = ApiSecret.TryParse(null, null, out ApiSecret result);

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
        bool success = ApiSecret.TryParse(invalidInput, null, out ApiSecret result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidLength_ReturnsFalse()
    {
        // Arrange: 42 chars (one short of required 43)
        string invalidInput = new string('a', 42);

        // Act
        bool success = ApiSecret.TryParse(invalidInput, null, out ApiSecret result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithInvalidCharacters_ReturnsFalse()
    {
        // Arrange: 43 chars but contains invalid characters (dashes)
        string invalidInput = "test-secret-with-dashes-1234567890abcdefg";

        // Act
        bool success = ApiSecret.TryParse(invalidInput, null, out ApiSecret result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
