namespace PulseMon.Monitoring;

public sealed record GpuSnapshot(double? UsagePercent, double? TemperatureCelsius)
{
    public static GpuSnapshot Unavailable { get; } = new(null, null);
}
