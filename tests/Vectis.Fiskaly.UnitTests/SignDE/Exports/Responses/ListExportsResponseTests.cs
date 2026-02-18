using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Responses;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Responses;

public class ListExportsResponseTests
{
    private readonly JsonSerializerOptions _options;

    public ListExportsResponseTests()
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
                          "_type": "EXPORT_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        ListExportsResponse? response = JsonSerializer.Deserialize<ListExportsResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Empty(response.Data);
        Assert.Equal(0, response.Count);
        Assert.Equal(ResourceType.ExportList, response.Type);
        Assert.Equal(Env.Test, response.Env);
        Assert.Equal("2.1.33", response.Version);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_SingleExport_ReturnsListWithOneItem()
    {
        string json = """
                      {
                          "data": [
                              {
                                  "_id": "a1b2c3d4-1234-4abc-9def-123456789001",
                                  "tss_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                                  "state": "COMPLETED",
                                  "time_start": 1704276000,
                                  "time_end": 1704276300,
                                  "_type": "EXPORT",
                                  "_env": "TEST"
                              }
                          ],
                          "count": 1,
                          "_type": "EXPORT_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        ListExportsResponse? response = JsonSerializer.Deserialize<ListExportsResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Single(response.Data);
        Assert.Equal(1, response.Count);
        Assert.NotNull(response.Data[0].TimeStart);
        Assert.NotNull(response.Data[0].TimeEnd);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MultipleExports_ReturnsAllItems()
    {
        string json = """
                      {
                          "data": [
                              {
                                  "_id": "a1b2c3d4-1234-4abc-9def-123456789001",
                                  "tss_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                                  "state": "PENDING",
                                  "_type": "EXPORT",
                                  "_env": "TEST"
                              },
                              {
                                  "_id": "c3d4e5f6-3456-4cde-9ef0-345678900003",
                                  "tss_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                                  "state": "WORKING",
                                  "time_start": 1704276000,
                                  "_type": "EXPORT",
                                  "_env": "TEST"
                              },
                              {
                                  "_id": "d4e5f6a7-4567-4def-9ef0-456789000004",
                                  "tss_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                                  "state": "COMPLETED",
                                  "time_start": 1704276300,
                                  "time_end": 1704276600,
                                  "_type": "EXPORT",
                                  "_env": "TEST"
                              }
                          ],
                          "count": 3,
                          "_type": "EXPORT_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        ListExportsResponse? response = JsonSerializer.Deserialize<ListExportsResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal(3, response.Data.Count);
        Assert.Equal(3, response.Count);
    }
}
