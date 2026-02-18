using Microsoft.AspNetCore.WebUtilities;

namespace Vectis.Fiskaly.SDK.SignDE.Common;

public static class QueryParameterExtensions
{
    public static string BuildUrl(this IQueryParameterProvider provider, string basePath)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(basePath);

        List<KeyValuePair<string, string?>> parameters = provider.ToQueryParameters().ToList();

        if (parameters.Count == 0)
        {
            return basePath;
        }

        return QueryHelpers.AddQueryString(basePath, parameters);
    }
}
