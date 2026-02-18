namespace Vectis.Fiskaly.SDK.SignDE.Common.ValueObjects;

public readonly record struct BusinessName : IParsable<BusinessName>
{
    public const int MaxLength = 60;

    public string Value { get; }

    private BusinessName(string value)
    {
        Value = value;
    }

    public static BusinessName From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        string normalized = value.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Business name cannot be empty.", nameof(value));
        }

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Business name cannot exceed {MaxLength} characters. " +
                $"Provided length: {normalized.Length}.", nameof(value));
        }

        return new BusinessName(normalized);
    }

    public static BusinessName Parse(string s, IFormatProvider? provider) => From(s);

    public static bool TryParse(string? s, IFormatProvider? provider, out BusinessName result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        string normalized = s.Trim();
        if (normalized.Length == 0 || normalized.Length > MaxLength)
        {
            return false;
        }

        result = new BusinessName(normalized);
        return true;
    }

    public override string ToString() => Value;
}
