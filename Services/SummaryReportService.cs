using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class SummaryService
    {
        #region Data Models

        public class MonthlySummaryResult
        {
            public string Month { get; set; } = string.Empty;
            public List<VesselConsumptionSummary> ConsumptionSummary { get; set; } = [];
            public List<SupplierPurchaseSummary> PurchaseSummary { get; set; } = [];
            public List<AllocationSummary> AllocationSummary { get; set; } = [];
            public FinancialSummary FinancialSummary { get; set; } = new();
            public ExecutiveSummary ExecutiveSummary { get; set; } = new();
        }

        public class VesselConsumptionSummary
        {
            public string VesselName { get; set; } = string.Empty;
            public string VesselType { get; set; } = string.Empty;
            public string Route { get; set; } = string.Empty;
            public decimal TotalConsumptionL { get; set; }
            public decimal TotalConsumptionT { get; set; }
            public int TotalLegs { get; set; }
            public decimal AvgConsumptionPerLegL { get; set; }
            public decimal AvgConsumptionPerLegT { get; set; }
            public decimal TotalAllocatedValueUSD { get; set; }
            public decimal CostPerLiter { get; set; }
            public decimal CostPerTon { get; set; }
            public int ConsumptionEntries { get; set; }
        }

        public class SupplierPurchaseSummary
        {
            public string SupplierName { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public decimal TotalPurchasesL { get; set; }
            public decimal TotalPurchasesT { get; set; }
            public decimal TotalValueOriginal { get; set; }
            public decimal TotalValueUSD { get; set; }
            public decimal AvgPricePerLiter { get; set; }
            public decimal AvgPricePerTon { get; set; }
            public int PurchaseCount { get; set; }
            public decimal RemainingL { get; set; }
            public decimal RemainingT { get; set; }
        }

        public class AllocationSummary
        {
            public string VesselName { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public decimal AllocatedQuantityL { get; set; }
            public decimal AllocatedQuantityT { get; set; }
            public decimal AllocatedValueUSD { get; set; }
            public int AllocationCount { get; set; }
            public DateTime OldestPurchaseDate { get; set; }
            public DateTime NewestPurchaseDate { get; set; }
        }

        public class FinancialSummary
        {
            public decimal TotalPurchaseValueUSD { get; set; }
            public decimal TotalConsumptionValueUSD { get; set; }
            public decimal RemainingInventoryValueUSD { get; set; }
            public List<CurrencyBreakdown> CurrencyBreakdowns { get; set; } = [];
            public List<PaymentStatus> PaymentStatuses { get; set; } = [];
            public decimal AvgCostPerLiterUSD { get; set; }
            public decimal AvgCostPerTonUSD { get; set; }
        }

        public class CurrencyBreakdown
        {
            public string Currency { get; set; } = string.Empty;
            public decimal TotalValue { get; set; }
            public decimal TotalValueUSD { get; set; }
            public decimal ExchangeRate { get; set; }
        }

        public class PaymentStatus
        {
            public string SupplierName { get; set; } = string.Empty;
            public decimal TotalDue { get; set; }
            public decimal TotalOverdue { get; set; }
            public int OverdueCount { get; set; }
            public int TotalInvoices { get; set; }
        }

        public class ExecutiveSummary
        {
            public decimal TotalFleetConsumptionL { get; set; }
            public decimal TotalFleetConsumptionT { get; set; }
            public int TotalLegsCompleted { get; set; }
            public decimal FleetEfficiencyLPerLeg { get; set; }
            public decimal FleetEfficiencyTPerLeg { get; set; }
            public decimal TotalOperatingCostUSD { get; set; }
            public decimal CostPerLeg { get; set; }
            public int VesselsOperated { get; set; }
            public int SuppliersUsed { get; set; }
            public decimal InventoryTurnover { get; set; }
        }

        #endregion

        public async Task<MonthlySummaryResult> GenerateMonthlySummaryAsync(string month)
        {
            using var context = new InventoryContext();

            var result = new MonthlySummaryResult
            {
                Month = month,
                ConsumptionSummary = await GenerateConsumptionSummaryAsync(context, month),
                PurchaseSummary = await GeneratePurchaseSummaryAsync(context, month),
                AllocationSummary = await GenerateAllocationSummaryAsync(context, month),
                FinancialSummary = await GenerateFinancialSummaryAsync(context, month),
                ExecutiveSummary = await GenerateExecutiveSummaryAsync(context, month)
            };

            return result;
        }

        private async Task<List<VesselConsumptionSummary>> GenerateConsumptionSummaryAsync(InventoryContext context, string month)
        {
            var consumptions = await context.Consumptions
                .Include(c => c.Vessel)
                .Include(c => c.Allocations)
                .Where(c => c.Month == month)
                .ToListAsync();

            var summary = consumptions
                .GroupBy(c => new { c.VesselId, c.Vessel.Name, c.Vessel.Type, c.Vessel.Route })
                .Select(g => new VesselConsumptionSummary
                {
                    VesselName = g.Key.Name,
                    VesselType = g.Key.Type,
                    Route = g.Key.Route,
                    TotalConsumptionL = g.Sum(c => c.ConsumptionLiters),
                    TotalLegs = g.Sum(c => c.LegsCompleted ?? 0),
                    ConsumptionEntries = g.Count(),
                    TotalAllocatedValueUSD = g.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD)
                })
                .ToList();

            // Calculate derived fields
            foreach (var item in summary)
            {
                if (item.TotalLegs > 0)
                {
                    item.AvgConsumptionPerLegL = item.TotalConsumptionL / item.TotalLegs;
                }

                if (item.TotalConsumptionL > 0)
                {
                    item.CostPerLiter = item.TotalAllocatedValueUSD / item.TotalConsumptionL;
                }

                // Get FIFO density for tons calculation
                var vesselId = consumptions.First(c => c.Vessel.Name == item.VesselName).VesselId;
                var density = await GetFIFODensityAsync(context, vesselId);
                item.TotalConsumptionT = (item.TotalConsumptionL / 1000) * density;

                if (item.TotalLegs > 0)
                {
                    item.AvgConsumptionPerLegT = item.TotalConsumptionT / item.TotalLegs;
                }

                if (item.TotalConsumptionT > 0)
                {
                    item.CostPerTon = item.TotalAllocatedValueUSD / item.TotalConsumptionT;
                }
            }

            return summary.OrderByDescending(s => s.TotalConsumptionL).ToList();
        }

        private async Task<List<SupplierPurchaseSummary>> GeneratePurchaseSummaryAsync(InventoryContext context, string month)
        {
            // Parse the month to get year and month numbers
            var monthParts = month.Split('-');
            var year = int.Parse(monthParts[0]);
            var monthNum = int.Parse(monthParts[1]);

            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Where(p => p.PurchaseDate.Year == year && p.PurchaseDate.Month == monthNum)
                .ToListAsync();

            var summary = purchases
                .GroupBy(p => new { p.SupplierId, p.Supplier.Name, p.Supplier.Currency })
                .Select(g => new SupplierPurchaseSummary
                {
                    SupplierName = g.Key.Name,
                    Currency = g.Key.Currency,
                    TotalPurchasesL = g.Sum(p => p.QuantityLiters),
                    TotalPurchasesT = g.Sum(p => p.QuantityTons),
                    TotalValueOriginal = g.Sum(p => p.TotalValue),
                    TotalValueUSD = g.Sum(p => p.TotalValueUSD),
                    PurchaseCount = g.Count(),
                    RemainingL = g.Sum(p => p.RemainingQuantity),
                    RemainingT = g.Sum(p => p.RemainingQuantityTons)
                })
                .ToList();

            // Calculate averages
            foreach (var item in summary)
            {
                if (item.TotalPurchasesL > 0)
                {
                    item.AvgPricePerLiter = item.TotalValueOriginal / item.TotalPurchasesL;
                }

                if (item.TotalPurchasesT > 0)
                {
                    item.AvgPricePerTon = item.TotalValueOriginal / item.TotalPurchasesT;
                }
            }

            return summary.OrderByDescending(s => s.TotalValueUSD).ToList();
        }

        private async Task<List<AllocationSummary>> GenerateAllocationSummaryAsync(InventoryContext context, string month)
        {
            var allocations = await context.Allocations
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Vessel)
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Supplier)
                .Where(a => a.Month == month)
                .ToListAsync();

            var summary = allocations
                .GroupBy(a => new { VesselName = a.Purchase.Vessel.Name, SupplierName = a.Purchase.Supplier.Name })
                .Select(g => new AllocationSummary
                {
                    VesselName = g.Key.VesselName,
                    SupplierName = g.Key.SupplierName,
                    AllocatedQuantityL = g.Sum(a => a.AllocatedQuantity),
                    AllocatedQuantityT = g.Sum(a => a.AllocatedQuantityTons),
                    AllocatedValueUSD = g.Sum(a => a.AllocatedValueUSD),
                    AllocationCount = g.Count(),
                    OldestPurchaseDate = g.Min(a => a.Purchase.PurchaseDate),
                    NewestPurchaseDate = g.Max(a => a.Purchase.PurchaseDate)
                })
                .OrderByDescending(s => s.AllocatedValueUSD)
                .ToList();

            return summary;
        }

        private async Task<FinancialSummary> GenerateFinancialSummaryAsync(InventoryContext context, string month)
        {
            // Parse the month to get year and month numbers
            var monthParts = month.Split('-');
            var year = int.Parse(monthParts[0]);
            var monthNum = int.Parse(monthParts[1]);

            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Where(p => p.PurchaseDate.Year == year && p.PurchaseDate.Month == monthNum)
                .ToListAsync();

            var allocations = await context.Allocations
                .Where(a => a.Month == month)
                .ToListAsync();

            var summary = new FinancialSummary
            {
                TotalPurchaseValueUSD = purchases.Sum(p => p.TotalValueUSD),
                TotalConsumptionValueUSD = allocations.Sum(a => a.AllocatedValueUSD),
                RemainingInventoryValueUSD = purchases.Sum(p => (p.RemainingQuantity / p.QuantityLiters) * p.TotalValueUSD)
            };

            // Currency breakdown
            summary.CurrencyBreakdowns = purchases
                .GroupBy(p => new { p.Supplier.Currency, p.Supplier.ExchangeRate })
                .Select(g => new CurrencyBreakdown
                {
                    Currency = g.Key.Currency,
                    TotalValue = g.Sum(p => p.TotalValue),
                    TotalValueUSD = g.Sum(p => p.TotalValueUSD),
                    ExchangeRate = g.Key.ExchangeRate
                })
                .ToList();

            // Payment status
            var today = DateTime.Today;
            var purchasesWithDates = purchases.Where(p => p.DueDate.HasValue).ToList();

            summary.PaymentStatuses = purchasesWithDates
                .GroupBy(p => p.Supplier.Name)
                .Select(g => new PaymentStatus
                {
                    SupplierName = g.Key,
                    TotalDue = g.Sum(p => p.TotalValueUSD),
                    TotalOverdue = g.Where(p => p.DueDate < today).Sum(p => p.TotalValueUSD),
                    OverdueCount = g.Count(p => p.DueDate < today),
                    TotalInvoices = g.Count()
                })
                .ToList();

            // Calculate averages
            var totalQuantityL = purchases.Sum(p => p.QuantityLiters);
            var totalQuantityT = purchases.Sum(p => p.QuantityTons);

            if (totalQuantityL > 0)
                summary.AvgCostPerLiterUSD = summary.TotalPurchaseValueUSD / totalQuantityL;

            if (totalQuantityT > 0)
                summary.AvgCostPerTonUSD = summary.TotalPurchaseValueUSD / totalQuantityT;

            return summary;
        }

        private async Task<ExecutiveSummary> GenerateExecutiveSummaryAsync(InventoryContext context, string month)
        {
            var consumptions = await context.Consumptions
                .Include(c => c.Vessel)
                .Include(c => c.Allocations)
                .Where(c => c.Month == month)
                .ToListAsync();

            // Parse the month to get year and month numbers
            var monthParts = month.Split('-');
            var year = int.Parse(monthParts[0]);
            var monthNum = int.Parse(monthParts[1]);

            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Where(p => p.PurchaseDate.Year == year && p.PurchaseDate.Month == monthNum)
                .ToListAsync();

            var summary = new ExecutiveSummary
            {
                TotalFleetConsumptionL = consumptions.Sum(c => c.ConsumptionLiters),
                TotalLegsCompleted = consumptions.Sum(c => c.LegsCompleted ?? 0),
                TotalOperatingCostUSD = consumptions.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD),
                VesselsOperated = consumptions.Select(c => c.VesselId).Distinct().Count(),
                SuppliersUsed = purchases.Select(p => p.SupplierId).Distinct().Count()
            };

            // Calculate efficiency metrics
            if (summary.TotalLegsCompleted > 0)
            {
                summary.FleetEfficiencyLPerLeg = summary.TotalFleetConsumptionL / summary.TotalLegsCompleted;
                summary.CostPerLeg = summary.TotalOperatingCostUSD / summary.TotalLegsCompleted;
            }

            // Calculate tons with average density
            var avgDensity = purchases.Any() ? purchases.Average(p => p.Density) : 0.85m;
            summary.TotalFleetConsumptionT = (summary.TotalFleetConsumptionL / 1000) * avgDensity;

            if (summary.TotalLegsCompleted > 0)
            {
                summary.FleetEfficiencyTPerLeg = summary.TotalFleetConsumptionT / summary.TotalLegsCompleted;
            }

            // Calculate inventory turnover
            var totalInventoryValue = purchases.Sum(p => p.TotalValueUSD);
            if (totalInventoryValue > 0)
            {
                summary.InventoryTurnover = summary.TotalOperatingCostUSD / totalInventoryValue;
            }

            return summary;
        }

        private async Task<decimal> GetFIFODensityAsync(InventoryContext context, int vesselId)
        {
            var oldestPurchase = await context.Purchases
                .Where(p => p.VesselId == vesselId && p.RemainingQuantity > 0)
                .OrderBy(p => p.PurchaseDate)
                .ThenBy(p => p.Id)
                .FirstOrDefaultAsync();

            return oldestPurchase?.Density ?? 0.85m;
        }

        public async Task<List<string>> GetAvailableMonthsAsync()
        {
            using var context = new InventoryContext();

            // Get all purchases and consumptions, then format dates in memory
            var purchases = await context.Purchases
                .Select(p => p.PurchaseDate)
                .ToListAsync();

            var consumptions = await context.Consumptions
                .Select(c => c.Month)
                .ToListAsync();

            var purchaseMonths = purchases
                .Select(p => p.ToString("yyyy-MM"))
                .Distinct();

            return purchaseMonths
                .Union(consumptions)
                .OrderByDescending(m => m)
                .ToList();
        }
    }
}