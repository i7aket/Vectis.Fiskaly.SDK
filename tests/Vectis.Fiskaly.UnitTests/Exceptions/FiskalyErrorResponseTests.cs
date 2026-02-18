using System.Text.Json;
using Vectis.Fiskaly.SDK.Exceptions;

namespace Vectis.Fiskaly.UnitTests.Exceptions;

public class FiskalyErrorResponseTests
{
    private readonly JsonSerializerOptions _options;

    public FiskalyErrorResponseTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // ============================================================================
    // Deserialize Complete Response Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ValidErrorResponse_SetsAllProperties()
    {
        string json = """
                      {
                          "code": "E_TSS_NOT_FOUND",
                          "message": "TSS with ID 'abc' not found",
                          "status_code": 404,
                          "error": "Not Found"
                      }
                      """;

        FiskalyErrorResponse? result = JsonSerializer.Deserialize<FiskalyErrorResponse>(json, _options);

        Assert.NotNull(result);
        Assert.Equal("E_TSS_NOT_FOUND", result.Code);
        Assert.Equal("TSS with ID 'abc' not found", result.Message);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Not Found", result.Error);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_ConflictError_ParsesCorrectly()
    {
        string json = """
                      {
                          "code": "E_TSS_LOCKED",
                          "message": "TSS is currently locked",
                          "status_code": 409,
                          "error": "Conflict"
                      }
                      """;

        FiskalyErrorResponse? result = JsonSerializer.Deserialize<FiskalyErrorResponse>(json, _options);

        Assert.NotNull(result);
        Assert.Equal("E_TSS_LOCKED", result.Code);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Conflict", result.Error);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_VerifyJsonPropertyNames_UsesSnakeCase()
    {
        // Verify that JSON uses snake_case for property names
        string json = """
                      {
                          "code": "E_CLIENT_NOT_FOUND",
                          "message": "Client not found",
                          "status_code": 404,
                          "error": "Not Found"
                      }
                      """;

        FiskalyErrorResponse? result = JsonSerializer.Deserialize<FiskalyErrorResponse>(json, _options);

        Assert.NotNull(result);
        Assert.Equal("E_CLIENT_NOT_FOUND", result.Code);
        Assert.Equal(404, result.StatusCode);
    }

    // ============================================================================
    // Required Properties Validation Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MissingCode_ThrowsJsonException()
    {
        string json = """
                      {
                          "message": "Some error",
                          "status_code": 500,
                          "error": "Internal Server Error"
                      }
                      """;

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<FiskalyErrorResponse>(json, _options));

        Assert.NotNull(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MissingMessage_ThrowsJsonException()
    {
        string json = """
                      {
                          "code": "E_UNKNOWN",
                          "status_code": 500,
                          "error": "Internal Server Error"
                      }
                      """;

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<FiskalyErrorResponse>(json, _options));

        Assert.NotNull(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MissingStatusCode_ThrowsJsonException()
    {
        string json = """
                      {
                          "code": "E_UNKNOWN",
                          "message": "Some error",
                          "error": "Internal Server Error"
                      }
                      """;

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<FiskalyErrorResponse>(json, _options));

        Assert.NotNull(exception);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Deserialize_MissingError_ThrowsJsonException()
    {
        string json = """
                      {
                          "code": "E_UNKNOWN",
                          "message": "Some error",
                          "status_code": 500
                      }
                      """;

        JsonException exception = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<FiskalyErrorResponse>(json, _options));

        Assert.NotNull(exception);
    }

    // ============================================================================
    // Serialize Error Response Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_ErrorResponse_ReturnsCorrectJson()
    {
        FiskalyErrorResponse response = new FiskalyErrorResponse
        {
            Code = "E_TSS_NOT_FOUND",
            Message = "TSS not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(response, _options);

        Assert.Contains("\"code\":\"E_TSS_NOT_FOUND\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"message\":\"TSS not found\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"status_code\":404", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"error\":\"Not Found\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_VerifySnakeCasePropertyNames()
    {
        FiskalyErrorResponse response = new FiskalyErrorResponse
        {
            Code = "TEST",
            Message = "Test message",
            StatusCode = 400,
            Error = "Bad Request"
        };

        string json = JsonSerializer.Serialize(response, _options);

        // Verify snake_case property names in serialized JSON
        Assert.Contains("status_code", json, StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================================
    // Round-trip Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void RoundTrip_PreservesAllData()
    {
        FiskalyErrorResponse original = new FiskalyErrorResponse
        {
            Code = "E_TX_NOT_FOUND",
            Message = "Transaction not found",
            StatusCode = 404,
            Error = "Not Found"
        };

        string json = JsonSerializer.Serialize(original, _options);
        FiskalyErrorResponse? deserialized = JsonSerializer.Deserialize<FiskalyErrorResponse>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Code, deserialized.Code);
        Assert.Equal(original.Message, deserialized.Message);
        Assert.Equal(original.StatusCode, deserialized.StatusCode);
        Assert.Equal(original.Error, deserialized.Error);
    }
}
