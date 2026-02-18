namespace Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

public sealed class CashPointClosingSegment(string fileName, byte[] content)
    : DsfinvkSegment(DsfinvkSegmentType.CashPointClosing, fileName, content);
