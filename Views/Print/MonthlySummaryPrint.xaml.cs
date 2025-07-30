using DOInventoryManager.Data;
using Microsoft.EntityFrameworkCore;
using DOInventoryManager.Services;
using System.Windows.Controls;

public class VesselConsumptionDetail
{
    public string VesselName { get; set; } = string.Empty;
    public decimal ConsumptionQty { get; set; }
    public int Legs { get; set; }
    public decimal PurchasesQty { get; set; }
    public decimal BeginningBalanceQty { get; set; }
    public decimal BeginningBalanceValue { get; set; }
    public decimal ConsumptionValue { get; set; }
    public decimal LPerLeg => Legs > 0 ? ConsumptionQty / Legs : 0;
    public decimal CostPerLeg => Legs > 0 ? ConsumptionValue / Legs : 0;
    public decimal EndingQty => BeginningBalanceQty + PurchasesQty - ConsumptionQty;
    public decimal EndingValue { get; set; } // This needs to be calculated based on FIFO costs
}

namespace DOInventoryManager.Views.Print
{
    public partial class MonthlySummaryPrint : UserControl
    {
        public MonthlySummaryPrint()
        {
            InitializeComponent();
        }

        public async Task LoadSummaryData(SummaryService.MonthlySummaryResult summaryData, string month)
        {
            try
            {
                // Set report title with month
                ReportTitleText.Text = $"Monthly Summary Report - {month}";

                // Load KPI data from ExecutiveSummary
                TotalConsumptionText.Text = summaryData.ExecutiveSummary.TotalFleetConsumptionL.ToString("N3") + " L";
                TotalLegsText.Text = summaryData.ExecutiveSummary.TotalLegsCompleted.ToString("N0");
                TotalPurchasesText.Text = summaryData.ExecutiveSummary.TotalOperatingCostUSD.ToString("C2");
                EfficiencyText.Text = summaryData.ExecutiveSummary.FleetEfficiencyLPerLeg.ToString("N3") + " L/leg";

                // Load expanded consumption data with detailed calculations
                var expandedConsumptionData = await CreateExpandedConsumptionDataAsync(summaryData, month);
                ConsumptionDataGrid.ItemsSource = null;
                ConsumptionDataGrid.ItemsSource = expandedConsumptionData;
                ConsumptionDataGrid.UpdateLayout();
                System.Diagnostics.Debug.WriteLine($"Expanded ConsumptionDataGrid.Items.Count: {ConsumptionDataGrid.Items.Count}");

                // Load purchase data

                if (summaryData.PurchaseSummary?.Any() == true)
                {
                    for (int i = 0; i < Math.Min(3, summaryData.PurchaseSummary.Count); i++)
                    {
                        var item = summaryData.PurchaseSummary[i];
                        System.Diagnostics.Debug.WriteLine($"Purchase item {i}: {item.SupplierName} - {item.TotalValueUSD:C2} - {item.TotalPurchasesL}L");
                    }
                }

                PurchaseDataGrid.ItemsSource = null; // Clear first
                PurchaseDataGrid.ItemsSource = summaryData.PurchaseSummary;
                PurchaseDataGrid.UpdateLayout();

                // Load FIFO allocation data (calculated from AllocationSummary)
                TotalAllocationsText.Text = summaryData.AllocationSummary.Sum(a => a.AllocationCount).ToString("N0");
                AllocatedQuantityText.Text = summaryData.AllocationSummary.Sum(a => a.AllocatedQuantityL).ToString("N3") + " L";
                AllocatedValueText.Text = summaryData.AllocationSummary.Sum(a => a.AllocatedValueUSD).ToString("C2");
                VesselsProcessedText.Text = summaryData.ExecutiveSummary.VesselsOperated.ToString("N0");

                // Load financial data directly to TextBlocks
                TotalPurchaseValueText.Text = summaryData.FinancialSummary.TotalPurchaseValueUSD.ToString("C2");
                TotalConsumptionValueText.Text = summaryData.FinancialSummary.TotalConsumptionValueUSD.ToString("C2");
                RemainingInventoryValueText.Text = summaryData.FinancialSummary.RemainingInventoryValueUSD.ToString("C2");

                // Calculate total outstanding from payment statuses
                var totalOutstanding = summaryData.FinancialSummary.PaymentStatuses?.Sum(p => p.TotalDue) ?? 0;
                TotalOutstandingText.Text = totalOutstanding.ToString("C2");

                // Force a complete refresh
                this.UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading summary data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task<List<VesselConsumptionDetail>> CreateExpandedConsumptionDataAsync(SummaryService.MonthlySummaryResult summaryData, string month)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Starting CreateExpandedConsumptionDataAsync for month: {month} ===");
                System.Diagnostics.Debug.WriteLine($"Input ConsumptionSummary count: {summaryData.ConsumptionSummary?.Count ?? 0}");

                using var context = new InventoryContext();

                // Parse month for database queries
                var monthParts = month.Split('-');
                var year = int.Parse(monthParts[0]);
                var monthNum = int.Parse(monthParts[1]);

                // Get previous month for beginning balance calculation
                var prevMonth = monthNum == 1 ? 12 : monthNum - 1;
                var prevYear = monthNum == 1 ? year - 1 : year;
                var previousMonth = $"{prevYear:0000}-{prevMonth:00}";

                var expandedData = new List<VesselConsumptionDetail>();

                foreach (var consumptionSummary in summaryData.ConsumptionSummary ?? new List<SummaryService.VesselConsumptionSummary>())
                {
                    // Get vessel ID
                    var vessel = await context.Vessels.FirstOrDefaultAsync(v => v.Name == consumptionSummary.VesselName);
                    if (vessel == null) continue;

                    // Get purchases for this vessel in current month
                    var monthlyPurchases = await context.Purchases
                        .Where(p => p.VesselId == vessel.Id &&
                                   p.PurchaseDate.Year == year &&
                                   p.PurchaseDate.Month == monthNum)
                        .ToListAsync();

                    // Calculate beginning balance from previous month's ending balance
                    var beginningBalance = await CalculatePreviousMonthEndingBalanceAsync(context, vessel.Id, previousMonth);

                    var detail = new VesselConsumptionDetail
                    {
                        VesselName = consumptionSummary.VesselName,
                        ConsumptionQty = consumptionSummary.TotalConsumptionL,
                        Legs = consumptionSummary.TotalLegs,
                        PurchasesQty = monthlyPurchases.Sum(p => p.QuantityLiters),
                        BeginningBalanceQty = beginningBalance.Quantity,
                        BeginningBalanceValue = beginningBalance.Value,
                        ConsumptionValue = consumptionSummary.TotalAllocatedValueUSD,
                        EndingValue = beginningBalance.Value + monthlyPurchases.Sum(p => p.TotalValueUSD) - consumptionSummary.TotalAllocatedValueUSD
                    };

                    expandedData.Add(detail);
                }

                // Add totals row
                if (expandedData.Any())
                {
                    var totalsRow = new VesselConsumptionDetail
                    {
                        VesselName = "TOTAL",
                        ConsumptionQty = expandedData.Sum(d => d.ConsumptionQty),
                        Legs = expandedData.Sum(d => d.Legs),
                        PurchasesQty = expandedData.Sum(d => d.PurchasesQty),
                        BeginningBalanceQty = expandedData.Sum(d => d.BeginningBalanceQty),
                        BeginningBalanceValue = expandedData.Sum(d => d.BeginningBalanceValue),
                        ConsumptionValue = expandedData.Sum(d => d.ConsumptionValue),
                        EndingValue = expandedData.Sum(d => d.EndingValue)
                    };

                    expandedData.Add(totalsRow);
                }

                System.Diagnostics.Debug.WriteLine($"=== Finished CreateExpandedConsumptionDataAsync ===");
                System.Diagnostics.Debug.WriteLine($"Returning {expandedData.Count} items");

                return expandedData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in CreateExpandedConsumptionDataAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<VesselConsumptionDetail>();
            }
        }

        private async Task<(decimal Quantity, decimal Value)> CalculatePreviousMonthEndingBalanceAsync(InventoryContext context, int vesselId, string previousMonth)
        {
            try
            {
                // Get all purchases for this vessel up to end of previous month
                var monthParts = previousMonth.Split('-');
                var prevYear = int.Parse(monthParts[0]);
                var prevMonthNum = int.Parse(monthParts[1]);
                var endOfPrevMonth = new DateTime(prevYear, prevMonthNum, DateTime.DaysInMonth(prevYear, prevMonthNum));

                var purchases = await context.Purchases
                    .Where(p => p.VesselId == vesselId && p.PurchaseDate <= endOfPrevMonth)
                    .ToListAsync();  // Execute query first, then sum in memory

                var totalPurchases = purchases.Sum(p => p.QuantityLiters);
                var totalPurchaseValue = purchases.Sum(p => p.TotalValueUSD);

                var consumptions = await context.Consumptions
                    .Where(c => c.VesselId == vesselId && c.ConsumptionDate <= endOfPrevMonth)
                    .ToListAsync();  // Execute query first, then sum in memory

                var totalConsumption = consumptions.Sum(c => c.ConsumptionLiters);

                var allocations = await context.Allocations
                    .Include(a => a.Consumption)
                    .Where(a => a.Consumption.VesselId == vesselId && a.Consumption.ConsumptionDate <= endOfPrevMonth)
                    .ToListAsync();  // Execute query first, then sum in memory

                var totalConsumptionValue = allocations.Sum(a => a.AllocatedValueUSD);

                return (totalPurchases - totalConsumption, totalPurchaseValue - totalConsumptionValue);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CalculatePreviousMonthEndingBalanceAsync: {ex.Message}");
                return (0, 0);
            }
        }
    }
}