using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

public sealed record Payment
{
    public MoneyAmount Amount { get; init; }

    public PaymentType Type { get; init; }

    public Payment(MoneyAmount amount, PaymentType type)
    {
        Amount = amount;
        Type = type;
    }

    public override string ToString() => $"{Amount.ToStringInvariant()} {Amount.CurrencyIsoCode} ({Type})";
}
