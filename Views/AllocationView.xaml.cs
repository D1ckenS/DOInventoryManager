using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Data;
using DOInventoryManager.Models;
using DOInventoryManager.Services;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Views
{
    public partial class AllocationView : UserControl
    {
        private readonly FIFOAllocationService _fifoService;
        private string _currentFilterMonth = "";

        public AllocationView()
        {
            InitializeComponent();
            _fifoService = new FIFOAllocationService();
            _ = LoadDataAsync();
        }

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                // Load month filters
                await LoadMonthFiltersAsync();

                // Load allocation data
                await LoadAllocationDataAsync();

                // Update summary
                await UpdateSummaryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading allocation data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMonthFiltersAsync()
        {
            try
            {
                using var context = new InventoryContext();

                // Get months with allocations or consumptions
                var allocationMonths = await context.Allocations
                    .Select(a => a.Month)
                    .Distinct()
                    .ToListAsync();

                var consumptionMonths = await context.Consumptions
                    .Select(c => c.Month)
                    .Distinct()
                    .ToListAsync();

                var allMonths = allocationMonths
                    .Union(consumptionMonths)
                    .OrderByDescending(m => m)
                    .ToList();

                // Add current month if not in list
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                if (!allMonths.Contains(currentMonth))
                {
                    allMonths.Insert(0, currentMonth);
                }

                MonthFilterComboBox.ItemsSource = allMonths;
                MonthFilterComboBox.SelectedItem = allMonths.FirstOrDefault();
                _currentFilterMonth = allMonths.FirstOrDefault() ?? currentMonth;
            }
            catch
            {
                // Default to current month
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                MonthFilterComboBox.ItemsSource = new List<string> { currentMonth };
                MonthFilterComboBox.SelectedItem = currentMonth;
                _currentFilterMonth = currentMonth;
            }
        }

        private async Task LoadAllocationDataAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilterMonth))
                    return;

                var allocations = await _fifoService.GetAllocationsByMonthAsync(_currentFilterMonth);
                AllocationGrid.ItemsSource = allocations;

                CurrentMonthText.Text = _currentFilterMonth;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading allocation data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateSummaryAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilterMonth))
                    return;

                using var context = new InventoryContext();

                var monthAllocations = await context.Allocations
                    .Include(a => a.Purchase)
                        .ThenInclude(p => p.Vessel)
                    .Where(a => a.Month == _currentFilterMonth)
                    .ToListAsync();

                var totalAllocations = monthAllocations.Count;
                var allocatedQuantity = monthAllocations.Sum(a => a.AllocatedQuantity);
                var allocatedValue = monthAllocations.Sum(a => a.AllocatedValueUSD);
                var vesselsProcessed = monthAllocations
                    .Select(a => a.Purchase.VesselId)
                    .Distinct()
                    .Count();

                TotalAllocationsText.Text = totalAllocations.ToString();
                AllocatedQuantityText.Text = $"{allocatedQuantity:N0} L";
                AllocatedValueText.Text = allocatedValue.ToString("C2");
                VesselsProcessedText.Text = vesselsProcessed.ToString();

                // Update status
                if (totalAllocations > 0)
                {
                    StatusText.Text = $"Showing {totalAllocations} allocations for {_currentFilterMonth}";
                    ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    StatusText.Text = "No allocations found for selected month";
                    ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Orange;
                }
            }
            catch
            {
                // Set defaults on error
                TotalAllocationsText.Text = "0";
                AllocatedQuantityText.Text = "0 L";
                AllocatedValueText.Text = "$0.00";
                VesselsProcessedText.Text = "0";
            }
        }

        #endregion

        #region Button Click Events

        private async void RunFIFO_Click(object sender, RoutedEventArgs e)
        {
            // Confirm before running
            var result = MessageBox.Show(
                "This will run FIFO allocation for all unallocated consumption records.\n\n" +
                "This process will:\n" +
                "• Allocate consumption against purchases (oldest first)\n" +
                "• Update remaining quantities on purchases\n" +
                "• Create allocation records\n\n" +
                "Do you want to proceed?",
                "Confirm FIFO Allocation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Update UI to show processing
                StatusText.Text = "Running FIFO allocation...";
                ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Orange;
                RunFIFOBtn.IsEnabled = false;

                // Run FIFO allocation
                var allocationResult = await _fifoService.RunFIFOAllocationAsync();

                // Update process log
                ProcessLogText.Text = string.Join("\n", allocationResult.Details);

                // Show result message
                if (allocationResult.Success)
                {
                    MessageBox.Show(allocationResult.Message, "FIFO Allocation Completed",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    StatusText.Text = "FIFO allocation completed successfully";
                    ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    MessageBox.Show(allocationResult.Message, "FIFO Allocation Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);

                    StatusText.Text = "FIFO allocation failed";
                    ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Red;
                }

                // Refresh data
                await LoadAllocationDataAsync();
                await UpdateSummaryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running FIFO allocation: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);

                StatusText.Text = "FIFO allocation error";
                ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                RunFIFOBtn.IsEnabled = true;
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void MonthFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonthFilterComboBox.SelectedItem != null)
            {
                _currentFilterMonth = MonthFilterComboBox.SelectedItem.ToString() ?? "";
                await LoadAllocationDataAsync();
                await UpdateSummaryAsync();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Excel export feature coming soon!", "DO Inventory Manager",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}