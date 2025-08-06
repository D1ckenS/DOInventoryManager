using DOInventoryManager.Data;
using DOInventoryManager.Models;
using DOInventoryManager.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace DOInventoryManager.Views
{
    public partial class VesselsView : UserControl
    {
        private Vessel? _editingVessel = null;

        public VesselsView()
        {
            InitializeComponent();
            _ = LoadVesselsAsync();
            ClearForm();
        }

        #region Data Loading

        private async Task LoadVesselsAsync()
        {
            try
            {
                using var context = new InventoryContext();
                var vessels = await context.Vessels
                    .OrderBy(v => v.Type)
                    .ThenBy(v => v.Name)
                    .ToListAsync();

                VesselsGrid.ItemsSource = vessels;
                UpdateStatistics(vessels);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading vessels: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics(List<Vessel> vessels)
        {
            var totalFleet = vessels.Count;
            var vesselsCount = vessels.Count(v => v.Type == "Vessel");
            var boatsCount = vessels.Count(v => v.Type == "Boat");

            TotalFleetText.Text = totalFleet.ToString();
            VesselsCountText.Text = vesselsCount.ToString();
            BoatsCountText.Text = boatsCount.ToString();
        }

        #endregion

        #region Form Management

        private void ClearForm()
        {
            _editingVessel = null;
            FormTitle.Text = "Add New Vessel";
            VesselNameTextBox.Text = "";
            VesselTypeComboBox.SelectedIndex = -1;
            RouteDisplayText.Text = "Select vessel type to see route";
            RouteDisplayText.Foreground = System.Windows.Media.Brushes.Gray;

            SaveBtn.Content = "💾 Save";
            EditVesselBtn.IsEnabled = false;
            DeleteVesselBtn.IsEnabled = false;
        }

        private void PopulateForm(Vessel vessel)
        {
            _editingVessel = vessel;
            FormTitle.Text = "Edit Vessel";
            VesselNameTextBox.Text = vessel.Name;

            // Set vessel type combobox
            foreach (ComboBoxItem item in VesselTypeComboBox.Items)
            {
                if (item.Tag?.ToString() == vessel.Type)
                {
                    VesselTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            UpdateRouteDisplay(vessel.Type);
            SaveBtn.Content = "💾 Update";
        }

        private void UpdateRouteDisplay(string vesselType)
        {
            if (vesselType == "Vessel")
            {
                RouteDisplayText.Text = "Aqaba → Nuweibaa → Aqaba";
                RouteDisplayText.Foreground = System.Windows.Media.Brushes.DarkBlue;
            }
            else if (vesselType == "Boat")
            {
                RouteDisplayText.Text = "Aqaba → Taba → Aqaba";
                RouteDisplayText.Foreground = System.Windows.Media.Brushes.DarkGreen;
            }
            else
            {
                RouteDisplayText.Text = "Select vessel type to see route";
                RouteDisplayText.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(VesselNameTextBox.Text))
            {
                MessageBox.Show("Vessel name is required.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                VesselNameTextBox.Focus();
                return false;
            }

            if (VesselTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a vessel type.", "Validation Error",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                VesselTypeComboBox.Focus();
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
            }
        }

        #endregion

        #region Event Handlers

        private void VesselsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedVessel = VesselsGrid.SelectedItem as Vessel;

            if (selectedVessel != null)
            {
                EditVesselBtn.IsEnabled = true;
                DeleteVesselBtn.IsEnabled = true;
                PopulateForm(selectedVessel);
            }
            else
            {
                EditVesselBtn.IsEnabled = false;
                DeleteVesselBtn.IsEnabled = false;
            }
        }

        private void VesselTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VesselTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string vesselType = selectedItem.Tag?.ToString() ?? "";
                UpdateRouteDisplay(vesselType);
            }
        }

        #endregion

        #region Button Click Events

        private void AddVessel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            VesselNameTextBox.Focus();
        }

        private void EditVessel_Click(object sender, RoutedEventArgs e)
        {
            var selectedVessel = VesselsGrid.SelectedItem as Vessel;
            if (selectedVessel != null)
            {
                PopulateForm(selectedVessel);
                VesselNameTextBox.Focus();
            }
        }

        private async void DeleteVessel_Click(object sender, RoutedEventArgs e)
        {
            var selectedVessel = VesselsGrid.SelectedItem as Vessel;
            if (selectedVessel == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete vessel '{selectedVessel.Name}'?\n\n" +
                "This action cannot be undone. Note: You cannot delete vessels that have purchase or consumption records.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var context = new InventoryContext();

                // Check if vessel has purchases or consumptions
                var hasPurchases = await context.Purchases
                    .AnyAsync(p => p.VesselId == selectedVessel.Id);

                var hasConsumptions = await context.Consumptions
                    .AnyAsync(c => c.VesselId == selectedVessel.Id);

                if (hasPurchases || hasConsumptions)
                {
                    MessageBox.Show(
                        "Cannot delete this vessel because it has purchase or consumption records.\n\n" +
                        "You can only delete vessels that have no associated transactions.",
                        "Cannot Delete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Delete vessel
                var vesselToDelete = await context.Vessels
                    .FindAsync(selectedVessel.Id);

                if (vesselToDelete != null)
                {
                    context.Vessels.Remove(vesselToDelete);
                    await context.SaveChangesAsync();

                    // Add auto-backup after successful delete
                    await CreateAutoBackupAsync("VesselDelete");

                    MessageBox.Show("Vessel deleted successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadVesselsAsync();
                    ClearForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting vessel: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            try
            {
                using var context = new InventoryContext();

                var vesselType = ((ComboBoxItem)VesselTypeComboBox.SelectedItem).Tag?.ToString() ?? "";
                var vesselName = VesselNameTextBox.Text.Trim();

                // Check for duplicate names (excluding current vessel if editing)
                var duplicateQuery = context.Vessels.Where(v => v.Name.ToLower() == vesselName.ToLower());
                if (_editingVessel != null)
                {
                    duplicateQuery = duplicateQuery.Where(v => v.Id != _editingVessel.Id);
                }

                if (await duplicateQuery.AnyAsync())
                {
                    MessageBox.Show("A vessel with this name already exists.", "Duplicate Name",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    VesselNameTextBox.Focus();
                    return;
                }

                if (_editingVessel == null)
                {
                    // Add new vessel
                    var newVessel = new Vessel
                    {
                        Name = vesselName,
                        Type = vesselType,
                        CreatedDate = DateTime.Now
                    };

                    context.Vessels.Add(newVessel);
                    await context.SaveChangesAsync();

                    // Add auto-backup after successful save
                    await CreateAutoBackupAsync("VesselAdd");

                    MessageBox.Show("Vessel added successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Update existing vessel
                    var vesselToUpdate = await context.Vessels
                        .FindAsync(_editingVessel.Id);

                    if (vesselToUpdate != null)
                    {
                        vesselToUpdate.Name = vesselName;
                        vesselToUpdate.Type = vesselType;

                        await context.SaveChangesAsync();

                        // Add auto-backup after successful update
                        await CreateAutoBackupAsync("VesselEdit");

                        MessageBox.Show("Vessel updated successfully!", "Success",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                await LoadVesselsAsync();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving vessel: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadVesselsAsync();
            ClearForm();
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