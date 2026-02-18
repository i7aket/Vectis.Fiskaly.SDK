using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.Requests;

public class ListTransactionsQueryParameters : ListQueryParametersBase
{
    public TransactionSortOption? Sort { get; set; }

    public TxStateFilter? StateFilter { get; set; }

    public override IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        List<KeyValuePair<string, string?>> parameters = new List<KeyValuePair<string, string?>>();

        if (Sort is { } sort)
        {
            (string orderBy, string order) = sort.ToQueryPair();
            parameters.Add(new KeyValuePair<string, string?>("order_by", orderBy));
            parameters.Add(new KeyValuePair<string, string?>("order", order));
        }

        if (StateFilter is { } stateFilter)
        {
            IReadOnlyList<string> values = stateFilter.ToApiValues();
            for (int index = 0; index < values.Count; index++)
            {
                parameters.Add(new KeyValuePair<string, string?>($"states[{index}]", values[index]));
            }
        }

        AddPaginationParameters(parameters);

        foreach (KeyValuePair<string, string?> parameter in parameters)
        {
            yield return parameter;
        }
    }
}
