using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Authentication.ValueObjects;

[JsonConverter(typeof(AccessTokenJsonConverter))]
public readonly partial record struct AccessToken : IParsable<AccessToken>
{
    public string Value { get; }

    private AccessToken(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        string trimmed = value.Trim();
        JwtTokenValidator.Validate(trimmed, nameof(value));

        Value = trimmed;
    }

    public static AccessToken From(string value) => new(value);

    public static bool TryFrom(string? value, out AccessToken token)
    {
        token = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            string trimmed = value.Trim();
            JwtTokenValidator.Validate(trimmed, nameof(value));

            token = new AccessToken(trimmed);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static AccessToken Parse(string s, IFormatProvider? provider) => From(s);

    public static bool TryParse(string? s, IFormatProvider? provider, out AccessToken result) =>
        TryFrom(s, out result);

    public override string ToString() => $"access_token:{Value[..Math.Min(Value.Length, 4)]}***";

    private sealed class AccessTokenJsonConverter : JsonConverter<AccessToken>
    {
        public override AccessToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string value = reader.GetString() ?? throw new JsonException("Expected non-null string for access token.");

            return From(value);
        }

        public override void Write(Utf8JsonWriter writer, AccessToken value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
