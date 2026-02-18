using System.Text.Json;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Clients.Models;
using Vectis.Fiskaly.SDK.SignDE.Clients.Requests;
using Vectis.Fiskaly.SDK.SignDE.Clients.Responses;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Clients;

public class ClientManagementClient(
    HttpClient httpClient,
    FiskalyHttpRequestExecutor executor,
    ILogger<ClientManagementClient> logger,
    JsonSerializerOptions serializerOptions)
    : IClientManagementClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly FiskalyHttpRequestExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly ILogger<ClientManagementClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

    public async Task<ClientResponse> CreateClientAsync(
        TssId tssId,
        ClientId clientId,
        CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Creating client {ClientId} for TSS {TssId}, SerialNumber: {SerialNumber}, Metadata: {HasMetadata}",
            clientId.Value, tssId.Value, request.SerialNumber, request.Metadata != null);

        ClientResponse clientResponse = await _executor.ExecutePutAsync<CreateClientRequest, ClientResponse>(
            _httpClient,
            $"tss/{tssId}/client/{clientId}",
            request,
            cancellationToken).ConfigureAwait(false);

        string createdId = (clientResponse.Id ?? clientId).ToString();
        string createdSerial = clientResponse.SerialNumber?.ToString() ?? "UNKNOWN";
        _logger.LogInformation("Client created successfully: {ClientId}, SerialNumber: {SerialNumber}",
            createdId, createdSerial);

        return clientResponse;
    }

    public async Task<ClientResponse> UpdateClientAsync(
        TssId tssId,
        ClientId clientId,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Updating client {ClientId} for TSS {TssId} to state {State} (Metadata: {HasMetadata})",
            clientId.Value, tssId.Value, request.State, request.Metadata != null);

        ClientResponse clientResponse = await _executor.ExecutePatchAsync<UpdateClientRequest, ClientResponse>(_httpClient,
            $"tss/{tssId}/client/{clientId}",
            request,
            cancellationToken).ConfigureAwait(false);

        string updatedId = (clientResponse.Id ?? clientId).ToString();
        string updatedState = clientResponse.State?.ToString() ?? "UNKNOWN";
        _logger.LogInformation("Client updated successfully: {ClientId}, New State: {State}",
            updatedId, updatedState);

        return clientResponse;
    }

    public async Task<ClientResponse> GetClientAsync(TssId tssId, ClientId clientId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving client: {ClientId} for TSS: {TssId}", clientId.Value, tssId.Value);

        ClientResponse client = await _executor.ExecuteGetAsync<ClientResponse>(_httpClient, $"tss/{tssId}/client/{clientId}", cancellationToken).ConfigureAwait(false);

        string retrievedId = (client.Id ?? clientId).ToString();
        string retrievedSerial = client.SerialNumber?.ToString() ?? "UNKNOWN";
        _logger.LogInformation("Retrieved client: {ClientId}, Serial: {SerialNumber}", retrievedId, retrievedSerial);

        return client;
    }

    public async Task<ClientListResponse> ListClientsAsync(
        TssId tssId,
        ListClientsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        string url = queryParameters?.BuildUrl($"tss/{tssId}/client") ?? $"tss/{tssId}/client";

        ClientListResponse response = await _executor.ExecuteGetAsync<ClientListResponse>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        int dataCount = response.Data?.Count ?? 0;
        int apiCount = response.Count ?? dataCount;
        _logger.LogInformation("Retrieved {Count} clients for TSS {TssId} (API count: {ApiCount})",
            dataCount, tssId.Value, apiCount);

        return response;
    }

    public async Task<ClientListResponse> ListAllClientsAsync(
        ListClientsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        string url = queryParameters?.BuildUrl("client") ?? "client";

        ClientListResponse response = await _executor.ExecuteGetAsync<ClientListResponse>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        int allDataCount = response.Data?.Count ?? 0;
        int allApiCount = response.Count ?? allDataCount;
        _logger.LogInformation("Retrieved {Count} clients across all TSS (API count: {ApiCount})",
            allDataCount, allApiCount);

        return response;
    }

    public async Task<MetadataCollection> GetClientMetadataAsync(TssId tssId, ClientId clientId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting metadata for client {ClientId} (TSS: {TssId})",
            clientId.Value, tssId.Value);

        MetadataCollection metadata = await MetadataOperations.GetAsync(
            _executor,
            _httpClient,
            $"tss/{tssId}/client/{clientId}/metadata",
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Retrieved metadata for client {ClientId} with {Count} entries",
            clientId.Value, metadata.Count);

        return metadata;
    }

    public async Task<MetadataCollection> UpdateClientMetadataAsync(TssId tssId, ClientId clientId, MetadataCollection metadata, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating client metadata for {ClientId} (TSS: {TssId}) with {Count} entries",
            clientId.Value, tssId.Value, metadata.Count);

        MetadataCollection result = await MetadataOperations.UpdateAsync(
            _executor,
            _httpClient,
            $"tss/{tssId}/client/{clientId}/metadata",
            metadata,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated client metadata for {ClientId}, new count: {Count}",
            clientId.Value, result.Count);

        return result;
    }

}
