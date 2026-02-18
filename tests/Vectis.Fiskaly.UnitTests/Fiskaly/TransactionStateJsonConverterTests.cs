using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class TransactionStateJsonConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(TxState.Active, "\"ACTIVE\"")]
    [InlineData(TxState.Finished, "\"FINISHED\"")]
    [InlineData(TxState.Cancelled, "\"CANCELLED\"")]
    public void Serialize_WritesUppercaseApiValue(TxState state, string expectedJson)
    {
        JsonSerializerOptions options = CreateOptions();

        string json = JsonSerializer.Serialize(state, options);

        json.Should().Be(expectedJson);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"ACTIVE\"", TxState.Active)]
    [InlineData("\"FINISHED\"", TxState.Finished)]
    [InlineData("\"CANCELLED\"", TxState.Cancelled)]
    public void Deserialize_KnownValues(string payload, TxState expected)
    {
        JsonSerializerOptions options = CreateOptions();

        TxState state = JsonSerializer.Deserialize<TxState>(payload, options);

        state.Should().Be(expected);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("\"suspended\"")]
    [InlineData("\"PENDING\"")]
    [InlineData("\"" + "unexpected" + "\"")]
    public void Deserialize_UnknownValues_ThrowsJsonException(string payload)
    {
        JsonSerializerOptions options = CreateOptions();

        Action act = () => JsonSerializer.Deserialize<TxState>(payload, options);

        act.Should().Throw<JsonException>();
    }
}
