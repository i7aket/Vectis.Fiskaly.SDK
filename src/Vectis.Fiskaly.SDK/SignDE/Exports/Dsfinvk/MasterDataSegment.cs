namespace Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;

public sealed class MasterDataSegment(string fileName, byte[] content)
    : DsfinvkSegment(DsfinvkSegmentType.MasterData, fileName, content);
