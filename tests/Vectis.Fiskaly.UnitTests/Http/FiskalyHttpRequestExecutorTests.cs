using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Responses;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Responses;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.Authentication.Models;
using Vectis.Fiskaly.SDK.Authentication.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Http;

public class FiskalyHttpRequestExecutorTests : IDisposable
{
    private readonly Mock<ILogger<FiskalyHttpRequestExecutor>> _loggerMock;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly FiskalyHttpRequestExecutor _executor;

    public FiskalyHttpRequestExecutorTests()
    {
        _loggerMock = new Mock<ILogger<FiskalyHttpRequestExecutor>>();
        _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttp) { BaseAddress = new Uri("https://kassensichv-middleware.fiskaly.com/api/v2/") };
        _executor = new FiskalyHttpRequestExecutor(_serializerOptions, _loggerMock.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHttp?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSerializerOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FiskalyHttpRequestExecutor(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FiskalyHttpRequestExecutor(_serializerOptions, null!));
    }

    #endregion

    #region ExecuteGetAsync Tests

    [Fact]
    public async Task ExecuteGetAsync_ValidUrl_ReturnsDeserializedResponse()
    {
        // Arrange
        TssResponse expectedResponse = new TssResponse
        {
            Id = TssId.From("a1b2c3d4-e5f6-4890-abcd-ef1234567890"),
            Env = Env.Test,
            Description = "Test TSS",
            State = TssState.Initialized,
            Type = ResourceType.Tss,
            SerialNumber = TssSerialNumber.From("test-serial-123"),
            TimeCreation = DateTimeOffset.UtcNow,
            Certificate = "MIICertificate...",
            PublicKey = "MIIBIjANBgkqhki...",
            SignatureAlgorithm = Algorithm.EcdsaPlainSha256,
            SignatureTimestampFormat = TimestampFormat.UnixTime,
            TransactionDataEncoding = DataEncoding.Utf8,
            MaxNumberActiveTransactions = 2000,
            MaxNumberRegisteredClients = 1000,
            SupportedUpdateVariants = SupportedUpdateVariants.Signed,
            Version = "2.0"
        };

        _mockHttp.When("https://kassensichv-middleware.fiskaly.com/api/v2/tss/123")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, _serializerOptions));

        // Act
        TssResponse result = await _executor.ExecuteGetAsync<TssResponse>(_httpClient, "tss/123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal("Test TSS", result.Description);
        Assert.Equal(TssState.Initialized, result.State);
    }

    [Fact]
    public async Task ExecuteGetAsync_NullUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecuteGetAsync<object>(_httpClient, null!));
    }

    [Fact]
    public async Task ExecuteGetAsync_EmptyUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _executor.ExecuteGetAsync<object>(_httpClient, string.Empty));
    }

    [Fact]
    public async Task ExecuteGetAsync_WhitespaceUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _executor.ExecuteGetAsync<object>(_httpClient, "   "));
    }

    [Fact]
    public async Task ExecuteGetAsync_LogsDebugMessage()
    {
        // Arrange
        bool logCalled = false;
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
        _loggerMock.Setup(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logCalled = true);

        _mockHttp.When("https://kassensichv-middleware.fiskaly.com/api/v2/tss/123")
            .Respond("application/json", "{}");

        // Act
        await _executor.ExecuteGetAsync<object>(_httpClient, "tss/123");

        // Assert
        Assert.True(logCalled, "Debug logging should have been called");
    }

    #endregion

    #region ExecutePutAsync Tests

    [Fact]
    public async Task ExecutePutAsync_ValidRequest_ReturnsDeserializedResponse()
    {
        // Arrange
        var request = new { Metadata = new Dictionary<string, string> { { "key", "value" } } };
        TssResponse expectedResponse = new TssResponse
        {
            Id = TssId.From("a1b2c3d4-e5f6-4890-abcd-ef1234567890"),
            Env = Env.Test,
            State = TssState.Created,
            Type = ResourceType.Tss
        };

        _mockHttp.When(HttpMethod.Put, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, _serializerOptions));

        // Act
        TssResponse result = await _executor.ExecutePutAsync<object, TssResponse>(_httpClient, "tss/123", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(TssState.Created, result.State);
    }

    [Fact]
    public async Task ExecutePutAsync_NullUrl_ThrowsArgumentException()
    {
        // Arrange
        var request = new { Metadata = new Dictionary<string, string>() };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecutePutAsync<object, object>(_httpClient, null!, request));
    }

    [Fact]
    public async Task ExecutePutAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecutePutAsync<object, object>(_httpClient, "tss/123", null!));
    }

    [Fact]
    public async Task ExecutePutAsync_LogsDebugMessage()
    {
        // Arrange
        bool logCalled = false;
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
        _loggerMock.Setup(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logCalled = true);

        var request = new { Data = "test" };
        _mockHttp.When(HttpMethod.Put, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123")
            .Respond("application/json", "{}");

        // Act
        await _executor.ExecutePutAsync<object, object>(_httpClient, "tss/123", request);

        // Assert
        Assert.True(logCalled, "Debug logging should have been called");
    }

    #endregion

    #region ExecutePatchAsync Tests

    [Fact]
    public async Task ExecutePatchAsync_ValidRequest_ReturnsDeserializedResponse()
    {
        // Arrange
        var request = new { State = "DEREGISTERED" };
        ClientResponse expectedResponse = new ClientResponse
        {
            Id = ClientId.From("b2c3d4e5-f6a7-4901-bcde-f23456789012"),
            SerialNumber = ClientSerialNumber.From("CLIENT-123"),
            State = ClientState.Deregistered,
            TssId = TssId.From("a1b2c3d4-e5f6-4890-abcd-ef1234567890"),
            Env = Env.Test,
            Type = ResourceType.Client,
            Version = "2.1.33",
            TimeCreation = DateTimeOffset.UtcNow,
            TimeUpdate = DateTimeOffset.UtcNow
        };

        _mockHttp.When(HttpMethod.Patch, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123/client/456")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, _serializerOptions));

        // Act
        ClientResponse result = await _executor.ExecutePatchAsync<object, ClientResponse>(_httpClient, "tss/123/client/456", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(ClientState.Deregistered, result.State);
    }

    [Fact]
    public async Task ExecutePatchAsync_NullUrl_ThrowsArgumentException()
    {
        // Arrange
        var request = new { State = "DISABLED" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecutePatchAsync<object, object>(_httpClient, null!, request));
    }

    [Fact]
    public async Task ExecutePatchAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecutePatchAsync<object, object>(_httpClient, "tss/123", null!));
    }

    [Fact]
    public async Task ExecutePatchAsync_LogsDebugMessage()
    {
        // Arrange
        bool logCalled = false;
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
        _loggerMock.Setup(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logCalled = true);

        var request = new { Data = "test" };
        _mockHttp.When(HttpMethod.Patch, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123/client/456")
            .Respond("application/json", "{}");

        // Act
        await _executor.ExecutePatchAsync<object, object>(_httpClient, "tss/123/client/456", request);

        // Assert
        Assert.True(logCalled, "Debug logging should have been called");
    }

    #endregion

    #region ExecutePostAsync (with response) Tests

    [Fact]
    public async Task ExecutePostAsync_WithResponse_ReturnsDeserializedResponse()
    {
        // Arrange
        var request = new { api_key = "test-key", api_secret = "test-secret" };
        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ0IjoiMTIzIn0.SIGN123";
        const string refresh = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyIjoiNDU2In0.REF456";
        AuthenticationResponse expectedResponse = new AuthenticationResponse
        {
            AccessToken = AccessToken.From(token),
            RefreshToken = RefreshToken.From(refresh),
            ExpiresIn = 600
        };

        _mockHttp.When(HttpMethod.Post, "https://kassensichv-middleware.fiskaly.com/api/v2/auth")
            .Respond("application/json", JsonSerializer.Serialize(expectedResponse, _serializerOptions));

        // Act
        AuthenticationResponse result = await _executor.ExecutePostAsync<object, AuthenticationResponse>(_httpClient, "auth", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token, result!.AccessToken.Value);
    }

    [Fact]
    public async Task ExecutePostAsync_WithResponse_NullUrl_ThrowsArgumentException()
    {
        // Arrange
        var request = new { admin_pin = "12345" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecutePostAsync<object, object>(_httpClient, null!, request));
    }

    [Fact]
    public async Task ExecutePostAsync_WithResponse_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecutePostAsync<object, object>(_httpClient, "tss/123/admin/auth", null!));
    }

    #endregion

    #region ExecutePostAsync (no response) Tests

    [Fact]
    public async Task ExecutePostAsync_NoResponse_CompletesSuccessfully()
    {
        // Arrange
        var request = new { admin_pin = "12345" };

        _mockHttp.When(HttpMethod.Post, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123/admin/logout")
            .Respond(HttpStatusCode.OK);

        // Act & Assert (should not throw)
        await _executor.ExecutePostAsync(_httpClient, "tss/123/admin/logout", request);
    }

    [Fact]
    public async Task ExecutePostAsync_NoResponse_NullUrl_ThrowsArgumentException()
    {
        // Arrange
        var request = new { admin_pin = "12345" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecutePostAsync(_httpClient, null!, request));
    }

    [Fact]
    public async Task ExecutePostAsync_NoResponse_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecutePostAsync<object>(_httpClient, "tss/123/admin/logout", null!));
    }

    [Fact]
    public async Task ExecutePostAsync_NoResponse_LogsDebugMessage()
    {
        // Arrange
        bool logCalled = false;
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
        _loggerMock.Setup(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logCalled = true);

        var request = new { admin_pin = "12345" };
        _mockHttp.When(HttpMethod.Post, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123/admin/logout")
            .Respond(HttpStatusCode.OK);

        // Act
        await _executor.ExecutePostAsync(_httpClient, "tss/123/admin/logout", request);

        // Assert
        Assert.True(logCalled, "Debug logging should have been called");
    }

    #endregion

    #region ExecuteDeleteAsync Tests

    [Fact]
    public async Task ExecuteDeleteAsync_ValidUrl_CompletesSuccessfully()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Delete, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123/export/456")
            .Respond(HttpStatusCode.NoContent);

        // Act & Assert (should not throw)
        await _executor.ExecuteDeleteAsync(_httpClient, "tss/123/export/456");
    }

    [Fact]
    public async Task ExecuteDeleteAsync_NullUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _executor.ExecuteDeleteAsync(_httpClient, null!));
    }

    [Fact]
    public async Task ExecuteDeleteAsync_EmptyUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _executor.ExecuteDeleteAsync(_httpClient, string.Empty));
    }

    [Fact]
    public async Task ExecuteDeleteAsync_LogsDebugMessage()
    {
        // Arrange
        bool logCalled = false;
        _loggerMock.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
        _loggerMock.Setup(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Debug),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logCalled = true);

        _mockHttp.When(HttpMethod.Delete, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123/export/456")
            .Respond(HttpStatusCode.NoContent);

        // Act
        await _executor.ExecuteDeleteAsync(_httpClient, "tss/123/export/456");

        // Assert
        Assert.True(logCalled, "Debug logging should have been called");
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteGetAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttp.When("https://kassensichv-middleware.fiskaly.com/api/v2/tss/123")
            .Respond("application/json", "{}");

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _executor.ExecuteGetAsync<object>(_httpClient, "tss/123", cts.Token));
    }

    [Fact]
    public async Task ExecutePutAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new { Data = "test" };

        _mockHttp.When(HttpMethod.Put, "https://kassensichv-middleware.fiskaly.com/api/v2/tss/123")
            .Respond("application/json", "{}");

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _executor.ExecutePutAsync<object, object>(_httpClient, "tss/123", request, cts.Token));
    }

    #endregion
}
