using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Responses;

public class TxListResponseTests
{
    private readonly JsonSerializerOptions _options;

    public TxListResponseTests()
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
                          "_type": "TRANSACTION_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        TxListResponse? response = JsonSerializer.Deserialize<TxListResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Empty(response.Data);
        Assert.Equal(0, response.Count);
        Assert.Equal(ResourceType.TransactionList, response.Type);
        Assert.Equal(Env.Test, response.Env);
        Assert.Equal("2.1.33", response.Version);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_SingleTransaction_ReturnsListWithOneItem()
    {
        string json = """
                      {
                          "data": [
                              {
                                  "_id": "a1b2c3d4-1234-4abc-9def-123456789001",
                                  "state": "FINISHED",
                                  "client_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                                  "number": 100,
                                  "time_start": 1704276000,
                                  "time_end": 1704276300,
                                  "_type": "TRANSACTION",
                                  "_env": "TEST",
                                  "_version": "2.1.33",
                                  "tss_serial_number": "TSS001",
                                  "client_serial_number": "CLIENT001",
                                  "latest_revision": 1,
                                  "revision": 1,
                                  "log": {
                                      "operation": "Finish",
                                      "timestamp": 1704276300,
                                      "timestamp_format": "unixTime"
                                  }
                              }
                          ],
                          "count": 1,
                          "_type": "TRANSACTION_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        TxListResponse? response = JsonSerializer.Deserialize<TxListResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Single(response.Data);
        Assert.Equal(1, response.Count);
        Assert.Equal(100, response.Data[0].Number);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MultipleTransactions_ReturnsAllItems()
    {
        string json = """
                      {
                          "data": [
                              {
                                  "_id": "a1b2c3d4-1234-4abc-9def-123456789001",
                                  "state": "ACTIVE",
                                  "number": 100,
                                  "time_start": 1704276000,
                                  "_type": "TRANSACTION",
                                  "_env": "TEST",
                                  "_version": "2.1.33",
                                  "tss_serial_number": "TSS001",
                                  "client_serial_number": "CLIENT001",
                                  "latest_revision": 1,
                                  "revision": 1,
                                  "log": {
                                      "operation": "Start",
                                      "timestamp": 1704276000,
                                      "timestamp_format": "unixTime"
                                  }
                              },
                              {
                                  "_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                                  "state": "FINISHED",
                                  "number": 101,
                                  "time_start": 1704276300,
                                  "time_end": 1704276600,
                                  "_type": "TRANSACTION",
                                  "_env": "TEST",
                                  "_version": "2.1.33",
                                  "tss_serial_number": "TSS001",
                                  "client_serial_number": "CLIENT001",
                                  "latest_revision": 1,
                                  "revision": 1,
                                  "log": {
                                      "operation": "Finish",
                                      "timestamp": 1704276600,
                                      "timestamp_format": "unixTime"
                                  }
                              },
                              {
                                  "_id": "c3d4e5f6-3456-4cde-9ef0-345678900003",
                                  "state": "CANCELLED",
                                  "number": 102,
                                  "time_start": 1704276900,
                                  "time_end": 1704277200,
                                  "_type": "TRANSACTION",
                                  "_env": "TEST",
                                  "_version": "2.1.33",
                                  "tss_serial_number": "TSS001",
                                  "client_serial_number": "CLIENT001",
                                  "latest_revision": 1,
                                  "revision": 1,
                                  "log": {
                                      "operation": "Finish",
                                      "timestamp": 1704277200,
                                      "timestamp_format": "unixTime"
                                  }
                              }
                          ],
                          "count": 3,
                          "_type": "TRANSACTION_LIST",
                          "_env": "TEST",
                          "_version": "2.1.33"
                      }
                      """;

        TxListResponse? response = JsonSerializer.Deserialize<TxListResponse>(json, _options);

        Assert.NotNull(response);
        Assert.Equal(3, response.Data.Count);
        Assert.Equal(3, response.Count);
        Assert.Equal(100, response.Data[0].Number);
        Assert.Equal(101, response.Data[1].Number);
        Assert.Equal(102, response.Data[2].Number);
    }
}
