namespace PulseMon.Tray;

public sealed class TrayManager : IDisposable
{
    private const double HighMemoryUsageThresholdPercent = 60d;
    private static readonly TimeSpan NormalAnimationInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan HighMemoryAnimationInterval = TimeSpan.FromMilliseconds(90);

    private readonly NotifyIcon _notifyIcon;
    private readonly Action _toggleWindow;
    private readonly Action _showWindow;
    private readonly Action _showSettings;
    private readonly Action _exitApplication;
    private readonly Icon[] _runningFrames;
    private readonly System.Windows.Forms.Timer _animationTimer;
    private int _currentFrameIndex;
    private bool _disposed;

    public TrayManager(Action toggleWindow, Action showWindow, Action showSettings, Action exitApplication)
    {
        _toggleWindow = toggleWindow ?? throw new ArgumentNullException(nameof(toggleWindow));
        _showWindow = showWindow ?? throw new ArgumentNullException(nameof(showWindow));
        _showSettings = showSettings ?? throw new ArgumentNullException(nameof(showSettings));
        _exitApplication = exitApplication ?? throw new ArgumentNullException(nameof(exitApplication));
        _runningFrames = TrayIconRenderer.CreateRunningFrames();

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open PulseMon", null, OnOpenClicked);
        contextMenu.Items.Add("Settings", null, OnSettingsClicked);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, OnExitClicked);

        _notifyIcon = new NotifyIcon
        {
            Icon = _runningFrames[0],
            Text = "PulseMon",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.MouseClick += OnNotifyIconMouseClick;

        _animationTimer = new System.Windows.Forms.Timer
        {
            Interval = (int)NormalAnimationInterval.TotalMilliseconds
        };
        _animationTimer.Tick += OnAnimationTimerTick;
        _animationTimer.Start();
    }

    public void UpdateMemoryUsage(double memoryUsagePercent)
    {
        if (_disposed)
        {
            return;
        }

        var targetInterval = memoryUsagePercent >= HighMemoryUsageThresholdPercent
            ? HighMemoryAnimationInterval
            : NormalAnimationInterval;
        var targetText = memoryUsagePercent >= HighMemoryUsageThresholdPercent
            ? $"PulseMon - RAM high ({memoryUsagePercent:0}%)"
            : "PulseMon";

        if (_animationTimer.Interval != (int)targetInterval.TotalMilliseconds)
        {
            _animationTimer.Interval = (int)targetInterval.TotalMilliseconds;
        }

        _notifyIcon.Text = targetText;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.MouseClick -= OnNotifyIconMouseClick;
        _animationTimer.Stop();
        _animationTimer.Tick -= OnAnimationTimerTick;
        _animationTimer.Dispose();

        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();

        foreach (var frame in _runningFrames)
        {
            frame.Dispose();
        }

        _disposed = true;
    }

    private void OnAnimationTimerTick(object? sender, EventArgs e)
    {
        _currentFrameIndex = (_currentFrameIndex + 1) % _runningFrames.Length;
        _notifyIcon.Icon = _runningFrames[_currentFrameIndex];
    }

    private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _toggleWindow();
        }
    }

    private void OnOpenClicked(object? sender, EventArgs e)
    {
        _showWindow();
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        _showSettings();
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        _exitApplication();
    }
}
