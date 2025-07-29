using System.Windows.Controls;
using DOInventoryManager.Services;

namespace DOInventoryManager.Views.Print
{
    public partial class MonthlySummaryPrint : UserControl
    {
        public MonthlySummaryPrint()
        {
            InitializeComponent();
        }

        public void LoadSummaryData(SummaryService.MonthlySummaryResult summaryData, string month)
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

                // Load consumption data
                if (summaryData.ConsumptionSummary?.Any() == true)
                {
                    for (int i = 0; i < Math.Min(3, summaryData.ConsumptionSummary.Count); i++)
                    {
                        var item = summaryData.ConsumptionSummary[i];
                    }
                }

                ConsumptionDataGrid.ItemsSource = null; // Clear first
                ConsumptionDataGrid.ItemsSource = summaryData.ConsumptionSummary;

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
                TotalAllocationsText.Text = summaryData.AllocationSummary.Count.ToString("N0");
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
    }
}