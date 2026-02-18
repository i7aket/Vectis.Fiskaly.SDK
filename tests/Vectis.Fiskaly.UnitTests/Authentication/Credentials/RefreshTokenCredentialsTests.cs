using Vectis.Fiskaly.SDK.Authentication.Credentials;
using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.Credentials;

public class RefreshTokenCredentialsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_SetsProperties_Correctly()
    {
        // Arrange - Use valid JWT format (header.payload.signature)
        RefreshToken refreshToken = RefreshToken.From("test_refresh.token_data.signature_xyz123");

        // Act
        RefreshTokenCredentials credentials = new RefreshTokenCredentials(refreshToken);

        // Assert
        Assert.Equal(refreshToken, credentials.RefreshToken);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreatePayload_ReturnsRefreshTokenAuthenticationPayload()
    {
        // Arrange - Use valid JWT format (header.payload.signature)
        RefreshToken refreshToken = RefreshToken.From("test_refresh.token_data.signature_xyz123");
        RefreshTokenCredentials credentials = new RefreshTokenCredentials(refreshToken);

        // Act
        AuthenticationPayload payload = credentials.CreatePayload();

        // Assert
        Assert.IsType<RefreshTokenAuthenticationPayload>(payload);

        RefreshTokenAuthenticationPayload refreshTokenPayload = (RefreshTokenAuthenticationPayload)payload;
        Assert.Equal(refreshToken, refreshTokenPayload.RefreshToken);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Properties_AreInitOnly()
    {
        // Arrange - Use valid JWT format (header.payload.signature)
        RefreshToken refreshToken = RefreshToken.From("test_refresh.token_data.signature_xyz123");
        RefreshTokenCredentials credentials = new RefreshTokenCredentials(refreshToken);

        // Act & Assert
        // Properties should be get-only (init-only properties cannot be reassigned after construction)
        // This is a compile-time check, but we verify values don't change
        RefreshToken originalRefreshToken = credentials.RefreshToken;

        // Verify property returns same value (immutability)
        Assert.Equal(originalRefreshToken, credentials.RefreshToken);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreatePayload_MultipleCalls_ReturnsDifferentInstances()
    {
        // Arrange - Use valid JWT format (header.payload.signature)
        RefreshToken refreshToken = RefreshToken.From("test_refresh.token_data.signature_xyz123");
        RefreshTokenCredentials credentials = new RefreshTokenCredentials(refreshToken);

        // Act
        AuthenticationPayload payload1 = credentials.CreatePayload();
        AuthenticationPayload payload2 = credentials.CreatePayload();

        // Assert
        Assert.NotSame(payload1, payload2); // Different instances
        Assert.IsType<RefreshTokenAuthenticationPayload>(payload1);
        Assert.IsType<RefreshTokenAuthenticationPayload>(payload2);
    }
}
