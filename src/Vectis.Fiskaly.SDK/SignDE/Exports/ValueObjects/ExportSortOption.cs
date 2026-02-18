using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Exports.Enums;

namespace Vectis.Fiskaly.SDK.SignDE.Exports.ValueObjects;

public readonly record struct ExportSortOption
{
    private readonly SortOption<ExportSortField> _inner;

    public ExportSortOption(ExportSortField field, SortDirection direction)
    {
        _inner = new SortOption<ExportSortField>(field, direction, "export sort field");
    }

    public ExportSortField Field => _inner.Field;

    public SortDirection Direction => _inner.Direction;

    public static ExportSortOption By(ExportSortField field, SortDirection direction = SortDirection.Ascending) =>
        new(field, direction);

    public override string ToString() => _inner.ToString();
}
