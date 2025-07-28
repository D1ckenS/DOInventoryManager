using DOInventoryManager.Data;
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
            ExcelPackage.License.SetNonCommercialPersonal("DO Inventory Manager User");

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
            using var context = new InventoryContext();

            // Get all the required data for comprehensive report
            var alertService = new AlertService();
            var agingItems = await alertService.GetPaymentAgingAnalysisAsync();
            var supplierSummaries = await alertService.GetSupplierPaymentSummaryAsync();
            var paidInvoices = await alertService.GetPaidInvoicesAsync();

            // 1. Payment Schedule Worksheet
            var scheduleSheet = package.Workbook.Worksheets.Add("Payment Schedule");
            CreatePaymentScheduleSheet(scheduleSheet, agingItems);

            // 2. Aging Analysis Worksheet  
            var agingSheet = package.Workbook.Worksheets.Add("Aging Analysis");
            CreateAgingAnalysisSheet(agingSheet, supplierSummaries);

            // 3. Paid Invoices Worksheet
            var paidSheet = package.Workbook.Worksheets.Add("Paid Invoices");
            CreatePaidInvoicesSheet(paidSheet, paidInvoices);

            // 4. Summary Worksheet
            var summarySheet = package.Workbook.Worksheets.Add("Payment Summary");
            var paymentSummary = await alertService.GetPaymentSummaryAsync();
            CreatePaymentSummarySheet(summarySheet, paymentSummary, agingItems.Count, paidInvoices.Count);

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

        #region Supplier Account Export

        private void CreateSupplierAccountSheet(ExcelWorksheet sheet, ReportService.SupplierAccountReportResult supplierAccount, string supplierName, DateTime fromDate, DateTime toDate)
        {
            // Title
            sheet.Cells["A1"].Value = $"Supplier Account Statement - {supplierName}";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Period: {fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            sheet.Cells["A3"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A3"].Style.Font.Italic = true;

            // Summary Cards - CORRECTED PROPERTY NAMES
            sheet.Cells["A5"].Value = "ACCOUNT SUMMARY";
            sheet.Cells["A5"].Style.Font.Bold = true;
            sheet.Cells["A5"].Style.Font.Size = 14;

            sheet.Cells["A7"].Value = "Beginning Balance:";
            sheet.Cells["B7"].Value = supplierAccount.Summary.BeginningBalance; // CORRECTED
            sheet.Cells["B7"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A8"].Value = "Period Purchases:";
            sheet.Cells["B8"].Value = supplierAccount.Summary.PeriodPurchases; // CORRECTED
            sheet.Cells["B8"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A9"].Value = "Period Consumption:";
            sheet.Cells["B9"].Value = supplierAccount.Summary.PeriodConsumption; // CORRECTED
            sheet.Cells["B9"].Style.Numberformat.Format = "#,##0.000";

            sheet.Cells["A10"].Value = "Ending Balance:";
            sheet.Cells["B10"].Value = supplierAccount.Summary.EndingBalance; // CORRECTED
            sheet.Cells["B10"].Style.Numberformat.Format = "#,##0.000";
            sheet.Cells["B10"].Style.Font.Bold = true;

            // Transaction Headers - CORRECTED PROPERTY NAMES
            var headers = new string[]
            {
        "Date", "Vessel", "Type", "Reference", "Purchase Qty", "Consumption Qty", "Balance", "Value", "Currency", "USD Value"
            };

            int startRow = 14;
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[startRow, i + 1].Value = headers[i];
                sheet.Cells[startRow, i + 1].Style.Font.Bold = true;
                sheet.Cells[startRow, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[startRow, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Transaction Data - CORRECTED PROPERTY NAMES
            for (int row = 0; row < supplierAccount.Transactions.Count; row++)
            {
                var transaction = supplierAccount.Transactions[row];
                var excelRow = row + startRow + 1;

                sheet.Cells[excelRow, 1].Value = transaction.TransactionDate; // CORRECTED
                sheet.Cells[excelRow, 2].Value = transaction.VesselName; // CORRECTED
                sheet.Cells[excelRow, 3].Value = transaction.TransactionType; // CORRECTED
                sheet.Cells[excelRow, 4].Value = transaction.Reference; // CORRECTED
                sheet.Cells[excelRow, 5].Value = transaction.PurchaseQuantity; // CORRECTED
                sheet.Cells[excelRow, 6].Value = transaction.ConsumptionQuantity; // CORRECTED
                sheet.Cells[excelRow, 7].Value = transaction.RunningBalance; // CORRECTED
                sheet.Cells[excelRow, 8].Value = transaction.Value; // CORRECTED
                sheet.Cells[excelRow, 9].Value = transaction.Currency; // CORRECTED
                sheet.Cells[excelRow, 10].Value = transaction.ValueUSD; // CORRECTED

                // Formatting
                sheet.Cells[excelRow, 1].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 5].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 6].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.000";
                if (transaction.Currency == "USD")
                {
                    sheet.Cells[excelRow, 8].Style.Numberformat.Format = "$#,##0.00";
                }
                else
                {
                    sheet.Cells[excelRow, 8].Style.Numberformat.Format = "#,##0.000";
                }
                sheet.Cells[excelRow, 10].Style.Numberformat.Format = "$#,##0.00";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        #endregion

        #region Payment Due Sheet Creation Methods

        private void CreatePaymentScheduleSheet(ExcelWorksheet sheet, List<AlertService.PaymentAgingItem> agingItems)
        {
            // Title
            sheet.Cells["A1"].Value = "Payment Schedule - Outstanding Invoices";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            sheet.Cells["A4"].Value = $"Total Outstanding Invoices: {agingItems.Count}";
            sheet.Cells["A4"].Style.Font.Bold = true;

            // Headers
            var headers = new string[]
            {
        "Invoice Reference", "Supplier", "Vessel", "Receipt Date", "Due Date",
        "Days Overdue", "Amount", "Currency", "USD Amount", "Status", "Aging Category"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[6, i + 1].Value = headers[i];
                sheet.Cells[6, i + 1].Style.Font.Bold = true;
                sheet.Cells[6, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[6, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data rows
            for (int row = 0; row < agingItems.Count; row++)
            {
                var item = agingItems[row];
                var excelRow = row + 7;

                sheet.Cells[excelRow, 1].Value = item.InvoiceReference;
                sheet.Cells[excelRow, 2].Value = item.SupplierName;
                sheet.Cells[excelRow, 3].Value = item.VesselName;
                sheet.Cells[excelRow, 4].Value = item.InvoiceReceiptDate;
                sheet.Cells[excelRow, 5].Value = item.DueDate;
                sheet.Cells[excelRow, 6].Value = item.DaysOverdue;
                sheet.Cells[excelRow, 7].Value = item.Currency == "USD" ? item.TotalValueUSD : item.TotalValue;
                sheet.Cells[excelRow, 8].Value = item.Currency;
                sheet.Cells[excelRow, 9].Value = item.TotalValueUSD;
                sheet.Cells[excelRow, 10].Value = item.StatusText;
                sheet.Cells[excelRow, 11].Value = item.AgingCategory;

                // Formatting
                sheet.Cells[excelRow, 4].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 5].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 9].Style.Numberformat.Format = "$#,##0.00";

                if (item.Currency == "USD")
                    sheet.Cells[excelRow, 7].Style.Numberformat.Format = "$#,##0.00";
                else
                    sheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.000";

                // Color coding by status
                var rowColor = item.PaymentStatus switch
                {
                    AlertService.PaymentStatus.Overdue => System.Drawing.Color.FromArgb(255, 235, 238),
                    AlertService.PaymentStatus.DueToday => System.Drawing.Color.FromArgb(255, 243, 224),
                    AlertService.PaymentStatus.DueTomorrow => System.Drawing.Color.FromArgb(255, 252, 230),
                    AlertService.PaymentStatus.DueThisWeek => System.Drawing.Color.FromArgb(240, 248, 255),
                    _ => System.Drawing.Color.White
                };

                if (rowColor != System.Drawing.Color.White)
                {
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        sheet.Cells[excelRow, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        sheet.Cells[excelRow, col].Style.Fill.BackgroundColor.SetColor(rowColor);
                    }
                }
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateAgingAnalysisSheet(ExcelWorksheet sheet, List<AlertService.SupplierPaymentSummary> supplierSummaries)
        {
            sheet.Cells["A1"].Value = "Aging Analysis - Payment Summary by Supplier";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            sheet.Cells["A4"].Value = $"Total Suppliers with Outstanding Payments: {supplierSummaries.Count}";
            sheet.Cells["A4"].Style.Font.Bold = true;

            // Headers
            var headers = new string[]
            {
        "Supplier", "Currency", "Total Outstanding", "Total Overdue",
        "Total Invoices", "Overdue Invoices", "Avg Days Overdue"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[6, i + 1].Value = headers[i];
                sheet.Cells[6, i + 1].Style.Font.Bold = true;
                sheet.Cells[6, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[6, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data rows
            for (int row = 0; row < supplierSummaries.Count; row++)
            {
                var supplier = supplierSummaries[row];
                var excelRow = row + 7;

                sheet.Cells[excelRow, 1].Value = supplier.SupplierName;
                sheet.Cells[excelRow, 2].Value = supplier.Currency;
                sheet.Cells[excelRow, 3].Value = supplier.TotalOutstanding;
                sheet.Cells[excelRow, 4].Value = supplier.TotalOverdue;
                sheet.Cells[excelRow, 5].Value = supplier.TotalInvoices;
                sheet.Cells[excelRow, 6].Value = supplier.OverdueInvoices;
                sheet.Cells[excelRow, 7].Value = supplier.AvgDaysOverdue;

                // Formatting
                if (supplier.Currency == "USD")
                {
                    sheet.Cells[excelRow, 3].Style.Numberformat.Format = "$#,##0.00";
                    sheet.Cells[excelRow, 4].Style.Numberformat.Format = "$#,##0.00";
                }
                else
                {
                    sheet.Cells[excelRow, 3].Style.Numberformat.Format = "#,##0.000";
                    sheet.Cells[excelRow, 4].Style.Numberformat.Format = "#,##0.000";
                }
                sheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.0";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreatePaidInvoicesSheet(ExcelWorksheet sheet, List<AlertService.PaidInvoiceItem> paidInvoices)
        {
            sheet.Cells["A1"].Value = "Paid Invoices - Payment History";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            sheet.Cells["A4"].Value = $"Total Paid Invoices: {paidInvoices.Count}";
            sheet.Cells["A4"].Style.Font.Bold = true;

            // Headers
            var headers = new string[]
            {
        "Payment Date", "Invoice Reference", "Supplier", "Vessel", "Receipt Date",
        "Due Date", "Payment Amount", "Currency", "USD Amount", "Invoice Value"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[6, i + 1].Value = headers[i];
                sheet.Cells[6, i + 1].Style.Font.Bold = true;
                sheet.Cells[6, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[6, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
            }

            // Data rows
            for (int row = 0; row < paidInvoices.Count; row++)
            {
                var invoice = paidInvoices[row];
                var excelRow = row + 7;

                sheet.Cells[excelRow, 1].Value = invoice.PaymentDate;
                sheet.Cells[excelRow, 2].Value = invoice.InvoiceReference;
                sheet.Cells[excelRow, 3].Value = invoice.SupplierName;
                sheet.Cells[excelRow, 4].Value = invoice.VesselName;
                sheet.Cells[excelRow, 5].Value = invoice.InvoiceReceiptDate;
                sheet.Cells[excelRow, 6].Value = invoice.DueDate;
                sheet.Cells[excelRow, 7].Value = invoice.Currency == "USD" ? invoice.PaymentAmountUSD : invoice.PaymentAmount;
                sheet.Cells[excelRow, 8].Value = invoice.Currency;
                sheet.Cells[excelRow, 9].Value = invoice.PaymentAmountUSD;
                sheet.Cells[excelRow, 10].Value = invoice.Currency == "USD" ? invoice.TotalValueUSD : invoice.TotalValue;

                // Formatting
                sheet.Cells[excelRow, 1].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 5].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 6].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 9].Style.Numberformat.Format = "$#,##0.00";

                if (invoice.Currency == "USD")
                {
                    sheet.Cells[excelRow, 7].Style.Numberformat.Format = "$#,##0.00";
                    sheet.Cells[excelRow, 10].Style.Numberformat.Format = "$#,##0.00";
                }
                else
                {
                    sheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.000";
                    sheet.Cells[excelRow, 10].Style.Numberformat.Format = "#,##0.000";
                }
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreatePaymentSummarySheet(ExcelWorksheet sheet, AlertService.PaymentSummary summary, int outstandingCount, int paidCount)
        {
            sheet.Cells["A1"].Value = "Payment Summary - Executive Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            // Summary Cards
            sheet.Cells["A5"].Value = "OVERDUE INVOICES";
            sheet.Cells["A5"].Style.Font.Bold = true;
            sheet.Cells["A6"].Value = summary.TotalOverdueAmount;
            sheet.Cells["A6"].Style.Numberformat.Format = "$#,##0.00";
            sheet.Cells["A6"].Style.Font.Size = 14;
            sheet.Cells["A7"].Value = $"{summary.OverdueCount} invoices";

            sheet.Cells["C5"].Value = "DUE TODAY";
            sheet.Cells["C5"].Style.Font.Bold = true;
            sheet.Cells["C6"].Value = summary.DueTodayAmount;
            sheet.Cells["C6"].Style.Numberformat.Format = "$#,##0.00";
            sheet.Cells["C6"].Style.Font.Size = 14;
            sheet.Cells["C7"].Value = $"{summary.DueTodayCount} invoices";

            sheet.Cells["E5"].Value = "DUE THIS WEEK";
            sheet.Cells["E5"].Style.Font.Bold = true;
            sheet.Cells["E6"].Value = summary.DueThisWeekAmount;
            sheet.Cells["E6"].Style.Numberformat.Format = "$#,##0.00";
            sheet.Cells["E6"].Style.Font.Size = 14;
            sheet.Cells["E7"].Value = $"{summary.DueThisWeekCount} invoices";

            sheet.Cells["A10"].Value = "TOTAL OUTSTANDING";
            sheet.Cells["A10"].Style.Font.Bold = true;
            sheet.Cells["A11"].Value = summary.TotalOutstandingAmount;
            sheet.Cells["A11"].Style.Numberformat.Format = "$#,##0.00";
            sheet.Cells["A11"].Style.Font.Size = 14;
            sheet.Cells["A12"].Value = $"{summary.TotalOutstandingCount} invoices";

            sheet.Cells["C10"].Value = "PAID INVOICES";
            sheet.Cells["C10"].Style.Font.Bold = true;
            sheet.Cells["C11"].Value = paidCount;
            sheet.Cells["C11"].Style.Font.Size = 14;
            sheet.Cells["C12"].Value = "completed payments";

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        #endregion

        #region Inventory Valuation Export - CORRECTED WITH EXACT PROPERTY NAMES

        private void CreateInventoryOverviewSheet(ExcelWorksheet sheet, InventoryValuationService.InventoryValuationSummary summary)
        {
            sheet.Cells["A1"].Value = "Inventory Valuation Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            // KPI Cards - EXACT PROPERTY NAMES FROM InventoryValuationSummary
            sheet.Cells["A5"].Value = "INVENTORY SUMMARY";
            sheet.Cells["A5"].Style.Font.Bold = true;
            sheet.Cells["A5"].Style.Font.Size = 14;

            var kpis = new (string Label, object Value, string Format)[]
            {
        ("Total Inventory (Liters):", summary.TotalInventoryLiters, "#,##0.000"),
        ("Total Inventory (Tons):", summary.TotalInventoryTons, "#,##0.000"),
        ("Total FIFO Value (USD):", summary.TotalFIFOValueUSD, "$#,##0.00"),
        ("Number of Purchase Lots:", summary.NumberOfPurchaseLots, "#,##0"),
        ("Vessels with Inventory:", summary.VesselsWithInventory, "#,##0"),
        ("Suppliers with Inventory:", summary.SuppliersWithInventory, "#,##0"),
        ("Avg Cost per Liter (USD):", summary.AvgCostPerLiterUSD, "$#,##0.000000"),
        ("Avg Cost per Ton (USD):", summary.AvgCostPerTonUSD, "$#,##0.00")
            };

            for (int i = 0; i < kpis.Length; i++)
            {
                var row = 7 + i;
                sheet.Cells[row, 1].Value = kpis[i].Label;
                sheet.Cells[row, 1].Style.Font.Bold = true;
                sheet.Cells[row, 2].Value = kpis[i].Value;
                sheet.Cells[row, 2].Style.Numberformat.Format = kpis[i].Format;
                sheet.Cells[row, 2].Style.Font.Size = 12;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateInventoryByVesselSheet(ExcelWorksheet sheet, List<InventoryValuationService.VesselInventoryItem> vesselInventory)
        {
            sheet.Cells["A1"].Value = "Inventory Valuation by Vessel";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            var headers = new string[]
            {
        "Vessel", "Type", "Route", "Inventory (L)", "Inventory (T)", "FIFO Value (USD)",
        "Lots", "Oldest Purchase", "Newest Purchase", "Days Old", "Cost/L (USD)", "Cost/T (USD)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, i + 1].Value = headers[i];
                sheet.Cells[3, i + 1].Style.Font.Bold = true;
                sheet.Cells[3, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            for (int row = 0; row < vesselInventory.Count; row++)
            {
                var vessel = vesselInventory[row];
                var excelRow = row + 4;

                // EXACT PROPERTY NAMES FROM VesselInventoryItem
                sheet.Cells[excelRow, 1].Value = vessel.VesselName;
                sheet.Cells[excelRow, 2].Value = vessel.VesselType;
                sheet.Cells[excelRow, 3].Value = vessel.Route;
                sheet.Cells[excelRow, 4].Value = vessel.CurrentInventoryL;
                sheet.Cells[excelRow, 5].Value = vessel.CurrentInventoryT;
                sheet.Cells[excelRow, 6].Value = vessel.FIFOValueUSD;
                sheet.Cells[excelRow, 7].Value = vessel.PurchaseLotsCount;
                sheet.Cells[excelRow, 8].Value = vessel.OldestPurchaseDate;
                sheet.Cells[excelRow, 9].Value = vessel.NewestPurchaseDate;
                sheet.Cells[excelRow, 10].Value = vessel.DaysOldestInventory;
                sheet.Cells[excelRow, 11].Value = vessel.AvgCostPerLiterUSD;
                sheet.Cells[excelRow, 12].Value = vessel.AvgCostPerTonUSD;

                // Formatting
                sheet.Cells[excelRow, 4].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 5].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 6].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 8].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 9].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 11].Style.Numberformat.Format = "$#,##0.000000";
                sheet.Cells[excelRow, 12].Style.Numberformat.Format = "$#,##0.00";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateInventoryBySupplierSheet(ExcelWorksheet sheet, List<InventoryValuationService.SupplierInventoryItem> supplierInventory)
        {
            sheet.Cells["A1"].Value = "Inventory Valuation by Supplier";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            var headers = new string[]
            {
        "Supplier", "Currency", "Inventory (L)", "Inventory (T)", "FIFO Value (Original)",
        "FIFO Value (USD)", "Cost/L (Original)", "Cost/L (USD)", "Lots", "Vessels", "Oldest", "Newest"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, i + 1].Value = headers[i];
                sheet.Cells[3, i + 1].Style.Font.Bold = true;
                sheet.Cells[3, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
            }

            for (int row = 0; row < supplierInventory.Count; row++)
            {
                var supplier = supplierInventory[row];
                var excelRow = row + 4;

                // EXACT PROPERTY NAMES FROM SupplierInventoryItem
                sheet.Cells[excelRow, 1].Value = supplier.SupplierName;
                sheet.Cells[excelRow, 2].Value = supplier.Currency;
                sheet.Cells[excelRow, 3].Value = supplier.RemainingInventoryL;
                sheet.Cells[excelRow, 4].Value = supplier.RemainingInventoryT;
                sheet.Cells[excelRow, 5].Value = supplier.FIFOValueOriginal;
                sheet.Cells[excelRow, 6].Value = supplier.FIFOValueUSD;
                sheet.Cells[excelRow, 7].Value = supplier.AvgCostPerLiterOriginal;
                sheet.Cells[excelRow, 8].Value = supplier.AvgCostPerLiterUSD;
                sheet.Cells[excelRow, 9].Value = supplier.PurchaseLotsCount;
                sheet.Cells[excelRow, 10].Value = supplier.VesselsSupplied;
                sheet.Cells[excelRow, 11].Value = supplier.OldestPurchaseDate;
                sheet.Cells[excelRow, 12].Value = supplier.NewestPurchaseDate;

                // Formatting
                sheet.Cells[excelRow, 3].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 4].Style.Numberformat.Format = "#,##0.000";
                if (supplier.Currency == "USD")
                {
                    sheet.Cells[excelRow, 5].Style.Numberformat.Format = "$#,##0.00";
                    sheet.Cells[excelRow, 7].Style.Numberformat.Format = "$#,##0.000000";
                }
                else
                {
                    sheet.Cells[excelRow, 5].Style.Numberformat.Format = "#,##0.000";
                    sheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.000000";
                }
                sheet.Cells[excelRow, 6].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 8].Style.Numberformat.Format = "$#,##0.000000";
                sheet.Cells[excelRow, 11].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 12].Style.Numberformat.Format = "dd/mm/yyyy";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreatePurchaseLotsSheet(ExcelWorksheet sheet, List<InventoryValuationService.PurchaseLotItem> purchaseLots)
        {
            sheet.Cells["A1"].Value = "Purchase Lots - Individual Inventory Items";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            var headers = new string[]
            {
        "Purchase Date", "Invoice Ref", "Vessel", "Supplier", "Currency", "Original (L)",
        "Remaining (L)", "Remaining (T)", "Days in Inventory", "% Remaining",
        "Remaining Value", "Cost/L", "Aging Category"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, i + 1].Value = headers[i];
                sheet.Cells[3, i + 1].Style.Font.Bold = true;
                sheet.Cells[3, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
            }

            for (int row = 0; row < purchaseLots.Count; row++)
            {
                var lot = purchaseLots[row];
                var excelRow = row + 4;

                // EXACT PROPERTY NAMES FROM PurchaseLotItem
                sheet.Cells[excelRow, 1].Value = lot.PurchaseDate;
                sheet.Cells[excelRow, 2].Value = lot.InvoiceReference;
                sheet.Cells[excelRow, 3].Value = lot.VesselName;
                sheet.Cells[excelRow, 4].Value = lot.SupplierName;
                sheet.Cells[excelRow, 5].Value = lot.Currency;
                sheet.Cells[excelRow, 6].Value = lot.OriginalQuantityL;
                sheet.Cells[excelRow, 7].Value = lot.RemainingQuantityL;
                sheet.Cells[excelRow, 8].Value = lot.RemainingQuantityT;
                sheet.Cells[excelRow, 9].Value = lot.DaysInInventory;
                sheet.Cells[excelRow, 10].Value = lot.PercentageRemaining / 100; // Convert to percentage
                sheet.Cells[excelRow, 11].Value = lot.Currency == "USD" ? lot.RemainingValueUSD : lot.RemainingValueOriginal;
                sheet.Cells[excelRow, 12].Value = lot.Currency == "USD" ? lot.CostPerLiterUSD : lot.CostPerLiterOriginal;
                sheet.Cells[excelRow, 13].Value = lot.AgingCategory;

                // Formatting
                sheet.Cells[excelRow, 1].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 6].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 8].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 10].Style.Numberformat.Format = "0.0%";

                if (lot.Currency == "USD")
                {
                    sheet.Cells[excelRow, 11].Style.Numberformat.Format = "$#,##0.00";
                    sheet.Cells[excelRow, 12].Style.Numberformat.Format = "$#,##0.000000";
                }
                else
                {
                    sheet.Cells[excelRow, 11].Style.Numberformat.Format = "#,##0.000";
                    sheet.Cells[excelRow, 12].Style.Numberformat.Format = "#,##0.000000";
                }

                // Color code by aging - CORRECTED COLOR
                var ageColor = lot.DaysInInventory switch
                {
                    <= 30 => System.Drawing.Color.LightGreen,
                    <= 60 => System.Drawing.Color.LightYellow,
                    <= 90 => System.Drawing.Color.FromArgb(255, 218, 185), // CORRECTED: Light orange
                    _ => System.Drawing.Color.LightCoral
                };

                for (int col = 1; col <= headers.Length; col++)
                {
                    sheet.Cells[excelRow, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    sheet.Cells[excelRow, col].Style.Fill.BackgroundColor.SetColor(ageColor);
                }
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        #endregion

        #region Fleet Efficiency Export

        private void CreateFleetOverviewSheet(ExcelWorksheet sheet, FleetEfficiencyService.FleetOverview overview)
        {
            sheet.Cells["A1"].Value = "Fleet Efficiency Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            // Fleet KPIs - CORRECTED PROPERTY NAMES
            sheet.Cells["A5"].Value = "FLEET PERFORMANCE METRICS";
            sheet.Cells["A5"].Style.Font.Bold = true;
            sheet.Cells["A5"].Style.Font.Size = 14;

            var metrics = new (string Label, object Value, string Format)[]
            {
        ("Total Active Vessels:", overview.TotalActiveVessels, "#,##0"),
        ("Vessel Routes:", overview.TotalVesselRoutes, "#,##0"),
        ("Boat Routes:", overview.TotalBoatRoutes, "#,##0"),
        ("Total Fleet Consumption (L):", overview.TotalFleetConsumptionL, "#,##0.000"),
        ("Total Fleet Consumption (T):", overview.TotalFleetConsumptionT, "#,##0.000"),
        ("Total Legs Completed:", overview.TotalLegsCompleted, "#,##0"),
        ("Avg Fleet Efficiency (L/Leg):", overview.AvgFleetEfficiencyLPerLeg, "#,##0.000"),
        ("Avg Fleet Efficiency (T/Leg):", overview.AvgFleetEfficiencyTPerLeg, "#,##0.000"),
        ("Total Fleet Cost (USD):", overview.TotalFleetCostUSD, "$#,##0.00"),
        ("Avg Cost per Leg (USD):", overview.AvgCostPerLegUSD, "$#,##0.00"),
        ("Avg Cost per Liter (USD):", overview.AvgCostPerLiterUSD, "$#,##0.000000"),
        ("Most Efficient Vessel:", overview.MostEfficientVessel, "General"),
        ("Least Efficient Vessel:", overview.LeastEfficientVessel, "General"),
        ("Best Route:", overview.BestRoute, "General"),
        ("Best Route Efficiency:", overview.BestRouteEfficiency, "#,##0.000")
            };

            for (int i = 0; i < metrics.Length; i++)
            {
                var row = 7 + i;
                sheet.Cells[row, 1].Value = metrics[i].Label;
                sheet.Cells[row, 1].Style.Font.Bold = true;
                sheet.Cells[row, 2].Value = metrics[i].Value;
                if (metrics[i].Format != "General")
                    sheet.Cells[row, 2].Style.Numberformat.Format = metrics[i].Format;
                sheet.Cells[row, 2].Style.Font.Size = 12;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateVesselPerformanceSheet(ExcelWorksheet sheet, List<FleetEfficiencyService.VesselEfficiencyDetail> vesselPerformance)
        {
            sheet.Cells["A1"].Value = "Individual Vessel Performance";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            var headers = new string[]
            {
        "Vessel", "Type", "Route", "Consumption (L)", "Consumption (T)", "Legs",
        "Efficiency L/Leg", "Efficiency T/Leg", "Cost (USD)", "Cost/Leg", "Cost/L", "Rank", "Grade"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, i + 1].Value = headers[i];
                sheet.Cells[3, i + 1].Style.Font.Bold = true;
                sheet.Cells[3, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            for (int row = 0; row < vesselPerformance.Count; row++)
            {
                var vessel = vesselPerformance[row];
                var excelRow = row + 4;

                // CORRECTED PROPERTY NAMES
                sheet.Cells[excelRow, 1].Value = vessel.VesselName;
                sheet.Cells[excelRow, 2].Value = vessel.VesselType;
                sheet.Cells[excelRow, 3].Value = vessel.Route;
                sheet.Cells[excelRow, 4].Value = vessel.TotalConsumptionL;
                sheet.Cells[excelRow, 5].Value = vessel.TotalConsumptionT;
                sheet.Cells[excelRow, 6].Value = vessel.TotalLegs;
                sheet.Cells[excelRow, 7].Value = vessel.EfficiencyLPerLeg; // CORRECTED
                sheet.Cells[excelRow, 8].Value = vessel.EfficiencyTPerLeg; // CORRECTED  
                sheet.Cells[excelRow, 9].Value = vessel.TotalCostUSD;
                sheet.Cells[excelRow, 10].Value = vessel.CostPerLegUSD; // CORRECTED
                sheet.Cells[excelRow, 11].Value = vessel.CostPerLiterUSD; // CORRECTED
                sheet.Cells[excelRow, 12].Value = vessel.EfficiencyRank;
                sheet.Cells[excelRow, 13].Value = vessel.EfficiencyGrade;

                // Formatting
                sheet.Cells[excelRow, 4].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 5].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 8].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 9].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 10].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 11].Style.Numberformat.Format = "$#,##0.000000";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateRouteAnalysisSheet(ExcelWorksheet sheet, List<FleetEfficiencyService.RouteEfficiencyComparison> routeComparison)
        {
            sheet.Cells["A1"].Value = "Route Efficiency Analysis";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            var headers = new string[]
            {
        "Route Type", "Route Name", "Vessels", "Total Consumption (L)", "Total Legs", "Avg Efficiency L/Leg",
        "Total Cost (USD)", "Cost per Leg", "Cost per Liter", "Best Vessel", "Worst Vessel"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, i + 1].Value = headers[i];
                sheet.Cells[3, i + 1].Style.Font.Bold = true;
                sheet.Cells[3, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
            }

            for (int row = 0; row < routeComparison.Count; row++)
            {
                var route = routeComparison[row];
                var excelRow = row + 4;

                // CORRECTED PROPERTY NAMES
                sheet.Cells[excelRow, 1].Value = route.RouteType;
                sheet.Cells[excelRow, 2].Value = route.RouteName; // CORRECTED
                sheet.Cells[excelRow, 3].Value = route.VesselCount;
                sheet.Cells[excelRow, 4].Value = route.TotalConsumptionL;
                sheet.Cells[excelRow, 5].Value = route.TotalLegs;
                sheet.Cells[excelRow, 6].Value = route.AvgEfficiencyLPerLeg;
                sheet.Cells[excelRow, 7].Value = route.TotalCostUSD;
                sheet.Cells[excelRow, 8].Value = route.AvgCostPerLegUSD; // CORRECTED
                sheet.Cells[excelRow, 9].Value = route.AvgCostPerLiterUSD;
                sheet.Cells[excelRow, 10].Value = route.BestVesselName;
                sheet.Cells[excelRow, 11].Value = route.WorstVesselName;

                // Formatting
                sheet.Cells[excelRow, 4].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 6].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 7].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 8].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 9].Style.Numberformat.Format = "$#,##0.000000";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        #endregion

        #region FIFO Allocation Detail Export

        private void CreateFIFOFlowSheet(ExcelWorksheet sheet, FIFOAllocationDetailService.AllocationFlowSummary flowSummary)
        {
            sheet.Cells["A1"].Value = "FIFO Allocation Flow Summary";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            // Flow Summary KPIs - CORRECTED PROPERTY NAMES
            sheet.Cells["A5"].Value = "ALLOCATION FLOW METRICS";
            sheet.Cells["A5"].Style.Font.Bold = true;
            sheet.Cells["A5"].Style.Font.Size = 14;

            var flowMetrics = new (string Label, object Value, string Format)[]
            {
        ("Total Purchases (L):", flowSummary.TotalPurchasesL, "#,##0.000"),
        ("Total Consumption (L):", flowSummary.TotalConsumptionL, "#,##0.000"),
        ("Total Allocated (L):", flowSummary.TotalAllocatedL, "#,##0.000"),
        ("Unallocated Purchases (L):", flowSummary.UnallocatedPurchasesL, "#,##0.000"),
        ("Unallocated Consumption (L):", flowSummary.UnallocatedConsumptionL, "#,##0.000"),
        ("Total FIFO Value (USD):", flowSummary.TotalFIFOValueUSD, "$#,##0.00"),
        ("Allocation Transactions:", flowSummary.TotalAllocationTransactions, "#,##0"),
        ("Unique Purchase Lots:", flowSummary.UniquePurchaseLots, "#,##0"),
        ("Unique Consumption Entries:", flowSummary.UniqueConsumptionEntries, "#,##0"),
        ("Vessels Involved:", flowSummary.VesselsInvolved, "#,##0"),
        ("Suppliers Involved:", flowSummary.SuppliersInvolved, "#,##0"),
        ("Oldest Purchase Date:", flowSummary.OldestPurchaseDate, "dd/mm/yyyy"),
        ("Latest Consumption Date:", flowSummary.LatestConsumptionDate, "dd/mm/yyyy"),
        ("Allocation Accuracy %:", flowSummary.AllocationAccuracyPercentage, "0.00%")
            };

            for (int i = 0; i < flowMetrics.Length; i++)
            {
                var row = 7 + i;
                sheet.Cells[row, 1].Value = flowMetrics[i].Label;
                sheet.Cells[row, 1].Style.Font.Bold = true;
                sheet.Cells[row, 2].Value = flowMetrics[i].Value;
                if (flowMetrics[i].Format.Contains("%"))
                    sheet.Cells[row, 2].Value = (decimal)flowMetrics[i].Value / 100;
                sheet.Cells[row, 2].Style.Numberformat.Format = flowMetrics[i].Format;
                sheet.Cells[row, 2].Style.Font.Size = 12;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateFIFORecordsSheet(ExcelWorksheet sheet, List<FIFOAllocationDetailService.DetailedAllocationRecord> allocationRecords)
        {
            sheet.Cells["A1"].Value = "FIFO Allocation Records - Complete Audit Trail";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            var headers = new string[]
            {
        "Allocation Date", "Purchase Date", "Consumption Date", "Invoice Ref", "Month",
        "Purchase Vessel", "Consumption Vessel", "Supplier", "Allocated Qty (L)", "Allocated Value (USD)",
        "Purchase Balance After", "FIFO Sequence", "Cross-Vessel"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, i + 1].Value = headers[i];
                sheet.Cells[3, i + 1].Style.Font.Bold = true;
                sheet.Cells[3, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCyan);
            }

            for (int row = 0; row < allocationRecords.Count; row++)
            {
                var record = allocationRecords[row];
                var excelRow = row + 4;

                // CORRECTED PROPERTY NAMES
                sheet.Cells[excelRow, 1].Value = record.AllocationDate;
                sheet.Cells[excelRow, 2].Value = record.PurchaseDate;
                sheet.Cells[excelRow, 3].Value = record.ConsumptionDate;
                sheet.Cells[excelRow, 4].Value = record.InvoiceReference;
                sheet.Cells[excelRow, 5].Value = record.Month;
                sheet.Cells[excelRow, 6].Value = record.PurchaseVessel; // CORRECTED
                sheet.Cells[excelRow, 7].Value = record.ConsumptionVessel; // CORRECTED
                sheet.Cells[excelRow, 8].Value = record.SupplierName;
                sheet.Cells[excelRow, 9].Value = record.AllocatedQuantityL; // CORRECTED
                sheet.Cells[excelRow, 10].Value = record.AllocatedValueUSD;
                sheet.Cells[excelRow, 11].Value = record.PurchaseBalanceAfterL; // CORRECTED
                sheet.Cells[excelRow, 12].Value = record.FIFOSequence; // CORRECTED
                sheet.Cells[excelRow, 13].Value = record.IsCrossVesselAllocation ? "Yes" : "No";

                // Formatting
                sheet.Cells[excelRow, 1].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 2].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 3].Style.Numberformat.Format = "dd/mm/yyyy";
                sheet.Cells[excelRow, 9].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 10].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 11].Style.Numberformat.Format = "#,##0.000";

                // Highlight cross-vessel allocations
                if (record.IsCrossVesselAllocation)
                {
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        sheet.Cells[excelRow, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        sheet.Cells[excelRow, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                    }
                }
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        #endregion

        #region Cost Analysis Export - CORRECTED WITH EXACT PROPERTY NAMES

        private void CreateCostOverviewSheet(ExcelWorksheet sheet, CostAnalysisService.CostAnalysisOverview overview)
        {
            sheet.Cells["A1"].Value = "Cost Analysis Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            // Cost KPIs - EXACT PROPERTY NAMES FROM CostAnalysisService.cs
            sheet.Cells["A5"].Value = "PROCUREMENT COST METRICS";
            sheet.Cells["A5"].Style.Font.Bold = true;
            sheet.Cells["A5"].Style.Font.Size = 14;

            var costMetrics = new (string Label, object Value, string Format)[]
            {
        ("Total Procurement Value (USD):", overview.TotalProcurementValueUSD, "$#,##0.00"),
        ("Avg Cost per Liter (USD):", overview.AvgCostPerLiterUSD, "$#,##0.000000"),
        ("Avg Cost per Ton (USD):", overview.AvgCostPerTonUSD, "$#,##0.00"),
        ("Total Purchase Transactions:", overview.TotalPurchaseTransactions, "#,##0"),
        ("Unique Suppliers Used:", overview.UniqueSuppliersUsed, "#,##0"),
        ("Price Volatility Index:", overview.PriceVolatilityIndex, "#,##0.00"),
        ("Best Performing Supplier:", overview.BestPerformingSupplier, "General"),
        ("Worst Performing Supplier:", overview.WorstPerformingSupplier, "General"),
        ("Cost Savings Opportunity (USD):", overview.CostSavingsOpportunityUSD, "$#,##0.00"),
        ("Procurement Efficiency Score:", overview.ProcurementEfficiencyScore, "#,##0.0%"),
        ("Lowest Cost per Liter (USD):", overview.LowestCostPerLiterUSD, "$#,##0.000000"),
        ("Highest Cost per Liter (USD):", overview.HighestCostPerLiterUSD, "$#,##0.000000"),
        ("Most Cost Efficient Month:", overview.MostCostEfficientMonth, "General"),
        ("Least Cost Efficient Month:", overview.LeastCostEfficientMonth, "General")
            };

            for (int i = 0; i < costMetrics.Length; i++)
            {
                var row = 7 + i;
                sheet.Cells[row, 1].Value = costMetrics[i].Label;
                sheet.Cells[row, 1].Style.Font.Bold = true;
                sheet.Cells[row, 2].Value = costMetrics[i].Value;
                if (costMetrics[i].Format.Contains("%"))
                    sheet.Cells[row, 2].Value = (decimal)costMetrics[i].Value / 100;
                if (costMetrics[i].Format != "General")
                    sheet.Cells[row, 2].Style.Numberformat.Format = costMetrics[i].Format;
                sheet.Cells[row, 2].Style.Font.Size = 12;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreatePriceTrendsSheet(ExcelWorksheet sheet, List<CostAnalysisService.PriceTrendAnalysis> priceTrends)
        {
            sheet.Cells["A1"].Value = "Price Trends Analysis";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            var headers = new string[]
            {
        "Month", "Avg Cost/L (USD)", "Avg Cost/T (USD)", "Volume (L)", "Value (USD)", "Transactions",
        "Price Variance %", "Volume Variance %", "Trend", "Volatility", "Best Supplier", "Best Price",
        "Worst Supplier", "Worst Price"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, i + 1].Value = headers[i];
                sheet.Cells[3, i + 1].Style.Font.Bold = true;
                sheet.Cells[3, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSalmon);
            }

            for (int row = 0; row < priceTrends.Count; row++)
            {
                var trend = priceTrends[row];
                var excelRow = row + 4;

                // EXACT PROPERTY NAMES FROM PriceTrendAnalysis
                sheet.Cells[excelRow, 1].Value = trend.Month;
                sheet.Cells[excelRow, 2].Value = trend.AvgCostPerLiterUSD;
                sheet.Cells[excelRow, 3].Value = trend.AvgCostPerTonUSD;
                sheet.Cells[excelRow, 4].Value = trend.TotalVolumeL;
                sheet.Cells[excelRow, 5].Value = trend.TotalValueUSD;
                sheet.Cells[excelRow, 6].Value = trend.TransactionCount;
                sheet.Cells[excelRow, 7].Value = trend.PriceVarianceFromPrevious / 100;
                sheet.Cells[excelRow, 8].Value = trend.VolumeVarianceFromPrevious / 100;
                sheet.Cells[excelRow, 9].Value = trend.TrendDirection;
                sheet.Cells[excelRow, 10].Value = trend.MarketVolatility;
                sheet.Cells[excelRow, 11].Value = trend.BestSupplierThisMonth;
                sheet.Cells[excelRow, 12].Value = trend.BestPriceThisMonth;
                sheet.Cells[excelRow, 13].Value = trend.WorstSupplierThisMonth;
                sheet.Cells[excelRow, 14].Value = trend.WorstPriceThisMonth;

                // Formatting
                sheet.Cells[excelRow, 2].Style.Numberformat.Format = "$#,##0.000000";
                sheet.Cells[excelRow, 3].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 4].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 5].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 7].Style.Numberformat.Format = "0.00%";
                sheet.Cells[excelRow, 8].Style.Numberformat.Format = "0.00%";
                sheet.Cells[excelRow, 10].Style.Numberformat.Format = "#,##0.00";
                sheet.Cells[excelRow, 12].Style.Numberformat.Format = "$#,##0.000000";
                sheet.Cells[excelRow, 14].Style.Numberformat.Format = "$#,##0.000000";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        #endregion

        #region Route Performance Export - CORRECTED WITH EXACT PROPERTY NAMES

        private void CreateRouteOverviewSheet(ExcelWorksheet sheet, RoutePerformanceService.RoutePerformanceOverview overview)
        {
            sheet.Cells["A1"].Value = "Route Performance Overview";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            sheet.Cells["A2"].Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            sheet.Cells["A2"].Style.Font.Italic = true;

            // Route Performance KPIs - EXACT PROPERTY NAMES FROM RoutePerformanceOverview
            sheet.Cells["A5"].Value = "ROUTE PERFORMANCE METRICS";
            sheet.Cells["A5"].Style.Font.Bold = true;
            sheet.Cells["A5"].Style.Font.Size = 14;

            var routeMetrics = new (string Label, object Value, string Format)[]
            {
        ("Total Active Routes:", overview.TotalActiveRoutes, "#,##0"),
        ("Most Efficient Route:", overview.MostEfficientRoute, "General"),
        ("Least Efficient Route:", overview.LeastEfficientRoute, "General"),
        ("Best Route Efficiency (L/Leg):", overview.BestRouteEfficiencyLPerLeg, "#,##0.000"),
        ("Worst Route Efficiency (L/Leg):", overview.WorstRouteEfficiencyLPerLeg, "#,##0.000"),
        ("Total Route Distance (km):", overview.TotalRouteDistanceKm, "#,##0.0"),
        ("Total Legs Completed:", overview.TotalLegsCompleted, "#,##0"),
        ("Total Fuel Consumed (L):", overview.TotalFuelConsumedL, "#,##0.000"),
        ("Total Fuel Consumed (T):", overview.TotalFuelConsumedT, "#,##0.000"),
        ("Total Route Cost (USD):", overview.TotalRouteCostUSD, "$#,##0.00"),
        ("Avg Cost per Km:", overview.AvgCostPerKm, "$#,##0.000000"),
        ("Avg Fuel per Km:", overview.AvgFuelPerKm, "#,##0.000"),
        ("Most Profitable Route:", overview.MostProfitableRoute, "General"),
        ("Least Profitable Route:", overview.LeastProfitableRoute, "General"),
        ("Route Efficiency Gap %:", overview.RouteEfficiencyGap, "#,##0.0%")
            };

            for (int i = 0; i < routeMetrics.Length; i++)
            {
                var row = 7 + i;
                sheet.Cells[row, 1].Value = routeMetrics[i].Label;
                sheet.Cells[row, 1].Style.Font.Bold = true;
                sheet.Cells[row, 2].Value = routeMetrics[i].Value;
                if (routeMetrics[i].Format.Contains("%"))
                    sheet.Cells[row, 2].Value = (decimal)routeMetrics[i].Value / 100;
                if (routeMetrics[i].Format != "General")
                    sheet.Cells[row, 2].Style.Numberformat.Format = routeMetrics[i].Format;
                sheet.Cells[row, 2].Style.Font.Size = 12;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        private void CreateRouteComparisonSheet(ExcelWorksheet sheet, List<RoutePerformanceService.RouteComparison> routeComparisons)
        {
            sheet.Cells["A1"].Value = "Route Performance Comparison";
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.Font.Size = 16;

            var headers = new string[]
            {
        "Route Type", "Description", "Distance (km)", "Active Vessels", "Legs Completed",
        "Fuel Consumed (L)", "Fuel Consumed (T)", "Efficiency L/Leg", "Efficiency T/Leg",
        "Efficiency L/km", "Cost (USD)", "Cost/Leg", "Cost/km", "Utilization %", "Performance Category", "Competitive Advantage %"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cells[3, i + 1].Value = headers[i];
                sheet.Cells[3, i + 1].Style.Font.Bold = true;
                sheet.Cells[3, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                sheet.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightPink);
            }

            for (int row = 0; row < routeComparisons.Count; row++)
            {
                var route = routeComparisons[row];
                var excelRow = row + 4;

                // EXACT PROPERTY NAMES FROM RouteComparison
                sheet.Cells[excelRow, 1].Value = route.RouteType;
                sheet.Cells[excelRow, 2].Value = route.RouteDescription;
                sheet.Cells[excelRow, 3].Value = route.EstimatedDistanceKm;
                sheet.Cells[excelRow, 4].Value = route.ActiveVessels;
                sheet.Cells[excelRow, 5].Value = route.TotalLegsCompleted;
                sheet.Cells[excelRow, 6].Value = route.TotalFuelConsumedL;
                sheet.Cells[excelRow, 7].Value = route.TotalFuelConsumedT;
                sheet.Cells[excelRow, 8].Value = route.AvgEfficiencyLPerLeg;
                sheet.Cells[excelRow, 9].Value = route.AvgEfficiencyTPerLeg;
                sheet.Cells[excelRow, 10].Value = route.AvgEfficiencyLPerKm;
                sheet.Cells[excelRow, 11].Value = route.TotalRouteCostUSD;
                sheet.Cells[excelRow, 12].Value = route.AvgCostPerLeg;
                sheet.Cells[excelRow, 13].Value = route.AvgCostPerKm;
                sheet.Cells[excelRow, 14].Value = route.RouteUtilizationRate / 100;
                sheet.Cells[excelRow, 15].Value = route.PerformanceCategory;
                sheet.Cells[excelRow, 16].Value = route.CompetitiveAdvantage / 100;

                // Formatting
                sheet.Cells[excelRow, 3].Style.Numberformat.Format = "#,##0.0";
                sheet.Cells[excelRow, 6].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 7].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 8].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 9].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 10].Style.Numberformat.Format = "#,##0.000";
                sheet.Cells[excelRow, 11].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 12].Style.Numberformat.Format = "$#,##0.00";
                sheet.Cells[excelRow, 13].Style.Numberformat.Format = "$#,##0.000000";
                sheet.Cells[excelRow, 14].Style.Numberformat.Format = "0.0%";
                sheet.Cells[excelRow, 16].Style.Numberformat.Format = "0.00%";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        }

        #endregion

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