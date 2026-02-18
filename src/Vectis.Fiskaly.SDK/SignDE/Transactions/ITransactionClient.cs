using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions;

/// <summary>
/// Typed wrapper over SIGN DE transaction endpoints (/api/v2/tss/*/tx*).
/// </summary>
public interface ITransactionClient
{
    /// <summary>
    /// Executes PUT /api/v2/tss/{tss_id}/tx/{tx_id_or_number} (operationId: upsertTransaction) to create revision 1 in ACTIVE state.
    /// </summary>
    Task<TxResponse> StartTransactionAsync(
        TssId tssId,
        TxId transactionId,
        StartTransactionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes PUT /api/v2/tss/{tss_id}/tx/{tx_id_or_number} to submit another ACTIVE revision.
    /// </summary>
    Task<TxResponse> UpdateTransactionAsync(
        TssId tssId,
        TxId transactionId,
        UpdateTransactionRequest request,
        int? txRevision = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes PUT /api/v2/tss/{tss_id}/tx/{tx_id_or_number} with a FINISHED payload.
    /// </summary>
    Task<TxResponse> FinishTransactionAsync(
        TssId tssId,
        TxId transactionId,
        FinishTransactionRequest request,
        int? txRevision = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes PUT /api/v2/tss/{tss_id}/tx/{tx_id_or_number} with a CANCELLED payload.
    /// </summary>
    Task<TxResponse> CancelTransactionAsync(
        TssId tssId,
        TxId transactionId,
        CancelTransactionRequest request,
        int? txRevision = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}/tx/{tx_id_or_number}.
    /// </summary>
    Task<TxResponse> GetTransactionAsync(
        TssId tssId,
        TxId transactionId,
        int? txRevision = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}/tx to list transactions with optional filters.
    /// </summary>
    Task<TxListResponse> ListTransactionsAsync(
        TssId tssId,
        ListTransactionsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-paginates GET /api/v2/tss/{tss_id}/tx until all pages are returned.
    /// </summary>
    Task<TxListResponse> ListAllTransactionsAsync(
        ListTransactionsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-paginates GET /api/v2/tss/{tss_id}/client/{client_id}/tx.
    /// </summary>
    Task<TxListResponse> ListClientTransactionsAsync(
        TssId tssId,
        ClientId clientId,
        ListTransactionsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}/tx/{tx_id_or_number}/metadata.
    /// </summary>
    Task<MetadataCollection> GetTransactionMetadataAsync(
        TssId tssId,
        TxId transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls PATCH /api/v2/tss/{tss_id}/tx/{tx_id_or_number}/metadata.
    /// </summary>
    Task<MetadataCollection> UpdateTransactionMetadataAsync(
        TssId tssId,
        TxId transactionId,
        MetadataCollection metadata,
        CancellationToken cancellationToken = default);
}
