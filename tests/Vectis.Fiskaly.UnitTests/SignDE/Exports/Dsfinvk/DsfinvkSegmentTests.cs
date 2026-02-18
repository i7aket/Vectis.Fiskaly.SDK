using System.Text;
using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports.Dsfinvk;

public class DsfinvkSegmentTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_SetsProperties_Correctly()
    {
        // Arrange
        string fileName = "test-segment.json";
        byte[] content = Encoding.UTF8.GetBytes("{\"test\":\"data\"}");

        // Act
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Assert
        Assert.Equal(DsfinvkSegmentType.Unknown, segment.Type);
        Assert.Equal(fileName, segment.FileName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullFileName_ThrowsArgumentException()
    {
        // Arrange
        byte[] content = Encoding.UTF8.GetBytes("{\"test\":\"data\"}");

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new UnknownDsfinvkSegment(null!, content));
        Assert.Contains("Segment file name cannot be null or whitespace", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithEmptyFileName_ThrowsArgumentException()
    {
        // Arrange
        byte[] content = Encoding.UTF8.GetBytes("{\"test\":\"data\"}");

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new UnknownDsfinvkSegment("", content));
        Assert.Contains("Segment file name cannot be null or whitespace", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithWhitespaceFileName_ThrowsArgumentException()
    {
        // Arrange
        byte[] content = Encoding.UTF8.GetBytes("{\"test\":\"data\"}");

        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new UnknownDsfinvkSegment("   ", content));
        Assert.Contains("Segment file name cannot be null or whitespace", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullContent_ThrowsArgumentNullException()
    {
        // Arrange
        string fileName = "test-segment.json";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UnknownDsfinvkSegment(fileName, null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void OpenStream_ReturnsReadOnlyStream_WithCorrectContent()
    {
        // Arrange
        string fileName = "test-segment.json";
        string expectedContent = "{\"test\":\"data\"}";
        byte[] content = Encoding.UTF8.GetBytes(expectedContent);
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Act
        using Stream stream = segment.OpenStream();
        using StreamReader reader = new StreamReader(stream);
        string actualContent = reader.ReadToEnd();

        // Assert
        Assert.Equal(expectedContent, actualContent);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void OpenStream_ReturnsIndependentStreams()
    {
        // Arrange
        string fileName = "test-segment.json";
        byte[] content = Encoding.UTF8.GetBytes("{\"test\":\"data\"}");
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Act
        using Stream stream1 = segment.OpenStream();
        using Stream stream2 = segment.OpenStream();

        // Read from first stream
        byte[] buffer1 = new byte[5];
        stream1.Read(buffer1, 0, 5);

        // Second stream should still be at position 0
        Assert.Equal(0, stream2.Position);
        Assert.Equal(5, stream1.Position);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void OpenStream_WithEmptyContent_ReturnsEmptyStream()
    {
        // Arrange
        string fileName = "empty-segment.json";
        byte[] content = Array.Empty<byte>();
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Act
        using Stream stream = segment.OpenStream();

        // Assert
        Assert.Equal(0, stream.Length);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void OpenStream_ReturnsNonWritableStream()
    {
        // Arrange
        string fileName = "test-segment.json";
        byte[] content = Encoding.UTF8.GetBytes("{\"test\":\"data\"}");
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Act
        using Stream stream = segment.OpenStream();

        // Assert
        Assert.False(stream.CanWrite);
        Assert.True(stream.CanRead);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void FileName_PreservesOriginalValue()
    {
        // Arrange
        string fileName = "master_data/stammdaten.json";
        byte[] content = Encoding.UTF8.GetBytes("{}");
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Act & Assert
        Assert.Equal(fileName, segment.FileName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void OpenJsonDocument_WithValidJson_ReturnsJsonDocument()
    {
        // Arrange
        string fileName = "test-segment.json";
        string jsonContent = """
                             {
                                 "name": "Test",
                                 "value": 123
                             }
                             """;
        byte[] content = Encoding.UTF8.GetBytes(jsonContent);
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Act
        using JsonDocument jsonDoc = segment.OpenJsonDocument();
        JsonElement root = jsonDoc.RootElement;

        // Assert
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal("Test", root.GetProperty("name").GetString());
        Assert.Equal(123, root.GetProperty("value").GetInt32());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void OpenJsonDocument_WithInvalidJson_Throws()
    {
        // Arrange
        string fileName = "invalid-segment.json";
        byte[] content = Encoding.UTF8.GetBytes("{invalid json}");
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Act & Assert
        // JsonDocument.Parse can throw JsonException or ArgumentException depending on the error
        Assert.ThrowsAny<Exception>(() => segment.OpenJsonDocument());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void OpenJsonDocument_WithEmptyContent_Throws()
    {
        // Arrange
        string fileName = "empty-segment.json";
        byte[] content = Array.Empty<byte>();
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Act & Assert
        // JsonDocument.Parse can throw JsonException or ArgumentException for empty content
        Assert.ThrowsAny<Exception>(() => segment.OpenJsonDocument());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Type_ReturnsCorrectSegmentType()
    {
        // Arrange
        string fileName = "test-segment.json";
        byte[] content = Encoding.UTF8.GetBytes("{}");

        // Act
        UnknownDsfinvkSegment segment = new UnknownDsfinvkSegment(fileName, content);

        // Assert
        Assert.Equal(DsfinvkSegmentType.Unknown, segment.Type);
    }
}
