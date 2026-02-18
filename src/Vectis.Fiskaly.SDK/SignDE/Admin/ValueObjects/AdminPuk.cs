using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;

[JsonConverter(typeof(AdminPukJsonConverter))]
public readonly record struct AdminPuk : IParsable<AdminPuk>
{
    public const int MinimumLength = 10;

    public string Value { get; }

    private AdminPuk(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        string trimmed = value.Trim();

        if (trimmed.Length < MinimumLength)
        {
            throw new ArgumentException($"Admin PUK must be at least {MinimumLength} characters long.", nameof(value));
        }

        Value = trimmed;
    }

    public static AdminPuk From(string value) => new(value);

    public static bool TryFrom(string? value, out AdminPuk puk)
    {
        puk = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        if (trimmed.Length < MinimumLength)
        {
            return false;
        }

        puk = new AdminPuk(trimmed);
        return true;
    }

    public static AdminPuk Parse(string s, IFormatProvider? provider)
    {
        return From(s);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out AdminPuk result)
    {
        return TryFrom(s, out result);
    }

    public override string ToString() => new('*', Math.Min(Value.Length, 4));

    private sealed class AdminPukJsonConverter : JsonConverter<AdminPuk>
    {
        public override AdminPuk Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("Admin PUK cannot be null.");
            }

            return From(reader.GetString() ?? throw new JsonException("Admin PUK cannot be null."));
        }

        public override void Write(Utf8JsonWriter writer, AdminPuk value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
