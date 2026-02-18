using System.Text.Json;
using AwesomeAssertions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.Fiskaly;

public class MoneyAmountTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters =
        {
            new MoneyAmountJsonConverter()
        }
    };

    [Trait("Category", "Unit")]
    [Fact]
    public void FromDecimal_PositiveValue_ShouldCreateAmount()
    {
        MoneyAmount money = MoneyAmount.Create(123.45m, CurrencyCode.EUR);

        money.Value.Should().Be(123.45m);
        money.Currency.Should().Be(CurrencyCode.EUR);
        money.IsNegative.Should().BeFalse();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureNonPositive_WithPositiveValue_ShouldThrow()
    {
        MoneyAmount amount = MoneyAmount.Create(99.99m, CurrencyCode.EUR);

        Action act = () => amount.EnsureNonPositive();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Storno amount must be zero or negative after normalization.*");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureNonPositive_WithNegativeValue_ShouldReturnAmount()
    {
        MoneyAmount storno = MoneyAmount.Create(-99.99m, CurrencyCode.EUR).EnsureNonPositive();

        storno.Value.Should().Be(-99.99m);
        storno.IsNegative.Should().BeTrue();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ApproximatelyEquals_WithTolerance_ShouldReturnTrue()
    {
        MoneyAmount left = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        MoneyAmount right = MoneyAmount.Create(10.01m, CurrencyCode.EUR);

        left.ApproximatelyEquals(right, 0.01m).Should().BeTrue();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void JsonConverter_ShouldRoundTrip()
    {
        MoneyAmount original = MoneyAmount.Create(-42.10m, CurrencyCode.EUR).EnsureNonPositive();

        string json = JsonSerializer.Serialize(original, JsonOptions);
        json.Should().Be("\"-42.10\"");

        MoneyAmount deserialized = JsonSerializer.Deserialize<MoneyAmount>(json, JsonOptions);
        deserialized.Should().Be(original);
    }

    // ========== Multi-Currency Tests ==========

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(CurrencyCode.USD)]
    [InlineData(CurrencyCode.GBP)]
    [InlineData(CurrencyCode.CHF)]
    [InlineData(CurrencyCode.NOK)]
    [InlineData(CurrencyCode.PLN)]
    public void FromDecimal_WithDifferentCurrencies_ShouldCreateAmount(CurrencyCode currency)
    {
        MoneyAmount money = MoneyAmount.Create(10.00m, currency);

        money.Value.Should().Be(10.00m);
        money.Currency.Should().Be(currency);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_WithSameCurrency_ShouldReturnSum()
    {
        MoneyAmount money1 = MoneyAmount.Create(10.50m, CurrencyCode.EUR);
        MoneyAmount money2 = MoneyAmount.Create(5.25m, CurrencyCode.EUR);

        MoneyAmount result = money1 + money2;

        result.Value.Should().Be(15.75m);
        result.Currency.Should().Be(CurrencyCode.EUR);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Add_WithDifferentCurrencies_ShouldThrowException()
    {
        MoneyAmount eurMoney = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        MoneyAmount usdMoney = MoneyAmount.Create(10.00m, CurrencyCode.USD);

        Func<MoneyAmount> act = () => eurMoney + usdMoney;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EUR*USD*");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Subtract_WithDifferentCurrencies_ShouldThrowException()
    {
        MoneyAmount eurMoney = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        MoneyAmount usdMoney = MoneyAmount.Create(5.00m, CurrencyCode.USD);

        Func<MoneyAmount> act = () => eurMoney - usdMoney;

        act.Should().Throw<InvalidOperationException>();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Zero_ShouldReturnEURZero()
    {
        MoneyAmount zero = MoneyAmount.Zero(CurrencyCode.EUR);

        zero.Value.Should().Be(0m);
        zero.Currency.Should().Be(CurrencyCode.EUR);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void StructuralEquality_WithSameValueAndCurrency_ShouldBeEqual()
    {
        MoneyAmount money1 = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        MoneyAmount money2 = MoneyAmount.Create(10.00m, CurrencyCode.EUR);

        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void StructuralEquality_WithDifferentCurrencies_ShouldNotBeEqual()
    {
        MoneyAmount eurMoney = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        MoneyAmount usdMoney = MoneyAmount.Create(10.00m, CurrencyCode.USD);

        eurMoney.Should().NotBe(usdMoney);
        (eurMoney == usdMoney).Should().BeFalse();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        MoneyAmount money1 = MoneyAmount.Create(10.00m, CurrencyCode.EUR);
        MoneyAmount money2 = MoneyAmount.Create(10.00m, CurrencyCode.EUR);

        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ToString_ShouldIncludeCurrency()
    {
        MoneyAmount money = MoneyAmount.Create(10.50m, CurrencyCode.USD);

        string result = money.ToString();

        result.Should().Contain(CurrencyCode.USD.ToIsoString());
        result.Should().Contain("10");
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void CurrencyNormalization_LowercaseInput_ShouldConvertToUppercase()
    {
        MoneyAmount money = MoneyAmount.Create(10.00m, CurrencyCode.USD);

        money.Currency.Should().Be(CurrencyCode.USD);
    }
}
