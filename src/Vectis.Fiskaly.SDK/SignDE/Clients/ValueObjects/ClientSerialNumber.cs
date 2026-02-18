using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

[JsonConverter(typeof(ClientSerialNumberJsonConverter))]
public readonly partial record struct ClientSerialNumber : IParsable<ClientSerialNumber>
{
    [GeneratedRegex(@"^[A-Za-z0-9 '()+,-.:=?]{1,70}$")]
    private static partial Regex Pattern();

    public string Value { get; }

    private ClientSerialNumber(string value)
    {
        Value = value;
    }

    public static ClientSerialNumber From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        string trimmed = value.Trim();
        if (!Pattern().IsMatch(trimmed))
        {
            throw new FormatException(
                $"Serial number '{value}' does not match DSFinV-K requirements. " +
                $"Must be 1-70 characters from: A-Z, a-z, 0-9, space, and '()+,-.:=? " +
                $"(Note: '/' and '_' are excluded for DSFinV-K 2.3 compatibility)");
        }

        return new ClientSerialNumber(trimmed);
    }

    public static bool TryParse(string? value, out ClientSerialNumber serialNumber)
    {
        return TryParse(value, null, out serialNumber);
    }

    public static ClientSerialNumber Parse(string s, IFormatProvider? provider)
    {
        return From(s);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out ClientSerialNumber result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        string trimmed = s.Trim();
        if (!Pattern().IsMatch(trimmed))
        {
            return false;
        }

        result = new ClientSerialNumber(trimmed);
        return true;
    }

    public override string ToString() => Value;

    private sealed class ClientSerialNumberJsonConverter : JsonConverter<ClientSerialNumber>
    {
        public override ClientSerialNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? value = reader.GetString();

            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            return From(value);
        }

        public override void Write(Utf8JsonWriter writer, ClientSerialNumber value, JsonSerializerOptions options)
        {
            if (value.Value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value);
            }
        }
    }
}
