using System.Globalization;
using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

public record MoneyAmount
{
    private static readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;

    public decimal Value { get; private set; }

    public CurrencyCode Currency { get; private set; }

    public string CurrencyIsoCode => Currency.ToIsoString();

    public static MoneyAmount Zero(CurrencyCode currency) => new(0m, currency);

    // EF Core / serializers
    private MoneyAmount()
    {
        Currency = default;
        Value = 0m;
    }

    private MoneyAmount(decimal value, CurrencyCode currency)
    {
        if (currency == CurrencyCode.Unknown)
        {
            throw new ArgumentException("Currency must be explicitly specified.", nameof(currency));
        }

        Currency = currency;

        decimal normalized = Normalize(value);

        Value = normalized;
    }

    /// <summary>
    /// Creates a monetary amount without enforcing sign restrictions. Call
    /// <see cref="EnsureNonNegative"/> or <see cref="EnsureNonPositive"/> afterwards to apply invariants.
    /// </summary>
    public static MoneyAmount Create(decimal value, CurrencyCode currency)
    {
        return new MoneyAmount(value, currency);
    }


    public static MoneyAmount operator +(MoneyAmount left, MoneyAmount right)
    {
        left.EnsureSameCurrency(right);
        return new MoneyAmount(left.Value + right.Value, left.Currency);
    }

    public static MoneyAmount operator -(MoneyAmount left, MoneyAmount right)
    {
        left.EnsureSameCurrency(right);
        return new MoneyAmount(left.Value - right.Value, left.Currency);
    }

    public MoneyAmount Abs() => Value >= 0
        ? this
        : new MoneyAmount(decimal.Negate(Value), Currency).EnsureNonNegative();

    public MoneyAmount Negate()
    {
        return Value == 0
            ? this
            : new MoneyAmount(decimal.Negate(Value), Currency);
    }

    public bool IsNegative => Value < 0;

    public bool IsZero => Value == 0;

    public MoneyAmount EnsureNonNegative(string? paramName = null)
    {
        if (Value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName ?? nameof(Value), Value,
                "Negative amounts are only permitted for storno flows.");
        }

        return this;
    }

    public MoneyAmount EnsureNonPositive(string? paramName = null)
    {
        if (Value > 0)
        {
            throw new ArgumentOutOfRangeException(paramName ?? nameof(Value), Value,
                "Storno amount must be zero or negative after normalization.");
        }

        return this;
    }

    public void EnsureSameCurrency(MoneyAmount other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}.");
        }
    }

    public bool ApproximatelyEquals(MoneyAmount other, decimal tolerance = 0.01m)
    {
        EnsureSameCurrency(other);
        return Math.Abs(Value - other.Value) <= tolerance;
    }

    public string ToStringInvariant() => Value.ToString("0.00###", _invariantCulture);

    public string ToApiString()
    {
        return ToStringInvariant();
    }

    public override string ToString()
    {
        return $"{CurrencyIsoCode} {Value.ToString("0.00###", CultureInfo.GetCultureInfo("de-DE"))}";
    }

    private static decimal Normalize(decimal value)
    {
        int decimalPlaces = GetDecimalPlaces(value);

        return decimalPlaces > 5
            ? decimal.Round(value, 5, MidpointRounding.ToEven)
            : value;
    }

    private static int GetDecimalPlaces(decimal value)
    {
        int[] bits = decimal.GetBits(value);
        return (bits[3] >> 16) & 0xFF;
    }
}
