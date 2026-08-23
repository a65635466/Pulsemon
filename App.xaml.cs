using System.Windows;
using PulseMon.Models;
using PulseMon.Tray;

namespace PulseMon;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\PulseMon.SingleInstance";
    private const string ShowWindowEventName = @"Local\PulseMon.ShowWindow";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _showWindowEvent;
    private CancellationTokenSource? _showWindowListenerCancellation;
    private bool _ownsSingleInstanceMutex;
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private TrayManager? _trayManager;
    private AppTheme _currentTheme = AppTheme.Black;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _showWindowEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, ShowWindowEventName);
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out _ownsSingleInstanceMutex);
        if (!_ownsSingleInstanceMutex)
        {
            _showWindowEvent.Set();
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        StartShowWindowListener();

        _mainWindow = new MainWindow();
        _mainWindow.Closing += OnMainWindowClosing;
        _mainWindow.StatusUpdated += OnMainWindowStatusUpdated;

        _trayManager = new TrayManager(ToggleMainWindow, ShowMainWindow, ShowSettingsWindow, ExitApplication);
        ShowMainWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _showWindowListenerCancellation?.Cancel();
        _showWindowEvent?.Set();

        _trayManager?.Dispose();
        _trayManager = null;

        if (_mainWindow is not null)
        {
            _mainWindow.Closing -= OnMainWindowClosing;
            _mainWindow.StatusUpdated -= OnMainWindowStatusUpdated;
            _mainWindow.StopMonitoring();
        }

        if (_settingsWindow is not null)
        {
            _settingsWindow.Closing -= OnSettingsWindowClosing;
            _settingsWindow.Close();
            _settingsWindow = null;
        }

        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;
        _ownsSingleInstanceMutex = false;

        _showWindowListenerCancellation?.Dispose();
        _showWindowListenerCancellation = null;
        _showWindowEvent?.Dispose();
        _showWindowEvent = null;

        base.OnExit(e);
    }

    private void StartShowWindowListener()
    {
        if (_showWindowEvent is null)
        {
            return;
        }

        _showWindowListenerCancellation = new CancellationTokenSource();
        var cancellationToken = _showWindowListenerCancellation.Token;
        var showWindowEvent = _showWindowEvent;

        _ = Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    showWindowEvent.WaitOne();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                Dispatcher.BeginInvoke(ShowMainWindow);
            }
        }, cancellationToken);
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
            return;
        }

        _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow(_currentTheme, ApplyTheme);
            _settingsWindow.Closing += OnSettingsWindowClosing;
        }

        _settingsWindow.SetCurrentTheme(_currentTheme);
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ApplyTheme(AppTheme theme)
    {
        _currentTheme = theme;
        _mainWindow?.ApplyTheme(theme);
        _settingsWindow?.SetCurrentTheme(theme);
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _trayManager?.Dispose();
        Shutdown();
    }

    private void OnMainWindowStatusUpdated(SystemStatus status)
    {
        if (status.MemoryTotalGb <= 0)
        {
            _trayManager?.UpdateMemoryUsage(0);
            return;
        }

        var memoryUsagePercent = Math.Clamp((status.MemoryUsedGb / status.MemoryTotalGb) * 100, 0, 100);
        _trayManager?.UpdateMemoryUsage(memoryUsagePercent);
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void OnSettingsWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        _settingsWindow?.Hide();
    }
}
