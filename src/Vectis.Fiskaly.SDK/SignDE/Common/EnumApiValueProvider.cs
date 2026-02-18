using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Common;

public static class EnumApiValueProvider
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<long, string>> Cache = new();

    public static string GetApiName(Enum value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        Type enumType = value.GetType();
        IReadOnlyDictionary<long, string> mappings = Cache.GetOrAdd(enumType, BuildMap);
        long key = Convert.ToInt64(value);

        if (mappings.TryGetValue(key, out string? name))
        {
            return name;
        }

        throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown {enumType.Name} value: {value}");
    }

    private static IReadOnlyDictionary<long, string> BuildMap(Type enumType)
    {
        Dictionary<long, string> result = new Dictionary<long, string>();
        FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (FieldInfo field in fields)
        {
            Enum enumValue = (Enum)field.GetValue(null)!;
            long key = Convert.ToInt64(enumValue);

            JsonStringEnumMemberNameAttribute? jsonAttribute = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
            if (!string.IsNullOrWhiteSpace(jsonAttribute?.Name))
            {
                result[key] = jsonAttribute.Name!;
                continue;
            }

            EnumMemberAttribute? enumMemberAttribute = field.GetCustomAttribute<EnumMemberAttribute>();
            if (!string.IsNullOrWhiteSpace(enumMemberAttribute?.Value))
            {
                result[key] = enumMemberAttribute!.Value!;
                continue;
            }

            result[key] = field.Name;
        }

        return result;
    }
}
