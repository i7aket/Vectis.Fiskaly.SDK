using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Management.Organizations.Models;
using Vectis.Fiskaly.SDK.Management.Organizations.Responses;

namespace Vectis.Fiskaly.SDK.Management.Organizations;

/// <summary>
/// Read-only wrapper for Management API organization endpoints (fiskaly-management-api-spec-v0.12.0).
/// </summary>
public interface IOrganizationClient
{
    /// <summary>
    /// Calls GET /organizations with optional filters/paging.
    /// </summary>
    /// <param name="queryParameters">Filter/paging options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List response defined by the Management API spec.</returns>
    Task<ListOrganizationsResponse> ListOrganizationsAsync(
        ListOrganizationsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls GET /organizations/{organization_id}.
    /// </summary>
    /// <param name="organizationId">Organization identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The OrganizationResponse from the Management API.</returns>
    Task<OrganizationResponse> GetOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default);
}
