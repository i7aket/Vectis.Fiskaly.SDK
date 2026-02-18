namespace Vectis.Fiskaly.SDK.SignDE.Tss;

internal static partial class TssClientLog
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Creating TSS with ID: {TssId}")]
    internal static partial void LogCreatingTss(this ILogger logger, string tssId);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "TSS created successfully: {TssId}, State: {State}, AdminPuk: {AdminPukPresent}")]
    internal static partial void LogTssCreated(this ILogger logger, string tssId, string state, string adminPukPresent);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "Admin PUK returned for TSS {TssId}. This value MUST be stored securely as it cannot be retrieved later.")]
    internal static partial void LogAdminPukReturned(this ILogger logger, string tssId);

    [LoggerMessage(EventId = 4016, Level = LogLevel.Information, Message = "Updating TSS {TssId} to {State} (Description: {Description}, Metadata: {HasMetadata})")]
    internal static partial void LogUpdatingTss(this ILogger logger, string tssId, string state, string description, bool hasMetadata);

    [LoggerMessage(EventId = 4017, Level = LogLevel.Information, Message = "TSS {TssId} updated (State: {State}, Description: {Description})")]
    internal static partial void LogTssUpdated(this ILogger logger, string tssId, string state, string description);

    [LoggerMessage(EventId = 4009, Level = LogLevel.Debug, Message = "Retrieving TSS: {TssId}")]
    internal static partial void LogRetrievingTss(this ILogger logger, string tssId);

    [LoggerMessage(EventId = 4010, Level = LogLevel.Information, Message = "Retrieved TSS: {TssId}, State: {State}")]
    internal static partial void LogTssRetrieved(this ILogger logger, string tssId, string state);

    [LoggerMessage(EventId = 4011, Level = LogLevel.Information, Message = "Retrieved {Count} TSS (Type: {Type}, Env: {Env}, Version: {Version})")]
    internal static partial void LogTssListRetrieved(this ILogger logger, int count, string type, string env, string version);

    [LoggerMessage(EventId = 4012, Level = LogLevel.Debug, Message = "Getting metadata for TSS {TssId}")]
    internal static partial void LogGettingTssMetadata(this ILogger logger, string tssId);

    [LoggerMessage(EventId = 4013, Level = LogLevel.Information, Message = "Retrieved metadata for TSS {TssId} with {Count} entries")]
    internal static partial void LogTssMetadataRetrieved(this ILogger logger, string tssId, int count);

    [LoggerMessage(EventId = 4014, Level = LogLevel.Information, Message = "Updating TSS metadata for {TssId} with {Count} entries")]
    internal static partial void LogUpdatingTssMetadata(this ILogger logger, string tssId, int count);

    [LoggerMessage(EventId = 4015, Level = LogLevel.Information, Message = "Updated TSS metadata for {TssId}, new count: {Count}")]
    internal static partial void LogTssMetadataUpdated(this ILogger logger, string tssId, int count);
}
