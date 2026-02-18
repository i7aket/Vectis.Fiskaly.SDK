using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

public static class PaymentTypeExtensions
{
    public static string ToApiString(this PaymentType paymentType) =>
        EnumApiValueProvider.GetApiName(paymentType);
}
