using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

[JsonConverter(typeof(StandardV1SchemaJsonConverter))]
public sealed class StandardV1Schema
{
    private readonly StandardV1SchemaPayload _payload;

    private StandardV1Schema(StandardV1SchemaPayload payload)
    {
        _payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    internal StandardV1SchemaPayload Payload => _payload;

    public static StandardV1Schema ForReceipt(Receipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new StandardV1Schema(receipt);
    }

    public static StandardV1Schema ForOrder(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return new StandardV1Schema(order);
    }

    public static StandardV1Schema ForOther(Other other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new StandardV1Schema(other);
    }

    public Receipt? Receipt => _payload as Receipt;

    public Order? Order => _payload as Order;

    public Other? Other => _payload as Other;

    public bool TryGetReceipt([NotNullWhen(true)] out Receipt? receipt)
    {
        if (_payload is Receipt r)
        {
            receipt = r;
            return true;
        }

        receipt = null;
        return false;
    }

    public bool TryGetOrder([NotNullWhen(true)] out Order? order)
    {
        if (_payload is Order o)
        {
            order = o;
            return true;
        }

        order = null;
        return false;
    }

    public bool TryGetOther([NotNullWhen(true)] out Other? other)
    {
        if (_payload is Other o)
        {
            other = o;
            return true;
        }

        other = null;
        return false;
    }
}
