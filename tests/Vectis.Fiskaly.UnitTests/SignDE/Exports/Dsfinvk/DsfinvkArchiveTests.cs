using Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Dsfinvk;

public class DsfinvkArchiveTests
{
    private readonly ExportId _testExportId = ExportId.From("550e8400-e29b-41d4-a716-446655440000");

    // ============================================================================
    // Factory Method Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task FromStreamAsync_ValidStream_CreatesArchive()
    {
        // Arrange
        byte[] testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using MemoryStream stream = new MemoryStream(testData);
        FakeDsfinvkVersionStrategy strategy = new FakeDsfinvkVersionStrategy();

        // Act
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(_testExportId, stream, strategy);

        // Assert
        Assert.NotNull(archive);
        Assert.Equal(_testExportId, archive.ExportId);
        Assert.Equal(strategy, archive.Strategy);
        Assert.NotNull(archive.Segments);
        Assert.Equal(4, archive.RawContent.Length);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task FromStreamAsync_NullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DsfinvkArchive.FromStreamAsync(_testExportId, null!, null));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task FromStreamAsync_EmptyStream_CreatesArchiveWithNoSegments()
    {
        // Arrange
        using MemoryStream emptyStream = new MemoryStream();
        FakeDsfinvkVersionStrategy strategy = new FakeDsfinvkVersionStrategy();

        // Act
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(_testExportId, emptyStream, strategy);

        // Assert
        Assert.NotNull(archive);
        Assert.Empty(archive.Segments);
        Assert.Equal(0, archive.RawContent.Length);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task FromStreamAsync_WithCustomStrategy_UsesStrategy()
    {
        // Arrange
        byte[] testData = new byte[] { 0xAA, 0xBB, 0xCC };
        using MemoryStream stream = new MemoryStream(testData);
        FakeDsfinvkVersionStrategy customStrategy = new FakeDsfinvkVersionStrategy
        {
            SegmentsToReturn = new[]
            {
                new UnknownDsfinvkSegment("test1.json", new byte[] { 0x01 }),
                new UnknownDsfinvkSegment("test2.json", new byte[] { 0x02 })
            }
        };

        // Act
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(_testExportId, stream, customStrategy);

        // Assert
        Assert.Equal(customStrategy, archive.Strategy);
        Assert.Equal(2, archive.Segments.Count);
        Assert.True(customStrategy.ParseAsyncWasCalled);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task FromStreamAsync_WithNullStrategy_UsesDefaultStrategy()
    {
        // Arrange
        // Load real DSFinV-K TAR archive from Fiskaly (138 files: info.csv + logs)
        string tarPath = Path.Combine(AppContext.BaseDirectory, "TestData", "dsfinvk-sample.tar");
        using FileStream stream = File.OpenRead(tarPath);

        // Act
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(_testExportId, stream, strategy: null);

        // Assert
        Assert.NotNull(archive.Strategy);
        Assert.Equal("2.x", archive.Strategy.Version); // DsfinvkV2SegmentStrategy default version

        // Verify archive was actually parsed - should contain 138 segments from real Fiskaly export
        Assert.NotEmpty(archive.Segments);
        Assert.Equal(138, archive.Segments.Count);

        // Verify raw TAR content was preserved (141KB)
        Assert.True(archive.RawContent.Length > 140_000,
            $"Expected TAR size > 140KB, got {archive.RawContent.Length} bytes");
    }

    // ============================================================================
    // Segment Filtering Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task OfType_WithMatchingSegments_ReturnsFiltered()
    {
        // Arrange
        byte[] testData = new byte[] { 0x01 };
        using MemoryStream stream = new MemoryStream(testData);
        FakeDsfinvkVersionStrategy strategy = new FakeDsfinvkVersionStrategy
        {
            SegmentsToReturn = new[]
            {
                new UnknownDsfinvkSegment("unknown1.json", new byte[] { 0x01 }),
                new UnknownDsfinvkSegment("unknown2.json", new byte[] { 0x02 })
            }
        };
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(_testExportId, stream, strategy);

        // Act
        List<UnknownDsfinvkSegment> unknownSegments = archive.OfType<UnknownDsfinvkSegment>().ToList();

        // Assert
        Assert.Equal(2, unknownSegments.Count);
        Assert.All(unknownSegments, s => Assert.IsType<UnknownDsfinvkSegment>(s));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task OfType_WithNoMatches_ReturnsEmpty()
    {
        // Arrange
        using MemoryStream stream = new MemoryStream(new byte[] { 0x01 });
        FakeDsfinvkVersionStrategy strategy = new FakeDsfinvkVersionStrategy
        {
            SegmentsToReturn = Array.Empty<DsfinvkSegment>()
        };
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(_testExportId, stream, strategy);

        // Act
        List<UnknownDsfinvkSegment> unknownSegments = archive.OfType<UnknownDsfinvkSegment>().ToList();

        // Assert
        Assert.Empty(unknownSegments);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task OfType_MultipleTypes_FiltersCorrectly()
    {
        // Arrange
        byte[] testData = new byte[] { 0x01 };
        using MemoryStream stream = new MemoryStream(testData);
        FakeDsfinvkVersionStrategy strategy = new FakeDsfinvkVersionStrategy
        {
            SegmentsToReturn = new DsfinvkSegment[]
            {
                new UnknownDsfinvkSegment("unknown.json", new byte[] { 0x01 }),
                new TestDsfinvkSegment("test.json", new byte[] { 0x02 }),
                new UnknownDsfinvkSegment("unknown2.json", new byte[] { 0x03 })
            }
        };
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(_testExportId, stream, strategy);

        // Act
        List<UnknownDsfinvkSegment> unknownSegments = archive.OfType<UnknownDsfinvkSegment>().ToList();
        List<TestDsfinvkSegment> testSegments = archive.OfType<TestDsfinvkSegment>().ToList();

        // Assert
        Assert.Equal(2, unknownSegments.Count);
        Assert.Single(testSegments);
    }

    // ============================================================================
    // Stream Operations Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task OpenRawStream_ReturnsReadableStream()
    {
        // Arrange
        byte[] testData = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        using MemoryStream stream = new MemoryStream(testData);
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(
            _testExportId,
            stream,
            new FakeDsfinvkVersionStrategy());

        // Act
        using Stream rawStream = archive.OpenRawStream();
        byte[] buffer = new byte[4];
        int bytesRead = await rawStream.ReadAsync(buffer);

        // Assert
        Assert.Equal(4, bytesRead);
        Assert.Equal(testData, buffer);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task OpenRawStream_MultipleCalls_IndependentStreams()
    {
        // Arrange
        byte[] testData = new byte[] { 0x01, 0x02, 0x03 };
        using MemoryStream stream = new MemoryStream(testData);
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(
            _testExportId,
            stream,
            new FakeDsfinvkVersionStrategy());

        // Act
        using Stream stream1 = archive.OpenRawStream();
        using Stream stream2 = archive.OpenRawStream();

        // Read from stream1
        byte[] buffer1 = new byte[1];
        await stream1.ReadAsync(buffer1);

        // Read from stream2 (should start from beginning)
        byte[] buffer2 = new byte[1];
        await stream2.ReadAsync(buffer2);

        // Assert
        Assert.Equal(0x01, buffer1[0]);
        Assert.Equal(0x01, buffer2[0]); // Independent stream, starts from beginning
        Assert.NotSame(stream1, stream2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task RawContent_ReturnsReadOnlyMemory()
    {
        // Arrange
        byte[] testData = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        using MemoryStream stream = new MemoryStream(testData);
        DsfinvkArchive archive = await DsfinvkArchive.FromStreamAsync(
            _testExportId,
            stream,
            new FakeDsfinvkVersionStrategy());

        // Act
        ReadOnlyMemory<byte> rawContent = archive.RawContent;

        // Assert
        Assert.Equal(4, rawContent.Length);
        Assert.Equal(testData, rawContent.ToArray());
    }

    // ============================================================================
    // Test Helpers
    // ============================================================================

    private sealed class FakeDsfinvkVersionStrategy : IDsfinvkVersionStrategy
    {
        public string Version => "2.3-test";
        public bool ParseAsyncWasCalled { get; private set; }
        public DsfinvkSegment[]? SegmentsToReturn { get; init; }

        public Task<IReadOnlyCollection<DsfinvkSegment>> ParseAsync(
            Stream archiveStream,
            CancellationToken cancellationToken = default)
        {
            ParseAsyncWasCalled = true;
            return Task.FromResult<IReadOnlyCollection<DsfinvkSegment>>(
                SegmentsToReturn ?? Array.Empty<DsfinvkSegment>());
        }
    }

    private sealed class TestDsfinvkSegment : DsfinvkSegment
    {
        public TestDsfinvkSegment(string fileName, byte[] content)
            : base(DsfinvkSegmentType.MasterData, fileName, content)
        {
        }
    }
}
