using System.Formats.Tar;
using System.IO.Compression;
using Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Dsfinvk;

public class DsfinvkV2SegmentStrategyTests
{
    private readonly DsfinvkV2SegmentStrategy _strategy = new();

    [Trait("Category", "Unit")]
    [Fact]
    public void Version_Returns2x()
    {
        // Act
        string version = _strategy.Version;

        // Assert
        Assert.Equal("2.x", version);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_WithValidTar_ReturnsSegments()
    {
        // Arrange
        string tarPath = Path.Combine("TestData", "dsfinvk-sample.tar");
        await using FileStream stream = File.OpenRead(tarPath);

        // Act
        IReadOnlyCollection<DsfinvkSegment> segments = await _strategy.ParseAsync(stream);

        // Assert
        Assert.NotNull(segments);
        Assert.NotEmpty(segments);
        Assert.Equal(138, segments.Count); // Known segment count from dsfinvk-sample.tar
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_WithEmptyStream_ReturnsEmptyCollection()
    {
        // Arrange
        using MemoryStream stream = new MemoryStream();

        // Act
        IReadOnlyCollection<DsfinvkSegment> segments = await _strategy.ParseAsync(stream);

        // Assert
        Assert.NotNull(segments);
        Assert.Empty(segments);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _strategy.ParseAsync(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_RespectsCancellationToken()
    {
        // Arrange
        string tarPath = Path.Combine("TestData", "dsfinvk-sample.tar");
        await using FileStream stream = File.OpenRead(tarPath);
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _strategy.ParseAsync(stream, cts.Token));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_WithGzipTar_ReturnsSegments()
    {
        // Arrange - Create a GZIP-compressed TAR in memory
        string originalTarPath = Path.Combine("TestData", "dsfinvk-sample.tar");
        byte[] originalTarBytes = await File.ReadAllBytesAsync(originalTarPath);

        using MemoryStream compressedStream = new MemoryStream();
        using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
        {
            await gzipStream.WriteAsync(originalTarBytes);
        }
        compressedStream.Position = 0;

        // Act
        IReadOnlyCollection<DsfinvkSegment> segments = await _strategy.ParseAsync(compressedStream);

        // Assert
        Assert.NotNull(segments);
        Assert.Equal(138, segments.Count);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_WithActualSampleData_CategorizesSegmentsCorrectly()
    {
        // Arrange
        // The dsfinvk-sample.tar contains log files that don't match master/transaction/closing patterns
        // So they should be categorized as Unknown
        string tarPath = Path.Combine("TestData", "dsfinvk-sample.tar");
        await using FileStream stream = File.OpenRead(tarPath);

        // Act
        IReadOnlyCollection<DsfinvkSegment> segments = await _strategy.ParseAsync(stream);

        // Assert
        Assert.NotEmpty(segments);
        Assert.All(segments, seg => Assert.NotNull(seg.FileName));
        Assert.All(segments, seg => Assert.NotEqual(string.Empty, seg.FileName));

        // Most segments in this sample are categorized as Unknown (log files)
        List<DsfinvkSegment> unknownSegments = segments.Where(s => s.Type == DsfinvkSegmentType.Unknown).ToList();
        Assert.NotEmpty(unknownSegments);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_WithNonSeekableStream_WorksCorrectly()
    {
        // Arrange - Create non-seekable wrapper
        string tarPath = Path.Combine("TestData", "dsfinvk-sample.tar");
        byte[] tarBytes = await File.ReadAllBytesAsync(tarPath);
        using NonSeekableStream nonSeekableStream = new NonSeekableStream(tarBytes);

        // Act
        IReadOnlyCollection<DsfinvkSegment> segments = await _strategy.ParseAsync(nonSeekableStream);

        // Assert
        Assert.NotNull(segments);
        Assert.Equal(138, segments.Count);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_SkipsEmptyEntries()
    {
        // Arrange - Create minimal TAR with empty entry
        using MemoryStream tarStream = CreateTarWithEmptyEntry();

        // Act
        IReadOnlyCollection<DsfinvkSegment> segments = await _strategy.ParseAsync(tarStream);

        // Assert
        // Empty entries should be skipped
        Assert.NotNull(segments);
        Assert.Empty(segments);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ParseAsync_NormalizesFilePaths()
    {
        // Arrange
        string tarPath = Path.Combine("TestData", "dsfinvk-sample.tar");
        await using FileStream stream = File.OpenRead(tarPath);

        // Act
        IReadOnlyCollection<DsfinvkSegment> segments = await _strategy.ParseAsync(stream);

        // Assert
        // File names should use forward slashes (Unix-style paths)
        Assert.All(segments, seg => Assert.DoesNotContain("\\", seg.FileName));
    }

    // ============================================================================
    // Helper Classes and Methods
    // ============================================================================

    /// <summary>
    /// Non-seekable stream wrapper for testing EnsureSeekableAsync
    /// </summary>
    private sealed class NonSeekableStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableStream(byte[] data)
        {
            _inner = new MemoryStream(data);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false; // Non-seekable!
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private static MemoryStream CreateTarWithEmptyEntry()
    {
        MemoryStream stream = new MemoryStream();
        using (TarWriter writer = new System.Formats.Tar.TarWriter(stream, leaveOpen: true))
        {
            // Create an empty entry
            PaxTarEntry emptyEntry = new System.Formats.Tar.PaxTarEntry(System.Formats.Tar.TarEntryType.RegularFile, "empty.json");
            writer.WriteEntry(emptyEntry);
        }
        stream.Position = 0;
        return stream;
    }
}
