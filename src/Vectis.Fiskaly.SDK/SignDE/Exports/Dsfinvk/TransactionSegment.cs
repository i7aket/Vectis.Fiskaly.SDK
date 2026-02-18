namespace Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

public sealed class TransactionSegment(string fileName, byte[] content)
    : DsfinvkSegment(DsfinvkSegmentType.TransactionData, fileName, content);
