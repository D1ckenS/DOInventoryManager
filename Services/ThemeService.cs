using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;

namespace DOInventoryManager.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public class ThemeService : INotifyPropertyChanged
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        private AppTheme _currentTheme = AppTheme.System;
        private bool _isSystemDarkMode = false;
        private const string SettingsFileName = "theme-settings.json";

        public event PropertyChangedEventHandler? PropertyChanged;

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTheme)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualTheme)));
                }
            }
        }

        public AppTheme ActualTheme =>
            CurrentTheme == AppTheme.System 
                ? (_isSystemDarkMode ? AppTheme.Dark : AppTheme.Light) 
                : CurrentTheme;

        public bool IsSystemDarkMode
        {
            get => _isSystemDarkMode;
            private set
            {
                if (_isSystemDarkMode != value)
                {
                    _isSystemDarkMode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSystemDarkMode)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualTheme)));
                    
                    // If using system theme, update the UI
                    if (CurrentTheme == AppTheme.System)
                    {
                        ApplyTheme();
                    }
                }
            }
        }

        private ThemeService()
        {
            LoadSettings();
            DetectSystemTheme();
            SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
        }

        public void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            ApplyTheme();
            SaveSettings();
        }

        private void ApplyTheme()
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return;

                // Use Dispatcher to ensure proper UI thread execution
                app.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Find the AppTheme resource dictionary
                    ResourceDictionary? appThemeDict = null;
                    foreach (var dict in app.Resources.MergedDictionaries)
                    {
                        if (dict.Source?.ToString().Contains("AppTheme.xaml") == true)
                        {
                            appThemeDict = dict;
                            break;
                        }
                    }

                    if (appThemeDict == null) return;

                    // Find the ThemeResources dictionary within AppTheme
                    ResourceDictionary? themeResourcesDict = null;
                    foreach (var dict in appThemeDict.MergedDictionaries)
                    {
                        if (dict.Contains("PrimaryBrand"))
                        {
                            themeResourcesDict = dict;
                            break;
                        }
                    }

                    if (themeResourcesDict == null) return;

                    // Determine which theme to apply
                    var targetTheme = ActualTheme;
                    var themeFileName = targetTheme == AppTheme.Dark ? "DarkTheme.xaml" : "LightTheme.xaml";

                    // Create new resource dictionary for the target theme
                    var newThemeDict = new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/Themes/{themeFileName}", UriKind.Absolute)
                    };

                    // Replace the theme resources
                    var index = appThemeDict.MergedDictionaries.IndexOf(themeResourcesDict);
                    if (index >= 0)
                    {
                        appThemeDict.MergedDictionaries[index] = newThemeDict;
                    }

                    // Force complete visual refresh
                    ForceCompleteRefresh();
                }));
            }
            catch (Exception ex)
            {
                // Log error (you might want to use a proper logging framework)
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private void DetectSystemTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var systemUsesLightTheme = key?.GetValue("SystemUsesLightTheme");
                
                // If the registry value is 0 or doesn't exist, assume dark mode
                IsSystemDarkMode = systemUsesLightTheme is int value && value == 0;
            }
            catch
            {
                // Default to light theme if we can't detect
                IsSystemDarkMode = false;
            }
        }

        private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General ||
                e.Category == UserPreferenceCategory.VisualStyle)
            {
                DetectSystemTheme();
            }
        }

        private void ForceCompleteRefresh()
        {
            var app = Application.Current;
            if (app?.MainWindow != null)
            {
                // Refresh all windows first
                foreach (Window window in app.Windows)
                {
                    if (window != null)
                    {
                        // Force complete resource refresh for each window
                        RefreshWindowResources(window);
                        InvalidateCompleteVisualTree(window);
                    }
                }
                
                // Update default styles for all control types
                UpdateDefaultStyles();
                
                // Force application-level resource refresh
                RefreshApplicationResources();
                
                // Final invalidation pass
                app.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (Window window in app.Windows)
                    {
                        if (window != null)
                        {
                            window.InvalidateVisual();
                            window.UpdateLayout();
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void RefreshApplicationResources()
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            // Force refresh of merged dictionaries
            var mergedDictionaries = app.Resources.MergedDictionaries.ToList();
            app.Resources.MergedDictionaries.Clear();
            foreach (var dict in mergedDictionaries)
            {
                app.Resources.MergedDictionaries.Add(dict);
            }
        }

        private void RefreshWindowResources(Window window)
        {
            // Clear window resources to force re-evaluation
            window.Resources.Clear();
            
            // Force style re-application
            var originalStyle = window.Style;
            window.Style = null;
            window.UpdateLayout();
            window.Style = originalStyle;
            
            // Refresh all child resources
            RefreshAllResources(window);
        }

        private void RefreshAllResources(DependencyObject obj)
        {
            if (obj is FrameworkElement element)
            {
                // Force resource refresh
                element.Resources.Clear();
                
                // Trigger style re-application
                var originalStyle = element.Style;
                element.Style = null;
                element.UpdateLayout();
                element.Style = originalStyle;
            }

            var childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                RefreshAllResources(child);
            }
        }

        private void UpdateDefaultStyles()
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            // Force re-application of default styles for common control types
            var controlTypes = new Type[]
            {
                typeof(TextBlock), typeof(Button), typeof(TextBox), typeof(ComboBox),
                typeof(Border), typeof(UserControl), typeof(Window), typeof(TabItem),
                typeof(DataGrid), typeof(DataGridCell), typeof(DataGridColumnHeader)
            };

            foreach (var controlType in controlTypes)
            {
                if (app.Resources.Contains(controlType))
                {
                    var style = app.Resources[controlType];
                    app.Resources.Remove(controlType);
                    app.Resources.Add(controlType, style);
                }
            }
        }

        private void InvalidateCompleteVisualTree(DependencyObject obj)
        {
            if (obj is FrameworkElement element)
            {
                element.InvalidateVisual();
                element.InvalidateMeasure();
                element.InvalidateArrange();
                element.UpdateLayout();
            }

            var childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                InvalidateCompleteVisualTree(child);
            }
        }

        private void LoadSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                    if (settings != null && Enum.IsDefined(typeof(AppTheme), settings.Theme))
                    {
                        _currentTheme = settings.Theme;
                    }
                }
            }
            catch
            {
                // Use default theme if loading fails
                _currentTheme = AppTheme.System;
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new ThemeSettings { Theme = CurrentTheme };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        ~ThemeService()
        {
            SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
        }
    }

    internal class ThemeSettings
    {
        public AppTheme Theme { get; set; } = AppTheme.System;
    }
}