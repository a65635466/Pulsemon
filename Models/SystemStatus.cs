namespace PulseMon.Models;

public sealed class SystemStatus
{
    public double CpuUsagePercent { get; init; }

    public double MemoryUsedGb { get; init; }

    public double MemoryTotalGb { get; init; }

    public double? GpuUsagePercent { get; init; }

    public double? GpuTemperatureCelsius { get; init; }

    public double? CpuTemperatureCelsius { get; init; }

    public double? FanRpm { get; init; }

    public double DownloadMbps { get; init; }

    public double UploadMbps { get; init; }

    public double? BatteryPercent { get; init; }

    public string BatteryStatus { get; init; } = "N/A";

    public TimeSpan? BatteryRemainingTime { get; init; }

    public DateTime UpdatedAt { get; init; } = DateTime.Now;
}
