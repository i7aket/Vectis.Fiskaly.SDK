using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;

public class TxResponse
{
    [JsonPropertyName("_id")]
    public TxId? Id { get; init; }
    [JsonPropertyName("state")]
    public TxState? State { get; init; }
    [JsonPropertyName("client_id")]
    public ClientId? ClientId { get; init; }
    [JsonPropertyName("number")]
    public long? Number { get; init; }
    [JsonPropertyName("signature")]
    public TxSignature? Signature { get; init; }
    [JsonPropertyName("tss_serial_number")]
    public TssSerialNumber? TssSerialNumber { get; init; }
    [JsonPropertyName("log")]
    public TxLog? Log { get; init; }
    [JsonPropertyName("qr_code_data")]
    public string? QrCodeData { get; init; }
    [JsonPropertyName("time_start")]
    public DateTimeOffset? TimeStart { get; init; }
    [JsonPropertyName("time_end")]
    public DateTimeOffset? TimeEnd { get; init; }
    [JsonPropertyName("_env")]
    public Env? Env { get; init; }
    [JsonPropertyName("_type")]
    public ResourceType? Type { get; init; }
    [JsonPropertyName("_version")]
    public string? Version { get; init; }
    [JsonPropertyName("client_serial_number")]
    public ClientSerialNumber? ClientSerialNumber { get; init; }
    [JsonPropertyName("latest_revision")]
    public int? LatestRevision { get; init; }
    [JsonPropertyName("revision")]
    public int? Revision { get; init; }
    [JsonPropertyName("metadata")]
    public MetadataCollection? Metadata { get; init; }
    [JsonPropertyName("schema")]
    public TransactionSchema? Schema { get; init; }
    [JsonPropertyName("tss_id")]
    public TssId? TssId { get; init; }
}

public class TxSignature
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }
    [JsonPropertyName("counter")]
    public long? Counter { get; init; }
    [JsonPropertyName("algorithm")]
    public Algorithm? Algorithm { get; init; }
    [JsonPropertyName("public_key")]
    public string? PublicKey { get; init; }
}

public class TxLog
{
    [JsonPropertyName("operation")]
    public TxOperation? Operation { get; init; }
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }
    [JsonPropertyName("timestamp_format")]
    public TimestampFormat? TimestampFormat { get; init; }
}
