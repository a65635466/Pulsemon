namespace PulseMon.Monitoring;

public sealed class CpuMonitor
{
    private long? _previousIdleTime;
    private long? _previousTotalTime;

    public double GetUsagePercent()
    {
        if (!NativeMethods.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            throw new InvalidOperationException("Failed to read CPU system times.");
        }

        var idle = ToLong(idleTime);
        var kernel = ToLong(kernelTime);
        var user = ToLong(userTime);
        var total = kernel + user;

        if (_previousIdleTime is null || _previousTotalTime is null)
        {
            _previousIdleTime = idle;
            _previousTotalTime = total;
            return 0;
        }

        var idleDelta = idle - _previousIdleTime.Value;
        var totalDelta = total - _previousTotalTime.Value;

        _previousIdleTime = idle;
        _previousTotalTime = total;

        if (totalDelta <= 0)
        {
            return 0;
        }

        var usage = 100.0 * (1.0 - ((double)idleDelta / totalDelta));
        return Math.Clamp(usage, 0, 100);
    }

    private static long ToLong(NativeMethods.FileTime fileTime)
    {
        return ((long)fileTime.HighDateTime << 32) + fileTime.LowDateTime;
    }
}
