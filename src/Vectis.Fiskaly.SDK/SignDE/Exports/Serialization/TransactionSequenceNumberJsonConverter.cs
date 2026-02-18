using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Serialization;

internal sealed class TransactionSequenceNumberJsonConverter : JsonConverter<TransactionSequenceNumber>
{
    public override TransactionSequenceNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long value = reader.TokenType switch
        {
            JsonTokenType.Number when reader.TryGetInt64(out long number) => number,
            JsonTokenType.String when long.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed) => parsed,
            _ => throw new JsonException("Expected number or numeric string for TransactionSequenceNumber.")
        };

        return TransactionSequenceNumber.From(value);
    }

    public override void Write(Utf8JsonWriter writer, TransactionSequenceNumber value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
