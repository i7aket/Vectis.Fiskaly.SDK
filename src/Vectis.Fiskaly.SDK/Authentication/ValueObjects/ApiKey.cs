using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Vectis.Fiskaly.SDK.Authentication.ValueObjects;

[JsonConverter(typeof(ApiKeyJsonConverter))]
public readonly partial record struct ApiKey : IParsable<ApiKey>
{
    private const int MinimumLength = 6;

    private const int MaximumLength = 512;

    [GeneratedRegex(@".*[^\s].*")]
    private static partial Regex ValidationPattern();

    public string Value { get; }

    private ApiKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        string trimmed = value.Trim();

        if (trimmed.Length < MinimumLength)
        {
            throw new ArgumentException(
                $"API key must be at least {MinimumLength} characters long. " +
                $"Current: {trimmed.Length} characters.",
                nameof(value));
        }

        if (trimmed.Length > MaximumLength)
        {
            throw new ArgumentException(
                $"API key must not exceed {MaximumLength} characters. " +
                $"Current: {trimmed.Length} characters. " +
                "Verify your fiskaly API key configuration.",
                nameof(value));
        }

        if (!ValidationPattern().IsMatch(trimmed))
        {
            throw new ArgumentException(
                "API key must contain at least one non-whitespace character. " +
                "Current value appears to be only whitespace.",
                nameof(value));
        }

        Value = trimmed;
    }

    public static ApiKey From(string value) => new(value);

    public static bool TryFrom(string? value, out ApiKey apiKey)
    {
        apiKey = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();

        if (trimmed.Length < MinimumLength || trimmed.Length > MaximumLength)
        {
            return false;
        }

        if (!ValidationPattern().IsMatch(trimmed))
        {
            return false;
        }

        apiKey = new ApiKey(trimmed);
        return true;
    }

    public static ApiKey Parse(string s, IFormatProvider? provider)
    {
        return From(s);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out ApiKey result)
    {
        return TryFrom(s, out result);
    }

    public override string ToString() => new('*', Math.Min(Value.Length, 8));

    private sealed class ApiKeyJsonConverter : JsonConverter<ApiKey>
    {
        public override ApiKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();
            if (value is null)
            {
                throw new JsonException("Expected non-null string for API key.");
            }

            return From(value);
        }

        public override void Write(Utf8JsonWriter writer, ApiKey value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
