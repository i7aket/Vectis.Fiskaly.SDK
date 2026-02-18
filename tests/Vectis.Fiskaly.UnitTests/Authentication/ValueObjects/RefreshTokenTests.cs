using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.ValueObjects;

public class RefreshTokenTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidJwtToken_ReturnsRefreshToken()
    {
        // Arrange
        string jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

        // Act
        RefreshToken token = RefreshToken.From(jwtToken);

        // Assert
        Assert.Equal(jwtToken, token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_NullToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => RefreshToken.From(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_EmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RefreshToken.From(string.Empty));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WhitespaceToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RefreshToken.From("   "));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Equals_SameToken_ReturnsTrue()
    {
        // Arrange
        string jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.signature";
        RefreshToken token1 = RefreshToken.From(jwtToken);
        RefreshToken token2 = RefreshToken.From(jwtToken);

        // Act & Assert
        Assert.Equal(token1, token2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetHashCode_SameToken_ReturnsSameHash()
    {
        // Arrange
        string jwtToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.signature";
        RefreshToken token1 = RefreshToken.From(jwtToken);
        RefreshToken token2 = RefreshToken.From(jwtToken);

        // Act
        int hash1 = token1.GetHashCode();
        int hash2 = token2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_MasksToken_ShowsOnlyPrefix()
    {
        // Arrange - Use valid JWT format token
        RefreshToken token = RefreshToken.From("test_token.value_123.signature");

        // Act
        string result = token.ToString();

        // Assert
        Assert.StartsWith("refresh_token:", result);
        Assert.Contains("test", result); // Should show first 4 chars
        Assert.Contains("***", result); // Should mask the rest
        Assert.DoesNotContain("_token.value_123.signature", result); // Ensures full token not exposed
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_ValidToken_ReturnsTrue()
    {
        // Arrange - Use valid JWT format token
        string validToken = "valid_refresh.token_value.signature";

        // Act
        bool success = RefreshToken.TryFrom(validToken, out RefreshToken token);

        // Assert
        Assert.True(success);
        Assert.Equal(validToken, token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_NullToken_ReturnsFalse()
    {
        // Act
        bool success = RefreshToken.TryFrom(null, out RefreshToken token);

        // Assert
        Assert.False(success);
        Assert.Equal(default, token);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_EmptyToken_ReturnsFalse()
    {
        // Act
        bool success = RefreshToken.TryFrom(string.Empty, out RefreshToken token);

        // Assert
        Assert.False(success);
        Assert.Equal(default, token);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TokenWithWhitespace_TrimsWhitespace()
    {
        // Arrange - Use valid JWT format with leading/trailing whitespace
        string tokenWithSpaces = "  test_token.value_data.signature  ";

        // Act
        RefreshToken token = RefreshToken.From(tokenWithSpaces);

        // Assert
        Assert.Equal("test_token.value_data.signature", token.Value); // Should be trimmed
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ShortToken_ThrowsArgumentException()
    {
        // Arrange - RefreshToken now enforces minimum length of 10 characters
        string shortToken = "abc";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(shortToken));
        Assert.Contains("too short", exception.Message);
    }

    #region IParsable - Parse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithValidInput_ReturnsInstance()
    {
        // Arrange
        string validInput = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

        // Act
        RefreshToken result = RefreshToken.Parse(validInput, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validInput, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithNullInput_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => RefreshToken.Parse(null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RefreshToken.Parse(string.Empty, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RefreshToken.Parse("   ", null));
    }

    #endregion

    #region IParsable - TryParse Method

    [Trait("Category", "Unit")]
    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Arrange - Use valid JWT format token
        string validInput = "valid_refresh.token_value.signature";

        // Act
        bool success = RefreshToken.TryParse(validInput, null, out RefreshToken result);

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
        bool success = RefreshToken.TryParse(null, null, out RefreshToken result);

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
        bool success = RefreshToken.TryParse(invalidInput, null, out RefreshToken result);

        // Assert
        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion

    #region JWT Structure Validation Tests

    // NOTE: These tests verify JWT structure validation.
    // JWT refresh tokens must follow the format: header.payload.signature (3 parts separated by dots).
    // Invalid JWT structures are rejected with ArgumentException.

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidThreePartJwt_Accepted()
    {
        // Arrange - Valid JWT structure with 3 parts
        string validJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

        // Act
        RefreshToken token = RefreshToken.From(validJwt);

        // Assert
        Assert.Equal(validJwt, token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_SinglePartToken_ThrowsArgumentException()
    {
        // Arrange - Single-part tokens are not valid JWTs
        string singlePart = "notAValidJwtNoDots";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(singlePart));
        Assert.Contains("Expected 3 parts", exception.Message);
        Assert.Contains("found 1 parts", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TwoPartToken_ThrowsArgumentException()
    {
        // Arrange - Two-part tokens are not valid JWTs (missing signature)
        string twoPart = "header.payload";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(twoPart));
        Assert.Contains("Expected 3 parts", exception.Message);
        Assert.Contains("found 2 parts", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_FourPartToken_ThrowsArgumentException()
    {
        // Arrange - Four-part tokens are not valid JWTs
        string fourPart = "part1.part2.part3.part4";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(fourPart));
        Assert.Contains("Expected 3 parts", exception.Message);
        Assert.Contains("found 4 parts", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_EmptyPartsBetweenDots_ThrowsArgumentException()
    {
        // Arrange - Empty parts between dots are not valid JWT components
        string emptyParts = "..";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(emptyParts));
        // Will fail on "too short" (2 chars < 10 min) or "header cannot be empty"
        Assert.True(
            exception.Message.Contains("too short") || exception.Message.Contains("cannot be empty"),
            $"Expected error about length or empty parts, got: {exception.Message}");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_DotsAtStartAndEnd_ThrowsArgumentException()
    {
        // Arrange - Dots at start/end are not valid JWT format
        string invalidDots = ".header.payload.signature.";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(invalidDots));
        Assert.Contains("Expected 3 parts", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ConsecutiveDots_ThrowsArgumentException()
    {
        // Arrange - Consecutive dots are not valid JWT format
        string consecutiveDots = "header..payload...signature";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(consecutiveDots));
        // Will fail on part count or empty part validation
        Assert.True(
            exception.Message.Contains("Expected 3 parts") || exception.Message.Contains("cannot be empty"),
            "Should reject consecutive dots");
    }

    #endregion

    #region Character Set Validation Tests

    // NOTE: JWT tokens must only contain Base64URL characters: A-Z, a-z, 0-9, -, _, and dots as separators.
    // Invalid characters are rejected with ArgumentException.

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidBase64UrlCharacters_Accepted()
    {
        // Arrange - All valid Base64URL characters in proper JWT structure
        string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.payload.signature";

        // Act
        RefreshToken token = RefreshToken.From(validChars);

        // Assert
        Assert.Equal(validChars, token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TokenWithSpacesInMiddle_ThrowsArgumentException()
    {
        // Arrange - Spaces within token (not leading/trailing) are invalid
        string tokenWithSpaces = "header.pay load.signature";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(tokenWithSpaces));
        Assert.Contains("invalid character ' '", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TokenWithSpecialCharacters_ThrowsArgumentException()
    {
        // Arrange - Special characters (@#$%^&*) are not valid in JWTs
        string specialChars = "header@#$.payload%^&.signature*()";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(specialChars));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TokenWithUnicodeCharacters_ThrowsArgumentException()
    {
        // Arrange - Unicode/emoji characters are not valid in JWTs
        string unicodeToken = "header😀.payload🎉.signature✨";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(unicodeToken));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TokenWithNewlines_ThrowsArgumentException()
    {
        // Arrange - Newlines within token are invalid
        string tokenWithNewlines = "header.\npayload.\nsignature";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(tokenWithNewlines));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TokenWithTabs_ThrowsArgumentException()
    {
        // Arrange - Tabs within token are invalid
        string tokenWithTabs = "header.\tpayload.\tsignature";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(tokenWithTabs));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TokenWithControlCharacters_ThrowsArgumentException()
    {
        // Arrange - Control characters are invalid
        string controlChars = "header\x00payload\x01signature";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(controlChars));
        Assert.Contains("invalid character", exception.Message);
    }

    #endregion

    #region Length Validation Tests

    // NOTE: JWT refresh tokens must be between 10-4096 characters.
    // Min length prevents trivially short invalid tokens.
    // Max length prevents DoS attacks via memory exhaustion.

    [Trait("Category", "Unit")]
    [Fact]
    public void From_SingleCharacterToken_ThrowsArgumentException()
    {
        // Arrange - Single character cannot be a valid JWT
        string singleChar = "a";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(singleChar));
        Assert.Contains("too short", exception.Message);
        Assert.Contains("Minimum length is 10", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TwoCharacterToken_ThrowsArgumentException()
    {
        // Arrange - Two characters cannot be a valid JWT
        string twoChars = "ab";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(twoChars));
        Assert.Contains("too short", exception.Message);
        Assert.Contains("Minimum length is 10", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_TypicalJwtLength_Accepted()
    {
        // Arrange - Typical JWT length (100-500 characters) - this SHOULD be accepted
        string typicalJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyNDI2MjIsImRhdGEiOiJ0aGlzIGlzIHNvbWUgYWRkaXRpb25hbCBkYXRhIHRvIG1ha2UgdGhlIHRva2VuIGxvbmdlciJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        RefreshToken token = RefreshToken.From(typicalJwt);

        // Assert
        Assert.Equal(typicalJwt, token.Value);
        Assert.InRange(token.Value.Length, 100, 500);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_VeryLongToken_StillAcceptedUnderLimit()
    {
        // Arrange - Very long tokens are accepted if under 4096 character limit
        string veryLongToken = new string('a', 1000) + "." + new string('b', 1000) + "." + new string('c', 1000);

        // Act
        RefreshToken token = RefreshToken.From(veryLongToken);

        // Assert - Should be accepted (3002 chars < 4096 limit)
        Assert.Equal(veryLongToken, token.Value);
        Assert.True(token.Value.Length > 1000);
        Assert.True(token.Value.Length < 4096);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ExtremelyLongToken_ThrowsArgumentException()
    {
        // Arrange - Extremely long tokens (10000+ chars) are a DoS risk
        string extremelyLongToken = new string('x', 10000);

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(extremelyLongToken));
        Assert.Contains("exceeds maximum length", exception.Message);
        Assert.Contains("4096", exception.Message);
    }

    #endregion

    #region Injection Attack Prevention Tests

    // NOTE: These tests verify that the SDK rejects dangerous payloads via character set validation.
    // Attack patterns containing special characters are rejected, preventing potential downstream vulnerabilities.

    [Trait("Category", "Unit")]
    [Fact]
    public void From_SqlInjectionPattern_ThrowsArgumentException()
    {
        // Arrange - SQL injection patterns contain invalid characters
        string sqlInjection = "'; DROP TABLE users; --";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(sqlInjection));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_SqlInjectionUnion_ThrowsArgumentException()
    {
        // Arrange - SQL UNION injection patterns contain invalid characters
        string sqlUnion = "' UNION SELECT * FROM users --";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(sqlUnion));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_XssScriptTag_ThrowsArgumentException()
    {
        // Arrange - XSS payloads contain invalid characters
        string xssPayload = "<script>alert('xss')</script>";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(xssPayload));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_XssImgTag_ThrowsArgumentException()
    {
        // Arrange - XSS image tag payloads contain invalid characters
        string xssImg = "<img src=x onerror=alert('xss')>";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(xssImg));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_CommandInjectionPattern_ThrowsArgumentException()
    {
        // Arrange - Command injection patterns contain invalid characters
        string cmdInjection = "; rm -rf /";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(cmdInjection));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_CommandInjectionPipe_ThrowsArgumentException()
    {
        // Arrange - Command injection with pipes contains invalid characters
        string cmdPipe = "token | cat /etc/passwd";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(cmdPipe));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_PathTraversalPattern_ThrowsArgumentException()
    {
        // Arrange - Path traversal patterns contain invalid characters
        string pathTraversal = "../../etc/passwd";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(pathTraversal));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_LdapInjectionPattern_ThrowsArgumentException()
    {
        // Arrange - LDAP injection patterns contain invalid characters
        string ldapInjection = "*)(uid=*))(|(uid=*";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(ldapInjection));
        Assert.Contains("invalid character", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_NullByteInjection_ThrowsArgumentException()
    {
        // Arrange - Null byte injection contains invalid characters
        string nullByte = "token\0malicious";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(nullByte));
        Assert.Contains("invalid character", exception.Message);
    }

    #endregion

    #region Edge Cases and Malformed JWT Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void From_OnlyDots_ThrowsArgumentException()
    {
        // Arrange - Token consisting only of dots is invalid
        string onlyDots = "...";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(onlyDots));
        // Will fail on "too short" (3 chars < 10 min) or "empty parts"
        Assert.True(
            exception.Message.Contains("too short") || exception.Message.Contains("cannot be empty"),
            "Should reject dots-only token");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_InvalidBase64Padding_ThrowsArgumentException()
    {
        // Arrange - Invalid Base64 padding contains '=' which is not allowed in Base64Url
        string invalidPadding = "header===.payload==.signature=";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(invalidPadding));
        Assert.Contains("invalid character '='", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_MixedCaseSeparators_CurrentlyAccepted()
    {
        // Arrange
        // NOTE: This is actually valid for JWT (case matters in Base64URL)
        string mixedCase = "Header.Payload.Signature";

        // Act
        RefreshToken token = RefreshToken.From(mixedCase);

        // Assert
        Assert.Equal(mixedCase, token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_NumericOnly_ThrowsArgumentException()
    {
        // Arrange - Numeric-only tokens without dots are not valid JWTs
        string numericOnly = "123456789";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(numericOnly));
        // Will fail on "too short" (9 chars < 10 min) before checking structure
        Assert.True(
            exception.Message.Contains("too short") || exception.Message.Contains("Expected 3 parts"),
            $"Expected error about length or structure, got: {exception.Message}");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_RepeatedCharacters_Accepted()
    {
        // Arrange - Repetitive patterns are technically valid if they have correct structure
        string repeated = "aaaaaaaaaa.bbbbbbbbbb.cccccccccc";

        // Act
        RefreshToken token = RefreshToken.From(repeated);

        // Assert - Should be accepted (valid Base64Url chars, 3 parts, within length limits)
        Assert.Equal(repeated, token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_ValidJwtWithExtraWhitespace_TrimsCorrectly()
    {
        // Arrange
        string jwtWithSpaces = "  eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U  ";

        // Act
        RefreshToken token = RefreshToken.From(jwtWithSpaces);

        // Assert - Leading/trailing whitespace is trimmed (this is expected behavior)
        Assert.Equal("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U", token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_UrlEncodedCharacters_ThrowsArgumentException()
    {
        // Arrange - URL-encoded characters (%) are not valid in Base64Url
        string urlEncoded = "header%20test.payload%2Ftest.signature%3D";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(urlEncoded));
        Assert.Contains("invalid character '%'", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_HtmlEntityEncoded_ThrowsArgumentException()
    {
        // Arrange - HTML entity encoding (&, ;, <, >) contains invalid characters
        string htmlEncoded = "header&lt;test&gt;.payload&amp;.signature&quot;";

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => RefreshToken.From(htmlEncoded));
        Assert.Contains("invalid character", exception.Message);
    }

    #endregion
}
