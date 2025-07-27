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
                ContentFrame.Content = new DashboardView();
                SetActiveButton(DashboardBtn);
                StatusText.Text = "Dashboard loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading dashboard: {ex.Message}";
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
                ContentFrame.Content = new ConsumptionView();
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
            DashboardBtn.Style = (Style)FindResource("NavButtonStyle");
            SuppliersBtn.Style = (Style)FindResource("NavButtonStyle");
            VesselsBtn.Style = (Style)FindResource("NavButtonStyle");
            PurchasesBtn.Style = (Style)FindResource("NavButtonStyle");
            ConsumptionBtn.Style = (Style)FindResource("NavButtonStyle");
            TripsBtn.Style = (Style)FindResource("NavButtonStyle");
            AllocationBtn.Style = (Style)FindResource("NavButtonStyle");
            SummaryBtn.Style = (Style)FindResource("NavButtonStyle");
            BackupBtn.Style = (Style)FindResource("NavButtonStyle");
            SettingsBtn.Style = (Style)FindResource("NavButtonStyle");

            // Set active button style
            activeButton.Style = (Style)FindResource("ActiveNavButtonStyle");
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

        private async void Backup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Creating backup...";
                var backupService = new BackupService();

                var result = await backupService.CreateBackupAsync("Manual");

                if (result.Success)
                {
                    StatusText.Text = $"Backup completed - {result.TotalBackups} total backups";

                    var message = $"✅ {result.Message}\n\n" +
                                 $"📁 Backup Location: {Path.GetFileName(result.BackupPath)}\n" +
                                 $"📏 File Size: {result.BackupSizeBytes / 1024.0 / 1024.0:F2} MB\n" +
                                 $"📊 Total Backups: {result.TotalBackups}\n\n" +
                                 $"Would you like to open the backup folder?";

                    var openFolder = MessageBox.Show(message, "Backup Completed",
                                                   MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (openFolder == MessageBoxResult.Yes)
                    {
                        backupService.OpenBackupFolder();
                    }
                }
                else
                {
                    StatusText.Text = "Backup failed";
                    MessageBox.Show($"❌ {result.Message}", "Backup Failed",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Backup error";
                MessageBox.Show($"Backup error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement settings
            MessageBox.Show("Settings feature coming soon!", "DO Inventory Manager",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}