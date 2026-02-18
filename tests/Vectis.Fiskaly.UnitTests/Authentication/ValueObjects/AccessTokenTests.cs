using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.ValueObjects;

public class AccessTokenTests
{
    private const string ValidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0IjoxfQ._-t7L4aln0";

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithValidToken_ReturnsStruct()
    {
        AccessToken token = AccessToken.From(ValidToken);
        Assert.Equal(ValidToken, token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void From_WithInvalidToken_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AccessToken.From("invalid"));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_WithValidToken_ReturnsTrue()
    {
        bool success = AccessToken.TryFrom(ValidToken, out AccessToken token);
        Assert.True(success);
        Assert.Equal(ValidToken, token.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TryFrom_WithInvalidToken_ReturnsFalse()
    {
        bool success = AccessToken.TryFrom("invalid", out AccessToken token);
        Assert.False(success);
        Assert.Equal(default, token);
    }
}
