namespace PulseMon.Monitoring;

public sealed class MemoryMonitor
{
    private const double BytesPerGb = 1024d * 1024d * 1024d;

    public MemorySnapshot GetMemorySnapshot()
    {
        var memoryStatus = new NativeMethods.MemoryStatusEx();

        if (!NativeMethods.GlobalMemoryStatusEx(ref memoryStatus))
        {
            throw new InvalidOperationException("Failed to read memory status.");
        }

        var totalGb = memoryStatus.TotalPhys / BytesPerGb;
        var availableGb = memoryStatus.AvailPhys / BytesPerGb;
        var usedGb = Math.Clamp(totalGb - availableGb, 0, totalGb);

        return new MemorySnapshot(usedGb, totalGb);
    }
}
