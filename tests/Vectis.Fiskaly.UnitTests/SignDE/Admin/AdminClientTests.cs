using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.Handlers;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.Metrics;
using Vectis.Fiskaly.SDK.SignDE.Admin;
using Vectis.Fiskaly.SDK.SignDE.Admin.Requests;
using Vectis.Fiskaly.SDK.SignDE.Admin.Responses;
using Vectis.Fiskaly.SDK.SignDE.Admin.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Admin;

public class AdminClientTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public AdminClientTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Creates an AdminClient with mocked HttpMessageHandler for testing.
    /// </summary>
    private AdminClient CreateAdminClient(
        HttpMessageHandler httpMessageHandler,
        ILogger<AdminClient>? logger = null)
    {
        HttpClient httpClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("https://api.fiskaly.com/api/v2/")
        };

        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(
            _jsonOptions,
            NullLogger<FiskalyHttpRequestExecutor>.Instance);

        return new AdminClient(
            httpClient,
            executor,
            logger ?? NullLogger<AdminClient>.Instance);
    }

    /// <summary>
    /// Creates an AdminClient with error handling pipeline for testing error propagation.
    /// </summary>
    private AdminClient CreateAdminClientWithErrorHandler(
        HttpMessageHandler httpMessageHandler,
        ILogger<AdminClient>? logger = null)
    {
        // Create error handler with mocked dependencies
        Mock<IMeterFactory> mockMeterFactory = new Mock<IMeterFactory>(MockBehavior.Loose);
        mockMeterFactory.Setup(x => x.Create(It.IsAny<MeterOptions>()))
            .Returns(new Meter("TestMeter"));

        FiskalyMetrics metrics = new FiskalyMetrics(mockMeterFactory.Object);
        FiskalyErrorHandler errorHandler = new FiskalyErrorHandler(
            NullLogger<FiskalyErrorHandler>.Instance,
            _jsonOptions,
            metrics)
        {
            InnerHandler = httpMessageHandler
        };

        HttpClient httpClient = new HttpClient(errorHandler)
        {
            BaseAddress = new Uri("https://api.fiskaly.com/api/v2/")
        };

        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(
            _jsonOptions,
            NullLogger<FiskalyHttpRequestExecutor>.Instance);

        return new AdminClient(
            httpClient,
            executor,
            logger ?? NullLogger<AdminClient>.Instance);
    }

    // ========================================
    // Constructor Tests (4 tests)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Arrange
        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(_jsonOptions, NullLogger<FiskalyHttpRequestExecutor>.Instance);

        // Act
        Exception? exception = Record.Exception(() => new AdminClient(null!, executor, NullLogger<AdminClient>.Instance));

        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
        ((ArgumentNullException)exception!).ParamName.Should().Be("httpClient");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullExecutor_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();

        // Act
        Exception? exception = Record.Exception(() => new AdminClient(httpClient, null!, NullLogger<AdminClient>.Instance));

        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
        ((ArgumentNullException)exception!).ParamName.Should().Be("executor");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        HttpClient httpClient = new HttpClient();
        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(_jsonOptions, NullLogger<FiskalyHttpRequestExecutor>.Instance);

        // Act
        Exception? exception = Record.Exception(() => new AdminClient(httpClient, executor, null!));

        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
        ((ArgumentNullException)exception!).ParamName.Should().Be("logger");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // Act
        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Assert
        client.Should().NotBeNull();
    }

    // ========================================
    // AuthenticateAdminAsync Tests (5 tests)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task AuthenticateAdminAsync_WithValidCredentials_ReturnsSuccessResponse()
    {
        // Arrange
        TssId tssId = TssId.New();
        AdminPin adminPin = AdminPin.From("123456");
        AdminAuthenticationRequest request = new AdminAuthenticationRequest
        {
            AdminPin = adminPin
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Act
        AdminAuthenticationResponse result = await client.AuthenticateAdminAsync(tssId, request);

        // Assert
        result.Should().NotBeNull();
        result.TssId.Should().Be(tssId);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task AuthenticateAdminAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        TssId tssId = TssId.New();
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Act
        Exception? exception = await Record.ExceptionAsync(() =>
            client.AuthenticateAdminAsync(tssId, null!));

        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task AuthenticateAdminAsync_WithApiError_PropagatesFiskalyApiException()
    {
        // Arrange
        TssId tssId = TssId.New();
        AdminPin adminPin = AdminPin.From("123456");
        AdminAuthenticationRequest request = new AdminAuthenticationRequest
        {
            AdminPin = adminPin
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"code\":\"E_UNAUTHORIZED\",\"message\":\"Invalid credentials\"}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClientWithErrorHandler(mockHandler.Object);

        // Act
        Exception? exception = await Record.ExceptionAsync(() =>
            client.AuthenticateAdminAsync(tssId, request));

        // Assert
        exception.Should().BeOfType<FiskalyApiException>();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task AuthenticateAdminAsync_WithValidRequest_LogsInformation()
    {
        // Arrange
        TssId tssId = TssId.New();
        AdminPin adminPin = AdminPin.From("123456");
        AdminAuthenticationRequest request = new AdminAuthenticationRequest
        {
            AdminPin = adminPin
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        Mock<ILogger<AdminClient>> mockLogger = new Mock<ILogger<AdminClient>>(MockBehavior.Loose);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object, mockLogger.Object);

        // Act
        await client.AuthenticateAdminAsync(tssId, request);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Authenticating admin")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task AuthenticateAdminAsync_WithValidRequest_UsesCorrectEndpoint()
    {
        // Arrange
        TssId tssId = TssId.New();
        AdminPin adminPin = AdminPin.From("123456");
        AdminAuthenticationRequest request = new AdminAuthenticationRequest
        {
            AdminPin = adminPin
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedRequest = req;
                // Capture body BEFORE HttpContent is disposed
                if (req.Content != null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync();
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Act
        await client.AuthenticateAdminAsync(tssId, request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().Should().Contain($"tss/{tssId.Value}/admin/auth");

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Be("{\"admin_pin\":\"123456\"}");
    }

    // ========================================
    // LogoutAdminAsync Tests (4 tests)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task LogoutAdminAsync_WithValidTssId_CompletesSuccessfully()
    {
        // Arrange
        TssId tssId = TssId.New();
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Act
        Exception? exception = await Record.ExceptionAsync(() =>
            client.LogoutAdminAsync(tssId));

        // Assert
        exception.Should().BeNull();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task LogoutAdminAsync_WithNullTssId_ThrowsArgumentException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Act
        Exception? exception = await Record.ExceptionAsync(() =>
            client.LogoutAdminAsync(default));

        // Assert
        exception.Should().NotBeNull();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task LogoutAdminAsync_WithApiError_PropagatesFiskalyApiException()
    {
        // Arrange
        TssId tssId = TssId.New();
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"code\":\"E_BAD_REQUEST\",\"message\":\"Bad request\"}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClientWithErrorHandler(mockHandler.Object);

        // Act
        Exception? exception = await Record.ExceptionAsync(() =>
            client.LogoutAdminAsync(tssId));

        // Assert
        exception.Should().BeOfType<FiskalyApiException>();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task LogoutAdminAsync_WithValidRequest_LogsInformation()
    {
        // Arrange
        TssId tssId = TssId.New();
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        Mock<ILogger<AdminClient>> mockLogger = new Mock<ILogger<AdminClient>>(MockBehavior.Loose);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object, mockLogger.Object);

        // Act
        await client.LogoutAdminAsync(tssId);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Logging out admin")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // ========================================
    // ChangeAdminPinAsync Tests (6 tests)
    // ========================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ChangeAdminPinAsync_WithValidRequest_CompletesSuccessfully()
    {
        // Arrange
        TssId tssId = TssId.New();
        ChangeAdminPinRequest request = new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From("1234567890"),
            NewAdminPin = AdminPin.From("654321")
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Act
        Exception? exception = await Record.ExceptionAsync(() =>
            client.ChangeAdminPinAsync(tssId, request));

        // Assert
        exception.Should().BeNull();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ChangeAdminPinAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        TssId tssId = TssId.New();
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Act
        Exception? exception = await Record.ExceptionAsync(() =>
            client.ChangeAdminPinAsync(tssId, null!));

        // Assert
        exception.Should().BeOfType<ArgumentNullException>();
        ((ArgumentNullException)exception!).ParamName.Should().Be("request");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ChangeAdminPinAsync_WithApiError_PropagatesFiskalyApiException()
    {
        // Arrange
        TssId tssId = TssId.New();
        ChangeAdminPinRequest request = new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From("1234567890"),
            NewAdminPin = AdminPin.From("654321")
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("{\"code\":\"E_FORBIDDEN\",\"message\":\"Invalid PUK\"}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClientWithErrorHandler(mockHandler.Object);

        // Act
        Exception? exception = await Record.ExceptionAsync(() =>
            client.ChangeAdminPinAsync(tssId, request));

        // Assert
        exception.Should().BeOfType<FiskalyApiException>();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ChangeAdminPinAsync_WithValidRequest_LogsInformation()
    {
        // Arrange
        TssId tssId = TssId.New();
        ChangeAdminPinRequest request = new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From("1234567890"),
            NewAdminPin = AdminPin.From("654321")
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        Mock<ILogger<AdminClient>> mockLogger = new Mock<ILogger<AdminClient>>(MockBehavior.Loose);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object, mockLogger.Object);

        // Act
        await client.ChangeAdminPinAsync(tssId, request);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Changing admin PIN")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ChangeAdminPinAsync_WithValidRequest_ShouldNeverLogCredentials()
    {
        // Arrange
        TssId tssId = TssId.New();
        string pukValue = "1234567890";
        string pinValue = "654321";
        ChangeAdminPinRequest request = new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From(pukValue),
            NewAdminPin = AdminPin.From(pinValue)
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        Mock<ILogger<AdminClient>> mockLogger = new Mock<ILogger<AdminClient>>(MockBehavior.Loose);

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object, mockLogger.Object);

        // Act
        await client.ChangeAdminPinAsync(tssId, request);

        // Assert - Verify that credentials are NEVER logged
        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(pukValue)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Admin PUK value should NEVER be logged");

        mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(pinValue)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Admin PIN value should NEVER be logged");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ChangeAdminPinAsync_WithValidRequest_UsesCorrectEndpointAndMethod()
    {
        // Arrange
        TssId tssId = TssId.New();
        ChangeAdminPinRequest request = new ChangeAdminPinRequest
        {
            AdminPuk = AdminPuk.From("1234567890"),
            NewAdminPin = AdminPin.From("654321")
        };
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        HttpRequestMessage? capturedRequest = null;

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        AdminClient client = CreateAdminClient(mockHandler.Object);

        // Act
        await client.ChangeAdminPinAsync(tssId, request);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Patch);
        capturedRequest.RequestUri!.ToString().Should().Contain($"tss/{tssId.Value}/admin");
    }
}
