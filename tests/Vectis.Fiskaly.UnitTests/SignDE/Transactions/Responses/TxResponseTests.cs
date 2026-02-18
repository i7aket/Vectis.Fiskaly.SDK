using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Responses;

public class TxResponseTests
{
    private readonly JsonSerializerOptions _options;

    public TxResponseTests()
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
    public void Deserialize_ActiveTransaction_ReturnsPopulatedObject()
    {
        string json = """
                      {
                          "_id": "a1b2c3d4-1234-4abc-9def-123456789001",
                          "state": "ACTIVE",
                          "client_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                          "tss_id": "c4d5e6f7-3456-4cde-9ef0-345678900003",
                          "client_serial_number": "POS-001",
                          "tss_serial_number": "fiskaly-12345678",
                          "number": 123,
                          "revision": 1,
                          "latest_revision": 1,
                          "_type": "TRANSACTION",
                          "_env": "TEST",
                          "_version": "2.1.35",
                          "time_start": 1704276000,
                          "log": {
                              "operation": "Start",
                              "timestamp": 1704276000,
                              "timestamp_format": "unixTime"
                          },
                          "signature": {
                              "value": "VGhpcyBpcyBhIHNpZ25hdHVyZQ==",
                              "counter": 1,
                              "algorithm": "ecdsa-plain-SHA256",
                              "public_key": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE..."
                          }
                      }
                      """;

        TxResponse? response = JsonSerializer.Deserialize<TxResponse>(json, _options);

        TxResponse actual = Assert.IsType<TxResponse>(response);
        Assert.Equal("a1b2c3d4-1234-4abc-9def-123456789001", actual.Id?.ToString());
        Assert.Equal(TxState.Active, actual.State);
        Assert.Equal("b2c3d4e5-2345-4bcd-9ef0-234567890002", actual.ClientId?.ToString());
        Assert.Equal(123L, actual.Number);
        DateTimeOffset? timeStart = actual.TimeStart;
        Assert.NotNull(timeStart);
        Assert.Equal(1704276000, timeStart.Value.ToUnixTimeSeconds());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_FinishedTransaction_HasEndTime()
    {
        string json = """
                      {
                          "_id": "c3d4e5f6-3456-4cde-9ef0-345678900003",
                          "state": "FINISHED",
                          "client_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                          "tss_id": "c4d5e6f7-3456-4cde-9ef0-345678900003",
                          "client_serial_number": "POS-001",
                          "tss_serial_number": "fiskaly-12345678",
                          "number": 456,
                          "revision": 2,
                          "latest_revision": 2,
                          "_type": "TRANSACTION",
                          "_env": "TEST",
                          "_version": "2.1.35",
                          "time_start": 1704276000,
                          "time_end": 1704276300,
                          "log": {
                              "operation": "Finish",
                              "timestamp": 1704276300,
                              "timestamp_format": "unixTime"
                          },
                          "signature": {
                              "value": "VGhpcyBpcyBhIHNpZ25hdHVyZQ==",
                              "counter": 2,
                              "algorithm": "ecdsa-plain-SHA256",
                              "public_key": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE..."
                          }
                      }
                      """;

        TxResponse actual = Assert.IsType<TxResponse>(JsonSerializer.Deserialize<TxResponse>(json, _options));

        Assert.Equal(TxState.Finished, actual.State);
        DateTimeOffset? timeEnd = actual.TimeEnd;
        DateTimeOffset? timeStart = actual.TimeStart;
        Assert.NotNull(timeEnd);
        Assert.NotNull(timeStart);
        Assert.True(timeEnd.Value > timeStart.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WithQrCodeData_ParsesCorrectly()
    {
        string json = """
                      {
                          "_id": "d4e5f6a7-4567-4def-9ef0-456789000004",
                          "state": "FINISHED",
                          "client_id": "b2c3d4e5-2345-4bcd-9ef0-234567890002",
                          "tss_id": "c4d5e6f7-3456-4cde-9ef0-345678900003",
                          "client_serial_number": "POS-001",
                          "tss_serial_number": "fiskaly-12345678",
                          "number": 789,
                          "revision": 3,
                          "latest_revision": 3,
                          "_type": "TRANSACTION",
                          "_env": "TEST",
                          "_version": "2.1.35",
                          "time_start": 1704276000,
                          "log": {
                              "operation": "Finish",
                              "timestamp": 1704276300,
                              "timestamp_format": "unixTime"
                          },
                          "signature": {
                              "value": "VGhpcyBpcyBhIHNpZ25hdHVyZQ==",
                              "counter": 3,
                              "algorithm": "ecdsa-plain-SHA256",
                              "public_key": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE..."
                          },
                          "qr_code_data": "V0;TEST;Kassenbeleg..."
                      }
                      """;

        TxResponse actual = Assert.IsType<TxResponse>(JsonSerializer.Deserialize<TxResponse>(json, _options));

        Assert.NotNull(actual.QrCodeData);
        Assert.StartsWith("V0;TEST;", actual.QrCodeData, StringComparison.Ordinal);
    }

    // ========================================
    // TxSignature Tests
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void TransactionSignature_Deserialize_MapsAllProperties()
    {
        string json = """
                      {
                          "value": "VGhpcyBpcyBhIHNpZ25hdHVyZQ==",
                          "counter": 42,
                          "algorithm": "ecdsa-plain-SHA256",
                          "public_key": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE..."
                      }
                      """;

        TxSignature? signature = JsonSerializer.Deserialize<TxSignature>(json, _options);

        Assert.NotNull(signature);
        Assert.Equal("VGhpcyBpcyBhIHNpZ25hdHVyZQ==", signature.Value);
        Assert.Equal(42, signature.Counter);
        Assert.Equal(Algorithm.EcdsaPlainSha256, signature.Algorithm);
        Assert.Equal("MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...", signature.PublicKey);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TransactionSignature_Serialize_WritesCorrectJson()
    {
        TxSignature signature = new TxSignature
        {
            Value = "VGhpcyBpcyBhIHNpZ25hdHVyZQ==",
            Counter = 42,
            Algorithm = Algorithm.EcdsaPlainSha256,
            PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE..."
        };

        string json = JsonSerializer.Serialize(signature, _options);

        Assert.Contains("\"value\":\"VGhpcyBpcyBhIHNpZ25hdHVyZQ==\"", json);
        Assert.Contains("\"counter\":42", json);
        Assert.Contains("\"algorithm\":\"ecdsa-plain-SHA256\"", json);
        Assert.Contains("\"public_key\":\"MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...\"", json);
    }

    // ========================================
    // TxLog Tests
    // ========================================

    // TODO: TxLogOperation type not implemented yet - uncomment when type is added
    // See OpenAPI spec for TxLog operation enum values
    /*
    [Trait("Category", "Unit")]
    [Fact]
    public void TransactionLog_Deserialize_MapsAllProperties()
    {
        string json = """
                      {
                          "operation": "Finish",
                          "timestamp": 1704276300,
                          "timestamp_format": "unixTime"
                      }
                      """;

        TxLog? log = JsonSerializer.Deserialize<TxLog>(json, _options);

        Assert.NotNull(log);
        Assert.Equal(TxLogOperation.Finish, log.Operation);
        Assert.NotNull(log.Timestamp);
        Assert.Equal(1704276300, log.Timestamp.Value.ToUnixTimeSeconds());
        Assert.Equal("unixTime", log.TimestampFormat);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void TransactionLog_Serialize_WritesCorrectJson()
    {
        TxLog log = new TxLog
        {
            Operation = TxLogOperation.Finish,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1704276300),
            TimestampFormat = "unixTime"
        };

        string json = JsonSerializer.Serialize(log, _options);

        Assert.Contains("\"operation\":\"Finish\"", json);
        Assert.Contains("\"timestamp\":1704276300", json);
        Assert.Contains("\"timestamp_format\":\"unixTime\"", json);
    }
    */
}
