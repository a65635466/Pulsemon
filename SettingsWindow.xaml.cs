using System.Windows;

namespace PulseMon;

public partial class SettingsWindow : Window
{
    private readonly Action<AppTheme> _themeChanged;
    private bool _isInitializing;

    public SettingsWindow(AppTheme currentTheme, Action<AppTheme> themeChanged)
    {
        _themeChanged = themeChanged ?? throw new ArgumentNullException(nameof(themeChanged));

        InitializeComponent();
        SetCurrentTheme(currentTheme);
    }

    public void SetCurrentTheme(AppTheme theme)
    {
        _isInitializing = true;

        BlackThemeRadioButton.IsChecked = theme == AppTheme.Black;
        LightThemeRadioButton.IsChecked = theme == AppTheme.Light;

        _isInitializing = false;
    }

    private void OnBlackThemeChecked(object sender, RoutedEventArgs e)
    {
        if (!_isInitializing)
        {
            _themeChanged(AppTheme.Black);
        }
    }

    private void OnLightThemeChecked(object sender, RoutedEventArgs e)
    {
        if (!_isInitializing)
        {
            _themeChanged(AppTheme.Light);
        }
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
