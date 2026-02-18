using System.Text.Json;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

public abstract class DsfinvkSegment
{
    private readonly byte[] _content;

    protected DsfinvkSegment(DsfinvkSegmentType type, string fileName, byte[] content)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Segment file name cannot be null or whitespace.", nameof(fileName));
        }

        Type = type;
        FileName = fileName;
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public DsfinvkSegmentType Type { get; }

    public string FileName { get; }

    public Stream OpenStream() => new MemoryStream(_content, writable: false);

    public JsonDocument OpenJsonDocument(JsonDocumentOptions options = default) =>
        JsonDocument.Parse(_content, options);
}
