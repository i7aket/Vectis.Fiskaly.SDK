using System.Text.Json;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.Responses;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Exports;

public class ExportClient(
    HttpClient httpClient,
    FiskalyHttpRequestExecutor executor,
    ILogger<ExportClient> logger,
    JsonSerializerOptions serializerOptions,
    IDsfinvkVersionStrategy dsfinvkStrategy)
    : IExportClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly FiskalyHttpRequestExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly ILogger<ExportClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
    private readonly IDsfinvkVersionStrategy _dsfinvkStrategy = dsfinvkStrategy ?? throw new ArgumentNullException(nameof(dsfinvkStrategy));

    public async Task<ExportJob> TriggerFullExportAsync(
        TssId tssId,
        ExportId exportId,
        DsfinvkFullExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Triggering full DSFinV-K export {ExportId} for TSS {TssId} (StartDate: {StartDate}, EndDate: {EndDate}, ClientId: {ClientId})",
            exportId.Value, tssId.Value, request.StartDate, request.EndDate, request.ClientId?.ToString() ?? "all");

        string url = request.BuildUrl($"tss/{tssId}/export/{exportId}");

        ExportJob exportResponse = await _executor.ExecutePutAsync<ExportJob>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Full export triggered: {ExportId}, State: {State}", exportResponse.Id.Value, exportResponse.State);

        return exportResponse;
    }

    public async Task<ExportJob> TriggerClientExportAsync(
        TssId tssId,
        ExportId exportId,
        DsfinvkClientExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Triggering client-specific DSFinV-K export {ExportId} for TSS {TssId}, Client {ClientId}",
            exportId.Value, tssId.Value, request.ClientId.ToString());

        string url = request.BuildUrl($"tss/{tssId}/export/{exportId}");

        ExportJob exportResponse = await _executor.ExecutePutAsync<ExportJob>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Client export triggered: {ExportId}, State: {State}", exportResponse.Id.Value, exportResponse.State);

        return exportResponse;
    }

    public async Task<ExportJob> TriggerLogExportAsync(
        TssId tssId,
        ExportId exportId,
        DsfinvkLogExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Triggering log DSFinV-K export {ExportId} for TSS {TssId}, TransactionNumber: {TransactionNumber}",
            exportId.Value, tssId.Value, request.TransactionNumber?.ToString() ?? "all");

        string url = request.BuildUrl($"tss/{tssId}/export/{exportId}");

        ExportJob exportResponse = await _executor.ExecutePutAsync<ExportJob>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Log export triggered: {ExportId}, State: {State}", exportResponse.Id.Value, exportResponse.State);

        return exportResponse;
    }

    public async Task<ExportJob> GetExportAsync(
        TssId tssId,
        ExportId exportId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching export {ExportId} status for TSS {TssId}", exportId.Value, tssId.Value);

        ExportJob exportResponse = await _executor.ExecuteGetAsync<ExportJob>(_httpClient, $"tss/{tssId.Value}/export/{exportId.Value}", cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Export {ExportId} state: {State}", exportId.Value, exportResponse.State);

        return exportResponse;
    }

    public async Task<DsfinvkArchive> DownloadExportAsync(
        TssId tssId,
        ExportId exportId,
        IDsfinvkVersionStrategy? strategy = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading export {ExportId} for TSS {TssId}", exportId.Value, tssId.Value);

        ExportJob export = await GetExportAsync(tssId, exportId, cancellationToken).ConfigureAwait(false);

        if (export.State != ExportState.Completed)
        {
            string message = export.State == ExportState.Error
                ? $"Cannot download export {exportId.Value}: export failed with state ERROR. Exception: {export.ExceptionCode}"
                : $"Cannot download export {exportId.Value}: state is {export.State}, expected COMPLETED";

            _logger.LogWarning(message);
            throw new InvalidOperationException(message);
        }

        using HttpResponseMessage response = await _httpClient.GetAsync($"tss/{tssId.Value}/export/{exportId.Value}/file", cancellationToken).ConfigureAwait(false);
        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        IDsfinvkVersionStrategy effectiveStrategy = strategy ?? _dsfinvkStrategy;

        return await DsfinvkArchive.FromStreamAsync(exportId, stream, effectiveStrategy, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExportJob> CancelExportAsync(
        TssId tssId,
        ExportId exportId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling export {ExportId} for TSS {TssId}", exportId.Value, tssId.Value);

        ExportJob exportResponse = await _executor.ExecuteDeleteAsync<ExportJob>(_httpClient, $"tss/{tssId.Value}/export/{exportId.Value}", cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Export cancelled: {ExportId}, State: {State}", exportResponse.Id.Value, exportResponse.State);

        return exportResponse;
    }

    public async Task<ListExportsResponse> ListExportsAsync(
        TssId tssId,
        ListExportsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        string url = queryParameters?.BuildUrl($"tss/{tssId.Value}/export") ?? $"tss/{tssId.Value}/export";

        ListExportsResponse response = await _executor.ExecuteGetAsync<ListExportsResponse>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Retrieved {Count} exports for TSS {TssId}",
            response.Data.Count, tssId.Value);

        return response;
    }

    public async Task<ListExportsResponse> ListAllExportsAsync(
        ListExportsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        string url = queryParameters?.BuildUrl("export") ?? "export";

        ListExportsResponse response = await _executor.ExecuteGetAsync<ListExportsResponse>(_httpClient, url, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Retrieved {Count} exports across all TSS",
            response.Data.Count);

        return response;
    }

    public async Task<MetadataCollection> GetExportMetadataAsync(
        TssId tssId,
        ExportId exportId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting metadata for export {ExportId} (TSS: {TssId})",
            exportId.Value, tssId.Value);

        MetadataCollection metadata = await MetadataOperations.GetAsync(
            _executor,
            _httpClient,
            $"tss/{tssId.Value}/export/{exportId.Value}/metadata",
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Retrieved metadata for export {ExportId} with {Count} entries",
            exportId.Value, metadata.Count);

        return metadata;
    }

    public async Task<MetadataCollection> UpdateExportMetadataAsync(
        TssId tssId,
        ExportId exportId,
        MetadataCollection metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata, nameof(metadata));

        _logger.LogInformation("Updating export metadata for {ExportId} (TSS: {TssId}) with {Count} entries",
            exportId.Value, tssId.Value, metadata.Count);

        MetadataCollection result = await MetadataOperations.UpdateAsync(
            _executor,
            _httpClient,
            $"tss/{tssId.Value}/export/{exportId.Value}/metadata",
            metadata,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated export metadata for {ExportId}, new count: {Count}",
            exportId.Value, result.Count);

        return result;
    }
}
