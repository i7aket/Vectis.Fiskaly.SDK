using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Tss.Responses;

public class TssResponse
{
    internal const string ExpectedResourceType = "TSS";
    [JsonPropertyName("_id")]
    public TssId? Id { get; init; }
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    [JsonPropertyName("state")]
    public TssState? State { get; init; }
    [JsonPropertyName("serial_number")]
    public TssSerialNumber? SerialNumber { get; init; }
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }
    [JsonPropertyName("time_creation")]
    public DateTimeOffset? TimeCreation { get; init; }
    [JsonPropertyName("initialized_at")]
    public string? InitializedAt { get; init; }
    [JsonPropertyName("time_init")]
    public DateTimeOffset? TimeInit { get; init; }
    [JsonPropertyName("admin_puk")]
    public AdminPuk? AdminPuk { get; init; }
    [JsonPropertyName("_env")]
    public Env? Env { get; init; }
    [JsonPropertyName("_type")]
    public ResourceType? Type { get; init; }
    [JsonPropertyName("_version")]
    public string? Version { get; init; }
    [JsonPropertyName("bsi_certification_id")]
    public BsiCertificationId? BsiCertificationId { get; init; }
    [JsonPropertyName("certificate")]
    public string? Certificate { get; init; }

    [JsonIgnore]
    public CertificateSerialNumber? CertificateSerialNumber
    {
        get
        {
            if (!_certificateSerialEvaluated && ValueObjects.CertificateSerialNumber.TryFromCertificate(Certificate, out CertificateSerialNumber serial))
            {
                _certificateSerialNumber = serial;
            }

            _certificateSerialEvaluated = true;
            return _certificateSerialNumber;
        }
    }

    private CertificateSerialNumber? _certificateSerialNumber;
    private bool _certificateSerialEvaluated;
    [JsonPropertyName("public_key")]
    public string? PublicKey { get; init; }
    [JsonPropertyName("signature_algorithm")]
    public Algorithm? SignatureAlgorithm { get; init; }
    [JsonPropertyName("signature_counter")]
    [JsonConverter(typeof(NullableLongJsonConverter))]
    public long? SignatureCounter { get; init; }
    [JsonPropertyName("signature_timestamp_format")]
    public TimestampFormat? SignatureTimestampFormat { get; init; }
    [JsonPropertyName("transaction_counter")]
    [JsonConverter(typeof(NullableLongJsonConverter))]
    public long? TransactionCounter { get; init; }
    [JsonPropertyName("transaction_data_encoding")]
    public DataEncoding? TransactionDataEncoding { get; init; }
    [JsonPropertyName("metadata")]
    public MetadataCollection? Metadata { get; init; }
    [JsonPropertyName("max_number_active_transactions")]
    public int? MaxNumberActiveTransactions { get; init; }
    [JsonPropertyName("max_number_registered_clients")]
    public int? MaxNumberRegisteredClients { get; init; }
    [JsonPropertyName("number_active_transactions")]
    public int? NumberActiveTransactions { get; init; }
    [JsonPropertyName("number_registered_clients")]
    public int? NumberRegisteredClients { get; init; }
    [JsonPropertyName("supported_update_variants")]
    public SupportedUpdateVariants? SupportedUpdateVariants { get; init; }
    [JsonPropertyName("time_defective")]
    public DateTimeOffset? TimeDefective { get; init; }
    [JsonPropertyName("time_disable")]
    public DateTimeOffset? TimeDisable { get; init; }
    [JsonPropertyName("time_uninit")]
    public DateTimeOffset? TimeUninit { get; init; }
}
