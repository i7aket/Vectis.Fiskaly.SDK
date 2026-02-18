using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

public sealed record ReceiptItem
{
    public MoneyAmount Amount { get; init; }

    public VatRate VatRate { get; init; }

    public ReceiptItem(MoneyAmount amount, VatRate vatRate)
    {
        Amount = amount;
        VatRate = vatRate;
    }

    public override string ToString() => $"{Amount.ToStringInvariant()} {Amount.CurrencyIsoCode} @ {VatRate}";
}
