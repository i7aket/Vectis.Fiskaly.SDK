using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Responses;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.Responses;

public class ClientResponseTests
{
    private readonly JsonSerializerOptions _options;

    public ClientResponseTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters =
            {
                new UnixEpochDateTimeOffsetConverterFactory(),
                new JsonStringEnumConverter()
            }
        };
    }
    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_RegisteredClient_ReturnsPopulatedObject()
    {
        string json = """
                      {
                          "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                          "serial_number": "KASSE-001",
                          "state": "REGISTERED",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "_type": "CLIENT",
                          "_env": "TEST",
                          "_version": "2.1.33",
                          "time_creation": 1609459200,
                          "time_update": 1609459260
                      }
                      """;

        ClientResponse? response = JsonSerializer.Deserialize<ClientResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal("KASSE-001", response.SerialNumber?.Value);
        Assert.Equal(ClientState.Registered, response.State);
        Assert.Equal(ResourceType.Client, response.Type);
        Assert.Equal(Env.Test, response.Env);
        Assert.Equal("2.1.33", response.Version);
        Assert.NotNull(response.TimeCreation);
        Assert.NotNull(response.TimeUpdate);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_DeregisteredClient_HasDeregisteredState()
    {
        string json = """
                      {
                          "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                          "serial_number": "POS-999",
                          "state": "DEREGISTERED",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "_type": "CLIENT",
                          "_env": "TEST",
                          "_version": "2.1.33",
                          "time_creation": 1609459200,
                          "time_update": 1609459260
                      }
                      """;

        ClientResponse? response = JsonSerializer.Deserialize<ClientResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal(ClientState.Deregistered, response.State);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WithTimestamps_ParsesCorrectly()
    {
        string json = """
                      {
                          "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                          "serial_number": "TERMINAL-1",
                          "state": "REGISTERED",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "_type": "CLIENT",
                          "_env": "TEST",
                          "_version": "2.1.33",
                          "time_creation": 1609459200,
                          "time_update": 1609462800
                      }
                      """;

        ClientResponse? response = JsonSerializer.Deserialize<ClientResponse>(json, _options);

        Assert.NotNull(response);
        Assert.NotNull(response.TimeCreation);
        Assert.NotNull(response.TimeUpdate);
        Assert.True(response.TimeUpdate > response.TimeCreation);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PropertySetters_CanSetAllProperties()
    {
        ClientId clientId = ClientId.From("a1b2c3d4-1234-4abc-9def-123456789012");
        ClientSerialNumber serialNumber = ClientSerialNumber.From("KASSE-001");
        TssId tssId = TssId.From("7b3e4f8a-1234-4abc-9def-123456789012");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        ClientResponse response = new ClientResponse
        {
            Id = clientId,
            SerialNumber = serialNumber,
            State = ClientState.Registered,
            TssId = tssId,
            TimeCreation = now,
            TimeUpdate = now,
            Env = Env.Test,
            Type = ResourceType.Client,
            Version = "v2"
        };

        Assert.Equal(clientId, response.Id);
        Assert.Equal(serialNumber, response.SerialNumber);
        Assert.Equal(ClientState.Registered, response.State);
        Assert.Equal(tssId, response.TssId);
        Assert.Equal(now, response.TimeCreation);
    }
}
