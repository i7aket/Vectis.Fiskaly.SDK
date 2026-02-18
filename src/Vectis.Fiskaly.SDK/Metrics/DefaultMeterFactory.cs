using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Vectis.Fiskaly.SDK.Metrics;

internal sealed class DefaultMeterFactory : IMeterFactory
{
    private readonly ConcurrentDictionary<MeterKey, Meter> _meters = new();
    private bool _disposed;

    public Meter Create(MeterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(DefaultMeterFactory));
        }

        MeterKey key = new MeterKey(options.Name, options.Version);

        return _meters.GetOrAdd(key, _ => new Meter(options.Name, options.Version, tags: null, scope: this));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (Meter meter in _meters.Values)
        {
            meter.Dispose();
        }

        _meters.Clear();
    }

    private readonly record struct MeterKey(string? Name, string? Version);
}
