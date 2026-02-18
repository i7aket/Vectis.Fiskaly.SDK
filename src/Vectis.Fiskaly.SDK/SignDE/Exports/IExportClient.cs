
using Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.Responses;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Exports;

/// <summary>
/// Wraps SIGN DE export endpoints (/api/v2/tss/*/export*) from the official Fiskaly SIGN DE API.
/// </summary>
public interface IExportClient
{
    /// <summary>
    /// Calls PUT /api/v2/tss/{tss_id}/export/{export_id} with a DsfinvkFullExportRequest payload.
    /// </summary>
    Task<ExportJob> TriggerFullExportAsync(
        TssId tssId,
        ExportId exportId,
        DsfinvkFullExportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls PUT /api/v2/tss/{tss_id}/export/{export_id} for a DsfinvkClientExportRequest.
    /// </summary>
    Task<ExportJob> TriggerClientExportAsync(
        TssId tssId,
        ExportId exportId,
        DsfinvkClientExportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls PUT /api/v2/tss/{tss_id}/export/{export_id} for a DsfinvkLogExportRequest.
    /// </summary>
    Task<ExportJob> TriggerLogExportAsync(
        TssId tssId,
        ExportId exportId,
        DsfinvkLogExportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}/export/{export_id}.
    /// </summary>
    Task<ExportJob> GetExportAsync(
        TssId tssId,
        ExportId exportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}/export/{export_id}/file to download an archive and optionally parse it with a custom DSFinV-K strategy.
    /// </summary>
    Task<DsfinvkArchive> DownloadExportAsync(
        TssId tssId,
        ExportId exportId,
        IDsfinvkVersionStrategy? strategy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls DELETE /api/v2/tss/{tss_id}/export/{export_id} to cancel an export job.
    /// </summary>
    Task<ExportJob> CancelExportAsync(
        TssId tssId,
        ExportId exportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}/export with optional filters.
    /// </summary>
    Task<ListExportsResponse> ListExportsAsync(
        TssId tssId,
        ListExportsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/export (organization scope) and returns the paged result.
    /// </summary>
    Task<ListExportsResponse> ListAllExportsAsync(
        ListExportsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}/export/{export_id}/metadata.
    /// </summary>
    Task<MetadataCollection> GetExportMetadataAsync(
        TssId tssId,
        ExportId exportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls PATCH /api/v2/tss/{tss_id}/export/{export_id}/metadata.
    /// </summary>
    Task<MetadataCollection> UpdateExportMetadataAsync(
        TssId tssId,
        ExportId exportId,
        MetadataCollection metadata,
        CancellationToken cancellationToken = default);
}
