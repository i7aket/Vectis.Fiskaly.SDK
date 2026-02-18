using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports;
using Vectis.Fiskaly.SDK.SignDE.Exports.Dsfinvk;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;
using Vectis.Fiskaly.SDK.SignDE.Exports.Models;
using Vectis.Fiskaly.SDK.SignDE.Exports.Responses;
using Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Exports;

/// <summary>
/// Unit tests for ExportClient following Microsoft and dev community best practices.
/// Tests focus on:
/// - Constructor validation
/// - Export triggering operations (Full, Client, Log)
/// - Export status and lifecycle operations
/// - Metadata operations
/// - Cancellation token propagation
/// </summary>
[Trait("Category", "Unit")]
public class ExportClientTests
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TssId _testTssId = TssId.New();
    private readonly ExportId _testExportId = ExportId.New();
    private readonly ClientId _testClientId = ClientId.New();

    public ExportClientTests()
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
    /// Creates an ExportClient with real executor and mocked HttpClient.
    /// </summary>
    private ExportClient CreateExportClient(
        HttpMessageHandler httpMessageHandler,
        ILogger<ExportClient>? logger = null,
        IDsfinvkVersionStrategy? dsfinvkStrategy = null)
    {
        HttpClient httpClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("https://kassensichv-middleware.fiskaly.com/api/v2/")
        };

        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(
            _jsonOptions,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FiskalyHttpRequestExecutor>()
        );

        IDsfinvkVersionStrategy strategy = dsfinvkStrategy ?? Mock.Of<IDsfinvkVersionStrategy>();

        return new ExportClient(
            httpClient,
            executor,
            logger ?? new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportClient>(),
            _jsonOptions,
            strategy
        );
    }

    /// <summary>
    /// Creates a mock HTTP response for export operations.
    /// </summary>
    private HttpResponseMessage CreateExportJobResponse(
        ExportId exportId,
        TssId tssId,
        ExportState state,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        ClientId? clientId = null,
        MetadataCollection? metadata = null)
    {
        ExportJob exportJob = new ExportJob
        {
            Id = exportId,
            TssId = tssId,
            State = state,
            StartDate = startDate,
            EndDate = endDate,
            ClientId = clientId,
            Metadata = metadata,
            Type = ResourceType.Export,
            Env = Env.Test,
            Version = "2.1.33",
            TimeStart = state == ExportState.Working ? DateTimeOffset.UtcNow : null,
            TimeEnd = state == ExportState.Completed ? DateTimeOffset.UtcNow : null
        };

        string json = JsonSerializer.Serialize(exportJob, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>
    /// Creates a mock HTTP response for list export operations.
    /// </summary>
    private HttpResponseMessage CreateListExportsResponse(params ExportJob[] exports)
    {
        ListExportsResponse listResponse = new ListExportsResponse
        {
            Data = [.. exports],
            Count = exports.Length,
            Type = ResourceType.ExportList,
            Env = Env.Test,
            Version = "2.1.33"
        };

        string json = JsonSerializer.Serialize(listResponse, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>
    /// Creates a mock HTTP response for metadata operations.
    /// </summary>
    private HttpResponseMessage CreateMetadataResponse(MetadataCollection metadata)
    {
        string json = JsonSerializer.Serialize(metadata, _jsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    #endregion

    #region Constructor Tests (5 tests)

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(
            _jsonOptions,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FiskalyHttpRequestExecutor>()
        );
        NullLogger<ExportClient> logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportClient>();
        IDsfinvkVersionStrategy strategy = Mock.Of<IDsfinvkVersionStrategy>();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ExportClient(null!, executor, logger, _jsonOptions, strategy));

        Assert.Equal("httpClient", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        NullLogger<ExportClient> logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportClient>();
        IDsfinvkVersionStrategy strategy = Mock.Of<IDsfinvkVersionStrategy>();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ExportClient(httpClient, null!, logger, _jsonOptions, strategy));

        Assert.Equal("executor", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(
            _jsonOptions,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FiskalyHttpRequestExecutor>()
        );
        IDsfinvkVersionStrategy strategy = Mock.Of<IDsfinvkVersionStrategy>();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ExportClient(httpClient, executor, null!, _jsonOptions, strategy));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSerializerOptions_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(
            _jsonOptions,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FiskalyHttpRequestExecutor>()
        );
        NullLogger<ExportClient> logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportClient>();
        IDsfinvkVersionStrategy strategy = Mock.Of<IDsfinvkVersionStrategy>();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ExportClient(httpClient, executor, logger, null!, strategy));

        Assert.Equal("serializerOptions", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDsfinvkStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(
            _jsonOptions,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FiskalyHttpRequestExecutor>()
        );
        NullLogger<ExportClient> logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ExportClient>();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ExportClient(httpClient, executor, logger, _jsonOptions, null!));

        Assert.Equal("dsfinvkStrategy", exception.ParamName);
    }

    #endregion

    #region TriggerFullExportAsync Tests (4 tests)

    [Fact]
    public async Task TriggerFullExportAsync_WithValidRequest_ReturnsExportJob()
    {
        // Arrange
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartDate = DateTimeOffset.UtcNow.AddDays(-7),
            EndDate = DateTimeOffset.UtcNow
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateExportJobResponse(
                _testExportId,
                _testTssId,
                ExportState.Pending,
                request.StartDate,
                request.EndDate));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ExportJob result = await client.TriggerFullExportAsync(_testTssId, _testExportId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testExportId, result.Id);
        Assert.Equal(_testTssId, result.TssId);
        Assert.Equal(ExportState.Pending, result.State);
        Assert.Equal(request.StartDate, result.StartDate);
        Assert.Equal(request.EndDate, result.EndDate);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export/{_testExportId}")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task TriggerFullExportAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.TriggerFullExportAsync(_testTssId, _testExportId, null!));
    }

    [Fact]
    public async Task TriggerFullExportAsync_WithClientIdFilter_ReturnsExportJobWithClientId()
    {
        // Arrange
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartDate = DateTimeOffset.UtcNow.AddDays(-7),
            EndDate = DateTimeOffset.UtcNow,
            ClientId = _testClientId
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateExportJobResponse(
                _testExportId,
                _testTssId,
                ExportState.Pending,
                request.StartDate,
                request.EndDate,
                _testClientId));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ExportJob result = await client.TriggerFullExportAsync(_testTssId, _testExportId, request);

        // Assert
        Assert.Equal(_testClientId, result.ClientId);
    }

    [Fact]
    public async Task TriggerFullExportAsync_PropagatesCancellationToken()
    {
        // Arrange
        DsfinvkFullExportRequest request = new DsfinvkFullExportRequest
        {
            StartDate = DateTimeOffset.UtcNow.AddDays(-7),
            EndDate = DateTimeOffset.UtcNow
        };

        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.TriggerFullExportAsync(_testTssId, _testExportId, request, cts.Token));
    }

    #endregion

    #region TriggerClientExportAsync Tests (3 tests)

    [Fact]
    public async Task TriggerClientExportAsync_WithValidRequest_ReturnsExportJob()
    {
        // Arrange
        DsfinvkClientExportRequest request = new DsfinvkClientExportRequest(_testClientId);

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateExportJobResponse(
                _testExportId,
                _testTssId,
                ExportState.Pending,
                clientId: _testClientId));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ExportJob result = await client.TriggerClientExportAsync(_testTssId, _testExportId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testExportId, result.Id);
        Assert.Equal(_testTssId, result.TssId);
        Assert.Equal(ExportState.Pending, result.State);
        Assert.Equal(_testClientId, result.ClientId);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export/{_testExportId}") &&
                req.RequestUri!.ToString().Contains($"client_id={_testClientId}")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task TriggerClientExportAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.TriggerClientExportAsync(_testTssId, _testExportId, null!));
    }

    [Fact]
    public async Task TriggerClientExportAsync_PropagatesCancellationToken()
    {
        // Arrange
        DsfinvkClientExportRequest request = new DsfinvkClientExportRequest(_testClientId);
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.TriggerClientExportAsync(_testTssId, _testExportId, request, cts.Token));
    }

    #endregion

    #region TriggerLogExportAsync Tests (3 tests)

    [Fact]
    public async Task TriggerLogExportAsync_WithValidRequest_ReturnsExportJob()
    {
        // Arrange
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest
        {
            TransactionNumber = TransactionSequenceNumber.From(1234)
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateExportJobResponse(
                _testExportId,
                _testTssId,
                ExportState.Pending));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ExportJob result = await client.TriggerLogExportAsync(_testTssId, _testExportId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testExportId, result.Id);
        Assert.Equal(_testTssId, result.TssId);
        Assert.Equal(ExportState.Pending, result.State);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export/{_testExportId}") &&
                req.RequestUri!.ToString().Contains("transaction_number=1234")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task TriggerLogExportAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.TriggerLogExportAsync(_testTssId, _testExportId, null!));
    }

    [Fact]
    public async Task TriggerLogExportAsync_PropagatesCancellationToken()
    {
        // Arrange
        DsfinvkLogExportRequest request = new DsfinvkLogExportRequest();
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.TriggerLogExportAsync(_testTssId, _testExportId, request, cts.Token));
    }

    #endregion

    #region GetExportAsync Tests (3 tests)

    [Fact]
    public async Task GetExportAsync_WithValidIds_ReturnsExportJob()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateExportJobResponse(
                _testExportId,
                _testTssId,
                ExportState.Completed));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ExportJob result = await client.GetExportAsync(_testTssId, _testExportId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testExportId, result.Id);
        Assert.Equal(_testTssId, result.TssId);
        Assert.Equal(ExportState.Completed, result.State);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export/{_testExportId}")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetExportAsync_PropagatesCancellationToken()
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

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.GetExportAsync(_testTssId, _testExportId, cts.Token));
    }

    #endregion

    #region CancelExportAsync Tests (2 tests)

    [Fact]
    public async Task CancelExportAsync_WithValidIds_ReturnsExportJobWithCancelledState()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateExportJobResponse(
                _testExportId,
                _testTssId,
                ExportState.Cancelled));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ExportJob result = await client.CancelExportAsync(_testTssId, _testExportId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testExportId, result.Id);
        Assert.Equal(_testTssId, result.TssId);
        Assert.Equal(ExportState.Cancelled, result.State);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Delete &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export/{_testExportId}")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CancelExportAsync_PropagatesCancellationToken()
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

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.CancelExportAsync(_testTssId, _testExportId, cts.Token));
    }

    #endregion

    #region ListExportsAsync Tests (3 tests)

    [Fact]
    public async Task ListExportsAsync_WithValidTssId_ReturnsListExportsResponse()
    {
        // Arrange
        ExportJob export1 = new ExportJob
        {
            Id = _testExportId,
            Env = Env.Test,
            TssId = _testTssId,
            State = ExportState.Completed,
            Type = ResourceType.Export
        };

        ExportJob export2 = new ExportJob
        {
            Id = ExportId.New(),
            Env = Env.Test,
            TssId = _testTssId,
            State = ExportState.Pending,
            Type = ResourceType.Export
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateListExportsResponse(export1, export2));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ListExportsResponse result = await client.ListExportsAsync(_testTssId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, e => e.Id == _testExportId && e.State == ExportState.Completed);
        Assert.Contains(result.Data, e => e.State == ExportState.Pending);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListExportsAsync_WithQueryParameters_IncludesParametersInRequest()
    {
        // Arrange
        ListExportsQueryParameters queryParams = new ListExportsQueryParameters
        {
            Limit = 10,
            Offset = 5
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateListExportsResponse());

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ListExportsResponse result = await client.ListExportsAsync(_testTssId, queryParams);

        // Assert
        Assert.NotNull(result);
        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export") &&
                req.RequestUri!.ToString().Contains("limit=10") &&
                req.RequestUri!.ToString().Contains("offset=5")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListExportsAsync_PropagatesCancellationToken()
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

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.ListExportsAsync(_testTssId, cancellationToken: cts.Token));
    }

    #endregion

    #region ListAllExportsAsync Tests (2 tests)

    [Fact]
    public async Task ListAllExportsAsync_WithoutParameters_ReturnsAllExports()
    {
        // Arrange
        ExportJob export1 = new ExportJob
        {
            Id = _testExportId,
            Env = Env.Test,
            TssId = _testTssId,
            State = ExportState.Completed,
            Type = ResourceType.Export
        };

        ExportJob export2 = new ExportJob
        {
            Id = ExportId.New(),
            Env = Env.Test,
            TssId = TssId.New(),
            State = ExportState.Working,
            Type = ResourceType.Export
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateListExportsResponse(export1, export2));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        ListExportsResponse result = await client.ListAllExportsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Data.Count);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().EndsWith("export")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ListAllExportsAsync_PropagatesCancellationToken()
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

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.ListAllExportsAsync(cancellationToken: cts.Token));
    }

    #endregion

    #region GetExportMetadataAsync Tests (3 tests)

    [Fact]
    public async Task GetExportMetadataAsync_WithValidIds_ReturnsMetadataCollection()
    {
        // Arrange
        MetadataCollection expectedMetadata = MetadataCollection.From(new Dictionary<string, string>
        {
            { "requester", "admin@example.com" },
            { "purpose", "tax audit" }
        });

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateMetadataResponse(expectedMetadata));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.GetExportMetadataAsync(_testTssId, _testExportId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("admin@example.com", result["requester"]);
        Assert.Equal("tax audit", result["purpose"]);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export/{_testExportId}/metadata")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetExportMetadataAsync_PropagatesCancellationToken()
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

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.GetExportMetadataAsync(_testTssId, _testExportId, cts.Token));
    }

    #endregion

    #region UpdateExportMetadataAsync Tests (3 tests)

    [Fact]
    public async Task UpdateExportMetadataAsync_WithValidMetadata_ReturnsUpdatedMetadata()
    {
        // Arrange
        MetadataCollection metadata = MetadataCollection.From(new Dictionary<string, string>
        {
            { "requester", "admin@example.com" },
            { "purpose", "tax audit" }
        });

        MetadataCollection updatedMetadata = MetadataCollection.From(new Dictionary<string, string>
        {
            { "requester", "admin@example.com" },
            { "purpose", "tax audit" },
            { "status", "completed" }
        });

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateMetadataResponse(updatedMetadata));

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.UpdateExportMetadataAsync(_testTssId, _testExportId, metadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("completed", result["status"]);

        mockHandler.Protected().Verify("SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Patch &&
                req.RequestUri!.ToString().Contains($"tss/{_testTssId}/export/{_testExportId}/metadata")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdateExportMetadataAsync_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.UpdateExportMetadataAsync(_testTssId, _testExportId, null!));
    }

    [Fact]
    public async Task UpdateExportMetadataAsync_PropagatesCancellationToken()
    {
        // Arrange
        MetadataCollection metadata = MetadataCollection.From(new Dictionary<string, string>
        {
            { "test", "value" }
        });
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel the token

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(ct => ct.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        ExportClient client = CreateExportClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await client.UpdateExportMetadataAsync(_testTssId, _testExportId, metadata, cts.Token));
    }

    #endregion
}
