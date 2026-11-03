using PulseMon.Models;
using PulseMon.Monitoring;

namespace PulseMon.Services;

public sealed class MonitoringService : IDisposable
{
    private readonly CpuMonitor _cpuMonitor;
    private readonly MemoryMonitor _memoryMonitor;
    private readonly NetworkMonitor _networkMonitor;
    private readonly GpuMonitor _gpuMonitor;
    private readonly BatteryMonitor _batteryMonitor;
    private readonly HardwareSensorMonitor _hardwareSensorMonitor;
    private Exception? _lastReadError;
    private bool _disposed;

    public Exception? LastReadError => _lastReadError;

    public MonitoringService()
        : this(new CpuMonitor(), new MemoryMonitor(), new NetworkMonitor(), new GpuMonitor(), new BatteryMonitor(), new HardwareSensorMonitor())
    {
    }

    public MonitoringService(
        CpuMonitor cpuMonitor,
        MemoryMonitor memoryMonitor,
        NetworkMonitor networkMonitor,
        GpuMonitor gpuMonitor,
        BatteryMonitor batteryMonitor,
        HardwareSensorMonitor hardwareSensorMonitor)
    {
        _cpuMonitor = cpuMonitor ?? throw new ArgumentNullException(nameof(cpuMonitor));
        _memoryMonitor = memoryMonitor ?? throw new ArgumentNullException(nameof(memoryMonitor));
        _networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
        _gpuMonitor = gpuMonitor ?? throw new ArgumentNullException(nameof(gpuMonitor));
        _batteryMonitor = batteryMonitor ?? throw new ArgumentNullException(nameof(batteryMonitor));
        _hardwareSensorMonitor = hardwareSensorMonitor ?? throw new ArgumentNullException(nameof(hardwareSensorMonitor));
    }

    public SystemStatus GetCurrentStatus()
    {
        var cpuUsagePercent = ReadCpuUsagePercent();
        var memory = ReadMemorySnapshot();
        var network = ReadNetworkSpeedSnapshot();
        var gpu = ReadGpuSnapshot();
        var battery = ReadBatterySnapshot();
        var sensors = ReadHardwareSensorSnapshot();

        return new SystemStatus
        {
            CpuUsagePercent = cpuUsagePercent,
            MemoryUsedGb = memory.UsedGb,
            MemoryTotalGb = memory.TotalGb,
            GpuUsagePercent = gpu.UsagePercent,
            CpuTemperatureCelsius = sensors.CpuTemperatureCelsius,
            FanRpm = sensors.FanRpm,
            GpuTemperatureCelsius = sensors.GpuTemperatureCelsius ?? gpu.TemperatureCelsius,
            DownloadMbps = network.DownloadMbps,
            UploadMbps = network.UploadMbps,
            BatteryPercent = battery.Percent,
            BatteryStatus = battery.Status,
            BatteryRemainingTime = battery.RemainingTime,
            UpdatedAt = DateTime.Now
        };
    }

    private double ReadCpuUsagePercent()
    {
        try
        {
            return _cpuMonitor.GetUsagePercent();
        }
        catch (Exception exception)
        {
            RecordReadError(exception);
            return 0;
        }
    }

    private MemorySnapshot ReadMemorySnapshot()
    {
        try
        {
            return _memoryMonitor.GetMemorySnapshot();
        }
        catch (Exception exception)
        {
            RecordReadError(exception);
            return new MemorySnapshot(0, 0);
        }
    }

    private NetworkSpeedSnapshot ReadNetworkSpeedSnapshot()
    {
        try
        {
            return _networkMonitor.GetSpeedSnapshot();
        }
        catch (Exception exception)
        {
            RecordReadError(exception);
            return new NetworkSpeedSnapshot(0, 0);
        }
    }

    private GpuSnapshot ReadGpuSnapshot()
    {
        try
        {
            return _gpuMonitor.GetGpuSnapshot();
        }
        catch (Exception exception)
        {
            RecordReadError(exception);
            return GpuSnapshot.Unavailable;
        }
    }

    private BatterySnapshot ReadBatterySnapshot()
    {
        try
        {
            return _batteryMonitor.GetBatterySnapshot();
        }
        catch (Exception exception)
        {
            RecordReadError(exception);
            return BatterySnapshot.Unavailable;
        }
    }

    private HardwareSensorSnapshot ReadHardwareSensorSnapshot()
    {
        try
        {
            return _hardwareSensorMonitor.GetSnapshot();
        }
        catch (Exception exception)
        {
            RecordReadError(exception);
            return HardwareSensorSnapshot.Unavailable;
        }
    }

    private void RecordReadError(Exception exception)
    {
        _lastReadError = exception;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gpuMonitor.Dispose();
        _hardwareSensorMonitor.Dispose();
        _disposed = true;
    }
}
