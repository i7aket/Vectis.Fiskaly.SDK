using System.Text.Json;
using Vectis.Fiskaly.SDK.Authentication.Models;

namespace Vectis.Fiskaly.UnitTests.Authentication.Models;

public class AuthenticationResponseTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidResponse_ReturnsPopulatedObject()
    {
        string json = """
                      {
                          "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
                          "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyIjoiMSJ9.DUyFQJvZ8rUY",
                          "access_token_expires_in": 600
                      }
                      """;

        AuthenticationResponse? response = JsonSerializer.Deserialize<AuthenticationResponse>(json);

        Assert.NotNull(response);
        Assert.Equal("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", response!.AccessToken.Value);
        Assert.NotNull(response.RefreshToken);  // RefreshToken is present in this response
        Assert.Equal("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyIjoiMSJ9.DUyFQJvZ8rUY", response.RefreshToken.Value.Value);
        Assert.Equal(600, response.ExpiresIn);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void AccessTokenClaims_Deserialize_WithOrganizationId()
    {
        string json = """
                      {
                          "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
                          "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyIjoiMSJ9.DUyFQJvZ8rUY",
                          "access_token_claims": {
                              "env": "TEST",
                              "organization_id": "7b3e4f8a-1234-4abc-9def-123456789012"
                          }
                      }
                      """;

        AuthenticationResponse? response = JsonSerializer.Deserialize<AuthenticationResponse>(json);

        Assert.NotNull(response?.Claims);
        Assert.Equal("TEST", response.Claims.Environment);
        Assert.NotNull(response.Claims.OrganizationId);
    }

    /// <summary>
    /// Tests minimal AuthenticationResponse with only required fields per OpenAPI spec.
    /// Verifies that refresh_token is truly optional and response can be deserialized without it.
    /// </summary>
    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MinimalResponse_OnlyAccessToken_RefreshTokenIsNull()
    {
        // Arrange - Minimal response per OpenAPI spec (only access_token is required)
        string json = """
                      {
                          "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
                      }
                      """;

        // Act
        AuthenticationResponse? response = JsonSerializer.Deserialize<AuthenticationResponse>(json);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.AccessToken);
        Assert.Equal("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", response.AccessToken.Value);
        Assert.Null(response.RefreshToken);  // Optional field per OpenAPI spec
        Assert.Equal(0, response.ExpiresIn);  // Default value
    }
}
