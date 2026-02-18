using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Vectis.Fiskaly.SDK.Authentication.ValueObjects;

[JsonConverter(typeof(ApiSecretJsonConverter))]
public readonly partial record struct ApiSecret : IParsable<ApiSecret>
{
    private const int ExactLength = 43;

    [GeneratedRegex(@"^[0-9A-Za-z]{43}$")]
    private static partial Regex ValidationPattern();

    public string Value { get; }

    private ApiSecret(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        string trimmed = value.Trim();

        if (!ValidationPattern().IsMatch(trimmed))
        {
            throw new FormatException(
                $"API secret must be exactly {ExactLength} alphanumeric characters. " +
                $"Expected format: test_xxxxxxxxxxxxxxxxxxxxx_xxx (43 chars). " +
                $"Current value: {trimmed.Length} characters" +
                (trimmed.Length != ExactLength ? $", expected {ExactLength}" : "") + ". " +
                "Verify your fiskaly credentials.");
        }

        Value = trimmed;
    }

    public static ApiSecret From(string value) => new(value);

    public static bool TryFrom(string? value, out ApiSecret secret)
    {
        secret = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();

        if (!ValidationPattern().IsMatch(trimmed))
        {
            return false;
        }

        secret = new ApiSecret(trimmed);
        return true;
    }

    public static ApiSecret Parse(string s, IFormatProvider? provider)
    {
        return From(s);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out ApiSecret result)
    {
        return TryFrom(s, out result);
    }

    public override string ToString()
    {
        return new string('*', Math.Min(Value.Length, 8));
    }

    private sealed class ApiSecretJsonConverter : JsonConverter<ApiSecret>
    {
        public override ApiSecret Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (value is null)
            {
                throw new JsonException("Expected non-null string for API secret.");
            }

            return From(value);
        }

        public override void Write(Utf8JsonWriter writer, ApiSecret value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
