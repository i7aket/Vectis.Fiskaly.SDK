using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.ValueObjects;

namespace Vectis.Fiskaly.SDK.Authentication.ValueObjects;

[JsonConverter(typeof(UuidIdentifierJsonConverterFactory))]
public readonly partial record struct OrganizationId : IUuidIdentifier<OrganizationId>, IParsable<OrganizationId>
{
    public Guid Value { get; }

    private OrganizationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Organization identifier cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static OrganizationId New() => new(Guid.NewGuid());

    public static OrganizationId From(string uuid)
        => UuidIdentifierHelper.From(uuid, value => new OrganizationId(value), "organization identifier");

    public static bool TryParse(string? value, out OrganizationId result)
        => TryParse(value, null, out result);

    public static OrganizationId Parse(string s, IFormatProvider? provider)
        => UuidIdentifierHelper.Parse(s, provider, value => new OrganizationId(value), "organization identifier");

    public static bool TryParse(string? s, IFormatProvider? provider, out OrganizationId result)
    {
        result = default;

        try
        {
            return UuidIdentifierHelper.TryParse(s, provider, value => new OrganizationId(value), out result);
        }
        catch (ArgumentException)
        {
            result = default;
            return false;
        }
    }

    public override string ToString() => Value.ToString();
}
