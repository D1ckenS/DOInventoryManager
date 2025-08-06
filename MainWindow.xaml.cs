using DOInventoryManager.Services;
using DOInventoryManager.Views;
using System.Windows;
using System.Windows.Controls;
using System.IO;


namespace DOInventoryManager
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
            LoadDashboard(); // Load dashboard by default
        }

        private async void InitializeWindow()
        {
            // Set current date in header
            CurrentDateText.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

            // Set window title with version
            this.Title = "DO Inventory Manager - v1.0.0";

            // Load navigation state
            LoadNavigationState();

            // Initialize theme toggle button
            var themeService = ThemeService.Instance;
            UpdateThemeToggleButton(themeService.ActualTheme);

            // Check for due date alerts on startup
            await CheckStartupAlertsAsync();
        }

        private async Task CheckStartupAlertsAsync()
        {
            try
            {
                var alertService = new AlertService();
                var alerts = await alertService.GetDueDateAlertsAsync();

                if (alerts.Any())
                {
                    // Update status bar with alert count
                    var alertSummary = alertService.GetAlertSummary(alerts);
                    DatabaseStatus.Text = $"⚠️ Alerts: {alertSummary}";
                    DatabaseStatus.Foreground = alerts.Any(a => a.AlertLevel == AlertService.DueDateAlertLevel.Overdue ||
                                                               a.AlertLevel == AlertService.DueDateAlertLevel.DueToday)
                                                               ? System.Windows.Media.Brushes.Red
                                                               : System.Windows.Media.Brushes.Orange;

                    // Show startup alert popup for critical alerts
                    var criticalAlerts = alerts.Where(a => a.AlertLevel == AlertService.DueDateAlertLevel.Overdue ||
                                                          a.AlertLevel == AlertService.DueDateAlertLevel.DueToday).ToList();

                    if (criticalAlerts.Any())
                    {
                        ShowStartupAlertPopup(criticalAlerts);
                    }
                }
                else
                {
                    DatabaseStatus.Text = "Database: Connected";
                    DatabaseStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking startup alerts: {ex.Message}");
            }
        }

        private void ShowStartupAlertPopup(List<AlertService.DueDateAlert> criticalAlerts)
        {
            var alertMessage = "🚨 CRITICAL PAYMENT ALERTS 🚨\n\n";

            foreach (var alert in criticalAlerts.Take(5)) // Show max 5 in popup
            {
                alertMessage += $"• {alert.AlertMessage}\n";
                alertMessage += $"  Invoice: {alert.InvoiceReference} | Supplier: {alert.SupplierName}\n";
                alertMessage += $"  Amount: {alert.FormattedValue} | Due: {alert.FormattedDueDate}\n\n";
            }

            if (criticalAlerts.Count > 5)
            {
                alertMessage += $"... and {criticalAlerts.Count - 5} more alerts\n\n";
            }

            alertMessage += "Go to Purchase Entry to view all payment alerts.";

            MessageBox.Show(alertMessage, "Payment Due Date Alerts",
                           MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        #region Navigation Methods

        private void LoadDashboard()
        {
            try
            {
                var dashboardView = new DashboardView();
                
                // Subscribe to navigation requests from dashboard
                dashboardView.NavigationRequested += OnDashboardNavigationRequested;
                
                ContentFrame.Content = dashboardView;
                SetActiveButton(DashboardBtn);
                StatusText.Text = "Dashboard loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading dashboard: {ex.Message}";
            }
        }

        private void OnDashboardNavigationRequested(string viewName)
        {
            switch (viewName)
            {
                case "Purchases":
                    LoadPurchases();
                    break;
                case "Consumption":
                    LoadConsumption();
                    break;
                case "Allocation":
                    LoadAllocation();
                    break;
                default:
                    StatusText.Text = $"Unknown view requested: {viewName}";
                    break;
            }
        }

        private void OnConsumptionNavigationRequested(string viewName)
        {
            switch (viewName)
            {
                case "Reports":
                    LoadSummary(); // This loads ReportsView
                    break;
                default:
                    StatusText.Text = $"Unknown view requested: {viewName}";
                    break;
            }
        }

        private void LoadSuppliers()
        {
            try
            {
                ContentFrame.Content = new SuppliersView();
                SetActiveButton(SuppliersBtn);
                StatusText.Text = "Suppliers management loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading suppliers: {ex.Message}";
            }
        }

        private void LoadVessels()
        {
            try
            {
                ContentFrame.Content = new VesselsView();
                SetActiveButton(VesselsBtn);
                StatusText.Text = "Vessels management loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading vessels: {ex.Message}";
            }
        }

        private void LoadPurchases()
        {
            try
            {
                ContentFrame.Content = new PurchasesView();
                SetActiveButton(PurchasesBtn);
                StatusText.Text = "Purchase entry loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading purchases: {ex.Message}";
            }
        }

        private void LoadConsumption()
        {
            try
            {
                var consumptionView = new ConsumptionView();
                consumptionView.NavigationRequested += OnConsumptionNavigationRequested;
                ContentFrame.Content = consumptionView;
                SetActiveButton(ConsumptionBtn);
                StatusText.Text = "Consumption entry loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading consumption: {ex.Message}";
            }
        }

        private void LoadTrips()
        {
            // TODO: Create TripsView
            StatusText.Text = "Trip management - Coming soon";
        }

        private void LoadAllocation()
        {
            try
            {
                ContentFrame.Content = new AllocationView();
                SetActiveButton(AllocationBtn);
                StatusText.Text = "Monthly allocation loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading allocation: {ex.Message}";
            }
        }

        private void LoadSummary()
        {
            try
            {
                ContentFrame.Content = new ReportsView();
                SetActiveButton(SummaryBtn);
                StatusText.Text = "Reports section loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading reports: {ex.Message}";
            }
        }

        private void SetActiveButton(Button activeButton)
        {
            // Reset all buttons to normal style
            DashboardBtn.Style = (Style)FindResource("NavigationButtonStyle");
            SuppliersBtn.Style = (Style)FindResource("NavigationButtonStyle");
            VesselsBtn.Style = (Style)FindResource("NavigationButtonStyle");
            PurchasesBtn.Style = (Style)FindResource("NavigationButtonStyle");
            ConsumptionBtn.Style = (Style)FindResource("NavigationButtonStyle");
            TripsBtn.Style = (Style)FindResource("NavigationButtonStyle");
            AllocationBtn.Style = (Style)FindResource("NavigationButtonStyle");
            SummaryBtn.Style = (Style)FindResource("NavigationButtonStyle");
            BackupBtn.Style = (Style)FindResource("NavigationButtonStyle");
            SettingsBtn.Style = (Style)FindResource("NavigationButtonStyle");

            // Set active button style
            activeButton.Style = (Style)FindResource("ActiveNavigationButtonStyle");
        }

        #endregion

        #region Button Click Events

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }

        private void Suppliers_Click(object sender, RoutedEventArgs e)
        {
            LoadSuppliers();
        }

        private void Vessels_Click(object sender, RoutedEventArgs e)
        {
            LoadVessels();
        }

        private void Purchases_Click(object sender, RoutedEventArgs e)
        {
            LoadPurchases();
        }

        private void Consumption_Click(object sender, RoutedEventArgs e)
        {
            LoadConsumption();
        }

        private void Trips_Click(object sender, RoutedEventArgs e)
        {
            LoadTrips();
        }

        private void Allocation_Click(object sender, RoutedEventArgs e)
        {
            LoadAllocation();
        }

        private void Summary_Click(object sender, RoutedEventArgs e)
        {
            LoadSummary();
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Report generation feature coming soon!", "DO Inventory Manager",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadBackupManagement()
        {
            try
            {
                ContentFrame.Content = new BackupManagementView();
                SetActiveButton(BackupBtn);
                StatusText.Text = "Backup management loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading backup management: {ex.Message}";
            }
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            LoadBackupManagement();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement settings
            MessageBox.Show("Settings feature coming soon!", "DO Inventory Manager",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Modern Navigation Features

        private bool _isNavigationCollapsed = false;
        
        private void HamburgerMenu_Click(object sender, RoutedEventArgs e)
        {
            ToggleNavigationPane();
        }

        private void ToggleNavigationPane()
        {
            _isNavigationCollapsed = !_isNavigationCollapsed;

            if (_isNavigationCollapsed)
            {
                // Collapse navigation - show only icons
                NavigationColumn.Width = new GridLength(56);
                
                // Hide all text elements
                NavigationTitle.Visibility = Visibility.Collapsed;
                NavigationFooter.Visibility = Visibility.Collapsed;
                
                // Hide text in navigation buttons
                DashboardText.Visibility = Visibility.Collapsed;
                SuppliersText.Visibility = Visibility.Collapsed;
                VesselsText.Visibility = Visibility.Collapsed;
                PurchasesText.Visibility = Visibility.Collapsed;
                ConsumptionText.Visibility = Visibility.Collapsed;
                TripsText.Visibility = Visibility.Collapsed;
                AllocationText.Visibility = Visibility.Collapsed;
                SummaryText.Visibility = Visibility.Collapsed;
                BackupText.Visibility = Visibility.Collapsed;
                SettingsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Expand navigation - show icons and text
                NavigationColumn.Width = new GridLength(280);
                
                // Show all text elements
                NavigationTitle.Visibility = Visibility.Visible;
                NavigationFooter.Visibility = Visibility.Visible;
                
                // Show text in navigation buttons
                DashboardText.Visibility = Visibility.Visible;
                SuppliersText.Visibility = Visibility.Visible;
                VesselsText.Visibility = Visibility.Visible;
                PurchasesText.Visibility = Visibility.Visible;
                ConsumptionText.Visibility = Visibility.Visible;
                TripsText.Visibility = Visibility.Visible;
                AllocationText.Visibility = Visibility.Visible;
                SummaryText.Visibility = Visibility.Visible;
                BackupText.Visibility = Visibility.Visible;
                SettingsText.Visibility = Visibility.Visible;
            }

            // Save navigation state
            SaveNavigationState();
        }

        private void SaveNavigationState()
        {
            try
            {
                // Save navigation state to user preferences
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "navigation-settings.json");
                var settings = new { IsCollapsed = _isNavigationCollapsed };
                var json = System.Text.Json.JsonSerializer.Serialize(settings);
                File.WriteAllText(settingsPath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private void LoadNavigationState()
        {
            try
            {
                var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "navigation-settings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    using var document = System.Text.Json.JsonDocument.Parse(json);
                    if (document.RootElement.TryGetProperty("IsCollapsed", out var property) && property.GetBoolean())
                    {
                        _isNavigationCollapsed = false; // Start expanded, then toggle to collapsed
                        ToggleNavigationPane();
                    }
                }
            }
            catch
            {
                // Use default expanded state if loading fails
            }
        }

        #endregion

        #region Theme Management
        
        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            var themeService = ThemeService.Instance;
            var nextTheme = themeService.ActualTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            themeService.SetTheme(nextTheme);
            
            // Update button tooltip and icon
            UpdateThemeToggleButton(nextTheme);
        }

        private void UpdateThemeToggleButton(AppTheme currentTheme)
        {
            if (ThemeToggleBtn.Content is TextBlock textBlock)
            {
                // Show sun icon when in dark mode (to switch to light)
                // Show moon icon when in light mode (to switch to dark)
                if (currentTheme == AppTheme.Dark)
                {
                    textBlock.Text = "☀️";
                    textBlock.FontSize = 18;
                    ThemeToggleBtn.ToolTip = "Switch to Light Theme";
                }
                else
                {
                    textBlock.Text = "🌙";
                    textBlock.FontSize = 18;
                    ThemeToggleBtn.ToolTip = "Switch to Dark Theme";
                }
            }
        }

        #endregion
    }
}