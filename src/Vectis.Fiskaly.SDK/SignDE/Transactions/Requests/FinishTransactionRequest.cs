using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Common;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;

public sealed class FinishTransactionRequest : TxRequest
{
    [SetsRequiredMembers]
    private FinishTransactionRequest(
        ClientId clientId,
        TransactionSchema schema,
        MetadataCollection? metadata)
    {
        ClientId = clientId;
        State = TxState.Finished;
        Schema = schema;
        Metadata = metadata;
    }

    [SetsRequiredMembers]
    public FinishTransactionRequest()
    {
        ClientId = default;
        State = TxState.Finished;
    }
    [JsonPropertyName("schema")]
    public TransactionSchema? Schema { get; init; }

    #region Receipt Factories

    public static FinishTransactionRequest CreateReceipt(
        ClientId clientId,
        Receipt receipt,
        MetadataCollection? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        Guard.Against.NegativeAmountsInNormalReceipt(receipt, nameof(receipt), "FinishTransactionRequest.CreateStornoReceipt");

        TransactionSchema schema = TransactionSchema.ForReceipt(receipt);

        return new FinishTransactionRequest(clientId, schema, metadata);
    }

    public static FinishTransactionRequest CreateStornoReceipt(
        ClientId clientId,
        Receipt receipt,
        MetadataCollection? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(receipt);

        Guard.Against.PositiveAmountsInStornoReceipt(receipt, nameof(receipt), "FinishTransactionRequest.CreateReceipt");

        TransactionSchema schema = TransactionSchema.ForReceipt(receipt);

        return new FinishTransactionRequest(clientId, schema, metadata);
    }

    #endregion

    #region Order Factories

    public static FinishTransactionRequest CreateOrder(
        ClientId clientId,
        Order order,
        MetadataCollection? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        Guard.Against.NegativeQuantitiesInNormalOrder(order, nameof(order), "FinishTransactionRequest.CreateStornoOrder");

        TransactionSchema schema = TransactionSchema.ForOrder(order);

        return new FinishTransactionRequest(clientId, schema, metadata);
    }

    public static FinishTransactionRequest CreateStornoOrder(
        ClientId clientId,
        Order order,
        MetadataCollection? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        Guard.Against.PositiveQuantitiesInStornoOrder(order, nameof(order), "FinishTransactionRequest.CreateOrder");

        TransactionSchema schema = TransactionSchema.ForOrder(order);

        return new FinishTransactionRequest(clientId, schema, metadata);
    }

    #endregion

    #region Other Factory

    public static FinishTransactionRequest CreateOther(
        ClientId clientId,
        Other other,
        MetadataCollection? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(other);

        TransactionSchema schema = TransactionSchema.ForOther(other);

        return new FinishTransactionRequest(clientId, schema, metadata);
    }

    #endregion
}
