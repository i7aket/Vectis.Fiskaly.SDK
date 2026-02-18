using System.Net;
using AwesomeAssertions;
using Vectis.Fiskaly.IntegrationTests.Base;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.IntegrationTests.SignDE;

/// <summary>
/// Integration tests for "Resource Not Found" error scenarios (404 errors).
/// </summary>
/// <remarks>
/// <para><strong>Scope</strong>: API error responses for non-existent resources</para>
///
/// <para><strong>Error Codes Tested</strong>:</para>
/// <list type="bullet">
///   <item><description>E_TSS_NOT_FOUND - TSS doesn't exist</description></item>
///   <item><description>E_CLIENT_NOT_FOUND - Client doesn't exist</description></item>
///   <item><description>E_TX_NOT_FOUND - Transaction doesn't exist</description></item>
/// </list>
///
/// <para><strong>HTTP Status</strong>: 404 Not Found (all tests)</para>
/// <para><strong>Category</strong>: Permanent errors (not retryable)</para>
///
/// <para><strong>Test Strategy</strong>:</para>
/// <para>Use randomly generated IDs (guaranteed not to exist) to trigger 404 errors.
/// No setup or cleanup required since resources are never created.</para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Feature", "ErrorScenarios")]
[Trait("Priority", "High")]
public class ResourceNotFoundErrorTests : FiskalyIntegrationTestBase
{
    public ResourceNotFoundErrorTests(FiskalyBaseTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetTss_WithNonExistentId_ThrowsE_TSS_NOT_FOUND()
    {
        // Arrange
        TssId nonExistentTssId = TssId.New();
        Console.WriteLine($"Testing with non-existent TSS ID: {nonExistentTssId}");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.TssClient.GetTssAsync(nonExistentTssId)
        );

        // Assert exception details
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_TSS_NOT_FOUND);
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("E_TSS_NOT_FOUND is a permanent error");
        exception.CorrelationId.Should().NotBeNullOrEmpty("CorrelationId should be provided for tracing");

        Console.WriteLine($"✅ E_TSS_NOT_FOUND verified");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Status Code: {exception.StatusCode}");
        Console.WriteLine($"   Category: {exception.Category}");
        Console.WriteLine($"   Correlation ID: {exception.CorrelationId}");
    }

    [Fact]
    public async Task GetClient_WithNonExistentId_ThrowsE_CLIENT_NOT_FOUND()
    {
        // Arrange
        ClientId nonExistentClientId = ClientId.New();
        Console.WriteLine($"Testing with TSS: {TssId}, non-existent Client ID: {nonExistentClientId}");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.ClientManagementClient.GetClientAsync(TssId, nonExistentClientId)
        );

        // Assert exception details
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_CLIENT_NOT_FOUND);
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("E_CLIENT_NOT_FOUND is a permanent error");
        exception.CorrelationId.Should().NotBeNullOrEmpty("CorrelationId should be provided for tracing");

        Console.WriteLine($"✅ E_CLIENT_NOT_FOUND verified");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Status Code: {exception.StatusCode}");
        Console.WriteLine($"   Correlation ID: {exception.CorrelationId}");
    }

    [Fact]
    public async Task GetTransaction_WithNonExistentId_ThrowsE_TX_NOT_FOUND()
    {
        // Arrange
        TxId nonExistentTxId = TxId.New();
        Console.WriteLine($"Testing with TSS: {TssId}, non-existent Transaction ID: {nonExistentTxId}");

        // Act & Assert
        FiskalyApiException exception = await Assert.ThrowsAsync<FiskalyApiException>(
            async () => await Fixture.TransactionClient.GetTransactionAsync(TssId, nonExistentTxId)
        );

        // Assert exception details
        exception.ErrorCode.Should().Be(FiskalyErrorCode.E_TX_NOT_FOUND);
        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Category.Should().Be(FiskalyErrorCategory.Permanent);
        exception.IsRetryable.Should().BeFalse("E_TX_NOT_FOUND is a permanent error");
        exception.CorrelationId.Should().NotBeNullOrEmpty("CorrelationId should be provided for tracing");

        Console.WriteLine($"✅ E_TX_NOT_FOUND verified");
        Console.WriteLine($"   Error Code: {exception.ErrorCode}");
        Console.WriteLine($"   Status Code: {exception.StatusCode}");
        Console.WriteLine($"   Correlation ID: {exception.CorrelationId}");
    }
}
