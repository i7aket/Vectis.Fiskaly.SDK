using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExportExceptionCode
{
    [JsonStringEnumMemberName("E_UNEXPECTED")]
    Unexpected,
    [JsonStringEnumMemberName("E_ID_NOT_FOUND")]
    IdNotFound,
    [JsonStringEnumMemberName("E_BAD_REQUEST")]
    BadRequest,
    [JsonStringEnumMemberName("E_INTERNAL")]
    Internal,
    [JsonStringEnumMemberName("E_TRANSACTION_ID_NOT_FOUND")]
    TransactionIdNotFound,
    [JsonStringEnumMemberName("E_NO_DATA_AVAILABLE")]
    NoDataAvailable,
    [JsonStringEnumMemberName("E_TOO_MANY_RECORDS")]
    TooManyRecords,
    [JsonStringEnumMemberName("E_ALREADY_PROCESSING")]
    AlreadyProcessing,
    [JsonStringEnumMemberName("E_LOGS_NOT_DELETED")]
    LogsNotDeleted,
    [JsonStringEnumMemberName("E_EXPORT_PROCESSING_TIMEOUT")]
    ExportProcessingTimeout
}
