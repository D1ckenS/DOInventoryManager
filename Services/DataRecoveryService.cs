using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class DataRecoveryService
    {
        public class RecoveryResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int FixedPurchases { get; set; }
            public int RemovedAllocations { get; set; }
            public List<string> Details { get; set; } = new List<string>();
        }

        public async Task<RecoveryResult> RerunFIFOAllocationAsync()
        {
            var result = new RecoveryResult();

            try
            {
                using var context = new InventoryContext();

                result.Details.Add("Starting complete FIFO re-allocation...");

                // Step 1: Clear all existing allocations
                var existingAllocations = await context.Allocations.ToListAsync();
                context.Allocations.RemoveRange(existingAllocations);
                result.RemovedAllocations = existingAllocations.Count;
                result.Details.Add($"Removed {existingAllocations.Count} existing allocations");

                // Step 2: Reset all purchase remaining quantities to original
                var purchases = await context.Purchases.ToListAsync();
                foreach (var purchase in purchases)
                {
                    purchase.RemainingQuantity = purchase.QuantityLiters;
                }
                result.FixedPurchases = purchases.Count;
                result.Details.Add($"Reset remaining quantities for {purchases.Count} purchases");

                await context.SaveChangesAsync();
                result.Details.Add("Cleared existing data successfully");

                // Step 3: Re-run FIFO allocation
                var fifoService = new FIFOAllocationService();
                var fifoResult = await fifoService.RunFIFOAllocationAsync();

                result.Details.AddRange(fifoResult.Details);

                if (fifoResult.Success)
                {
                    result.Success = true;
                    result.Message = $"Data recovery completed successfully!\n\n" +
                                   $"• Fixed {result.FixedPurchases} purchase records\n" +
                                   $"• Removed {result.RemovedAllocations} old allocations\n" +
                                   $"• Created {fifoResult.AllocationsCreated} new allocations\n" +
                                   $"• Processed {fifoResult.ProcessedConsumptions} consumptions";
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Data cleared but FIFO re-allocation failed: {fifoResult.Message}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error during data recovery: {ex.Message}";
                result.Details.Add($"ERROR: {ex.Message}");
            }

            return result;
        }

        public async Task<RecoveryResult> ManualCleanupInconsistentDataAsync()
        {
            var result = new RecoveryResult();

            try
            {
                using var context = new InventoryContext();

                result.Details.Add("Starting manual cleanup of inconsistent data...");

                // Find purchases with negative remaining quantities
                var problematicPurchases = await context.Purchases
                    .Where(p => p.RemainingQuantity < 0)
                    .ToListAsync();

                result.Details.Add($"Found {problematicPurchases.Count} purchases with negative remaining quantities");

                foreach (var purchase in problematicPurchases)
                {
                    // Get total allocated for this purchase - using ToList to avoid SQLite decimal sum issues
                    var allocations = await context.Allocations
                        .Where(a => a.PurchaseId == purchase.Id)
                        .Select(a => a.AllocatedQuantity)
                        .ToListAsync();

                    var totalAllocated = allocations.Sum();

                    // If allocated more than available, remove excess allocations
                    if (totalAllocated > purchase.QuantityLiters)
                    {
                        var excess = totalAllocated - purchase.QuantityLiters;
                        result.Details.Add($"Purchase {purchase.InvoiceReference}: {excess:N3}L over-allocated");

                        // Remove allocations starting from the newest
                        var allocationsToRemove = await context.Allocations
                            .Where(a => a.PurchaseId == purchase.Id)
                            .OrderByDescending(a => a.CreatedDate)
                            .ToListAsync();

                        decimal removedQuantity = 0;
                        var allocationsRemoved = 0;

                        foreach (var allocation in allocationsToRemove)
                        {
                            if (removedQuantity >= excess) break;

                            context.Allocations.Remove(allocation);
                            removedQuantity += allocation.AllocatedQuantity;
                            allocationsRemoved++;

                            result.Details.Add($"  Removed allocation: {allocation.AllocatedQuantity:N3}L");
                        }

                        result.RemovedAllocations += allocationsRemoved;
                    }

                    // Recalculate remaining quantity - using ToList to avoid SQLite decimal sum issues
                    var newAllocations = await context.Allocations
                        .Where(a => a.PurchaseId == purchase.Id)
                        .Select(a => a.AllocatedQuantity)
                        .ToListAsync();

                    var newTotalAllocated = newAllocations.Sum();

                    purchase.RemainingQuantity = purchase.QuantityLiters - newTotalAllocated;
                    result.FixedPurchases++;

                    result.Details.Add($"  Fixed: {purchase.InvoiceReference} - Remaining: {purchase.RemainingQuantity:N3}L");
                }

                await context.SaveChangesAsync();

                result.Success = true;
                result.Message = $"Manual cleanup completed!\n\n" +
                               $"• Fixed {result.FixedPurchases} purchases\n" +
                               $"• Removed {result.RemovedAllocations} excess allocations\n\n" +
                               "Data should now be consistent.";

                result.Details.Add("Manual cleanup completed successfully");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error during manual cleanup: {ex.Message}";
                result.Details.Add($"ERROR: {ex.Message}");
            }

            return result;
        }

        public async Task<List<string>> GetDataInconsistencyReportAsync()
        {
            var issues = new List<string>();

            try
            {
                using var context = new InventoryContext();

                // Check for purchases with negative remaining quantities
                var negativeRemaining = await context.Purchases
                    .Where(p => p.RemainingQuantity < 0)
                    .Select(p => new { p.InvoiceReference, p.RemainingQuantity })
                    .ToListAsync();

                foreach (var item in negativeRemaining)
                {
                    issues.Add($"Purchase {item.InvoiceReference}: Negative remaining quantity ({item.RemainingQuantity:N3}L)");
                }

                // Check for over-allocated purchases
                var purchases = await context.Purchases
                    .Include(p => p.Allocations)
                    .ToListAsync();

                foreach (var purchase in purchases)
                {
                    var totalAllocated = purchase.Allocations.Sum(a => a.AllocatedQuantity);
                    if (totalAllocated > purchase.QuantityLiters)
                    {
                        var excess = totalAllocated - purchase.QuantityLiters;
                        issues.Add($"Purchase {purchase.InvoiceReference}: Over-allocated by {excess:N3}L ({totalAllocated:N3}L allocated vs {purchase.QuantityLiters:N3}L available)");
                    }
                }

                // Check for inconsistent remaining quantities
                foreach (var purchase in purchases)
                {
                    var totalAllocated = purchase.Allocations.Sum(a => a.AllocatedQuantity);
                    var expectedRemaining = purchase.QuantityLiters - totalAllocated;
                    if (Math.Abs(purchase.RemainingQuantity - expectedRemaining) > 0.001m)
                    {
                        issues.Add($"Purchase {purchase.InvoiceReference}: Inconsistent remaining quantity (stored: {purchase.RemainingQuantity:N3}L, calculated: {expectedRemaining:N3}L)");
                    }
                }

                if (issues.Count == 0)
                {
                    issues.Add("No data inconsistencies found - all data appears correct!");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"Error checking data consistency: {ex.Message}");
            }

            return issues;
        }
    }
}