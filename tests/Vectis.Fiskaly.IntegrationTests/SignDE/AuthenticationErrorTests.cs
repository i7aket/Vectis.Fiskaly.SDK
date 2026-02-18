using System.Net;
using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Admin.Requests;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;

namespace Vectis.Fiskaly.IntegrationTests.SignDE;

/// <summary>
/// Integration tests for authentication error scenarios.
/// </summary>
/// <remarks>
/// <para><strong>Scope</strong>: Admin authentication failures</para>
///
/// <para><strong>Error Code Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>E_ADMIN_LOGIN_FAILED - Wrong admin credentials (PIN or PUK)</description></item>
/// </list>
///
/// <para><strong>HTTP Status</strong>: 401 Unauthorized</para>
/// <para><strong>Category</strong>: Permanent error (wrong credentials won't succeed on retry)</para>
///
/// <para><strong>Security Note</strong>:</para>
/// <para>E_ADMIN_LOGIN_FAILED is different from E_UNAUTHORIZED (expired JWT token).
/// Wrong credentials are a permanent error - retrying won't help unless credentials are corrected.
/// The SDK does NOT auto-retry authentication failures.</para>
///
/// <para><strong>Test Strategy</strong>:</para>
/// <para>Use intentionally wrong credentials (wrong PIN, wrong PUK) to trigger authentication failures.
/// Tests verify error codes, HTTP status, and error category.</para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "ErrorScenarios")]
[Trait("Priority", "Medium")]
public class AuthenticationErrorTests : FiskalyIntegrationTestBase
{
    public AuthenticationErrorTests(FiskalyBaseTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AuthenticateAdmin_WithWrongPin_ThrowsE_ADMIN_LOGIN_FAILED()
    {
        // Arrange
        AdminPin wrongPin = AdminPin.From("9999999999"); // Wrong PIN (10 digits but incorrect)
        AdminAuthenticationRequest request = new AdminAuthenticationRequest
        {
            AdminPin = wrongPin
        };

        Console.WriteLine($"Attempting admin authentication with wrong PIN...");
        Console.WriteLine($"   TSS ID: {TssId}");
        Console.WriteLine($"   Wrong PIN: {wrongPin.Value}");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.AdminClient.AuthenticateAdminAsync(TssId, request)
        );

        // Debug output
        Console.WriteLine($"📋 Actual API Error:");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Status Code: {exception.StatusCode} ({(int)exception.StatusCode})");
        Console.WriteLine($"   Category: {exception.Category}");
        Console.WriteLine($"   Message: {exception.Message}");

        // Assert exception details
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_ADMIN_LOGIN_FAILED,
            "wrong admin PIN should return E_ADMIN_LOGIN_FAILED");
        exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("wrong admin credentials are permanent errors");
        exception.CorrelationId.Should().NotBeNullOrEmpty("CorrelationId should be provided for tracing");

        Console.WriteLine($"✅ Wrong admin PIN error verified");
    }

    [Fact]
    public async Task ChangeAdminPin_WithWrongPuk_ThrowsE_ADMIN_LOGIN_FAILED()
    {
        // Arrange
        AdminPuk wrongPuk = AdminPuk.From("0000000000"); // Wrong PUK
        AdminPin newPin = AdminPin.From("1111111111");   // New PIN to set

        Console.WriteLine($"Attempting to change admin PIN with wrong PUK...");
        Console.WriteLine($"   TSS ID: {TssId}");
        Console.WriteLine($"   Wrong PUK: {wrongPuk.Value}");

        ChangeAdminPinRequest request = new ChangeAdminPinRequest
        {
            AdminPuk = wrongPuk,
            NewAdminPin = newPin
        };

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.AdminClient.ChangeAdminPinAsync(TssId, request)
        );

        // Debug output
        Console.WriteLine($"📋 Actual API Error:");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Status Code: {exception.StatusCode} ({(int)exception.StatusCode})");
        Console.WriteLine($"   Category: {exception.Category}");
        Console.WriteLine($"   Message: {exception.Message}");

        // Assert exception details - wrong PUK causes authentication failure
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_ADMIN_LOGIN_FAILED,
            "wrong admin PUK causes authentication failure before PIN change");
        exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("wrong admin PUK is a permanent error");
        exception.CorrelationId.Should().NotBeNullOrEmpty("CorrelationId should be provided for tracing");

        Console.WriteLine($"✅ Wrong admin PUK error verified");
    }
}
