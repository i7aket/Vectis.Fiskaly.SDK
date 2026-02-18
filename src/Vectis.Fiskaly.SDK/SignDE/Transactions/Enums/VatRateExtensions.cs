using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

public static class VatRateExtensions
{
    public static string ToApiString(this VatRate vatRate) =>
        EnumApiValueProvider.GetApiName(vatRate);
}
