using System.Text.RegularExpressions;

namespace Vectis.Fiskaly.SDK.ValueObjects;

/// <summary>
/// Source-generated regex used to validate UUIDv4 identifiers.
/// </summary>
internal static partial class UuidValidationRegex
{
    [GeneratedRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", RegexOptions.IgnoreCase)]
    internal static partial Regex UuidV4Pattern();
}

/// <summary>
/// Helper methods shared by strongly typed UUID identifiers.
/// </summary>
internal static class UuidIdentifierHelper
{
    internal static TId From<TId>(string uuid, Func<Guid, TId> constructor, string identifierName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uuid, nameof(uuid));

        string trimmed = uuid.Trim();

        if (!UuidValidationRegex.UuidV4Pattern().IsMatch(trimmed))
        {
            throw new ArgumentException(
                $"Invalid UUIDv4 format for {identifierName}. Expected: xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx (version=4, variant=RFC4122). Got: {uuid}",
                nameof(uuid)
            );
        }

        return constructor(Guid.Parse(trimmed));
    }

    internal static TId Parse<TId>(string s, IFormatProvider? provider, Func<Guid, TId> constructor, string identifierName)
    {
        return From(s, constructor, identifierName);
    }

    internal static bool TryParse<TId>(string? s, IFormatProvider? provider, Func<Guid, TId> constructor, out TId result)
    {
        result = default!;

        if (string.IsNullOrWhiteSpace(s))
            return false;

        string trimmed = s.Trim();

        if (UuidValidationRegex.UuidV4Pattern().IsMatch(trimmed) && Guid.TryParse(trimmed, out Guid guid))
        {
            result = constructor(guid);
            return true;
        }

        return false;
    }
}

/// <summary>
/// Contract for strongly typed UUID identifiers used across the SDK.
/// </summary>
public interface IUuidIdentifier<TSelf> : IEquatable<TSelf>
    where TSelf : IUuidIdentifier<TSelf>
{
    /// <summary>
    /// Generates a new UUIDv4 identifier.
    /// </summary>
    static abstract TSelf New();

    /// <summary>
    /// Creates an identifier from a UUID string (validates UUIDv4 format).
    /// </summary>
    static abstract TSelf From(string uuid);

    /// <summary>
    /// Tries to create an identifier from a UUID string.
    /// </summary>
    static abstract bool TryParse(string value, out TSelf result);

    /// <summary>
    /// Underlying GUID value.
    /// </summary>
    Guid Value { get; }

    /// <summary>
    /// Returns the canonical lowercase UUID string.
    /// </summary>
    string ToString()
    {
        return Value.ToString();
    }

    /// <summary>
    /// Hash code based on the underlying GUID.
    /// </summary>
    int GetHashCode()
    {
        return Value.GetHashCode();
    }

    /// <summary>
    /// Compares identifiers by GUID value.
    /// </summary>
    new bool Equals(TSelf? other)
    {
        return other is not null && Value.Equals(other.Value);
    }

    /// <summary>
    /// Determines whether a string matches the UUIDv4 pattern.
    /// </summary>
    static virtual bool IsValidUuidV4(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               UuidValidationRegex.UuidV4Pattern().IsMatch(value);
    }
}
