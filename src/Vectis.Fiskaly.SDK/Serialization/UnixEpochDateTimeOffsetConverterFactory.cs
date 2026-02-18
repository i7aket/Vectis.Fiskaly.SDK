using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.Serialization;

internal sealed class UnixEpochDateTimeOffsetConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(DateTimeOffset) || typeToConvert == typeof(DateTimeOffset?);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(DateTimeOffset))
        {
            return new UnixEpochDateTimeOffsetConverter();
        }

        if (typeToConvert == typeof(DateTimeOffset?))
        {
            return new NullableUnixEpochDateTimeOffsetConverter();
        }

        throw new NotSupportedException($"Type {typeToConvert} is not supported by {nameof(UnixEpochDateTimeOffsetConverterFactory)}.");
    }

    private sealed class UnixEpochDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()),
                JsonTokenType.String => ParseString(reader.GetString()),
                JsonTokenType.Null => throw new JsonException("Timestamp value cannot be null."),
                _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing timestamp.")
            };
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.ToUnixTimeSeconds());
        }
    }

    private sealed class NullableUnixEpochDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.Number => DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()),
                JsonTokenType.String => ParseString(reader.GetString()),
                _ => throw new JsonException($"Unexpected token {reader.TokenType} when parsing nullable timestamp.")
            };
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(value.Value.ToUnixTimeSeconds());
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    private static DateTimeOffset ParseString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException("Timestamp string cannot be empty.");
        }

        if (long.TryParse(raw, out long seconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        if (DateTimeOffset.TryParse(raw, out DateTimeOffset dto))
        {
            return dto.ToUniversalTime();
        }

        throw new JsonException($"Value '{raw}' is not a valid timestamp.");
    }
}
