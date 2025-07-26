using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DOInventoryManager.Views
{
    public partial class SuppliersView : UserControl
    {
        private Supplier? _editingSupplier = null;
        private const decimal JOD_USD_RATE = 1.4104372m;

        public SuppliersView()
        {
            InitializeComponent();
            _ = LoadSuppliersAsync();
            ClearForm();
        }

        #region Data Loading

        private async Task LoadSuppliersAsync()
        {
            try
            {
                using var context = new InventoryContext();
                var suppliers = await context.Suppliers
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                SuppliersGrid.ItemsSource = suppliers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Form Management

        private void ClearForm()
        {
            _editingSupplier = null;
            FormTitle.Text = "Add New Supplier";
            SupplierNameTextBox.Text = "";
            CurrencyComboBox.SelectedIndex = -1;
            ExchangeRateTextBox.Text = "";
            ExchangeRateHint.Text = "Select a currency first";
            
            SaveBtn.Content = "💾 Save";
            EditSupplierBtn.IsEnabled = false;
            DeleteSupplierBtn.IsEnabled = false;
        }

        private void PopulateForm(Supplier supplier)
        {
            _editingSupplier = supplier;
            FormTitle.Text = "Edit Supplier";
            SupplierNameTextBox.Text = supplier.Name;
            
            // Set currency combobox
            foreach (ComboBoxItem item in CurrencyComboBox.Items)
            {
                if (item.Tag?.ToString() == supplier.Currency)
                {
                    CurrencyComboBox.SelectedItem = item;
                    break;
                }
            }
            
            ExchangeRateTextBox.Text = supplier.ExchangeRate.ToString("F6", CultureInfo.InvariantCulture);
            SaveBtn.Content = "💾 Update";
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(SupplierNameTextBox.Text))
            {
                MessageBox.Show("Supplier name is required.", "Validation Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                SupplierNameTextBox.Focus();
                return false;
            }

            if (CurrencyComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a currency.", "Validation Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                CurrencyComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(ExchangeRateTextBox.Text))
            {
                MessageBox.Show("Exchange rate is required.", "Validation Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                ExchangeRateTextBox.Focus();
                return false;
            }

            if (!decimal.TryParse(ExchangeRateTextBox.Text, NumberStyles.Number, 
                                CultureInfo.InvariantCulture, out decimal rate) || rate <= 0)
            {
                MessageBox.Show("Please enter a valid exchange rate (greater than 0).", 
                              "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ExchangeRateTextBox.Focus();
                return false;
            }

            return true;
        }

        #endregion

        #region Event Handlers

        private void SuppliersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSupplier = SuppliersGrid.SelectedItem as Supplier;
            
            if (selectedSupplier != null)
            {
                EditSupplierBtn.IsEnabled = true;
                DeleteSupplierBtn.IsEnabled = true;
                PopulateForm(selectedSupplier);
            }
            else
            {
                EditSupplierBtn.IsEnabled = false;
                DeleteSupplierBtn.IsEnabled = false;
            }
        }

        private void CurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrencyComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string currency = selectedItem.Tag?.ToString() ?? "";
                
                switch (currency)
                {
                    case "USD":
                        ExchangeRateTextBox.Text = "1.000000";
                        ExchangeRateTextBox.IsEnabled = false;
                        ExchangeRateHint.Text = "USD is the base currency (always 1.0)";
                        break;
                        
                    case "JOD":
                        ExchangeRateTextBox.Text = JOD_USD_RATE.ToString("F6", CultureInfo.InvariantCulture);
                        ExchangeRateTextBox.IsEnabled = false;
                        ExchangeRateHint.Text = "JOD rate is fixed at 1.4104372 USD";
                        break;
                        
                    case "EGP":
                        ExchangeRateTextBox.Text = "";
                        ExchangeRateTextBox.IsEnabled = true;
                        ExchangeRateHint.Text = "Enter current EGP to USD exchange rate";
                        ExchangeRateTextBox.Focus();
                        break;
                        
                    default:
                        ExchangeRateTextBox.IsEnabled = true;
                        ExchangeRateHint.Text = "Enter exchange rate to USD";
                        break;
                }
            }
        }

        #endregion

        #region Button Click Events

        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            SupplierNameTextBox.Focus();
        }

        private void EditSupplier_Click(object sender, RoutedEventArgs e)
        {
            var selectedSupplier = SuppliersGrid.SelectedItem as Supplier;
            if (selectedSupplier != null)
            {
                PopulateForm(selectedSupplier);
                SupplierNameTextBox.Focus();
            }
        }

        private async void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            var selectedSupplier = SuppliersGrid.SelectedItem as Supplier;
            if (selectedSupplier == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete supplier '{selectedSupplier.Name}'?\n\n" +
                "This action cannot be undone. Note: You cannot delete suppliers that have purchase records.",
                "Confirm Delete", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var context = new InventoryContext();
                
                // Check if supplier has purchases
                var hasPurchases = await context.Purchases
                    .AnyAsync(p => p.SupplierId == selectedSupplier.Id);
                
                if (hasPurchases)
                {
                    MessageBox.Show(
                        "Cannot delete this supplier because it has purchase records.\n\n" +
                        "You can only delete suppliers that have no associated transactions.",
                        "Cannot Delete", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }

                // Delete supplier
                var supplierToDelete = await context.Suppliers
                    .FindAsync(selectedSupplier.Id);
                
                if (supplierToDelete != null)
                {
                    context.Suppliers.Remove(supplierToDelete);
                    await context.SaveChangesAsync();
                    
                    MessageBox.Show("Supplier deleted successfully!", "Success", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    await LoadSuppliersAsync();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting supplier: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                using var context = new InventoryContext();
                
                var currency = ((ComboBoxItem)CurrencyComboBox.SelectedItem).Tag?.ToString() ?? "";
                var exchangeRate = decimal.Parse(ExchangeRateTextBox.Text, CultureInfo.InvariantCulture);
                var supplierName = SupplierNameTextBox.Text.Trim();

                // Check for duplicate names (excluding current supplier if editing)
                var duplicateQuery = context.Suppliers.Where(s => s.Name.ToLower() == supplierName.ToLower());
                if (_editingSupplier != null)
                {
                    duplicateQuery = duplicateQuery.Where(s => s.Id != _editingSupplier.Id);
                }
                
                if (await duplicateQuery.AnyAsync())
                {
                    MessageBox.Show("A supplier with this name already exists.", "Duplicate Name", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    SupplierNameTextBox.Focus();
                    return;
                }

                if (_editingSupplier == null)
                {
                    // Add new supplier
                    var newSupplier = new Supplier
                    {
                        Name = supplierName,
                        Currency = currency,
                        ExchangeRate = exchangeRate,
                        CreatedDate = DateTime.Now
                    };

                    context.Suppliers.Add(newSupplier);
                    await context.SaveChangesAsync();
                    
                    MessageBox.Show("Supplier added successfully!", "Success", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Update existing supplier
                    var supplierToUpdate = await context.Suppliers
                        .FindAsync(_editingSupplier.Id);
                    
                    if (supplierToUpdate != null)
                    {
                        supplierToUpdate.Name = supplierName;
                        supplierToUpdate.Currency = currency;
                        supplierToUpdate.ExchangeRate = exchangeRate;
                        
                        await context.SaveChangesAsync();
                        
                        MessageBox.Show("Supplier updated successfully!", "Success", 
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                await LoadSuppliersAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving supplier: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadSuppliersAsync();
            ClearForm();
        }

        #endregion
    }
}