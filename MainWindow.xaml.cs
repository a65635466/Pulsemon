using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using PulseMon.Monitoring;
using PulseMon.Models;
using PulseMon.Services;

namespace PulseMon;

public partial class MainWindow : Window
{
    private const double CpuAlertThresholdPercent = 70d;
    private const double GpuAlertThresholdPercent = 60d;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);
    public static readonly IValueConverter ProgressScaleConverter = new ProgressValueToScaleConverter();

    private readonly MonitoringService _monitoringService = new();
    private readonly DeviceInfoService _deviceInfoService = new();
    private readonly CsvLogService _csvLogService = new();
    private readonly StatusHistory _statusHistory = new();
    private readonly DispatcherTimer _refreshTimer;
    private DeviceInfo? _deviceInfo;
    private bool _isMonitoringStopped;
    private bool _cpuAlertActive;
    private bool _gpuAlertActive;

    public event Action<SystemStatus>? StatusUpdated;
    public event Action<string>? AlertRaised;
    public event Action? ExitRequested;

    public MainWindow()
    {
        InitializeComponent();
        ApplyTheme(AppTheme.Light);
        UsageChartCanvas.SizeChanged += OnUsageChartCanvasSizeChanged;

        _refreshTimer = new DispatcherTimer
        {
            Interval = RefreshInterval
        };
        _refreshTimer.Tick += OnRefreshTimerTick;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadDeviceInfo();
        RefreshStatus();
        _refreshTimer.Start();
    }

    private void LoadDeviceInfo()
    {
        try
        {
            var deviceInfo = _deviceInfoService.GetDeviceInfo();
            _deviceInfo = deviceInfo;

            DeviceModelText.Text = TrimForDisplay(deviceInfo.ModelName, 34);
            DeviceCpuText.Text = $"CPU  {TrimForDisplay(deviceInfo.CpuName, 34)}";
            DeviceGpuText.Text = $"GPU  {TrimForDisplay(deviceInfo.GpuName, 34)}";
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            DeviceModelText.Text = "Unknown device";
            DeviceCpuText.Text = "CPU  Unknown";
            DeviceGpuText.Text = "GPU  Unknown";
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        UsageChartCanvas.SizeChanged -= OnUsageChartCanvasSizeChanged;
        StopMonitoring();
    }

    private void OnUsageChartCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateUsageChart();
    }

    private void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        try
        {
            var status = _monitoringService.GetCurrentStatus();
            UpdateStatus(status);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
            UpdatedAtText.Text = "Update failed";
        }
    }

    private void UpdateStatus(SystemStatus status)
    {
        var memoryUsagePercent = status.MemoryTotalGb <= 0
            ? 0
            : Math.Clamp((status.MemoryUsedGb / status.MemoryTotalGb) * 100, 0, 100);

        CpuProgressBar.Value = status.CpuUsagePercent;
        CpuUsageText.Text = $"{status.CpuUsagePercent:0}% / {FormatTemperature(status.CpuTemperatureCelsius)}";

        MemoryProgressBar.Value = memoryUsagePercent;
        MemoryUsageText.Text = $"{status.MemoryUsedGb:0.0} / {status.MemoryTotalGb:0.#} GB";

        GpuProgressBar.Value = status.GpuUsagePercent is null
            ? 0
            : Math.Clamp(status.GpuUsagePercent.Value, 0, 100);
        GpuStatusText.Text = FormatGpuStatus(status);

        NetworkSpeedText.Text = $"D {status.DownloadMbps:0.0} Mbps  U {status.UploadMbps:0.0} Mbps";

        BatteryPercentText.Text = status.BatteryPercent is null
            ? "N/A"
            : $"{status.BatteryPercent:0}%";
        BatteryStatusText.Text = FormatBatteryStatus(status);

        SensorStatusText.Text = $"CPU {FormatTemperature(status.CpuTemperatureCelsius)} / GPU {FormatTemperature(status.GpuTemperatureCelsius)} / FAN {FormatRpm(status.FanRpm)}";

        UpdatedAtText.Text = status.UpdatedAt.ToString("HH:mm:ss");
        UpdateAlerts(status);
        _statusHistory.Add(status);
        UpdateUsageChart();
        StatusUpdated?.Invoke(status);
    }

    private void UpdateAlerts(SystemStatus status)
    {
        var cpuAlert = status.CpuUsagePercent > CpuAlertThresholdPercent;
        var gpuAlert = status.GpuUsagePercent is > GpuAlertThresholdPercent;
        var messages = new List<string>();

        if (cpuAlert)
        {
            messages.Add($"CPU high: {status.CpuUsagePercent:0}%");
        }

        if (gpuAlert)
        {
            messages.Add($"GPU high: {status.GpuUsagePercent:0}%");
        }

        AlertStatusText.Text = messages.Count == 0
            ? string.Empty
            : $"⚠ {string.Join("   ⚠ ", messages)}";
        AlertStatusText.Visibility = messages.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        var newlyRaisedMessages = new List<string>();
        if (cpuAlert && !_cpuAlertActive)
        {
            newlyRaisedMessages.Add(
                $"CPU usage exceeded {CpuAlertThresholdPercent:0}% ({status.CpuUsagePercent:0}%)\n현재 CPU 사용량이 {CpuAlertThresholdPercent:0}%를 넘겼어요!");
        }

        if (gpuAlert && !_gpuAlertActive)
        {
            newlyRaisedMessages.Add(
                $"GPU usage exceeded {GpuAlertThresholdPercent:0}% ({status.GpuUsagePercent:0}%)\n현재 GPU 사용량이 {GpuAlertThresholdPercent:0}%를 넘겼어요!");
        }

        _cpuAlertActive = cpuAlert;
        _gpuAlertActive = gpuAlert;

        if (newlyRaisedMessages.Count > 0)
        {
            AlertRaised?.Invoke(string.Join(" / ", newlyRaisedMessages));
        }
    }

    private void UpdateUsageChart()
    {
        var samples = _statusHistory.GetSamples();
        UsageChartCanvas.Children.Clear();
        HistorySampleText.Text = $"{samples.Count} samples";

        if (samples.Count < 2 || UsageChartCanvas.ActualWidth <= 0)
        {
            return;
        }

        var width = Math.Max(1, UsageChartCanvas.ActualWidth);
        var height = Math.Max(1, UsageChartCanvas.ActualHeight);
        AddChartLine(samples, width, height, sample => sample.CpuUsagePercent, "#91A58F");
        AddChartLine(samples, width, height, sample => sample.MemoryTotalGb <= 0 ? 0 : sample.MemoryUsedGb / sample.MemoryTotalGb * 100, "#A8B3C0");
        AddChartLine(samples, width, height, sample => sample.GpuUsagePercent ?? 0, "#7F8F9F");
    }

    private void AddChartLine(IReadOnlyList<SystemStatus> samples, double width, double height, Func<SystemStatus, double> selector, string color)
    {
        var polyline = new System.Windows.Shapes.Polyline
        {
            Stroke = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)),
            StrokeThickness = 1.5,
            SnapsToDevicePixels = true
        };

        for (var index = 0; index < samples.Count; index++)
        {
            var x = samples.Count == 1 ? 0 : index * width / (samples.Count - 1);
            var value = Math.Clamp(selector(samples[index]), 0, 100);
            var y = height - value / 100 * height;
            polyline.Points.Add(new System.Windows.Point(x, y));
        }

        UsageChartCanvas.Children.Add(polyline);
    }

    private void OnCopyDeviceInfoClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            _deviceInfo ??= _deviceInfoService.GetDeviceInfo();
            var info = $"Device: {_deviceInfo.ModelName}\nCPU: {_deviceInfo.CpuName}\nGPU: {_deviceInfo.GpuName}\nRAM: {FormatMemory(_statusHistory.GetSamples().LastOrDefault())}\nOS: {Environment.OSVersion.VersionString}";
            System.Windows.Clipboard.SetText(info);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    private void OnExportCsvClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export PulseMon CSV",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"pulsemon-{DateTime.Now:yyyyMMdd-HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                _csvLogService.Export(dialog.FileName, _statusHistory.GetSamples());
            }
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }

    private static string FormatMemory(SystemStatus? status)
    {
        return status is null ? "N/A" : $"{status.MemoryUsedGb:0.0} / {status.MemoryTotalGb:0.#} GB";
    }

    public void StopMonitoring()
    {
        if (_isMonitoringStopped)
        {
            return;
        }

        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
        _monitoringService.Dispose();
        _isMonitoringStopped = true;
    }

    private static string FormatGpuStatus(SystemStatus status)
    {
        if (status.GpuUsagePercent is null && status.GpuTemperatureCelsius is null)
        {
            return "N/A";
        }

        var usageText = status.GpuUsagePercent is null
            ? "N/A"
            : $"{status.GpuUsagePercent:0}%";
        var temperatureText = status.GpuTemperatureCelsius is null
            ? "N/A"
            : $"{status.GpuTemperatureCelsius:0} C";

        return $"{usageText} / {temperatureText}";
    }

    private static string FormatBatteryStatus(SystemStatus status)
    {
        if (status.BatteryPercent is null)
        {
            return "N/A";
        }

        if (status.BatteryRemainingTime is not TimeSpan remainingTime)
        {
            return status.BatteryStatus;
        }

        return $"{status.BatteryStatus} / {remainingTime.Hours:00}:{remainingTime.Minutes:00} left";
    }

    private static string FormatTemperature(double? temperatureCelsius)
    {
        return temperatureCelsius is null ? "N/A" : $"{temperatureCelsius:0} C";
    }

    private static string FormatRpm(double? rpm)
    {
        return rpm is null ? "N/A" : $"{rpm:0} RPM";
    }

    private void OnRootPanelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnHideButtonClicked(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnExitButtonClicked(object sender, RoutedEventArgs e)
    {
        ExitRequested?.Invoke();
    }

    public void ApplyTheme(AppTheme theme)
    {
        if (theme == AppTheme.Black)
        {
            SetBrush("WindowBackgroundBrush", "#25211F");
            SetBrush("PanelBackgroundBrush", "#25211F");
            SetBrush("CardBackgroundBrush", "#342E2A");
            SetBrush("BorderBrush", "#5A5048");
            SetBrush("PrimaryTextBrush", "#FFF8EF");
            SetBrush("SecondaryTextBrush", "#C8BDB2");
            SetBrush("LabelTextBrush", "#E3D7CC");
            SetBrush("MutedTrackBrush", "#51463E");
            SetBrush("AccentBrush", "#D28A62");
            SetBrush("SuccessBrush", "#8DB594");
            SetBrush("TitleBarBrush", "#171412");
            SetBrush("TitleBarTextBrush", "#FFF8EF");
            Background = System.Windows.Media.Brushes.Transparent;
            RootPanel.Effect = null;
            return;
        }

        SetBrush("WindowBackgroundBrush", "#171A1F");
        SetBrush("PanelBackgroundBrush", "#272D34");
        SetBrush("CardBackgroundBrush", "#323A43");
        SetBrush("BorderBrush", "#647789");
        SetBrush("PrimaryTextBrush", "#E7E2DA");
        SetBrush("SecondaryTextBrush", "#AAA7A1");
        SetBrush("LabelTextBrush", "#AAA7A1");
        SetBrush("MutedTrackBrush", "#1D2228");
        SetBrush("AccentBrush", "#91A58F");
        SetBrush("SuccessBrush", "#91A58F");
        SetBrush("TitleBarBrush", "#323A43");
        SetBrush("TitleBarTextBrush", "#E7E2DA");
        Background = System.Windows.Media.Brushes.Transparent;
        RootPanel.Effect = null;
    }

    private void SetBrush(string resourceKey, string hexColor)
    {
        Resources[resourceKey] = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor));
    }

    private static string TrimForDisplay(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"{value[..Math.Max(0, maxLength - 3)]}...";
    }

    private sealed class ProgressValueToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is not double progressValue)
            {
                return 0d;
            }

            return Math.Clamp(progressValue / 100d, 0d, 1d);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
