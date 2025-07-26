using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Data;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            _ = LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                using var context = new InventoryContext();

                // Load statistics
                var totalVessels = await context.Vessels.CountAsync();
                var totalSuppliers = await context.Suppliers.CountAsync();

                // Get purchases with remaining quantities (simplified query)
                var purchases = await context.Purchases
                    .Where(p => p.RemainingQuantity > 0)
                    .ToListAsync();

                // Calculate current inventory (sum of remaining quantities)
                var currentInventory = purchases.Sum(p => p.RemainingQuantity);

                // Calculate inventory value in USD (simplified calculation)
                // Calculate inventory value in USD (based on remaining quantities)
                var inventoryValue = purchases
                    .Where(p => p.RemainingQuantity > 0)
                    .Sum(p => (p.RemainingQuantity / p.QuantityLiters) * p.TotalValueUSD);

                // Update UI
                TotalVesselsText.Text = totalVessels.ToString();
                TotalSuppliersText.Text = totalSuppliers.ToString();
                CurrentInventoryText.Text = currentInventory.ToString("N0");
                InventoryValueText.Text = inventoryValue.ToString("C0");

                // Load recent activity
                await LoadRecentActivity();

                // Hide "No data" message if we have data
                if (totalVessels > 0 || totalSuppliers > 0)
                {
                    NoDataText.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Set default values and show error
                TotalVesselsText.Text = "0";
                TotalSuppliersText.Text = "0";
                CurrentInventoryText.Text = "0";
                InventoryValueText.Text = "$0";

                System.Diagnostics.Debug.WriteLine($"Dashboard error: {ex.Message}");
                // Don't show message box for now, just set defaults
            }
        }

        private async Task LoadRecentActivity()
        {
            try
            {
                using var context = new InventoryContext();

                // Get recent purchases (simplified query)
                var recentPurchases = await context.Purchases
                    .Include(p => p.Vessel)
                    .Include(p => p.Supplier)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(5)
                    .ToListAsync();

                var purchaseActivity = recentPurchases.Select(p => new ActivityItem
                {
                    Date = p.PurchaseDate,
                    Type = "Purchase",
                    Vessel = p.Vessel.Name,
                    Description = $"Fuel purchase from {p.Supplier.Name}",
                    Amount = p.TotalValueUSD
                }).ToList();

                // Get recent consumptions with allocated values
                var recentConsumptions = await context.Consumptions
                    .Include(c => c.Vessel)
                    .Include(c => c.Allocations) // Include allocations to get values
                    .OrderByDescending(c => c.CreatedDate)
                    .Take(5)
                    .ToListAsync();

                var consumptionActivity = recentConsumptions.Select(c => new ActivityItem
                {
                    Date = c.ConsumptionDate,
                    Type = "Consumption",
                    Vessel = c.Vessel.Name,
                    Description = $"Fuel consumption - {c.LegsCompleted} legs",
                    Amount = -c.Allocations.Sum(a => a.AllocatedValueUSD) // Negative value from allocations
                }).ToList();

                // Combine and sort by date
                var allActivity = purchaseActivity
                    .Concat(consumptionActivity)
                    .OrderByDescending(a => a.Date)
                    .Take(10)
                    .ToList();

                RecentActivityGrid.ItemsSource = allActivity;
            }
            catch (Exception ex)
            {
                // Just set empty list if error
                RecentActivityGrid.ItemsSource = new List<ActivityItem>();
                System.Diagnostics.Debug.WriteLine($"Recent activity error: {ex.Message}");
            }
        }

        #region Button Click Events

        private void NewPurchase_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Purchase entry feature coming soon!", "DO Inventory Manager", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddConsumption_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Consumption entry feature coming soon!", "DO Inventory Manager", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunFIFO_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("FIFO allocation feature coming soon!", "DO Inventory Manager", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }

    // Helper class for recent activity display
    public class ActivityItem
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Vessel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        // Formatted amount property for display
        public string FormattedAmount
        {
            get
            {
                if (Amount < 0)
                    return $"({Math.Abs(Amount):C2})"; // Format as ($1,234.56)
                else
                    return Amount.ToString("C2"); // Format as $1,234.56
            }
        }
    }
}