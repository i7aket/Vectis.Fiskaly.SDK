using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Authentication.ValueObjects;

[JsonConverter(typeof(RefreshTokenJsonConverter))]
public readonly partial record struct RefreshToken : IParsable<RefreshToken>
{
    public string Value { get; }

    private RefreshToken(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        string trimmed = value.Trim();
        JwtTokenValidator.Validate(trimmed, nameof(value));
        Value = trimmed;
    }

    public static RefreshToken From(string value) => new(value);

    public static bool TryFrom(string? value, out RefreshToken token)
    {
        token = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        JwtTokenValidator.Validate(trimmed, nameof(value));
        token = new RefreshToken(trimmed);
        return true;
    }

    public static RefreshToken Parse(string s, IFormatProvider? provider)
    {
        return From(s);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out RefreshToken result)
    {
        return TryFrom(s, out result);
    }

    public override string ToString() => $"refresh_token:{Value[..Math.Min(Value.Length, 4)]}***";

    private sealed class RefreshTokenJsonConverter : JsonConverter<RefreshToken>
    {
        public override RefreshToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (value is null)
            {
                throw new JsonException("Expected non-null string for refresh token.");
            }

            return From(value);
        }

        public override void Write(Utf8JsonWriter writer, RefreshToken value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
