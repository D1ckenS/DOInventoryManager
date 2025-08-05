using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DOInventoryManager.Services;
using DOInventoryManager.Models;
using DOInventoryManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OfficeOpenXml;

namespace DOInventoryManager.Views
{
    public partial class BackupManagementView : UserControl
    {
        private readonly BackupService _backupService;
        private List<BackupInfo> _backupHistory = new();

        public BackupManagementView()
        {
            InitializeComponent();
            _backupService = new BackupService();
            LoadBackupData();
        }

        #region Data Loading

        private void LoadBackupData()
        {
            try
            {
                // Load backup history
                _backupHistory = _backupService.GetBackupHistory();
                BackupHistoryGrid.ItemsSource = _backupHistory;

                // Update status cards
                UpdateStatusCards();

                // Handle empty state
                if (_backupHistory.Count == 0)
                {
                    BackupHistoryGrid.Visibility = Visibility.Collapsed;
                    NoBackupsMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    BackupHistoryGrid.Visibility = Visibility.Visible;
                    NoBackupsMessage.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading backup data: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusCards()
        {
            try
            {
                // Update last backup info
                if (_backupHistory.Any())
                {
                    var lastBackup = _backupHistory.First(); // Already ordered by date desc
                    LastBackupText.Text = lastBackup.CreatedDate.ToString("dd/MM/yyyy HH:mm");
                    LastBackupOperationText.Text = $"Operation: {lastBackup.Operation}";
                }
                else
                {
                    LastBackupText.Text = "Never";
                    LastBackupOperationText.Text = "No backups created yet";
                }

                // Update total backups
                TotalBackupsText.Text = _backupHistory.Count.ToString();
                var totalSize = _backupHistory.Sum(b => b.SizeMB);
                TotalBackupSizeText.Text = $"{totalSize:F1} MB total";

                // Update database size
                var databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DOInventory.db");
                if (File.Exists(databasePath))
                {
                    var dbInfo = new FileInfo(databasePath);
                    DatabaseSizeText.Text = $"{dbInfo.Length / 1024.0 / 1024.0:F1} MB";
                }
                else
                {
                    DatabaseSizeText.Text = "Not found";
                }

                // Update backup folder path
                BackupFolderText.Text = Path.GetFileName(_backupService.GetBackupFolder());
            }
            catch (Exception ex)
            {
                // Set default values on error
                LastBackupText.Text = "Error";
                TotalBackupsText.Text = "0";
                DatabaseSizeText.Text = "Unknown";
                System.Diagnostics.Debug.WriteLine($"Status update error: {ex.Message}");
            }
        }

        #endregion

        #region Button Click Events

        private async void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateBackupBtn.IsEnabled = false;
                CreateBackupBtn.Content = "🔄 Creating...";

                var result = await _backupService.CreateBackupAsync("Manual");

                if (result.Success)
                {
                    MessageBox.Show($"✅ {result.Message}\n\n" +
                                  $"📁 Location: {Path.GetFileName(result.BackupPath)}\n" +
                                  $"📏 Size: {result.BackupSizeBytes / 1024.0 / 1024.0:F2} MB\n" +
                                  $"📊 Total Backups: {result.TotalBackups}",
                                  "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh the backup list
                    LoadBackupData();
                }
                else
                {
                    MessageBox.Show($"❌ {result.Message}", "Backup Failed",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CreateBackupBtn.IsEnabled = true;
                CreateBackupBtn.Content = "🗄️ Create Backup";
            }
        }

        private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if (_backupHistory.Count == 0)
            {
                MessageBox.Show("No backups available to restore from.", "No Backups",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show backup selection dialog
            var dialog = new BackupSelectionDialog(_backupHistory);
            if (dialog.ShowDialog() == true && dialog.SelectedBackup != null)
            {
                await RestoreFromBackupAsync(dialog.SelectedBackup);
            }
        }

        private async void RestoreSpecificBackup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string backupPath)
            {
                var backup = _backupHistory.FirstOrDefault(b => b.FilePath == backupPath);
                if (backup != null)
                {
                    await RestoreFromBackupAsync(backup);
                }
            }
        }

        private async Task RestoreFromBackupAsync(BackupInfo backup)
        {
            var confirmMessage = $"⚠️ RESTORE CONFIRMATION\n\n" +
                               $"This will replace your current database with the backup:\n\n" +
                               $"📄 File: {backup.FileName}\n" +
                               $"📅 Created: {backup.FormattedDate}\n" +
                               $"⚙️ Operation: {backup.Operation}\n" +
                               $"📏 Size: {backup.FormattedSize}\n\n" +
                               $"⚠️ WARNING: Your current data will be backed up first, but this action cannot be easily undone.\n\n" +
                               $"Do you want to proceed with the restore?";

            var result = MessageBox.Show(confirmMessage, "Confirm Restore",
                                       MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                RestoreBackupBtn.IsEnabled = false;
                RestoreBackupBtn.Content = "🔄 Restoring...";

                var success = await _backupService.RestoreFromBackupAsync(backup.FilePath);

                if (success)
                {
                    MessageBox.Show($"✅ Database restored successfully!\n\n" +
                                  $"📄 Restored from: {backup.FileName}\n" +
                                  $"📅 Backup Date: {backup.FormattedDate}\n\n" +
                                  $"ℹ️ The application should be restarted to ensure all data is properly loaded.",
                                  "Restore Completed", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh the backup list (there should be a new PreRestore backup)
                    LoadBackupData();
                }
                else
                {
                    MessageBox.Show("❌ Failed to restore from backup. Please check that the backup file exists and is not corrupted.",
                                  "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RestoreBackupBtn.IsEnabled = true;
                RestoreBackupBtn.Content = "🔄 Restore Backup";
            }
        }

        private async void DeleteBackup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string backupPath)
            {
                var backup = _backupHistory.FirstOrDefault(b => b.FilePath == backupPath);
                if (backup == null) return;

                var confirmMessage = $"🗑️ DELETE BACKUP CONFIRMATION\n\n" +
                                   $"Are you sure you want to delete this backup?\n\n" +
                                   $"📄 File: {backup.FileName}\n" +
                                   $"📅 Created: {backup.FormattedDate}\n" +
                                   $"⚙️ Operation: {backup.Operation}\n" +
                                   $"📏 Size: {backup.FormattedSize}\n\n" +
                                   $"⚠️ This action cannot be undone.";

                var result = MessageBox.Show(confirmMessage, "Confirm Delete",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                try
                {
                    File.Delete(backupPath);
                    MessageBox.Show($"✅ Backup deleted successfully!\n\n📄 {backup.FileName}",
                                  "Backup Deleted", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh the backup list
                    LoadBackupData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete backup: {ex.Message}", "Delete Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Export Data Package",
                    Filter = "ZIP files (*.zip)|*.zip",
                    FileName = $"DOInventory_DataPackage_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportDataBtn.IsEnabled = false;
                    ExportDataBtn.Content = "📦 Exporting...";

                    var result = await CreateDataPackageAsync(saveDialog.FileName);

                    if (result.Success)
                    {
                        var openResult = MessageBox.Show($"✅ Data Package exported successfully!\n\n" +
                                      $"📁 Location: {result.FilePath}\n" +
                                      $"📏 Size: {result.SizeMB:F2} MB\n" +
                                      $"📄 Files included: {result.FilesIncluded}\n\n" +
                                      $"This package contains:\n" +
                                      $"• Current active database\n" +
                                      $"• {result.BackupCount} recent backup files\n" +
                                      $"• Import instructions (README.txt)\n\n" +
                                      $"Would you like to open the folder containing the package?",
                                      "Export Completed", MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (openResult == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{result.FilePath}\"");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"❌ Export failed: {result.ErrorMessage}", "Export Error",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExportDataBtn.IsEnabled = true;
                ExportDataBtn.Content = "📦 Export Data Package";
            }
        }

        private async void UploadData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show upload guide dialog first
                var guideResult = ShowUploadGuideDialog();
                if (guideResult != MessageBoxResult.OK) return;

                // File selection dialog
                var openDialog = new OpenFileDialog
                {
                    Title = "Select Data File to Upload",
                    Filter = "Excel files (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All supported files|*.xlsx;*.csv",
                    FilterIndex = 3
                };

                if (openDialog.ShowDialog() == true)
                {
                    UploadDataBtn.IsEnabled = false;
                    UploadDataBtn.Content = "📄 Uploading...";

                    var result = await ProcessDataUploadAsync(openDialog.FileName);

                    if (result.Success)
                    {
                        MessageBox.Show($"✅ Data upload completed successfully!\n\n" +
                                      $"📊 Results:\n" +
                                      $"• Purchases imported: {result.PurchasesImported}\n" +
                                      $"• Consumption records imported: {result.ConsumptionImported}\n" +
                                      $"• Records skipped (duplicates): {result.RecordsSkipped}\n" +
                                      $"• Errors encountered: {result.ErrorCount}\n\n" +
                                      $"💡 Recommendation: Run FIFO allocation to link purchases to consumption.",
                                      "Upload Completed", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Refresh data if any records were imported
                        if (result.PurchasesImported > 0 || result.ConsumptionImported > 0)
                        {
                            LoadBackupData();
                        }
                    }
                    else
                    {
                        MessageBox.Show($"❌ Upload failed: {result.ErrorMessage}\n\n" +
                                      "Please check your file format and try again.",
                                      "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Upload error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UploadDataBtn.IsEnabled = true;
                UploadDataBtn.Content = "📄 Upload Data";
            }
        }

        private async void Cleanup_Click(object sender, RoutedEventArgs e)
        {
            if (_backupHistory.Count <= 5)
            {
                MessageBox.Show($"You currently have {_backupHistory.Count} backups.\n\n" +
                              "Cleanup is only recommended when you have more than 5 backups to save disk space.",
                              "No Cleanup Needed", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmMessage = $"🧹 CLEANUP OLD BACKUPS\n\n" +
                               $"This will keep the 5 most recent backups and delete {_backupHistory.Count - 5} older ones.\n\n" +
                               $"📊 Current backups: {_backupHistory.Count}\n" +
                               $"🗑️ Will delete: {_backupHistory.Count - 5} backups\n" +
                               $"💾 Will keep: 5 most recent backups\n\n" +
                               $"Do you want to proceed?";

            var result = MessageBox.Show(confirmMessage, "Confirm Cleanup",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                CleanupBtn.IsEnabled = false;
                CleanupBtn.Content = "🧹 Cleaning...";

                var backupsToDelete = _backupHistory.Skip(5).ToList();
                int deletedCount = 0;

                foreach (var backup in backupsToDelete)
                {
                    try
                    {
                        File.Delete(backup.FilePath);
                        deletedCount++;
                    }
                    catch
                    {
                        // Continue with other files if one fails
                    }
                }

                MessageBox.Show($"✅ Cleanup completed!\n\n" +
                              $"🗑️ Deleted: {deletedCount} old backups\n" +
                              $"💾 Kept: {Math.Min(5, _backupHistory.Count)} recent backups",
                              "Cleanup Completed", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the backup list
                LoadBackupData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cleanup error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CleanupBtn.IsEnabled = true;
                CleanupBtn.Content = "🧹 Cleanup Old Backups";
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var success = _backupService.OpenBackupFolder();
                if (!success)
                {
                    MessageBox.Show("Could not open backup folder. The folder may not exist yet.",
                                  "Folder Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadBackupData();
        }

        #endregion

        #region Data Upload

        public class DataUploadResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public int PurchasesImported { get; set; }
            public int ConsumptionImported { get; set; }
            public int RecordsSkipped { get; set; }
            public int ErrorCount { get; set; }
        }

        private MessageBoxResult ShowUploadGuideDialog()
        {
            var guide = @"📄 BULK DATA UPLOAD GUIDE

Upload Excel (.xlsx) or CSV files with Purchase or Consumption records.

📋 REQUIRED COLUMNS:

⛽ PURCHASES:
VesselName, SupplierName, PurchaseDate, QuantityLiters, QuantityTons, TotalValue, InvoiceReference

🔥 CONSUMPTION:
VesselName, ConsumptionDate, ConsumptionLiters, LegsCompleted

📝 EXAMPLE (Purchases):
VesselName,SupplierName,PurchaseDate,QuantityLiters,QuantityTons,TotalValue,InvoiceReference
Vessel Alpha,Marine Fuel Co,15/01/2025,5000.000,4.250,4250.00,INV-001

⚠️ IMPORTANT:
• Date format: dd/MM/yyyy (15/01/2025)
• Vessel/Supplier names must match existing records exactly
• Duplicate invoices will be skipped
• Auto backup created before import

💡 TIP: Import Purchases first, then Consumption records.

Ready to select your file?";

            return MessageBox.Show(guide, "📄 Upload Data - Quick Guide", 
                                 MessageBoxButton.OKCancel, MessageBoxImage.Information);
        }

        private async Task<DataUploadResult> ProcessDataUploadAsync(string filePath)
        {
            var result = new DataUploadResult();

            try
            {
                // Create backup before import
                await _backupService.CreateBackupAsync("PreImport");

                // Determine file type and process accordingly
                var extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".xlsx")
                {
                    return await ProcessExcelFileAsync(filePath);
                }
                else if (extension == ".csv")
                {
                    return await ProcessCsvFileAsync(filePath);
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "Unsupported file format. Please use .xlsx or .csv files.";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<DataUploadResult> ProcessExcelFileAsync(string filePath)
        {
            var result = new DataUploadResult();

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage(new FileInfo(filePath));
                using var context = new InventoryContext();

                // Load lookup data
                var vessels = await context.Vessels.ToDictionaryAsync(v => v.Name, v => v);
                var suppliers = await context.Suppliers.ToDictionaryAsync(s => s.Name, s => s);

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    var sheetName = worksheet.Name.ToLower();
                    
                    if (sheetName.Contains("purchase"))
                    {
                        var purchaseResult = await ProcessPurchaseWorksheet(worksheet, context, vessels, suppliers);
                        result.PurchasesImported += purchaseResult.PurchasesImported;
                        result.RecordsSkipped += purchaseResult.RecordsSkipped;
                        result.ErrorCount += purchaseResult.ErrorCount;
                    }
                    else if (sheetName.Contains("consumption"))
                    {
                        var consumptionResult = await ProcessConsumptionWorksheet(worksheet, context, vessels);
                        result.ConsumptionImported += consumptionResult.ConsumptionImported;
                        result.RecordsSkipped += consumptionResult.RecordsSkipped;
                        result.ErrorCount += consumptionResult.ErrorCount;
                    }
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<DataUploadResult> ProcessCsvFileAsync(string filePath)
        {
            var result = new DataUploadResult();

            try
            {
                using var context = new InventoryContext();
                var vessels = await context.Vessels.ToDictionaryAsync(v => v.Name, v => v);
                var suppliers = await context.Suppliers.ToDictionaryAsync(s => s.Name, s => s);

                var lines = await File.ReadAllLinesAsync(filePath);
                if (lines.Length < 2) 
                {
                    result.Success = false;
                    result.ErrorMessage = "File must contain at least a header row and one data row.";
                    return result;
                }

                var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
                
                // Determine file type based on headers
                if (headers.Contains("VesselName") && headers.Contains("SupplierName") && headers.Contains("QuantityLiters"))
                {
                    // Purchase file
                    result = await ProcessPurchaseCsv(lines, context, vessels, suppliers);
                }
                else if (headers.Contains("VesselName") && headers.Contains("ConsumptionLiters"))
                {
                    // Consumption file
                    result = await ProcessConsumptionCsv(lines, context, vessels);
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = "Unable to determine file type. Please check column headers.";
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        // Helper method to parse DD-MM-YYYY dates
        private DateTime? ParseDate(string dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;
            
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
                return date;
                
            if (DateTime.TryParse(dateStr, out date))
                return date;
                
            return null;
        }

        private async Task<DataUploadResult> ProcessPurchaseWorksheet(ExcelWorksheet worksheet, InventoryContext context, Dictionary<string, Vessel> vessels, Dictionary<string, Supplier> suppliers)
        {
            var result = new DataUploadResult();
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            
            if (rowCount < 2) return result; // No data rows

            try
            {
                // Get header row to map columns
                var headers = new Dictionary<string, int>();
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var header = worksheet.Cells[1, col].Text.Trim();
                    if (!string.IsNullOrEmpty(header))
                        headers[header] = col;
                }

                // Validate required columns
                string[] requiredColumns = { "VesselName", "SupplierName", "PurchaseDate", "QuantityLiters", "QuantityTons", "TotalValue", "InvoiceReference" };
                foreach (var required in requiredColumns)
                {
                    if (!headers.ContainsKey(required))
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Missing required column: {required}";
                        return result;
                    }
                }

                // Process data rows
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var vesselName = worksheet.Cells[row, headers["VesselName"]].Text.Trim();
                        var supplierName = worksheet.Cells[row, headers["SupplierName"]].Text.Trim();
                        var invoiceRef = worksheet.Cells[row, headers["InvoiceReference"]].Text.Trim();

                        // Skip empty rows
                        if (string.IsNullOrEmpty(vesselName) || string.IsNullOrEmpty(supplierName)) continue;

                        // Check for existing invoice reference
                        if (await context.Purchases.AnyAsync(p => p.InvoiceReference == invoiceRef))
                        {
                            result.RecordsSkipped++;
                            continue;
                        }

                        // Validate vessel and supplier exist
                        if (!vessels.ContainsKey(vesselName) || !suppliers.ContainsKey(supplierName))
                        {
                            result.ErrorCount++;
                            continue;
                        }

                        // Parse required fields
                        var purchaseDate = ParseDate(worksheet.Cells[row, headers["PurchaseDate"]].Text);
                        if (!purchaseDate.HasValue ||
                            !decimal.TryParse(worksheet.Cells[row, headers["QuantityLiters"]].Text, out var quantityLiters) ||
                            !decimal.TryParse(worksheet.Cells[row, headers["QuantityTons"]].Text, out var quantityTons) ||
                            !decimal.TryParse(worksheet.Cells[row, headers["TotalValue"]].Text, out var totalValue))
                        {
                            result.ErrorCount++;
                            continue;
                        }

                        // Parse optional fields
                        var invoiceReceiptDate = headers.ContainsKey("InvoiceReceiptDate") ? ParseDate(worksheet.Cells[row, headers["InvoiceReceiptDate"]].Text) : null;
                        var dueDate = headers.ContainsKey("DueDate") ? ParseDate(worksheet.Cells[row, headers["DueDate"]].Text) : null;
                        var createdDate = headers.ContainsKey("CreatedDate") ? ParseDate(worksheet.Cells[row, headers["CreatedDate"]].Text) ?? DateTime.Now : DateTime.Now;

                        // Calculate USD value
                        var supplier = suppliers[supplierName];
                        var totalValueUSD = supplier.Currency == "USD" ? totalValue : totalValue * supplier.ExchangeRate;

                        // Create purchase record
                        var purchase = new Purchase
                        {
                            VesselId = vessels[vesselName].Id,
                            SupplierId = suppliers[supplierName].Id,
                            PurchaseDate = purchaseDate.Value,
                            InvoiceReference = invoiceRef,
                            QuantityLiters = quantityLiters,
                            QuantityTons = quantityTons,
                            TotalValue = totalValue,
                            TotalValueUSD = totalValueUSD,
                            InvoiceReceiptDate = invoiceReceiptDate,
                            DueDate = dueDate,
                            RemainingQuantity = quantityLiters, // FIFO tracking
                            CreatedDate = createdDate
                        };

                        context.Purchases.Add(purchase);
                        result.PurchasesImported++;
                    }
                    catch
                    {
                        result.ErrorCount++;
                    }
                }

                await context.SaveChangesAsync();
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<DataUploadResult> ProcessConsumptionWorksheet(ExcelWorksheet worksheet, InventoryContext context, Dictionary<string, Vessel> vessels)
        {
            var result = new DataUploadResult();
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            
            if (rowCount < 2) return result; // No data rows

            try
            {
                // Get header row to map columns
                var headers = new Dictionary<string, int>();
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    var header = worksheet.Cells[1, col].Text.Trim();
                    if (!string.IsNullOrEmpty(header))
                        headers[header] = col;
                }

                // Validate required columns
                string[] requiredColumns = { "VesselName", "ConsumptionDate", "ConsumptionLiters", "LegsCompleted" };
                foreach (var required in requiredColumns)
                {
                    if (!headers.ContainsKey(required))
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Missing required column: {required}";
                        return result;
                    }
                }

                // Process data rows
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var vesselName = worksheet.Cells[row, headers["VesselName"]].Text.Trim();

                        // Skip empty rows
                        if (string.IsNullOrEmpty(vesselName)) continue;

                        // Validate vessel exists
                        if (!vessels.ContainsKey(vesselName))
                        {
                            result.ErrorCount++;
                            continue;
                        }

                        // Parse required fields
                        var consumptionDate = ParseDate(worksheet.Cells[row, headers["ConsumptionDate"]].Text);
                        if (!consumptionDate.HasValue ||
                            !decimal.TryParse(worksheet.Cells[row, headers["ConsumptionLiters"]].Text, out var consumptionLiters) ||
                            !int.TryParse(worksheet.Cells[row, headers["LegsCompleted"]].Text, out var legsCompleted))
                        {
                            result.ErrorCount++;
                            continue;
                        }

                        // Parse optional fields
                        var createdDate = headers.ContainsKey("CreatedDate") ? ParseDate(worksheet.Cells[row, headers["CreatedDate"]].Text) ?? DateTime.Now : DateTime.Now;

                        // Generate month string for FIFO processing
                        var month = consumptionDate.Value.ToString("yyyy-MM");

                        // Create consumption record
                        var consumption = new Consumption
                        {
                            VesselId = vessels[vesselName].Id,
                            ConsumptionDate = consumptionDate.Value,
                            ConsumptionLiters = consumptionLiters,
                            LegsCompleted = legsCompleted,
                            Month = month,
                            CreatedDate = createdDate
                        };

                        context.Consumptions.Add(consumption);
                        result.ConsumptionImported++;
                    }
                    catch
                    {
                        result.ErrorCount++;
                    }
                }

                await context.SaveChangesAsync();
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<DataUploadResult> ProcessPurchaseCsv(string[] lines, InventoryContext context, Dictionary<string, Vessel> vessels, Dictionary<string, Supplier> suppliers)
        {
            var result = new DataUploadResult();

            try
            {
                var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
                var headerMap = headers.Select((h, i) => new { Header = h, Index = i }).ToDictionary(x => x.Header, x => x.Index);

                // Validate required columns
                string[] requiredColumns = { "VesselName", "SupplierName", "PurchaseDate", "QuantityLiters", "QuantityTons", "TotalValue", "InvoiceReference" };
                foreach (var required in requiredColumns)
                {
                    if (!headerMap.ContainsKey(required))
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Missing required column: {required}";
                        return result;
                    }
                }

                // Process data rows
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = lines[i].Split(',').Select(v => v.Trim().Trim('"')).ToArray();
                        if (values.Length < headers.Length) continue; // Skip incomplete rows

                        var vesselName = values[headerMap["VesselName"]];
                        var supplierName = values[headerMap["SupplierName"]];
                        var invoiceRef = values[headerMap["InvoiceReference"]];

                        // Skip empty rows
                        if (string.IsNullOrEmpty(vesselName) || string.IsNullOrEmpty(supplierName)) continue;

                        // Check for existing invoice reference
                        if (await context.Purchases.AnyAsync(p => p.InvoiceReference == invoiceRef))
                        {
                            result.RecordsSkipped++;
                            continue;
                        }

                        // Validate vessel and supplier exist
                        if (!vessels.ContainsKey(vesselName) || !suppliers.ContainsKey(supplierName))
                        {
                            result.ErrorCount++;
                            continue;
                        }

                        // Parse required fields
                        var purchaseDate = ParseDate(values[headerMap["PurchaseDate"]]);
                        if (!purchaseDate.HasValue ||
                            !decimal.TryParse(values[headerMap["QuantityLiters"]], out var quantityLiters) ||
                            !decimal.TryParse(values[headerMap["QuantityTons"]], out var quantityTons) ||
                            !decimal.TryParse(values[headerMap["TotalValue"]], out var totalValue))
                        {
                            result.ErrorCount++;
                            continue;
                        }

                        // Parse optional fields
                        var invoiceReceiptDate = headerMap.ContainsKey("InvoiceReceiptDate") ? ParseDate(values[headerMap["InvoiceReceiptDate"]]) : null;
                        var dueDate = headerMap.ContainsKey("DueDate") ? ParseDate(values[headerMap["DueDate"]]) : null;
                        var createdDate = headerMap.ContainsKey("CreatedDate") ? ParseDate(values[headerMap["CreatedDate"]]) ?? DateTime.Now : DateTime.Now;

                        // Calculate USD value
                        var supplier = suppliers[supplierName];
                        var totalValueUSD = supplier.Currency == "USD" ? totalValue : totalValue * supplier.ExchangeRate;

                        // Create purchase record
                        var purchase = new Purchase
                        {
                            VesselId = vessels[vesselName].Id,
                            SupplierId = suppliers[supplierName].Id,
                            PurchaseDate = purchaseDate.Value,
                            InvoiceReference = invoiceRef,
                            QuantityLiters = quantityLiters,
                            QuantityTons = quantityTons,
                            TotalValue = totalValue,
                            TotalValueUSD = totalValueUSD,
                            InvoiceReceiptDate = invoiceReceiptDate,
                            DueDate = dueDate,
                            RemainingQuantity = quantityLiters, // FIFO tracking
                            CreatedDate = createdDate
                        };

                        context.Purchases.Add(purchase);
                        result.PurchasesImported++;
                    }
                    catch
                    {
                        result.ErrorCount++;
                    }
                }

                await context.SaveChangesAsync();
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private async Task<DataUploadResult> ProcessConsumptionCsv(string[] lines, InventoryContext context, Dictionary<string, Vessel> vessels)
        {
            var result = new DataUploadResult();

            try
            {
                var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
                var headerMap = headers.Select((h, i) => new { Header = h, Index = i }).ToDictionary(x => x.Header, x => x.Index);

                // Validate required columns
                string[] requiredColumns = { "VesselName", "ConsumptionDate", "ConsumptionLiters", "LegsCompleted" };
                foreach (var required in requiredColumns)
                {
                    if (!headerMap.ContainsKey(required))
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Missing required column: {required}";
                        return result;
                    }
                }

                // Process data rows
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = lines[i].Split(',').Select(v => v.Trim().Trim('"')).ToArray();
                        if (values.Length < headers.Length) continue; // Skip incomplete rows

                        var vesselName = values[headerMap["VesselName"]];

                        // Skip empty rows
                        if (string.IsNullOrEmpty(vesselName)) continue;

                        // Validate vessel exists
                        if (!vessels.ContainsKey(vesselName))
                        {
                            result.ErrorCount++;
                            continue;
                        }

                        // Parse required fields
                        var consumptionDate = ParseDate(values[headerMap["ConsumptionDate"]]);
                        if (!consumptionDate.HasValue ||
                            !decimal.TryParse(values[headerMap["ConsumptionLiters"]], out var consumptionLiters) ||
                            !int.TryParse(values[headerMap["LegsCompleted"]], out var legsCompleted))
                        {
                            result.ErrorCount++;
                            continue;
                        }

                        // Parse optional fields
                        var createdDate = headerMap.ContainsKey("CreatedDate") ? ParseDate(values[headerMap["CreatedDate"]]) ?? DateTime.Now : DateTime.Now;

                        // Generate month string for FIFO processing
                        var month = consumptionDate.Value.ToString("yyyy-MM");

                        // Create consumption record
                        var consumption = new Consumption
                        {
                            VesselId = vessels[vesselName].Id,
                            ConsumptionDate = consumptionDate.Value,
                            ConsumptionLiters = consumptionLiters,
                            LegsCompleted = legsCompleted,
                            Month = month,
                            CreatedDate = createdDate
                        };

                        context.Consumptions.Add(consumption);
                        result.ConsumptionImported++;
                    }
                    catch
                    {
                        result.ErrorCount++;
                    }
                }

                await context.SaveChangesAsync();
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        #endregion

        #region Data Package Export

        public class DataPackageResult
        {
            public bool Success { get; set; }
            public string FilePath { get; set; } = string.Empty;
            public string ErrorMessage { get; set; } = string.Empty;
            public double SizeMB { get; set; }
            public int FilesIncluded { get; set; }
            public int BackupCount { get; set; }
        }

        private async Task<DataPackageResult> CreateDataPackageAsync(string zipFilePath)
        {
            var result = new DataPackageResult();

            try
            {
                // Create temporary directory for preparing files
                var tempDir = Path.Combine(Path.GetTempPath(), $"DOInventory_Package_{DateTime.Now.Ticks}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    var filesIncluded = 0;
                    var backupCount = 0;

                    // 1. Copy current active database
                    var activeDatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DOInventory.db");
                    if (File.Exists(activeDatabasePath))
                    {
                        var targetDbPath = Path.Combine(tempDir, "DOInventory.db");
                        await Task.Run(() => File.Copy(activeDatabasePath, targetDbPath));
                        filesIncluded++;
                    }

                    // 2. Copy recent backup files (last 5)
                    var backupDir = Path.Combine(tempDir, "Backups");
                    Directory.CreateDirectory(backupDir);

                    var recentBackups = _backupHistory.Take(5).ToList();
                    foreach (var backup in recentBackups)
                    {
                        if (File.Exists(backup.FilePath))
                        {
                            var targetBackupPath = Path.Combine(backupDir, backup.FileName);
                            await Task.Run(() => File.Copy(backup.FilePath, targetBackupPath));
                            filesIncluded++;
                            backupCount++;
                        }
                    }

                    // 3. Create README instructions
                    var readmePath = Path.Combine(tempDir, "README.txt");
                    var readmeContent = CreateReadmeContent();
                    await File.WriteAllTextAsync(readmePath, readmeContent);
                    filesIncluded++;

                    // 4. Create the ZIP file
                    if (File.Exists(zipFilePath))
                    {
                        File.Delete(zipFilePath);
                    }

                    await Task.Run(() => ZipFile.CreateFromDirectory(tempDir, zipFilePath));

                    // Get file size
                    var zipInfo = new FileInfo(zipFilePath);
                    
                    result.Success = true;
                    result.FilePath = zipFilePath;
                    result.SizeMB = zipInfo.Length / 1024.0 / 1024.0;
                    result.FilesIncluded = filesIncluded;
                    result.BackupCount = backupCount;
                }
                finally
                {
                    // Clean up temporary directory
                    try
                    {
                        Directory.Delete(tempDir, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private string CreateReadmeContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine("DO INVENTORY MANAGER - DATA PACKAGE");
            sb.AppendLine("=====================================");
            sb.AppendLine($"Package created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Source computer: {Environment.MachineName}");
            sb.AppendLine($"User: {Environment.UserName}");
            sb.AppendLine();
            sb.AppendLine("CONTENTS:");
            sb.AppendLine("---------");
            sb.AppendLine("• DOInventory.db - Current active database with all your data");
            sb.AppendLine("• Backups/ - Recent backup files for safety");
            sb.AppendLine("• README.txt - This instruction file");
            sb.AppendLine();
            sb.AppendLine("HOW TO IMPORT ON NEW COMPUTER:");
            sb.AppendLine("-------------------------------");
            sb.AppendLine("1. Install DO Inventory Manager on the new computer");
            sb.AppendLine("2. Close the application if it's running");
            sb.AppendLine("3. Copy 'DOInventory.db' to the application folder:");
            sb.AppendLine("   - Replace the existing DOInventory.db file");
            sb.AppendLine("   - The application folder is typically where the .exe is located");
            sb.AppendLine("4. (Optional) Copy backup files to:");
            sb.AppendLine("   - Documents\\DO Inventory Backups\\");
            sb.AppendLine("5. Start DO Inventory Manager");
            sb.AppendLine("6. All your data should now be available!");
            sb.AppendLine();
            sb.AppendLine("ALTERNATIVE - USE BACKUP RESTORE:");
            sb.AppendLine("----------------------------------");
            sb.AppendLine("1. Start DO Inventory Manager on new computer");
            sb.AppendLine("2. Go to 'Backup Management'");
            sb.AppendLine("3. Copy one of the backup files to Documents\\DO Inventory Backups\\");
            sb.AppendLine("4. Use 'Restore Backup' to restore your data");
            sb.AppendLine();
            sb.AppendLine("TROUBLESHOOTING:");
            sb.AppendLine("----------------");
            sb.AppendLine("• If data doesn't appear, ensure DOInventory.db is in the correct location");
            sb.AppendLine("• Check that the application has read/write permissions to the database file");
            sb.AppendLine("• Make sure you're running the same version of DO Inventory Manager");
            sb.AppendLine();
            sb.AppendLine("SUPPORT:");
            sb.AppendLine("--------");
            sb.AppendLine("If you encounter issues with data transfer, check the application's");
            sb.AppendLine("backup management system for additional restore options.");
            sb.AppendLine();
            sb.AppendLine($"Package generated by DO Inventory Manager v1.0.0");

            return sb.ToString();
        }

        #endregion

    }

    // Simple backup selection dialog
    public partial class BackupSelectionDialog : Window
    {
        public BackupInfo? SelectedBackup { get; private set; }

        public BackupSelectionDialog(List<BackupInfo> backups)
        {
            InitializeComponent();
            
            // Set display text for each backup
            foreach (var backup in backups)
            {
                backup.DisplayText = $"{backup.FormattedDate} - {backup.Operation} ({backup.FormattedSize})";
            }
            
            BackupListBox.ItemsSource = backups;
            if (backups.Any())
            {
                BackupListBox.SelectedIndex = 0; // Select most recent
            }
        }

        private void InitializeComponent()
        {
            Title = "Select Backup to Restore";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            BackupListBox = new ListBox
            {
                Margin = new Thickness(10, 10, 10, 10),
                DisplayMemberPath = "DisplayText"
            };
            
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 10, 10, 10)
            };
            
            var okButton = new Button
            {
                Content = "Restore Selected",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 5, 5, 5),
                IsDefault = true
            };
            okButton.Click += (s, e) => { SelectedBackup = (BackupInfo?)BackupListBox.SelectedItem; DialogResult = true; };
            
            var cancelButton = new Button
            {
                Content = "Cancel",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 5, 5, 5),
                IsCancel = true
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            Grid.SetRow(BackupListBox, 0);
            Grid.SetRow(buttonPanel, 1);
            
            grid.Children.Add(BackupListBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
        
        private ListBox BackupListBox;
    }
}

// Extension for BackupInfo to support dialog display
namespace DOInventoryManager.Services
{
    public partial class BackupInfo
    {
        public string DisplayText { get; set; } = string.Empty;
    }
}