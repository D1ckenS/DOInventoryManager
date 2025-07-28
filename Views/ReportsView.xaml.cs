using System;
using System.Windows;
using System.Windows.Media;
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
        private readonly InventoryValuationService _inventoryService;
        private readonly FleetEfficiencyService _fleetEfficiencyService;
        private readonly FIFOAllocationDetailService _fifoDetailService;
        private readonly CostAnalysisService _costAnalysisService;
        private readonly RoutePerformanceService _routePerformanceService;
        private readonly ExcelExportService _excelExportService;
        private SummaryService.MonthlySummaryResult? _currentSummary;
        private ReportService.VesselAccountStatementResult? _currentVesselAccount;
        private ReportService.SupplierAccountReportResult? _currentSupplierAccount;


        public ReportsView()
        {
            InitializeComponent();
            _summaryService = new SummaryService();
            _reportService = new ReportService();
            _alertService = new AlertService();
            _inventoryService = new InventoryValuationService();
            _fleetEfficiencyService = new FleetEfficiencyService();
            _fifoDetailService = new FIFOAllocationDetailService();
            _costAnalysisService = new CostAnalysisService();
            _routePerformanceService = new RoutePerformanceService();
            _excelExportService = new ExcelExportService(); // Add this line
            FleetFromDatePicker.SelectedDate = DateTime.Today.AddMonths(-12);
            FleetToDatePicker.SelectedDate = DateTime.Today;
            FIFOFromDatePicker.SelectedDate = DateTime.Today.AddMonths(-6);
            FIFOToDatePicker.SelectedDate = DateTime.Today;
            CostFromDatePicker.SelectedDate = DateTime.Today.AddMonths(-12);
            CostToDatePicker.SelectedDate = DateTime.Today;
            RouteFromDatePicker.SelectedDate = DateTime.Today.AddMonths(-12);
            RouteToDatePicker.SelectedDate = DateTime.Today;
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
                _currentVesselAccount = report;
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

                _currentSupplierAccount = report;
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

        private async void PayInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int purchaseId)
            {
                try
                {
                    using var context = new InventoryContext();
                    var purchase = await context.Purchases
                        .Include(p => p.Supplier)
                        .Include(p => p.Vessel)
                        .FirstOrDefaultAsync(p => p.Id == purchaseId);

                    if (purchase == null)
                    {
                        MessageBox.Show("Invoice not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Show payment confirmation dialog
                    var paymentMessage = $"💳 PAYMENT CONFIRMATION\n\n" +
                                       $"Invoice: {purchase.InvoiceReference}\n" +
                                       $"Supplier: {purchase.Supplier.Name}\n" +
                                       $"Vessel: {purchase.Vessel.Name}\n" +
                                       $"Due Date: {purchase.DueDate:dd/MM/yyyy}\n\n" +
                                       $"Amount to Pay: {purchase.TotalValue:N3} {purchase.Supplier.Currency}\n" +
                                       $"USD Equivalent: {purchase.TotalValueUSD:C2}\n\n" +
                                       $"Mark this invoice as PAID today?";

                    var result = MessageBox.Show(paymentMessage, "Confirm Payment",
                                               MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Mark as paid
                        purchase.PaymentDate = DateTime.Now;
                        purchase.PaymentAmount = purchase.TotalValue;
                        purchase.PaymentAmountUSD = purchase.TotalValueUSD;

                        await context.SaveChangesAsync();

                        // Create backup
                        await CreateAutoBackupAsync("InvoicePayment");

                        MessageBox.Show($"✅ Invoice {purchase.InvoiceReference} marked as PAID!\n\n" +
                                      $"Payment Date: {DateTime.Now:dd/MM/yyyy}\n" +
                                      $"Amount: {purchase.TotalValue:N3} {purchase.Supplier.Currency}",
                                      "Payment Recorded", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Refresh the payment report
                        await GeneratePaymentReportAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing payment: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task GeneratePaymentReportAsync()
        {
            try
            {
                var summary = await _alertService.GetPaymentSummaryAsync();
                var agingItems = await _alertService.GetPaymentAgingAnalysisAsync();
                var supplierSummaries = await _alertService.GetSupplierPaymentSummaryAsync();
                var paidInvoices = await _alertService.GetPaidInvoicesAsync();

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
                PaidInvoicesGrid.ItemsSource = paidInvoices; // NEW

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
                            e.Row.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 238)); // Light red
                            e.Row.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)); // Red text
                            break;
                        case AlertService.PaymentStatus.DueToday:
                            e.Row.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 243, 224)); // Light orange
                            e.Row.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(253, 126, 20)); // Orange text
                            break;
                        case AlertService.PaymentStatus.DueTomorrow:
                        case AlertService.PaymentStatus.DueThisWeek:
                            e.Row.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 252, 230)); // Light yellow
                            e.Row.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)); // Yellow text
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

        #region Inventory Valuation Report

        private async Task GenerateInventoryValuationAsync()
        {
            try
            {
                var inventoryReport = await _inventoryService.GenerateInventoryValuationAsync();

                // Update summary cards
                InventoryTotalLitersText.Text = $"{inventoryReport.Summary.TotalInventoryLiters:N3} L";
                InventoryTotalTonsText.Text = $"{inventoryReport.Summary.TotalInventoryTons:N3} T";
                InventoryTotalFIFOValueText.Text = inventoryReport.Summary.TotalFIFOValueUSD.ToString("C2");
                InventoryPurchaseLotsText.Text = inventoryReport.Summary.NumberOfPurchaseLots.ToString();
                InventoryAvgCostPerLiterText.Text = inventoryReport.Summary.AvgCostPerLiterUSD.ToString("C6");

                // Update grids
                VesselInventoryGrid.ItemsSource = inventoryReport.VesselInventory;
                SupplierInventoryGrid.ItemsSource = inventoryReport.SupplierInventory;
                PurchaseLotsGrid.ItemsSource = inventoryReport.PurchaseLots;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating inventory valuation: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void GenerateInventory_Click(object sender, RoutedEventArgs e)
        {
            await GenerateInventoryValuationAsync();
            await CreateAutoBackupAsync("InventoryValuationGenerated");
            MessageBox.Show("Inventory valuation report generated successfully!", "Success",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportInventory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Inventory valuation export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Fleet Efficiency Report

        private async Task GenerateFleetEfficiencyAsync()
        {
            try
            {
                var fromDate = FleetFromDatePicker.SelectedDate;
                var toDate = FleetToDatePicker.SelectedDate;

                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    MessageBox.Show("Please select both from and to dates.", "Validation Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (fromDate.Value > toDate.Value)
                {
                    MessageBox.Show("From date cannot be later than to date.", "Validation Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fleetReport = await _fleetEfficiencyService.GenerateFleetEfficiencyAnalysisAsync(fromDate.Value, toDate.Value);

                // Update Fleet Overview summary cards
                FleetActiveVesselsText.Text = fleetReport.Overview.TotalActiveVessels.ToString();
                FleetTotalLegsText.Text = fleetReport.Overview.TotalLegsCompleted.ToString();
                FleetAvgEfficiencyLText.Text = $"{fleetReport.Overview.AvgFleetEfficiencyLPerLeg:N3}";
                FleetAvgCostPerLegText.Text = fleetReport.Overview.AvgCostPerLegUSD < 0
                    ? $"({Math.Abs(fleetReport.Overview.AvgCostPerLegUSD):C2})"
                    : fleetReport.Overview.AvgCostPerLegUSD.ToString("C2");
                FleetTotalCostText.Text = fleetReport.Overview.TotalFleetCostUSD < 0
                    ? $"({Math.Abs(fleetReport.Overview.TotalFleetCostUSD):C2})"
                    : fleetReport.Overview.TotalFleetCostUSD.ToString("C2");

                // Update Best Performers cards
                FleetBestVesselText.Text = fleetReport.Overview.MostEfficientVessel;
                FleetBestRouteText.Text = fleetReport.Overview.BestRoute;
                FleetBestRouteEfficiencyText.Text = $"{fleetReport.Overview.BestRouteEfficiency:N3} L/Leg";

                // Update all grids
                VesselPerformanceGrid.ItemsSource = fleetReport.VesselEfficiency;
                RouteComparisonGrid.ItemsSource = fleetReport.RouteComparison;
                MonthlyTrendsGrid.ItemsSource = fleetReport.MonthlyTrends;
                EfficiencyRankingsGrid.ItemsSource = fleetReport.EfficiencyRankings;
                CostEfficiencyGrid.ItemsSource = fleetReport.CostEfficiency;
                SeasonalPatternsGrid.ItemsSource = fleetReport.SeasonalPatterns;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating fleet efficiency analysis: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void GenerateFleetEfficiency_Click(object sender, RoutedEventArgs e)
        {
            await GenerateFleetEfficiencyAsync();
            await CreateAutoBackupAsync("FleetEfficiencyGenerated");
            MessageBox.Show("Fleet efficiency analysis generated successfully!", "Success",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportFleetEfficiency_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fleet efficiency export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region FIFO Allocation Detail Report

        private async Task GenerateFIFOAllocationDetailAsync()
        {
            try
            {
                var fromDate = FIFOFromDatePicker.SelectedDate;
                var toDate = FIFOToDatePicker.SelectedDate;

                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    MessageBox.Show("Please select both from and to dates.", "Validation Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (fromDate.Value > toDate.Value)
                {
                    MessageBox.Show("From date cannot be later than to date.", "Validation Error",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fifoDetailReport = await _fifoDetailService.GenerateFIFOAllocationDetailAsync(fromDate.Value, toDate.Value);

                // Update Flow Summary
                UpdateFlowSummaryUI(fifoDetailReport.FlowSummary);

                // Update all grids
                AllocationRecordsGrid.ItemsSource = fifoDetailReport.AllocationRecords;
                PurchaseLotTrackingGrid.ItemsSource = fifoDetailReport.PurchaseLotTracking;
                PeriodAnalysisGrid.ItemsSource = fifoDetailReport.PeriodAnalysis;
                ExceptionReportGrid.ItemsSource = fifoDetailReport.Exceptions;

                // Update Balance Verification
                UpdateBalanceVerificationUI(fifoDetailReport.BalanceVerification);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating FIFO allocation detail report: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFlowSummaryUI(FIFOAllocationDetailService.AllocationFlowSummary summary)
        {
            // Flow Summary Cards
            FIFOTotalPurchasesText.Text = $"{summary.TotalPurchasesL:N3} L";
            FIFOTotalConsumptionText.Text = $"{summary.TotalConsumptionL:N3} L";
            FIFOTotalAllocatedText.Text = $"{summary.TotalAllocatedL:N3} L";
            FIFOTotalValueText.Text = summary.FormattedTotalFIFOValue;
            FIFOAccuracyText.Text = $"{summary.AllocationAccuracyPercentage:N1}%";

            // Activity Summary Cards
            FIFOTransactionsText.Text = summary.TotalAllocationTransactions.ToString();
            FIFOPurchaseLotsText.Text = summary.UniquePurchaseLots.ToString();
            FIFOVesselsText.Text = summary.VesselsInvolved.ToString();
            FIFOSuppliersText.Text = summary.SuppliersInvolved.ToString();

            if (summary.TotalAllocationTransactions > 0)
            {
                FIFODateRangeText.Text = $"{summary.FormattedOldestPurchase} → {summary.FormattedLatestConsumption}";
            }
            else
            {
                FIFODateRangeText.Text = "No Data";
            }
        }

        private void UpdateBalanceVerificationUI(FIFOAllocationDetailService.BalanceVerificationResult verification)
        {
            // Balance Status
            BalanceStatusText.Text = verification.BalanceStatus;
            BalanceStatusText.Foreground = verification.IsBalanced
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;

            DataIntegrityScoreText.Text = $"{verification.DataIntegrityScore:N1}%";
            DataIntegrityScoreText.Foreground = verification.DataIntegrityScore >= 95
                ? System.Windows.Media.Brushes.Green
                : verification.DataIntegrityScore >= 90
                    ? System.Windows.Media.Brushes.Orange
                    : System.Windows.Media.Brushes.Red;

            InconsistenciesText.Text = verification.InconsistentAllocations.ToString();
            InconsistenciesText.Foreground = verification.InconsistentAllocations == 0
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;

            DataGradeText.Text = verification.DataIntegrityGrade;
            DataGradeText.Foreground = verification.DataIntegrityGrade == "Excellent"
                ? System.Windows.Media.Brushes.Green
                : verification.DataIntegrityGrade == "Good"
                    ? System.Windows.Media.Brushes.DarkGreen
                    : verification.DataIntegrityGrade == "Fair"
                        ? System.Windows.Media.Brushes.Orange
                        : System.Windows.Media.Brushes.Red;

            // Balance Details
            FIFOTotalPurchaseQuantityText.Text = $"{verification.TotalPurchaseQuantity:N3}";
            FIFOTotalConsumptionQuantityText.Text = $"{verification.TotalConsumptionQuantity:N3}";
            QuantityVarianceText.Text = verification.FormattedQuantityVariance;
            QuantityVarianceText.Foreground = Math.Abs(verification.QuantityVariance) < 0.001m
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;

            FIFOTotalPurchaseValueText.Text = verification.TotalPurchaseValue.ToString("C2");
            FIFOTotalConsumptionValueText.Text = verification.TotalConsumptionValue.ToString("C2");
            ValueVarianceText.Text = verification.FormattedValueVariance;
            ValueVarianceText.Foreground = Math.Abs(verification.ValueVariance) < 0.01m
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;

            // Balance Issues - FIXED VERSION
            var issuesDisplay = new List<string>();

            if (verification.BalanceIssues.Any())
            {
                issuesDisplay.AddRange(verification.BalanceIssues);
            }
            else
            {
                issuesDisplay.Add("✅ No balance issues detected");
            }

            BalanceIssuesList.ItemsSource = issuesDisplay;
        }

        private async void GenerateFIFODetail_Click(object sender, RoutedEventArgs e)
        {
            await GenerateFIFOAllocationDetailAsync();
            await CreateAutoBackupAsync("FIFODetailGenerated");
            MessageBox.Show("FIFO allocation detail report generated successfully!", "Success",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportFIFODetail_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("FIFO allocation detail export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Cost Analysis Report

        private async Task GenerateCostAnalysisAsync()
        {
            try
            {
                var fromDate = CostFromDatePicker.SelectedDate;
                var toDate = CostToDatePicker.SelectedDate;

                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    MessageBox.Show("Please select both from and to dates for cost analysis.", "Date Required",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (fromDate > toDate)
                {
                    MessageBox.Show("From date cannot be later than to date.", "Invalid Date Range",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var costAnalysis = await _costAnalysisService.GenerateCostAnalysisAsync(fromDate, toDate);

                // Update Overview
                UpdateCostAnalysisOverview(costAnalysis.Overview);

                // Update all grids
                PriceTrendsGrid.ItemsSource = costAnalysis.PriceTrends;
                SupplierComparisonGrid.ItemsSource = costAnalysis.SupplierComparison;
                CostVarianceGrid.ItemsSource = costAnalysis.CostVariance;
                ProcurementEfficiencyGrid.ItemsSource = costAnalysis.ProcurementEfficiency;
                MarketBenchmarkingGrid.ItemsSource = costAnalysis.MarketBenchmarking;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating cost analysis: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCostAnalysisOverview(CostAnalysisService.CostAnalysisOverview overview)
        {
            // Update summary cards
            CostTotalProcurementText.Text = overview.FormattedTotalProcurement;
            CostAvgPerLiterText.Text = overview.AvgCostPerLiterUSD.ToString("C6");
            CostAvgPerTonText.Text = overview.AvgCostPerTonUSD.ToString("C2");
            CostVolatilityText.Text = $"{overview.PriceVolatilityIndex:N1}%";
            CostEfficiencyScoreText.Text = overview.FormattedProcurementScore;

            // Update key metrics
            CostBestSupplierText.Text = $"Best Supplier: {overview.BestPerformingSupplier}";
            CostWorstSupplierText.Text = $"Worst Supplier: {overview.WorstPerformingSupplier}";
            CostSavingsOpportunityText.Text = $"Savings Opportunity: {overview.FormattedCostSavings}";
            CostUniqueSuppliersText.Text = $"Unique Suppliers: {overview.UniqueSuppliersUsed}";

            CostLowestPriceText.Text = $"Lowest Price: {overview.LowestCostPerLiterUSD:C6}";
            CostHighestPriceText.Text = $"Highest Price: {overview.HighestCostPerLiterUSD:C6}";
            CostMostEfficientMonthText.Text = $"Most Efficient Month: {overview.MostCostEfficientMonth}";
            CostTotalTransactionsText.Text = $"Total Transactions: {overview.TotalPurchaseTransactions}";
        }

        private async void GenerateCostAnalysis_Click(object sender, RoutedEventArgs e)
        {
            await GenerateCostAnalysisAsync();
            await CreateAutoBackupAsync("CostAnalysisGenerated");
            MessageBox.Show("Cost analysis report generated successfully!", "Success",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportCostAnalysis_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Cost analysis export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Route Performance Report

        private async Task GenerateRoutePerformanceAsync()
        {
            try
            {
                var fromDate = RouteFromDatePicker.SelectedDate;
                var toDate = RouteToDatePicker.SelectedDate;

                if (!fromDate.HasValue || !toDate.HasValue)
                {
                    MessageBox.Show("Please select both from and to dates for route performance analysis.", "Date Required",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (fromDate > toDate)
                {
                    MessageBox.Show("From date cannot be later than to date.", "Invalid Date Range",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var routePerformance = await _routePerformanceService.GenerateRoutePerformanceAsync(fromDate, toDate);

                // Update Overview
                UpdateRoutePerformanceOverview(routePerformance.Overview);

                // Update all grids
                RoutePerformanceComparisonGrid.ItemsSource = routePerformance.RouteComparisons;
                RoutePerformanceVesselGrid.ItemsSource = routePerformance.VesselPerformance;
                RoutePerformanceTrendsGrid.ItemsSource = routePerformance.EfficiencyTrends;
                RoutePerformanceCostGrid.ItemsSource = routePerformance.CostAnalysis;
                RoutePerformanceOptimizationGrid.ItemsSource = routePerformance.OptimizationRecommendations;
                RoutePerformanceProfitabilityGrid.ItemsSource = routePerformance.ProfitabilityAnalysis;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating route performance analysis: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRoutePerformanceOverview(RoutePerformanceService.RoutePerformanceOverview overview)
        {
            // Update summary cards
            RouteTotalActiveRoutesText.Text = overview.TotalActiveRoutes.ToString();
            RouteTotalLegsText.Text = overview.TotalLegsCompleted.ToString("N0");
            RouteTotalDistanceText.Text = $"{overview.TotalRouteDistanceKm:N0} km";
            RouteTotalCostText.Text = overview.FormattedTotalCost;

            // Update performance metrics
            RouteMostEfficientText.Text = $"Most Efficient: {overview.MostEfficientRoute}";
            RouteLeastEfficientText.Text = $"Least Efficient: {overview.LeastEfficientRoute}";
            RouteEfficiencyGapText.Text = $"Efficiency Gap: {overview.FormattedRouteGap}";
            RouteAvgCostPerKmText.Text = $"Avg Cost/km: {overview.AvgCostPerKm:C6}";

            RouteMostProfitableText.Text = $"Most Profitable: {overview.MostProfitableRoute}";
            RouteLeastProfitableText.Text = $"Least Profitable: {overview.LeastProfitableRoute}";
            RouteAvgFuelPerKmText.Text = $"Avg Fuel/km: {overview.AvgFuelPerKm:N3} L";
            RouteBestEfficiencyText.Text = $"Best Efficiency: {overview.BestRouteEfficiencyLPerLeg:N3} L/leg";
        }

        private async void GenerateRoutePerformance_Click(object sender, RoutedEventArgs e)
        {
            await GenerateRoutePerformanceAsync();
            await CreateAutoBackupAsync("RoutePerformanceGenerated");
            MessageBox.Show("Route performance report generated successfully!", "Success",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportRoutePerformance_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Route performance export feature coming soon!", "Export",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        // Replace the existing export button handlers with these corrected versions:

        #region Excel Export Handlers

        // Monthly Summary Export - replace existing Export_Click method
        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSummary == null)
            {
                MessageBox.Show("Please generate a report first.", "No Data",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var filePath = await _excelExportService.ExportMonthlySummaryToExcelAsync(_currentSummary, _currentSummary.Month);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Monthly summary exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Vessel Account Export - replace existing ExportVessel_Click method
        private async void ExportVessel_Click(object sender, RoutedEventArgs e)
        {
            if (VesselAccountComboBox.SelectedItem is not Vessel selectedVessel || _currentVesselAccount == null)
            {
                MessageBox.Show("Please generate a vessel account report first.", "No Data",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var fromDate = VesselFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = VesselToDatePicker.SelectedDate ?? DateTime.Today;

                var filePath = await _excelExportService.ExportVesselAccountToExcelAsync(_currentVesselAccount, selectedVessel.Name, fromDate, toDate);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Vessel account exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Supplier Account Export - replace existing ExportSupplier_Click method
        private async void ExportSupplier_Click(object sender, RoutedEventArgs e)
        {
            if (SupplierAccountComboBox.SelectedItem is not Supplier selectedSupplier || _currentSupplierAccount == null)
            {
                MessageBox.Show("Please generate a supplier account report first.", "No Data",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var fromDate = SupplierFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = SupplierToDatePicker.SelectedDate ?? DateTime.Today;

                var filePath = await _excelExportService.ExportSupplierAccountToExcelAsync(_currentSupplierAccount, selectedSupplier.Name, fromDate, toDate);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Supplier account exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Payment Due Export - replace existing ExportPayment_Click method
        private async void ExportPayment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var alerts = await _alertService.GetDueDateAlertsAsync();

                var filePath = await _excelExportService.ExportPaymentDueToExcelAsync(alerts);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Payment due report exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Inventory Valuation Export - replace existing ExportInventory_Click method
        private async void ExportInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inventoryReport = await _inventoryService.GenerateInventoryValuationAsync();

                var filePath = await _excelExportService.ExportInventoryValuationToExcelAsync(inventoryReport);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Inventory valuation exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fleet Efficiency Export - replace existing ExportFleet_Click method
        private async void ExportFleet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fromDate = FleetFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = FleetToDatePicker.SelectedDate ?? DateTime.Today;

                var fleetEfficiency = await _fleetEfficiencyService.GenerateFleetEfficiencyReportAsync(fromDate, toDate);

                var filePath = await _excelExportService.ExportFleetEfficiencyToExcelAsync(fleetEfficiency, fromDate, toDate);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Fleet efficiency exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // FIFO Allocation Export - replace existing ExportFIFO_Click method
        private async void ExportFIFO_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fromDate = FIFOFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-6);
                var toDate = FIFOToDatePicker.SelectedDate ?? DateTime.Today;

                var fifoDetail = await _fifoDetailService.GenerateFIFOAllocationDetailAsync(fromDate, toDate);

                var filePath = await _excelExportService.ExportFIFOAllocationDetailToExcelAsync(fifoDetail, fromDate, toDate);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"FIFO allocation detail exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Cost Analysis Export - replace existing ExportCostAnalysis_Click method
        private async void ExportCostAnalysis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fromDate = CostFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = CostToDatePicker.SelectedDate ?? DateTime.Today;

                var costAnalysis = await _costAnalysisService.GenerateCostAnalysisAsync(fromDate, toDate);

                var filePath = await _excelExportService.ExportCostAnalysisToExcelAsync(costAnalysis, fromDate, toDate);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Cost analysis exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Route Performance Export - replace existing ExportRoutePerformance_Click method
        private async void ExportRoutePerformance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fromDate = RouteFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = RouteToDatePicker.SelectedDate ?? DateTime.Today;

                var routePerformance = await _routePerformanceService.GenerateRoutePerformanceAsync(fromDate, toDate);

                var filePath = await _excelExportService.ExportRoutePerformanceToExcelAsync(routePerformance, fromDate, toDate);

                if (!string.IsNullOrEmpty(filePath))
                {
                    var result = MessageBox.Show($"Route performance exported successfully!\n\nFile saved to: {filePath}\n\nWould you like to open the file?",
                                               "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}