using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;

[JsonConverter(typeof(AdminPinJsonConverter))]
public readonly record struct AdminPin : IParsable<AdminPin>
{
    public const int MinimumLength = 6;

    public string Value { get; }

    private AdminPin(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        string trimmed = value.Trim();

        if (trimmed.Length < MinimumLength)
        {
            throw new ArgumentException($"Admin PIN must be at least {MinimumLength} characters long.", nameof(value));
        }

        Value = trimmed;
    }

    public static AdminPin From(string value) => new(value);

    public static bool TryFrom(string? value, out AdminPin pin)
    {
        pin = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length < MinimumLength)
        {
            return false;
        }

        pin = new AdminPin(trimmed);
        return true;
    }

    public static AdminPin Parse(string s, IFormatProvider? provider)
    {
        return From(s);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out AdminPin result)
    {
        return TryFrom(s, out result);
    }

    public override string ToString() => new('*', Math.Min(Value.Length, 4));

    private sealed class AdminPinJsonConverter : JsonConverter<AdminPin>
    {
        public override AdminPin Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Admin PIN cannot be null.");
            }

            return From(reader.GetString() ?? throw new JsonException("Admin PIN cannot be null."));
        }

        public override void Write(Utf8JsonWriter writer, AdminPin value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
