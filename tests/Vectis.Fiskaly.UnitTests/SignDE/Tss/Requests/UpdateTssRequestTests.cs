using System.Text.Json;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.Requests;

namespace Vectis.Fiskaly.UnitTests.SignDE.Tss.Requests;

public sealed class UpdateTssRequestTests
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    [Trait("Category", "Unit")]
    [Fact]
    public void Initialize_Factory_WithDescription_SetsStateAndDescription()
    {
        const string description = "Main POS Terminal - Store 1";
        MetadataCollection metadata = MetadataCollection.Empty.Add("location", "Store 1");

        UpdateTssRequest request = UpdateTssRequest.Initialize(description, metadata);

        Assert.Equal(TssState.Initialized, request.State);
        Assert.Equal(description, request.Description);
        Assert.Equal(metadata, request.Metadata);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Initialize_Factory_WithEmptyString_NormalizesToNull()
    {
        UpdateTssRequest request = UpdateTssRequest.Initialize(string.Empty, null);

        Assert.Equal(TssState.Initialized, request.State);
        Assert.Null(request.Description);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("Test's POS")]
    [InlineData("Store (Main)")]
    [InlineData("Location-1")]
    [InlineData("Price: 10.50")]
    [InlineData("Sum: 1+2")]
    [InlineData("A/B/C")]
    [InlineData("Test,1,2,3")]
    [InlineData("fiskaly sign cloud-TSE (tssid)")]
    public void Initialize_Factory_WithAllowedCharacters_AcceptsValue(string description)
    {
        UpdateTssRequest request = UpdateTssRequest.Initialize(description, null);

        Assert.Equal(description, request.Description);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Initialize_Factory_WithTooLongDescription_ThrowsArgumentException()
    {
        string description = new('A', 101);

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            UpdateTssRequest.Initialize(description, null));

        Assert.Equal("description", exception.ParamName);
        Assert.Contains("must not exceed", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("Test@Email")]
    [InlineData("Hash#Tag")]
    [InlineData("Dollar$Sign")]
    [InlineData("Percent%")]
    [InlineData("And&Symbol")]
    [InlineData("Star*")]
    [InlineData("Underscore_")]
    [InlineData("Backslash\\")]
    [InlineData("Pipe|")]
    [InlineData("Semicolon;")]
    [InlineData("Exclaim!")]
    public void Initialize_Factory_WithInvalidCharacters_ThrowsArgumentException(string description)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            UpdateTssRequest.Initialize(description, null));

        Assert.Equal("description", exception.ParamName);
        Assert.Contains("invalid characters", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Uninitialize_Factory_WithMetadata_SetsStateAndMetadata()
    {
        MetadataCollection metadata = MetadataCollection.Empty.Add("phase", "personalization");

        UpdateTssRequest request = UpdateTssRequest.Uninitialize(metadata);

        Assert.Equal(TssState.Uninitialized, request.State);
        Assert.Null(request.Description);
        Assert.Equal(metadata, request.Metadata);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Disable_Factory_WithoutMetadata_SetsState()
    {
        UpdateTssRequest request = UpdateTssRequest.Disable();

        Assert.Equal(TssState.Disabled, request.State);
        Assert.Null(request.Description);
        Assert.Null(request.Metadata);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_InitializeRequest_UsesSnakeCaseAndOmitNulls()
    {
        UpdateTssRequest request = UpdateTssRequest.Initialize(
            "Production TSS",
            MetadataCollection.Empty.Add("store", "Berlin"));

        string json = JsonSerializer.Serialize(request, _serializerOptions);

        Assert.Contains("\"state\":\"INITIALIZED\"", json);
        Assert.Contains("\"description\":\"Production TSS\"", json);
        Assert.Contains("\"metadata\"", json);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Serialize_UninitializeRequest_OmitsDescriptionWhenNull()
    {
        UpdateTssRequest request = UpdateTssRequest.Uninitialize();

        string json = JsonSerializer.Serialize(request, _serializerOptions);

        Assert.Contains("\"state\":\"UNINITIALIZED\"", json);
        Assert.DoesNotContain("\"description\"", json);
    }
}
