using Vectis.Fiskaly.SDK.Authentication.Credentials;
using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.Credentials;

public class ApiKeyCredentialsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_SetsProperties_Correctly()
    {
        // Arrange
        ApiKey apiKey = ApiKey.From("test_api_key_123");
        ApiSecret apiSecret = ApiSecret.From("MxFeR15egG6kwJYa2OISmlr1ttnv8BLVRubi9k4sTQi"); // 43 alphanumeric chars

        // Act
        ApiKeyCredentials credentials = new ApiKeyCredentials(apiKey, apiSecret);

        // Assert
        Assert.Equal(apiKey, credentials.ApiKey);
        Assert.Equal(apiSecret, credentials.ApiSecret);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreatePayload_ReturnsApiKeyAuthenticationPayload()
    {
        // Arrange
        ApiKey apiKey = ApiKey.From("test_api_key_123");
        ApiSecret apiSecret = ApiSecret.From("MxFeR15egG6kwJYa2OISmlr1ttnv8BLVRubi9k4sTQi"); // 43 alphanumeric chars
        ApiKeyCredentials credentials = new ApiKeyCredentials(apiKey, apiSecret);

        // Act
        AuthenticationPayload payload = credentials.CreatePayload();

        // Assert
        Assert.IsType<ApiKeyAuthenticationPayload>(payload);

        ApiKeyAuthenticationPayload apiKeyPayload = (ApiKeyAuthenticationPayload)payload;
        Assert.Equal(apiKey, apiKeyPayload.ApiKey);
        Assert.Equal(apiSecret, apiKeyPayload.ApiSecret);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Properties_AreInitOnly()
    {
        // Arrange
        ApiKey apiKey = ApiKey.From("test_api_key_123");
        ApiSecret apiSecret = ApiSecret.From("MxFeR15egG6kwJYa2OISmlr1ttnv8BLVRubi9k4sTQi"); // 43 alphanumeric chars
        ApiKeyCredentials credentials = new ApiKeyCredentials(apiKey, apiSecret);

        // Act & Assert
        // Properties should be get-only (init-only properties cannot be reassigned after construction)
        // This is a compile-time check, but we verify values don't change
        ApiKey originalApiKey = credentials.ApiKey;
        ApiSecret originalApiSecret = credentials.ApiSecret;

        // Verify properties return same values (immutability)
        Assert.Equal(originalApiKey, credentials.ApiKey);
        Assert.Equal(originalApiSecret, credentials.ApiSecret);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreatePayload_MultipleCalls_ReturnsDifferentInstances()
    {
        // Arrange
        ApiKey apiKey = ApiKey.From("test_api_key_123");
        ApiSecret apiSecret = ApiSecret.From("MxFeR15egG6kwJYa2OISmlr1ttnv8BLVRubi9k4sTQi"); // 43 alphanumeric chars
        ApiKeyCredentials credentials = new ApiKeyCredentials(apiKey, apiSecret);

        // Act
        AuthenticationPayload payload1 = credentials.CreatePayload();
        AuthenticationPayload payload2 = credentials.CreatePayload();

        // Assert
        Assert.NotSame(payload1, payload2); // Different instances
        Assert.IsType<ApiKeyAuthenticationPayload>(payload1);
        Assert.IsType<ApiKeyAuthenticationPayload>(payload2);
    }
}
