using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Exports.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

[JsonConverter(typeof(TransactionSequenceNumberJsonConverter))]
public readonly record struct TransactionSequenceNumber : IParsable<TransactionSequenceNumber>
{
    public const long Min = 0;
    public const long Max = 9_007_199_254_740_991;

    public long Value { get; }

    private TransactionSequenceNumber(long value)
    {
        Value = value;
    }

    public static TransactionSequenceNumber From(long value)
    {
        if (value < Min || value > Max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Transaction number must be between {Min} and {Max}.");
        }

        return new TransactionSequenceNumber(value);
    }

    public static bool TryFrom(long value, out TransactionSequenceNumber number)
    {
        if (value < Min || value > Max)
        {
            number = default;
            return false;
        }

        number = new TransactionSequenceNumber(value);
        return true;
    }

    public static TransactionSequenceNumber Parse(string s, IFormatProvider? provider)
    {
        long value = long.Parse(s, provider);
        return From(value);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out TransactionSequenceNumber result)
    {
        result = default;

        if (!long.TryParse(s, provider, out long value))
        {
            return false;
        }

        return TryFrom(value, out result);
    }

    public override string ToString() => Value.ToString();
}
