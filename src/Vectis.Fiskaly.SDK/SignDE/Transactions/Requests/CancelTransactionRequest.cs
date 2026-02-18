using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Schemas;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;

public sealed class CancelTransactionRequest : TxRequest
{
    [SetsRequiredMembers]
    public CancelTransactionRequest()
    {
        State = TxState.Cancelled;
    }
    [JsonPropertyName("schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TransactionSchema? Schema { get; init; }
}
