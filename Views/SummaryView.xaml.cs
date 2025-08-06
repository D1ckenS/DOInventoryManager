using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Services;
using System.Diagnostics;
using System.IO;
using DOInventoryManager.Views.Print;

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
                else
                {
                    MessageBox.Show("No data available for monthly summaries.", "No Data",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading months: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error",
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
            if (_currentSummary?.ExecutiveSummary == null) return;

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
            ConsumptionSummaryGrid.ItemsSource = _currentSummary?.ConsumptionSummary ?? [];
        }

        private void UpdatePurchaseSummary()
        {
            PurchaseSummaryGrid.ItemsSource = _currentSummary?.PurchaseSummary ?? [];
        }

        private void UpdateAllocationSummary()
        {
            AllocationSummaryGrid.ItemsSource = _currentSummary?.AllocationSummary ?? [];
        }

        private void UpdateFinancialSummary()
        {
            if (_currentSummary?.FinancialSummary == null) return;

            var financial = _currentSummary.FinancialSummary;

            TotalPurchaseValueText.Text = $"Total Purchases: {financial.TotalPurchaseValueUSD:C2}";
            TotalConsumptionValueText.Text = $"Total Consumption: {financial.TotalConsumptionValueUSD:C2}";
            RemainingInventoryText.Text = $"Remaining Inventory: {financial.RemainingInventoryValueUSD:C2}";
            AvgCostPerLiterText.Text = $"Avg Cost/L: {financial.AvgCostPerLiterUSD:C6}";
            AvgCostPerTonText.Text = $"Avg Cost/T: {financial.AvgCostPerTonUSD:C2}";

            CurrencyBreakdownGrid.ItemsSource = financial.CurrencyBreakdowns ?? [];
            PaymentStatusGrid.ItemsSource = financial.PaymentStatuses ?? [];
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
            if (MonthComboBox.SelectedItem?.ToString() is string selectedMonth)
            {
                await GenerateSummaryAsync(selectedMonth);
            }
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            if (MonthComboBox.SelectedItem?.ToString() is string selectedMonth)
            {
                await GenerateSummaryAsync(selectedMonth);

                await CreateAutoBackupAsync("SummaryGenerated");

                MessageBox.Show("Monthly summary report generated successfully!", "Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a month first.", "No Month Selected",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentSummary == null)
                {
                    MessageBox.Show("Please generate a report first.", "No Data",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedMonth = MonthComboBox.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedMonth))
                {
                    MessageBox.Show("Please select a month first.", "No Month Selected",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var exportService = new ExcelExportService();
                var filePath = await exportService.ExportMonthlySummaryToExcelAsync(_currentSummary, selectedMonth);

                var result = MessageBox.Show($"Monthly summary exported successfully!\n\nFile: {Path.GetFileName(filePath)}\n\nWould you like to open the file?",
                                           "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting summary report: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Print button clicked!");
        }

        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            var scrollViewer = FindParent<ScrollViewer>(dataGrid);
            if (scrollViewer == null) return;

            // Check if DataGrid needs internal scrolling
            var dataGridScrollViewer = FindChild<ScrollViewer>(dataGrid);
            if (dataGridScrollViewer != null)
            {
                // If scrolling down and can scroll down, let DataGrid handle it
                if (e.Delta < 0 && dataGridScrollViewer.VerticalOffset < dataGridScrollViewer.ScrollableHeight)
                    return;

                // If scrolling up and can scroll up, let DataGrid handle it  
                if (e.Delta > 0 && dataGridScrollViewer.VerticalOffset > 0)
                    return;
            }

            // Redirect to parent ScrollViewer
            e.Handled = true;
            var newEvent = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent
            };
            scrollViewer.RaiseEvent(newEvent);
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not T)
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childResult = FindChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        #endregion
    }
}