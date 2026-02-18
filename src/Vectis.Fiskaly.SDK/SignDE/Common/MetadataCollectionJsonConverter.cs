using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Common;

internal sealed class MetadataCollectionJsonConverter : JsonConverter<MetadataCollection>
{
    public override MetadataCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Metadata payload must be a JSON object.");
        }

        Dictionary<string, string> dictionary = new Dictionary<string, string>(MetadataCollection.MaxEntries, StringComparer.Ordinal);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Unexpected token when reading metadata.");
            }

            string key = reader.GetString() ?? throw new JsonException("Metadata key cannot be null.");

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading metadata.");
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    dictionary[key] = reader.GetString() ?? throw new JsonException(
                        $"Metadata value for key '{key}' cannot be null according to Fiskaly specification.");
                    break;
                case JsonTokenType.Null:
                    throw new JsonException(
                        $"Metadata value for key '{key}' cannot be null according to Fiskaly specification.");
                default:
                    throw new JsonException(
                        $"Metadata values must be strings or null according to Fiskaly API specification. " +
                        $"Received unexpected token type '{reader.TokenType}' for key '{key}'.");
            }
        }

        return MetadataCollection.FromDictionary(dictionary);
    }

    public override void Write(Utf8JsonWriter writer, MetadataCollection value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach ((string key, string metadataValue) in value)
        {
            writer.WriteString(key, metadataValue);
        }
        writer.WriteEndObject();
    }
}
