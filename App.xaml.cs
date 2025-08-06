using System.Configuration;
using System.Data;
using System.Windows;
using DOInventoryManager.Services;

namespace DOInventoryManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize the theme service and apply the saved theme
        var themeService = ThemeService.Instance;
        themeService.SetTheme(themeService.CurrentTheme);
    }
}

