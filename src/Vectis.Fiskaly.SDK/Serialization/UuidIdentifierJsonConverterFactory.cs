using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.ValueObjects;

namespace Vectis.Fiskaly.SDK.Serialization;

public sealed class UuidIdentifierJsonConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> Cache = new();

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsValueType)
            return false;

        return typeToConvert.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IUuidIdentifier<>));
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return Cache.GetOrAdd(typeToConvert, static type =>
        {
            Type converterType = typeof(UuidIdentifierJsonConverter<>).MakeGenericType(type);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        });
    }

    private sealed class UuidIdentifierJsonConverter<T> : JsonConverter<T>
        where T : struct, IUuidIdentifier<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string token for {typeof(T).Name}, got {reader.TokenType}.");
            }

            string? value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException($"{typeof(T).Name} cannot be null or whitespace.");
            }

            try
            {
                return T.From(value);
            }
            catch (ArgumentException ex)
            {
                throw new JsonException($"Invalid {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
