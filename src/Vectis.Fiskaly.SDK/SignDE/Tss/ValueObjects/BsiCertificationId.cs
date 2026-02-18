using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

[JsonConverter(typeof(BsiCertificationIdJsonConverter))]
public readonly record struct BsiCertificationId
{
    private static readonly Regex Pattern = new(@"^[A-Z0-9\-]{4,32}$", RegexOptions.Compiled);

    public string Value { get; } = string.Empty;

    private BsiCertificationId(string value)
    {
        Value = value;
    }

    public static BsiCertificationId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("BSI certification ID cannot be null or whitespace.", nameof(value));
        }

        string normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 32)
        {
            throw new ArgumentException("BSI certification ID must not exceed 32 characters.", nameof(value));
        }

        if (!Pattern.IsMatch(normalized))
        {
            throw new FormatException("BSI certification ID must contain only uppercase letters, numbers, or dashes.");
        }

        return new BsiCertificationId(normalized);
    }

    public static bool TryFrom(string? value, out BsiCertificationId certificationId)
    {
        certificationId = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            certificationId = From(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => Value;

    private sealed class BsiCertificationIdJsonConverter : JsonConverter<BsiCertificationId>
    {
        public override BsiCertificationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            return From(value ?? throw new JsonException("BSI certification ID cannot be null."));
        }

        public override void Write(Utf8JsonWriter writer, BsiCertificationId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
