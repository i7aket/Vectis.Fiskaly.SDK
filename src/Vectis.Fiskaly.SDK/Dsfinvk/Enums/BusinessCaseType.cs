using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Dsfinvk.Enums;

/// <summary>
/// DSFinV-K business transaction type (GV_TYP). Mapped to <c>transactions.data.lines.business_case.type</c> (JSON) and CSV <c>business_cases/lines</c>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BusinessCaseType
{
    /// <summary>Umsatz – regular sale.</summary>
    Umsatz = 0,

    /// <summary>Rabatt – discount line (negative).</summary>
    Rabatt,

    /// <summary>Aufschlag – surcharge/service fee.</summary>
    Aufschlag,

    /// <summary>Pfand – deposit.</summary>
    Pfand,

    /// <summary>PfandRueckzahlung – deposit return.</summary>
    PfandRueckzahlung,

    /// <summary>MehrzweckgutscheinKauf – multi-purpose voucher purchase.</summary>
    MehrzweckgutscheinKauf,

    /// <summary>MehrzweckgutscheinEinloesung – multi-purpose voucher redemption.</summary>
    MehrzweckgutscheinEinloesung,

    /// <summary>EinzweckgutscheinKauf – single-purpose voucher purchase.</summary>
    EinzweckgutscheinKauf,

    /// <summary>EinzweckgutscheinEinloesung – single-purpose voucher redemption.</summary>
    EinzweckgutscheinEinloesung,

    /// <summary>Forderungsentstehung – claim created.</summary>
    Forderungsentstehung,

    /// <summary>Forderungsaufloesung – claim settled.</summary>
    Forderungsaufloesung,

    /// <summary>Anzahlungseinstellung – down payment received.</summary>
    Anzahlungseinstellung,

    /// <summary>Anzahlungsaufloesung – down payment applied.</summary>
    Anzahlungsaufloesung,

    /// <summary>Privateinlage – owner deposit (privat → cash).</summary>
    Privateinlage,

    /// <summary>Privatentnahme – owner withdrawal (cash → privat).</summary>
    Privatentnahme,

    /// <summary>Geldtransit – cash transfer (cash ↔ bank/safe).</summary>
    Geldtransit,

    /// <summary>DifferenzSollIst – cash count difference.</summary>
    DifferenzSollIst,
    /// <summary>TrinkgeldAG – tips received by the employer (inflow only; outflow via Geldtransit/Privatentnahme); usually subject to VAT at the main rate.</summary>
    TrinkgeldAG,
    /// <summary>TrinkgeldAN – tips to the employee (can record both inflow and payout to the employee).</summary>
    TrinkgeldAN,

    /// <summary>Auszahlung – cash out (non-owner).</summary>
    Auszahlung,

    /// <summary>Einzahlung – cash in (non-owner).</summary>
    Einzahlung,

    /// <summary>Rabatt? already defined; next: ZuschussEcht – real subsidy.</summary>
    ZuschussEcht,

    /// <summary>ZuschussUnecht – notional subsidy.</summary>
    ZuschussUnecht,

    /// <summary>Lohnzahlung – wage payment.</summary>
    Lohnzahlung,

    /// <summary>Anfangsbestand – opening balance of the day.</summary>
    Anfangsbestand
}
