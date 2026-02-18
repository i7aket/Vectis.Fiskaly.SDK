using Vectis.Fiskaly.SDK.Authentication.Credentials;
using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Authentication.Credentials;

public class ServiceAccountCredentialsTests
{
    // ============================================================================
    // Constructor Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_StoresAllProperties()
    {
        ApiKey apiKey = ApiKey.From("test_key");
        ApiSecret apiSecret = ApiSecret.From("abcXYZ12345678901234567890ABCXYZ12345678901");
        OrganizationId orgId = OrganizationId.From("7b3e4f8a-1234-4abc-9def-123456789012");
        UserId userId = UserId.FromGuid(Guid.Parse("8c4f5a9b-2345-4bcd-9ef0-234567890123"));

        ServiceAccountCredentials credentials = new ServiceAccountCredentials(apiKey, apiSecret, orgId, userId);

        Assert.Equal(apiKey, credentials.ApiKey);
        Assert.Equal(apiSecret, credentials.ApiSecret);
        Assert.Equal(orgId, credentials.OrganizationId);
        Assert.Equal(userId, credentials.UserId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithProductionApiKey_CreatesCredentials()
    {
        ApiKey apiKey = ApiKey.From("prod_9xyz5abc3d12ef45_67gh");
        ApiSecret apiSecret = ApiSecret.From("abcXYZ12345678901234567890ABCXYZ12345678901");
        OrganizationId orgId = OrganizationId.From("7b3e4f8a-1234-4abc-9def-123456789012");
        UserId userId = UserId.FromGuid(Guid.NewGuid());

        ServiceAccountCredentials credentials = new ServiceAccountCredentials(apiKey, apiSecret, orgId, userId);

        Assert.NotNull(credentials);
        Assert.Equal(apiKey, credentials.ApiKey);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithTestApiKey_CreatesCredentials()
    {
        ApiKey apiKey = ApiKey.From("test_bbbbbbbbbbbbbbbbbbbbbbbbbbb_111");
        ApiSecret apiSecret = ApiSecret.From("MxFeR15egG6kwJYa2OISmlr1ttnv8BLVRubi9k4sTQi");
        OrganizationId orgId = OrganizationId.From("34bf24dd-f87b-443c-afb6-37f1aa09524a");
        UserId userId = UserId.FromGuid(Guid.NewGuid());

        ServiceAccountCredentials credentials = new ServiceAccountCredentials(apiKey, apiSecret, orgId, userId);

        Assert.NotNull(credentials);
        Assert.Equal(apiKey, credentials.ApiKey);
    }

    // ============================================================================
    // Payload Creation Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void CreatePayload_ReturnsApiKeyPayload()
    {
        ServiceAccountCredentials credentials = new ServiceAccountCredentials(
            ApiKey.From("test_key"),
            ApiSecret.From("abcXYZ12345678901234567890ABCXYZ12345678901"),
            OrganizationId.From("7b3e4f8a-1234-4abc-9def-123456789012"),
            UserId.FromGuid(Guid.Parse("8c4f5a9b-2345-4bcd-9ef0-234567890123"))
        );

        AuthenticationPayload payload = credentials.CreatePayload();

        Assert.IsType<ApiKeyAuthenticationPayload>(payload);
        Assert.Equal("api_key", payload.Kind);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreatePayload_ContainsApiKeyAndSecret()
    {
        ApiKey apiKey = ApiKey.From("test_key_123");
        ApiSecret apiSecret = ApiSecret.From("abcXYZ12345678901234567890ABCXYZ12345678901");

        ServiceAccountCredentials credentials = new ServiceAccountCredentials(
            apiKey,
            apiSecret,
            OrganizationId.From("7b3e4f8a-1234-4abc-9def-123456789012"),
            UserId.FromGuid(Guid.NewGuid())
        );

        AuthenticationPayload payload = credentials.CreatePayload();
        ApiKeyAuthenticationPayload apiKeyPayload = Assert.IsType<ApiKeyAuthenticationPayload>(payload);

        Assert.Equal(apiKey, apiKeyPayload.ApiKey);
        Assert.Equal(apiSecret, apiKeyPayload.ApiSecret);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CreatePayload_MultipleInvocations_ReturnsSeparateInstances()
    {
        ServiceAccountCredentials credentials = new ServiceAccountCredentials(
            ApiKey.From("test_key"),
            ApiSecret.From("abcXYZ12345678901234567890ABCXYZ12345678901"),
            OrganizationId.From("7b3e4f8a-1234-4abc-9def-123456789012"),
            UserId.FromGuid(Guid.NewGuid())
        );

        AuthenticationPayload payload1 = credentials.CreatePayload();
        AuthenticationPayload payload2 = credentials.CreatePayload();

        Assert.NotSame(payload1, payload2);
        Assert.Equal(payload1.Kind, payload2.Kind);
    }

    // ============================================================================
    // Property Immutability Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Properties_AreImmutableAfterConstruction()
    {
        ApiKey apiKey = ApiKey.From("test_key");
        ApiSecret apiSecret = ApiSecret.From("abcXYZ12345678901234567890ABCXYZ12345678901");
        OrganizationId orgId = OrganizationId.From("7b3e4f8a-1234-4abc-9def-123456789012");
        UserId userId = UserId.FromGuid(Guid.NewGuid());

        ServiceAccountCredentials credentials = new ServiceAccountCredentials(apiKey, apiSecret, orgId, userId);

        // Properties should be get-only (verified by compilation)
        Assert.Equal(apiKey, credentials.ApiKey);
        Assert.Equal(apiSecret, credentials.ApiSecret);
        Assert.Equal(orgId, credentials.OrganizationId);
        Assert.Equal(userId, credentials.UserId);
    }

    // ============================================================================
    // Interface Implementation Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void ServiceAccountCredentials_ImplementsIFiskalyCredentials()
    {
        ServiceAccountCredentials credentials = new ServiceAccountCredentials(
            ApiKey.From("test_key"),
            ApiSecret.From("abcXYZ12345678901234567890ABCXYZ12345678901"),
            OrganizationId.From("7b3e4f8a-1234-4abc-9def-123456789012"),
            UserId.FromGuid(Guid.NewGuid())
        );

        Assert.IsAssignableFrom<IFiskalyCredentials>(credentials);
        Assert.NotNull(credentials.CreatePayload());
    }
}
