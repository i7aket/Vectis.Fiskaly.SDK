using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

internal sealed class StandardV1SchemaJsonConverter : JsonConverter<StandardV1Schema>
{
    public override StandardV1Schema? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("standard_v1 payload must be an object.");
        }

        Receipt? receipt = null;
        Order? order = null;
        Other? other = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name within standard_v1 payload.");
            }

            string? propertyName = reader.GetString();
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Property name within standard_v1 payload cannot be null or whitespace.");
            }

            reader.Read();

            switch (propertyName)
            {
                case "receipt":
                    receipt = JsonSerializer.Deserialize<Receipt>(ref reader, options)
                              ?? throw new JsonException("receipt payload cannot be null.");
                    break;
                case "order":
                    order = JsonSerializer.Deserialize<Order>(ref reader, options)
                              ?? throw new JsonException("order payload cannot be null.");
                    break;
                case "other":
                    other = JsonSerializer.Deserialize<Other>(ref reader, options)
                              ?? throw new JsonException("other payload cannot be null.");
                    break;
                default:
                    throw new JsonException($"Unexpected property '{propertyName}' in standard_v1 payload.");
            }
        }

        int count = (receipt is not null ? 1 : 0)
                    + (order is not null ? 1 : 0)
                    + (other is not null ? 1 : 0);

        if (count != 1)
        {
            throw new JsonException("standard_v1 payload must contain exactly one of receipt, order, or other.");
        }

        if (receipt is not null)
        {
            return StandardV1Schema.ForReceipt(receipt);
        }

        if (order is not null)
        {
            return StandardV1Schema.ForOrder(order);
        }

        return StandardV1Schema.ForOther(other!);
    }

    public override void Write(Utf8JsonWriter writer, StandardV1Schema value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        switch (value.Payload)
        {
            case Receipt receipt:
                writer.WritePropertyName("receipt");
                JsonSerializer.Serialize(writer, receipt, options);
                break;
            case Order order:
                writer.WritePropertyName("order");
                JsonSerializer.Serialize(writer, order, options);
                break;
            case Other other:
                writer.WritePropertyName("other");
                JsonSerializer.Serialize(writer, other, options);
                break;
            default:
                throw new JsonException("Unsupported standard_v1 payload variant.");
        }

        writer.WriteEndObject();
    }
}
