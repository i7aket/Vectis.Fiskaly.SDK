using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.ValueObjects;

/// <summary>
/// Tests for ApiKey value object validation (Recommendation #2 from Mews analysis).
/// </summary>
/// <remarks>
/// Tests validation improvements from vectis-sdk-deep-analysis-from-mews.md:
/// - ApiKey: Length 1-512 characters, at least one non-whitespace (Mews pattern)
/// - Pattern: .*[^\s].* (prevents whitespace-only keys)
/// - Fail-fast validation at construction time
/// </remarks>
public class ApiKeyTests
{
    #region From Method - Valid Inputs

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiKeyMinLength_Succeeds()
    {
        // Arrange: Minimum valid length (6 characters)
        string validKey = "test_k";

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiKeyNormalLength_Succeeds()
    {
        // Arrange: Normal API key format (typical Fiskaly format)
        string validKey = "test_aaaaaaaaaaaaaaaaaaaaaaaaaaa_aaa";

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiKeyMaxLength_Succeeds()
    {
        // Arrange: Exactly 512 characters (maximum allowed)
        string validKey = new string('x', 512);

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiKeyWithUnderscores_Succeeds()
    {
        // Arrange: Realistic Fiskaly API key format
        string validKey = "test_57fg4bbn5v9mj8t507t63c1nr_test1";

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiKeyTrimsWhitespace_Succeeds()
    {
        // Arrange: Valid key with surrounding whitespace
        string keyWithWhitespace = "  test_key_valid  ";
        string expectedTrimmed = "test_key_valid";

        // Act
        ApiKey apiKey = ApiKey.From(keyWithWhitespace);

        // Assert
        Assert.Equal(expectedTrimmed, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidApiKeyMixedCase_Succeeds()
    {
        // Arrange: Mixed case (API keys can have any case)
        string validKey = "Test_API_Key_123_XyZ";

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    #endregion

    #region From Method - Invalid Length

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyTooShort5Chars_ThrowsArgumentException()
    {
        // Arrange: One character short of minimum (5 chars)
        string invalidKey = "short";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ApiKey.From(invalidKey));
        Assert.Contains("must be at least 6 characters long", exception.Message);
        Assert.Contains("Current: 5 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyTooShort1Char_ThrowsArgumentException()
    {
        // Arrange: Only 1 character
        string invalidKey = "a";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ApiKey.From(invalidKey));
        Assert.Contains("must be at least 6 characters long", exception.Message);
        Assert.Contains("Current: 1 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyTooLong513Chars_ThrowsArgumentException()
    {
        // Arrange: One character over maximum (513 chars)
        string invalidKey = new string('x', 513);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ApiKey.From(invalidKey));
        Assert.Contains("must not exceed 512 characters", exception.Message);
        Assert.Contains("Current: 513 characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyTooLong1000Chars_ThrowsArgumentException()
    {
        // Arrange: Far over maximum (1000 chars)
        string invalidKey = new string('y', 1000);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => ApiKey.From(invalidKey));
        Assert.Contains("must not exceed 512 characters", exception.Message);
        Assert.Contains("Current: 1000 characters", exception.Message);
    }

    #endregion

    #region From Method - Invalid Characters (Whitespace Only)

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyOnlySpaces_ThrowsArgumentException()
    {
        // Arrange: Only spaces (10 characters)
        // Note: IsNullOrWhitespace check happens before regex validation
        string invalidKey = new string(' ', 10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiKey.From(invalidKey));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyOnlyTabs_ThrowsArgumentException()
    {
        // Arrange: Only tabs (10 characters)
        // Note: IsNullOrWhitespace check happens before regex validation
        string invalidKey = new string('\t', 10);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiKey.From(invalidKey));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyMixedWhitespace_ThrowsArgumentException()
    {
        // Arrange: Mix of spaces, tabs, newlines (no non-whitespace)
        // Note: IsNullOrWhitespace check happens before regex validation
        string invalidKey = "  \t  \n  \r  ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiKey.From(invalidKey));
    }

    #endregion

    #region From Method - Valid Keys with Whitespace (Edge Cases)

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyWithInternalSpaces_Succeeds()
    {
        // Arrange: Valid key with spaces in middle (has non-whitespace chars)
        string validKey = "test key with spaces";

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKeyWithLeadingSpacesThenValid_SucceedsAfterTrim()
    {
        // Arrange: Leading spaces, valid key after trim
        string keyWithLeading = "     valid_key_here";
        string expectedTrimmed = "valid_key_here";

        // Act
        ApiKey apiKey = ApiKey.From(keyWithLeading);

        // Assert
        Assert.Equal(expectedTrimmed, apiKey.Value);
    }

    #endregion

    #region From Method - Null/Empty/Whitespace

    [Trait("Category", "Unit")]
    [Fact]
    public void From_NullApiKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ApiKey.From(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_EmptyApiKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiKey.From(string.Empty));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WhitespaceOnlyApiKey_ThrowsArgumentException()
    {
        // Arrange: Only spaces
        string whitespaceKey = "     ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiKey.From(whitespaceKey));
    }

    #endregion

    #region TryFrom Method - Valid Inputs

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidApiKey_ReturnsTrue()
    {
        // Arrange
        string validKey = "test_key_valid";

        // Act
        bool success = ApiKey.TryFrom(validKey, out ApiKey apiKey);

        // Assert
        Assert.True(success);
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidApiKeyMinLength_ReturnsTrue()
    {
        // Arrange: Exactly 6 characters
        string validKey = "test_k";

        // Act
        bool success = ApiKey.TryFrom(validKey, out ApiKey apiKey);

        // Assert
        Assert.True(success);
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidApiKeyMaxLength_ReturnsTrue()
    {
        // Arrange: Exactly 512 characters
        string validKey = new string('x', 512);

        // Act
        bool success = ApiKey.TryFrom(validKey, out ApiKey apiKey);

        // Assert
        Assert.True(success);
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidApiKeyWithWhitespace_ReturnsTrueAndTrims()
    {
        // Arrange
        string keyWithWhitespace = "  test_aaaaaaaaaaaaaaaaaaaaaaaaaaa_aaa  ";
        string expectedTrimmed = "test_aaaaaaaaaaaaaaaaaaaaaaaaaaa_aaa";

        // Act
        bool success = ApiKey.TryFrom(keyWithWhitespace, out ApiKey apiKey);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedTrimmed, apiKey.Value);
    }

    #endregion

    #region TryFrom Method - Invalid Inputs

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryFrom_NullOrWhitespace_ReturnsFalse(string? invalidKey)
    {
        // Act
        bool success = ApiKey.TryFrom(invalidKey, out ApiKey apiKey);

        // Assert
        Assert.False(success);
        Assert.Equal(default, apiKey);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("short")] // 5 chars (too short)
    [InlineData("a")] // 1 char (too short)
    [InlineData("key")] // 3 chars (too short)
    public void TryFrom_TooShort_ReturnsFalse(string invalidKey)
    {
        // Act
        bool success = ApiKey.TryFrom(invalidKey, out ApiKey apiKey);

        // Assert
        Assert.False(success);
        Assert.Equal(default, apiKey);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_TooLong513Chars_ReturnsFalse()
    {
        // Arrange: 513 characters (one over max)
        string invalidKey = new string('x', 513);

        // Act
        bool success = ApiKey.TryFrom(invalidKey, out ApiKey apiKey);

        // Assert
        Assert.False(success);
        Assert.Equal(default, apiKey);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_OnlyWhitespace10Chars_ReturnsFalse()
    {
        // Arrange: Only spaces (10 characters - passes length check, fails whitespace check)
        string invalidKey = new string(' ', 10);

        // Act
        bool success = ApiKey.TryFrom(invalidKey, out ApiKey apiKey);

        // Assert
        Assert.False(success);
        Assert.Equal(default, apiKey);
    }

    #endregion

    #region ToString Method

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_MasksValue()
    {
        string validKey = "test_key_value";
        ApiKey apiKey = ApiKey.From(validKey);

        // Act
        string stringValue = apiKey.ToString();

        // Assert
        Assert.Equal(new string('*', Math.Min(validKey.Length, 8)), stringValue);
    }

    #endregion

    #region Equality and Record Behavior

    [Trait("Category", "Unit")]
    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        // Arrange
        ApiKey key1 = ApiKey.From("test_key_same");
        ApiKey key2 = ApiKey.From("test_key_same");

        // Act & Assert
        Assert.Equal(key1, key2);
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        ApiKey key1 = ApiKey.From("test_key_one");
        ApiKey key2 = ApiKey.From("test_key_two");

        // Act & Assert
        Assert.NotEqual(key1, key2);
        Assert.False(key1 == key2);
        Assert.True(key1 != key2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetHashCode_SameValue_SameHashCode()
    {
        // Arrange
        ApiKey key1 = ApiKey.From("test_key_same");
        ApiKey key2 = ApiKey.From("test_key_same");

        // Act & Assert
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    #endregion

    #region Boundary Tests (Exact Length Validation)

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKey6CharsExactly_Succeeds()
    {
        // Arrange: Exactly at minimum boundary
        string validKey = "123456";

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKey512CharsExactly_Succeeds()
    {
        // Arrange: Exactly at maximum boundary
        string validKey = new string('a', 512);

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKey511Chars_Succeeds()
    {
        // Arrange: One character below maximum (valid)
        string validKey = new string('b', 511);

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ApiKey7Chars_Succeeds()
    {
        // Arrange: One character above minimum (valid)
        string validKey = "1234567";

        // Act
        ApiKey apiKey = ApiKey.From(validKey);

        // Assert
        Assert.Equal(validKey, apiKey.Value);
    }

    #endregion

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "test_aaaaaaaaaaaaaaaaaaaaaaaaaaa_aaa";

        // Act
        ApiKey result = ApiKey.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validInput, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ApiKey.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiKey.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ApiKey.Parse("   ", null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange
        string validInput = "test_aaaaaaaaaaaaaaaaaaaaaaaaaaa_aaa";

        // Act
        bool success = ApiKey.TryParse(validInput, null, out ApiKey result);

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
        bool success = ApiKey.TryParse(null, null, out ApiKey result);

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
        bool success = ApiKey.TryParse(invalidInput, null, out ApiKey result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithTooShortInput_ReturnsFalse()
    {
        // Arrange: Only 5 characters (below minimum)
        string invalidInput = "short";

        // Act
        bool success = ApiKey.TryParse(invalidInput, null, out ApiKey result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion
}
