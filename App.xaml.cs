using System.Windows;
using PulseMon.Tray;

namespace PulseMon;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\PulseMon.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private TrayManager? _trayManager;
    private AppTheme _currentTheme = AppTheme.Black;
    private bool _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out _ownsSingleInstanceMutex);
        if (!_ownsSingleInstanceMutex)
        {
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _mainWindow = new MainWindow();
        _mainWindow.Closing += OnMainWindowClosing;

        _trayManager = new TrayManager(ToggleMainWindow, ShowMainWindow, ShowSettingsWindow, ExitApplication);
        ShowMainWindow();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        _trayManager = null;

        if (_mainWindow is not null)
        {
            _mainWindow.Closing -= OnMainWindowClosing;
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

        base.OnExit(e);
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
