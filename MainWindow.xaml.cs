using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Views;

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

        private void InitializeWindow()
        {
            // Set current date in header
            CurrentDateText.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

            // Set window title with version
            this.Title = "DO Inventory Manager - v1.0.0";
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
            // TODO: Create SummaryView
            StatusText.Text = "Monthly summary - Coming soon";
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

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement backup functionality
            MessageBox.Show("Backup feature coming soon!", "DO Inventory Manager",
                          MessageBoxButton.OK, MessageBoxImage.Information);
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