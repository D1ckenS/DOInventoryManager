using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Data;
using DOInventoryManager.Models;
using DOInventoryManager.Services;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Views
{
    public partial class ReportsView : UserControl
    {
        private readonly SummaryService _summaryService;
        private readonly ReportService _reportService;
        private readonly AlertService _alertService;
        private SummaryService.MonthlySummaryResult? _currentSummary;

        public ReportsView()
        {
            InitializeComponent();
            _summaryService = new SummaryService();
            _reportService = new ReportService();
            _alertService = new AlertService(); // Add this line
            _ = LoadDataAsync();
        }

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                // Load monthly summary data
                var months = await _summaryService.GetAvailableMonthsAsync();
                MonthComboBox.ItemsSource = months;

                if (months.Any())
                {
                    MonthComboBox.SelectedItem = months.First(); // Select latest month
                }

                // Load vessels for account statement
                using var context = new InventoryContext();
                var vessels = await context.Vessels.OrderBy(v => v.Name).ToListAsync();
                VesselAccountComboBox.ItemsSource = vessels;

                // Load suppliers for account report
                var suppliers = await context.Suppliers.OrderBy(s => s.Name).ToListAsync();
                SupplierAccountComboBox.ItemsSource = suppliers;

                // Set default date ranges
                var defaultFromDate = DateTime.Now.AddMonths(-3).Date;
                var defaultToDate = DateTime.Now.Date;

                VesselFromDatePicker.SelectedDate = defaultFromDate;
                VesselToDatePicker.SelectedDate = defaultToDate;
                SupplierFromDatePicker.SelectedDate = defaultFromDate;
                SupplierToDatePicker.SelectedDate = defaultToDate;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Monthly Summary Tab

        private async Task GenerateSummaryAsync(string month)
        {
            try
            {
                _currentSummary = await _summaryService.GenerateMonthlySummaryAsync(month);
                UpdateSummaryUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating summary: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryUI()
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

            // Update all other grids
            ConsumptionSummaryGrid.ItemsSource = _currentSummary.ConsumptionSummary ?? [];
            PurchaseSummaryGrid.ItemsSource = _currentSummary.PurchaseSummary ?? [];
            AllocationSummaryGrid.ItemsSource = _currentSummary.AllocationSummary ?? [];

            // Update financial summary
            if (_currentSummary.FinancialSummary != null)
            {
                var financial = _currentSummary.FinancialSummary;
                TotalPurchaseValueText.Text = $"Total Purchases: {financial.TotalPurchaseValueUSD:C2}";
                TotalConsumptionValueText.Text = $"Total Consumption: {financial.TotalConsumptionValueUSD:C2}";
                RemainingInventoryText.Text = $"Remaining Inventory: {financial.RemainingInventoryValueUSD:C2}";
                AvgCostPerLiterText.Text = $"Avg Cost/L: {financial.AvgCostPerLiterUSD:C6}";
                AvgCostPerTonText.Text = $"Avg Cost/T: {financial.AvgCostPerTonUSD:C2}";

                CurrencyBreakdownGrid.ItemsSource = financial.CurrencyBreakdowns ?? [];
                PaymentStatusGrid.ItemsSource = financial.PaymentStatuses ?? [];
            }
        }

        #endregion

        #region Vessel Account Statement Tab

        private async Task GenerateVesselAccountAsync()
        {
            if (VesselAccountComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a vessel.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!VesselFromDatePicker.SelectedDate.HasValue || !VesselToDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select both from and to dates.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var vesselId = (int)VesselAccountComboBox.SelectedValue;
                var fromDate = VesselFromDatePicker.SelectedDate.Value;
                var toDate = VesselToDatePicker.SelectedDate.Value;

                var report = await _reportService.GenerateVesselAccountStatementAsync(vesselId, fromDate, toDate);

                // Update summary
                var vessel = (Vessel)VesselAccountComboBox.SelectedItem;
                VesselNameText.Text = vessel.Name;
                VesselTotalPurchasesText.Text = $"{report.Summary.TotalPurchases:N3} L";
                VesselTotalConsumptionText.Text = $"{report.Summary.TotalConsumption:N3} L";
                VesselCurrentBalanceText.Text = report.Summary.FormattedCurrentBalance;
                VesselTotalValueText.Text = report.Summary.FormattedTotalValue;

                // Update grid
                VesselAccountGrid.ItemsSource = report.Transactions;

                // Update totals row
                VesselTotalDebitsText.Text = report.FormattedTotalDebits;
                VesselTotalCreditsText.Text = report.FormattedTotalCredits;
                VesselNetBalanceText.Text = report.FormattedNetBalance;
                VesselAccountTotalValueText.Text = report.FormattedTotalValue;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating vessel account statement: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Supplier Account Report Tab

        private async Task GenerateSupplierAccountAsync()
        {
            if (SupplierAccountComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a supplier.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!SupplierFromDatePicker.SelectedDate.HasValue || !SupplierToDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select both from and to dates.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var supplierId = (int)SupplierAccountComboBox.SelectedValue;
                var fromDate = SupplierFromDatePicker.SelectedDate.Value;
                var toDate = SupplierToDatePicker.SelectedDate.Value;

                var report = await _reportService.GenerateSupplierAccountReportAsync(supplierId, fromDate, toDate);

                // Update summary
                var supplier = (Supplier)SupplierAccountComboBox.SelectedItem;
                SupplierNameText.Text = supplier.Name;
                SupplierBeginningBalanceText.Text = report.Summary.FormattedBeginningBalance;
                SupplierPeriodPurchasesText.Text = $"{report.Summary.PeriodPurchases:N3} L";
                SupplierPeriodConsumptionText.Text = $"{report.Summary.PeriodConsumption:N3} L";
                SupplierEndingBalanceText.Text = report.Summary.FormattedEndingBalance;

                // Update grid
                SupplierAccountGrid.ItemsSource = report.Transactions;

                // Update totals row
                SupplierTotalPurchasesText.Text = report.FormattedTotalPurchases;
                SupplierTotalConsumptionText.Text = report.FormattedTotalConsumption;
                SupplierNetBalanceText.Text = report.FormattedNetBalance;
                SupplierAccountTotalValueText.Text = report.FormattedTotalValue;
                SupplierAccountTotalUSDText.Text = report.FormattedTotalValueUSD;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating supplier account report: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        #region Event Handlers

        // Monthly Summary Events
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

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Excel export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Print feature coming soon!", "Print",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Vessel Account Events
        private async void GenerateVesselAccount_Click(object sender, RoutedEventArgs e)
        {
            await GenerateVesselAccountAsync();
        }

        private void ExportVessel_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Vessel account export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Supplier Account Events
        private async void GenerateSupplierAccount_Click(object sender, RoutedEventArgs e)
        {
            await GenerateSupplierAccountAsync();
        }

        private void ExportSupplier_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Supplier account export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Payment Due Report Tab

        private async Task GeneratePaymentReportAsync()
        {
            try
            {
                var summary = await _alertService.GetPaymentSummaryAsync();
                var agingItems = await _alertService.GetPaymentAgingAnalysisAsync();
                var supplierSummaries = await _alertService.GetSupplierPaymentSummaryAsync();

                // Update summary cards
                OverdueAmountText.Text = summary.TotalOverdueAmount.ToString("C2");
                OverdueCountText.Text = $"{summary.OverdueCount} invoices";
                
                DueTodayAmountText.Text = summary.DueTodayAmount.ToString("C2");
                DueTodayCountText.Text = $"{summary.DueTodayCount} invoices";
                
                DueThisWeekAmountText.Text = summary.DueThisWeekAmount.ToString("C2");
                DueThisWeekCountText.Text = $"{summary.DueThisWeekCount} invoices";
                
                DueNextWeekAmountText.Text = summary.DueNextWeekAmount.ToString("C2");
                DueNextWeekCountText.Text = $"{summary.DueNextWeekCount} invoices";
                
                TotalOutstandingAmountText.Text = summary.TotalOutstandingAmount.ToString("C2");
                TotalOutstandingCountText.Text = $"{summary.TotalOutstandingCount} invoices";

                // Update grids
                PaymentScheduleGrid.ItemsSource = agingItems;
                AgingAnalysisGrid.ItemsSource = supplierSummaries;

                // Apply row styling to payment schedule
                ApplyPaymentRowStyling();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating payment report: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyPaymentRowStyling()
        {
            PaymentScheduleGrid.LoadingRow += (sender, e) =>
            {
                if (e.Row.Item is AlertService.PaymentAgingItem payment)
                {
                    switch (payment.PaymentStatus)
                    {
                        case AlertService.PaymentStatus.Overdue:
                            e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light red
                            e.Row.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red text
                            break;
                        case AlertService.PaymentStatus.DueToday:
                            e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224)); // Light orange
                            e.Row.Foreground = new SolidColorBrush(Color.FromRgb(253, 126, 20)); // Orange text
                            break;
                        case AlertService.PaymentStatus.DueTomorrow:
                        case AlertService.PaymentStatus.DueThisWeek:
                            e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 252, 230)); // Light yellow
                            e.Row.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow text
                            break;
                    }
                }
            };
        }

        private async void GeneratePaymentReport_Click(object sender, RoutedEventArgs e)
        {
            await GeneratePaymentReportAsync();
            await CreateAutoBackupAsync("PaymentReportGenerated");
            MessageBox.Show("Payment due report generated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportPayment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Payment report export feature coming soon!", "Export",
                        MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}