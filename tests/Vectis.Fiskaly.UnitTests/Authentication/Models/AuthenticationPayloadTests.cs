using System.Text.Json;
using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.Models;

public class AuthenticationPayloadTests
{
    private readonly JsonSerializerOptions _options;

    public AuthenticationPayloadTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // ========================================
    // ApiKeyAuthenticationPayload Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ApiKeyPayload_Kind_ReturnsApiKey()
    {
        // Arrange
        ApiKey apiKey = ApiKey.From("test_api_key");
        ApiSecret apiSecret = ApiSecret.From("abcde1234567890abcde1234567890abcde12345678"); // 43 chars
        ApiKeyAuthenticationPayload payload = new ApiKeyAuthenticationPayload(apiKey, apiSecret);

        // Act
        string kind = payload.Kind;

        // Assert
        Assert.Equal("api_key", kind);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ApiKeyPayload_ApiKey_ReturnsUnderlyingValue()
    {
        // Arrange
        ApiKey apiKey = ApiKey.From("test_api_key_123");
        ApiSecret apiSecret = ApiSecret.From("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"); // 43 chars
        ApiKeyAuthenticationPayload payload = new ApiKeyAuthenticationPayload(apiKey, apiSecret);

        // Act
        string value = payload.ApiKey.Value;

        // Assert
        Assert.Equal("test_api_key_123", value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ApiKeyPayload_ApiSecret_ReturnsUnderlyingValue()
    {
        // Arrange
        ApiKey apiKey = ApiKey.From("test_api_key");
        ApiSecret apiSecret = ApiSecret.From("MxFeR15egG6kwJYa2OISmlr1ttnv8BLVRubi9k4sTQi"); // 43 chars
        ApiKeyAuthenticationPayload payload = new ApiKeyAuthenticationPayload(apiKey, apiSecret);

        // Act
        string value = payload.ApiSecret.Value;

        // Assert
        Assert.Equal("MxFeR15egG6kwJYa2OISmlr1ttnv8BLVRubi9k4sTQi", value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ApiKeyPayload_Serialize_ContainsCorrectJsonProperties()
    {
        // Arrange
        ApiKey apiKey = ApiKey.From("my_api_key");
        ApiSecret apiSecret = ApiSecret.From("abcXYZ12345678901234567890ABCXYZ12345678901"); // 43 chars
        ApiKeyAuthenticationPayload payload = new ApiKeyAuthenticationPayload(apiKey, apiSecret);

        // Act
        string json = JsonSerializer.Serialize(payload, _options);

        // Assert
        Assert.Contains("\"api_key\"", json);
        Assert.Contains("\"my_api_key\"", json);
        Assert.Contains("\"api_secret\"", json);
        Assert.Contains("\"abcXYZ12345678901234567890ABCXYZ12345678901\"", json);
        Assert.DoesNotContain("\"kind\"", json); // Kind is JsonIgnore
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RefreshTokenPayload_Kind_ReturnsRefreshToken()
    {
        // Arrange - Use valid JWT format (header.payload.signature)
        RefreshToken token = RefreshToken.From("test_refresh.token_value.signature123");
        RefreshTokenAuthenticationPayload payload = new RefreshTokenAuthenticationPayload(token);

        // Act
        string kind = payload.Kind;

        // Assert
        Assert.Equal("refresh_token", kind);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RefreshTokenPayload_RefreshToken_ReturnsUnderlyingValue()
    {
        // Arrange - Use valid JWT format (header.payload.signature)
        RefreshToken token = RefreshToken.From("my_refresh.token_data.signature456");
        RefreshTokenAuthenticationPayload payload = new RefreshTokenAuthenticationPayload(token);

        // Act
        string value = payload.RefreshToken.Value;

        // Assert
        Assert.Equal("my_refresh.token_data.signature456", value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void RefreshTokenPayload_Serialize_ContainsCorrectJsonProperty()
    {
        // Arrange - Use valid JWT format (header.payload.signature)
        RefreshToken token = RefreshToken.From("my_token_header.payload_data.signature789");
        RefreshTokenAuthenticationPayload payload = new RefreshTokenAuthenticationPayload(token);

        // Act
        string json = JsonSerializer.Serialize(payload, _options);

        // Assert
        Assert.Contains("\"refresh_token\"", json);
        Assert.Contains("\"my_token_header.payload_data.signature789\"", json);
        Assert.DoesNotContain("\"kind\"", json); // Kind is JsonIgnore
    }
}
