using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Models;

public class ExportJobTests
{
    private readonly JsonSerializerOptions _options;

    public ExportJobTests()
    {
        // Use same JSON options as production (from ServiceCollectionExtensions)
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new MoneyAmountJsonConverter(),
                new NullableLongJsonConverter(),
                new JsonStringEnumConverter(),
                new MetadataCollectionJsonConverter(),
                new UnixEpochDateTimeOffsetConverterFactory()
            }
        };
    }

    // ============================================================================
    // Serialization Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_PendingExport_SetsCorrectState()
    {
        string json = """
                      {
                          "_id": "550e8400-e29b-41d4-a716-446655440000",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "state": "PENDING",
                          "_type": "EXPORT",
                          "_env": "TEST",
                          "_version": "2"
                      }
                      """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.Equal(ExportState.Pending, exportJob.State);
        Assert.Equal("550e8400-e29b-41d4-a716-446655440000", exportJob.Id.ToString());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_CompletedExport_HasAllTimestamps()
    {
        string json = """
                      {
                          "_id": "550e8400-e29b-41d4-a716-446655440000",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "state": "COMPLETED",
                          "time_start": 1609459200,
                          "time_end": 1609459230,
                          "time_request": 1609459190,
                          "time_expiration": 1609545600,
                          "_type": "EXPORT",
                          "_env": "TEST"
                      }
                      """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.Equal(ExportState.Completed, exportJob.State);
        Assert.NotNull(exportJob.TimeStart);
        Assert.NotNull(exportJob.TimeEnd);
        Assert.NotNull(exportJob.TimeRequest);
        Assert.NotNull(exportJob.TimeExpiration);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ErrorExport_HasExceptionAndTimeError()
    {
        string json = """
                      {
                          "_id": "550e8400-e29b-41d4-a716-446655440000",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "state": "ERROR",
                          "exception": "E_ID_NOT_FOUND",
                          "time_error": 1609459200,
                          "_type": "EXPORT",
                          "_env": "TEST"
                      }
                      """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.Equal(ExportState.Error, exportJob.State);
        Assert.NotNull(exportJob.ExceptionCode);
        Assert.Equal(ExportExceptionCode.IdNotFound, exportJob.ExceptionCode.Value);
        Assert.NotNull(exportJob.TimeError);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ExportWithMetadata_PreservesMetadata()
    {
        string json = """
                      {
                          "_id": "550e8400-e29b-41d4-a716-446655440000",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "state": "COMPLETED",
                          "metadata": {
                              "requested_by": "admin@example.com",
                              "purpose": "tax_audit"
                          },
                          "_type": "EXPORT",
                          "_env": "TEST"
                      }
                      """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.NotNull(exportJob.Metadata);
        Assert.Equal("admin@example.com", exportJob.Metadata["requested_by"]);
        Assert.Equal("tax_audit", exportJob.Metadata["purpose"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ExportWithFilters_PreservesFilters()
    {
        string json = """
                      {
                          "_id": "550e8400-e29b-41d4-a716-446655440000",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "state": "COMPLETED",
                          "start_date": 1609459200,
                          "end_date": 1640995200,
                          "client_id": "12345678-1234-4234-9234-123456789012",
                          "_type": "EXPORT",
                          "_env": "TEST"
                      }
                      """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.NotNull(exportJob.StartDate);
        Assert.NotNull(exportJob.EndDate);
        Assert.NotNull(exportJob.ClientId);
        Assert.Equal("12345678-1234-4234-9234-123456789012", exportJob.ClientId.ToString());
    }

    // ============================================================================
    // State Transition Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("PENDING", ExportState.Pending)]
    [InlineData("WORKING", ExportState.Working)]
    [InlineData("COMPLETED", ExportState.Completed)]
    [InlineData("ERROR", ExportState.Error)]
    public void Deserialize_AllStates_AreRecognized(string stateString, ExportState expectedState)
    {
        string json = $$"""
                        {
                            "_id": "550e8400-e29b-41d4-a716-446655440000",
                            "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                            "state": "{{stateString}}",
                            "_type": "EXPORT",
                            "_env": "TEST"
                        }
                        """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.Equal(expectedState, exportJob.State);
    }

    // ============================================================================
    // Property Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_WithEstimatedTimeOfCompletion_PreservesValue()
    {
        string json = """
                      {
                          "_id": "550e8400-e29b-41d4-a716-446655440000",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "state": "WORKING",
                          "estimated_time_of_completion": 1609459260,
                          "_type": "EXPORT",
                          "_env": "TEST"
                      }
                      """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.NotNull(exportJob.EstimatedTimeOfCompletion);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_ExportJob_UsesSnakeCasePropertyNames()
    {
        ExportJob exportJob = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            Env = Env.Test,
            TssId = TssId.From("7b3e4f8a-1234-4abc-9def-123456789012"),
            State = ExportState.Pending,
            Type = ResourceType.Export
        };

        string json = JsonSerializer.Serialize(exportJob, _options);

        Assert.Contains("\"_id\"", json);
        Assert.Contains("\"tss_id\"", json);
        Assert.Contains("\"state\"", json);
        Assert.Contains("\"_type\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_NullOptionalFields_SetsToNull()
    {
        string json = """
                      {
                          "_id": "550e8400-e29b-41d4-a716-446655440000",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "state": "PENDING",
                          "_type": "EXPORT",
                          "_env": "TEST"
                      }
                      """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.Null(exportJob.TimeStart);
        Assert.Null(exportJob.TimeEnd);
        Assert.Null(exportJob.TimeError);
        Assert.Null(exportJob.ExceptionCode);
        Assert.Null(exportJob.Metadata);
        Assert.Null(exportJob.ClientId);
    }

    // ============================================================================
    // Round-trip Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_SerializeAndDeserialize_PreservesAllProperties()
    {
        ExportJob original = new ExportJob
        {
            Id = ExportId.From("550e8400-e29b-41d4-a716-446655440000"),
            TssId = TssId.From("7b3e4f8a-1234-4abc-9def-123456789012"),
            State = ExportState.Completed,
            TimeStart = DateTimeOffset.FromUnixTimeSeconds(1609459200),
            TimeEnd = DateTimeOffset.FromUnixTimeSeconds(1609459230),
            Metadata = MetadataCollection.From(new System.Collections.Generic.Dictionary<string, string>
            {
                ["key1"] = "value1"
            }),
            Type = ResourceType.Export,
            Env = Env.Test
        };

        string json = JsonSerializer.Serialize(original, _options);
        ExportJob? deserialized = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Id.ToString(), deserialized.Id.ToString());
        Assert.Equal(original.State, deserialized.State);
        Assert.Equal(original.TimeStart, deserialized.TimeStart);
        Assert.Equal(original.TimeEnd, deserialized.TimeEnd);
        Assert.NotNull(deserialized.Metadata);
        Assert.Equal("value1", deserialized.Metadata["key1"]);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MinimalExport_SetsRequiredFieldsOnly()
    {
        string json = """
                      {
                          "_id": "550e8400-e29b-41d4-a716-446655440000",
                          "tss_id": "7b3e4f8a-1234-4abc-9def-123456789012",
                          "state": "PENDING",
                          "_type": "EXPORT",
                          "_env": "TEST"
                      }
                      """;

        ExportJob? exportJob = JsonSerializer.Deserialize<ExportJob>(json, _options);

        Assert.NotNull(exportJob);
        Assert.NotEqual(default(ExportId), exportJob.Id);
        Assert.NotEqual(default(TssId), exportJob.TssId);
        Assert.Equal(ExportState.Pending, exportJob.State);
        Assert.Equal(ResourceType.Export, exportJob.Type);
    }
}
