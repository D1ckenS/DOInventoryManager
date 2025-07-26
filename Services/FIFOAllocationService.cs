using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class FIFOAllocationService
    {
        public class AllocationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int ProcessedConsumptions { get; set; }
            public int AllocationsCreated { get; set; }
            public decimal TotalAllocatedQuantity { get; set; }
            public decimal TotalAllocatedValue { get; set; }
            public List<string> Details { get; set; } = new List<string>();
        }

        public async Task<AllocationResult> RunFIFOAllocationAsync()
        {
            var result = new AllocationResult();

            try
            {
                using var context = new InventoryContext();

                result.Details.Add("Starting FIFO Allocation Process...");

                // Step 1: Get unallocated consumption records
                var unallocatedConsumptions = await GetUnallocatedConsumptionsAsync(context);
                result.Details.Add($"Found {unallocatedConsumptions.Count} unallocated consumption records");

                if (unallocatedConsumptions.Count == 0)
                {
                    result.Success = true;
                    result.Message = "No unallocated consumption records found.";
                    return result;
                }

                // Step 2: Group by month and process chronologically
                var monthGroups = unallocatedConsumptions
                    .GroupBy(c => c.Month)
                    .OrderBy(g => g.Key)
                    .ToList();

                result.Details.Add($"Processing {monthGroups.Count} months chronologically");

                // Step 3: Process each month
                foreach (var monthGroup in monthGroups)
                {
                    var monthResult = await ProcessMonthAllocationsAsync(context, monthGroup.Key, monthGroup.ToList());

                    result.ProcessedConsumptions += monthResult.ProcessedConsumptions;
                    result.AllocationsCreated += monthResult.AllocationsCreated;
                    result.TotalAllocatedQuantity += monthResult.TotalAllocatedQuantity;
                    result.TotalAllocatedValue += monthResult.TotalAllocatedValue;
                    result.Details.AddRange(monthResult.Details);
                }

                // Step 4: Save all changes
                await context.SaveChangesAsync();
                result.Details.Add("All allocations saved to database");

                result.Success = true;
                result.Message = $"FIFO allocation completed successfully! Processed {result.ProcessedConsumptions} consumption records, created {result.AllocationsCreated} allocations.";

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Error during FIFO allocation: {ex.Message}";
                result.Details.Add($"ERROR: {ex.Message}");
            }

            return result;
        }

        private async Task<List<Consumption>> GetUnallocatedConsumptionsAsync(InventoryContext context)
        {
            // Get consumptions that don't have allocations yet
            var allocatedConsumptionIds = await context.Allocations
                .Select(a => a.ConsumptionId)
                .Distinct()
                .ToListAsync();

            return await context.Consumptions
                .Include(c => c.Vessel)
                .Where(c => !allocatedConsumptionIds.Contains(c.Id))
                .OrderBy(c => c.Month)
                .ThenBy(c => c.ConsumptionDate)
                .ThenBy(c => c.Vessel.Name)
                .ToListAsync();
        }

        private async Task<AllocationResult> ProcessMonthAllocationsAsync(InventoryContext context, string month, List<Consumption> monthConsumptions)
        {
            var result = new AllocationResult();
            result.Details.Add($"Processing month: {month}");

            // Group by vessel for this month
            var vesselGroups = monthConsumptions
                .GroupBy(c => c.VesselId)
                .ToList();

            foreach (var vesselGroup in vesselGroups)
            {
                var vesselResult = await ProcessVesselAllocationsAsync(context, month, vesselGroup.ToList());

                result.ProcessedConsumptions += vesselResult.ProcessedConsumptions;
                result.AllocationsCreated += vesselResult.AllocationsCreated;
                result.TotalAllocatedQuantity += vesselResult.TotalAllocatedQuantity;
                result.TotalAllocatedValue += vesselResult.TotalAllocatedValue;
                result.Details.AddRange(vesselResult.Details);
            }

            return result;
        }

        private async Task<AllocationResult> ProcessVesselAllocationsAsync(InventoryContext context, string month, List<Consumption> vesselConsumptions)
        {
            var result = new AllocationResult();

            if (!vesselConsumptions.Any()) return result;

            var vesselId = vesselConsumptions.First().VesselId;
            var vesselName = vesselConsumptions.First().Vessel.Name;

            // Calculate total consumption for this vessel in this month
            var totalConsumption = vesselConsumptions.Sum(c => c.ConsumptionLiters);
            result.Details.Add($"  Vessel {vesselName}: {totalConsumption:N2} L consumption");

            // Parse month to create date for comparison
            var monthParts = month.Split('-');
            var year = int.Parse(monthParts[0]);
            var monthNum = int.Parse(monthParts[1]);
            var monthEndDate = new DateTime(year, monthNum, DateTime.DaysInMonth(year, monthNum));

            // Get available purchases for this vessel (FIFO order - oldest first)
            var availablePurchases = await context.Purchases
                .Include(p => p.Supplier)
                .Where(p => p.VesselId == vesselId &&
                           p.RemainingQuantity > 0 &&
                           p.PurchaseDate <= monthEndDate)
                .OrderBy(p => p.PurchaseDate)
                .ThenBy(p => p.Id)
                .ToListAsync();

            if (!availablePurchases.Any())
            {
                result.Details.Add($"    WARNING: No available purchases for vessel {vesselName} in month {month}");
                return result;
            }

            // Allocate consumption against purchases using FIFO
            var remainingConsumption = totalConsumption;
            var allocationsCreated = new List<Allocation>();

            foreach (var purchase in availablePurchases)
            {
                if (remainingConsumption <= 0) break;

                var allocatedQuantity = Math.Min(remainingConsumption, purchase.RemainingQuantity);

                if (allocatedQuantity > 0)
                {
                    // Calculate allocated value (proportional to quantity)
                    var allocatedValue = (allocatedQuantity / purchase.QuantityLiters) * purchase.TotalValue;
                    var allocatedValueUSD = (allocatedQuantity / purchase.QuantityLiters) * purchase.TotalValueUSD;

                    // Create allocation records for each consumption record (proportionally)
                    foreach (var consumption in vesselConsumptions)
                    {
                        var consumptionProportion = consumption.ConsumptionLiters / totalConsumption;
                        var allocationForThisConsumption = allocatedQuantity * consumptionProportion;
                        var valueForThisConsumption = allocatedValue * consumptionProportion;
                        var valueUSDForThisConsumption = allocatedValueUSD * consumptionProportion;

                        if (allocationForThisConsumption > 0)
                        {
                            var allocation = new Allocation
                            {
                                PurchaseId = purchase.Id,
                                ConsumptionId = consumption.Id,
                                AllocatedQuantity = allocationForThisConsumption,
                                AllocatedValue = valueForThisConsumption,
                                AllocatedValueUSD = valueUSDForThisConsumption,
                                Month = month,
                                CreatedDate = DateTime.Now
                            };

                            allocationsCreated.Add(allocation);
                        }
                    }

                    // Update purchase remaining quantity
                    purchase.RemainingQuantity -= allocatedQuantity;

                    // Update totals
                    result.TotalAllocatedQuantity += allocatedQuantity;
                    result.TotalAllocatedValue += allocatedValueUSD;
                    remainingConsumption -= allocatedQuantity;

                    result.Details.Add($"    Allocated {allocatedQuantity:N2} L from purchase {purchase.InvoiceReference} ({purchase.PurchaseDate:yyyy-MM-dd})");
                }
            }

            // Add all allocations to context
            context.Allocations.AddRange(allocationsCreated);

            result.ProcessedConsumptions = vesselConsumptions.Count;
            result.AllocationsCreated = allocationsCreated.Count;

            if (remainingConsumption > 0.01m) // Small tolerance for rounding
            {
                result.Details.Add($"    WARNING: Unallocated consumption remaining: {remainingConsumption:N2} L for vessel {vesselName}");
            }

            return result;
        }

        public async Task<List<Allocation>> GetAllocationsByMonthAsync(string month)
        {
            using var context = new InventoryContext();

            return await context.Allocations
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Vessel)
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Supplier)
                .Include(a => a.Consumption)
                    .ThenInclude(c => c.Vessel)
                .Where(a => a.Month == month)
                .OrderBy(a => a.Purchase.Vessel.Name)
                .ThenBy(a => a.Purchase.PurchaseDate)
                .ThenBy(a => a.Consumption.ConsumptionDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetMonthlyAllocationSummaryAsync()
        {
            using var context = new InventoryContext();

            return await context.Allocations
                .GroupBy(a => a.Month)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Sum(a => a.AllocatedValueUSD)
                );
        }
    }
}