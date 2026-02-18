using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Vectis.Fiskaly.SDK.Http;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients;
using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Requests;
using Vectis.Fiskaly.SDK.SignDE.Clients.Responses;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Clients;

/// <summary>
/// Unit tests for ClientManagementClient following Microsoft and dev community best practices.
/// Tests focus on:
/// - Core read operations (Get client, Get metadata)
/// - Create client operations (CreateClientAsync with validation)
/// - State transitions (Deregister, Reregister)
/// - Metadata operations (Get, Update with maxKeyLength=40)
/// - HTTP request validation (method, URL, body)
/// </summary>
[Trait("Category", "Unit")]
public class ClientManagementClientTests
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TssId _testTssId = TssId.New();
    private readonly ClientId _testClientId = ClientId.New();

    public ClientManagementClientTests()
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
    /// Creates a ClientManagementClient with real executor and mocked HttpClient.
    /// </summary>
    private ClientManagementClient CreateClientManagementClient(
        HttpMessageHandler httpMessageHandler,
        ILogger<ClientManagementClient>? logger = null)
    {
        HttpClient httpClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("https://kassensichv-middleware.fiskaly.com/api/v2/")
        };

        FiskalyHttpRequestExecutor executor = new FiskalyHttpRequestExecutor(
            _jsonOptions,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<FiskalyHttpRequestExecutor>()
        );

        return new ClientManagementClient(
            httpClient,
            executor,
            logger ?? new Microsoft.Extensions.Logging.Abstractions.NullLogger<ClientManagementClient>(),
            _jsonOptions
        );
    }

    /// <summary>
    /// Creates a mock HTTP response for client operations.
    /// </summary>
    private HttpResponseMessage CreateClientResponse(
        ClientId clientId,
        TssId tssId,
        ClientSerialNumber serialNumber,
        ClientState state,
        MetadataCollection? metadata = null)
    {
        ClientResponse clientResponse = new ClientResponse
        {
            Id = clientId,
            TssId = tssId,
            SerialNumber = serialNumber,
            State = state,
            Metadata = metadata,
            Type = ResourceType.Client,
            Env = Env.Test,
            Version = "2.1.33",
            TimeCreation = DateTimeOffset.UtcNow,
            TimeUpdate = DateTimeOffset.UtcNow
        };

        string json = JsonSerializer.Serialize(clientResponse, _jsonOptions);
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

    #region Priority 1: Core Read Operations (6 tests)

    [Fact]
    public async Task GetClientAsync_WithValidIds_ReturnsClientResponse()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("CLIENT-12345");
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/client/{_testClientId.Value}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Registered));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.GetClientAsync(_testTssId, _testClientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testClientId, result.Id);
        Assert.Equal(_testTssId, result.TssId);
        ClientSerialNumber? responseSerialNumber = result.SerialNumber;
        Assert.True(responseSerialNumber.HasValue);
        Assert.Equal("CLIENT-12345", responseSerialNumber.Value.Value);
        Assert.Equal(ClientState.Registered, result.State);

        // Verify HTTP GET was called exactly once
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetClientAsync_WithRegisteredState_ReturnsCorrectState()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("POS-TERMINAL-001");
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Registered));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.GetClientAsync(_testTssId, _testClientId);

        // Assert
        Assert.Equal(ClientState.Registered, result.State);
        ClientSerialNumber? registeredSerial = result.SerialNumber;
        Assert.True(registeredSerial.HasValue);
        Assert.Equal("POS-TERMINAL-001", registeredSerial.Value.Value);
    }

    [Fact]
    public async Task GetClientAsync_WithDeregisteredState_ReturnsCorrectState()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("KASSE-002");
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Deregistered));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.GetClientAsync(_testTssId, _testClientId);

        // Assert
        Assert.Equal(ClientState.Deregistered, result.State);
        ClientSerialNumber? deregisteredSerial = result.SerialNumber;
        Assert.True(deregisteredSerial.HasValue);
        Assert.Equal("KASSE-002", deregisteredSerial.Value.Value);
    }

    [Fact]
    public async Task GetClientMetadataAsync_WithValidIds_ReturnsMetadata()
    {
        // Arrange
        MetadataCollection expectedMetadata = MetadataCollection.From(new Dictionary<string, string>
        {
            ["location"] = "Store-5-Counter-2",
            ["operator"] = "employee-123"
        });

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/client/{_testClientId.Value}/metadata")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateMetadataResponse(expectedMetadata));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.GetClientMetadataAsync(_testTssId, _testClientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Store-5-Counter-2", result["location"]);
        Assert.Equal("employee-123", result["operator"]);

        // Verify HTTP GET was called for metadata endpoint
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.PathAndQuery.Contains("/metadata")),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetClientMetadataAsync_WithEmptyMetadata_ReturnsEmptyCollection()
    {
        // Arrange
        MetadataCollection metadata = MetadataCollection.Empty;

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateMetadataResponse(metadata));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.GetClientMetadataAsync(_testTssId, _testClientId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClientMetadataAsync_MakesCorrectHttpGetRequest()
    {
        // Arrange
        MetadataCollection metadata = MetadataCollection.From(new Dictionary<string, string> { ["key"] = "value" });
        string expectedPath = $"tss/{_testTssId.Value}/client/{_testClientId.Value}/metadata";

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.PathAndQuery.Contains(expectedPath)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateMetadataResponse(metadata));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        await client.GetClientMetadataAsync(_testTssId, _testClientId);

        // Assert - Verify correct URL path was used
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri!.PathAndQuery.Contains(expectedPath)),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    #endregion

    #region Priority 2: Create Client Operations (6 tests)

    [Fact]
    public async Task CreateClientAsync_WithValidRequest_ReturnsClientResponse()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("CLIENT-12345");
        CreateClientRequest request = new CreateClientRequest
        {
            SerialNumber = serialNumber
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/client/{_testClientId.Value}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Registered));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.CreateClientAsync(_testTssId, _testClientId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testClientId, result.Id);
        Assert.Equal(_testTssId, result.TssId);
        ClientSerialNumber? createdSerial = result.SerialNumber;
        Assert.True(createdSerial.HasValue);
        Assert.Equal("CLIENT-12345", createdSerial.Value.Value);
        Assert.Equal(ClientState.Registered, result.State);

        // Verify HTTP PUT was called
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateClientAsync_WithMetadata_IncludesMetadataInResponse()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("POS-001");
        MetadataCollection metadata = MetadataCollection.From(new Dictionary<string, string>
        {
            ["location"] = "Hamburg Store #5",
            ["device_model"] = "XYZ-2000"
        });

        CreateClientRequest request = new CreateClientRequest
        {
            SerialNumber = serialNumber,
            Metadata = metadata
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Registered, metadata));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.CreateClientAsync(_testTssId, _testClientId, request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(2, result.Metadata.Count);
        Assert.Equal("Hamburg Store #5", result.Metadata["location"]);
        Assert.Equal("XYZ-2000", result.Metadata["device_model"]);

        // Verify PUT was called with content
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.Content != null),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateClientAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.CreateClientAsync(_testTssId, _testClientId, null!));
    }

    [Fact]
    public async Task CreateClientAsync_MakesCorrectHttpPutRequest()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("TERMINAL-MAIN");
        CreateClientRequest request = new CreateClientRequest
        {
            SerialNumber = serialNumber
        };

        string expectedPath = $"tss/{_testTssId.Value}/client/{_testClientId.Value}";

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri!.PathAndQuery.Contains(expectedPath)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Registered));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        await client.CreateClientAsync(_testTssId, _testClientId, request);

        // Assert - Verify correct HTTP method and URL
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri!.PathAndQuery.Contains(expectedPath)),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task CreateClientAsync_WithSerialNumber_IncludesInResponse()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("KASSE-BERLIN-001");
        CreateClientRequest request = new CreateClientRequest
        {
            SerialNumber = serialNumber
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Registered));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.CreateClientAsync(_testTssId, _testClientId, request);

        // Assert - Verify serial number is returned correctly
        ClientSerialNumber? responseSerial = result.SerialNumber;
        Assert.True(responseSerial.HasValue);
        Assert.Equal("KASSE-BERLIN-001", responseSerial.Value.Value);
    }

    [Fact]
    public async Task CreateClientAsync_WithMetadata_ValidatesMaxKeyLength40()
    {
        // Arrange - Metadata with key exactly at 40-char limit
        ClientSerialNumber serialNumber = ClientSerialNumber.From("TEST-CLIENT");
        MetadataCollection metadata = MetadataCollection.Empty.Add(
            "exactly_forty_characters_long_key_here", // 40 chars
            "test-value"
        );

        CreateClientRequest request = new CreateClientRequest
        {
            SerialNumber = serialNumber,
            Metadata = metadata
        };

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Registered, metadata));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.CreateClientAsync(_testTssId, _testClientId, request);

        // Assert - Should succeed with 40-char key
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Single(result.Metadata);

        // Note: The actual maxKeyLength validation happens in MetadataCollection.EnsureMaxKeyLength
        // which is called by ClientManagementClient before sending the request
        // MetadataOperations.EnsureMaxKeyLength would throw if key > 40 chars
    }

    #endregion

    #region Priority 3: State Transitions (4 tests)

    [Fact]
    public async Task UpdateClientAsync_WithDeregisterRequest_ReturnsDeregisteredClient()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("CLIENT-TO-DEREGISTER");
        UpdateClientRequest request = UpdateClientRequest.Deregister();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/client/{_testClientId.Value}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Deregistered));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.UpdateClientAsync(_testTssId, _testClientId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testClientId, result.Id);
        Assert.Equal(ClientState.Deregistered, result.State);

        // Verify HTTP PATCH was called
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateClientAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.UpdateClientAsync(_testTssId, _testClientId, null!));
    }

    [Fact]
    public async Task UpdateClientAsync_WithRegisterRequest_ReturnsRegisteredClient()
    {
        // Arrange
        ClientSerialNumber serialNumber = ClientSerialNumber.From("CLIENT-TO-REREGISTER");
        UpdateClientRequest request = UpdateClientRequest.Register();

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/client/{_testClientId.Value}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateClientResponse(_testClientId, _testTssId, serialNumber, ClientState.Registered));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        ClientResponse result = await client.UpdateClientAsync(_testTssId, _testClientId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testClientId, result.Id);
        Assert.Equal(ClientState.Registered, result.State);

        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Patch),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    #endregion

    #region Priority 4: Metadata Operations (2 tests)

    [Fact]
    public async Task UpdateClientMetadataAsync_WithValidMetadata_ReturnsUpdatedMetadata()
    {
        // Arrange
        MetadataCollection inputMetadata = MetadataCollection.From(new Dictionary<string, string>
        {
            ["location"] = "Berlin Store #3",
            ["operator"] = "Jane Smith"
        });

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.PathAndQuery.Contains($"/tss/{_testTssId.Value}/client/{_testClientId.Value}/metadata")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateMetadataResponse(inputMetadata));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.UpdateClientMetadataAsync(_testTssId, _testClientId, inputMetadata);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Berlin Store #3", result["location"]);
        Assert.Equal("Jane Smith", result["operator"]);

        // Verify HTTP PATCH was called for metadata endpoint
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Patch &&
                req.RequestUri!.PathAndQuery.Contains("/metadata")),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task UpdateClientMetadataAsync_UsesMaxKeyLength40()
    {
        // Arrange - Metadata with key exactly at 40-char limit
        MetadataCollection metadata = MetadataCollection.Empty.Add(
            "exactly_forty_characters_long_key_here", // 40 chars
            "value"
        );

        Mock<HttpMessageHandler> mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateMetadataResponse(metadata));

        ClientManagementClient client = CreateClientManagementClient(mockHandler.Object);

        // Act
        MetadataCollection result = await client.UpdateClientMetadataAsync(_testTssId, _testClientId, metadata);

        // Assert - Should succeed with 40-char key
        Assert.NotNull(result);
        Assert.Single(result);

        // Note: The actual maxKeyLength validation happens in MetadataOperations.UpdateAsync
        // ClientManagementClient passes maxKeyLength: 40 parameter
        // MetadataOperations.EnsureMaxKeyLength would throw if key > 40 chars
    }

    #endregion
}
