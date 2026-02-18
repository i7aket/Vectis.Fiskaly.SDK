using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

public sealed class TransactionSchema
{
    [JsonPropertyName("standard_v1")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StandardV1Schema? StandardV1 { get; init; }

    public static TransactionSchema For(StandardV1Schema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return new TransactionSchema { StandardV1 = schema };
    }

    public static TransactionSchema ForReceipt(Receipt receipt)
    {
        return For(StandardV1Schema.ForReceipt(receipt));
    }

    public static TransactionSchema ForOrder(Order order)
    {
        return For(StandardV1Schema.ForOrder(order));
    }

    public static TransactionSchema ForOther(Other other)
    {
        return For(StandardV1Schema.ForOther(other));
    }

    public bool TryGetReceipt([NotNullWhen(true)] out Receipt? receipt)
    {
        if (StandardV1 is not null && StandardV1.TryGetReceipt(out Receipt? result))
        {
            receipt = result;
            return true;
        }

        receipt = null;
        return false;
    }

    public bool TryGetOrder([NotNullWhen(true)] out Order? order)
    {
        if (StandardV1 is not null && StandardV1.TryGetOrder(out Order? result))
        {
            order = result;
            return true;
        }

        order = null;
        return false;
    }

    public bool TryGetOther([NotNullWhen(true)] out Other? other)
    {
        if (StandardV1 is not null && StandardV1.TryGetOther(out Other? result))
        {
            other = result;
            return true;
        }

        other = null;
        return false;
    }
}
