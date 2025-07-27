using System.IO;

namespace DOInventoryManager.Services
{
    public class BackupService
    {
        private readonly string _backupFolder;
        private readonly string _databasePath;
        private const int MaxBackups = 10; // Keep last 10 backups

        public BackupService()
        {
            // Database file location
            _databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DOInventory.db");

            // Backup folder in user's Documents
            _backupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "DO Inventory Backups"
            );

            // Create backup folder if it doesn't exist
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
        }

        public class BackupResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string BackupPath { get; set; } = string.Empty;
            public long BackupSizeBytes { get; set; }
            public int TotalBackups { get; set; }
        }

        public async Task<BackupResult> CreateBackupAsync(string operation = "Manual")
        {
            var result = new BackupResult();

            try
            {
                // Check if database file exists
                if (!File.Exists(_databasePath))
                {
                    result.Success = false;
                    result.Message = "Database file not found. Cannot create backup.";
                    return result;
                }

                // Generate backup filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var backupFileName = $"DOInventory_{timestamp}_{operation}.db";
                var backupPath = Path.Combine(_backupFolder, backupFileName);

                // Copy database file to backup location
                await Task.Run(() => File.Copy(_databasePath, backupPath, overwrite: true));

                // Get backup file info
                var backupFileInfo = new FileInfo(backupPath);

                // Clean up old backups
                await CleanupOldBackupsAsync();

                // Get total backup count
                var backupFiles = Directory.GetFiles(_backupFolder, "DOInventory_*.db");

                result.Success = true;
                result.Message = $"Backup created successfully! ({operation})";
                result.BackupPath = backupPath;
                result.BackupSizeBytes = backupFileInfo.Length;
                result.TotalBackups = backupFiles.Length;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Backup failed: {ex.Message}";
                return result;
            }
        }

        public async Task<BackupResult> CreateBackupAsync()
        {
            return await CreateBackupAsync("Manual");
        }

        private async Task CleanupOldBackupsAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    var backupFiles = Directory.GetFiles(_backupFolder, "DOInventory_*.db")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.CreationTime)
                        .ToList();

                    // Keep only the most recent MaxBackups files
                    if (backupFiles.Count > MaxBackups)
                    {
                        var filesToDelete = backupFiles.Skip(MaxBackups);
                        foreach (var file in filesToDelete)
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch
                            {
                                // Ignore errors when deleting old backups
                            }
                        }
                    }
                });
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        public List<BackupInfo> GetBackupHistory()
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupFolder, "DOInventory_*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Select(f => new BackupInfo
                    {
                        FileName = f.Name,
                        FilePath = f.FullName,
                        CreatedDate = f.CreationTime,
                        SizeBytes = f.Length,
                        SizeMB = Math.Round(f.Length / 1024.0 / 1024.0, 2),
                        Operation = ExtractOperationFromFileName(f.Name)
                    })
                    .ToList();

                return backupFiles;
            }
            catch
            {
                return [];
            }
        }

        private string ExtractOperationFromFileName(string fileName)
        {
            try
            {
                // DOInventory_2025-07-26_14-30-15_Manual.db
                var parts = fileName.Replace(".db", "").Split('_');
                if (parts.Length >= 4)
                {
                    return parts[3]; // Operation part
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public string GetBackupFolder()
        {
            return _backupFolder;
        }

        public bool OpenBackupFolder()
        {
            try
            {
                if (Directory.Exists(_backupFolder))
                {
                    System.Diagnostics.Process.Start("explorer.exe", _backupFolder);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RestoreFromBackupAsync(string backupFilePath)
        {
            try
            {
                if (!File.Exists(backupFilePath))
                    return false;

                // Create a backup of current database before restoring
                await CreateBackupAsync("PreRestore");

                // Copy backup file to database location
                await Task.Run(() => File.Copy(backupFilePath, _databasePath, overwrite: true));

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public long SizeBytes { get; set; }
        public double SizeMB { get; set; }
        public string Operation { get; set; } = string.Empty;

        public string FormattedSize => $"{SizeMB:F2} MB";
        public string FormattedDate => CreatedDate.ToString("dd/MM/yyyy HH:mm:ss");
    }
}