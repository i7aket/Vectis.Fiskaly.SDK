namespace Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

public sealed class UnknownDsfinvkSegment(string fileName, byte[] content)
    : DsfinvkSegment(DsfinvkSegmentType.Unknown, fileName, content);
