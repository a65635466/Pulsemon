namespace PulseMon.Monitoring;

public sealed record BatterySnapshot(
    double? Percent,
    string Status,
    TimeSpan? RemainingTime)
{
    public static BatterySnapshot Unavailable { get; } = new(null, "N/A", null);
}
