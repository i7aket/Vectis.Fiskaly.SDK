using Vectis.Fiskaly.SDK.SignDE.Tss.Enums;
using Vectis.Fiskaly.SDK.SignDE.Tss.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.SignDE.Tss.Models;

public class ListTssQueryParameters : IQueryParameterProvider
{

    public TssSortOption? Sort { get; set; }

    public IReadOnlyCollection<TssState>? States { get; set; }

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
    private int? _limit;

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
    private int? _offset;

    public bool? ShowDeleted { get; set; } = true;

    public IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        List<KeyValuePair<string, string?>> parameters = new List<KeyValuePair<string, string?>>();

        if (Sort is { } sortOption)
        {
            parameters.Add(new KeyValuePair<string, string?>("order_by", EnumApiValueProvider.GetApiName(sortOption.Field)));
            parameters.Add(new KeyValuePair<string, string?>("order", EnumApiValueProvider.GetApiName(sortOption.Direction)));
        }

        if (States is { Count: > 0 } states)
        {
            int index = 0;
            foreach (TssState state in states)
            {
                string apiValue = EnumApiValueProvider.GetApiName(state);
                parameters.Add(new KeyValuePair<string, string?>($"states[{index++}]", apiValue));
            }
        }

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

        return parameters;
    }

    internal IEnumerable<KeyValuePair<string, string?>> ToKeyValuePairs() => ToQueryParameters();
}
