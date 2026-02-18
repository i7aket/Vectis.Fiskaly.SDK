using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Vectis.Fiskaly.SDK.SignDE.Common;

public sealed class MetadataCollection : IReadOnlyDictionary<string, string>, IEquatable<MetadataCollection>
{
    public const int MaxEntries = 40;
    public const int MaxKeyLength = 40;
    public const int MaxValueLength = 500;

    private static readonly StringComparer Comparer = StringComparer.Ordinal;
    private readonly IReadOnlyDictionary<string, string> _values;

    private MetadataCollection(IReadOnlyDictionary<string, string> values)
    {
        _values = values;
    }

    public static MetadataCollection Empty { get; } = new MetadataCollection(
        new Dictionary<string, string>(0, Comparer));

    public static MetadataCollection From(IEnumerable<KeyValuePair<string, string>> entries)
    {
        if (entries is MetadataCollection collection)
        {
            return collection;
        }

        Dictionary<string, string> materialized = new Dictionary<string, string>(Comparer);
        foreach (KeyValuePair<string, string> entry in entries)
        {
            AddValidated(materialized, entry.Key, entry.Value);
        }

        return new MetadataCollection(materialized);
    }

    public static MetadataCollection FromDictionary(IReadOnlyDictionary<string, string> dictionary) =>
        dictionary.Count == 0
            ? Empty
            : From(dictionary.AsEnumerable());

    public MetadataCollection Add(string key, string value)
    {
        Dictionary<string, string> builder = new Dictionary<string, string>(_values, Comparer);
        AddValidated(builder, key, value);
        return new MetadataCollection(builder);
    }

    public bool TryAdd(string key, string value, [NotNullWhen(true)] out MetadataCollection? collection, out string? error)
    {
        collection = null;
        error = null;

        Dictionary<string, string> builder = new Dictionary<string, string>(_values, Comparer);
        if (!TryAddValidated(builder, key, value, out error))
        {
            return false;
        }

        collection = new MetadataCollection(builder);
        return true;
    }

    public Dictionary<string, string> ToDictionary() => new(_values, Comparer);

    private static void AddValidated(IDictionary<string, string> target, string key, string value)
    {
        if (!TryAddValidated(target, key, value, out string? error))
        {
            throw new ArgumentException(error, nameof(key));
        }
    }

    private static bool TryAddValidated(IDictionary<string, string> target, string key, string value, out string? error)
    {
        error = null;

        if (target.Count >= MaxEntries && !target.ContainsKey(key))
        {
            error = $"Metadata cannot contain more than {MaxEntries} entries.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            error = "Metadata key cannot be null or whitespace.";
            return false;
        }

        if (key.Length > MaxKeyLength)
        {
            error = $"Metadata key '{key}' exceeds maximum length of {MaxKeyLength}.";
            return false;
        }

        if (value is null)
        {
            error = $"Metadata value for key '{key}' cannot be null.";
            return false;
        }

        if (value.Length > MaxValueLength)
        {
            error = $"Metadata value for key '{key}' exceeds maximum length of {MaxValueLength}.";
            return false;
        }

        target[key] = value;
        return true;
    }

    #region IReadOnlyDictionary implementation

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Count => _values.Count;
    public bool ContainsKey(string key) => _values.ContainsKey(key);
    public bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);
    public string this[string key] => _values[key];
    public IEnumerable<string> Keys => _values.Keys;
    public IEnumerable<string> Values => _values.Values;

    #endregion

    public bool Equals(MetadataCollection? other)
    {
        if (other is null || Count != other.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, string> kvp in _values)
        {
            if (!other.TryGetValue(kvp.Key, out string otherValue) ||
                !Comparer.Equals(kvp.Value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is MetadataCollection other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();
        foreach (KeyValuePair<string, string> kvp in _values.OrderBy(pair => pair.Key, Comparer))
        {
            hash.Add(kvp.Key, Comparer);
            hash.Add(kvp.Value, Comparer);
        }

        return hash.ToHashCode();
    }
}
