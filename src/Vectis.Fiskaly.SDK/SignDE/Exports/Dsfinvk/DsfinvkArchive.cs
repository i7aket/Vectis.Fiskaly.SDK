using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

public sealed class DsfinvkArchive
{
    private DsfinvkArchive(
        ExportId exportId,
        IDsfinvkVersionStrategy strategy,
        IReadOnlyCollection<DsfinvkSegment> segments,
        byte[] rawContent)
    {
        ExportId = exportId;
        Strategy = strategy;
        Segments = segments;
        _rawContent = rawContent;
    }

    private readonly byte[] _rawContent;

    public ExportId ExportId { get; }

    public IDsfinvkVersionStrategy Strategy { get; }

    public IReadOnlyCollection<DsfinvkSegment> Segments { get; }

    public ReadOnlyMemory<byte> RawContent => _rawContent;

    public static async Task<DsfinvkArchive> FromStreamAsync(
        ExportId exportId,
        Stream archiveStream,
        IDsfinvkVersionStrategy? strategy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);

        strategy ??= new DsfinvkV2SegmentStrategy();

        byte[] bytes = await ReadAllBytesAsync(archiveStream, cancellationToken).ConfigureAwait(false);

        using MemoryStream buffer = new MemoryStream(bytes, writable: false);
        IReadOnlyCollection<DsfinvkSegment> segments = await strategy.ParseAsync(buffer, cancellationToken).ConfigureAwait(false);

        return new DsfinvkArchive(exportId, strategy, segments, bytes);
    }

    public IEnumerable<TSegment> OfType<TSegment>() where TSegment : DsfinvkSegment =>
        Segments.OfType<TSegment>();

    public Stream OpenRawStream() => new MemoryStream(_rawContent, writable: false);

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }
}
