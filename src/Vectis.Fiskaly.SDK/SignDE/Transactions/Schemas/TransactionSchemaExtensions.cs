namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

public static class TransactionSchemaExtensions
{
    public static Receipt? GetReceipt(this TransactionSchema? schema)
        => schema is not null && schema.TryGetReceipt(out Receipt? receipt)
            ? receipt
            : null;

    public static Order? GetOrder(this TransactionSchema? schema)
        => schema is not null && schema.TryGetOrder(out Order? order)
            ? order
            : null;

    public static Other? GetOther(this TransactionSchema? schema)
        => schema is not null && schema.TryGetOther(out Other? other)
            ? other
            : null;
}
