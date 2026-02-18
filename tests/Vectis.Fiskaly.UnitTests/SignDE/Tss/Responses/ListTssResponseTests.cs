using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Responses;

namespace Vectis.Fiskaly.UnitTests.SignDE.Tss.Responses;

public class ListTssResponseTests
{
    private readonly JsonSerializerOptions _options;

    public ListTssResponseTests()
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
                          "_type": "TSS_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        ListTssResponse? response = JsonSerializer.Deserialize<ListTssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Empty(response.Data);
        Assert.Equal(0, response.Count);
        Assert.Equal(ResourceType.TssList, response.Type);
        Assert.Equal(Env.Test, response.Env);
        Assert.Equal("2.1.33", response.Version);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_SingleTss_ReturnsListWithOneItem()
    {
        string json = """
                      {
                          "data": [
                              {
                                  "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                                  "description": "Main TSS",
                                  "state": "INITIALIZED",
                                  "serial_number": "fiskaly-12345678",
                                  "_type": "TSS",
                                  "_env": "TEST"
                              }
                          ],
                          "count": 1,
                          "_type": "TSS_LIST",
                          "_env": "TEST"
                      }
                      """;

        ListTssResponse? response = JsonSerializer.Deserialize<ListTssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Single(response.Data);
        Assert.Equal(1, response.Count);
        Assert.Equal("Main TSS", response.Data[0].Description);
        Assert.Equal("fiskaly-12345678", response.Data[0].SerialNumber?.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MultipleTss_ReturnsAllItems()
    {
        string json = """
                      {
                          "data": [
                              {
                                  "_id": "a1b2c3d4-1234-4abc-9def-123456789012",
                                  "description": "TSS One",
                                  "state": "INITIALIZED",
                                  "_type": "TSS",
                                  "_env": "TEST",
                                  "_version": "2.1.33",
                                  "time_creation": 1704276000
                              },
                              {
                                  "_id": "b2c3d4e5-2345-4bcd-9ef0-234567890123",
                                  "description": "TSS Two",
                                  "state": "CREATED",
                                  "_type": "TSS",
                                  "_env": "TEST",
                                  "_version": "2.1.33",
                                  "time_creation": 1704276100
                              },
                              {
                                  "_id": "c3d4e5f6-3456-4cde-9ef0-345678901234",
                                  "description": "TSS Three",
                                  "state": "DISABLED",
                                  "_type": "TSS",
                                  "_env": "TEST",
                                  "_version": "2.1.33",
                                  "time_creation": 1704276200
                              }
                          ],
                          "count": 3,
                          "_type": "TSS_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        ListTssResponse? response = JsonSerializer.Deserialize<ListTssResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal(3, response.Data.Count);
        Assert.Equal(3, response.Count);
        Assert.Equal("TSS One", response.Data[0].Description);
        Assert.Equal("TSS Two", response.Data[1].Description);
        Assert.Equal("TSS Three", response.Data[2].Description);
    }
}
