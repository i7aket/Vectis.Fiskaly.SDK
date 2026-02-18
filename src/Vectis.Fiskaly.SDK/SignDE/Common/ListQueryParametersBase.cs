namespace Vectis.Fiskaly.SDK.SignDE.Common;

public abstract class ListQueryParametersBase : IQueryParameterProvider
{
    private int? _limit;
    private int? _offset;

    public int? Limit
    {
        get => _limit;
        set
        {
            if (value is null)
            {
                _limit = null;
                return;
            }

            if (value < 1 || value > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Limit must be between 1 and 100.");
            }

            _limit = value;
        }
    }

    public int? Offset
    {
        get => _offset;
        set
        {
            if (value is null)
            {
                _offset = null;
                return;
            }

            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Offset must be zero or positive.");
            }

            _offset = value;
        }
    }

    public bool? ShowDeleted { get; set; }

    public abstract IEnumerable<KeyValuePair<string, string?>> ToQueryParameters();

    protected void AddPaginationParameters(List<KeyValuePair<string, string?>> parameters)
    {
        if (Limit.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("limit", Limit.Value.ToString()));
        }

        if (Offset.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("offset", Offset.Value.ToString()));
        }

        if (ShowDeleted.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("show_deleted", ShowDeleted.Value ? "true" : "false"));
        }
    }
}
