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
        private Vessel? _selectedVessel = null;
        private string _currentFilterMonth = "";

        public ConsumptionView()
        {
            InitializeComponent();
            InitializeForm();
            _ = LoadDataAsync();
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
                var totalLegs = monthData.Sum(c => c.LegsCompleted);
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
            UpdateVesselDisplay();
            CalculateConsumptionPerLeg();
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
                var legs = GetIntValue(LegsCompletedTextBox.Text);

                if (consumption > 0 && legs > 0)
                {
                    var consumptionPerLeg = consumption / legs;
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

        private int GetIntValue(string text)
        {
            if (int.TryParse(text, out int value))
                return value;
            return 0;
        }

        private bool ValidateForm()
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

            if (GetIntValue(LegsCompletedTextBox.Text) <= 0)
            {
                MessageBox.Show("Please enter the number of legs completed.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                LegsCompletedTextBox.Focus();
                return false;
            }

            return true;
        }

        #endregion

        #region Event Handlers

        private void VesselComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedVessel = VesselComboBox.SelectedItem as Vessel;
            UpdateVesselDisplay();
        }

        private void ConsumptionDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMonthDisplay();
        }

        private void ConsumptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateConsumptionPerLeg();
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

        #endregion

        #region Button Click Events

        private async void SaveConsumption_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                using var context = new InventoryContext();

                var month = ConsumptionDatePicker.SelectedDate!.Value.ToString("yyyy-MM");

                var consumption = new Consumption
                {
                    VesselId = (int)VesselComboBox.SelectedValue,
                    ConsumptionDate = ConsumptionDatePicker.SelectedDate.Value,
                    ConsumptionLiters = GetDecimalValue(ConsumptionLitersTextBox.Text),
                    Month = month,
                    LegsCompleted = GetIntValue(LegsCompletedTextBox.Text),
                    CreatedDate = DateTime.Now
                };

                context.Consumptions.Add(consumption);
                await context.SaveChangesAsync();

                MessageBox.Show("Consumption record saved successfully!", "Success",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadConsumptionAsync();
                await UpdateMonthlyStatisticsAsync();
                await LoadMonthFiltersAsync(); // Refresh month filter in case new month added
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
            if (!ValidateForm()) return;

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
                    LegsCompleted = GetIntValue(LegsCompletedTextBox.Text),
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
            MessageBox.Show("Monthly Summary report feature coming soon!", "DO Inventory Manager",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}