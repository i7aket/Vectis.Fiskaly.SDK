using System.Text.Json;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Tss.Models;
using Vectis.Fiskaly.SDK.SignDE.Tss.Requests;
using Vectis.Fiskaly.SDK.SignDE.Tss.Responses;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Tss;

public class TssClient(
    HttpClient httpClient,
    FiskalyHttpRequestExecutor executor,
    ILogger<TssClient> logger,
    JsonSerializerOptions serializerOptions)
    : ITssClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly FiskalyHttpRequestExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly ILogger<TssClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

    public async Task<TssResponse> CreateTssAsync(
        TssId tssId,
        MetadataCollection? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogCreatingTss(tssId.Value.ToString());

        CreateTssRequest requestDto = new CreateTssRequest
        {
            Metadata = metadata
        };

        TssResponse tssResponse = await _executor.ExecutePutAsync<CreateTssRequest, TssResponse>(
            _httpClient,
            $"tss/{tssId}",
            requestDto,
            cancellationToken).ConfigureAwait(false);

        string createdId = (tssResponse.Id ?? tssId).ToString();
        string createdState = tssResponse.State?.ToString() ?? "UNKNOWN";
        _logger.LogTssCreated(createdId, createdState, tssResponse.AdminPuk.HasValue ? "Yes" : "No");

        if (tssResponse.AdminPuk.HasValue)
        {
            _logger.LogAdminPukReturned(tssId.Value.ToString());
        }

        return tssResponse;
    }

    public async Task<TssResponse> UpdateTssAsync(
        TssId tssId,
        UpdateTssRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogUpdatingTss(tssId.Value.ToString(), request.State.ToString(), request.Description ?? "(none)", request.Metadata != null);

        TssResponse tssResponse = await ExecuteUpdateAsync(tssId, request, cancellationToken).ConfigureAwait(false);

        string updatedState = tssResponse.State?.ToString() ?? "UNKNOWN";
        _logger.LogTssUpdated(tssId.Value.ToString(), updatedState, tssResponse.Description ?? "(none)");

        return tssResponse;
    }

    public async Task<TssResponse> GetTssAsync(TssId tssId, CancellationToken cancellationToken = default)
    {
        _logger.LogRetrievingTss(tssId.Value.ToString());

        TssResponse tss = await _executor.ExecuteGetAsync<TssResponse>(_httpClient, $"tss/{tssId}", cancellationToken).ConfigureAwait(false);

        string retrievedId = tss.Id?.ToString() ?? tssId.Value.ToString();
        string retrievedState = tss.State?.ToString() ?? "UNKNOWN";
        _logger.LogTssRetrieved(retrievedId, retrievedState);

        return tss;
    }

    public async Task<ListTssResponse> ListTssAsync(ListTssQueryParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        string url = queryParameters?.BuildUrl("tss") ?? "tss";

        ListTssResponse response = await _executor.ExecuteGetAsync<ListTssResponse>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        int listCount = response.Count ?? 0;
        string listType = response.Type?.ToString() ?? "UNKNOWN";
        string listEnv = response.Env?.ToString() ?? "UNKNOWN";
        string listVersion = response.Version ?? "UNKNOWN";
        _logger.LogTssListRetrieved(listCount, listType, listEnv, listVersion);

        return response;
    }

    public async Task<MetadataCollection> GetTssMetadataAsync(TssId tssId, CancellationToken cancellationToken = default)
    {
        _logger.LogGettingTssMetadata(tssId.Value.ToString());

        MetadataCollection metadata = await MetadataOperations.GetAsync(
            _executor,
            _httpClient,
            $"tss/{tssId}/metadata",
            cancellationToken).ConfigureAwait(false);

        _logger.LogTssMetadataRetrieved(tssId.Value.ToString(), metadata.Count);

        return metadata;
    }

    public async Task<MetadataCollection> UpdateTssMetadataAsync(TssId tssId, MetadataCollection metadata, CancellationToken cancellationToken = default)
    {
        _logger.LogUpdatingTssMetadata(tssId.Value.ToString(), metadata.Count);

        MetadataCollection result = await MetadataOperations.UpdateAsync(
            _executor,
            _httpClient,
            $"tss/{tssId}/metadata",
            metadata,
            cancellationToken).ConfigureAwait(false);

        _logger.LogTssMetadataUpdated(tssId.Value.ToString(), result.Count);

        return result;
    }

    private Task<TssResponse> ExecuteUpdateAsync(
        TssId tssId,
        UpdateTssRequest request,
        CancellationToken cancellationToken)
    {
        return _executor.ExecutePatchAsync<UpdateTssRequest, TssResponse>(
            _httpClient,
            $"tss/{tssId}",
            request,
            cancellationToken);
    }
}
