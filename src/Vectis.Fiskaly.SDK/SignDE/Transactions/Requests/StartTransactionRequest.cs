using System.Diagnostics.CodeAnalysis;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;

public class StartTransactionRequest : TxRequest
{
    [SetsRequiredMembers]
    public StartTransactionRequest()
    {
        State = TxState.Active;
    }
}
