using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Receipt = Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas.Receipt;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Extensions;

public static class ReceiptExtensions
{
    public static FinishTransactionRequest ToFiskalyRequest(
        this Aggregates.Receipt receipt,
        ClientId clientId)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        List<VatRateAmount> amountsPerVatRate = receipt.Items
            .GroupBy(item => item.VatRate)
            .Select(group => new VatRateAmount
            {
                VatRate = group.Key,
                Amount = group
                    .Select(item => item.Amount)
                    .Aggregate((acc, amount) => acc + amount)
            })
            .ToList();

        List<PaymentTypeAmount> amountsPerPaymentType = receipt.Payments
            .GroupBy(payment => payment.Type)
            .Select(group => new PaymentTypeAmount
            {
                PaymentType = group.Key,
                Amount = group
                    .Select(payment => payment.Amount)
                    .Aggregate((acc, amount) => acc + amount)
            })
            .ToList();

        return FinishTransactionRequest.CreateReceipt(
            clientId,
            new Receipt
            {
                ReceiptType = receipt.Type,
                AmountsPerVatRate = amountsPerVatRate,
                AmountsPerPaymentType = amountsPerPaymentType
            });
    }
}
