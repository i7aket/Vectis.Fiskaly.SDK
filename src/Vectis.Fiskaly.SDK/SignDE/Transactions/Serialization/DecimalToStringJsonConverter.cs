using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;

public sealed class DecimalToStringJsonConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                string? stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    throw new JsonException("Decimal string cannot be null or empty.");
                }

                if (decimal.TryParse(stringValue, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out decimal parsedDecimal))
                {
                    return parsedDecimal;
                }

                throw new JsonException($"Value '{stringValue}' is not a valid decimal string. " +
                                        "Expected format: ^-?\\d+(\\.\\d{{1,5}})?$ (e.g., \"10.98\", \"-2.75\", \"0.5\")");

            case JsonTokenType.Number:
                if (!reader.TryGetDecimal(out decimal numericDecimal))
                {
                    throw new JsonException("Numeric value cannot be converted to decimal.");
                }
                return numericDecimal;

            default:
                throw new JsonException($"Unexpected token {reader.TokenType} while parsing decimal. " +
                                        "Expected string or number.");
        }
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        string stringValue = value.ToString("0.#####", CultureInfo.InvariantCulture);
        writer.WriteStringValue(stringValue);
    }
}
