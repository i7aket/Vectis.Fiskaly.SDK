using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Exceptions;
using Vectis.Fiskaly.SDK.Extensions;

namespace Vectis.Fiskaly.UnitTests.Extensions;

public class HttpContentExtensionsTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpContentExtensionsTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    // ============================================================================
    // Success Cases
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        string json = """
                      {
                          "name": "Test",
                          "value": 123
                      }
                      """;
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        TestDto result = await content.ReadFiskalyJsonAsync<TestDto>(_jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(123, result.Value);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithComplexObject_DeserializesCorrectly()
    {
        // Arrange
        string json = """
                      {
                          "name": "Complex",
                          "value": 456,
                          "nested": {
                              "id": "abc-123"
                          }
                      }
                      """;
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        ComplexTestDto result = await content.ReadFiskalyJsonAsync<ComplexTestDto>(_jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Complex", result.Name);
        Assert.Equal(456, result.Value);
        Assert.NotNull(result.Nested);
        Assert.Equal("abc-123", result.Nested.Id);
    }

    // ============================================================================
    // Error Cases
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithNullContent_ThrowsArgumentNullException()
    {
        // Arrange
        HttpContent? content = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            content!.ReadFiskalyJsonAsync<TestDto>(_jsonOptions));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithNullSerializerOptions_ThrowsArgumentNullException()
    {
        // Arrange
        string json = """{"name": "Test", "value": 123}""";
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            content.ReadFiskalyJsonAsync<TestDto>(null!));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithNullJsonValue_ThrowsFiskalyException()
    {
        // Arrange
        string json = "null"; // Valid JSON but deserializes to null
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act & Assert
        FiskalyException exception = await Assert.ThrowsAsync<FiskalyException>(() =>
            content.ReadFiskalyJsonAsync<TestDto>(_jsonOptions));

        Assert.Contains("Failed to deserialize JSON to type TestDto", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        string json = "{invalid json}"; // Malformed JSON
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            content.ReadFiskalyJsonAsync<TestDto>(_jsonOptions));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithTypeMismatch_ThrowsJsonException()
    {
        // Arrange
        string json = """
                      {
                          "name": "Test",
                          "value": "not_a_number"
                      }
                      """;
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            content.ReadFiskalyJsonAsync<TestDto>(_jsonOptions));
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithEmptyObject_ReturnsDefaultValues()
    {
        // Arrange
        string json = "{}"; // Empty object
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        TestDto result = await content.ReadFiskalyJsonAsync<TestDto>(_jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name); // String defaults to null
        Assert.Equal(0, result.Value); // Int defaults to 0
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        string json = "[]"; // Empty array
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        TestDto[] result = await content.ReadFiskalyJsonAsync<TestDto[]>(_jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public async Task ReadFiskalyJsonAsync_WithArrayOfObjects_DeserializesAll()
    {
        // Arrange
        string json = """
                      [
                          {"name": "First", "value": 1},
                          {"name": "Second", "value": 2}
                      ]
                      """;
        using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        TestDto[] result = await content.ReadFiskalyJsonAsync<TestDto[]>(_jsonOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("First", result[0].Name);
        Assert.Equal(1, result[0].Value);
        Assert.Equal("Second", result[1].Name);
        Assert.Equal(2, result[1].Value);
    }

    // ============================================================================
    // Test DTOs
    // ============================================================================

    private class TestDto
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    private class ComplexTestDto
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public NestedDto? Nested { get; set; }
    }

    private class NestedDto
    {
        public string? Id { get; set; }
    }
}
