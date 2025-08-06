using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using DOInventoryManager.Services;

namespace DOInventoryManager.Views
{
    public partial class ConsumptionView : UserControl
    {
        // Event to request navigation to other views
        public event Action<string>? NavigationRequested;
        
        private Vessel? _selectedVessel = null;
        private string _currentFilterMonth = "";
        private Consumption? _editingConsumption = null;
        private bool _isEditMode = false;

        public ConsumptionView()
        {
            InitializeComponent();
            InitializeForm();
            _ = LoadDataAsync();
            
            // Smooth scrolling temporarily disabled due to scroll conflicts
            // SmoothScrollingService.AutoEnableSmoothScrolling(this);
        }

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                using var context = new InventoryContext();

                // Load vessels
                var vessels = await context.Vessels
                    .OrderBy(v => v.Name)
                    .ToListAsync();
                VesselComboBox.ItemsSource = vessels;

                // Load months for filter
                await LoadMonthFiltersAsync();

                // Load consumption records
                await LoadConsumptionAsync();

                // Update statistics
                await UpdateMonthlyStatisticsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMonthFiltersAsync()
        {
            try
            {
                using var context = new InventoryContext();

                // Get unique months from consumption records
                var months = await context.Consumptions
                    .Select(c => c.Month)
                    .Distinct()
                    .OrderByDescending(m => m)
                    .ToListAsync();

                // Add current month if not in list
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                if (!months.Contains(currentMonth))
                {
                    months.Insert(0, currentMonth);
                }

                MonthFilterComboBox.ItemsSource = months;
                MonthFilterComboBox.SelectedItem = currentMonth;
                _currentFilterMonth = currentMonth;
            }
            catch
            {
                // Set default to current month if loading fails
                var currentMonth = DateTime.Now.ToString("yyyy-MM");
                MonthFilterComboBox.ItemsSource = new List<string> { currentMonth };
                MonthFilterComboBox.SelectedItem = currentMonth;
                _currentFilterMonth = currentMonth;
            }
        }

        private async Task LoadConsumptionAsync()
        {
            try
            {
                using var context = new InventoryContext();

                var query = context.Consumptions
                    .Include(c => c.Vessel)
                    .AsQueryable();

                // Filter by month if selected
                if (!string.IsNullOrEmpty(_currentFilterMonth))
                {
                    query = query.Where(c => c.Month == _currentFilterMonth);
                }

                var consumptions = await query
                    .OrderByDescending(c => c.ConsumptionDate)
                    .ToListAsync();

                ConsumptionGrid.ItemsSource = consumptions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading consumption records: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateMonthlyStatisticsAsync()
        {
            try
            {
                using var context = new InventoryContext();

                var monthData = await context.Consumptions
                    .Where(c => c.Month == _currentFilterMonth)
                    .ToListAsync();

                var totalConsumption = monthData.Sum(c => c.ConsumptionLiters);
                var totalLegs = monthData.Sum(c => c.LegsCompleted ?? 0);
                var avgPerLeg = totalLegs > 0 ? totalConsumption / totalLegs : 0;

                CurrentMonthText.Text = _currentFilterMonth;
                TotalConsumptionText.Text = $"{totalConsumption:N0} L";
                TotalLegsText.Text = totalLegs.ToString();
                AvgPerLegText.Text = $"{avgPerLeg:N2} L/leg";
            }
            catch
            {
                // Set defaults on error
                CurrentMonthText.Text = _currentFilterMonth;
                TotalConsumptionText.Text = "0 L";
                TotalLegsText.Text = "0";
                AvgPerLegText.Text = "0.00 L/leg";
            }
        }

        private async Task<decimal> GetAvailableInventoryAsync(int vesselId)
        {
            try
            {
                using var context = new InventoryContext();

                // Sum remaining quantities for this vessel
                var remainingQuantities = await context.Purchases
                    .Where(p => p.VesselId == vesselId)
                    .Select(p => p.RemainingQuantity)
                    .ToListAsync();

                return remainingQuantities.Sum();
            }
            catch
            {
                return 0; // Return 0 if error to be safe
            }
        }

        private async Task<decimal> GetFIFODensityAsync(int vesselId)
        {
            try
            {
                using var context = new InventoryContext();

                // Get oldest purchase with remaining quantity (FIFO logic)
                var oldestPurchase = await context.Purchases
                    .Where(p => p.VesselId == vesselId && p.RemainingQuantity > 0)
                    .OrderBy(p => p.PurchaseDate)
                    .ThenBy(p => p.Id)
                    .FirstOrDefaultAsync();

                return oldestPurchase?.Density ?? 0.85m; // Default density if no purchases found
            }
            catch
            {
                return 0.834m; // Safe default density
            }
        }

        #endregion

        #region Form Management

        private void InitializeForm()
        {
            // Set current month
            var currentMonth = DateTime.Now.ToString("yyyy-MM");

            // Add null checks
            if (CurrentMonthText != null)
                CurrentMonthText.Text = currentMonth;
            if (MonthDisplayText != null)
                MonthDisplayText.Text = currentMonth;

            ClearForm();
        }

        private void ClearForm()
        {
            // Add null checks for all UI elements
            if (VesselComboBox == null || ConsumptionDatePicker == null ||
                ConsumptionLitersTextBox == null || LegsCompletedTextBox == null)
                return;

            VesselComboBox.SelectedIndex = -1;
            ConsumptionDatePicker.SelectedDate = DateTime.Now;
            ConsumptionLitersTextBox.Text = "";
            LegsCompletedTextBox.Text = "";

            _selectedVessel = null;
            ExitEditMode(); // Exit edit mode
            UpdateVesselDisplay();
            _ = CalculateConsumptionTonsAsync();
            UpdateMonthDisplay();
        }

        private void UpdateVesselDisplay()
        {
            if (_selectedVessel != null)
            {
                RouteDisplayText.Text = _selectedVessel.Route;
                VesselTypeText.Text = _selectedVessel.Type;

                // Set colors based on vessel type
                if (_selectedVessel.Type == "Vessel")
                {
                    RouteDisplayText.Foreground = System.Windows.Media.Brushes.DarkBlue;
                }
                else if (_selectedVessel.Type == "Boat")
                {
                    RouteDisplayText.Foreground = System.Windows.Media.Brushes.DarkGreen;
                }
            }
            else
            {
                RouteDisplayText.Text = "Select vessel to see route";
                RouteDisplayText.Foreground = System.Windows.Media.Brushes.Gray;
                VesselTypeText.Text = "Not Selected";
            }
        }

        private void UpdateMonthDisplay()
        {
            // Add null check for MonthDisplayText
            if (MonthDisplayText == null) return;

            if (ConsumptionDatePicker?.SelectedDate.HasValue == true)
            {
                var month = ConsumptionDatePicker.SelectedDate.Value.ToString("yyyy-MM");
                MonthDisplayText.Text = month;
            }
            else
            {
                MonthDisplayText.Text = DateTime.Now.ToString("yyyy-MM");
            }
        }

        private void CalculateConsumptionPerLeg()
        {
            try
            {
                var consumption = GetDecimalValue(ConsumptionLitersTextBox.Text);
                var legs = GetNullableIntValue(LegsCompletedTextBox.Text);

                if (consumption > 0 && legs.HasValue && legs > 0)
                {
                    var consumptionPerLeg = consumption / legs.Value;
                    ConsumptionPerLegText.Text = $"{consumptionPerLeg:N2} L/leg";

                    // Calculate efficiency rating (basic example)
                    string efficiency;
                    if (consumptionPerLeg < 100)
                        efficiency = "Excellent";
                    else if (consumptionPerLeg < 200)
                        efficiency = "Good";
                    else if (consumptionPerLeg < 300)
                        efficiency = "Average";
                    else
                        efficiency = "Needs Review";

                    EfficiencyRatingText.Text = efficiency;
                }
                else if (consumption > 0 && (!legs.HasValue || legs == 0))
                {
                    ConsumptionPerLegText.Text = "Stationary Operation";
                    EfficiencyRatingText.Text = "No Movement";
                }
                else
                {
                    ConsumptionPerLegText.Text = "0.00 L/leg";
                    EfficiencyRatingText.Text = "Calculate";
                }
            }
            catch
            {
                ConsumptionPerLegText.Text = "0.00 L/leg";
                EfficiencyRatingText.Text = "Error";
            }
        }

        private decimal GetDecimalValue(string text)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                return value;
            return 0;
        }

        private int? GetNullableIntValue(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;
            if (int.TryParse(text, out int value))
                return value;
            return null;
        }

        private int GetIntValue(string text)
        {
            if (int.TryParse(text, out int value))
                return value;
            return 0;
        }

        private async Task<bool> ValidateFormAsync()
        {
            if (VesselComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a vessel.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                VesselComboBox.Focus();
                return false;
            }

            if (!ConsumptionDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a consumption date.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                ConsumptionDatePicker.Focus();
                return false;
            }

            if (GetDecimalValue(ConsumptionLitersTextBox.Text) <= 0)
            {
                MessageBox.Show("Please enter a valid consumption amount in liters.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                ConsumptionLitersTextBox.Focus();
                return false;
            }

            // Legs completed can be 0 or null for stationary consumption (engines running without movement)
            // No validation needed for legs - allow 0, null, or any positive number

            // Add inventory validation
            if (!await ValidateInventoryAsync())
            {
                ConsumptionLitersTextBox.Focus();
                return false;
            }

            return true;
        }

        private async Task<bool> ValidateInventoryAsync()
        {
            if (VesselComboBox.SelectedValue == null) return true;

            var vesselId = (int)VesselComboBox.SelectedValue;
            var consumptionAmount = GetDecimalValue(ConsumptionLitersTextBox.Text);

            if (consumptionAmount <= 0) return true;

            var availableInventory = await GetAvailableInventoryAsync(vesselId);

            if (consumptionAmount > availableInventory)
            {
                var vesselName = ((Vessel)VesselComboBox.SelectedItem)?.Name ?? "Unknown";

                MessageBox.Show(
                    $"⚠️ INVENTORY SHORTAGE ALERT\n\n" +
                    $"Vessel: {vesselName}\n" +
                    $"Consumption Requested: {consumptionAmount:N2} L\n" +
                    $"Available Inventory: {availableInventory:N2} L\n" +
                    $"Shortage: {(consumptionAmount - availableInventory):N2} L\n\n" +
                    $"This consumption exceeds available fuel inventory.\n\n" +
                    $"Options:\n" +
                    $"• Reduce consumption amount to {availableInventory:N2} L or less\n" +
                    $"• Add fuel purchases for this vessel first\n" +
                    $"• Proceed anyway (will create allocation deficit)",
                    "Inventory Shortage Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            return true;
        }

        private async Task UpdateInventoryDisplayAsync()
        {
            if (VesselComboBox.SelectedValue == null)
            {
                // Update efficiency rating text to show inventory info
                EfficiencyRatingText.Text = "Select Vessel";
                EfficiencyRatingText.Foreground = System.Windows.Media.Brushes.Gray;
                return;
            }

            var vesselId = (int)VesselComboBox.SelectedValue;
            var availableInventory = await GetAvailableInventoryAsync(vesselId);
            var consumptionAmount = GetDecimalValue(ConsumptionLitersTextBox.Text);

            if (consumptionAmount > 0 && consumptionAmount > availableInventory)
            {
                var shortage = consumptionAmount - availableInventory;
                EfficiencyRatingText.Text = $"⚠️ {shortage:N0}L Short";
                EfficiencyRatingText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (availableInventory > 0)
            {
                EfficiencyRatingText.Text = $"✅ {availableInventory:N0}L Available";
                EfficiencyRatingText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                EfficiencyRatingText.Text = "❌ No Inventory";
                EfficiencyRatingText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private async Task CalculateConsumptionTonsAsync()
        {
            try
            {
                var consumption = GetDecimalValue(ConsumptionLitersTextBox.Text);
                var legs = GetNullableIntValue(LegsCompletedTextBox.Text);
                var vesselId = VesselComboBox.SelectedValue as int?;

                if (consumption > 0 && vesselId.HasValue)
                {
                    var density = await GetFIFODensityAsync(vesselId.Value);
                    var consumptionTons = (consumption / 1000) * density;

                    // Update consumption per leg calculation
                    if (legs.HasValue && legs > 0)
                    {
                        var consumptionPerLeg = consumption / legs.Value;
                        var tonsPerLeg = consumptionTons / legs.Value;
                        ConsumptionPerLegText.Text = $"{consumptionPerLeg:N3} L/leg | {tonsPerLeg:N3} T/leg";
                    }
                    else if (!legs.HasValue || legs == 0)
                    {
                        ConsumptionPerLegText.Text = "Stationary Operation | No Movement";
                    }
                    else
                    {
                        ConsumptionPerLegText.Text = $"0.000 L/leg | 0.000 T/leg";
                    }

                    // Update vessel type to show consumption tons
                    VesselTypeText.Text = $"{consumptionTons:N3} T";
                }
                else
                {
                    ConsumptionPerLegText.Text = $"0.000 L/leg | 0.000 T/leg";
                    VesselTypeText.Text = _selectedVessel?.Type ?? "Not Selected";
                }
            }
            catch
            {
                ConsumptionPerLegText.Text = $"0.000 L/leg | 0.000 T/leg";
                VesselTypeText.Text = _selectedVessel?.Type ?? "Not Selected";
            }
        }

        private void EnableEditMode(Consumption consumption)
        {
            _editingConsumption = consumption;
            _isEditMode = true;

            // Populate form with consumption data
            VesselComboBox.SelectedValue = consumption.VesselId;
            ConsumptionDatePicker.SelectedDate = consumption.ConsumptionDate;
            ConsumptionLitersTextBox.Text = consumption.ConsumptionLiters.ToString("F3");
            LegsCompletedTextBox.Text = consumption.LegsCompleted?.ToString() ?? "";

            // Update UI
            SaveConsumptionBtn.Content = "💾 Update Consumption";
            RunFIFOBtn.Content = "🔄 Update & Run FIFO";
            EditConsumptionBtn.IsEnabled = false;
            DeleteConsumptionBtn.IsEnabled = false;
        }

        private void ExitEditMode()
        {
            _editingConsumption = null;
            _isEditMode = false;
            SaveConsumptionBtn.Content = "💾 Save Consumption";
            RunFIFOBtn.Content = "🔄 Save & Run FIFO";
            EditConsumptionBtn.IsEnabled = false;
            DeleteConsumptionBtn.IsEnabled = false;
        }

        private async Task<bool> ValidateConsumptionEditAsync()
        {
            if (_editingConsumption == null) return true;

            try
            {
                using var context = new InventoryContext();

                // Check if this consumption has allocations
                var hasAllocations = await context.Allocations
                    .AnyAsync(a => a.ConsumptionId == _editingConsumption.Id);

                if (hasAllocations)
                {
                    var newQuantity = GetDecimalValue(ConsumptionLitersTextBox.Text);
                    var oldQuantity = _editingConsumption.ConsumptionLiters;

                    if (Math.Abs(newQuantity - oldQuantity) > 0.001m) // Allow small rounding differences
                    {
                        var result = MessageBox.Show(
                            $"⚠️ ALLOCATION IMPACT WARNING\n\n" +
                            $"This consumption record has FIFO allocations.\n\n" +
                            $"Original Quantity: {oldQuantity:N3} L\n" +
                            $"New Quantity: {newQuantity:N3} L\n\n" +
                            $"Changing the quantity will affect existing allocations.\n" +
                            $"You may need to re-run FIFO allocation afterward.\n\n" +
                            $"Continue with the change?",
                            "Allocation Impact Warning",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        return result == MessageBoxResult.Yes;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating consumption edit: {ex.Message}", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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

        private async void VesselComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedVessel = VesselComboBox.SelectedItem as Vessel;
            UpdateVesselDisplay();
            await UpdateInventoryDisplayAsync();
            await CalculateConsumptionTonsAsync();
        }

        private void ConsumptionDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMonthDisplay();
        }

        private async void ConsumptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateConsumptionPerLeg();
            await UpdateInventoryDisplayAsync();
            await CalculateConsumptionTonsAsync();
        }

        private async void MonthFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonthFilterComboBox.SelectedItem != null)
            {
                _currentFilterMonth = MonthFilterComboBox.SelectedItem.ToString() ?? "";
                await LoadConsumptionAsync();
                await UpdateMonthlyStatisticsAsync();
            }
        }

        private void ConsumptionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedConsumption = ConsumptionGrid.SelectedItem as Consumption;

            if (selectedConsumption != null)
            {
                EditConsumptionBtn.IsEnabled = true;
                DeleteConsumptionBtn.IsEnabled = true;
            }
            else
            {
                EditConsumptionBtn.IsEnabled = false;
                DeleteConsumptionBtn.IsEnabled = false;
            }
        }

        #endregion

        #region Button Click Events

        private async void SaveConsumption_Click(object sender, RoutedEventArgs e)
        {
            if (!await ValidateFormAsync()) return;

            // Additional validation for edits
            if (_isEditMode && !await ValidateConsumptionEditAsync()) return;

            try
            {
                using var context = new InventoryContext();

                var month = ConsumptionDatePicker.SelectedDate!.Value.ToString("yyyy-MM");

                if (_isEditMode && _editingConsumption != null)
                {
                    // Update existing consumption
                    var consumptionToUpdate = await context.Consumptions
                        .FindAsync(_editingConsumption.Id);

                    if (consumptionToUpdate != null)
                    {
                        consumptionToUpdate.VesselId = (int)VesselComboBox.SelectedValue;
                        consumptionToUpdate.ConsumptionDate = ConsumptionDatePicker.SelectedDate.Value;
                        consumptionToUpdate.ConsumptionLiters = GetDecimalValue(ConsumptionLitersTextBox.Text);
                        consumptionToUpdate.Month = month;
                        consumptionToUpdate.LegsCompleted = GetNullableIntValue(LegsCompletedTextBox.Text);

                        await context.SaveChangesAsync();

                        await CreateAutoBackupAsync(_isEditMode ? "ConsumptionEdit" : "ConsumptionAdd");

                        MessageBox.Show(_isEditMode ? "Consumption record updated successfully!" : "Consumption record saved successfully!", "Success",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Create new consumption
                    var consumption = new Consumption
                    {
                        VesselId = (int)VesselComboBox.SelectedValue,
                        ConsumptionDate = ConsumptionDatePicker.SelectedDate.Value,
                        ConsumptionLiters = GetDecimalValue(ConsumptionLitersTextBox.Text),
                        Month = month,
                        LegsCompleted = GetNullableIntValue(LegsCompletedTextBox.Text),
                        CreatedDate = DateTime.Now
                    };

                    context.Consumptions.Add(consumption);
                    await context.SaveChangesAsync();

                    MessageBox.Show("Consumption record saved successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await LoadConsumptionAsync();
                await UpdateMonthlyStatisticsAsync();
                await LoadMonthFiltersAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving consumption: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RunFIFO_Click(object sender, RoutedEventArgs e)
        {
            // First save the consumption
            if (!await ValidateFormAsync()) return;

            try
            {
                // Save consumption first
                using var context = new InventoryContext();

                var month = ConsumptionDatePicker.SelectedDate!.Value.ToString("yyyy-MM");

                var consumption = new Consumption
                {
                    VesselId = (int)VesselComboBox.SelectedValue,
                    ConsumptionDate = ConsumptionDatePicker.SelectedDate.Value,
                    ConsumptionLiters = GetDecimalValue(ConsumptionLitersTextBox.Text),
                    Month = month,
                    LegsCompleted = GetNullableIntValue(LegsCompletedTextBox.Text),
                    CreatedDate = DateTime.Now
                };

                context.Consumptions.Add(consumption);
                await context.SaveChangesAsync();

                // Run FIFO allocation
                var fifoService = new FIFOAllocationService();
                var result = await fifoService.RunFIFOAllocationAsync();

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Consumption saved and FIFO allocation completed!\n\n" +
                        $"• Processed: {result.ProcessedConsumptions} consumption records\n" +
                        $"• Created: {result.AllocationsCreated} allocations\n" +
                        $"• Total allocated: {result.TotalAllocatedQuantity:N2} L\n" +
                        $"• Total value: {result.TotalAllocatedValue:C2}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"Consumption saved but FIFO allocation had issues:\n\n{result.Message}",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                await LoadConsumptionAsync();
                await UpdateMonthlyStatisticsAsync();
                await LoadMonthFiltersAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving consumption or running FIFO: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void RefreshConsumption_Click(object sender, RoutedEventArgs e)
        {
            await LoadConsumptionAsync();
            await UpdateMonthlyStatisticsAsync();
        }

        private void MonthSummary_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Reports view for Monthly Summary
            NavigationRequested?.Invoke("Reports");
        }

        private void EditConsumption_Click(object sender, RoutedEventArgs e)
        {
            var selectedConsumption = ConsumptionGrid.SelectedItem as Consumption;
            if (selectedConsumption != null)
            {
                EnableEditMode(selectedConsumption);
            }
        }

        private async void DeleteConsumption_Click(object sender, RoutedEventArgs e)
        {
            var selectedConsumption = ConsumptionGrid.SelectedItem as Consumption;
            if (selectedConsumption == null) return;

            try
            {
                using var context = new InventoryContext();

                // Check if consumption has allocations
                var allocations = await context.Allocations
                    .Where(a => a.ConsumptionId == selectedConsumption.Id)
                    .ToListAsync();

                string warningMessage;
                if (allocations.Count > 0)
                {
                    var totalAllocated = allocations.Sum(a => a.AllocatedQuantity);
                    warningMessage =
                        $"⚠️ ALLOCATION IMPACT WARNING\n\n" +
                        $"This consumption has {allocations.Count} FIFO allocations totaling {totalAllocated:N3} L.\n\n" +
                        $"Deleting this consumption will:\n" +
                        $"• Remove all related allocations\n" +
                        $"• Increase remaining quantities on related purchases\n" +
                        $"• May affect other consumption allocations\n\n" +
                        $"Consumption Details:\n" +
                        $"Date: {selectedConsumption.ConsumptionDate:dd/MM/yyyy}\n" +
                        $"Vessel: {selectedConsumption.Vessel?.Name}\n" +
                        $"Quantity: {selectedConsumption.ConsumptionLiters:N3} L\n" +
                        $"Legs: {selectedConsumption.LegsCompleted?.ToString() ?? "Stationary"}\n\n" +
                        $"Consider re-running FIFO allocation after deletion.\n\n" +
                        $"Are you sure you want to proceed?";
                }
                else
                {
                    warningMessage =
                        $"Are you sure you want to delete this consumption?\n\n" +
                        $"Date: {selectedConsumption.ConsumptionDate:dd/MM/yyyy}\n" +
                        $"Vessel: {selectedConsumption.Vessel?.Name}\n" +
                        $"Quantity: {selectedConsumption.ConsumptionLiters:N3} L\n" +
                        $"Legs: {selectedConsumption.LegsCompleted?.ToString() ?? "Stationary"}\n\n" +
                        $"This action cannot be undone.";
                }

                var result = MessageBox.Show(warningMessage, "Confirm Delete Consumption",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                // Delete related allocations first
                if (allocations.Count > 0)
                {
                    // Restore remaining quantities to purchases
                    foreach (var allocation in allocations)
                    {
                        var purchase = await context.Purchases.FindAsync(allocation.PurchaseId);
                        if (purchase != null)
                        {
                            purchase.RemainingQuantity += allocation.AllocatedQuantity;
                        }
                    }

                    context.Allocations.RemoveRange(allocations);
                }

                // Delete the consumption
                var consumptionToDelete = await context.Consumptions
                    .FindAsync(selectedConsumption.Id);

                if (consumptionToDelete != null)
                {
                    context.Consumptions.Remove(consumptionToDelete);
                    await context.SaveChangesAsync();

                    await CreateAutoBackupAsync("ConsumptionDelete");

                    MessageBox.Show(
                        $"Consumption deleted successfully!\n\n" +
                        $"• Removed {allocations.Count} related allocations\n" +
                        $"• Restored quantities to affected purchases\n\n" +
                        $"Consider re-running FIFO allocation to optimize remaining allocations.",
                        "Deletion Completed", MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadConsumptionAsync();
                    await UpdateMonthlyStatisticsAsync();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting consumption: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            var scrollViewer = FindParent<ScrollViewer>(dataGrid);
            if (scrollViewer == null) return;

            // Check if DataGrid needs internal scrolling
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

            // Redirect to parent ScrollViewer
            e.Handled = true;
            var newEvent = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent
            };
            scrollViewer.RaiseEvent(newEvent);
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