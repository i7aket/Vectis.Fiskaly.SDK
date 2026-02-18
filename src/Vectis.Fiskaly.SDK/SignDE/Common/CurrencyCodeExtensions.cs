namespace Vectis.Fiskaly.SDK.SignDE.Common;

public static class CurrencyCodeExtensions
{
    private static readonly IReadOnlyDictionary<string, CurrencyCode> _lookup = new Dictionary<string, CurrencyCode>(StringComparer.OrdinalIgnoreCase)
    {
        ["CHF"] = CurrencyCode.CHF,
        ["CZK"] = CurrencyCode.CZK,
        ["DKK"] = CurrencyCode.DKK,
        ["EUR"] = CurrencyCode.EUR,
        ["GBP"] = CurrencyCode.GBP,
        ["HUF"] = CurrencyCode.HUF,
        ["NOK"] = CurrencyCode.NOK,
        ["PLN"] = CurrencyCode.PLN,
        ["SEK"] = CurrencyCode.SEK,
        ["USD"] = CurrencyCode.USD
    };

    public static string ToIsoString(this CurrencyCode code) => code switch
    {
        CurrencyCode.CHF => "CHF",
        CurrencyCode.CZK => "CZK",
        CurrencyCode.DKK => "DKK",
        CurrencyCode.EUR => "EUR",
        CurrencyCode.GBP => "GBP",
        CurrencyCode.HUF => "HUF",
        CurrencyCode.NOK => "NOK",
        CurrencyCode.PLN => "PLN",
        CurrencyCode.SEK => "SEK",
        CurrencyCode.USD => "USD",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unsupported currency code")
    };

    public static CurrencyCode ParseIsoString(string? isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            throw new ArgumentException("Currency code cannot be null or empty.", nameof(isoCode));
        }

        if (_lookup.TryGetValue(isoCode.Trim(), out CurrencyCode code))
        {
            return code;
        }

        throw new ArgumentException($"Unsupported currency code '{isoCode}'.", nameof(isoCode));
    }

    public static bool TryParseIsoString(string? isoCode, out CurrencyCode code)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            code = default;
            return false;
        }

        return _lookup.TryGetValue(isoCode.Trim(), out code);
    }
}
