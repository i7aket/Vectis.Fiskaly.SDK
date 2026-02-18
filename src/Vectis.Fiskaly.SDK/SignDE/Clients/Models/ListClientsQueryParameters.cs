using Vectis.Fiskaly.SDK.SignDE.Clients.Enums;
using Vectis.Fiskaly.SDK.SignDE.Clients.ValueObjects;
using Vectis.Fiskaly.SDK.SignDE.Common;

namespace Vectis.Fiskaly.SDK.SignDE.Clients.Models;

public class ListClientsQueryParameters : ListQueryParametersBase
{
    public ListClientsQueryParameters()
    {
        ShowDeleted = true;
    }

    public ClientSortOption? Sort { get; set; }

    public ClientSerialNumber? SerialNumber { get; set; }

    public ClientState? State { get; set; }

    public override IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        List<KeyValuePair<string, string?>> parameters = new List<KeyValuePair<string, string?>>();

        if (Sort is { } sortOption)
        {
            parameters.Add(new KeyValuePair<string, string?>("order_by", EnumApiValueProvider.GetApiName(sortOption.Field)));
            parameters.Add(new KeyValuePair<string, string?>("order", EnumApiValueProvider.GetApiName(sortOption.Direction)));
        }

        if (SerialNumber is { } serialNumber)
        {
            parameters.Add(new KeyValuePair<string, string?>("serial_number", serialNumber.Value));
        }

        if (State.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("state", EnumApiValueProvider.GetApiName(State.Value)));
        }

        AddPaginationParameters(parameters);

        return parameters;
    }
}
