using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

public static class ReceiptTypeExtensions
{
    public static string ToApiString(this ReceiptType receiptType) =>
        EnumApiValueProvider.GetApiName(receiptType);
}
