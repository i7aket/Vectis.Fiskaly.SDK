using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Dsfinvk.Enums;

/// <summary>
/// DSFinV-K payment type (cash_statement.payment.payment_types[].type).
/// <para>
/// Names mirror the DSFinV-K <c>ZAHLART_TYP</c> values (see DSFinV-K v2.4, Anhang D).
/// For Sign-DE only Cash/NonCash are used: Bar → Cash, everything else → NonCash.
/// </para>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DsfinvkPaymentType
{
    /// <summary>Cash payments (BARGELD), including foreign currency.</summary>
    Bar = 0,
    /// <summary>Generic non-cash (UNBAR) when no finer classification is available.</summary>
    Unbar,
    /// <summary>Debit/EC card (ECKarte/Girocard).</summary>
    ECKarte,
    /// <summary>Credit card (Kreditkarte).</summary>
    Kreditkarte,
    /// <summary>Electronic payment service providers (e.g., PayPal, Stripe) — EL_ZAHLUNGSDIENSTLEISTER.</summary>
    ElZahlungsdienstleister,
    /// <summary>Prepaid / gift cards (Guthabenkarte).</summary>
    Guthabenkarte,
    /// <summary>No payment (KEINE), rare informational cases.</summary>
    Keine
}
