namespace Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

/// <summary>
/// Parses DSFinV-K exports according to a specific specification version.
/// </summary>
public interface IDsfinvkVersionStrategy
{
    /// <summary>
    /// Version identifier (for example, <c>2.3</c>).
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Parses an export archive stream into typed DSFinV-K segments.
    /// </summary>
    /// <param name="archiveStream">TAR or ZIP archive provided by the export endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of parsed segments.</returns>
    Task<IReadOnlyCollection<DsfinvkSegment>> ParseAsync(Stream archiveStream, CancellationToken cancellationToken = default);
}
