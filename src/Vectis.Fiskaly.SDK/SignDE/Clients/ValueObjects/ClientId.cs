using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

[JsonConverter(typeof(UuidIdentifierJsonConverterFactory))]
public readonly partial record struct ClientId : IUuidIdentifier<ClientId>, IParsable<ClientId>
{
    public Guid Value { get; }

    private ClientId(Guid value) => Value = value;


    public static ClientId New() => new(Guid.NewGuid());

    public static ClientId From(string uuid)
        => UuidIdentifierHelper.From(uuid, value => new ClientId(value), "client identifier");

    public static bool TryParse(string? value, out ClientId result)
    {
        return TryParse(value, null, out result);
    }

    public static ClientId Parse(string s, IFormatProvider? provider)
        => UuidIdentifierHelper.Parse(s, provider, value => new ClientId(value), "client identifier");

    public static bool TryParse(string? s, IFormatProvider? provider, out ClientId result)
        => UuidIdentifierHelper.TryParse(s, provider, value => new ClientId(value), out result);

    public override string ToString() => Value.ToString();
}
