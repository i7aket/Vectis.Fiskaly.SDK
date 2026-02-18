using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Aggregates;

public sealed class Receipt
{
    private readonly List<ReceiptItem> _items;
    private readonly List<Payment> _payments;

    public ReceiptType Type { get; }

    public IReadOnlyList<ReceiptItem> Items => _items.AsReadOnly();

    public IReadOnlyList<Payment> Payments => _payments.AsReadOnly();

    public MoneyAmount TotalItemAmount { get; }

    public MoneyAmount TotalPaymentAmount { get; }

    private Receipt(
        ReceiptType type,
        IEnumerable<ReceiptItem> items,
        IEnumerable<Payment> payments)
    {
        _items = items.ToList();
        _payments = payments.ToList();

        ValidateInvariants();

        Type = type;

        TotalItemAmount = CalculateTotalItemAmount();
        TotalPaymentAmount = CalculateTotalPaymentAmount();

        ValidateAmountsMatch();
    }

    public static Receipt CreateSale(
        IEnumerable<ReceiptItem> items,
        IEnumerable<Payment> payments)
    {
        return new Receipt(ReceiptType.Receipt, items, payments);
    }

    public static Receipt CreateTraining(
        IEnumerable<ReceiptItem> items,
        IEnumerable<Payment> payments)
    {
        return new Receipt(ReceiptType.Training, items, payments);
    }

    public static Receipt CreateCancellation(
        IEnumerable<ReceiptItem> items,
        IEnumerable<Payment> payments)
    {
        return new Receipt(ReceiptType.Cancellation, items, payments);
    }

    public static Receipt CreateStorno(
        IEnumerable<ReceiptItem> items,
        IEnumerable<Payment> payments)
    {
        List<ReceiptItem> itemList = items.ToList();
        List<Payment> paymentList = payments.ToList();

        if (itemList.Any(i => i.Amount.Value >= 0))
        {
            throw new ArgumentException(
                "All item amounts must be negative for ANNULATION receipts. Use MoneyAmount.ForStorno().",
                nameof(items));
        }

        if (paymentList.Any(p => p.Amount.Value >= 0))
        {
            throw new ArgumentException(
                "All payment amounts must be negative for ANNULATION receipts. Use MoneyAmount.ForStorno().",
                nameof(payments));
        }

        return new Receipt(ReceiptType.Annulation, itemList, paymentList);
    }

    private void ValidateInvariants()
    {
        if (_items.Count == 0)
        {
            throw new ArgumentException("Receipt must have at least one item.", nameof(_items));
        }

        if (_payments.Count == 0)
        {
            throw new ArgumentException("Receipt must have at least one payment.", nameof(_payments));
        }

        CurrencyCode currency = _items[0].Amount.Currency;
        if (_items.Any(i => i.Amount.Currency != currency))
        {
            throw new ArgumentException(
                $"All items must use the same currency. Expected '{currency.ToIsoString()}'.",
                nameof(_items));
        }

        if (_payments.Any(p => p.Amount.Currency != currency))
        {
            throw new ArgumentException(
                $"All payments must use the same currency as items. Expected '{currency.ToIsoString()}'.",
                nameof(_payments));
        }
    }

    private MoneyAmount CalculateTotalItemAmount()
    {
        MoneyAmount total = _items[0].Amount;
        for (int i = 1; i < _items.Count; i++)
        {
            total += _items[i].Amount;
        }
        return total;
    }

    private MoneyAmount CalculateTotalPaymentAmount()
    {
        MoneyAmount total = _payments[0].Amount;
        for (int i = 1; i < _payments.Count; i++)
        {
            total += _payments[i].Amount;
        }
        return total;
    }

    private void ValidateAmountsMatch()
    {
        if (!TotalItemAmount.ApproximatelyEquals(TotalPaymentAmount, tolerance: 0.01m))
        {
            throw new ArgumentException(
                $"Total item amount ({TotalItemAmount.ToStringInvariant()} {TotalItemAmount.CurrencyIsoCode}) " +
                $"must match total payment amount ({TotalPaymentAmount.ToStringInvariant()} {TotalPaymentAmount.CurrencyIsoCode}).");
        }
    }

    public override string ToString() =>
        $"{Type}: {TotalItemAmount.ToStringInvariant()} {TotalItemAmount.CurrencyIsoCode} " +
        $"({_items.Count} items, {_payments.Count} payments)";
}
