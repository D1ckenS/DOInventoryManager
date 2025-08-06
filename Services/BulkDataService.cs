using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class BulkDataService
    {
        public class BulkDeleteResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int DeletedCount { get; set; }
            public int FailedCount { get; set; }
            public List<string> Details { get; set; } = [];
            public List<string> Errors { get; set; } = [];
        }

        public class PurchaseFilter
        {
            public DateTime? DateFrom { get; set; }
            public DateTime? DateTo { get; set; }
            public int? VesselId { get; set; }
            public int? SupplierId { get; set; }
            public string? InvoiceReference { get; set; }
        }

        public class ConsumptionFilter
        {
            public DateTime? Month { get; set; }
            public int? VesselId { get; set; }
        }

        public async Task<List<PurchaseSelectionItem>> GetFilteredPurchasesAsync(PurchaseFilter filter)
        {
            try
            {
                using var context = new InventoryContext();

                var query = context.Purchases
                    .Include(p => p.Vessel)
                    .Include(p => p.Supplier)
                    .AsQueryable();

                // Apply filters
                if (filter.DateFrom.HasValue)
                {
                    query = query.Where(p => p.PurchaseDate >= filter.DateFrom.Value);
                }

                if (filter.DateTo.HasValue)
                {
                    query = query.Where(p => p.PurchaseDate <= filter.DateTo.Value);
                }

                if (filter.VesselId.HasValue)
                {
                    query = query.Where(p => p.VesselId == filter.VesselId.Value);
                }

                if (filter.SupplierId.HasValue)
                {
                    query = query.Where(p => p.SupplierId == filter.SupplierId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.InvoiceReference))
                {
                    query = query.Where(p => p.InvoiceReference.Contains(filter.InvoiceReference));
                }

                var purchases = await query
                    .OrderByDescending(p => p.PurchaseDate)
                    .ToListAsync();

                return purchases.Select(p => new PurchaseSelectionItem
                {
                    Id = p.Id,
                    VesselId = p.VesselId,
                    Vessel = p.Vessel,
                    SupplierId = p.SupplierId,
                    Supplier = p.Supplier,
                    PurchaseDate = p.PurchaseDate,
                    InvoiceReference = p.InvoiceReference,
                    QuantityLiters = p.QuantityLiters,
                    QuantityTons = p.QuantityTons,
                    TotalValue = p.TotalValue,
                    TotalValueUSD = p.TotalValueUSD,
                    InvoiceReceiptDate = p.InvoiceReceiptDate,
                    DueDate = p.DueDate,
                    RemainingQuantity = p.RemainingQuantity,
                    CreatedDate = p.CreatedDate,
                    PaymentDate = p.PaymentDate,
                    PaymentAmount = p.PaymentAmount,
                    PaymentAmountUSD = p.PaymentAmountUSD,
                    IsSelected = false
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error filtering purchases: {ex.Message}", ex);
            }
        }

        public async Task<List<ConsumptionSelectionItem>> GetFilteredConsumptionsAsync(ConsumptionFilter filter)
        {
            try
            {
                using var context = new InventoryContext();

                var query = context.Consumptions
                    .Include(c => c.Vessel)
                    .AsQueryable();

                // Apply filters
                if (filter.Month.HasValue)
                {
                    var monthString = filter.Month.Value.ToString("yyyy-MM");
                    query = query.Where(c => c.Month == monthString);
                }

                if (filter.VesselId.HasValue)
                {
                    query = query.Where(c => c.VesselId == filter.VesselId.Value);
                }

                var consumptions = await query
                    .OrderByDescending(c => c.ConsumptionDate)
                    .ToListAsync();

                return consumptions.Select(c => new ConsumptionSelectionItem
                {
                    Id = c.Id,
                    VesselId = c.VesselId,
                    Vessel = c.Vessel,
                    ConsumptionDate = c.ConsumptionDate,
                    Month = c.Month,
                    ConsumptionLiters = c.ConsumptionLiters,
                    LegsCompleted = c.LegsCompleted,
                    CreatedDate = c.CreatedDate,
                    IsSelected = false
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error filtering consumptions: {ex.Message}", ex);
            }
        }

        public async Task<List<PurchaseSelectionItem>> GetRecentPurchasesAsync(int limit = 30)
        {
            try
            {
                using var context = new InventoryContext();

                var purchases = await context.Purchases
                    .Include(p => p.Vessel)
                    .Include(p => p.Supplier)
                    .OrderByDescending(p => p.PurchaseDate)
                    .Take(limit)
                    .ToListAsync();

                return purchases.Select(p => new PurchaseSelectionItem
                {
                    Id = p.Id,
                    VesselId = p.VesselId,
                    Vessel = p.Vessel,
                    SupplierId = p.SupplierId,
                    Supplier = p.Supplier,
                    PurchaseDate = p.PurchaseDate,
                    InvoiceReference = p.InvoiceReference,
                    QuantityLiters = p.QuantityLiters,
                    QuantityTons = p.QuantityTons,
                    TotalValue = p.TotalValue,
                    TotalValueUSD = p.TotalValueUSD,
                    InvoiceReceiptDate = p.InvoiceReceiptDate,
                    DueDate = p.DueDate,
                    RemainingQuantity = p.RemainingQuantity,
                    CreatedDate = p.CreatedDate,
                    PaymentDate = p.PaymentDate,
                    PaymentAmount = p.PaymentAmount,
                    PaymentAmountUSD = p.PaymentAmountUSD,
                    IsSelected = false
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading recent purchases: {ex.Message}", ex);
            }
        }

        public async Task<List<ConsumptionSelectionItem>> GetRecentConsumptionsAsync(int monthsBack = 2)
        {
            try
            {
                using var context = new InventoryContext();

                // Calculate the cutoff date (monthsBack months ago)
                var cutoffDate = DateTime.Now.AddMonths(-monthsBack);
                var cutoffMonthString = cutoffDate.ToString("yyyy-MM");

                var consumptions = await context.Consumptions
                    .Include(c => c.Vessel)
                    .Where(c => string.Compare(c.Month, cutoffMonthString) >= 0)
                    .OrderByDescending(c => c.ConsumptionDate)
                    .ToListAsync();

                return consumptions.Select(c => new ConsumptionSelectionItem
                {
                    Id = c.Id,
                    VesselId = c.VesselId,
                    Vessel = c.Vessel,
                    ConsumptionDate = c.ConsumptionDate,
                    Month = c.Month,
                    ConsumptionLiters = c.ConsumptionLiters,
                    LegsCompleted = c.LegsCompleted,
                    CreatedDate = c.CreatedDate,
                    IsSelected = false
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading recent consumptions: {ex.Message}", ex);
            }
        }

        public async Task<int> GetTotalPurchaseCountAsync()
        {
            try
            {
                using var context = new InventoryContext();
                return await context.Purchases.CountAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting total purchase count: {ex.Message}", ex);
            }
        }

        public async Task<int> GetTotalConsumptionCountAsync()
        {
            try
            {
                using var context = new InventoryContext();
                return await context.Consumptions.CountAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting total consumption count: {ex.Message}", ex);
            }
        }

        public async Task<BulkDeleteResult> DeleteSelectedPurchasesAsync(List<int> purchaseIds)
        {
            var result = new BulkDeleteResult();

            try
            {
                using var context = new InventoryContext();

                result.Details.Add($"Starting bulk deletion of {purchaseIds.Count} purchases...");

                foreach (var purchaseId in purchaseIds)
                {
                    try
                    {
                        // Check if purchase has allocations
                        var allocations = await context.Allocations
                            .Where(a => a.PurchaseId == purchaseId)
                            .ToListAsync();

                        var purchase = await context.Purchases
                            .Include(p => p.Vessel)
                            .Include(p => p.Supplier)
                            .FirstOrDefaultAsync(p => p.Id == purchaseId);

                        if (purchase == null)
                        {
                            result.FailedCount++;
                            result.Errors.Add($"Purchase with ID {purchaseId} not found");
                            continue;
                        }

                        // Remove allocations first
                        if (allocations.Any())
                        {
                            context.Allocations.RemoveRange(allocations);
                            result.Details.Add($"Removed {allocations.Count} allocations for purchase {purchase.InvoiceReference}");
                        }

                        // Remove the purchase
                        context.Purchases.Remove(purchase);
                        result.DeletedCount++;
                        result.Details.Add($"Deleted purchase: {purchase.InvoiceReference} ({purchase.PurchaseDate:dd/MM/yyyy})");
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to delete purchase ID {purchaseId}: {ex.Message}");
                    }
                }

                // Save all changes in a single transaction
                await context.SaveChangesAsync();

                result.Success = result.DeletedCount > 0;
                result.Message = result.Success
                    ? $"Successfully deleted {result.DeletedCount} purchases"
                    : "No purchases were deleted";

                if (result.FailedCount > 0)
                {
                    result.Message += $" ({result.FailedCount} failed)";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Bulk deletion failed: {ex.Message}";
                result.Errors.Add($"Critical error: {ex.Message}");
            }

            return result;
        }

        public async Task<BulkDeleteResult> DeleteSelectedConsumptionsAsync(List<int> consumptionIds)
        {
            var result = new BulkDeleteResult();

            try
            {
                using var context = new InventoryContext();

                result.Details.Add($"Starting bulk deletion of {consumptionIds.Count} consumptions...");

                foreach (var consumptionId in consumptionIds)
                {
                    try
                    {
                        // Check if consumption has allocations
                        var allocations = await context.Allocations
                            .Where(a => a.ConsumptionId == consumptionId)
                            .ToListAsync();

                        var consumption = await context.Consumptions
                            .Include(c => c.Vessel)
                            .FirstOrDefaultAsync(c => c.Id == consumptionId);

                        if (consumption == null)
                        {
                            result.FailedCount++;
                            result.Errors.Add($"Consumption with ID {consumptionId} not found");
                            continue;
                        }

                        // Remove allocations first
                        if (allocations.Any())
                        {
                            context.Allocations.RemoveRange(allocations);
                            result.Details.Add($"Removed {allocations.Count} allocations for consumption {consumption.Vessel?.Name} ({consumption.ConsumptionDate:dd/MM/yyyy})");
                        }

                        // Remove the consumption
                        context.Consumptions.Remove(consumption);
                        result.DeletedCount++;
                        result.Details.Add($"Deleted consumption: {consumption.Vessel?.Name} - {consumption.ConsumptionDate:dd/MM/yyyy}");
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to delete consumption ID {consumptionId}: {ex.Message}");
                    }
                }

                // Save all changes in a single transaction
                await context.SaveChangesAsync();

                result.Success = result.DeletedCount > 0;
                result.Message = result.Success
                    ? $"Successfully deleted {result.DeletedCount} consumptions"
                    : "No consumptions were deleted";

                if (result.FailedCount > 0)
                {
                    result.Message += $" ({result.FailedCount} failed)";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Bulk deletion failed: {ex.Message}";
                result.Errors.Add($"Critical error: {ex.Message}");
            }

            return result;
        }

        public async Task<int> ValidatePurchasesForDeletionAsync(List<int> purchaseIds)
        {
            try
            {
                using var context = new InventoryContext();

                var allocatedPurchases = await context.Allocations
                    .Where(a => purchaseIds.Contains(a.PurchaseId))
                    .Select(a => a.PurchaseId)
                    .Distinct()
                    .CountAsync();

                return allocatedPurchases;
            }
            catch (Exception)
            {
                return -1; // Error occurred
            }
        }

        public async Task<int> ValidateConsumptionsForDeletionAsync(List<int> consumptionIds)
        {
            try
            {
                using var context = new InventoryContext();

                var allocatedConsumptions = await context.Allocations
                    .Where(a => consumptionIds.Contains(a.ConsumptionId))
                    .Select(a => a.ConsumptionId)
                    .Distinct()
                    .CountAsync();

                return allocatedConsumptions;
            }
            catch (Exception)
            {
                return -1; // Error occurred
            }
        }
    }
}