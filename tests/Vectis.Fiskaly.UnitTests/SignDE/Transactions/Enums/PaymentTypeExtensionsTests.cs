using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Enums;

public class PaymentTypeExtensionsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Cash_ReturnsCASH()
    {
        PaymentType paymentType = PaymentType.Cash;

        string result = paymentType.ToApiString();

        Assert.Equal("CASH", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_NonCash_ReturnsNON_CASH()
    {
        PaymentType paymentType = PaymentType.NonCash;

        string result = paymentType.ToApiString();

        Assert.Equal("NON_CASH", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_AllEnumValues_ReturnNonEmptyStrings()
    {
        foreach (PaymentType paymentType in Enum.GetValues<PaymentType>())
        {
            string result = paymentType.ToApiString();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}
