using LibreHardwareMonitor.Hardware;

namespace PulseMon.Monitoring;

public sealed class HardwareSensorMonitor : IDisposable
{
    private readonly Computer? _computer;
    private bool _disposed;

    public HardwareSensorMonitor()
    {
        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true
            };

            _computer.Open();
        }
        catch
        {
            _computer = null;
        }
    }

    public HardwareSensorSnapshot GetSnapshot()
    {
        if (_disposed || _computer is null)
        {
            return HardwareSensorSnapshot.Unavailable;
        }

        var visitor = new UpdateVisitor();
        _computer.Accept(visitor);

        double? cpuTemperature = null;
        double? gpuTemperature = null;
        double? fanRpm = null;

        foreach (var hardware in visitor.Hardware)
        {
            ReadHardware(hardware, ref cpuTemperature, ref gpuTemperature, ref fanRpm);
        }

        return new HardwareSensorSnapshot(cpuTemperature, gpuTemperature, fanRpm);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _computer?.Close();
        _disposed = true;
    }

    private static void ReadHardware(
        IHardware hardware,
        ref double? cpuTemperature,
        ref double? gpuTemperature,
        ref double? fanRpm)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is not float sensorValue)
            {
                continue;
            }

            var value = (double)sensorValue;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                continue;
            }

            if (sensor.SensorType == SensorType.Temperature)
            {
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    cpuTemperature = PreferSensor(cpuTemperature, sensor, value, "package", "core average", "tctl");
                }
                else if (hardware.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
                {
                    gpuTemperature = PreferSensor(gpuTemperature, sensor, value, "core", "hot spot", "gpu");
                }
            }
            else if (sensor.SensorType == SensorType.Fan)
            {
                fanRpm = fanRpm is null ? Math.Max(0, value) : Math.Max(fanRpm.Value, value);
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            ReadHardware(subHardware, ref cpuTemperature, ref gpuTemperature, ref fanRpm);
        }
    }

    private static double PreferSensor(double? current, ISensor sensor, double value, params string[] preferredNames)
    {
        if (current is null)
        {
            return value;
        }

        var isPreferred = preferredNames.Any(name => sensor.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        return isPreferred ? value : current.Value;
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public List<IHardware> Hardware { get; } = [];

        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            Hardware.Add(hardware);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
