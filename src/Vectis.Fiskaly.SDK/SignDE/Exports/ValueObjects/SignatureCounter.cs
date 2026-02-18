using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

[JsonConverter(typeof(SignatureCounterJsonConverter))]
public readonly record struct SignatureCounter : IParsable<SignatureCounter>
{
    public const long Min = 0;
    public const long Max = 9_007_199_254_740_991; // per OpenAPI (max safe integer)

    public long Value { get; }

    private SignatureCounter(long value)
    {
        Value = value;
    }

    public static SignatureCounter From(long value)
    {
        if (value < Min || value > Max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Signature counter must be between {Min} and {Max}.");
        }

        return new SignatureCounter(value);
    }

    public static bool TryFrom(long value, out SignatureCounter counter)
    {
        if (value < Min || value > Max)
        {
            counter = default;
            return false;
        }

        counter = new SignatureCounter(value);
        return true;
    }

    public static SignatureCounter Parse(string s, IFormatProvider? provider)
    {
        long value = long.Parse(s, provider);
        return From(value);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out SignatureCounter result)
    {
        result = default;

        if (!long.TryParse(s, provider, out long value))
        {
            return false;
        }

        return TryFrom(value, out result);
    }

    public override string ToString() => Value.ToString();

    private sealed class SignatureCounterJsonConverter : JsonConverter<SignatureCounter>
    {
        public override SignatureCounter Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            string? value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("Expected non-null string for signature counter.");
            }

            if (!long.TryParse(value, out long longValue))
            {
                throw new JsonException($"Invalid signature counter format: '{value}'. Expected numeric string.");
            }

            return From(longValue);
        }

        public override void Write(
            Utf8JsonWriter writer,
            SignatureCounter value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
    }
}
