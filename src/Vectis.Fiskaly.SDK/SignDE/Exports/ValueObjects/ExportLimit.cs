namespace Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

public readonly record struct ExportLimit : IParsable<ExportLimit>
{
    public const int Min = 1;
    public const int Max = 1_000_000;

    public int Value { get; }

    private ExportLimit(int value)
    {
        Value = value;
    }

    public static ExportLimit From(int value)
    {
        if (value < Min || value > Max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Export limit must be between {Min} and {Max}.");
        }

        return new ExportLimit(value);
    }

    public static bool TryFrom(int value, out ExportLimit limit)
    {
        if (value < Min || value > Max)
        {
            limit = default;
            return false;
        }

        limit = new ExportLimit(value);
        return true;
    }

    public static ExportLimit Parse(string s, IFormatProvider? provider)
    {
        int value = int.Parse(s, provider);
        return From(value);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out ExportLimit result)
    {
        result = default;

        if (!int.TryParse(s, provider, out int value))
        {
            return false;
        }

        return TryFrom(value, out result);
    }

    public override string ToString() => Value.ToString();
}
