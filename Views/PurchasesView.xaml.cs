using DOInventoryManager.Data;
using DOInventoryManager.Models;
using DOInventoryManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace DOInventoryManager.Views
{
    public partial class PurchasesView : UserControl
    {
        private Supplier? _selectedSupplier = null;
        private Purchase? _editingPurchase = null;
        private bool _isEditMode = false;

        public PurchasesView()
        {
            InitializeComponent();
            _ = LoadDataAsync();
            ClearForm();
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

                // Load suppliers
                var suppliers = await context.Suppliers
                    .OrderBy(s => s.Name)
                    .ToListAsync();
                SupplierComboBox.ItemsSource = suppliers;

                // Load recent purchases
                await LoadPurchasesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPurchasesAsync()
        {
            try
            {
                using var context = new InventoryContext();
                var purchases = await context.Purchases
                    .Include(p => p.Vessel)
                    .Include(p => p.Supplier)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Take(50) // Show last 50 purchases
                    .ToListAsync();

                PurchasesGrid.ItemsSource = purchases;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading purchases: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Form Management

        private void ClearForm()
        {
            VesselComboBox.SelectedIndex = -1;
            SupplierComboBox.SelectedIndex = -1;
            PurchaseDatePicker.SelectedDate = DateTime.Now;
            QuantityLitersTextBox.Text = "";
            QuantityTonsTextBox.Text = "";
            InvoiceRefTextBox.Text = "";
            TotalValueTextBox.Text = "";
            InvoiceReceiptDatePicker.SelectedDate = null;
            DueDatePicker.SelectedDate = null;

            _selectedSupplier = null;
            ExitEditMode(); // Exit edit mode
            UpdateCurrencyDisplay();
            CalculateValues();
            UpdatePaymentStatus();
        }

        private void UpdateCurrencyDisplay()
        {
            if (_selectedSupplier != null)
            {
                TotalValueLabel.Text = $"Total Value ({_selectedSupplier.Currency}) *";
                ExchangeRateTextBox.Text = _selectedSupplier.ExchangeRate.ToString("F6", CultureInfo.InvariantCulture);
                ExchangeRateDisplayText.Text = _selectedSupplier.ExchangeRate.ToString("F3");

                // Enable exchange rate editing for non-USD and non-JOD currencies
                if (_selectedSupplier.Currency != "USD" && _selectedSupplier.Currency != "JOD")
                {
                    ExchangeRateTextBox.IsEnabled = true;
                    ExchangeRateTextBox.Background = System.Windows.Media.Brushes.White;
                }
                else
                {
                    ExchangeRateTextBox.IsEnabled = false;
                    ExchangeRateTextBox.Background = System.Windows.Media.Brushes.LightGray;
                }
            }
            else
            {
                TotalValueLabel.Text = "Total Value *";
                ExchangeRateTextBox.Text = "1.000000";
                ExchangeRateTextBox.IsEnabled = false;
                ExchangeRateDisplayText.Text = "1.000";
            }
        }

        private void CalculateValues()
        {
            try
            {
                // Get input values
                var quantityLiters = GetDecimalValue(QuantityLitersTextBox.Text);
                var quantityTons = GetDecimalValue(QuantityTonsTextBox.Text);
                var totalValue = GetDecimalValue(TotalValueTextBox.Text);
                var exchangeRate = GetDecimalValue(ExchangeRateTextBox.Text);

                string currency = _selectedSupplier?.Currency ?? "USD";

                // Calculate Price per Liter
                if (quantityLiters > 0 && totalValue > 0)
                {
                    var pricePerLiter = totalValue / quantityLiters;
                    PricePerLiterText.Text = $"{pricePerLiter:N6} {currency}";
                }
                else
                {
                    PricePerLiterText.Text = $"0.000000 {currency}";
                }

                // Calculate Price per Ton
                if (quantityTons > 0 && totalValue > 0)
                {
                    var pricePerTon = totalValue / quantityTons;
                    PricePerTonText.Text = $"{pricePerTon:N3} {currency}";
                }
                else
                {
                    PricePerTonText.Text = $"0.000 {currency}";
                }

                // Calculate Density = Ton / (Liters / 1000)
                if (quantityLiters > 0 && quantityTons > 0)
                {
                    var density = quantityTons / (quantityLiters / 1000);
                    DensityText.Text = density.ToString("N3");
                }
                else
                {
                    DensityText.Text = "0.000";
                }

                // Display Exchange Rate
                ExchangeRateDisplayText.Text = exchangeRate.ToString("N3");

                // Calculate USD Value
                if (totalValue > 0 && exchangeRate > 0)
                {
                    var usdValue = totalValue * exchangeRate;
                    TotalUSDValueText.Text = usdValue.ToString("C2");
                }
                else
                {
                    TotalUSDValueText.Text = "$0.00";
                }
            }
            catch
            {
                // Reset to defaults if calculation fails
                string currency = _selectedSupplier?.Currency ?? "USD";
                PricePerLiterText.Text = $"0.000000 {currency}";
                PricePerTonText.Text = $"0.000 {currency}";
                DensityText.Text = "0.000";
                TotalUSDValueText.Text = "$0.00";
                ExchangeRateDisplayText.Text = "1.000";
            }
        }

        private decimal GetDecimalValue(string text)
        {
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                return value;
            return 0;
        }

        private void UpdatePaymentStatus()
        {
            var hasReceiptDate = InvoiceReceiptDatePicker.SelectedDate.HasValue;
            var hasDueDate = DueDatePicker.SelectedDate.HasValue;

            if (hasReceiptDate && hasDueDate)
            {
                var daysUntilDue = (DueDatePicker.SelectedDate!.Value - DateTime.Now).Days;
                if (daysUntilDue <= 0)
                {
                    PaymentStatusText.Text = "⚠️ Payment Overdue!";
                    PaymentStatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (daysUntilDue <= 3)
                {
                    PaymentStatusText.Text = $"⏰ Due in {daysUntilDue} days";
                    PaymentStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    PaymentStatusText.Text = $"📋 Due in {daysUntilDue} days";
                    PaymentStatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            else if (hasReceiptDate || hasDueDate)
            {
                PaymentStatusText.Text = "⚠️ Both dates required if tracking";
                PaymentStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                PaymentStatusText.Text = "📋 Optional Invoice Tracking";
                PaymentStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            }
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

            if (SupplierComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a supplier.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                SupplierComboBox.Focus();
                return false;
            }

            if (!PurchaseDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a purchase date.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                PurchaseDatePicker.Focus();
                return false;
            }

            if (GetDecimalValue(QuantityLitersTextBox.Text) <= 0)
            {
                MessageBox.Show("Please enter a valid quantity in liters.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityLitersTextBox.Focus();
                return false;
            }

            if (GetDecimalValue(QuantityTonsTextBox.Text) <= 0)
            {
                MessageBox.Show("Please enter a valid quantity in tons.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTonsTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(InvoiceRefTextBox.Text))
            {
                MessageBox.Show("Please enter an invoice reference.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                InvoiceRefTextBox.Focus();
                return false;
            }

            if (GetDecimalValue(TotalValueTextBox.Text) <= 0)
            {
                MessageBox.Show("Please enter a valid total value.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TotalValueTextBox.Focus();
                return false;
            }

            // Validate invoice dates
            var hasReceiptDate = InvoiceReceiptDatePicker.SelectedDate.HasValue;
            var hasDueDate = DueDatePicker.SelectedDate.HasValue;

            if (hasReceiptDate && !hasDueDate)
            {
                MessageBox.Show("Due date is required when invoice receipt date is provided.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                DueDatePicker.Focus();
                return false;
            }

            if (!hasReceiptDate && hasDueDate)
            {
                MessageBox.Show("Invoice receipt date is required when due date is provided.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                InvoiceReceiptDatePicker.Focus();
                return false;
            }

            return true;
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
                // Just continue with normal operation
            }
        }

        #endregion

        #region Edit/Delete Management

        private void EnableEditMode(Purchase purchase)
        {
            _editingPurchase = purchase;
            _isEditMode = true;

            // Populate form with purchase data
            VesselComboBox.SelectedValue = purchase.VesselId;
            SupplierComboBox.SelectedValue = purchase.SupplierId;
            PurchaseDatePicker.SelectedDate = purchase.PurchaseDate;
            QuantityLitersTextBox.Text = purchase.QuantityLiters.ToString("F3");
            QuantityTonsTextBox.Text = purchase.QuantityTons.ToString("F3");
            InvoiceRefTextBox.Text = purchase.InvoiceReference;
            TotalValueTextBox.Text = purchase.TotalValue.ToString("F2");
            InvoiceReceiptDatePicker.SelectedDate = purchase.InvoiceReceiptDate;
            DueDatePicker.SelectedDate = purchase.DueDate;

            // Update UI
            SavePurchaseBtn.Content = "💾 Update Purchase";
            EditPurchaseBtn.IsEnabled = false;
            DeletePurchaseBtn.IsEnabled = false;
        }

        private void ExitEditMode()
        {
            _editingPurchase = null;
            _isEditMode = false;
            SavePurchaseBtn.Content = "💾 Complete Purchase";
            EditPurchaseBtn.IsEnabled = false;
            DeletePurchaseBtn.IsEnabled = false;
        }

        #endregion

        #region Event Handlers

        private void SupplierComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedSupplier = SupplierComboBox.SelectedItem as Supplier;
            UpdateCurrencyDisplay();
            CalculateValues();
        }

        private void QuantityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateValues();
        }

        private void TotalValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateValues();
        }

        private void InvoiceDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePaymentStatus();
        }

        private void PurchasesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPurchase = PurchasesGrid.SelectedItem as Purchase;

            if (selectedPurchase != null)
            {
                EditPurchaseBtn.IsEnabled = true;
                DeletePurchaseBtn.IsEnabled = true;
            }
            else
            {
                EditPurchaseBtn.IsEnabled = false;
                DeletePurchaseBtn.IsEnabled = false;
            }
        }

        #endregion

        #region Button Click Events

        private void EditPurchase_Click(object sender, RoutedEventArgs e)
        {
            var selectedPurchase = PurchasesGrid.SelectedItem as Purchase;
            if (selectedPurchase != null)
            {
                EnableEditMode(selectedPurchase);
            }
        }

        private async void DeletePurchase_Click(object sender, RoutedEventArgs e)
        {
            var selectedPurchase = PurchasesGrid.SelectedItem as Purchase;
            if (selectedPurchase == null) return;

            try
            {
                using var context = new InventoryContext();

                // Check if purchase has allocations
                var hasAllocations = await context.Allocations
                    .AnyAsync(a => a.PurchaseId == selectedPurchase.Id);

                if (hasAllocations)
                {
                    var result = MessageBox.Show(
                        $"This purchase has FIFO allocations associated with it.\n\n" +
                        "Deleting this purchase will also remove all related allocations.\n" +
                        "This may affect consumption records and reports.\n\n" +
                        "Do you want to proceed?",
                        "Purchase Has Allocations",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes) return;

                    // Delete related allocations first
                    var allocations = await context.Allocations
                        .Where(a => a.PurchaseId == selectedPurchase.Id)
                        .ToListAsync();

                    context.Allocations.RemoveRange(allocations);
                }

                // Final confirmation
                var confirmResult = MessageBox.Show(
                    $"Are you sure you want to delete this purchase?\n\n" +
                    $"Invoice: {selectedPurchase.InvoiceReference}\n" +
                    $"Date: {selectedPurchase.PurchaseDate:dd/MM/yyyy}\n" +
                    $"Amount: {selectedPurchase.TotalValueUSD:C2}\n\n" +
                    "This action cannot be undone.",
                    "Confirm Delete Purchase",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes) return;

                // Delete the purchase
                var purchaseToDelete = await context.Purchases
                    .FindAsync(selectedPurchase.Id);

                if (purchaseToDelete != null)
                {
                    context.Purchases.Remove(purchaseToDelete);
                    await context.SaveChangesAsync();

                    await CreateAutoBackupAsync("PurchaseDelete");

                    MessageBox.Show("Purchase deleted successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadPurchasesAsync();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting purchase: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> ValidateEditQuantityAsync(int purchaseId, decimal newQuantityLiters)
        {
            try
            {
                using var context = new InventoryContext();

                // Get all allocations for this purchase and sum in memory to avoid SQLite decimal issues
                var allocations = await context.Allocations
                    .Where(a => a.PurchaseId == purchaseId)
                    .Select(a => a.AllocatedQuantity)
                    .ToListAsync();

                var totalAllocated = allocations.Sum();

                if (newQuantityLiters < totalAllocated)
                {
                    MessageBox.Show(
                        $"Cannot reduce purchase quantity to {newQuantityLiters:N3} L\n\n" +
                        $"This purchase has {totalAllocated:N3} L already allocated to consumption records.\n" +
                        $"Minimum allowed quantity: {totalAllocated:N3} L\n\n" +
                        $"To reduce this purchase:\n" +
                        $"1. Delete related consumption records, or\n" +
                        $"2. Re-run FIFO allocation after other changes",
                        "Cannot Reduce Purchase Quantity",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating purchase quantity: {ex.Message}", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async void SavePurchase_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                using var context = new InventoryContext();

                if (_isEditMode && _editingPurchase != null)
                {
                    // Validate quantity reduction against allocations
                    var newQuantity = GetDecimalValue(QuantityLitersTextBox.Text);
                    if (!await ValidateEditQuantityAsync(_editingPurchase.Id, newQuantity))
                    {
                        return; // Validation failed, don't save
                    }

                    // Update existing purchase
                    var purchaseToUpdate = await context.Purchases
                        .FindAsync(_editingPurchase.Id);

                    if (purchaseToUpdate != null)
                    {
                        var oldQuantity = purchaseToUpdate.QuantityLiters;
                        var quantityDifference = newQuantity - oldQuantity;

                        purchaseToUpdate.VesselId = (int)VesselComboBox.SelectedValue;
                        purchaseToUpdate.SupplierId = (int)SupplierComboBox.SelectedValue;
                        purchaseToUpdate.PurchaseDate = PurchaseDatePicker.SelectedDate!.Value;
                        purchaseToUpdate.InvoiceReference = InvoiceRefTextBox.Text.Trim();
                        purchaseToUpdate.QuantityLiters = newQuantity;
                        purchaseToUpdate.QuantityTons = GetDecimalValue(QuantityTonsTextBox.Text);
                        purchaseToUpdate.TotalValue = GetDecimalValue(TotalValueTextBox.Text);
                        purchaseToUpdate.TotalValueUSD = GetDecimalValue(TotalValueTextBox.Text) * GetDecimalValue(ExchangeRateTextBox.Text);
                        purchaseToUpdate.InvoiceReceiptDate = InvoiceReceiptDatePicker.SelectedDate;
                        purchaseToUpdate.DueDate = DueDatePicker.SelectedDate;

                        // Update remaining quantity proportionally
                        purchaseToUpdate.RemainingQuantity += quantityDifference;

                        await context.SaveChangesAsync();

                        await CreateAutoBackupAsync(_isEditMode ? "PurchaseEdit" : "PurchaseAdd");

                        MessageBox.Show(_isEditMode ? "Purchase updated successfully!" : "Purchase saved successfully!", "Success",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Create new purchase
                    var purchase = new Purchase
                    {
                        VesselId = (int)VesselComboBox.SelectedValue,
                        SupplierId = (int)SupplierComboBox.SelectedValue,
                        PurchaseDate = PurchaseDatePicker.SelectedDate!.Value,
                        InvoiceReference = InvoiceRefTextBox.Text.Trim(),
                        QuantityLiters = GetDecimalValue(QuantityLitersTextBox.Text),
                        QuantityTons = GetDecimalValue(QuantityTonsTextBox.Text),
                        TotalValue = GetDecimalValue(TotalValueTextBox.Text),
                        TotalValueUSD = GetDecimalValue(TotalValueTextBox.Text) * GetDecimalValue(ExchangeRateTextBox.Text),
                        InvoiceReceiptDate = InvoiceReceiptDatePicker.SelectedDate,
                        DueDate = DueDatePicker.SelectedDate,
                        RemainingQuantity = GetDecimalValue(QuantityLitersTextBox.Text), // Initially all quantity remains
                        CreatedDate = DateTime.Now
                    };

                    context.Purchases.Add(purchase);
                    await context.SaveChangesAsync();

                    MessageBox.Show("Purchase saved successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await LoadPurchasesAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving purchase: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void RefreshPurchases_Click(object sender, RoutedEventArgs e)
        {
            await LoadPurchasesAsync();
        }

        #endregion
    }
}