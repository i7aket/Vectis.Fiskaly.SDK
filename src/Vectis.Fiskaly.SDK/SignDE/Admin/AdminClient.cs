using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Admin.Requests;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Admin.Responses;

namespace Vectis.Fiskaly.SDK.SignDE.Admin;

public class AdminClient(
    HttpClient httpClient,
    FiskalyHttpRequestExecutor executor,
    ILogger<AdminClient> logger) : IAdminClient
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly FiskalyHttpRequestExecutor _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    private readonly ILogger<AdminClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task ChangeAdminPinAsync(
        TssId tssId,
        ChangeAdminPinRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Changing admin PIN for TSS: {TssId}", tssId.Value);

        _logger.LogDebug("Changing admin PIN for TSS {TssId} (credentials redacted)", tssId.Value);

        await _executor.ExecutePatchAsync<ChangeAdminPinRequest, object>(
            _httpClient,
            $"tss/{tssId.Value}/admin",
            request,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Admin PIN changed successfully for TSS: {TssId}", tssId.Value);
    }

    public async Task<AdminAuthenticationResponse> AuthenticateAdminAsync(
        TssId tssId,
        AdminAuthenticationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Authenticating admin for TSS: {TssId}", tssId.Value);

        await _executor.ExecutePostAsync(
            _httpClient,
            $"tss/{tssId.Value}/admin/auth",
            request,
            cancellationToken).ConfigureAwait(false);

        AdminAuthenticationResponse authResponse = new AdminAuthenticationResponse
        {
            TssId = tssId
        };

        _logger.LogInformation("Admin authenticated successfully for TSS {TssId} - server-side session created", tssId.Value);

        return authResponse;
    }

    public async Task LogoutAdminAsync(
        TssId tssId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging out admin for TSS: {TssId}", tssId.Value);

        await _executor.ExecutePostAsync(
            _httpClient,
            $"tss/{tssId}/admin/logout",
            AdminLogoutRequest.Empty,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Admin logged out successfully for TSS: {TssId}", tssId.Value);
    }
}
