using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Data;
using DOInventoryManager.Models;
using DOInventoryManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;

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
                AllocatedQuantityText.Text = $"{allocatedQuantity:N3} L";
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

                if (allocationResult.Success)
                {
                    await CreateAutoBackupAsync("FIFO");
                }

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

        private async void Recovery_Click(object sender, RoutedEventArgs e)
        {
            var recoveryService = new DataRecoveryService();

            // First show current issues
            var issues = await recoveryService.GetDataInconsistencyReportAsync();
            var issueReport = string.Join("\n", issues);

            var choice = MessageBox.Show(
                $"Data Consistency Report:\n{issueReport}\n\n" +
                "Choose recovery option:\n\n" +
                "YES = Re-run Complete FIFO Allocation (clears all allocations and recalculates)\n" +
                "NO = Manual Cleanup (fixes inconsistencies, keeps valid allocations)\n" +
                "CANCEL = Do nothing",
                "Data Recovery Options",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (choice == MessageBoxResult.Cancel) return;

            try
            {
                StatusText.Text = "Running data recovery...";
                ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Orange;

                DataRecoveryService.RecoveryResult result;

                if (choice == MessageBoxResult.Yes)
                {
                    result = await recoveryService.RerunFIFOAllocationAsync();
                }
                else
                {
                    result = await recoveryService.ManualCleanupInconsistentDataAsync();
                }

                // Update process log
                ProcessLogText.Text = string.Join("\n", result.Details);

                // Show result
                MessageBox.Show(result.Message,
                              result.Success ? "Recovery Completed" : "Recovery Failed",
                              MessageBoxButton.OK,
                              result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                if (result.Success)
                {
                    await CreateAutoBackupAsync(choice == MessageBoxResult.Yes ? "DataRecovery-Complete" : "DataRecovery-Manual");

                    StatusText.Text = "Data recovery completed successfully";
                    ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Green;

                    // Refresh all data
                    await LoadAllocationDataAsync();
                    await UpdateSummaryAsync();
                }
                else
                {
                    StatusText.Text = "Data recovery failed";
                    ((Border)StatusText.Parent).Background = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during recovery: {ex.Message}", "Recovery Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilterMonth))
                {
                    MessageBox.Show("Please select a month to export allocation data.", "No Month Selected",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get allocation data for the selected month
                using var context = new InventoryContext();
                var allocations = await context.Allocations
                    .Include(a => a.Purchase)
                        .ThenInclude(p => p.Vessel)
                    .Include(a => a.Purchase)
                        .ThenInclude(p => p.Supplier)
                    .Include(a => a.Consumption)
                        .ThenInclude(c => c.Vessel)
                    .Where(a => a.Month == _currentFilterMonth)
                    .OrderByDescending(a => a.Purchase.PurchaseDate)
                    .ToListAsync();

                if (!allocations.Any())
                {
                    MessageBox.Show($"No allocation data found for {_currentFilterMonth}.", "No Data",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var exportService = new ExcelExportService();
                var filePath = await exportService.ExportAllocationDataToExcelAsync(allocations, _currentFilterMonth);

                var result = MessageBox.Show($"Allocation data exported successfully!\n\nFile: {Path.GetFileName(filePath)}\n\nWould you like to open the file?",
                                           "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting allocation data: {ex.Message}", "Export Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}