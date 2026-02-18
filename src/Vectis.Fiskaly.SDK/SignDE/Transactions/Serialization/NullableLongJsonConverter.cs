using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;

public class NullableLongJsonConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt64();

            case JsonTokenType.String:
                string? stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return null;
                }

                if (long.TryParse(stringValue, out long parsedValue))
                {
                    return parsedValue;
                }

                return null;

            case JsonTokenType.Null:
                return null;

            default:
                throw new JsonException($"Cannot convert {reader.TokenType} to long?");
        }
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
