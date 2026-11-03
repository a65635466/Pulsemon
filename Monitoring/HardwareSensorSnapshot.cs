namespace PulseMon.Monitoring;

public sealed record HardwareSensorSnapshot(
    double? CpuTemperatureCelsius,
    double? GpuTemperatureCelsius,
    double? FanRpm)
{
    public static HardwareSensorSnapshot Unavailable { get; } = new(null, null, null);
}
