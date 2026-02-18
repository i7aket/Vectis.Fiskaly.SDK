using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.UnitTests.SignDE.Common;

public class EnumApiValueProviderTests
{
    // ============================================================================
    // GetApiName with Attributes Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_TssStateInitialized_ReturnsINITIALIZED()
    {
        string result = EnumApiValueProvider.GetApiName(TssState.Initialized);

        Assert.Equal("INITIALIZED", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_TssStateCreated_ReturnsCREATED()
    {
        string result = EnumApiValueProvider.GetApiName(TssState.Created);

        Assert.Equal("CREATED", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_ClientStateRegistered_ReturnsREGISTERED()
    {
        string result = EnumApiValueProvider.GetApiName(ClientState.Registered);

        Assert.Equal("REGISTERED", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_PaymentTypeCash_ReturnsCASH()
    {
        string result = EnumApiValueProvider.GetApiName(PaymentType.Cash);

        Assert.Equal("CASH", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_TransactionStateActive_ReturnsACTIVE()
    {
        string result = EnumApiValueProvider.GetApiName(TxState.Active);

        Assert.Equal("ACTIVE", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_ReceiptTypeReceipt_ReturnsRECEIPT()
    {
        string result = EnumApiValueProvider.GetApiName(ReceiptType.Receipt);

        Assert.Equal("RECEIPT", result);
    }

    // ============================================================================
    // Error Handling Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_NullEnum_ThrowsArgumentNullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            EnumApiValueProvider.GetApiName(null!));

        Assert.Equal("value", exception.ParamName);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_UnknownEnumValue_ThrowsArgumentOutOfRangeException()
    {
        TssState unknownValue = (TssState)999;

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            EnumApiValueProvider.GetApiName(unknownValue));

        Assert.Equal("value", exception.ParamName);
        Assert.Contains("Unknown TssState value", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_InvalidClientState_ThrowsArgumentOutOfRangeException()
    {
        ClientState invalidValue = (ClientState)(-1);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            EnumApiValueProvider.GetApiName(invalidValue));

        Assert.Contains("Unknown ClientState value", exception.Message);
    }

    // ============================================================================
    // Caching Behavior Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_CalledTwice_UsesCachedMapping()
    {
        // First call should build the cache
        string result1 = EnumApiValueProvider.GetApiName(TssState.Initialized);

        // Second call should use cached mapping
        string result2 = EnumApiValueProvider.GetApiName(TssState.Initialized);

        Assert.Equal(result1, result2);
        Assert.Equal("INITIALIZED", result2);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_DifferentEnumTypes_HaveSeparateCaches()
    {
        // Call with TssState
        string tssResult = EnumApiValueProvider.GetApiName(TssState.Initialized);

        // Call with ClientState
        string clientResult = EnumApiValueProvider.GetApiName(ClientState.Registered);

        // Both should work independently
        Assert.Equal("INITIALIZED", tssResult);
        Assert.Equal("REGISTERED", clientResult);
    }

    // ============================================================================
    // Real Enum Examples from SDK
    // ============================================================================

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(TssState.Uninitialized, "UNINITIALIZED")]
    [InlineData(TssState.Created, "CREATED")]
    [InlineData(TssState.Initialized, "INITIALIZED")]
    [InlineData(TssState.Disabled, "DISABLED")]
    public void GetApiName_TssState_ReturnsCorrectApiString(TssState state, string expected)
    {
        string result = EnumApiValueProvider.GetApiName(state);

        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(ClientState.Registered, "REGISTERED")]
    [InlineData(ClientState.Deregistered, "DEREGISTERED")]
    public void GetApiName_ClientState_ReturnsCorrectApiString(ClientState state, string expected)
    {
        string result = EnumApiValueProvider.GetApiName(state);

        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(PaymentType.Cash, "CASH")]
    [InlineData(PaymentType.NonCash, "NON_CASH")]
    public void GetApiName_PaymentType_ReturnsCorrectApiString(PaymentType type, string expected)
    {
        string result = EnumApiValueProvider.GetApiName(type);

        Assert.Equal(expected, result);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(ReceiptType.Receipt, "RECEIPT")]
    [InlineData(ReceiptType.Training, "TRAINING")]
    [InlineData(ReceiptType.Transfer, "TRANSFER")]
    [InlineData(ReceiptType.Order, "ORDER")]
    [InlineData(ReceiptType.Cancellation, "CANCELLATION")]
    [InlineData(ReceiptType.Annulation, "ANNULATION")]
    public void GetApiName_ReceiptType_ReturnsCorrectApiString(ReceiptType type, string expected)
    {
        string result = EnumApiValueProvider.GetApiName(type);

        Assert.Equal(expected, result);
    }

    // ============================================================================
    // Attribute Priority Tests
    // ============================================================================

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_TestEnumWithMultipleAttributes_PrefersJsonStringEnumMemberName()
    {
        string result = EnumApiValueProvider.GetApiName(TestEnum.ValueWithBothAttributes);

        // Should use JsonStringEnumMemberNameAttribute value, not EnumMemberAttribute
        Assert.Equal("JSON_ATTRIBUTE", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_TestEnumWithEnumMemberOnly_UsesEnumMemberAttribute()
    {
        string result = EnumApiValueProvider.GetApiName(TestEnum.ValueWithEnumMemberOnly);

        Assert.Equal("ENUM_MEMBER", result);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetApiName_TestEnumWithNoAttributes_UsesFieldName()
    {
        string result = EnumApiValueProvider.GetApiName(TestEnum.ValueWithNoAttributes);

        Assert.Equal("ValueWithNoAttributes", result);
    }

    // ============================================================================
    // Test Enums for Attribute Priority
    // ============================================================================

    private enum TestEnum
    {
        [JsonStringEnumMemberName("JSON_ATTRIBUTE")]
        [EnumMember(Value = "ENUM_MEMBER_FALLBACK")]
        ValueWithBothAttributes,

        [EnumMember(Value = "ENUM_MEMBER")]
        ValueWithEnumMemberOnly,

        ValueWithNoAttributes
    }
}
