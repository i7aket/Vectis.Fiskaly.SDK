using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions;

internal static partial class TransactionClientLog
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Starting transaction {TxId} for client {ClientId}")]
    internal static partial void LogStartingTransaction(this ILogger logger, TxId txId, ClientId clientId);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Transaction started successfully: {TxId}, Number: {Number}, Metadata: {MetadataCount}")]
    internal static partial void LogTransactionStarted(this ILogger logger, TxId? txId, long? number, int? metadataCount);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Finishing transaction {TxId}")]
    internal static partial void LogFinishingTransaction(this ILogger logger, TxId txId);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Transaction finished successfully: {TxId}, Number: {Number}, Signature: {HasSignature}")]
    internal static partial void LogTransactionFinished(this ILogger logger, TxId? txId, long? number, bool? hasSignature);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Updating transaction {TxId}")]
    internal static partial void LogUpdatingTransaction(this ILogger logger, TxId txId);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "Transaction updated successfully: {TxId}, Metadata: {MetadataCount}")]
    internal static partial void LogTransactionUpdated(this ILogger logger, TxId? txId, int? metadataCount);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information, Message = "Cancelling transaction {TxId}")]
    internal static partial void LogCancellingTransaction(this ILogger logger, TxId txId);

    [LoggerMessage(EventId = 3007, Level = LogLevel.Information, Message = "Transaction cancelled successfully: {TxId}, Reason: {Reason}")]
    internal static partial void LogTransactionCancelled(this ILogger logger, TxId? txId, string? reason);

    [LoggerMessage(EventId = 3008, Level = LogLevel.Debug, Message = "Retrieving transaction {TxId} for TSS {TssId}{RevisionInfo}")]
    internal static partial void LogRetrievingTransaction(this ILogger logger, TxId txId, string tssId, string revisionInfo);

    [LoggerMessage(EventId = 3009, Level = LogLevel.Information, Message = "Retrieved transaction: {TxId}, Number: {Number}, State: {State}, Revision: {Revision}")]
    internal static partial void LogTransactionRetrieved(this ILogger logger, TxId? txId, long? number, string? state, int? revision);

    [LoggerMessage(EventId = 3010, Level = LogLevel.Information, Message = "Retrieved {Count} transactions for TSS {TssId}")]
    internal static partial void LogTransactionsForTss(this ILogger logger, int? count, string tssId);

    [LoggerMessage(EventId = 3011, Level = LogLevel.Information, Message = "Retrieved {Count} transactions across all TSS")]
    internal static partial void LogTransactionsAll(this ILogger logger, int? count);

    [LoggerMessage(EventId = 3012, Level = LogLevel.Information, Message = "Retrieved {Count} transactions for client {ClientId}")]
    internal static partial void LogTransactionsForClient(this ILogger logger, int? count, string clientId);

    [LoggerMessage(EventId = 3013, Level = LogLevel.Debug, Message = "Getting metadata for transaction {TxId} (TSS: {TssId})")]
    internal static partial void LogGettingTransactionMetadata(this ILogger logger, string txId, string tssId);

    [LoggerMessage(EventId = 3014, Level = LogLevel.Information, Message = "Retrieved metadata for transaction {TxId} with {Count} entries")]
    internal static partial void LogTransactionMetadataRetrieved(this ILogger logger, string txId, int count);

    [LoggerMessage(EventId = 3015, Level = LogLevel.Information, Message = "Updating transaction metadata for {TxId} (TSS: {TssId}) with {Count} entries")]
    internal static partial void LogUpdatingTransactionMetadata(this ILogger logger, string txId, string tssId, int count);

    [LoggerMessage(EventId = 3016, Level = LogLevel.Information, Message = "Updated transaction metadata for {TxId}, new count: {Count}")]
    internal static partial void LogTransactionMetadataUpdated(this ILogger logger, string txId, int count);

    [LoggerMessage(EventId = 3017, Level = LogLevel.Debug, Message = "Using explicit revision {Revision} for {Operation} transaction {TxId}")]
    internal static partial void LogUsingExplicitRevision(this ILogger logger, int revision, string operation, TxId txId);

    [LoggerMessage(EventId = 3018, Level = LogLevel.Debug, Message = "Auto-fetching current revision for {Operation} transaction {TxId}")]
    internal static partial void LogAutoFetchingRevision(this ILogger logger, string operation, TxId txId);

    [LoggerMessage(EventId = 3019, Level = LogLevel.Debug, Message = "Resolved revision for {Operation} transaction {TxId}: current={CurrentRevision}, next={NextRevision}")]
    internal static partial void LogRevisionResolved(this ILogger logger, string operation, TxId txId, int? currentRevision, int? nextRevision);

    [LoggerMessage(EventId = 3020, Level = LogLevel.Error, Message = "Cannot {Operation} transaction {TxId} in state {State}. Expected state: {ExpectedState}. Only ACTIVE transactions can be {Operation}ed.")]
    internal static partial void LogInvalidTransactionState(this ILogger logger, string operation, TxId txId, string? state, string expectedState);

    [LoggerMessage(EventId = 3021, Level = LogLevel.Debug, Message = "Executing transaction HTTP PUT: {TssId}/tx/{TxId}?tx_revision={Revision}")]
    internal static partial void LogExecutingTransactionPut(this ILogger logger, string tssId, string txId, int revision);
}
