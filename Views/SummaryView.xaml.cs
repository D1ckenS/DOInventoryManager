using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Services;

namespace DOInventoryManager.Views
{
    public partial class SummaryView : UserControl
    {
        private readonly SummaryService _summaryService;
        private SummaryService.MonthlySummaryResult? _currentSummary;

        public SummaryView()
        {
            InitializeComponent();
            _summaryService = new SummaryService();
            _ = LoadDataAsync();
        }

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                var months = await _summaryService.GetAvailableMonthsAsync();
                MonthComboBox.ItemsSource = months;

                if (months.Any())
                {
                    MonthComboBox.SelectedItem = months.First(); // Select latest month
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading months: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GenerateSummaryAsync(string month)
        {
            try
            {
                _currentSummary = await _summaryService.GenerateMonthlySummaryAsync(month);
                UpdateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating summary: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUI()
        {
            if (_currentSummary == null) return;

            UpdateExecutiveSummary();
            UpdateConsumptionSummary();
            UpdatePurchaseSummary();
            UpdateAllocationSummary();
            UpdateFinancialSummary();
        }

        private void UpdateExecutiveSummary()
        {
            var executive = _currentSummary.ExecutiveSummary;

            TotalConsumptionText.Text = $"{executive.TotalFleetConsumptionL:N0} L";
            TotalLegsText.Text = executive.TotalLegsCompleted.ToString();
            TotalCostText.Text = executive.TotalOperatingCostUSD.ToString("C0");
            EfficiencyText.Text = $"{executive.FleetEfficiencyLPerLeg:N2} L/leg";

            VesselsOperatedText.Text = $"Vessels Operated: {executive.VesselsOperated}";
            SuppliersUsedText.Text = $"Suppliers Used: {executive.SuppliersUsed}";
            CostPerLegText.Text = $"Cost per Leg: {executive.CostPerLeg:C2}";
            FleetEfficiencyLText.Text = $"Fleet Efficiency (L): {executive.FleetEfficiencyLPerLeg:N3} L/leg";
            FleetEfficiencyTText.Text = $"Fleet Efficiency (T): {executive.FleetEfficiencyTPerLeg:N3} T/leg";
            InventoryTurnoverText.Text = $"Inventory Turnover: {executive.InventoryTurnover:N2}";
        }

        private void UpdateConsumptionSummary()
        {
            ConsumptionSummaryGrid.ItemsSource = _currentSummary.ConsumptionSummary;
        }

        private void UpdatePurchaseSummary()
        {
            PurchaseSummaryGrid.ItemsSource = _currentSummary.PurchaseSummary;
        }

        private void UpdateAllocationSummary()
        {
            AllocationSummaryGrid.ItemsSource = _currentSummary.AllocationSummary;
        }

        private void UpdateFinancialSummary()
        {
            var financial = _currentSummary.FinancialSummary;

            TotalPurchaseValueText.Text = $"Total Purchases: {financial.TotalPurchaseValueUSD:C2}";
            TotalConsumptionValueText.Text = $"Total Consumption: {financial.TotalConsumptionValueUSD:C2}";
            RemainingInventoryText.Text = $"Remaining Inventory: {financial.RemainingInventoryValueUSD:C2}";
            AvgCostPerLiterText.Text = $"Avg Cost/L: {financial.AvgCostPerLiterUSD:C6}";
            AvgCostPerTonText.Text = $"Avg Cost/T: {financial.AvgCostPerTonUSD:C2}";

            CurrencyBreakdownGrid.ItemsSource = financial.CurrencyBreakdowns;
            PaymentStatusGrid.ItemsSource = financial.PaymentStatuses;
        }

        #endregion

        #region Backup Management

        private async Task CreateAutoBackupAsync(string operation)
        {
            try
            {
                var backupService = new BackupService();
                await backupService.CreateBackupAsync(operation);
            }
            catch
            {
                // Don't show errors for auto-backup failures
            }
        }

        #endregion

        #region Button Click Events

        private async void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonthComboBox.SelectedItem != null)
            {
                var selectedMonth = MonthComboBox.SelectedItem.ToString();
                await GenerateSummaryAsync(selectedMonth);
            }
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (MonthComboBox.SelectedItem != null)
            {
                var selectedMonth = MonthComboBox.SelectedItem.ToString();
                await GenerateSummaryAsync(selectedMonth);

                await CreateAutoBackupAsync("SummaryGenerated");

                MessageBox.Show("Monthly summary report generated successfully!", "Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSummary == null)
            {
                MessageBox.Show("Please generate a report first.", "No Data",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Excel export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSummary == null)
            {
                MessageBox.Show("Please generate a report first.", "No Data",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Print feature coming soon!", "Print",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}