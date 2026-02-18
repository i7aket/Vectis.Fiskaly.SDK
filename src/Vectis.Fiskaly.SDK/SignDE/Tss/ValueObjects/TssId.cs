using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

[JsonConverter(typeof(UuidIdentifierJsonConverterFactory))]
public readonly partial record struct TssId : IUuidIdentifier<TssId>, IParsable<TssId>
{
    public Guid Value { get; }

    private TssId(Guid value) => Value = value;

    public static TssId New() => new(Guid.NewGuid());

    public static TssId From(string uuid)
        => UuidIdentifierHelper.From(uuid, value => new TssId(value), "TSS identifier");

    public static bool TryParse(string? value, out TssId result)
        => TryParse(value, null, out result);

    public static TssId Parse(string s, IFormatProvider? provider)
        => UuidIdentifierHelper.Parse(s, provider, value => new TssId(value), "TSS identifier");

    public static bool TryParse(string? s, IFormatProvider? provider, out TssId result)
        => UuidIdentifierHelper.TryParse(s, provider, value => new TssId(value), out result);

    public override string ToString() => Value.ToString();
}
