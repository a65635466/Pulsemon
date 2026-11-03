namespace PulseMon.Monitoring;

public sealed class BatteryMonitor
{
    private const byte UnknownValue = 255;
    private const byte NoBatteryFlag = 128;

    public BatterySnapshot GetBatterySnapshot()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var powerStatus))
        {
            throw new InvalidOperationException("Failed to read battery status.");
        }

        if (powerStatus.BatteryFlag == NoBatteryFlag || powerStatus.BatteryLifePercent == UnknownValue)
        {
            return BatterySnapshot.Unavailable;
        }

        var percent = Math.Clamp((double)powerStatus.BatteryLifePercent, 0, 100);
        var status = powerStatus.ACLineStatus switch
        {
            1 => "CHARGING / AC",
            0 => "ON BATTERY",
            _ => "UNKNOWN"
        };

        TimeSpan? remainingTime = powerStatus.BatteryLifeTimeSeconds == -1
            ? null
            : TimeSpan.FromSeconds(Math.Max(0, powerStatus.BatteryLifeTimeSeconds));

        return new BatterySnapshot(percent, status, remainingTime);
    }
}
