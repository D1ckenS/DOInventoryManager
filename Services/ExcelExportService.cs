using DOInventoryManager.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;

namespace DOInventoryManager.Services
{
    public class ExcelExportService
    {
        public ExcelExportService()
        {
            // Set EPPlus license context (fixed for EPPlus 7+)
            if (ExcelPackage.LicenseContext == OfficeOpenXml.LicenseContext.Commercial)
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            }
        }

        #region Monthly Summary Export

        public async Task<string> ExportMonthlySummaryToExcelAsync(SummaryService.MonthlySummaryResult summary, string month)
        {
            using var package = new ExcelPackage();

            // Executive Summary Worksheet
            var execSheet = package.Workbook.Worksheets.Add("Executive Summary");
            CreateExecutiveSummarySheet(execSheet, summary.ExecutiveSummary, month);

            // Consumption Summary Worksheet
            var consumptionSheet = package.Workbook.Worksheets.Add("Consumption Summary");
            CreateConsumptionSummarySheet(consumptionSheet, summary.ConsumptionSummary);

            // Purchase Summary Worksheet
            var purchaseSheet = package.Workbook.Worksheets.Add("Purchase Summary");
            CreatePurchaseSummarySheet(purchaseSheet, summary.PurchaseSummary);

            // Allocation Summary Worksheet
            var allocationSheet = package.Workbook.Worksheets.Add("Allocation Summary");
            CreateAllocationSummarySheet(allocationSheet, summary.AllocationSummary);

            // Financial Summary Worksheet
            var financialSheet = package.Workbook.Worksheets.Add("Financial Summary");
            CreateFinancialSummarySheet(financialSheet, summary.FinancialSummary);

            return await SaveExcelFileAsync(package, $"Monthly_Summary_{month}.xlsx");
        }

        #endregion

        #region Vessel Account Export

        public async Task<string> ExportVesselAccountToExcelAsync(ReportService.VesselAccountStatementResult vesselAccount, string vesselName, DateTime fromDate, DateTime toDate)
        {
            using var package = new ExcelPackage();

            // Account Statement Worksheet
            var accountSheet = package.Workbook.Worksheets.Add("Account Statement");
            CreateVesselAccountSheet(accountSheet, vesselAccount, vesselName, fromDate, toDate);

            return await SaveExcelFileAsync(package, $"Vessel_Account_{vesselName}_{fromDate:yyyyMM}-{toDate:yyyyMM}.xlsx");
        }

        #endregion

        #region Supplier Account Export

        public async Task<string> ExportSupplierAccountToExcelAsync(ReportService.SupplierAccountReportResult supplierAccount, string supplierName, DateTime fromDate, DateTime toDate)
        {
            using var package = new ExcelPackage();

            // Supplier Account Worksheet
            var accountSheet = package.Workbook.Worksheets.Add("Supplier Account");
            CreateSupplierAccountSheet(accountSheet, supplierAccount, supplierName, fromDate, toDate);

            return await SaveExcelFileAsync(package, $"Supplier_Account_{supplierName}_{fromDate:yyyyMM}-{toDate:yyyyMM}.xlsx");
        }

        #endregion

        #region Payment Due Export

        public async Task<string> ExportPaymentDueToExcelAsync(List<AlertService.DueDateAlert> alerts)
        {
            using var package = new ExcelPackage();

            // Payment Schedule Worksheet
            var scheduleSheet = package.Workbook.Worksheets.Add("Payment Due");
            CreatePaymentDueSheet(scheduleSheet, alerts);

            return await SaveExcelFileAsync(package, $"Payment_Due_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        #endregion

        #region Inventory Valuation Export

        public async Task<string> ExportInventoryValuationToExcelAsync(InventoryValuationService.InventoryValuationResult inventory)
        {
            using var package = new ExcelPackage();

            // Overview Worksheet
            var overviewSheet = package.Workbook.Worksheets.Add("Overview");
            CreateInventoryOverviewSheet(overviewSheet, inventory.Summary);

            // By Vessel Worksheet
            var vesselSheet = package.Workbook.Worksheets.Add("By Vessel");
            CreateInventoryByVesselSheet(vesselSheet, inventory.VesselInventory);

            // By Supplier Worksheet
            var supplierSheet = package.Workbook.Worksheets.Add("By Supplier");
            CreateInventoryBySupplierSheet(supplierSheet, inventory.SupplierInventory);

            // Purchase Lots Worksheet
            var lotsSheet = package.Workbook.Worksheets.Add("Purchase Lots");
            CreatePurchaseLotsSheet(lotsSheet, inventory.PurchaseLots);

            return await SaveExcelFileAsync(package, $"Inventory_Valuation_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        #endregion

        #region Fleet Efficiency Export

        public async Task<string> ExportFleetEfficiencyToExcelAsync(FleetEfficiencyService.FleetEfficiencyResult fleetEfficiency, DateTime fromDate, DateTime toDate)
        {
            using var package = new ExcelPackage();

            // Fleet Overview Worksheet
            var overviewSheet = package.Workbook.Worksheets.Add("Fleet Overview");
            CreateFleetOverviewSheet(overviewSheet, fleetEfficiency.Overview);

            // Vessel Performance Worksheet
            var vesselSheet = package.Workbook.Worksheets.Add("Vessel Performance");
            CreateVesselPerformanceSheet(vesselSheet, fleetEfficiency.VesselEfficiency);

            // Route Analysis Worksheet
            var routeSheet = package.Workbook.Worksheets.Add("Route Analysis");
            CreateRouteAnalysisSheet(routeSheet, fleetEfficiency.RouteComparison);

            return await SaveExcelFileAsync(package, $"Fleet_Efficiency_{fromDate:yyyyMM}-{toDate:yyyyMM}.xlsx");
        }

        #endregion

        #region FIFO Allocation Detail Export

        public async Task<string> ExportFIFOAllocationDetailToExcelAsync(FIFOAllocationDetailService.FIFOAllocationDetailResult fifoDetail, DateTime fromDate, DateTime toDate)
        {
            using var package = new ExcelPackage();

            // Allocation Flow Worksheet
            var flowSheet = package.Workbook.Worksheets.Add("Allocation Flow");
            CreateFIFOFlowSheet(flowSheet, fifoDetail.FlowSummary);

            // Allocation Records Worksheet
            var recordsSheet = package.Workbook.Worksheets.Add("Allocation Records");
            CreateFIFORecordsSheet(recordsSheet, fifoDetail.AllocationRecords);

            return await SaveExcelFileAsync(package, $"FIFO_Allocation_Detail_{fromDate:yyyyMM}-{toDate:yyyyMM}.xlsx");
        }

        #endregion

        #region Cost Analysis Export

        public async Task<string> ExportCostAnalysisToExcelAsync(CostAnalysisService.CostAnalysisResult costAnalysis, DateTime fromDate, DateTime toDate)
        {
            using var package = new ExcelPackage();

            // Overview Worksheet
            var overviewSheet = package.Workbook.Worksheets.Add("Overview");
            CreateCostOverviewSheet(overviewSheet, costAnalysis.Overview);

            // Price Trends Worksheet
            var trendsSheet = package.Workbook.Worksheets.Add("Price Trends");
            CreatePriceTrendsSheet(trendsSheet, costAnalysis.PriceTrends);

            return await SaveExcelFileAsync(package, $"Cost_Analysis_{fromDate:yyyyMM}-{toDate:yyyyMM}.xlsx");
        }

        #endregion

        #region Route Performance Export

        public async Task<string> ExportRoutePerformanceToExcelAsync(RoutePerformanceService.RoutePerformanceResult routePerformance, DateTime fromDate, DateTime toDate)
        {
            using var package = new ExcelPackage();

            // Overview Worksheet
            var overviewSheet = package.Workbook.Worksheets.Add("Overview");
            CreateRouteOverviewSheet(overviewSheet, routePerformance.Overview);

            // Route Comparison Worksheet
            var comparisonSheet = package.Workbook.Worksheets.Add("Route Comparison");
            CreateRouteComparisonSheet(comparisonSheet, routePerformance.RouteComparisons);

            return await SaveExcelFileAsync(package, $"Route_Performance_{fromDate:yyyyMM}-{toDate:yyyyMM}.xlsx");
        }

        #endregion

        #region Private Helper Methods - Sheet Creation

        private void CreateExecutiveSummarySheet(ExcelWorksheet sheet, SummaryService.ExecutiveSummary summary, string month)
        {
            // Title
            sheet.Cells["A1"].Value = $"Executive Summary - {month}";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;

            // Key Metrics
            sheet.Cells["A3"].Value = "Key Performance Indicators";
            sheet.Cells["A3"].Style.Font.Bold = true;

            sheet.Cells["A5"].Value = "Total Fleet Consumption (L)";
            sheet.Cells["B5"].Value = summary.TotalFleetConsumptionL;
            sheet.Cells["B5"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A6"].Value = "Total Fleet Consumption (T)";
            sheet.Cells["B6"].Value = summary.TotalFleetConsumptionT;
            sheet.Cells["B6"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A7"].Value = "Total Legs Completed";
            sheet.Cells["B7"].Value = summary.TotalLegsCompleted;
            sheet.Cells["B7"].Style.Numberformat.Format = "#,##0";

            sheet.Cells["A8"].Value = "Fleet Efficiency (L/Leg)";
            sheet.Cells["B8"].Value = summary.FleetEfficiencyLPerLeg;
            sheet.Cells["B8"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A9"].Value = "Total Operating Cost";
            sheet.Cells["B9"].Value = summary.TotalOperatingCostUSD;
            sheet.Cells["B9"].Style.Numberformat.Format = "$#,##0.00";

            sheet.Cells["A10"].Value = "Cost Per Leg";
            sheet.Cells["B10"].Value = summary.CostPerLeg;
            sheet.Cells["B10"].Style.Numberformat.Format = "$#,##0.00";

            // Auto-fit columns
            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateConsumptionSummarySheet(ExcelWorksheet sheet, List<SummaryService.VesselConsumptionSummary> consumptionData)
        {
            // Headers
            var headers = new[] { "Vessel", "Type", "Route", "Consumption (L)", "Consumption (T)", "Legs", "L/Leg", "T/Leg", "Cost", "Cost/L", "Cost/T", "Entries" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = headers[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            }

            // Data
            for (int row = 0; row < consumptionData.Count; row++)
            {
                var data = consumptionData[row];
                sheet.Cells[row + 2, 1].Value = data.VesselName;
                sheet.Cells[row + 2, 2].Value = data.VesselType;
                sheet.Cells[row + 2, 3].Value = data.Route;
                sheet.Cells[row + 2, 4].Value = data.TotalConsumptionL;
                sheet.Cells[row + 2, 5].Value = data.TotalConsumptionT;
                sheet.Cells[row + 2, 6].Value = data.TotalLegs;
                sheet.Cells[row + 2, 7].Value = data.AvgConsumptionPerLegL;
                sheet.Cells[row + 2, 8].Value = data.AvgConsumptionPerLegT;
                sheet.Cells[row + 2, 9].Value = data.TotalAllocatedValueUSD;
                sheet.Cells[row + 2, 10].Value = data.CostPerLiter;
                sheet.Cells[row + 2, 11].Value = data.CostPerTon;
                sheet.Cells[row + 2, 12].Value = data.ConsumptionEntries;

                // Format numbers
                sheet.Cells[row + 2, 4].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 5].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 7].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 8].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 9].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[row + 2, 10].Style.Numberformat.Format = "$#,##0.000000";
                sheet.Cells[row + 2, 11].Style.Numberformat.Format = "$#,##0.00";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreatePurchaseSummarySheet(ExcelWorksheet sheet, List<SummaryService.SupplierPurchaseSummary> purchaseData)
        {
            // Headers
            var headers = new[] { "Supplier", "Currency", "Purchases (L)", "Purchases (T)", "Value (Original)", "Value (USD)", "Avg Price/L", "Avg Price/T", "Purchases", "Remaining (L)", "Remaining (T)" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = headers[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
            }

            // Data
            for (int row = 0; row < purchaseData.Count; row++)
            {
                var data = purchaseData[row];
                sheet.Cells[row + 2, 1].Value = data.SupplierName;
                sheet.Cells[row + 2, 2].Value = data.Currency;
                sheet.Cells[row + 2, 3].Value = data.TotalPurchasesL;
                sheet.Cells[row + 2, 4].Value = data.TotalPurchasesT;
                sheet.Cells[row + 2, 5].Value = data.TotalValueOriginal;
                sheet.Cells[row + 2, 6].Value = data.TotalValueUSD;
                sheet.Cells[row + 2, 7].Value = data.AvgPricePerLiter;
                sheet.Cells[row + 2, 8].Value = data.AvgPricePerTon;
                sheet.Cells[row + 2, 9].Value = data.PurchaseCount;
                sheet.Cells[row + 2, 10].Value = data.RemainingL;
                sheet.Cells[row + 2, 11].Value = data.RemainingT;

                // Format numbers
                sheet.Cells[row + 2, 3].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 4].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 5].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 6].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[row + 2, 7].Style.Numberformat.Format = "#,##0.000000";
                sheet.Cells[row + 2, 8].Style.Numberformat.Format = "#,##0.00";
                sheet.Cells[row + 2, 10].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 11].Style.Numberformat.Format = "#,##0.000";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateAllocationSummarySheet(ExcelWorksheet sheet, List<SummaryService.AllocationSummary> allocationData)
        {
            // Headers
            var headers = new[] { "Vessel", "Supplier", "Allocated (L)", "Allocated (T)", "Value (USD)", "Allocations", "Oldest Purchase", "Newest Purchase" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[1, i + 1].Value = headers[i];
                sheet.Cells[1, i + 1].Style.Font.Bold = true;
                sheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
            }

            // Data
            for (int row = 0; row < allocationData.Count; row++)
            {
                var data = allocationData[row];
                sheet.Cells[row + 2, 1].Value = data.VesselName;
                sheet.Cells[row + 2, 2].Value = data.SupplierName;
                sheet.Cells[row + 2, 3].Value = data.AllocatedQuantityL;
                sheet.Cells[row + 2, 4].Value = data.AllocatedQuantityT;
                sheet.Cells[row + 2, 5].Value = data.AllocatedValueUSD;
                sheet.Cells[row + 2, 6].Value = data.AllocationCount;
                sheet.Cells[row + 2, 7].Value = data.OldestPurchaseDate;
                sheet.Cells[row + 2, 8].Value = data.NewestPurchaseDate;

                // Format numbers and dates
                sheet.Cells[row + 2, 3].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 4].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 2, 5].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[row + 2, 7].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[row + 2, 8].Style.Numberformat.Format = "dd/mm/yyyy";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateFinancialSummarySheet(ExcelWorksheet sheet, SummaryService.FinancialSummary financial)
        {
            // Title
            sheet.Cells["A1"].Value = "Financial Summary";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;

            // Financial Metrics
            sheet.Cells["A3"].Value = "Financial Metrics";
            sheet.Cells["A3"].Style.Font.Bold = true;

            sheet.Cells["A5"].Value = "Total Purchase Value";
            sheet.Cells["B5"].Value = financial.TotalPurchaseValueUSD;
            sheet.Cells["B5"].Style.Numberformat.Format = "$#,##0.00";

            sheet.Cells["A6"].Value = "Total Consumption Value";
            sheet.Cells["B6"].Value = financial.TotalConsumptionValueUSD;
            sheet.Cells["B6"].Style.Numberformat.Format = "$#,##0.00";

            sheet.Cells["A7"].Value = "Remaining Inventory Value";
            sheet.Cells["B7"].Value = financial.RemainingInventoryValueUSD;
            sheet.Cells["B7"].Style.Numberformat.Format = "$#,##0.00";

            sheet.Cells["A8"].Value = "Average Cost per Liter";
            sheet.Cells["B8"].Value = financial.AvgCostPerLiterUSD;
            sheet.Cells["B8"].Style.Numberformat.Format = "$#,##0.000000";

            sheet.Cells["A9"].Value = "Average Cost per Ton";
            sheet.Cells["B9"].Value = financial.AvgCostPerTonUSD;
            sheet.Cells["B9"].Style.Numberformat.Format = "$#,##0.00";

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateVesselAccountSheet(ExcelWorksheet sheet, ReportService.VesselAccountStatementResult vesselAccount, string vesselName, DateTime fromDate, DateTime toDate)
        {
            // Title and header info
            sheet.Cells["A1"].Value = $"Vessel Account Statement - {vesselName}";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;

            sheet.Cells["A2"].Value = $"Period: {fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}";

            // Summary section
            sheet.Cells["A4"].Value = "Summary";
            sheet.Cells["A4"].Style.Font.Bold = true;

            sheet.Cells["A6"].Value = "Total Purchases";
            sheet.Cells["B6"].Value = vesselAccount.Summary.TotalPurchases;
            sheet.Cells["B6"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A7"].Value = "Total Consumption";
            sheet.Cells["B7"].Value = vesselAccount.Summary.TotalConsumption;
            sheet.Cells["B7"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A8"].Value = "Current Balance";
            sheet.Cells["B8"].Value = vesselAccount.Summary.CurrentBalance;
            sheet.Cells["B8"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A9"].Value = "Total Value";
            sheet.Cells["B9"].Value = vesselAccount.Summary.TotalValue;
            sheet.Cells["B9"].Style.Numberformat.Format = "$#,##0.00";

            // Transaction details
            sheet.Cells["A12"].Value = "Transaction Details";
            sheet.Cells["A12"].Style.Font.Bold = true;

            var transactionHeaders = new[] { "Date", "Type", "Reference", "Supplier", "Debit (L)", "Credit (L)", "Balance (L)", "Value (USD)", "Description" };
            for (int i = 0; i < transactionHeaders.Length; i++)
            {
                sheet.Cells[14, i + 1].Value = transactionHeaders[i];
                sheet.Cells[14, i + 1].Style.Font.Bold = true;
                sheet.Cells[14, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                sheet.Cells[14, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
            }

            // Transaction data
            for (int row = 0; row < vesselAccount.Transactions.Count; row++)
            {
                var transaction = vesselAccount.Transactions[row];
                sheet.Cells[row + 15, 1].Value = transaction.TransactionDate;
                sheet.Cells[row + 15, 2].Value = transaction.TransactionType;
                sheet.Cells[row + 15, 3].Value = transaction.Reference;
                sheet.Cells[row + 15, 4].Value = transaction.SupplierName;
                sheet.Cells[row + 15, 5].Value = transaction.DebitQuantity;
                sheet.Cells[row + 15, 6].Value = transaction.CreditQuantity;
                sheet.Cells[row + 15, 7].Value = transaction.RunningBalance;
                sheet.Cells[row + 15, 8].Value = transaction.ValueUSD;
                sheet.Cells[row + 15, 9].Value = transaction.Description;

                // Format
                sheet.Cells[row + 15, 1].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[row + 15, 5].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 15, 6].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 15, 7].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[row + 15, 8].Style.Numberformat.Format = "$#,##0.00";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        // Simplified placeholder methods for other sheet types
        private void CreateSupplierAccountSheet(ExcelWorksheet sheet, ReportService.SupplierAccountReportResult supplierAccount, string supplierName, DateTime fromDate, DateTime toDate)
        {
            sheet.Cells["A1"].Value = $"Supplier Account - {supplierName}";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreatePaymentDueSheet(ExcelWorksheet sheet, List<AlertService.DueDateAlert> alerts)
        {
            sheet.Cells["A1"].Value = "Payment Due Report";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateInventoryOverviewSheet(ExcelWorksheet sheet, InventoryValuationService.InventoryValuationSummary summary)
        {
            sheet.Cells["A1"].Value = "Inventory Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateInventoryByVesselSheet(ExcelWorksheet sheet, List<InventoryValuationService.VesselInventoryItem> vesselInventory)
        {
            sheet.Cells["A1"].Value = "Inventory by Vessel";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateInventoryBySupplierSheet(ExcelWorksheet sheet, List<InventoryValuationService.SupplierInventoryItem> supplierInventory)
        {
            sheet.Cells["A1"].Value = "Inventory by Supplier";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreatePurchaseLotsSheet(ExcelWorksheet sheet, List<InventoryValuationService.PurchaseLotItem> purchaseLots)
        {
            sheet.Cells["A1"].Value = "Purchase Lots";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateFleetOverviewSheet(ExcelWorksheet sheet, FleetEfficiencyService.FleetOverview overview)
        {
            sheet.Cells["A1"].Value = "Fleet Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateVesselPerformanceSheet(ExcelWorksheet sheet, List<FleetEfficiencyService.VesselEfficiencyDetail> vesselPerformance)
        {
            sheet.Cells["A1"].Value = "Vessel Performance";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateRouteAnalysisSheet(ExcelWorksheet sheet, List<FleetEfficiencyService.RouteEfficiencyComparison> routeComparison)
        {
            sheet.Cells["A1"].Value = "Route Analysis";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateFIFOFlowSheet(ExcelWorksheet sheet, FIFOAllocationDetailService.AllocationFlowSummary flowSummary)
        {
            sheet.Cells["A1"].Value = "FIFO Flow Summary";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateFIFORecordsSheet(ExcelWorksheet sheet, List<FIFOAllocationDetailService.DetailedAllocationRecord> allocationRecords)
        {
            sheet.Cells["A1"].Value = "FIFO Records";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateCostOverviewSheet(ExcelWorksheet sheet, CostAnalysisService.CostAnalysisOverview overview)
        {
            sheet.Cells["A1"].Value = "Cost Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreatePriceTrendsSheet(ExcelWorksheet sheet, List<CostAnalysisService.PriceTrendAnalysis> priceTrends)
        {
            sheet.Cells["A1"].Value = "Price Trends";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateRouteOverviewSheet(ExcelWorksheet sheet, RoutePerformanceService.RoutePerformanceOverview overview)
        {
            sheet.Cells["A1"].Value = "Route Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        private void CreateRouteComparisonSheet(ExcelWorksheet sheet, List<RoutePerformanceService.RouteComparison> routeComparisons)
        {
            sheet.Cells["A1"].Value = "Route Comparison";
            sheet.Cells["A1"].Style.Font.Bold = true;
        }

        #endregion

        #region Save File Helper

        private async Task<string> SaveExcelFileAsync(ExcelPackage package, string defaultFileName)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = defaultFileName,
                DefaultExt = ".xlsx",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var filePath = saveFileDialog.FileName;
                await package.SaveAsAsync(new FileInfo(filePath));
                return filePath;
            }

            return string.Empty;
        }

        #endregion
    }
}