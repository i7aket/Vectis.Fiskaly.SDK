using Vectis.Fiskaly.SDK.Authentication.ValueObjects;
using Vectis.Fiskaly.SDK.Management.Common.Enums;
using Vectis.Fiskaly.SDK.SignDE.Common;
using Vectis.Fiskaly.SDK.SignDE.Common.Enums;

namespace Vectis.Fiskaly.SDK.Management.Organizations.Models;

public class ListOrganizationsQueryParameters : ListQueryParametersBase
{
    private int? _limit;

    public new int? Limit
    {
        get => _limit;
        set
        {
            if (value is null)
            {
                _limit = null;
                return;
            }

            if (value < 1 || value > 5000)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Limit must be between 1 and 5000 for Management API.");
            }

            _limit = value;
        }
    }

    public OrganizationSortField? OrderBy { get; set; }

    public SortDirection? Order { get; set; }

    public Env? Env { get; set; }

    public OrganizationType? Type { get; set; }

    public OrganizationId? ManagedByOrganizationId { get; set; }

    public override IEnumerable<KeyValuePair<string, string?>> ToQueryParameters()
    {
        List<KeyValuePair<string, string?>> parameters = new List<KeyValuePair<string, string?>>();

        if (OrderBy.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("order_by",
                EnumApiValueProvider.GetApiName(OrderBy.Value)));
        }

        if (Order.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("order",
                EnumApiValueProvider.GetApiName(Order.Value)));
        }

        if (Limit.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("limit", Limit.Value.ToString()));
        }

        if (base.Offset.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("offset", base.Offset.Value.ToString()));
        }

        if (Env.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("env",
                EnumApiValueProvider.GetApiName(Env.Value)));
        }

        if (Type.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("type",
                EnumApiValueProvider.GetApiName(Type.Value)));
        }

        if (ManagedByOrganizationId.HasValue)
        {
            parameters.Add(new KeyValuePair<string, string?>("managed_by_organization_id",
                ManagedByOrganizationId.Value.ToString()));
        }

        return parameters;
    }
}
