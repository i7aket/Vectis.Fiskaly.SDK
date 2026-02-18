using System.Text.Json;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.Management.Organizations.Models;
using Vectis.Fiskaly.SDK.Management.Organizations.Responses;
using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.Management.Organizations;

public partial class OrganizationClient(
    HttpClient httpClient,
    FiskalyHttpRequestExecutor executor,
    ILogger<OrganizationClient> logger,
    JsonSerializerOptions serializerOptions)
    : IOrganizationClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly FiskalyHttpRequestExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly ILogger<OrganizationClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly JsonSerializerOptions _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));

    public async Task<ListOrganizationsResponse> ListOrganizationsAsync(
        ListOrganizationsQueryParameters? queryParameters = null,
        CancellationToken cancellationToken = default)
    {
        string url = queryParameters?.BuildUrl("organizations") ?? "organizations";

        _logger.LogDebug("Listing organizations with URL: {Url}", url);

        ListOrganizationsResponse response = await _executor.ExecuteGetAsync<ListOrganizationsResponse>(
            _httpClient,
            url,
            cancellationToken).ConfigureAwait(false);

        int dataCount = response.Data?.Count ?? 0;
        int apiCount = response.Count ?? dataCount;
        _logger.LogInformation("Retrieved {Count} organizations (API count: {ApiCount})",
            dataCount, apiCount);

        return response;
    }

    public async Task<OrganizationResponse> GetOrganizationAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving organization: {OrganizationId}", organizationId.Value);

        OrganizationResponse organization = await _executor.ExecuteGetAsync<OrganizationResponse>(
            _httpClient,
            $"organizations/{organizationId}",
            cancellationToken).ConfigureAwait(false);

        string retrievedId = (organization.Id ?? organizationId).ToString();
        string retrievedName = organization.Name ?? "UNKNOWN";
        _logger.LogInformation("Retrieved organization: {OrganizationId}, Name: {Name}",
            retrievedId, retrievedName);

        return organization;
    }
}
