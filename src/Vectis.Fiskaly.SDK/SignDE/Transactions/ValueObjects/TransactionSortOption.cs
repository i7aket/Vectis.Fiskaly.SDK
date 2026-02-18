using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Transactions.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Transactions.ValueObjects;

public readonly record struct TransactionSortOption
{
    private readonly SortOption<TransactionSortField> _inner;

    public TransactionSortOption(TransactionSortField field, SortDirection direction)
    {
        _inner = new SortOption<TransactionSortField>(field, direction, "transaction sort field");
    }

    public TransactionSortField Field => _inner.Field;

    public SortDirection Direction => _inner.Direction;

    public static TransactionSortOption By(TransactionSortField field, SortDirection direction = SortDirection.Ascending) =>
        new(field, direction);

    public (string OrderBy, string Order) ToQueryPair() =>
        (Field.ToApiString(), EnumApiValueProvider.GetApiName(Direction));

    public override string ToString() => _inner.ToString();
}
