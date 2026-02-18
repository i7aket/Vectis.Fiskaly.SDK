namespace Vectis.Fiskaly.UnitTests.SignDE.Common;

public class QueryParameterExtensionsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithNoParameters_ReturnsBasePathUnchanged()
    {
        // Arrange
        EmptyQueryParameters provider = new EmptyQueryParameters();

        // Act
        string url = provider.BuildUrl("api/tss");

        // Assert
        Assert.Equal("api/tss", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithSingleParameter_AppendsQueryString()
    {
        // Arrange
        SingleParameterQueryParameters provider = new SingleParameterQueryParameters { Limit = 50 };

        // Act
        string url = provider.BuildUrl("api/tss");

        // Assert
        Assert.Equal("api/tss?limit=50", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithMultipleParameters_EncodesCorrectly()
    {
        // Arrange
        MultipleParameterQueryParameters provider = new MultipleParameterQueryParameters
        {
            Limit = 50,
            Offset = 100,
            ShowDeleted = true
        };

        // Act
        string url = provider.BuildUrl("api/tss");

        // Assert
        Assert.Equal("api/tss?limit=50&offset=100&show_deleted=true", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithSpecialCharacters_EncodesCorrectly()
    {
        // Arrange
        SpecialCharQueryParameters provider = new SpecialCharQueryParameters
        {
            SerialNumber = "ABC-123 & Co."
        };

        // Act
        string url = provider.BuildUrl("api/client");

        // Assert
        Assert.Contains("serial_number=ABC-123%20%26%20Co.", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithSpacesAndSymbols_EncodesCorrectly()
    {
        // Arrange
        SpecialCharQueryParameters provider = new SpecialCharQueryParameters
        {
            SerialNumber = "Test?Value=1&Other=2"
        };

        // Act
        string url = provider.BuildUrl("api/resource");

        // Assert
        Assert.Contains("serial_number=Test%3FValue%3D1%26Other%3D2", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithNullProvider_ThrowsArgumentNullException()
    {
        // Arrange
        IQueryParameterProvider provider = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => provider.BuildUrl("api/tss"));
        Assert.Contains("provider", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithNullBasePath_ThrowsArgumentNullException()
    {
        // Arrange
        EmptyQueryParameters provider = new EmptyQueryParameters();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => provider.BuildUrl(null!));
        Assert.Contains("basePath", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithEmptyBasePath_WorksCorrectly()
    {
        // Arrange
        SingleParameterQueryParameters provider = new SingleParameterQueryParameters { Limit = 10 };

        // Act
        string url = provider.BuildUrl("");

        // Assert
        Assert.Equal("?limit=10", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithExistingQueryString_AppendsAdditionalParameters()
    {
        // Arrange
        SingleParameterQueryParameters provider = new SingleParameterQueryParameters { Limit = 50 };

        // Act
        string url = provider.BuildUrl("api/tss?existing=value");

        // Assert
        Assert.Equal("api/tss?existing=value&limit=50", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithArrayParameters_EncodesArrayIndices()
    {
        // Arrange
        ArrayParameterQueryParameters provider = new ArrayParameterQueryParameters
        {
            States = new[] { "INITIALIZED", "CREATED" }
        };

        // Act
        string url = provider.BuildUrl("api/tss");

        // Assert
        Assert.Contains("states%5B0%5D=INITIALIZED", url);
        Assert.Contains("states%5B1%5D=CREATED", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithNullParameterValues_SkipsNullValues()
    {
        // Arrange
        NullableParameterQueryParameters provider = new NullableParameterQueryParameters
        {
            Limit = 50,
            Offset = null // Null value should be skipped
        };

        // Act
        string url = provider.BuildUrl("api/tss");

        // Assert
        Assert.Equal("api/tss?limit=50", url);
        Assert.DoesNotContain("offset", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithComplexPath_PreservesPathStructure()
    {
        // Arrange
        SingleParameterQueryParameters provider = new SingleParameterQueryParameters { Limit = 25 };

        // Act
        string url = provider.BuildUrl("tss/abc123/client/xyz456/tx");

        // Assert
        Assert.Equal("tss/abc123/client/xyz456/tx?limit=25", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithUnicodeCharacters_EncodesCorrectly()
    {
        // Arrange
        SpecialCharQueryParameters provider = new SpecialCharQueryParameters
        {
            SerialNumber = "Café München"
        };

        // Act
        string url = provider.BuildUrl("api/search");

        // Assert
        Assert.Contains("serial_number=Caf%C3%A9%20M%C3%BCnchen", url);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithMultipleCallsOnSameProvider_ProducesSameResult()
    {
        // Arrange
        MultipleParameterQueryParameters provider = new MultipleParameterQueryParameters
        {
            Limit = 50,
            Offset = 100,
            ShowDeleted = true
        };

        // Act
        string url1 = provider.BuildUrl("api/tss");
        string url2 = provider.BuildUrl("api/tss");

        // Assert
        Assert.Equal(url1, url2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void BuildUrl_WithBooleanParameters_ConvertsToLowercase()
    {
        // Arrange
        MultipleParameterQueryParameters provider = new MultipleParameterQueryParameters
        {
            ShowDeleted = true
        };

        // Act
        string url = provider.BuildUrl("api/tss");

        // Assert
        Assert.Contains("show_deleted=true", url);
    }

    #region Test Helper Classes

    private class EmptyQueryParameters : IQueryParameterProvider
    {
        public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
        {
            yield break; // No parameters
        }
    }

    private class SingleParameterQueryParameters : IQueryParameterProvider
    {
        public int? Limit { get; set; }

        public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
        {
            if (Limit.HasValue)
                yield return new KeyValuePair<string, string?>("limit", Limit.Value.ToString());
        }
    }

    private class MultipleParameterQueryParameters : IQueryParameterProvider
    {
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public bool? ShowDeleted { get; set; }

        public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
        {
            if (Limit.HasValue)
                yield return new KeyValuePair<string, string?>("limit", Limit.Value.ToString());

            if (Offset.HasValue)
                yield return new KeyValuePair<string, string?>("offset", Offset.Value.ToString());

            if (ShowDeleted.HasValue)
                yield return new KeyValuePair<string, string?>("show_deleted", ShowDeleted.Value.ToString().ToLower());
        }
    }

    private class SpecialCharQueryParameters : IQueryParameterProvider
    {
        public string? SerialNumber { get; set; }

        public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
        {
            if (SerialNumber != null)
                yield return new KeyValuePair<string, string?>("serial_number", SerialNumber);
        }
    }

    private class ArrayParameterQueryParameters : IQueryParameterProvider
    {
        public string[]? States { get; set; }

        public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
        {
            if (States is { Length: > 0 })
            {
                for (int i = 0; i < States.Length; i++)
                {
                    yield return new KeyValuePair<string, string?>($"states[{i}]", States[i]);
                }
            }
        }
    }

    private class NullableParameterQueryParameters : IQueryParameterProvider
    {
        public int? Limit { get; set; }
        public int? Offset { get; set; }

        public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
        {
            if (Limit.HasValue)
                yield return new KeyValuePair<string, string?>("limit", Limit.Value.ToString());

            // Note: Offset is intentionally not yielded when null
        }
    }

    #endregion
}
