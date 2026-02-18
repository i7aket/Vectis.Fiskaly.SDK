using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Vectis.Fiskaly.SDK.Authentication;
using Vectis.Fiskaly.SDK.Handlers;

namespace Vectis.Fiskaly.UnitTests.Handlers;

/// <summary>
/// Unit tests for <see cref="JwtAuthHandler"/>.
/// Tests JWT token injection, error handling, and security-critical behavior.
/// </summary>
public class JwtAuthHandlerTests : IDisposable
{
    private readonly Mock<IFiskalyAuthenticationService> _authServiceMock;
    private readonly ILogger<JwtAuthHandler> _logger;
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;
    private readonly JwtAuthHandler _handler;
    private readonly HttpMessageInvoker _invoker;

    public JwtAuthHandlerTests()
    {
        _authServiceMock = new Mock<IFiskalyAuthenticationService>();
        _logger = NullLogger<JwtAuthHandler>.Instance;
        _innerHandlerMock = new Mock<HttpMessageHandler>();

        _handler = new JwtAuthHandler(_authServiceMock.Object, _logger);
        _handler.InnerHandler = _innerHandlerMock.Object;

        _invoker = new HttpMessageInvoker(_handler);
    }

    public void Dispose()
    {
        _handler?.Dispose();
        _invoker?.Dispose();
    }

    #region Constructor Tests

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        JwtAuthHandler handler = new JwtAuthHandler(_authServiceMock.Object, _logger);

        // Assert
        Assert.NotNull(handler);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullAuthService_ThrowsArgumentNullException()
    {
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new JwtAuthHandler(null!, _logger));

        Assert.Equal("authService", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new JwtAuthHandler(_authServiceMock.Object, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    #endregion

    #region SendAsync Core Behavior Tests

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_AddsAuthorizationHeader_WithBearerScheme()
    {
        // Arrange
        const string expectedToken = "test_jwt_token_12345";
        _authServiceMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act
        await _invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(expectedToken, request.Headers.Authorization.Parameter);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_UsesTokenFromAuthService()
    {
        // Arrange
        const string expectedToken = "unique_token_abc123xyz";
        _authServiceMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.fiskaly.com/resource");

        // Act
        await _invoker.SendAsync(request, CancellationToken.None);

        // Assert
        _authServiceMock.Verify(
            x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(expectedToken, request.Headers.Authorization?.Parameter);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_CallsBaseHandler_AndReturnsResponse()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test_token");

        HttpResponseMessage expectedResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("Success")
        };

        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.fiskaly.com/test");

        // Act
        HttpResponseMessage actualResponse = await _invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Same(expectedResponse, actualResponse);
        Assert.Equal(HttpStatusCode.Created, actualResponse.StatusCode);

        _innerHandlerMock
            .Protected()
            .Verify("SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_PassesCancellationToken_ToAuthService()
    {
        // Arrange
        using CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken cancellationToken = cts.Token;

        _authServiceMock
            .Setup(x => x.GetAccessTokenAsync(cancellationToken))
            .ReturnsAsync("test_token");

        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act
        await _invoker.SendAsync(request, cancellationToken);

        // Assert
        _authServiceMock.Verify(
            x => x.GetAccessTokenAsync(cancellationToken),
            Times.Once);
    }

    #endregion

    #region Edge Case Tests

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WhenAuthServiceThrows_PropagatesException()
    {
        // Arrange
        InvalidOperationException expectedException = new InvalidOperationException("Authentication failed");

        _authServiceMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _invoker.SendAsync(request, CancellationToken.None));

        Assert.Same(expectedException, actualException);
        Assert.Equal("Authentication failed", actualException.Message);

        // Verify inner handler was NOT called (exception prevented it)
        _innerHandlerMock
            .Protected()
            .Verify("SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        _authServiceMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _invoker.SendAsync(request, cts.Token));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task SendAsync_OverwritesExistingAuthHeader_WithFreshToken()
    {
        // Arrange
        const string freshToken = "fresh_token_from_service";

        _authServiceMock
            .Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(freshToken);

        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.fiskaly.com/test");

        // Pre-set an old/stale authorization header (security test: ensure it gets replaced)
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "old_stale_token");

        // Act
        await _invoker.SendAsync(request, CancellationToken.None);

        // Assert - Critical security check: handler MUST replace old token with fresh one
        Assert.NotNull(request.Headers.Authorization);
        Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
        Assert.Equal(freshToken, request.Headers.Authorization.Parameter);
        Assert.NotEqual("old_stale_token", request.Headers.Authorization.Parameter);
    }

    #endregion
}
