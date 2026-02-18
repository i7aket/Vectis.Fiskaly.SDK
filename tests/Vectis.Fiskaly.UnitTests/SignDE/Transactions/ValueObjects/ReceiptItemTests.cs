using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.ValueObjects;

public class ReceiptItemTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_CreatesReceiptItemWithAmountAndVatRate()
    {
        MoneyAmount amount = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        VatRate vatRate = VatRate.Normal;

        ReceiptItem item = new ReceiptItem(amount, vatRate);

        Assert.Equal(amount, item.Amount);
        Assert.Equal(vatRate, item.VatRate);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Constructor_WithDifferentVatRates_CreatesDistinctItems()
    {
        MoneyAmount amount = MoneyAmount.Create(10.00m, CurrencyCode.EUR);

        ReceiptItem normalItem = new ReceiptItem(amount, VatRate.Normal);
        ReceiptItem reducedItem = new ReceiptItem(amount, VatRate.Reduced1);

        Assert.Equal(VatRate.Normal, normalItem.VatRate);
        Assert.Equal(VatRate.Reduced1, reducedItem.VatRate);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        MoneyAmount amount = MoneyAmount.Create(17.00m, CurrencyCode.EUR);
        ReceiptItem item = new ReceiptItem(amount, VatRate.Normal);

        string result = item.ToString();

        Assert.Contains("17.00", result);
        Assert.Contains(CurrencyCode.EUR.ToIsoString(), result);
        Assert.Contains("Normal", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_SameAmountAndVatRate_AreEqual()
    {
        MoneyAmount amount = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        ReceiptItem item1 = new ReceiptItem(amount, VatRate.Normal);
        ReceiptItem item2 = new ReceiptItem(amount, VatRate.Normal);

        Assert.Equal(item1, item2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentVatRate_AreNotEqual()
    {
        MoneyAmount amount = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        ReceiptItem item1 = new ReceiptItem(amount, VatRate.Normal);
        ReceiptItem item2 = new ReceiptItem(amount, VatRate.Reduced1);

        Assert.NotEqual(item1, item2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ValueEquality_DifferentAmount_AreNotEqual()
    {
        ReceiptItem item1 = new ReceiptItem(MoneyAmount.Create(10.00m, CurrencyCode.EUR), VatRate.Normal);
        ReceiptItem item2 = new ReceiptItem(MoneyAmount.Create(20.00m, CurrencyCode.EUR), VatRate.Normal);

        Assert.NotEqual(item1, item2);
    }
}
