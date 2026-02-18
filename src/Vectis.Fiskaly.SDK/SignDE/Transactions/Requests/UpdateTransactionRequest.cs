using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.Common;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;
using Vectis.Fiskaly.SDK.Guards;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;

public sealed class UpdateTransactionRequest : TxRequest
{
    [SetsRequiredMembers]
    private UpdateTransactionRequest(
        ClientId clientId,
        TransactionSchema schema,
        MetadataCollection? metadata)
    {
        ClientId = clientId;
        State = TxState.Active;
        Schema = schema;
        Metadata = metadata;
    }

    [SetsRequiredMembers]
    public UpdateTransactionRequest()
    {
        ClientId = default;
        State = TxState.Active;
    }
    [JsonPropertyName("schema")]
    public TransactionSchema? Schema { get; init; }

    #region Receipt Factories

    public static UpdateTransactionRequest CreateReceipt(
        ClientId clientId,
        Receipt receipt,
        MetadataCollection? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        Guard.Against.NegativeAmountsInNormalReceipt(receipt, nameof(receipt), "UpdateTransactionRequest.CreateStornoReceipt");

        TransactionSchema schema = TransactionSchema.ForReceipt(receipt);

        return new UpdateTransactionRequest(clientId, schema, metadata);
    }

    public static UpdateTransactionRequest CreateStornoReceipt(
        ClientId clientId,
        Receipt receipt,
        MetadataCollection? metadata = null)
    {
        ThrowIf.Default(clientId);
        ArgumentNullException.ThrowIfNull(receipt);

        Guard.Against.PositiveAmountsInStornoReceipt(receipt, nameof(receipt), "UpdateTransactionRequest.CreateReceipt");

        TransactionSchema schema = TransactionSchema.ForReceipt(receipt);

        return new UpdateTransactionRequest(clientId, schema, metadata);
    }

    #endregion

    #region Order Factories

    public static UpdateTransactionRequest CreateOrder(
        ClientId clientId,
        Order order,
        MetadataCollection? metadata = null)
    {
        ThrowIf.Default(clientId);
        ArgumentNullException.ThrowIfNull(order);
        Guard.Against.NegativeQuantitiesInNormalOrder(order, nameof(order), "UpdateTransactionRequest.CreateStornoOrder");

        TransactionSchema schema = TransactionSchema.ForOrder(order);

        return new UpdateTransactionRequest(clientId, schema, metadata);
    }

    public static UpdateTransactionRequest CreateStornoOrder(
        ClientId clientId,
        Order order,
        MetadataCollection? metadata = null)
    {
        ThrowIf.Default(clientId);
        ArgumentNullException.ThrowIfNull(order);

        Guard.Against.PositiveQuantitiesInStornoOrder(order, nameof(order), "UpdateTransactionRequest.CreateOrder");

        TransactionSchema schema = TransactionSchema.ForOrder(order);

        return new UpdateTransactionRequest(clientId, schema, metadata);
    }

    #endregion

    #region Other Factory

    public static UpdateTransactionRequest CreateOther(
        ClientId clientId,
        Other other,
        MetadataCollection? metadata = null)
    {
        ThrowIf.Default(clientId);
        ArgumentNullException.ThrowIfNull(other);

        TransactionSchema schema = TransactionSchema.ForOther(other);

        return new UpdateTransactionRequest(clientId, schema, metadata);
    }

    #endregion
}
