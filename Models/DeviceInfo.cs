namespace PulseMon.Models;

public sealed class DeviceInfo
{
    public string ModelName { get; init; } = "Unknown device";

    public string CpuName { get; init; } = "Unknown CPU";

    public string GpuName { get; init; } = "Unknown GPU";
}
