using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Responses;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions;

/// <summary>
/// Unit tests for TransactionClient lifecycle operations and state validation.
/// Phase 1: Constructor, Start, Finish, Update, Cancel, ExecuteTransactionHttpAsync, Cancellation Token
///
/// CRITICAL FISCAL COMPLIANCE TESTS:
/// - State machine validation (ACTIVE → FINISHED/CANCELLED transitions)
/// - Revision management (fast path vs auto-fetch)
/// - Forbidden state transitions (Update/Cancel on FINISHED/CANCELLED)
/// </summary>
[Trait("Category", "Unit")]
public class TransactionClientTests
{
    private readonly TssId _testTssId = TssId.New();
    private readonly TxId _testTransactionId = TxId.New();
    private readonly ClientId _testClientId = ClientId.New();
    private readonly JsonSerializerOptions _jsonOptions;

    public TransactionClientTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters =
            {
                new Vectis.Fiskaly.SDK.SignDE.Common.MetadataCollectionJsonConverter()
            }
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
    /// Creates a mocked HTTP GET response for GetTransactionAsync.
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
            ClientId = _testClientId,
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

    /// <summary>
    /// Creates a mocked HTTP PUT response for transaction operations.
    /// </summary>
    private HttpResponseMessage CreatePutTransactionResponse(
        TxState state,
        int revision)
    {
        TxResponse transactionResponse = new TxResponse
        {
            Id = _testTransactionId,
            Number = 12345,
            State = state,
            ClientId = _testClientId,
            TssId = _testTssId,
            LatestRevision = revision,
            Revision = revision,
            TimeStart = DateTimeOffset.UtcNow,
            TimeEnd = state == TxState.Finished ? DateTimeOffset.UtcNow : null,
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
                Operation = state == TxState.Finished ? TxOperation.Finish : TxOperation.Update,
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

    /// <summary>
    /// Creates a valid StartTransactionRequest for testing.
    /// </summary>
    private StartTransactionRequest CreateValidStartRequest()
    {
        return new StartTransactionRequest
        {
            ClientId = _testClientId
        };
    }

    private FinishTransactionRequest CreateValidFinishRequest()
    {
        return FinishTransactionRequest.CreateReceipt(
            _testClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(100.00m, CurrencyCode.EUR) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(100.00m, CurrencyCode.EUR) }
                }
            });
    }

    /// <summary>
    /// Creates a valid UpdateTransactionRequest for testing.
    /// </summary>
    private UpdateTransactionRequest CreateValidUpdateRequest()
    {
        return UpdateTransactionRequest.CreateReceipt(
            _testClientId,
            new Receipt
            {
                ReceiptType = ReceiptType.Receipt,
                AmountsPerVatRate = new List<VatRateAmount>
                {
                    new() { VatRate = VatRate.Normal, Amount = MoneyAmount.Create(50.00m, CurrencyCode.EUR) }
                },
                AmountsPerPaymentType = new List<PaymentTypeAmount>
                {
                    new() { PaymentType = PaymentType.Cash, Amount = MoneyAmount.Create(50.00m, CurrencyCode.EUR) }
                }
            });
    }

    /// <summary>
    /// Creates a valid CancelTransactionRequest for testing.
    /// </summary>
    private CancelTransactionRequest CreateValidCancelRequest()
    {
        return new CancelTransactionRequest
        {
            ClientId = _testClientId
        };
    }

    #endregion

    #region Constructor Tests (5 tests)

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        FiskalyHttpRequestExecutor executor = new Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor(
            _jsonOptions,
            NullLogger<Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor>.Instance);
        NullLogger<TransactionClient> logger = NullLogger<TransactionClient>.Instance;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new TransactionClient(null!, executor, logger, _jsonOptions));

        Assert.Equal("httpClient", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        NullLogger<TransactionClient> logger = NullLogger<TransactionClient>.Instance;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new TransactionClient(httpClient, null!, logger, _jsonOptions));

        Assert.Equal("executor", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        FiskalyHttpRequestExecutor executor = new Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor(
            _jsonOptions,
            NullLogger<Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor>.Instance);

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new TransactionClient(httpClient, executor, null!, _jsonOptions));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSerializerOptions_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        FiskalyHttpRequestExecutor executor = new Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor(
            _jsonOptions,
            NullLogger<Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor>.Instance);
        NullLogger<TransactionClient> logger = NullLogger<TransactionClient>.Instance;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new TransactionClient(httpClient, executor, logger, null!));

        Assert.Equal("serializerOptions", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        FiskalyHttpRequestExecutor executor = new Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor(
            _jsonOptions,
            NullLogger<Vectis.Fiskaly.SDK.Http.FiskalyHttpRequestExecutor>.Instance);
        NullLogger<TransactionClient> logger = NullLogger<TransactionClient>.Instance;

        // Act
        TransactionClient client = new TransactionClient(httpClient, executor, logger, _jsonOptions);

        // Assert
        Assert.NotNull(client);
    }

    #endregion

    #region StartTransactionAsync Tests (6 tests)

    [Fact]
    public async Task StartTransactionAsync_WithValidRequest_ReturnsTransactionResponse()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/tx/{_testTransactionId}") &&
                    req.RequestUri.PathAndQuery.Contains("tx_revision=1")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Active, 1));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act
        TxResponse result = await client.StartTransactionAsync(_testTssId, _testTransactionId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testTransactionId, result.Id);
        Assert.Equal(TxState.Active, result.State);
        Assert.Equal(1, result.Revision);

        // Verify PUT was called exactly once
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task StartTransactionAsync_WithDefaultTssId_ThrowsArgumentException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.StartTransactionAsync(default, _testTransactionId, request));
    }

    [Fact]
    public async Task StartTransactionAsync_WithDefaultTransactionId_ThrowsArgumentException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.StartTransactionAsync(_testTssId, default, request));
    }

    [Fact]
    public async Task StartTransactionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.StartTransactionAsync(_testTssId, _testTransactionId, null!));
    }

    [Fact]
    public async Task StartTransactionAsync_AlwaysUsesRevisionOne()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains("tx_revision=1")), // Must be revision 1
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Active, 1));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act
        TxResponse result = await client.StartTransactionAsync(_testTssId, _testTransactionId, request);

        // Assert
        Assert.Equal(1, result.Revision);

        // Verify NO GET request (START doesn't use ResolveRevisionAsync)
        mockHandler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task StartTransactionAsync_PropagatesCancellationToken()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.StartTransactionAsync(_testTssId, _testTransactionId, request, cts.Token));
    }

    #endregion

    #region FinishTransactionAsync Tests (8 tests)

    [Fact]
    public async Task FinishTransactionAsync_WithExplicitRevision_UsesRevisionDirectly()
    {
        // Arrange: Fast path - explicit revision provided
        const int explicitRevision = 5;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains($"tx_revision={explicitRevision}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Finished, explicitRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        FinishTransactionRequest request = CreateValidFinishRequest();

        // Act
        TxResponse result = await client.FinishTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: explicitRevision);

        // Assert
        Assert.Equal(TxState.Finished, result.State);
        Assert.Equal(explicitRevision, result.Revision);

        // Verify NO GET request (fast path)
        mockHandler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());

        // Verify exactly ONE PUT request
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task FinishTransactionAsync_WithNullRevision_AutoFetchesAndCalculatesRevision()
    {
        // Arrange: Auto-fetch path
        const int currentRevision = 3;
        const int expectedNextRevision = 4;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // Setup GET for auto-fetch
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/tx/{_testTransactionId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, currentRevision));

        // Setup PUT for finish operation
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains($"tx_revision={expectedNextRevision}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Finished, expectedNextRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        FinishTransactionRequest request = CreateValidFinishRequest();

        // Act
        TxResponse result = await client.FinishTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: null); // Auto-fetch

        // Assert
        Assert.Equal(TxState.Finished, result.State);
        Assert.Equal(expectedNextRevision, result.Revision);

        // Verify GET was called once (auto-fetch)
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());

        // Verify PUT was called once
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task FinishTransactionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.FinishTransactionAsync(_testTssId, _testTransactionId, null!));
    }

    [Fact]
    public async Task FinishTransactionAsync_DoesNotEnforceStateValidation()
    {
        // Arrange: FINISH operation has requireActiveState=false
        // This test verifies FINISH can be attempted on any state (though API may reject)
        const int currentRevision = 5;
        const int expectedNextRevision = 6;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // GET returns FINISHED state (unusual but allowed for FINISH operation)
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, currentRevision));

        // PUT should still be attempted (no state validation)
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Finished, expectedNextRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        FinishTransactionRequest request = CreateValidFinishRequest();

        // Act: Should NOT throw despite FINISHED state
        TxResponse result = await client.FinishTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: null);

        // Assert: Operation proceeds without state validation
        Assert.Equal(TxState.Finished, result.State);
    }

    [Fact]
    public async Task FinishTransactionAsync_WithActiveTransaction_Succeeds()
    {
        // Arrange: Normal happy path - finishing ACTIVE transaction
        const int currentRevision = 2;
        const int expectedNextRevision = 3;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, currentRevision));

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Finished, expectedNextRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        FinishTransactionRequest request = CreateValidFinishRequest();

        // Act
        TxResponse result = await client.FinishTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: null);

        // Assert
        Assert.Equal(TxState.Finished, result.State);
        Assert.Equal(expectedNextRevision, result.Revision);
    }

    [Fact]
    public async Task FinishTransactionAsync_PropagatesCancellationToken()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        FinishTransactionRequest request = CreateValidFinishRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.FinishTransactionAsync(_testTssId, _testTransactionId, request, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task FinishTransactionAsync_BuildsCorrectUrl()
    {
        // Arrange
        const int explicitRevision = 7;
        string expectedUrl = $"tss/{_testTssId.Value}/tx/{_testTransactionId}?tx_revision={explicitRevision}";

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains(expectedUrl)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Finished, explicitRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        FinishTransactionRequest request = CreateValidFinishRequest();

        // Act
        await client.FinishTransactionAsync(_testTssId, _testTransactionId, request, txRevision: explicitRevision);

        // Assert: Verified by mock setup
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains(expectedUrl)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task FinishTransactionAsync_WithDefaultTssId_ThrowsArgumentException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        FinishTransactionRequest request = CreateValidFinishRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.FinishTransactionAsync(default, _testTransactionId, request));
    }

    #endregion

    #region UpdateTransactionAsync Tests (10 tests) - CRITICAL STATE VALIDATION

    [Fact]
    public async Task UpdateTransactionAsync_OnActiveTransaction_Succeeds()
    {
        // Arrange: Happy path - updating ACTIVE transaction
        const int currentRevision = 3;
        const int expectedNextRevision = 4;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, currentRevision));

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Active, expectedNextRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act
        TxResponse result = await client.UpdateTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: null);

        // Assert
        Assert.Equal(TxState.Active, result.State);
        Assert.Equal(expectedNextRevision, result.Revision);
    }

    [Fact]
    public async Task UpdateTransactionAsync_OnFinishedTransaction_ThrowsInvalidOperationException()
    {
        // Arrange: CRITICAL - Cannot update FINISHED transaction (fiscal compliance)
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // GET returns FINISHED state
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, 5));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.UpdateTransactionAsync(_testTssId, _testTransactionId, request, txRevision: null));

        // Verify error message contains context
        Assert.Contains("update", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FINISHED", exception.Message);
        Assert.Contains("ACTIVE", exception.Message);

        // Verify NO PUT request was made (state validation prevented it)
        mockHandler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTransactionAsync_OnCancelledTransaction_ThrowsInvalidOperationException()
    {
        // Arrange: CRITICAL - Cannot update CANCELLED transaction (fiscal compliance)
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // GET returns CANCELLED state
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Cancelled, 3));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.UpdateTransactionAsync(_testTssId, _testTransactionId, request, txRevision: null));

        // Verify error message contains context
        Assert.Contains("update", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CANCELLED", exception.Message);

        // Verify NO PUT request was made
        mockHandler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTransactionAsync_ErrorMessageContainsOperationName()
    {
        // Arrange: Verify error messages are helpful for debugging
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, 2));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.UpdateTransactionAsync(_testTssId, _testTransactionId, request, txRevision: null));

        // Verify error message identifies the operation that failed
        Assert.Contains("update", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateTransactionAsync_ErrorMessageContainsCurrentAndExpectedState()
    {
        // Arrange: Verify error messages show BOTH current state and expected state
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, 2));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.UpdateTransactionAsync(_testTssId, _testTransactionId, request, txRevision: null));

        // Verify error message shows current state (FINISHED)
        Assert.Contains("FINISHED", exception.Message);

        // Verify error message shows expected state (ACTIVE)
        Assert.Contains("ACTIVE", exception.Message);
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithExplicitRevision_SkipsStateValidation()
    {
        // Arrange: Fast path - explicit revision bypasses GET (and thus state validation)
        const int explicitRevision = 10;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // Only PUT is called (no GET for fast path)
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains($"tx_revision={explicitRevision}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Active, explicitRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act
        TxResponse result = await client.UpdateTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: explicitRevision);

        // Assert
        Assert.Equal(explicitRevision, result.Revision);

        // Verify NO GET request (fast path)
        mockHandler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithNullRevision_AutoFetches()
    {
        // Arrange: Auto-fetch path with state validation
        const int currentRevision = 2;
        const int expectedNextRevision = 3;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, currentRevision));

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Active, expectedNextRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act
        TxResponse result = await client.UpdateTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: null);

        // Assert
        Assert.Equal(expectedNextRevision, result.Revision);

        // Verify GET was called (auto-fetch)
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.UpdateTransactionAsync(_testTssId, _testTransactionId, null!));
    }

    [Fact]
    public async Task UpdateTransactionAsync_PropagatesCancellationToken()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.UpdateTransactionAsync(_testTssId, _testTransactionId, request, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithDefaultTssId_ThrowsArgumentException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.UpdateTransactionAsync(default, _testTransactionId, request));
    }

    #endregion

    #region CancelTransactionAsync Tests (10 tests) - CRITICAL STATE VALIDATION

    [Fact]
    public async Task CancelTransactionAsync_OnActiveTransaction_Succeeds()
    {
        // Arrange: Happy path - cancelling ACTIVE transaction
        const int currentRevision = 2;
        const int expectedNextRevision = 3;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, currentRevision));

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Cancelled, expectedNextRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act
        TxResponse result = await client.CancelTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: null);

        // Assert
        Assert.Equal(TxState.Cancelled, result.State);
        Assert.Equal(expectedNextRevision, result.Revision);
    }

    [Fact]
    public async Task CancelTransactionAsync_OnFinishedTransaction_ThrowsInvalidOperationException()
    {
        // Arrange: CRITICAL - Cannot cancel FINISHED transaction (fiscal compliance)
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // GET returns FINISHED state
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, 5));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.CancelTransactionAsync(_testTssId, _testTransactionId, request, txRevision: null));

        // Verify error message contains context
        Assert.Contains("cancel", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FINISHED", exception.Message);
        Assert.Contains("ACTIVE", exception.Message);

        // Verify NO PUT request was made (state validation prevented it)
        mockHandler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CancelTransactionAsync_OnCancelledTransaction_ThrowsInvalidOperationException()
    {
        // Arrange: CRITICAL - Cannot cancel already CANCELLED transaction
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // GET returns CANCELLED state
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Cancelled, 3));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.CancelTransactionAsync(_testTssId, _testTransactionId, request, txRevision: null));

        // Verify error message contains context
        Assert.Contains("cancel", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CANCELLED", exception.Message);

        // Verify NO PUT request was made
        mockHandler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CancelTransactionAsync_ErrorMessageContainsOperationName()
    {
        // Arrange: Verify error messages identify the operation
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, 2));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.CancelTransactionAsync(_testTssId, _testTransactionId, request, txRevision: null));

        // Verify error message identifies the operation that failed
        Assert.Contains("cancel", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelTransactionAsync_ErrorMessageContainsCurrentAndExpectedState()
    {
        // Arrange: Verify error messages show BOTH current state and expected state
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Cancelled, 2));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.CancelTransactionAsync(_testTssId, _testTransactionId, request, txRevision: null));

        // Verify error message shows current state (CANCELLED)
        Assert.Contains("CANCELLED", exception.Message);

        // Verify error message shows expected state (ACTIVE)
        Assert.Contains("ACTIVE", exception.Message);
    }

    [Fact]
    public async Task CancelTransactionAsync_WithExplicitRevision_SkipsStateValidation()
    {
        // Arrange: Fast path - explicit revision bypasses GET (and thus state validation)
        const int explicitRevision = 8;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // Only PUT is called (no GET for fast path)
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains($"tx_revision={explicitRevision}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Cancelled, explicitRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act
        TxResponse result = await client.CancelTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: explicitRevision);

        // Assert
        Assert.Equal(explicitRevision, result.Revision);

        // Verify NO GET request (fast path)
        mockHandler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CancelTransactionAsync_WithNullRevision_AutoFetches()
    {
        // Arrange: Auto-fetch path with state validation
        const int currentRevision = 1;
        const int expectedNextRevision = 2;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, currentRevision));

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Cancelled, expectedNextRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act
        TxResponse result = await client.CancelTransactionAsync(
            _testTssId, _testTransactionId, request, txRevision: null);

        // Assert
        Assert.Equal(expectedNextRevision, result.Revision);

        // Verify GET was called (auto-fetch)
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CancelTransactionAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.CancelTransactionAsync(_testTssId, _testTransactionId, null!));
    }

    [Fact]
    public async Task CancelTransactionAsync_PropagatesCancellationToken()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.CancelTransactionAsync(_testTssId, _testTransactionId, request, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task CancelTransactionAsync_WithDefaultTransactionId_ThrowsArgumentException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.CancelTransactionAsync(_testTssId, default, request));
    }

    #endregion

    #region ExecuteTransactionHttpAsync Tests (4 tests)

    [Fact]
    public async Task ExecuteTransactionHttpAsync_BuildsCorrectUrl()
    {
        // Arrange
        const int revision = 5;
        string expectedUrl = $"tss/{_testTssId.Value}/tx/{_testTransactionId}?tx_revision={revision}";

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains(expectedUrl)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Active, revision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act
        TxResponse result = await client.ExecuteTransactionHttpAsync(
            _testTssId, _testTransactionId, request, revision, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains(expectedUrl)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteTransactionHttpAsync_SerializesRequestCorrectly()
    {
        // Arrange
        const int revision = 3;
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        string? capturedJson = null;
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Active, revision))
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedJson = req.Content?.ReadAsStringAsync(ct).Result;
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act
        await client.ExecuteTransactionHttpAsync(
            _testTssId, _testTransactionId, request, revision, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedJson);
        Assert.Contains("client_id", capturedJson);
        Assert.Contains(_testClientId.Value.ToString(), capturedJson);
    }

    [Fact]
    public async Task ExecuteTransactionHttpAsync_DeserializesResponseCorrectly()
    {
        // Arrange
        const int revision = 7;
        const long expectedNumber = 12345;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreatePutTransactionResponse(TxState.Active, revision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act
        TxResponse result = await client.ExecuteTransactionHttpAsync(
            _testTssId, _testTransactionId, request, revision, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testTransactionId, result.Id);
        Assert.Equal(expectedNumber, result.Number);
        Assert.Equal(revision, result.Revision);
    }

    [Fact]
    public async Task ExecuteTransactionHttpAsync_PropagatesCancellationToken()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.ExecuteTransactionHttpAsync(
                _testTssId, _testTransactionId, request, 1, cts.Token));
    }

    #endregion

    #region GetTransaction Tests (4 tests)

    [Fact]
    public async Task GetTransactionAsync_WithRevision_ReturnsTransactionAtSpecificRevision()
    {
        // Arrange
        const int requestedRevision = 5;
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/tx/{_testTransactionId}") &&
                    req.RequestUri.PathAndQuery.Contains($"tx_revision={requestedRevision}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, requestedRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        TxResponse result = await client.GetTransactionAsync(
            _testTssId, _testTransactionId, txRevision: requestedRevision, CancellationToken.None);

        // Assert
        Assert.Equal(_testTransactionId, result.Id);
        Assert.Equal(TxState.Active, result.State);
        Assert.Equal(requestedRevision, result.Revision);

        // Verify URL contains revision parameter
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.PathAndQuery.Contains($"tx_revision={requestedRevision}")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTransactionAsync_WithoutRevision_ReturnsLatestTransaction()
    {
        // Arrange
        const int latestRevision = 7;
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/tx/{_testTransactionId}") &&
                    !req.RequestUri.PathAndQuery.Contains("tx_revision")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Finished, latestRevision));

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        TxResponse result = await client.GetTransactionAsync(
            _testTssId, _testTransactionId, txRevision: null, CancellationToken.None);

        // Assert
        Assert.Equal(_testTransactionId, result.Id);
        Assert.Equal(TxState.Finished, result.State);
        Assert.Equal(latestRevision, result.Revision);

        // Verify URL does NOT contain revision parameter
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                !req.RequestUri!.PathAndQuery.Contains("tx_revision")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTransactionAsync_WithDefaultTssId_ThrowsArgumentException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.GetTransactionAsync(default, _testTransactionId, cancellationToken: CancellationToken.None));
    }

    #endregion

    #region Transaction Metadata Tests (8 tests)

    [Fact]
    public async Task GetTransactionMetadataAsync_Success_ReturnsMetadata()
    {
        // Arrange
        MetadataCollection expectedMetadata = MetadataCollection.From(new Dictionary<string, string>
        {
            ["order_id"] = "ORDER-12345",
            ["customer_id"] = "CUST-67890",
            ["table_number"] = "42"
        });

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/tx/{_testTransactionId}/metadata")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedMetadata.ToDictionary(), _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.GetTransactionMetadataAsync(_testTssId, _testTransactionId, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("ORDER-12345", result["order_id"]);
        Assert.Equal("CUST-67890", result["customer_id"]);
        Assert.Equal("42", result["table_number"]);
    }

    [Fact]
    public async Task UpdateTransactionMetadataAsync_Success_ReturnsUpdatedMetadata()
    {
        // Arrange
        MetadataCollection requestMetadata = MetadataCollection.From(new Dictionary<string, string>
        {
            ["order_id"] = "ORDER-99999",
            ["notes"] = "Updated order"
        });

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/tx/{_testTransactionId}/metadata")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestMetadata.ToDictionary(), _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.UpdateTransactionMetadataAsync(
            _testTssId, _testTransactionId, requestMetadata, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("ORDER-99999", result["order_id"]);
        Assert.Equal("Updated order", result["notes"]);
    }

    [Fact]
    public async Task UpdateTransactionMetadataAsync_CreatesNewMetadata_ReturnsMetadata()
    {
        // Arrange: Empty metadata collection being created
        MetadataCollection newMetadata = MetadataCollection.From(new Dictionary<string, string>
        {
            ["first_key"] = "first_value"
        });

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.PathAndQuery.Contains("/metadata")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(newMetadata.ToDictionary(), _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.UpdateTransactionMetadataAsync(
            _testTssId, _testTransactionId, newMetadata, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Equal("first_value", result["first_key"]);
    }

    [Fact]
    public async Task UpdateTransactionMetadataAsync_NullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.UpdateTransactionMetadataAsync(_testTssId, _testTransactionId, null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetTransactionMetadataAsync_WithCancellation_PropagatesCancellationToken()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.GetTransactionMetadataAsync(_testTssId, _testTransactionId, cts.Token));
    }

    [Fact]
    public async Task GetTransactionMetadataAsync_EmptyMetadata_ReturnsEmptyCollection()
    {
        // Arrange: API returns empty metadata object
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.GetTransactionMetadataAsync(_testTssId, _testTransactionId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public async Task GetTransactionMetadataAsync_BuildsCorrectUrl()
    {
        // Arrange
        string expectedPath = $"tss/{_testTssId.Value}/tx/{_testTransactionId}/metadata";
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains(expectedPath)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        await client.GetTransactionMetadataAsync(_testTssId, _testTransactionId, CancellationToken.None);

        // Assert: Verified by mock setup
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains(expectedPath)),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region ListTransactions Tests (9 tests)

    [Fact]
    public async Task ListTransactionsAsync_WithDefaultParameters_ReturnsAllTransactions()
    {
        // Arrange
        TxListResponse expectedResponse = new TxListResponse
        {
            Data = new List<TxResponse>
            {
                new()
                {
                    Id = _testTransactionId,
                    TssId = _testTssId,
                    State = TxState.Finished,
                    Number = 1,
                    ClientId = ClientId.New(),
                    Type = ResourceType.Transaction,
                    Env = Env.Test,
                    Version = "2.1.35",
                    Revision = 1,
                    LatestRevision = 1,
                    TimeStart = DateTimeOffset.UtcNow,
                    ClientSerialNumber = ClientSerialNumber.From("CLIENT-001"),
                    TssSerialNumber = TssSerialNumber.From("fiskaly-12345"),
                    Log = new TxLog
                    {
                        Operation = TxOperation.Finish,
                        Timestamp = DateTimeOffset.UtcNow,
                        TimestampFormat = TimestampFormat.UnixTime
                    }
                },
                new()
                {
                    Id = TxId.New(),
                    TssId = _testTssId,
                    State = TxState.Finished,
                    Number = 2,
                    ClientId = ClientId.New(),
                    Type = ResourceType.Transaction,
                    Env = Env.Test,
                    Version = "2.1.35",
                    Revision = 1,
                    LatestRevision = 1,
                    TimeStart = DateTimeOffset.UtcNow,
                    ClientSerialNumber = ClientSerialNumber.From("CLIENT-002"),
                    TssSerialNumber = TssSerialNumber.From("fiskaly-67890"),
                    Log = new TxLog
                    {
                        Operation = TxOperation.Finish,
                        Timestamp = DateTimeOffset.UtcNow,
                        TimestampFormat = TimestampFormat.UnixTime
                    }
                }
            },
            Count = 2,
            Type = ResourceType.TransactionList
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery == $"/api/v2/tss/{_testTssId.Value}/tx"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedResponse, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        TxListResponse result = await client.ListTransactionsAsync(_testTssId, queryParameters: null, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(ResourceType.TransactionList, result.Type);
    }

    [Fact]
    public async Task ListTransactionsAsync_WithCustomOffset_BuildsCorrectUrl()
    {
        // Arrange
        const int offset = 100;
        ListTransactionsQueryParameters queryParams = new ListTransactionsQueryParameters { Offset = offset };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"offset={offset}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new TxListResponse { Data = new(), Count = 0, Type = ResourceType.TransactionList }, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        await client.ListTransactionsAsync(_testTssId, queryParams, CancellationToken.None);

        // Assert: Verified by mock
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains($"offset={offset}")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListTransactionsAsync_WithCustomLimit_BuildsCorrectUrl()
    {
        // Arrange
        const int limit = 50;
        ListTransactionsQueryParameters queryParams = new ListTransactionsQueryParameters { Limit = limit };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"limit={limit}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new TxListResponse { Data = new(), Count = 0, Type = ResourceType.TransactionList }, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        await client.ListTransactionsAsync(_testTssId, queryParams, CancellationToken.None);

        // Assert: Verified by mock
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains($"limit={limit}")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListTransactionsAsync_WithShowDeleted_BuildsCorrectUrl()
    {
        // Arrange
        ListTransactionsQueryParameters queryParams = new ListTransactionsQueryParameters { ShowDeleted = true };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains("show_deleted=true")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new TxListResponse { Data = new(), Count = 0, Type = ResourceType.TransactionList }, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        await client.ListTransactionsAsync(_testTssId, queryParams, CancellationToken.None);

        // Assert: Verified by mock
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("show_deleted=true")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListTransactionsAsync_WithMultipleParameters_CombinesAllInUrl()
    {
        // Arrange
        ListTransactionsQueryParameters queryParams = new ListTransactionsQueryParameters
        {
            Limit = 25,
            Offset = 50,
            ShowDeleted = false
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains("limit=25") &&
                    req.RequestUri.PathAndQuery.Contains("offset=50") &&
                    req.RequestUri.PathAndQuery.Contains("show_deleted=false")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new TxListResponse { Data = new(), Count = 0, Type = ResourceType.TransactionList }, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        await client.ListTransactionsAsync(_testTssId, queryParams, CancellationToken.None);

        // Assert: Verified by mock - all three parameters should be in URL
        mockHandler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.PathAndQuery.Contains("limit=25") &&
                req.RequestUri.PathAndQuery.Contains("offset=50") &&
                req.RequestUri.PathAndQuery.Contains("show_deleted=false")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListTransactionsAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange: API returns empty transaction list
        TxListResponse emptyResponse = new TxListResponse
        {
            Data = new List<TxResponse>(),
            Count = 0,
            Type = ResourceType.TransactionList
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(emptyResponse, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            });

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act
        TxListResponse result = await client.ListTransactionsAsync(_testTssId, queryParameters: null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task ListTransactionsAsync_WithDefaultTssId_ThrowsArgumentException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.ListTransactionsAsync(default, queryParameters: null, CancellationToken.None));
    }

    [Fact]
    public async Task ListTransactionsAsync_PropagatesCancellationToken()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.ListTransactionsAsync(_testTssId, queryParameters: null, cts.Token));
    }

    #endregion

    #region Cancellation Token Propagation Tests (6 tests)

    [Fact]
    public async Task GetTransactionAsync_PropagatesCancellationToken()
    {
        // Arrange: This tests cancellation in GetTransactionAsync (used by ResolveRevisionAsync)
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.GetTransactionAsync(_testTssId, _testTransactionId, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithAutoFetch_PropagatesCancellationToGetRequest()
    {
        // Arrange: Test cancellation during GET (auto-fetch phase)
        CancellationTokenSource cts = new CancellationTokenSource();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                cts.Cancel(); // Cancel during GET
            })
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.UpdateTransactionAsync(_testTssId, _testTransactionId, request,
                txRevision: null, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task CancelTransactionAsync_WithAutoFetch_PropagatesCancellationToGetRequest()
    {
        // Arrange: Test cancellation during GET (auto-fetch phase)
        CancellationTokenSource cts = new CancellationTokenSource();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                cts.Cancel(); // Cancel during GET
            })
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        CancelTransactionRequest request = CreateValidCancelRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.CancelTransactionAsync(_testTssId, _testTransactionId, request,
                txRevision: null, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task UpdateTransactionAsync_WithAutoFetch_PropagatesCancellationToPutRequest()
    {
        // Arrange: Test cancellation during PUT (after successful GET)
        CancellationTokenSource cts = new CancellationTokenSource();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // GET succeeds
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, 2));

        // PUT gets cancelled
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                cts.Cancel(); // Cancel during PUT
            })
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        UpdateTransactionRequest request = CreateValidUpdateRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.UpdateTransactionAsync(_testTssId, _testTransactionId, request,
                txRevision: null, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task FinishTransactionAsync_WithAutoFetch_PropagatesCancellationToPutRequest()
    {
        // Arrange: Test cancellation during PUT (after successful GET)
        CancellationTokenSource cts = new CancellationTokenSource();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // GET succeeds
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateGetTransactionResponse(TxState.Active, 3));

        // PUT gets cancelled
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                cts.Cancel(); // Cancel during PUT
            })
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        FinishTransactionRequest request = CreateValidFinishRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.FinishTransactionAsync(_testTssId, _testTransactionId, request,
                txRevision: null, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task StartTransactionAsync_PropagatesCancellationToPutRequest()
    {
        // Arrange: START only has PUT (no GET), test cancellation
        CancellationTokenSource cts = new CancellationTokenSource();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                cts.Cancel(); // Cancel during PUT
            })
            .ThrowsAsync(new TaskCanceledException());

        TransactionClient client = CreateTransactionClient(mockHandler.Object);
        StartTransactionRequest request = CreateValidStartRequest();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.StartTransactionAsync(_testTssId, _testTransactionId, request, cts.Token));
    }

    #endregion
}
