using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Tss.Models;
using Vectis.Fiskaly.SDK.SignDE.Tss.Requests;
using Vectis.Fiskaly.SDK.SignDE.Tss.Responses;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Tss;

/// <summary>
/// Strongly typed access to SIGN DE TSS endpoints (/api/v2/tss*).
/// </summary>
public interface ITssClient
{
    /// <summary>
    /// Calls PUT /api/v2/tss/{tss_id} to create or replace a TSS (returns admin_puk once per spec).
    /// </summary>
    Task<TssResponse> CreateTssAsync(
        TssId tssId,
        MetadataCollection? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls PATCH /api/v2/tss/{tss_id} with the supplied update payload.
    /// </summary>
    Task<TssResponse> UpdateTssAsync(
        TssId tssId,
        UpdateTssRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}.
    /// </summary>
    Task<TssResponse> GetTssAsync(
        TssId tssId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss with optional paging/filter parameters.
    /// </summary>
    Task<ListTssResponse> ListTssAsync(
        ListTssQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /api/v2/tss/{tss_id}/metadata.
    /// </summary>
    Task<MetadataCollection> GetTssMetadataAsync(
        TssId tssId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls PATCH /api/v2/tss/{tss_id}/metadata to merge metadata.
    /// </summary>
    Task<MetadataCollection> UpdateTssMetadataAsync(
        TssId tssId,
        MetadataCollection metadata,
        CancellationToken cancellationToken = default);
}
