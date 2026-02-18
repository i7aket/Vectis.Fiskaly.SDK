using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.Enums;

public class ReceiptTypeExtensionsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Receipt_ReturnsRECEIPT()
    {
        ReceiptType receiptType = ReceiptType.Receipt;

        string result = receiptType.ToApiString();

        Assert.Equal("RECEIPT", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Training_ReturnsTRAINING()
    {
        ReceiptType receiptType = ReceiptType.Training;

        string result = receiptType.ToApiString();

        Assert.Equal("TRAINING", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Transfer_ReturnsTRANSFER()
    {
        ReceiptType receiptType = ReceiptType.Transfer;

        string result = receiptType.ToApiString();

        Assert.Equal("TRANSFER", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Order_ReturnsORDER()
    {
        ReceiptType receiptType = ReceiptType.Order;

        string result = receiptType.ToApiString();

        Assert.Equal("ORDER", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Cancellation_ReturnsCANCELLATION()
    {
        ReceiptType receiptType = ReceiptType.Cancellation;

        string result = receiptType.ToApiString();

        Assert.Equal("CANCELLATION", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Abort_ReturnsABORT()
    {
        ReceiptType receiptType = ReceiptType.Abort;

        string result = receiptType.ToApiString();

        Assert.Equal("ABORT", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_BenefitInKind_ReturnsBENEFIT_IN_KIND()
    {
        ReceiptType receiptType = ReceiptType.BenefitInKind;

        string result = receiptType.ToApiString();

        Assert.Equal("BENEFIT_IN_KIND", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Invoice_ReturnsINVOICE()
    {
        ReceiptType receiptType = ReceiptType.Invoice;

        string result = receiptType.ToApiString();

        Assert.Equal("INVOICE", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Other_ReturnsOTHER()
    {
        ReceiptType receiptType = ReceiptType.Other;

        string result = receiptType.ToApiString();

        Assert.Equal("OTHER", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_Annulation_ReturnsANNULATION()
    {
        ReceiptType receiptType = ReceiptType.Annulation;

        string result = receiptType.ToApiString();

        Assert.Equal("ANNULATION", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_AllEnumValues_ReturnNonEmptyStrings()
    {
        foreach (ReceiptType receiptType in Enum.GetValues<ReceiptType>())
        {
            string result = receiptType.ToApiString();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}
