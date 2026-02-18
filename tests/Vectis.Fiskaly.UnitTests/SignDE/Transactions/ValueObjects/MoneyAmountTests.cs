using System.Globalization;
using System.Text.RegularExpressions;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.UnitTests.SignDE.Transactions.ValueObjects;

public class MoneyAmountTests
{
    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(10.50, "10.50")]
    [InlineData(100, "100.00")]
    [InlineData(0.01, "0.01")]
    public void FromDecimal_ValidPositiveAmount_CreatesAmount(decimal value, string expected)
    {
        MoneyAmount amount = MoneyAmount.Create(value, CurrencyCode.EUR);

        Assert.Equal(expected, amount.ToStringInvariant());
        Assert.Equal(CurrencyCode.EUR, amount.Currency);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Create_NegativeAmount_AllowsValue()
    {
        MoneyAmount amount = MoneyAmount.Create(-10.50m, CurrencyCode.EUR);

        Assert.Equal("-10.50", amount.ToStringInvariant());
        Assert.True(amount.IsNegative);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureNonNegative_WithNegativeInput_Throws()
    {
        MoneyAmount amount = MoneyAmount.Create(-10.50m, CurrencyCode.EUR);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => amount.EnsureNonNegative());

        Assert.Contains("Negative amounts are only permitted", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(-10.50, "-10.50")]
    [InlineData(-100, "-100.00")]
    public void EnsureNonPositive_WithValidInput_KeepsNegativeAmount(decimal input, string expected)
    {
        MoneyAmount amount = MoneyAmount.Create(input, CurrencyCode.EUR).EnsureNonPositive();

        Assert.Equal(expected, amount.ToStringInvariant());
        Assert.True(amount.IsNegative);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureNonPositive_WithPositiveInput_Throws()
    {
        MoneyAmount amount = MoneyAmount.Create(10.50m, CurrencyCode.EUR);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => amount.EnsureNonPositive());

        Assert.Contains("Storno amount must be zero", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Zero_IsZeroAmount()
    {
        MoneyAmount zero = MoneyAmount.Zero(CurrencyCode.EUR);

        Assert.True(zero.IsZero);
        Assert.Equal(0m, zero.Value);
        Assert.Equal(CurrencyCode.EUR, zero.Currency);
    }


    [Trait("Category", "Unit")]
    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        MoneyAmount amount1 = MoneyAmount.Create(10.50m, CurrencyCode.EUR);
        MoneyAmount amount2 = MoneyAmount.Create(5.25m, CurrencyCode.EUR);

        MoneyAmount sum = amount1 + amount2;

        Assert.Equal("15.75", sum.ToStringInvariant());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        MoneyAmount amount1 = MoneyAmount.Create(10.50m, CurrencyCode.EUR);
        MoneyAmount amount2 = MoneyAmount.Create(5.25m, CurrencyCode.EUR);

        MoneyAmount diff = amount1 - amount2;

        Assert.Equal("5.25", diff.ToStringInvariant());
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Abs_NegativeAmount_ReturnsPositive()
    {
        MoneyAmount amount = MoneyAmount.Create(-10.50m, CurrencyCode.EUR);

        MoneyAmount abs = amount.Abs();

        Assert.Equal("10.50", abs.ToStringInvariant());
        Assert.False(abs.IsNegative);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Negate_PositiveAmount_ReturnsNegative()
    {
        MoneyAmount amount = MoneyAmount.Create(10.50m, CurrencyCode.EUR);

        MoneyAmount negated = amount.Negate();

        Assert.Equal("-10.50", negated.ToStringInvariant());
        Assert.True(negated.IsNegative);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void Negate_ZeroAmount_ReturnsZero()
    {
        MoneyAmount zero = MoneyAmount.Zero(CurrencyCode.EUR);

        MoneyAmount negated = zero.Negate();

        Assert.True(negated.IsZero);
        Assert.Equal(zero, negated);
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ApproximatelyEquals_WithinTolerance_ReturnsTrue()
    {
        MoneyAmount amount1 = MoneyAmount.Create(10.50m, CurrencyCode.EUR);
        MoneyAmount amount2 = MoneyAmount.Create(10.51m, CurrencyCode.EUR);

        Assert.True(amount1.ApproximatelyEquals(amount2, tolerance: 0.02m));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void ApproximatelyEquals_OutsideTolerance_ReturnsFalse()
    {
        MoneyAmount amount1 = MoneyAmount.Create(10.50m, CurrencyCode.EUR);
        MoneyAmount amount2 = MoneyAmount.Create(10.60m, CurrencyCode.EUR);

        Assert.False(amount1.ApproximatelyEquals(amount2, tolerance: 0.05m));
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void EnsureSameCurrency_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        MoneyAmount eur = MoneyAmount.Create(10m, CurrencyCode.EUR);
        MoneyAmount usd = MoneyAmount.Create(10m, CurrencyCode.USD);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            eur.EnsureSameCurrency(usd));

        Assert.Contains("Currency mismatch", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ParseIsoString_InvalidCurrency_ThrowsArgumentException(string currency)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CurrencyCodeExtensions.ParseIsoString(currency));

        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("EU1")]
    public void ParseIsoString_UnsupportedCurrency_ThrowsArgumentException(string currency)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CurrencyCodeExtensions.ParseIsoString(currency));

        Assert.Contains("Unsupported currency code", exception.Message);
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(10.556789, "10.55679")]  // Rounds >5 decimals to 5
    [InlineData(10.554444, "10.55444")]  // Rounds >5 decimals to 5
    [InlineData(10.555555, "10.55556")]  // Rounds >5 decimals to 5 (banker's rounding)
    public void FromDecimal_MoreThan5Decimals_RoundsTo5Decimals(decimal input, string expected)
    {
        MoneyAmount amount = MoneyAmount.Create(input, CurrencyCode.EUR);

        Assert.Equal(expected, amount.ToStringInvariant());
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData(100, "100.00")]          // 0 decimals → formatted as 2
    [InlineData(100.5, "100.50")]        // 1 decimal → formatted as 2
    [InlineData(100.12, "100.12")]       // 2 decimals preserved
    [InlineData(100.123, "100.123")]     // 3 decimals preserved
    [InlineData(100.1234, "100.1234")]   // 4 decimals preserved
    [InlineData(100.12345, "100.12345")] // 5 decimals preserved
    public void FromDecimal_0To5Decimals_PreservesOrFormatsCorrectly(decimal input, string expected)
    {
        MoneyAmount amount = MoneyAmount.Create(input, CurrencyCode.EUR);

        Assert.Equal(expected, amount.ToStringInvariant());
    }

    [Trait("Category", "Unit")]
    [Theory]
    [InlineData("100.00")]        // 2 decimals
    [InlineData("100.123")]       // 3 decimals
    [InlineData("100.1234")]      // 4 decimals
    [InlineData("100.12345")]     // 5 decimals
    [InlineData("-50.123")]       // negative with 3 decimals
    [InlineData("0.12345")]       // small value with 5 decimals
    public void ToApiString_VariousDecimals_MatchesOpenAPIPattern(string input)
    {
        // OpenAPI pattern: ^-?\d+(\.\d{2,5})$
        Regex pattern = new System.Text.RegularExpressions.Regex(@"^-?\d+(\.\d{2,5})$");

        MoneyAmount amount = MoneyAmount.Create(decimal.Parse(input, CultureInfo.InvariantCulture), CurrencyCode.EUR);
        string apiString = amount.ToApiString();

        Assert.Matches(pattern, apiString);
    }

}
