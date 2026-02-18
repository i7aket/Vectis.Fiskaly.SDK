using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions;

/// <summary>
/// Unit tests for TransactionClient internal helper methods.
/// Tests focus on ResolveRevisionAsync which contains critical business logic:
/// - Fast path optimization (explicit revision)
/// - Auto-fetch with revision calculation
/// - State validation for UPDATE/CANCEL operations
/// </summary>
[Trait("Category", "Unit")]
public class TransactionClientHelperTests
{
    private readonly TssId _testTssId = TssId.New();
    private readonly TxId _testTransactionId = TxId.New();
    private readonly JsonSerializerOptions _jsonOptions;

    public TransactionClientHelperTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    #region Helper Methods

    /// <summary>
    /// Creates a TransactionClient with mocked HttpClient.
    /// </summary>
    private TransactionClient CreateTransactionClient(HttpMessageHandler httpMessageHandler)
    {
        HttpClient httpClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("https://api.fiskaly.com/api/v2/")
        };

        FiskalyHttpRequestExecutor executor = new Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor(
            _jsonOptions,
            NullLogger<Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor>.Instance
        );

        return new TransactionClient(
            httpClient,
            executor,
            NullLogger<TransactionClient>.Instance,
            _jsonOptions
        );
    }

    /// <summary>
    /// Creates a mocked HTTP response for GetTransactionAsync.
    /// </summary>
    private HttpResponseMessage CreateGetTransactionResponse(
        TxState state,
        int latestRevision)
    {
        TxResponse transactionResponse = new TxResponse
        {
            Id = _testTransactionId,
            Number = 12345,
            State = state,
            ClientId = ClientId.New(),  // Required: valid UUIDv4
            TssId = _testTssId,
            LatestRevision = latestRevision,
            Revision = latestRevision,
            TimeStart = DateTimeOffset.UtcNow,
            Env = Env.Test,
            Type = ResourceType.Transaction,
            Version = "2.1.35",
            ClientSerialNumber = ClientSerialNumber.From("CLIENT-0001"),
            TssSerialNumber = TssSerialNumber.From("fiskaly-12345"),
            Signature = new TxSignature
            {
                Value = "dummy-signature",
                Counter = 1,
                Algorithm = Algorithm.EcdsaPlainSha256,
                PublicKey = "dummy-public-key"
            },
            Log = new TxLog
            {
                Operation = TxOperation.Start,
                Timestamp = DateTimeOffset.UtcNow,
                TimestampFormat = TimestampFormat.UnixTime
            }
        };

        string json = JsonSerializer.Serialize(transactionResponse, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    #endregion

    #region Fast Path Tests

    [Fact]
    public async Task ResolveRevisionAsync_WithExplicitRevision_ReturnsDirectlyWithoutHttpRequest()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        const int explicitRevision = 42;

        // Act
        int result = await client.ResolveRevisionAsync(
            _testTssId,
            _testTransactionId,
            explicitRevision: explicitRevision,
            requireActiveState: false,
            operationName: "test",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(explicitRevision, result);

        // Verify NO HTTP request was made (fast path optimization)
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    #endregion

    #region Auto-Fetch Happy Path Tests

    [Fact]
    public async Task ResolveRevisionAsync_WithNullRevision_FetchesCurrentAndCalculatesNextRevision()
    {
        // Arrange
        const int currentRevision = 5;
        const int expectedNextRevision = 6;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/tx/{_testTransactionId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, currentRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        int result = await client.ResolveRevisionAsync(
            _testTssId,
            _testTransactionId,
            explicitRevision: null,  // Auto-fetch
            requireActiveState: false,
            operationName: "test",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedNextRevision, result);

        // Verify HTTP GET was called exactly once
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task ResolveRevisionAsync_WithZeroLatestRevision_CalculatesAsOne()
    {
        // Arrange: Transaction has LatestRevision = 0 (edge case - defensive programming)
        // SDK implementation: nextRevision = (LatestRevision ?? 0) + 1 = 1
        TxResponse transactionResponse = new TxResponse
        {
            Id = _testTransactionId,
            Number = 12345,
            State = TxState.Active,
            ClientId = ClientId.New(),  // Required: valid UUIDv4
            TssId = _testTssId,
            LatestRevision = 0,  // Edge case: SDK handles gracefully
            Revision = 0,
            TimeStart = DateTimeOffset.UtcNow,
            Env = Env.Test,
            Type = ResourceType.Transaction,
            Version = "2.1.35",
            ClientSerialNumber = ClientSerialNumber.From("CLIENT-0001"),
            TssSerialNumber = TssSerialNumber.From("fiskaly-12345"),
            Signature = new TxSignature
            {
                Value = "dummy-signature",
                Counter = 1,
                Algorithm = Algorithm.EcdsaPlainSha256,
                PublicKey = "dummy-public-key"
            },
            Log = new TxLog
            {
                Operation = TxOperation.Start,
                Timestamp = DateTimeOffset.UtcNow,
                TimestampFormat = TimestampFormat.UnixTime
            }
        };

        string json = JsonSerializer.Serialize(transactionResponse, _jsonOptions);
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act: SDK should calculate next revision as 1
        int result = await client.ResolveRevisionAsync(
            _testTssId,
            _testTransactionId,
            explicitRevision: null,
            requireActiveState: false,
            operationName: "test",
            CancellationToken.None);

        // Assert: LatestRevision=0 → next revision should be 1
        Assert.Equal(1, result);
    }

    #endregion

    #region State Validation Tests - requireActiveState=true

    [Fact]
    public async Task ResolveRevisionAsync_RequireActiveState_StateActive_Succeeds()
    {
        // Arrange
        const int currentRevision = 3;
        const int expectedNextRevision = 4;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, currentRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        int result = await client.ResolveRevisionAsync(
            _testTssId,
            _testTransactionId,
            explicitRevision: null,
            requireActiveState: true,  // ✅ Require ACTIVE state
            operationName: "update",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedNextRevision, result);
    }

    [Fact]
    public async Task ResolveRevisionAsync_RequireActiveState_StateFinished_ThrowsInvalidOperationException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, latestRevision: 5));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.ResolveRevisionAsync(
                _testTssId,
                _testTransactionId,
                explicitRevision: null,
                requireActiveState: true,  // ✅ Require ACTIVE state
                operationName: "update",
                CancellationToken.None
            )
        );

        // Assert error message contains key information
        Assert.Contains("Cannot update transaction", exception.Message);
        Assert.Contains("FINISHED", exception.Message);
        Assert.Contains("ACTIVE", exception.Message);
    }

    [Fact]
    public async Task ResolveRevisionAsync_RequireActiveState_StateCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Cancelled, latestRevision: 3));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.ResolveRevisionAsync(
                _testTssId,
                _testTransactionId,
                explicitRevision: null,
                requireActiveState: true,  // ✅ Require ACTIVE state
                operationName: "cancel",
                CancellationToken.None
            )
        );

        // Assert error message
        Assert.Contains("Cannot cancel transaction", exception.Message);
        Assert.Contains("CANCELLED", exception.Message);
    }

    #endregion

    #region State Validation Tests - requireActiveState=false

    [Fact]
    public async Task ResolveRevisionAsync_NotRequireActiveState_FinishedState_Succeeds()
    {
        // Arrange: FINISH operation can work with any state
        const int currentRevision = 7;
        const int expectedNextRevision = 8;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, currentRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act: Should NOT throw even though state is FINISHED
        int result = await client.ResolveRevisionAsync(
            _testTssId,
            _testTransactionId,
            explicitRevision: null,
            requireActiveState: false,  // ❌ State validation disabled
            operationName: "finish",
            CancellationToken.None
        );

        // Assert
        Assert.Equal(expectedNextRevision, result);
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public async Task ResolveRevisionAsync_InvalidState_ErrorMessageContainsOperationName()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, latestRevision: 2));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.ResolveRevisionAsync(
                _testTssId,
                _testTransactionId,
                explicitRevision: null,
                requireActiveState: true,
                operationName: "customOperation",  // Custom operation name
                CancellationToken.None
            )
        );

        // Assert: Error message uses the operation name
        Assert.Contains("customOperation", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
