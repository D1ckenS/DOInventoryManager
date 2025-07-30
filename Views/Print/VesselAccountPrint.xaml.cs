using System;
using System.Linq;
using System.Windows.Controls;
using DOInventoryManager.Services;

namespace DOInventoryManager.Views.Print
{
    public partial class VesselAccountPrint : UserControl
    {
        public VesselAccountPrint()
        {
            InitializeComponent();
        }

        public async Task LoadVesselAccountData(ReportService.VesselAccountStatementResult accountData, string vesselName, DateTime fromDate, DateTime toDate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadVesselAccountData called for vessel: {vesselName}");

                // Simulate async operation if needed
                await Task.Delay(10); // Remove this if you add real async logic

                ReportTitleText.Text = $"Vessel Account Statement - {vesselName}";

                if (accountData.Summary != null)
                {
                    TotalPurchasesText.Text = accountData.Summary.TotalPurchases.ToString("N0") + " L";
                    TotalConsumptionText.Text = accountData.Summary.TotalConsumption.ToString("N0") + " L";
                    CurrentBalanceText.Text = accountData.Summary.CurrentBalance.ToString("N0") + " L";
                    TotalValueText.Text = accountData.Summary.TotalValue.ToString("C0");
                }

                VesselNameText.Text = vesselName;
                FromDateText.Text = fromDate.ToString("dd/MM/yyyy");
                ToDateText.Text = toDate.ToString("dd/MM/yyyy");

                var beginningBalance = await Task.Run(() => CalculateBeginningBalance(accountData));
                BeginningBalanceText.Text = beginningBalance.ToString("N0") + " L";

                TransactionDataGrid.ItemsSource = accountData.Transactions;
                TransactionDataGrid.UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private decimal CalculateBeginningBalance(ReportService.VesselAccountStatementResult accountData)
        {
            try
            {
                if (accountData.Transactions?.Any() != true)
                    return 0;

                // Get the first transaction
                var firstTransaction = accountData.Transactions.OrderBy(t => t.TransactionDate).First();

                // Beginning balance = Running balance after first transaction - net effect of first transaction
                var netEffect = firstTransaction.DebitQuantity - firstTransaction.CreditQuantity;
                var beginningBalance = firstTransaction.RunningBalance - netEffect;

                return beginningBalance;
            }
            catch
            {
                return 0;
            }
        }
    }
}