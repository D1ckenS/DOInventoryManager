using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using DOInventoryManager.Data;
using DOInventoryManager.Models;
using DOInventoryManager.Services;
using Microsoft.EntityFrameworkCore;
using DOInventoryManager.Views.Print;

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
            
            // Smooth scrolling temporarily disabled due to scroll conflicts
            // SmoothScrollingService.AutoEnableSmoothScrolling(this);
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

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tabControl = this.FindName("ReportsTabControl") as TabControl;
                if (tabControl?.SelectedItem is TabItem selectedTab)
                {
                    string reportTitle = selectedTab.Header.ToString() ?? "Report";

                    // Handle Monthly Summary specifically with actual data
                    if (reportTitle.Contains("Monthly Summary") && _currentSummary != null)
                    {
                        // Create Monthly Summary print layout
                        var printLayout = new Views.Print.MonthlySummaryPrint();
                        string selectedMonth = MonthComboBox.SelectedItem?.ToString() ?? "Unknown";

                        // Load actual data instead of extracting from UI
                        await printLayout.LoadSummaryData(_currentSummary, selectedMonth);

                        // Force layout update
                        printLayout.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        printLayout.Arrange(new Rect(printLayout.DesiredSize));
                        printLayout.UpdateLayout();

                        // Use print service
                        var printService = new Services.PrintService();
                        var result = printService.ShowPrintPreview(printLayout, $"Monthly Summary - {selectedMonth}");

                        if (!result.Success && !result.Message.Contains("cancelled"))
                        {
                            MessageBox.Show(result.Message, "Print Error",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }

                    // Handle other report types using GenericReportPrint
                    await HandleGenericReportPrintAsync(reportTitle);
                }
                else
                {
                    MessageBox.Show("Please select a report tab first.", "No Report Selected",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PrintVesselAccount_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tabControl = this.FindName("ReportsTabControl") as TabControl;
                if (tabControl?.SelectedItem is TabItem selectedTab)
                {
                    string reportTitle = selectedTab.Header.ToString() ?? "Report";

                    // Handle Vessel Account Statement
                    if (reportTitle.Contains("Vessel Account") && _currentVesselAccount != null)
                    {
                        // Get selected vessel and date range
                        var selectedVessel = VesselAccountComboBox.SelectedItem as Vessel;
                        var fromDate = VesselFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                        var toDate = VesselToDatePicker.SelectedDate ?? DateTime.Today;

                        if (selectedVessel == null)
                        {
                            MessageBox.Show("Please select a vessel first.", "No Vessel Selected",
                                          MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Create Vessel Account print layout
                        var printLayout = new Views.Print.VesselAccountPrint();

                        // Load actual data
                        await printLayout.LoadVesselAccountData(_currentVesselAccount, selectedVessel.Name, fromDate, toDate);

                        // Force layout update
                        printLayout.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        printLayout.Arrange(new Rect(printLayout.DesiredSize));
                        printLayout.UpdateLayout();

                        // Use print service
                        var printService = new Services.PrintService();
                        var result = printService.ShowPrintPreview(printLayout, $"Vessel Account - {selectedVessel.Name}");

                        if (!result.Success && !result.Message.Contains("cancelled"))
                        {
                            MessageBox.Show(result.Message, "Print Error",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }

                    // Handle Supplier Account Report specifically
                    if (reportTitle.Contains("Supplier Account") && _currentSupplierAccount != null)
                    {
                        await HandleSupplierAccountPrintAsync();
                        return;
                    }

                    // Handle other report types using GenericReportPrint
                    await HandleGenericReportPrintAsync(reportTitle);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        private async void ExportFleetEfficiency_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fromDate = FleetFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = FleetToDatePicker.SelectedDate ?? DateTime.Today;

                var fleetEfficiency = await _fleetEfficiencyService.GenerateFleetEfficiencyAnalysisAsync(fromDate, toDate);

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
        private async void ExportFIFODetail_Click(object sender, RoutedEventArgs e)
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

        #region Additional Print Handlers

        private Task HandleSupplierAccountPrintAsync()
        {
            try
            {
                // Get selected supplier and date range
                var selectedSupplier = SupplierAccountComboBox.SelectedItem as Supplier;
                var fromDate = SupplierFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = SupplierToDatePicker.SelectedDate ?? DateTime.Today;

                if (selectedSupplier == null)
                {
                    MessageBox.Show("Please select a supplier first.", "No Supplier Selected",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return Task.CompletedTask;
                }

                // Create generic print layout
                var printLayout = new Views.Print.GenericReportPrint();
                
                // Set title
                printLayout.SetReportTitle($"Supplier Account Report - {selectedSupplier.Name}");
                
                // Add summary cards
                printLayout.AddSummaryCard("Beginning Balance", _currentSupplierAccount?.Summary?.FormattedBeginningBalance ?? "N/A");
                printLayout.AddSummaryCard("Period Purchases", $"{_currentSupplierAccount?.Summary?.PeriodPurchases:N3} L");
                printLayout.AddSummaryCard("Period Consumption", $"{_currentSupplierAccount?.Summary?.PeriodConsumption:N3} L");
                printLayout.AddSummaryCard("Ending Balance", _currentSupplierAccount?.Summary?.FormattedEndingBalance ?? "N/A");
                
                // Add date range info
                printLayout.AddTextBlock($"Period: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}", true);
                printLayout.AddSeparator();
                
                // Add transactions data
                printLayout.AddDataGrid(SupplierAccountGrid, "Account Transactions");
                
                // Add totals section
                printLayout.AddSectionTitle("Account Summary");
                printLayout.AddTextBlock($"Total Purchases: {_currentSupplierAccount?.FormattedTotalPurchases ?? "N/A"}");
                printLayout.AddTextBlock($"Total Consumption: {_currentSupplierAccount?.FormattedTotalConsumption ?? "N/A"}");
                printLayout.AddTextBlock($"Net Balance: {_currentSupplierAccount?.FormattedNetBalance ?? "N/A"}");
                printLayout.AddTextBlock($"Total Value: {_currentSupplierAccount?.FormattedTotalValue ?? "N/A"}");
                printLayout.AddTextBlock($"Total Value (USD): {_currentSupplierAccount?.FormattedTotalValueUSD ?? "N/A"}");

                // Force layout update
                printLayout.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                printLayout.Arrange(new Rect(printLayout.DesiredSize));
                printLayout.UpdateLayout();

                // Use print service
                var printService = new Services.PrintService();
                var result = printService.ShowPrintPreview(printLayout, $"Supplier Account - {selectedSupplier.Name}");

                if (!result.Success && !result.Message.Contains("cancelled"))
                {
                    MessageBox.Show(result.Message, "Print Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing supplier account: {ex.Message}", "Print Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return Task.CompletedTask;
            }
        }

        private async Task HandleGenericReportPrintAsync(string reportTitle)
        {
            try
            {
                var printLayout = new Views.Print.GenericReportPrint();
                printLayout.SetReportTitle(reportTitle);

                // Handle different report types
                if (reportTitle.Contains("Payment Due"))
                {
                    await HandlePaymentDuePrintAsync(printLayout);
                }
                else if (reportTitle.Contains("Inventory Valuation"))
                {
                    await HandleInventoryValuationPrintAsync(printLayout);
                }
                else if (reportTitle.Contains("Fleet Efficiency"))
                {
                    await HandleFleetEfficiencyPrintAsync(printLayout);
                }
                else if (reportTitle.Contains("FIFO Allocation Detail"))
                {
                    await HandleFIFODetailPrintAsync(printLayout);
                }
                else if (reportTitle.Contains("Cost Analysis"))
                {
                    await HandleCostAnalysisPrintAsync(printLayout);
                }
                else if (reportTitle.Contains("Route Performance"))
                {
                    await HandleRoutePerformancePrintAsync(printLayout);
                }
                else
                {
                    // Default handling for unknown report types
                    printLayout.AddTextBlock("This report type is not yet configured for printing.", true);
                    printLayout.AddTextBlock("Please use the Excel export functionality instead.");
                }

                // Force layout update
                printLayout.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                printLayout.Arrange(new Rect(printLayout.DesiredSize));
                printLayout.UpdateLayout();

                // Use print service
                var printService = new Services.PrintService();
                var result = printService.ShowPrintPreview(printLayout, reportTitle);

                if (!result.Success && !result.Message.Contains("cancelled"))
                {
                    MessageBox.Show(result.Message, "Print Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing report: {ex.Message}", "Print Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task HandlePaymentDuePrintAsync(Views.Print.GenericReportPrint printLayout)
        {
            try
            {
                var summary = await _alertService.GetPaymentSummaryAsync();
                
                // Add summary cards
                printLayout.AddSummaryCard("Overdue", summary.TotalOverdueAmount.ToString("C2"), $"{summary.OverdueCount} invoices");
                printLayout.AddSummaryCard("Due Today", summary.DueTodayAmount.ToString("C2"), $"{summary.DueTodayCount} invoices");
                printLayout.AddSummaryCard("Due This Week", summary.DueThisWeekAmount.ToString("C2"), $"{summary.DueThisWeekCount} invoices");
                printLayout.AddSummaryCard("Total Outstanding", summary.TotalOutstandingAmount.ToString("C2"), $"{summary.TotalOutstandingCount} invoices");
                
                printLayout.AddSeparator();
                
                // Add payment schedule grid
                printLayout.AddDataGrid(PaymentScheduleGrid, "Payment Schedule");
                
                // Add aging analysis grid
                printLayout.AddDataGrid(AgingAnalysisGrid, "Aging Analysis by Supplier");
            }
            catch (Exception ex)
            {
                printLayout.AddTextBlock($"Error loading payment data: {ex.Message}");
            }
        }

        private async Task HandleInventoryValuationPrintAsync(Views.Print.GenericReportPrint printLayout)
        {
            try
            {
                var inventoryReport = await _inventoryService.GenerateInventoryValuationAsync();
                
                // Add summary cards
                printLayout.AddSummaryCard("Total Inventory", $"{inventoryReport.Summary.TotalInventoryLiters:N3} L");
                printLayout.AddSummaryCard("Total Weight", $"{inventoryReport.Summary.TotalInventoryTons:N3} T");
                printLayout.AddSummaryCard("FIFO Value", inventoryReport.Summary.TotalFIFOValueUSD.ToString("C2"));
                printLayout.AddSummaryCard("Purchase Lots", inventoryReport.Summary.NumberOfPurchaseLots.ToString());
                printLayout.AddSummaryCard("Avg Cost/L", inventoryReport.Summary.AvgCostPerLiterUSD.ToString("C6"));
                
                printLayout.AddSeparator();
                
                // Add inventory grids - use compact layout for detailed grids
                printLayout.AddDataGrid(VesselInventoryGrid, "Inventory by Vessel");
                printLayout.AddDataGrid(SupplierInventoryGrid, "Inventory by Supplier");
                printLayout.AddCompactDataGrid(PurchaseLotsGrid, "Purchase Lots Detail", 7);
            }
            catch (Exception ex)
            {
                printLayout.AddTextBlock($"Error loading inventory data: {ex.Message}");
            }
        }

        private async Task HandleFleetEfficiencyPrintAsync(Views.Print.GenericReportPrint printLayout)
        {
            try
            {
                var fromDate = FleetFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = FleetToDatePicker.SelectedDate ?? DateTime.Today;
                var fleetReport = await _fleetEfficiencyService.GenerateFleetEfficiencyAnalysisAsync(fromDate, toDate);
                
                // Add date range
                printLayout.AddTextBlock($"Period: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}", true);
                printLayout.AddSeparator();
                
                // Add summary cards
                printLayout.AddSummaryCard("Active Vessels", fleetReport.Overview.TotalActiveVessels.ToString());
                printLayout.AddSummaryCard("Total Legs", fleetReport.Overview.TotalLegsCompleted.ToString());
                printLayout.AddSummaryCard("Avg Efficiency", $"{fleetReport.Overview.AvgFleetEfficiencyLPerLeg:N3} L/leg");
                printLayout.AddSummaryCard("Avg Cost/Leg", fleetReport.Overview.AvgCostPerLegUSD.ToString("C2"));
                
                // Add best performers info
                printLayout.AddTextBlock($"Most Efficient Vessel: {fleetReport.Overview.MostEfficientVessel}", true);
                printLayout.AddTextBlock($"Best Route: {fleetReport.Overview.BestRoute} ({fleetReport.Overview.BestRouteEfficiency:N3} L/Leg)");
                
                printLayout.AddSeparator();
                
                // Add data grids - use compact for complex performance data
                printLayout.AddCompactDataGrid(VesselPerformanceGrid, "Vessel Performance", 8);
                printLayout.AddDataGrid(RouteComparisonGrid, "Route Comparison");
                printLayout.AddDataGrid(MonthlyTrendsGrid, "Monthly Trends");
            }
            catch (Exception ex)
            {
                printLayout.AddTextBlock($"Error loading fleet efficiency data: {ex.Message}");
            }
        }

        private async Task HandleFIFODetailPrintAsync(Views.Print.GenericReportPrint printLayout)
        {
            try
            {
                var fromDate = FIFOFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-6);
                var toDate = FIFOToDatePicker.SelectedDate ?? DateTime.Today;
                var fifoReport = await _fifoDetailService.GenerateFIFOAllocationDetailAsync(fromDate, toDate);
                
                // Add date range
                printLayout.AddTextBlock($"Period: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}", true);
                printLayout.AddSeparator();
                
                // Add flow summary cards
                printLayout.AddSummaryCard("Total Purchases", $"{fifoReport.FlowSummary.TotalPurchasesL:N3} L");
                printLayout.AddSummaryCard("Total Consumption", $"{fifoReport.FlowSummary.TotalConsumptionL:N3} L");
                printLayout.AddSummaryCard("Total Allocated", $"{fifoReport.FlowSummary.TotalAllocatedL:N3} L");
                printLayout.AddSummaryCard("FIFO Value", fifoReport.FlowSummary.FormattedTotalFIFOValue);
                printLayout.AddSummaryCard("Accuracy", $"{fifoReport.FlowSummary.AllocationAccuracyPercentage:N1}%");
                
                // Add balance verification info
                printLayout.AddTextBlock($"Balance Status: {fifoReport.BalanceVerification.BalanceStatus}", true);
                printLayout.AddTextBlock($"Data Integrity Score: {fifoReport.BalanceVerification.DataIntegrityScore:N1}%");
                
                printLayout.AddSeparator();
                
                // Add data grids - use compact for detailed allocation data
                printLayout.AddCompactDataGrid(AllocationRecordsGrid, "Allocation Records", 7);
                printLayout.AddCompactDataGrid(PurchaseLotTrackingGrid, "Purchase Lot Tracking", 6);
            }
            catch (Exception ex)
            {
                printLayout.AddTextBlock($"Error loading FIFO detail data: {ex.Message}");
            }
        }

        private async Task HandleCostAnalysisPrintAsync(Views.Print.GenericReportPrint printLayout)
        {
            try
            {
                var fromDate = CostFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = CostToDatePicker.SelectedDate ?? DateTime.Today;
                var costAnalysis = await _costAnalysisService.GenerateCostAnalysisAsync(fromDate, toDate);
                
                // Add date range
                printLayout.AddTextBlock($"Period: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}", true);
                printLayout.AddSeparator();
                
                // Add overview cards
                printLayout.AddSummaryCard("Total Procurement", costAnalysis.Overview.FormattedTotalProcurement);
                printLayout.AddSummaryCard("Avg Cost/L", costAnalysis.Overview.AvgCostPerLiterUSD.ToString("C6"));
                printLayout.AddSummaryCard("Avg Cost/T", costAnalysis.Overview.AvgCostPerTonUSD.ToString("C2"));
                printLayout.AddSummaryCard("Price Volatility", $"{costAnalysis.Overview.PriceVolatilityIndex:N1}%");
                printLayout.AddSummaryCard("Efficiency Score", costAnalysis.Overview.FormattedProcurementScore);
                
                // Add key insights
                printLayout.AddTextBlock($"Best Supplier: {costAnalysis.Overview.BestPerformingSupplier}", true);
                printLayout.AddTextBlock($"Savings Opportunity: {costAnalysis.Overview.FormattedCostSavings}");
                printLayout.AddTextBlock($"Price Range: {costAnalysis.Overview.LowestCostPerLiterUSD:C6} - {costAnalysis.Overview.HighestCostPerLiterUSD:C6}");
                
                printLayout.AddSeparator();
                
                // Add data grids - use compact for detailed cost analysis
                printLayout.AddDataGrid(PriceTrendsGrid, "Price Trends");
                printLayout.AddCompactDataGrid(SupplierComparisonGrid, "Supplier Comparison", 8);
                printLayout.AddDataGrid(CostVarianceGrid, "Cost Variance Analysis");
            }
            catch (Exception ex)
            {
                printLayout.AddTextBlock($"Error loading cost analysis data: {ex.Message}");
            }
        }

        private async Task HandleRoutePerformancePrintAsync(Views.Print.GenericReportPrint printLayout)
        {
            try
            {
                var fromDate = RouteFromDatePicker.SelectedDate ?? DateTime.Today.AddMonths(-12);
                var toDate = RouteToDatePicker.SelectedDate ?? DateTime.Today;
                var routePerformance = await _routePerformanceService.GenerateRoutePerformanceAsync(fromDate, toDate);
                
                // Add date range
                printLayout.AddTextBlock($"Period: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}", true);
                printLayout.AddSeparator();
                
                // Add overview cards
                printLayout.AddSummaryCard("Active Routes", routePerformance.Overview.TotalActiveRoutes.ToString());
                printLayout.AddSummaryCard("Total Legs", routePerformance.Overview.TotalLegsCompleted.ToString("N0"));
                printLayout.AddSummaryCard("Total Distance", $"{routePerformance.Overview.TotalRouteDistanceKm:N0} km");
                printLayout.AddSummaryCard("Total Cost", routePerformance.Overview.FormattedTotalCost);
                
                // Add performance insights
                printLayout.AddTextBlock($"Most Efficient Route: {routePerformance.Overview.MostEfficientRoute}", true);
                printLayout.AddTextBlock($"Most Profitable Route: {routePerformance.Overview.MostProfitableRoute}");
                printLayout.AddTextBlock($"Average Cost per km: {routePerformance.Overview.AvgCostPerKm:C6}");
                printLayout.AddTextBlock($"Best Efficiency: {routePerformance.Overview.BestRouteEfficiencyLPerLeg:N3} L/leg");
                
                printLayout.AddSeparator();
                
                // Add data grids - use compact for complex route performance data
                printLayout.AddCompactDataGrid(RoutePerformanceComparisonGrid, "Route Performance Comparison", 7);
                printLayout.AddCompactDataGrid(RoutePerformanceVesselGrid, "Vessel Performance by Route", 8);
                printLayout.AddDataGrid(RoutePerformanceTrendsGrid, "Efficiency Trends");
            }
            catch (Exception ex)
            {
                printLayout.AddTextBlock($"Error loading route performance data: {ex.Message}");
            }
        }

        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            // Check if DataGrid needs internal scrolling first
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

            // DataGrid doesn't need scrolling, bubble up through ScrollViewer hierarchy
            e.Handled = true;
            BubbleScrollToParentScrollViewers(dataGrid, e.Delta, e.MouseDevice, e.Timestamp);
        }

        private void BubbleScrollToParentScrollViewers(DependencyObject startElement, int delta, System.Windows.Input.MouseDevice mouseDevice, int timestamp)
        {
            var currentElement = startElement;
            
            // Find all parent ScrollViewers and try scrolling them in order
            while (currentElement != null)
            {
                var parentScrollViewer = FindParent<ScrollViewer>(currentElement);
                if (parentScrollViewer == null)
                    break;

                // Check if this ScrollViewer can handle the scroll
                if (CanScrollViewerHandle(parentScrollViewer, delta))
                {
                    // Found a ScrollViewer that can handle the scroll, send event to it
                    var newEvent = new System.Windows.Input.MouseWheelEventArgs(mouseDevice, timestamp, delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent
                    };
                    parentScrollViewer.RaiseEvent(newEvent);
                    return; // Successfully handled, stop bubbling
                }

                // This ScrollViewer can't handle it, continue to its parent
                currentElement = parentScrollViewer;
            }
        }

        private bool CanScrollViewerHandle(ScrollViewer scrollViewer, int delta)
        {
            if (scrollViewer == null) return false;

            // Check if ScrollViewer can scroll in the requested direction
            if (delta < 0) // Scrolling down
            {
                return scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;
            }
            else // Scrolling up
            {
                return scrollViewer.VerticalOffset > 0;
            }
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