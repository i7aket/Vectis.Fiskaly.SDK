using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Responses;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients.Responses;

public class ClientListResponseTests
{
    private readonly JsonSerializerOptions _options;

    public ClientListResponseTests()
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
    public void Deserialize_EmptyList_ReturnsPopulatedObject()
    {
        string json = """
                      {
                          "data": [],
                          "count": 0,
                          "_type": "CLIENT_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        ClientListResponse? response = JsonSerializer.Deserialize<ClientListResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Empty(response.Data);
        Assert.Equal(0, response.Count);
        Assert.Equal(ResourceType.ClientList, response.Type);
        Assert.Equal(Env.Test, response.Env);
        Assert.Equal("2.1.33", response.Version);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_SingleClient_ReturnsListWithOneItem()
    {
        string json = """
                      {
                          "data": [
                              {
                                  "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                                  "serial_number": "KASSE-001",
                                  "state": "REGISTERED",
                                  "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                                  "registered_at": 1609459200,
                                  "_version": "2",
                                  "_type": "CLIENT",
                                  "_env": "TEST"
                              }
                          ],
                          "count": 1,
                          "_version": "2.1.33",
                          "_type": "CLIENT_LIST",
                          "_env": "TEST"
                      }
                      """;

        ClientListResponse? response = JsonSerializer.Deserialize<ClientListResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Single(response.Data);
        Assert.Equal(1, response.Count);
        Assert.Equal("KASSE-001", response.Data[0].SerialNumber?.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MultipleClients_ReturnsAllItems()
    {
        string json = """
                      {
                          "data": [
                              {
                                  "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                                  "serial_number": "KASSE-001",
                                  "state": "REGISTERED",
                                  "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                                  "_version": "2",
                                  "_type": "CLIENT",
                                  "_env": "TEST"
                              },
                              {
                                  "_id": "b2c3d4e5-2345-4bcd-9ef0-234567890123",
                                  "serial_number": "KASSE-002",
                                  "state": "REGISTERED",
                                  "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                                  "_version": "2",
                                  "_type": "CLIENT",
                                  "_env": "TEST"
                              },
                              {
                                  "_id": "c3d4e5f6-3456-4cde-9ef0-345678901234",
                                  "serial_number": "KASSE-003",
                                  "state": "DEREGISTERED",
                                  "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                                  "_version": "2",
                                  "_type": "CLIENT",
                                  "_env": "TEST"
                              }
                          ],
                          "count": 3,
                          "_version": "2.1.33",
                          "_type": "CLIENT_LIST",
                          "_env": "TEST"
                      }
                      """;

        ClientListResponse? response = JsonSerializer.Deserialize<ClientListResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal(3, response.Data.Count);
        Assert.Equal(3, response.Count);
        Assert.Equal("KASSE-001", response.Data[0].SerialNumber?.Value);
        Assert.Equal("KASSE-002", response.Data[1].SerialNumber?.Value);
        Assert.Equal("KASSE-003", response.Data[2].SerialNumber?.Value);
    }
}
