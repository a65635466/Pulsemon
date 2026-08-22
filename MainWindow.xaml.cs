using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PulseMon.Models;
using PulseMon.Services;

namespace PulseMon;

public partial class MainWindow : Window
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);
    public static readonly IValueConverter ProgressScaleConverter = new ProgressValueToScaleConverter();

    private readonly MonitoringService _monitoringService = new();
    private readonly DeviceInfoService _deviceInfoService = new();
    private readonly DispatcherTimer _refreshTimer;
    private bool _isMonitoringStopped;

    public MainWindow()
    {
        InitializeComponent();
        ApplyTheme(AppTheme.Black);

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
        StopMonitoring();
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
        CpuUsageText.Text = $"{status.CpuUsagePercent:0}%";

        MemoryProgressBar.Value = memoryUsagePercent;
        MemoryUsageText.Text = $"{status.MemoryUsedGb:0.0} / {status.MemoryTotalGb:0.#} GB";

        GpuProgressBar.Value = status.GpuUsagePercent is null
            ? 0
            : Math.Clamp(status.GpuUsagePercent.Value, 0, 100);
        GpuStatusText.Text = FormatGpuStatus(status);

        NetworkSpeedText.Text = $"D {status.DownloadMbps:0.0} Mbps  U {status.UploadMbps:0.0} Mbps";

        UpdatedAtText.Text = status.UpdatedAt.ToString("HH:mm:ss");
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

    public void ApplyTheme(AppTheme theme)
    {
        if (theme == AppTheme.Black)
        {
            SetBrush("WindowBackgroundBrush", "#15181D");
            SetBrush("PanelBackgroundBrush", "#DF1F252D");
            SetBrush("CardBackgroundBrush", "#18FFFFFF");
            SetBrush("BorderBrush", "#24FFFFFF");
            SetBrush("PrimaryTextBrush", "#F5F7FA");
            SetBrush("SecondaryTextBrush", "#9AA4AF");
            SetBrush("LabelTextBrush", "#C9D1D9");
            SetBrush("MutedTrackBrush", "#24FFFFFF");
            SetBrush("AccentBrush", "#36D6E7");
            SetBrush("SuccessBrush", "#45E08F");
            Background = System.Windows.Media.Brushes.Transparent;
            RootPanel.Effect = null;
            return;
        }

        SetBrush("WindowBackgroundBrush", "#F5F7FA");
        SetBrush("PanelBackgroundBrush", "#F2FFFFFF");
        SetBrush("CardBackgroundBrush", "#E8EEF5");
        SetBrush("BorderBrush", "#D6DEE8");
        SetBrush("PrimaryTextBrush", "#17202A");
        SetBrush("SecondaryTextBrush", "#667085");
        SetBrush("LabelTextBrush", "#475467");
        SetBrush("MutedTrackBrush", "#D9E2EC");
        SetBrush("AccentBrush", "#0EA5E9");
        SetBrush("SuccessBrush", "#16A34A");
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
