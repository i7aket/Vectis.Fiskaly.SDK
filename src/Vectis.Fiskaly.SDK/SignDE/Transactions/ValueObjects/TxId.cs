using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

[JsonConverter(typeof(UuidIdentifierJsonConverterFactory))]
public readonly partial record struct TxId : IUuidIdentifier<TxId>, IParsable<TxId>
{
    public Guid Value { get; }

    private TxId(Guid value) => Value = value;


    public static TxId New() => new(Guid.NewGuid());

    public static TxId From(string uuid)
        => UuidIdentifierHelper.From(uuid, value => new TxId(value), "transaction identifier");

    public static bool TryParse(string? value, out TxId result)
        => TryParse(value, null, out result);

    public static TxId Parse(string s, IFormatProvider? provider)
        => UuidIdentifierHelper.Parse(s, provider, value => new TxId(value), "transaction identifier");

    public static bool TryParse(string? s, IFormatProvider? provider, out TxId result)
        => UuidIdentifierHelper.TryParse(s, provider, value => new TxId(value), out result);

    public override string ToString() => Value.ToString();
}
