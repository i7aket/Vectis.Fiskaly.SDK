namespace Vectis.Fiskaly.SDK.SignDE.Common;

public readonly record struct SortOption<TField> where TField : struct, Enum
{
    private readonly TField _field;
    private readonly SortDirection _direction;

    public SortOption(TField field, SortDirection direction, string fieldTypeName = "sort field")
    {
        if (!Enum.IsDefined(field))
        {
            throw new ArgumentOutOfRangeException(nameof(field), field, $"Unsupported {fieldTypeName}.");
        }

        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported sort direction.");
        }

        _field = field;
        _direction = direction;
    }

    public TField Field => _field;

    public SortDirection Direction => _direction;

    public static SortOption<TField> By(TField field, SortDirection direction = SortDirection.Ascending, string fieldTypeName = "sort field") =>
        new(field, direction, fieldTypeName);

    public override string ToString() => $"{Field} ({Direction})";
}
