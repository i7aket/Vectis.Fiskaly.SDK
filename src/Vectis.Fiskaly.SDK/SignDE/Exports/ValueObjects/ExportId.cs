using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Serialization;
using Vectis.Fiskaly.SDK.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

[JsonConverter(typeof(UuidIdentifierJsonConverterFactory))]
public readonly partial record struct ExportId : IUuidIdentifier<ExportId>, IParsable<ExportId>
{
    public Guid Value { get; }

    private ExportId(Guid value) => Value = value;


    public static ExportId New() => new(Guid.NewGuid());

    public static ExportId From(string uuid)
        => UuidIdentifierHelper.From(uuid, value => new ExportId(value), "export identifier");

    public static bool TryParse(string? value, out ExportId result)
    {
        return TryParse(value, null, out result);
    }

    public static ExportId Parse(string s, IFormatProvider? provider)
        => UuidIdentifierHelper.Parse(s, provider, value => new ExportId(value), "export identifier");

    public static bool TryParse(string? s, IFormatProvider? provider, out ExportId result)
        => UuidIdentifierHelper.TryParse(s, provider, value => new ExportId(value), out result);

    public override string ToString() => Value.ToString();
}
