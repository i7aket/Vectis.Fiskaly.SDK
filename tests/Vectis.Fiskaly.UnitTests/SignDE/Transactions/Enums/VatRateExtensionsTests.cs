using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Enums;

public class VatRateExtensionsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Normal_ReturnsNORMAL()
    {
        VatRate vatRate = VatRate.Normal;

        string result = vatRate.ToApiString();

        Assert.Equal("NORMAL", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Reduced1_ReturnsREDUCED_1()
    {
        VatRate vatRate = VatRate.Reduced1;

        string result = vatRate.ToApiString();

        Assert.Equal("REDUCED_1", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_SpecialRate1_ReturnsSPECIAL_RATE_1()
    {
        VatRate vatRate = VatRate.SpecialRate1;

        string result = vatRate.ToApiString();

        Assert.Equal("SPECIAL_RATE_1", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_SpecialRate2_ReturnsSPECIAL_RATE_2()
    {
        VatRate vatRate = VatRate.SpecialRate2;

        string result = vatRate.ToApiString();

        Assert.Equal("SPECIAL_RATE_2", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Null_ReturnsNULL()
    {
        VatRate vatRate = VatRate.Null;

        string result = vatRate.ToApiString();

        Assert.Equal("NULL", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_AllEnumValues_ReturnNonEmptyStrings()
    {
        foreach (VatRate vatRate in Enum.GetValues<VatRate>())
        {
            string result = vatRate.ToApiString();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}
