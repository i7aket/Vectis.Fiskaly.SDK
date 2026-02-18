using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class FiskalyEnumExtensionsTests
{
    [Trait("Category", "Unit")]
    [Fact]
    public void ReceiptType_ToApiString_ReturnsAttributeValuesForEveryMember()
    {
        foreach (ReceiptType value in Enum.GetValues<ReceiptType>())
        {
            value.ToApiString().Should().Be(GetExpectedApiValue(value));
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void VatRate_ToApiString_ReturnsAttributeValuesForEveryMember()
    {
        foreach (VatRate value in Enum.GetValues<VatRate>())
        {
            value.ToApiString().Should().Be(GetExpectedApiValue(value));
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void PaymentType_ToApiString_ReturnsAttributeValuesForEveryMember()
    {
        foreach (PaymentType value in Enum.GetValues<PaymentType>())
        {
            value.ToApiString().Should().Be(GetExpectedApiValue(value));
        }
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToApiString_ThrowsForUnknownEnumValue()
    {
        Action act = () => ((ReceiptType)999).ToApiString();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnumApiValueProvider_RespectsEnumMemberAndFallbackToName()
    {
        EnumApiValueProvider.GetApiName(LegacyEnum.WithEnumMember).Should().Be("LEGACY");
        EnumApiValueProvider.GetApiName(LegacyEnum.WithoutAttribute).Should().Be(nameof(LegacyEnum.WithoutAttribute));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnumApiValueProvider_ThrowsForNull()
    {
        Action act = () => EnumApiValueProvider.GetApiName((Enum)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SortDirection_EnumApiValueProvider_ReturnsCorrectApiStrings()
    {
        // Verify consolidated SortDirection enum works with EnumApiValueProvider
        EnumApiValueProvider.GetApiName(SortDirection.Ascending).Should().Be("asc");
        EnumApiValueProvider.GetApiName(SortDirection.Descending).Should().Be("desc");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SortDirection_SerializesToJson_UsingJsonStringEnumConverter()
    {
        // Verify SortDirection serializes correctly with JsonStringEnumConverter
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        string ascending = JsonSerializer.Serialize(SortDirection.Ascending, options);
        string descending = JsonSerializer.Serialize(SortDirection.Descending, options);

        ascending.Should().Be("\"asc\"");
        descending.Should().Be("\"desc\"");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void SortDirection_DeserializesFromJson_UsingJsonStringEnumConverter()
    {
        // Verify SortDirection deserializes correctly
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        SortDirection ascending = JsonSerializer.Deserialize<SortDirection>("\"asc\"", options);
        SortDirection descending = JsonSerializer.Deserialize<SortDirection>("\"desc\"", options);

        ascending.Should().Be(SortDirection.Ascending);
        descending.Should().Be(SortDirection.Descending);
    }

    private static string GetExpectedApiValue(Enum value)
    {
        FieldInfo field = value.GetType().GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static)
                          ?? throw new InvalidOperationException($"Enum field metadata not found for {value}.");

        JsonStringEnumMemberNameAttribute? jsonAttribute = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
        if (!string.IsNullOrWhiteSpace(jsonAttribute?.Name))
        {
            return jsonAttribute!.Name!;
        }

        EnumMemberAttribute? enumMemberAttribute = field.GetCustomAttribute<EnumMemberAttribute>();
        if (!string.IsNullOrWhiteSpace(enumMemberAttribute?.Value))
        {
            return enumMemberAttribute!.Value!;
        }

        return field.Name;
    }

    private enum LegacyEnum
    {
        [EnumMember(Value = "LEGACY")]
        WithEnumMember,
        WithoutAttribute
    }
}
