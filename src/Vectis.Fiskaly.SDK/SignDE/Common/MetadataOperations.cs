using Vectis.Fiskaly.SDK.Http;

namespace Vectis.Fiskaly.SDK.SignDE.Common;

public static class MetadataOperations
{
    public static Task<MetadataCollection> GetAsync(
        FiskalyHttpRequestExecutor executor,
        HttpClient httpClient,
        string metadataPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(httpClient);

        return executor.ExecuteGetAsync<MetadataCollection>(
            httpClient,
            metadataPath,
            cancellationToken);
    }

    public static Task<MetadataCollection> UpdateAsync(
        FiskalyHttpRequestExecutor executor,
        HttpClient httpClient,
        string metadataPath,
        MetadataCollection metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(metadata);

        return executor.ExecutePatchAsync<MetadataCollection, MetadataCollection>(
            httpClient,
            metadataPath,
            metadata,
            cancellationToken);
    }
}
