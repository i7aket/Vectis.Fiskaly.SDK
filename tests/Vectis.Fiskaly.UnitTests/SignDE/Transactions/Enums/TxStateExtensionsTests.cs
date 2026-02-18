using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Enums;

public class TransactionStateExtensionsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Active_ReturnsACTIVE()
    {
        TxState state = TxState.Active;

        string result = state.ToApiString();

        Assert.Equal("ACTIVE", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Finished_ReturnsFINISHED()
    {
        TxState state = TxState.Finished;

        string result = state.ToApiString();

        Assert.Equal("FINISHED", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Cancelled_ReturnsCANCELLED()
    {
        TxState state = TxState.Cancelled;

        string result = state.ToApiString();

        Assert.Equal("CANCELLED", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_AllEnumValues_ReturnNonEmptyStrings()
    {
        foreach (TxState state in Enum.GetValues<TxState>())
        {
            string result = state.ToApiString();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}
