using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReceiptType
{
    /// <summary>
    /// Standard receipt (DSFinV-K BON_TYP: Beleg). Also used for cancellation in TSS systems: send RECEIPT with negative amounts and set storno=true in DSFinV-K.
    /// </summary>
    [JsonStringEnumMemberName("RECEIPT")]
    Receipt = 0,

    /// <summary>Training receipt (BON_TYP: AVTraining).</summary>
    [JsonStringEnumMemberName("TRAINING")]
    Training,

    /// <summary>Transfer/internal movement (BON_TYP: AVTransfer).</summary>
    [JsonStringEnumMemberName("TRANSFER")]
    Transfer,

    /// <summary>Order/pre-order (BON_TYP: AVBestellung).</summary>
    [JsonStringEnumMemberName("ORDER")]
    Order,

    /// <summary>Abort of an unfinished receipt (BON_TYP: AVBelegabbruch).</summary>
    [JsonStringEnumMemberName("CANCELLATION")]
    Cancellation,

    /// <summary>Abort (alias to cancellation, BON_TYP: AVBelegabbruch).</summary>
    [JsonStringEnumMemberName("ABORT")]
    Abort,

    /// <summary>Benefit in kind / non-cash compensation (BON_TYP: AVSachbezug).</summary>
    [JsonStringEnumMemberName("BENEFIT_IN_KIND")]
    BenefitInKind,

    /// <summary>Invoice mode (BON_TYP: AVRechnung).</summary>
    [JsonStringEnumMemberName("INVOICE")]
    Invoice,

    /// <summary>Other process type (BON_TYP: AVSonstige).</summary>
    [JsonStringEnumMemberName("OTHER")]
    Other,

    /// <summary>
    /// Annulation (BON_TYP: AVBelegstorno). Per Fiskaly guidance, this is not used for TSS systems; for cancellation in TSS use Receipt with negative amounts plus storno=true in DSFinV-K.
    /// </summary>
    [JsonStringEnumMemberName("ANNULATION")]
    Annulation
}
