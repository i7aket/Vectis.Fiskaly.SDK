using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

public sealed class DsfinvkV2SegmentStrategy : IDsfinvkVersionStrategy
{
    public string Version => "2.x";

    public async Task<IReadOnlyCollection<DsfinvkSegment>> ParseAsync(Stream archiveStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);

        Stream materializedStream = await EnsureSeekableAsync(archiveStream, cancellationToken).ConfigureAwait(false);

        if (IsZip(materializedStream))
        {
            materializedStream.Position = 0;
            return await ParseZipAsync(materializedStream, cancellationToken).ConfigureAwait(false);
        }

        if (IsGzip(materializedStream))
        {
            materializedStream.Position = 0;
            using MemoryStream decompressed = new MemoryStream();
            using (GZipStream gzip = new GZipStream(materializedStream, CompressionMode.Decompress, leaveOpen: true))
            {
                await gzip.CopyToAsync(decompressed, cancellationToken).ConfigureAwait(false);
            }

            decompressed.Position = 0;
            return await ParseTarAsync(decompressed, cancellationToken).ConfigureAwait(false);
        }

        materializedStream.Position = 0;
        return await ParseTarAsync(materializedStream, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Stream> EnsureSeekableAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
            return stream;
        }

        MemoryStream buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;
        return buffer;
    }

    private static bool IsZip(Stream stream)
    {
        Span<byte> signature = stackalloc byte[4];
        int read = stream.Read(signature);
        stream.Position = 0;
        return read == 4 && signature[0] == 0x50 && signature[1] == 0x4B && (signature[2] == 0x03 || signature[2] == 0x05 || signature[2] == 0x07);
    }

    private static bool IsGzip(Stream stream)
    {
        Span<byte> signature = stackalloc byte[2];
        int read = stream.Read(signature);
        stream.Position = 0;
        return read == 2 && signature[0] == 0x1F && signature[1] == 0x8B;
    }

    private static async Task<IReadOnlyCollection<DsfinvkSegment>> ParseZipAsync(Stream stream, CancellationToken cancellationToken)
    {
        List<DsfinvkSegment> segments = new List<DsfinvkSegment>();

        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Length == 0 || string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            using Stream entryStream = entry.Open();
            byte[] content = await ReadAllBytesAsync(entryStream, cancellationToken).ConfigureAwait(false);
            segments.Add(CreateSegment(entry.FullName, content));
        }

        return segments;
    }

    private static async Task<IReadOnlyCollection<DsfinvkSegment>> ParseTarAsync(Stream stream, CancellationToken cancellationToken)
    {
        List<DsfinvkSegment> segments = new List<DsfinvkSegment>();
        using TarReader reader = new TarReader(stream, leaveOpen: true);

        TarEntry? entry;
        while ((entry = reader.GetNextEntry()) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.EntryType != TarEntryType.RegularFile)
            {
                continue;
            }

            if (entry.DataStream is null)
            {
                continue;
            }

            using Stream? dataStream = entry.DataStream;
            byte[] content = await ReadAllBytesAsync(dataStream, cancellationToken).ConfigureAwait(false);
            segments.Add(CreateSegment(entry.Name, content));
        }

        return segments;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using MemoryStream buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static DsfinvkSegment CreateSegment(string archivePath, byte[] content)
    {
        string normalizedName = NormalizePath(archivePath);
        string fileName = Path.GetFileName(normalizedName);
        string lowered = fileName.ToLowerInvariant();

        if (lowered.Contains("master"))
        {
            return new MasterDataSegment(normalizedName, content);
        }

        if (lowered.Contains("transaction") || lowered.Contains("receipt") || lowered.Contains("tx"))
        {
            return new TransactionSegment(normalizedName, content);
        }

        if (lowered.Contains("closing") || lowered.Contains("cashpoint") || lowered.Contains("cash_point"))
        {
            return new CashPointClosingSegment(normalizedName, content);
        }

        return new UnknownDsfinvkSegment(normalizedName, content);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder(path.Length);
        foreach (char ch in path)
        {
            builder.Append(ch == '\\' ? '/' : ch);
        }

        return builder.ToString();
    }
}
