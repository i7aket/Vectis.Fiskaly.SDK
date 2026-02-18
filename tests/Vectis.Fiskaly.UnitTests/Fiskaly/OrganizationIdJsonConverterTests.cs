using System.Text.Json;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class OrganizationIdJsonConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        // OrganizationId has [JsonConverter(typeof(UuidIdentifierJsonConverterFactory))] attribute
        return new JsonSerializerOptions();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidUuidV4String_ReturnsOrganizationId()
    {
        // Valid UUIDv4 (note: version 4 in third group, variant 8/9/a/b in fourth group)
        string uuid = "550e8400-e29b-41d4-a716-446655440000";
        string json = $"\"{uuid}\"";

        OrganizationId organizationId = JsonSerializer.Deserialize<OrganizationId>(json, CreateOptions());

        Assert.Equal(Guid.Parse(uuid), organizationId.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_Null_ReturnsNull()
    {
        string json = "null";

        OrganizationId? organizationId = JsonSerializer.Deserialize<OrganizationId?>(json, CreateOptions());

        Assert.Null(organizationId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_InvalidString_ThrowsJsonException()
    {
        string json = "\"not-a-guid\"";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<OrganizationId>(json, CreateOptions()));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NonUuidV4_ThrowsJsonException()
    {
        // Version 3 UUID (not v4)
        string json = "\"550e8400-e29b-31d4-a716-446655440000\"";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<OrganizationId>(json, CreateOptions()));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_WritesLowercaseUuidString()
    {
        // Valid UUIDv4
        string uuid = "550e8400-e29b-41d4-a716-446655440000";
        OrganizationId organizationId = OrganizationId.From(uuid);

        string json = JsonSerializer.Serialize(organizationId, CreateOptions());

        Assert.Equal($"\"{uuid}\"", json);
    }
}
