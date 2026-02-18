using System.Text.Json;
using Vectis.Fiskaly.SDK.Guards;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions;

public class TransactionClient(
    HttpClient httpClient,
    FiskalyHttpRequestExecutor executor,
    ILogger<TransactionClient> logger,
    JsonSerializerOptions serializerOptions)
    : ITransactionClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly FiskalyHttpRequestExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly ILogger<TransactionClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

    public async Task<TxResponse> StartTransactionAsync(
        TssId tssId,
        TxId transactionId,
        StartTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIf.Default(tssId, nameof(tssId));
        ThrowIf.Default(transactionId, nameof(transactionId));
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogStartingTransaction(transactionId, request.ClientId);

        TxResponse transactionResponse = await ExecuteTransactionHttpAsync(
            tssId,
            transactionId,
            request,
            revision: 1,
            cancellationToken
        ).ConfigureAwait(false);

        _logger.LogTransactionStarted(transactionResponse.Id ?? transactionId, transactionResponse.Number, request.Metadata?.Count);

        return transactionResponse;
    }

    public async Task<TxResponse> FinishTransactionAsync(
        TssId tssId,
        TxId transactionId,
        FinishTransactionRequest request,
        int? txRevision = null,
        CancellationToken cancellationToken = default)
    {
        if (txRevision.HasValue && txRevision.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(txRevision),
                txRevision.Value,
                "Transaction revision must be >= 1 (OpenAPI v2.1.35 constraint).");
        }

        _logger.LogFinishingTransaction(transactionId);

        TxResponse transactionResponse = await ExecuteTransactionOperationAsync(
            tssId,
            transactionId,
            request,
            txRevision,
            requireActiveState: false,  // FINISH can operate on ACTIVE transactions
            operationName: "finish",
            cancellationToken).ConfigureAwait(false);

        bool? hasSignature = transactionResponse.Signature?.Value is { Length: > 0 };
        _logger.LogTransactionFinished(transactionResponse.Id ?? transactionId, transactionResponse.Number, hasSignature);

        return transactionResponse;
    }

    public async Task<TxResponse> UpdateTransactionAsync(
        TssId tssId,
        TxId transactionId,
        UpdateTransactionRequest request,
        int? txRevision = null,
        CancellationToken cancellationToken = default)
    {
        if (txRevision.HasValue && txRevision.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(txRevision),
                txRevision.Value,
                "Transaction revision must be >= 1 (OpenAPI v2.1.35 constraint).");
        }

        _logger.LogUpdatingTransaction(transactionId);

        TxResponse transactionResponse = await ExecuteTransactionOperationAsync(
            tssId,
            transactionId,
            request,
            txRevision,
            requireActiveState: true,  // UPDATE requires ACTIVE state
            operationName: "update",
            cancellationToken).ConfigureAwait(false);

        _logger.LogTransactionUpdated(transactionResponse.Id ?? transactionId, request.Metadata?.Count);

        return transactionResponse;
    }

    public async Task<TxResponse> CancelTransactionAsync(
        TssId tssId,
        TxId transactionId,
        CancelTransactionRequest request,
        int? txRevision = null,
        CancellationToken cancellationToken = default)
    {
        if (txRevision.HasValue && txRevision.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(txRevision),
                txRevision.Value,
                "Transaction revision must be >= 1 (OpenAPI v2.1.35 constraint).");
        }

        _logger.LogCancellingTransaction(transactionId);

        TxResponse transactionResponse = await ExecuteTransactionOperationAsync(
            tssId,
            transactionId,
            request,
            txRevision,
            requireActiveState: true,  // CANCEL requires ACTIVE state
            operationName: "cancel",
            cancellationToken).ConfigureAwait(false);

        _logger.LogTransactionCancelled(transactionResponse.Id ?? transactionId, request.Metadata?.GetValueOrDefault("cancellation_reason") ?? "Not specified");

        return transactionResponse;
    }


    public async Task<TxResponse> GetTransactionAsync(
        TssId tssId,
        TxId transactionId,
        int? txRevision = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIf.Default(tssId, nameof(tssId));
        ThrowIf.Default(transactionId, nameof(transactionId));

        if (txRevision.HasValue && txRevision.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(txRevision),
                txRevision.Value,
                "Transaction revision must be >= 1 (OpenAPI v2.1.35 constraint).");
        }

        string revisionInfo = txRevision.HasValue ? $" at revision {txRevision.Value}" : "";
        _logger.LogRetrievingTransaction(transactionId, tssId.Value.ToString(), revisionInfo);

        string transactionKey = transactionId.ToString();
        string path = $"tss/{tssId.Value}/tx/{transactionKey}";
        if (txRevision.HasValue)
        {
            path += $"?tx_revision={txRevision.Value}";
        }

        TxResponse transaction = await _executor.ExecuteGetAsync<TxResponse>(_httpClient, path, cancellationToken).ConfigureAwait(false);

        string? stateName = transaction.State?.ToApiString();
        _logger.LogTransactionRetrieved(transaction.Id, transaction.Number, stateName, transaction.Revision);

        return transaction;
    }

    public async Task<TxListResponse> ListTransactionsAsync(
        TssId tssId,
        ListTransactionsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIf.Default(tssId, nameof(tssId));

        string url = queryParameters?.BuildUrl($"tss/{tssId.Value}/tx") ?? $"tss/{tssId.Value}/tx";

        TxListResponse response = await _executor.ExecuteGetAsync<TxListResponse>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        _logger.LogTransactionsForTss(response.Data?.Count, tssId.Value.ToString());

        return response;
    }

    public async Task<TxListResponse> ListAllTransactionsAsync(
        ListTransactionsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        string url = queryParameters?.BuildUrl("tx") ?? "tx";

        TxListResponse response = await _executor.ExecuteGetAsync<TxListResponse>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        _logger.LogTransactionsAll(response.Data?.Count);

        return response;
    }

    public async Task<TxListResponse> ListClientTransactionsAsync(
        TssId tssId,
        ClientId clientId,
        ListTransactionsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIf.Default(tssId, nameof(tssId));
        ThrowIf.Default(clientId, nameof(clientId));

        string url = queryParameters?.BuildUrl($"tss/{tssId.Value}/client/{clientId.Value}/tx") ?? $"tss/{tssId.Value}/client/{clientId.Value}/tx";

        TxListResponse response = await _executor.ExecuteGetAsync<TxListResponse>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        _logger.LogTransactionsForClient(response.Data?.Count, clientId.Value.ToString());

        return response;
    }

    public async Task<MetadataCollection> GetTransactionMetadataAsync(
        TssId tssId,
        TxId transactionId,
        CancellationToken cancellationToken = default)
    {
        ThrowIf.Default(tssId, nameof(tssId));
        ThrowIf.Default(transactionId, nameof(transactionId));

        string transactionKey = transactionId.ToString();

        _logger.LogGettingTransactionMetadata(transactionKey, tssId.Value.ToString());

        MetadataCollection metadata = await MetadataOperations.GetAsync(
            _executor,
            _httpClient,
            $"tss/{tssId.Value}/tx/{transactionKey}/metadata",
            cancellationToken).ConfigureAwait(false);

        _logger.LogTransactionMetadataRetrieved(transactionKey, metadata.Count);

        return metadata;
    }

    public async Task<MetadataCollection> UpdateTransactionMetadataAsync(
        TssId tssId,
        TxId transactionId,
        MetadataCollection metadata,
        CancellationToken cancellationToken = default)
    {
        ThrowIf.Default(tssId, nameof(tssId));
        ThrowIf.Default(transactionId, nameof(transactionId));
        ArgumentNullException.ThrowIfNull(metadata, nameof(metadata));

        string transactionKey = transactionId.ToString();

        _logger.LogUpdatingTransactionMetadata(transactionKey, tssId.Value.ToString(), metadata.Count);

        MetadataCollection result = await MetadataOperations.UpdateAsync(
            _executor,
            _httpClient,
            $"tss/{tssId.Value}/tx/{transactionKey}/metadata",
            metadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogTransactionMetadataUpdated(transactionKey, result.Count);

        return result;
    }

    internal async Task<int> ResolveRevisionAsync(
        TssId tssId,
        TxId transactionId,
        int? explicitRevision,
        bool requireActiveState,
        string operationName,
        CancellationToken cancellationToken)
    {
        if (explicitRevision != null)
        {
            _logger.LogUsingExplicitRevision(explicitRevision.Value, operationName, transactionId);
            return explicitRevision.Value;
        }

        _logger.LogAutoFetchingRevision(operationName, transactionId);
        TxResponse currentTransaction = await GetTransactionAsync(tssId, transactionId, null, cancellationToken)
            .ConfigureAwait(false);

        TxState? currentState = currentTransaction.State;
        string? currentStateName = currentState?.ToApiString();
        if (requireActiveState && currentState != TxState.Active)
        {
            string reportedState = currentStateName ?? "null";
            string message = $"Cannot {operationName} transaction {transactionId} in state {reportedState}. " +
                             $"Expected state: {TxState.Active.ToApiString()}. " +
                             $"Only ACTIVE transactions can be {operationName}ed.";
            _logger.LogInvalidTransactionState(operationName, transactionId, currentStateName, TxState.Active.ToApiString());
            throw new InvalidOperationException(message);
        }

        int? currentRevision = currentTransaction.LatestRevision ?? currentTransaction.Revision;
        if (currentRevision is null)
        {
            string message = $"Cannot resolve next revision for transaction {transactionId}: server response omitted both latest_revision and revision.";
            throw new InvalidOperationException(message);
        }

        int nextRevision = checked(currentRevision.Value + 1);
        _logger.LogRevisionResolved(operationName, transactionId, currentRevision, nextRevision);

        return nextRevision;
    }

    private async Task<TxResponse> ExecuteTransactionOperationAsync<TRequest>(
        TssId tssId,
        TxId transactionId,
        TRequest request,
        int? txRevision,
        bool requireActiveState,
        string operationName,
        CancellationToken cancellationToken)
        where TRequest : TxRequest
    {
        ThrowIf.Default(tssId, nameof(tssId));
        ThrowIf.Default(transactionId, nameof(transactionId));
        ArgumentNullException.ThrowIfNull(request);

        int revision = await ResolveRevisionAsync(
            tssId,
            transactionId,
            txRevision,
            requireActiveState,
            operationName,
            cancellationToken).ConfigureAwait(false);

        return await ExecuteTransactionHttpAsync(
            tssId,
            transactionId,
            request,
            revision,
            cancellationToken).ConfigureAwait(false);
    }

    internal async Task<TxResponse> ExecuteTransactionHttpAsync<TRequest>(
        TssId tssId,
        TxId transactionId,
        TRequest request,
        int revision,
        CancellationToken cancellationToken)
        where TRequest : TxRequest
    {
        string transactionKey = transactionId.ToString();

        _logger.LogExecutingTransactionPut(tssId.Value.ToString(), transactionKey, revision);

        return await _executor.ExecutePutAsync<TRequest, TxResponse>(
            _httpClient,
            $"tss/{tssId.Value}/tx/{transactionKey}?tx_revision={revision}",
            request,
            cancellationToken).ConfigureAwait(false);
    }
}
