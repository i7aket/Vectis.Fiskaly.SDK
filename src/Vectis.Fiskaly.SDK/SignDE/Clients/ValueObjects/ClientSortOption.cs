using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;

public readonly record struct ClientSortOption
{
    private readonly SortOption<ClientSortField> _inner;

    public ClientSortOption(ClientSortField field, SortDirection direction)
    {
        _inner = new SortOption<ClientSortField>(field, direction, "client sort field");
    }

    public ClientSortField Field => _inner.Field;

    public SortDirection Direction => _inner.Direction;

    public static ClientSortOption By(ClientSortField field, SortDirection direction = SortDirection.Ascending) =>
        new(field, direction);

    public override string ToString() => _inner.ToString();
}
