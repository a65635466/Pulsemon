using System.Diagnostics;

namespace PulseMon.Monitoring;

public sealed class GpuMonitor : IDisposable
{
    private const string GpuEngineCategoryName = "GPU Engine";
    private const string UtilizationCounterName = "% Utilization";

    private IReadOnlyList<PerformanceCounter>? _utilizationCounters;
    private bool _isUnsupported;
    private bool _disposed;

    public GpuSnapshot GetGpuSnapshot()
    {
        if (_isUnsupported)
        {
            return GpuSnapshot.Unavailable;
        }

        try
        {
            var usagePercent = TryGetUsagePercent();

            return new GpuSnapshot(
                usagePercent,
                TemperatureCelsius: null);
        }
        catch (Exception) when (!_disposed)
        {
            _isUnsupported = true;
            DisposeCounters();
            return GpuSnapshot.Unavailable;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DisposeCounters();
        _disposed = true;
    }

    private double? TryGetUsagePercent()
    {
        var counters = GetUtilizationCounters();

        if (counters.Count == 0)
        {
            return null;
        }

        var usage = counters.Sum(counter => Math.Max(0, counter.NextValue()));
        return Math.Clamp(usage, 0, 100);
    }

    private IReadOnlyList<PerformanceCounter> GetUtilizationCounters()
    {
        if (_utilizationCounters is not null)
        {
            return _utilizationCounters;
        }

        if (!PerformanceCounterCategory.Exists(GpuEngineCategoryName))
        {
            _utilizationCounters = Array.Empty<PerformanceCounter>();
            return _utilizationCounters;
        }

        var category = new PerformanceCounterCategory(GpuEngineCategoryName);
        var counters = category.GetInstanceNames()
            .Where(instanceName => instanceName.Contains("engtype_", StringComparison.OrdinalIgnoreCase))
            .Select(instanceName => new PerformanceCounter(
                GpuEngineCategoryName,
                UtilizationCounterName,
                instanceName,
                readOnly: true))
            .ToArray();

        _utilizationCounters = counters;
        return _utilizationCounters;
    }

    private void DisposeCounters()
    {
        if (_utilizationCounters is null)
        {
            return;
        }

        foreach (var counter in _utilizationCounters)
        {
            counter.Dispose();
        }

        _utilizationCounters = null;
    }
}
