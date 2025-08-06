using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    }
}