namespace Vectis.Fiskaly.SDK.SignDE.Common;

/// <summary>
/// Converts strongly typed query objects into key/value pairs for fiskaly endpoint URLs.
/// </summary>
public interface IQueryParameterProvider
{
    /// <summary>
    /// Returns non-encoded parameter entries (omit nulls; QueryHelpers.AddQueryString encodes later).
    /// </summary>
    IEnumerable<KeyValuePair<string, string?>> ToQueryParameters();
}
