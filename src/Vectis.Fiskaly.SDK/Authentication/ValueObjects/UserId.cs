using Vectis.Fiskaly.SDK.ValueObjects;

namespace Vectis.Fiskaly.SDK.Authentication.ValueObjects;

public readonly partial record struct UserId : IParsable<UserId>
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static UserId From(string uuid)
        => UuidIdentifierHelper.From(uuid, value => new UserId(value), "user identifier");

    public static UserId FromGuid(Guid value) => new(value);

    public static UserId Parse(string value) => From(value);

    public static UserId Parse(string s, IFormatProvider? provider)
        => UuidIdentifierHelper.Parse(s, provider, value => new UserId(value), "user identifier");

    public static bool TryParse(string? value, out UserId userId)
        => TryParse(value, null, out userId);

    public static bool TryParse(string? s, IFormatProvider? provider, out UserId result)
    {
        result = default;

        try
        {
            return UuidIdentifierHelper.TryParse(s, provider, value => new UserId(value), out result);
        }
        catch (ArgumentException)
        {
            result = default;
            return false;
        }
    }

    public override string ToString() => Value.ToString();
}
