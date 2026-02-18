using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;

public sealed class MoneyAmountJsonConverter : JsonConverter<MoneyAmount>
{
    public override MoneyAmount Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => ReadFromNumber(ref reader),
            JsonTokenType.String => ReadFromString(ref reader),
            _ => throw new JsonException($"Unexpected token {reader.TokenType} while parsing MoneyAmount. Expected string or number.")
        };
    }

    public override void Write(Utf8JsonWriter writer, MoneyAmount value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToStringInvariant());
    }

    private static MoneyAmount ReadFromNumber(ref Utf8JsonReader reader)
    {
        if (!reader.TryGetDecimal(out decimal decimalValue))
        {
            throw new JsonException("Numeric value cannot be converted to decimal.");
        }

        try
        {
            return MoneyAmount.Create(decimalValue, CurrencyCode.EUR);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new JsonException($"Numeric value '{decimalValue}' is outside the allowed range.", ex);
        }
    }

    private static MoneyAmount ReadFromString(ref Utf8JsonReader reader)
    {
        string? raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException("Money amount string cannot be null or empty.");
        }

        if (!decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal parsed))
        {
            throw new JsonException($"Value '{raw}' is not a valid Sign DE monetary amount.");
        }

        try
        {
            return MoneyAmount.Create(parsed, CurrencyCode.EUR);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new JsonException($"Value '{raw}' is outside the allowed range.", ex);
        }
    }
}
