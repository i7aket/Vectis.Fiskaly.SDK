using System.Text.Json;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

namespace Vectis.Fiskaly.SDK.Common;

public static class Guard
{
    public static class Against
    {
        public static void PositiveAmountsInStornoReceipt(Receipt receipt, string parameterName, string suggestedRequestType = "UpdateTransactionRequest.Receipt")
        {
            ArgumentNullException.ThrowIfNull(receipt);

            foreach (VatRateAmount vatAmount in receipt.AmountsPerVatRate)
            {
                if (vatAmount.Amount.Value >= 0)
                {
                    throw new ArgumentException(
                        $"Negative amounts required for storno receipt. " +
                        $"Found positive/zero amount {vatAmount.Amount} for VAT rate {vatAmount.VatRate}. " +
                        $"Use {suggestedRequestType} for normal sales with positive amounts.",
                        parameterName);
                }
            }

            foreach (PaymentTypeAmount paymentAmount in receipt.AmountsPerPaymentType)
            {
                if (paymentAmount.Amount.Value >= 0)
                {
                    throw new ArgumentException(
                        $"Negative amounts required for storno receipt. " +
                        $"Found positive/zero amount {paymentAmount.Amount} for payment type {paymentAmount.PaymentType}. " +
                        $"Use {suggestedRequestType} for normal sales with positive amounts.",
                        parameterName);
                }
            }
        }

        public static void PositiveQuantitiesInStornoOrder(Order order, string parameterName, string suggestedRequestType = "UpdateTransactionRequest.Order")
        {
            ArgumentNullException.ThrowIfNull(order);

            foreach (LineItem item in order.LineItems)
            {
                if (item.Quantity >= 0)
                {
                    throw new ArgumentException(
                        $"Negative quantities required for storno order. " +
                        $"Found positive/zero quantity {item.Quantity} for item '{item.Text}'. " +
                        $"Use {suggestedRequestType} for normal orders with positive quantities.",
                        parameterName);
                }
            }
        }

        public static void NegativeAmountsInNormalReceipt(Receipt receipt, string parameterName, string suggestedRequestType = "UpdateTransactionRequest.StornoReceipt")
        {
            ArgumentNullException.ThrowIfNull(receipt);

            foreach (VatRateAmount vatAmount in receipt.AmountsPerVatRate)
            {
                if (vatAmount.Amount.Value < 0)
                {
                    throw new ArgumentException(
                        $"Positive amounts required for normal receipt. " +
                        $"Found negative amount {vatAmount.Amount} for VAT rate {vatAmount.VatRate}. " +
                        $"Use {suggestedRequestType} for returns/cancellations with negative amounts.",
                        parameterName);
                }
            }

            foreach (PaymentTypeAmount paymentAmount in receipt.AmountsPerPaymentType)
            {
                if (paymentAmount.Amount.Value < 0)
                {
                    throw new ArgumentException(
                        $"Positive amounts required for normal receipt. " +
                        $"Found negative amount {paymentAmount.Amount} for payment type {paymentAmount.PaymentType}. " +
                        $"Use {suggestedRequestType} for returns/cancellations with negative amounts.",
                        parameterName);
                }
            }
        }

        public static void NegativeQuantitiesInNormalOrder(Order order, string parameterName, string suggestedRequestType = "UpdateTransactionRequest.StornoOrder")
        {
            ArgumentNullException.ThrowIfNull(order);

            foreach (LineItem item in order.LineItems)
            {
                if (item.Quantity <= 0)
                {
                    throw new ArgumentException(
                        $"Positive quantities required for normal order. " +
                        $"Found negative/zero quantity {item.Quantity} for item '{item.Text}'. " +
                        $"Use {suggestedRequestType} for returns/cancellations with negative quantities.",
                        parameterName);
                }
            }
        }
    }

    public static class Json
    {
        public static void NotNull(object? value, string fieldName)
        {
            if (value is null)
            {
                throw new JsonException($"Required field '{fieldName}' is missing in JSON payload.");
            }
        }

        public static void NotNull<T>(T? value, string fieldName) where T : struct
        {
            if (!value.HasValue)
            {
                throw new JsonException($"Required field '{fieldName}' is missing in JSON payload.");
            }
        }

        public static void NotNullOrWhiteSpace(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException($"Required field '{fieldName}' is missing or empty in JSON payload.");
            }
        }

        public static void Equals(string? actual, string expected, string fieldName)
        {
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new JsonException(
                    $"Field '{fieldName}' expected value '{expected}' but received '{actual ?? "<null>"}'.");
            }
        }
    }
}
