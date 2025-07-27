using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class FIFOAllocationDetailService
    {
        #region Data Models

        public class FIFOAllocationDetailResult
        {
            public AllocationFlowSummary FlowSummary { get; set; } = new();
            public List<DetailedAllocationRecord> AllocationRecords { get; set; } = [];
            public List<PurchaseLotTracker> PurchaseLotTracking { get; set; } = [];
            public BalanceVerificationResult BalanceVerification { get; set; } = new();
            public List<PeriodAllocationAnalysis> PeriodAnalysis { get; set; } = [];
            public List<AllocationException> Exceptions { get; set; } = [];
        }

        public class AllocationFlowSummary
        {
            public decimal TotalPurchasesL { get; set; }
            public decimal TotalConsumptionL { get; set; }
            public decimal TotalAllocatedL { get; set; }
            public decimal UnallocatedPurchasesL { get; set; }
            public decimal UnallocatedConsumptionL { get; set; }
            public decimal TotalFIFOValueUSD { get; set; }
            public int TotalAllocationTransactions { get; set; }
            public int UniquePurchaseLots { get; set; }
            public int UniqueConsumptionEntries { get; set; }
            public int VesselsInvolved { get; set; }
            public int SuppliersInvolved { get; set; }
            public DateTime OldestPurchaseDate { get; set; }
            public DateTime LatestConsumptionDate { get; set; }
            public decimal AllocationAccuracyPercentage { get; set; }

            // Formatted properties
            public string FormattedOldestPurchase => OldestPurchaseDate.ToString("dd/MM/yyyy");
            public string FormattedLatestConsumption => LatestConsumptionDate.ToString("dd/MM/yyyy");
            public string FormattedTotalFIFOValue => TotalFIFOValueUSD < 0 ? $"({Math.Abs(TotalFIFOValueUSD):C2})" : TotalFIFOValueUSD.ToString("C2");
            public string FormattedUnallocatedPurchases => UnallocatedPurchasesL < 0 ? $"({Math.Abs(UnallocatedPurchasesL):N3})" : UnallocatedPurchasesL.ToString("N3");
            public string FormattedUnallocatedConsumption => UnallocatedConsumptionL < 0 ? $"({Math.Abs(UnallocatedConsumptionL):N3})" : UnallocatedConsumptionL.ToString("N3");
        }

        public class DetailedAllocationRecord
        {
            public int AllocationId { get; set; }
            public DateTime AllocationDate { get; set; }
            public string Month { get; set; } = string.Empty;

            // Purchase Details
            public int PurchaseId { get; set; }
            public string InvoiceReference { get; set; } = string.Empty;
            public DateTime PurchaseDate { get; set; }
            public string PurchaseVessel { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string SupplierCurrency { get; set; } = string.Empty;
            public decimal PurchasePricePerLiter { get; set; }
            public decimal PurchasePricePerLiterUSD { get; set; }
            public decimal PurchaseDensity { get; set; }

            // Consumption Details
            public int ConsumptionId { get; set; }
            public DateTime ConsumptionDate { get; set; }
            public string ConsumptionVessel { get; set; } = string.Empty;
            public int LegsCompleted { get; set; }
            public decimal ConsumptionTotalL { get; set; }

            // Allocation Details
            public decimal AllocatedQuantityL { get; set; }
            public decimal AllocatedQuantityT { get; set; }
            public decimal AllocatedValueOriginal { get; set; }
            public decimal AllocatedValueUSD { get; set; }
            public decimal PurchaseBalanceAfterL { get; set; }
            public decimal PurchaseBalanceAfterT { get; set; }

            // FIFO Sequence
            public int FIFOSequence { get; set; }
            public bool IsCompleteAllocation { get; set; }
            public decimal AllocationPercentageOfPurchase { get; set; }
            public decimal AllocationPercentageOfConsumption { get; set; }

            // Cross-vessel tracking
            public bool IsCrossVesselAllocation { get; set; }
            public string AllocationNotes { get; set; } = string.Empty;

            // Formatted properties
            public string FormattedAllocationDate => AllocationDate.ToString("dd/MM/yyyy");
            public string FormattedPurchaseDate => PurchaseDate.ToString("dd/MM/yyyy");
            public string FormattedConsumptionDate => ConsumptionDate.ToString("dd/MM/yyyy");
            public string FormattedAllocatedValue => SupplierCurrency == "USD" ? AllocatedValueUSD.ToString("C2") : $"{AllocatedValueOriginal:N3} {SupplierCurrency}";
            public string FormattedPricePerLiter => SupplierCurrency == "USD" ? PurchasePricePerLiterUSD.ToString("C6") : $"{PurchasePricePerLiter:N6} {SupplierCurrency}";
            public string AllocationStatus => IsCompleteAllocation ? "Complete" : "Partial";
            public string VesselMatchStatus => IsCrossVesselAllocation ? "Cross-Vessel" : "Same Vessel";
        }

        public class PurchaseLotTracker
        {
            public int PurchaseId { get; set; }
            public string InvoiceReference { get; set; } = string.Empty;
            public DateTime PurchaseDate { get; set; }
            public string VesselName { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public decimal OriginalQuantityL { get; set; }
            public decimal OriginalQuantityT { get; set; }
            public decimal OriginalValueUSD { get; set; }
            public decimal RemainingQuantityL { get; set; }
            public decimal RemainingQuantityT { get; set; }
            public decimal RemainingValueUSD { get; set; }
            public decimal TotalAllocatedL { get; set; }
            public decimal TotalAllocatedT { get; set; }
            public decimal TotalAllocatedValueUSD { get; set; }
            public int AllocationCount { get; set; }
            public DateTime FirstAllocationDate { get; set; }
            public DateTime LastAllocationDate { get; set; }
            public int DaysInInventory { get; set; }
            public decimal InventoryTurnoverRate { get; set; }
            public string LotStatus { get; set; } = string.Empty; // "Fully Consumed", "Partially Consumed", "Not Consumed"
            public decimal ConsumptionProgress { get; set; } // Percentage consumed

            // Formatted properties
            public string FormattedPurchaseDate => PurchaseDate.ToString("dd/MM/yyyy");
            public string FormattedFirstAllocation => AllocationCount > 0 ? FirstAllocationDate.ToString("dd/MM/yyyy") : "Not Allocated";
            public string FormattedLastAllocation => AllocationCount > 0 ? LastAllocationDate.ToString("dd/MM/yyyy") : "Not Allocated";
            public string FormattedOriginalValue => Currency == "USD" ? OriginalValueUSD.ToString("C2") : $"{OriginalValueUSD:N3} USD";
            public string FormattedRemainingValue => RemainingValueUSD < 0 ? $"({Math.Abs(RemainingValueUSD):C2})" : RemainingValueUSD.ToString("C2");
            public string FormattedConsumptionProgress => $"{ConsumptionProgress:N1}%";
        }

        public class BalanceVerificationResult
        {
            public bool IsBalanced { get; set; }
            public decimal TotalPurchaseQuantity { get; set; }
            public decimal TotalConsumptionQuantity { get; set; }
            public decimal TotalAllocatedQuantity { get; set; }
            public decimal QuantityVariance { get; set; }
            public decimal TotalPurchaseValue { get; set; }
            public decimal TotalConsumptionValue { get; set; }
            public decimal TotalAllocatedValue { get; set; }
            public decimal ValueVariance { get; set; }
            public int InconsistentAllocations { get; set; }
            public int OrphanedPurchases { get; set; }
            public int OrphanedConsumptions { get; set; }
            public int OrphanedAllocations { get; set; }
            public List<string> BalanceIssues { get; set; } = [];
            public decimal DataIntegrityScore { get; set; }

            // Formatted properties
            public string BalanceStatus => IsBalanced ? "✅ Balanced" : "⚠️ Unbalanced";
            public string FormattedQuantityVariance => QuantityVariance < 0 ? $"({Math.Abs(QuantityVariance):N3})" : QuantityVariance.ToString("N3");
            public string FormattedValueVariance => ValueVariance < 0 ? $"({Math.Abs(ValueVariance):C2})" : ValueVariance.ToString("C2");
            public string DataIntegrityGrade => DataIntegrityScore switch
            {
                >= 98 => "Excellent",
                >= 95 => "Good",
                >= 90 => "Fair",
                _ => "Poor"
            };
        }

        public class PeriodAllocationAnalysis
        {
            public string Period { get; set; } = string.Empty; // "2025-01", etc.
            public DateTime PeriodDate { get; set; }
            public decimal PeriodPurchasesL { get; set; }
            public decimal PeriodConsumptionL { get; set; }
            public decimal PeriodAllocationsL { get; set; }
            public decimal PeriodAllocationsValueUSD { get; set; }
            public int AllocationTransactions { get; set; }
            public int PurchaseLotsInvolved { get; set; }
            public int ConsumptionEntriesInvolved { get; set; }
            public decimal AvgAllocationSizeL { get; set; }
            public decimal AvgAllocationValueUSD { get; set; }
            public decimal FIFOVelocity { get; set; } // How quickly inventory turns over
            public decimal CrossVesselAllocationsPercent { get; set; }
            public int VesselsActive { get; set; }
            public int SuppliersActive { get; set; }

            // Comparison with previous period
            public decimal AllocationVolumeChange { get; set; }
            public decimal AllocationValueChange { get; set; }
            public decimal FIFOVelocityChange { get; set; }

            // Formatted properties
            public string FormattedPeriodAllocationsValue => PeriodAllocationsValueUSD < 0 ? $"({Math.Abs(PeriodAllocationsValueUSD):C2})" : PeriodAllocationsValueUSD.ToString("C2");
            public string FormattedAvgAllocationValue => AvgAllocationValueUSD.ToString("C2");
            public string FormattedVolumeChange => AllocationVolumeChange >= 0 ? $"+{AllocationVolumeChange:N1}%" : $"({Math.Abs(AllocationVolumeChange):N1}%)";
            public string FormattedValueChange => AllocationValueChange >= 0 ? $"+{AllocationValueChange:N1}%" : $"({Math.Abs(AllocationValueChange):N1}%)";
            public string FormattedVelocityChange => FIFOVelocityChange >= 0 ? $"+{FIFOVelocityChange:N1}%" : $"({Math.Abs(FIFOVelocityChange):N1}%)";
        }

        public class AllocationException
        {
            public string ExceptionType { get; set; } = string.Empty;
            public string ExceptionLevel { get; set; } = string.Empty; // "Critical", "Warning", "Info"
            public string Description { get; set; } = string.Empty;
            public int? PurchaseId { get; set; }
            public int? ConsumptionId { get; set; }
            public int? AllocationId { get; set; }
            public string InvoiceReference { get; set; } = string.Empty;
            public string VesselName { get; set; } = string.Empty;
            public DateTime? TransactionDate { get; set; }
            public decimal? QuantityAffected { get; set; }
            public decimal? ValueAffected { get; set; }
            public string RecommendedAction { get; set; } = string.Empty;
            public bool IsResolved { get; set; }

            // Formatted properties
            public string FormattedTransactionDate => TransactionDate?.ToString("dd/MM/yyyy") ?? "N/A";
            public string FormattedQuantityAffected => QuantityAffected?.ToString("N3") ?? "N/A";
            public string FormattedValueAffected => ValueAffected?.ToString("C2") ?? "N/A";
            public string ExceptionIcon => ExceptionLevel switch
            {
                "Critical" => "🔴",
                "Warning" => "⚠️",
                "Info" => "ℹ️",
                _ => "❓"
            };
            public string ResolutionStatus => IsResolved ? "✅ Resolved" : "⏳ Pending";
        }

        #endregion

        #region Main Service Method

        public async Task<FIFOAllocationDetailResult> GenerateFIFOAllocationDetailAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var context = new InventoryContext();

            // Default to last 6 months if no dates provided
            var endDate = toDate ?? DateTime.Today;
            var startDate = fromDate ?? endDate.AddMonths(-6);

            // Get ALL purchases within date range (not just those with allocations)
            var allPurchases = await context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Vessel)
                .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate)
                .OrderBy(p => p.PurchaseDate)
                .ToListAsync();

            // Get ALL consumptions within date range
            var allConsumptions = await context.Consumptions
                .Include(c => c.Vessel)
                .Where(c => c.ConsumptionDate >= startDate && c.ConsumptionDate <= endDate)
                .OrderBy(c => c.ConsumptionDate)
                .ToListAsync();

            // Get ALL allocations related to these purchases and consumptions
            var purchaseIds = allPurchases.Select(p => p.Id).ToList();
            var consumptionIds = allConsumptions.Select(c => c.Id).ToList();

            var allocations = await context.Allocations
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Supplier)
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Vessel)
                .Include(a => a.Consumption)
                    .ThenInclude(c => c.Vessel)
                .Where(a => purchaseIds.Contains(a.PurchaseId) || consumptionIds.Contains(a.ConsumptionId))
                .OrderBy(a => a.Purchase.PurchaseDate)
                .ThenBy(a => a.Consumption.ConsumptionDate)
                .ThenBy(a => a.Id)
                .ToListAsync();

            if (!allPurchases.Any() && !allConsumptions.Any())
            {
                return new FIFOAllocationDetailResult(); // Return empty result
            }

            var result = new FIFOAllocationDetailResult
            {
                FlowSummary = GenerateFlowSummaryFixed(allPurchases, allConsumptions, allocations),
                AllocationRecords = GenerateDetailedAllocationRecords(allocations),
                PurchaseLotTracking = GeneratePurchaseLotTrackingFixed(allPurchases, allocations),
                BalanceVerification = VerifyBalancesFixed(allPurchases, allConsumptions, allocations),
                PeriodAnalysis = GeneratePeriodAnalysis(allocations),
                Exceptions = DetectAllocationExceptionsFixed(allPurchases, allConsumptions, allocations)
            };

            return result;
        }

        #endregion

        #region Private Methods - Flow Summary

        private AllocationFlowSummary GenerateFlowSummaryFixed(List<Purchase> allPurchases, List<Consumption> allConsumptions, List<Allocation> allocations)
        {
            var totalPurchasesL = allPurchases.Sum(p => p.QuantityLiters);
            var totalConsumptionL = allConsumptions.Sum(c => c.ConsumptionLiters);
            var totalAllocatedL = allocations.Sum(a => a.AllocatedQuantity);
            var unallocatedPurchasesL = allPurchases.Sum(p => p.RemainingQuantity);
            var unallocatedConsumptionL = totalConsumptionL - totalAllocatedL;

            // Calculate allocation accuracy
            var allocationAccuracy = totalConsumptionL > 0 ? (totalAllocatedL / totalConsumptionL) * 100 : 100;

            return new AllocationFlowSummary
            {
                TotalPurchasesL = totalPurchasesL,
                TotalConsumptionL = totalConsumptionL,
                TotalAllocatedL = totalAllocatedL,
                UnallocatedPurchasesL = unallocatedPurchasesL,
                UnallocatedConsumptionL = unallocatedConsumptionL,
                TotalFIFOValueUSD = allocations.Sum(a => a.AllocatedValueUSD),
                TotalAllocationTransactions = allocations.Count,
                UniquePurchaseLots = allPurchases.Count,
                UniqueConsumptionEntries = allConsumptions.Count,
                VesselsInvolved = allConsumptions.Select(c => c.VesselId).Distinct().Count(),
                SuppliersInvolved = allPurchases.Select(p => p.SupplierId).Distinct().Count(),
                OldestPurchaseDate = allPurchases.Any() ? allPurchases.Min(p => p.PurchaseDate) : DateTime.MinValue,
                LatestConsumptionDate = allConsumptions.Any() ? allConsumptions.Max(c => c.ConsumptionDate) : DateTime.MinValue,
                AllocationAccuracyPercentage = allocationAccuracy
            };
        }

        #endregion

        #region Private Methods - Detailed Allocation Records

        private List<DetailedAllocationRecord> GenerateDetailedAllocationRecords(List<Allocation> allocations)
        {
            var records = new List<DetailedAllocationRecord>();
            var fifoSequence = 1;

            foreach (var allocation in allocations)
            {
                var purchase = allocation.Purchase;
                var consumption = allocation.Consumption;
                var allocatedTons = allocation.AllocatedQuantity > 0 && purchase.QuantityLiters > 0
                    ? (allocation.AllocatedQuantity / 1000) * purchase.Density
                    : 0;

                var record = new DetailedAllocationRecord
                {
                    AllocationId = allocation.Id,
                    AllocationDate = allocation.CreatedDate,
                    Month = allocation.Month,

                    // Purchase details
                    PurchaseId = purchase.Id,
                    InvoiceReference = purchase.InvoiceReference,
                    PurchaseDate = purchase.PurchaseDate,
                    PurchaseVessel = purchase.Vessel.Name,
                    SupplierName = purchase.Supplier.Name,
                    SupplierCurrency = purchase.Supplier.Currency,
                    PurchasePricePerLiter = purchase.PricePerLiter,
                    PurchasePricePerLiterUSD = purchase.PricePerLiterUSD,
                    PurchaseDensity = purchase.Density,

                    // Consumption details
                    ConsumptionId = consumption.Id,
                    ConsumptionDate = consumption.ConsumptionDate,
                    ConsumptionVessel = consumption.Vessel.Name,
                    LegsCompleted = consumption.LegsCompleted,
                    ConsumptionTotalL = consumption.ConsumptionLiters,

                    // Allocation details
                    AllocatedQuantityL = allocation.AllocatedQuantity,
                    AllocatedQuantityT = allocatedTons,
                    AllocatedValueOriginal = allocation.AllocatedValue,
                    AllocatedValueUSD = allocation.AllocatedValueUSD,
                    PurchaseBalanceAfterL = allocation.PurchaseBalanceAfter,
                    PurchaseBalanceAfterT = allocation.PurchaseBalanceAfter > 0 ? (allocation.PurchaseBalanceAfter / 1000) * purchase.Density : 0,

                    // FIFO tracking
                    FIFOSequence = fifoSequence++,
                    IsCompleteAllocation = allocation.PurchaseBalanceAfter == 0,
                    AllocationPercentageOfPurchase = purchase.QuantityLiters > 0 ? (allocation.AllocatedQuantity / purchase.QuantityLiters) * 100 : 0,
                    AllocationPercentageOfConsumption = consumption.ConsumptionLiters > 0 ? (allocation.AllocatedQuantity / consumption.ConsumptionLiters) * 100 : 0,

                    // Cross-vessel tracking
                    IsCrossVesselAllocation = purchase.VesselId != consumption.VesselId,
                    AllocationNotes = purchase.VesselId != consumption.VesselId
                        ? $"Cross-vessel: {purchase.Vessel.Name} → {consumption.Vessel.Name}"
                        : "Same vessel allocation"
                };

                records.Add(record);
            }

            return records;
        }

        #endregion

        #region Private Methods - Purchase Lot Tracking

        private List<PurchaseLotTracker> GeneratePurchaseLotTrackingFixed(List<Purchase> allPurchases, List<Allocation> allocations)
        {
            var purchaseTrackers = new List<PurchaseLotTracker>();

            foreach (var purchase in allPurchases)
            {
                var purchaseAllocations = allocations.Where(a => a.PurchaseId == purchase.Id).OrderBy(a => a.Consumption.ConsumptionDate).ToList();
                var totalAllocatedL = purchaseAllocations.Sum(a => a.AllocatedQuantity);
                var totalAllocatedValueUSD = purchaseAllocations.Sum(a => a.AllocatedValueUSD);
                var totalAllocatedT = totalAllocatedL > 0 ? (totalAllocatedL / 1000) * purchase.Density : 0;

                var consumptionProgress = purchase.QuantityLiters > 0 ? (totalAllocatedL / purchase.QuantityLiters) * 100 : 0;
                var lotStatus = consumptionProgress >= 99.9m ? "Fully Consumed" :
                               consumptionProgress > 0 ? "Partially Consumed" : "Not Consumed";

                var daysInInventory = (DateTime.Today - purchase.PurchaseDate).Days;
                var inventoryTurnover = daysInInventory > 0 ? (consumptionProgress / 100) / daysInInventory * 365 : 0;

                purchaseTrackers.Add(new PurchaseLotTracker
                {
                    PurchaseId = purchase.Id,
                    InvoiceReference = purchase.InvoiceReference,
                    PurchaseDate = purchase.PurchaseDate,
                    VesselName = purchase.Vessel.Name,
                    SupplierName = purchase.Supplier.Name,
                    Currency = purchase.Supplier.Currency,
                    OriginalQuantityL = purchase.QuantityLiters,
                    OriginalQuantityT = purchase.QuantityTons,
                    OriginalValueUSD = purchase.TotalValueUSD,
                    RemainingQuantityL = purchase.RemainingQuantity,
                    RemainingQuantityT = purchase.RemainingQuantity > 0 ? (purchase.RemainingQuantity / 1000) * purchase.Density : 0,
                    RemainingValueUSD = purchase.RemainingQuantity > 0 ? (purchase.RemainingQuantity / purchase.QuantityLiters) * purchase.TotalValueUSD : 0,
                    TotalAllocatedL = totalAllocatedL,
                    TotalAllocatedT = totalAllocatedT,
                    TotalAllocatedValueUSD = totalAllocatedValueUSD,
                    AllocationCount = purchaseAllocations.Count,
                    FirstAllocationDate = purchaseAllocations.Any() ? purchaseAllocations.Min(a => a.Consumption.ConsumptionDate) : DateTime.MinValue,
                    LastAllocationDate = purchaseAllocations.Any() ? purchaseAllocations.Max(a => a.Consumption.ConsumptionDate) : DateTime.MinValue,
                    DaysInInventory = daysInInventory,
                    InventoryTurnoverRate = inventoryTurnover,
                    LotStatus = lotStatus,
                    ConsumptionProgress = consumptionProgress
                });
            }

            return purchaseTrackers.OrderBy(p => p.PurchaseDate).ToList();
        }

        #endregion

        #region Private Methods - Balance Verification

        private BalanceVerificationResult VerifyBalancesFixed(List<Purchase> allPurchases, List<Consumption> allConsumptions, List<Allocation> allocations)
        {
            var totalPurchaseQuantity = allPurchases.Sum(p => p.QuantityLiters);
            var totalConsumptionQuantity = allConsumptions.Sum(c => c.ConsumptionLiters);
            var totalAllocatedQuantity = allocations.Sum(a => a.AllocatedQuantity);

            var totalPurchaseValue = allPurchases.Sum(p => p.TotalValueUSD);
            var totalConsumptionValue = allocations.Sum(a => a.AllocatedValueUSD);
            var totalAllocatedValue = allocations.Sum(a => a.AllocatedValueUSD);

            var quantityVariance = totalPurchaseQuantity - totalConsumptionQuantity;
            var valueVariance = totalPurchaseValue - totalConsumptionValue;

            var balanceIssues = new List<string>();
            var inconsistentAllocations = 0;

            // Check current remaining inventory vs calculated variance
            var currentRemainingInventory = allPurchases.Sum(p => p.RemainingQuantity);
            if (Math.Abs(quantityVariance - currentRemainingInventory) > 0.001m)
                balanceIssues.Add($"Calculated variance ({quantityVariance:N3}L) doesn't match remaining inventory ({currentRemainingInventory:N3}L)");

            // Check for allocation consistency
            foreach (var allocation in allocations)
            {
                var expectedValue = allocation.AllocatedQuantity * allocation.Purchase.PricePerLiterUSD;
                if (Math.Abs(allocation.AllocatedValueUSD - expectedValue) > 0.01m)
                    inconsistentAllocations++;
            }

            if (inconsistentAllocations > 0)
                balanceIssues.Add($"{inconsistentAllocations} allocations with value inconsistencies");

            // Check if all consumption is allocated
            if (Math.Abs(totalConsumptionQuantity - totalAllocatedQuantity) > 0.001m)
                balanceIssues.Add($"Unallocated consumption: {totalConsumptionQuantity - totalAllocatedQuantity:N3}L");

            var dataIntegrityScore = 100m - (balanceIssues.Count * 5) - (inconsistentAllocations * 0.1m);
            dataIntegrityScore = Math.Max(0, Math.Min(100, dataIntegrityScore));

            return new BalanceVerificationResult
            {
                IsBalanced = balanceIssues.Count == 0,
                TotalPurchaseQuantity = totalPurchaseQuantity,
                TotalConsumptionQuantity = totalConsumptionQuantity,
                TotalAllocatedQuantity = totalAllocatedQuantity,
                QuantityVariance = quantityVariance,
                TotalPurchaseValue = totalPurchaseValue,
                TotalConsumptionValue = totalConsumptionValue,
                TotalAllocatedValue = totalAllocatedValue,
                ValueVariance = valueVariance,
                InconsistentAllocations = inconsistentAllocations,
                OrphanedPurchases = 0, // Would need separate query
                OrphanedConsumptions = 0, // Would need separate query
                OrphanedAllocations = 0, // Would need separate query
                BalanceIssues = balanceIssues,
                DataIntegrityScore = dataIntegrityScore
            };
        }

        #endregion

        #region Private Methods - Period Analysis

        private List<PeriodAllocationAnalysis> GeneratePeriodAnalysis(List<Allocation> allocations)
        {
            var periodData = allocations
                .GroupBy(a => a.Month)
                .Select(g =>
                {
                    var periodAllocations = g.ToList();
                    var purchases = periodAllocations.Select(a => a.Purchase).Distinct().ToList();
                    var consumptions = periodAllocations.Select(a => a.Consumption).Distinct().ToList();

                    var periodPurchasesL = purchases.Sum(p => p.QuantityLiters);
                    var periodConsumptionL = consumptions.Sum(c => c.ConsumptionLiters);
                    var periodAllocationsL = periodAllocations.Sum(a => a.AllocatedQuantity);
                    var periodAllocationsValueUSD = periodAllocations.Sum(a => a.AllocatedValueUSD);

                    var crossVesselAllocations = periodAllocations.Count(a => a.Purchase.VesselId != a.Consumption.VesselId);
                    var crossVesselPercent = periodAllocations.Count > 0 ? (decimal)crossVesselAllocations / periodAllocations.Count * 100 : 0;

                    var fifoVelocity = periodPurchasesL > 0 ? periodAllocationsL / periodPurchasesL : 0;

                    return new PeriodAllocationAnalysis
                    {
                        Period = g.Key,
                        PeriodDate = DateTime.ParseExact(g.Key, "yyyy-MM", null),
                        PeriodPurchasesL = periodPurchasesL,
                        PeriodConsumptionL = periodConsumptionL,
                        PeriodAllocationsL = periodAllocationsL,
                        PeriodAllocationsValueUSD = periodAllocationsValueUSD,
                        AllocationTransactions = periodAllocations.Count,
                        PurchaseLotsInvolved = purchases.Count,
                        ConsumptionEntriesInvolved = consumptions.Count,
                        AvgAllocationSizeL = periodAllocations.Count > 0 ? periodAllocationsL / periodAllocations.Count : 0,
                        AvgAllocationValueUSD = periodAllocations.Count > 0 ? periodAllocationsValueUSD / periodAllocations.Count : 0,
                        FIFOVelocity = fifoVelocity,
                        CrossVesselAllocationsPercent = crossVesselPercent,
                        VesselsActive = consumptions.Select(c => c.VesselId).Distinct().Count(),
                        SuppliersActive = purchases.Select(p => p.SupplierId).Distinct().Count(),
                        AllocationVolumeChange = 0, // Will be calculated below
                        AllocationValueChange = 0,
                        FIFOVelocityChange = 0
                    };
                })
                .OrderBy(p => p.PeriodDate)
                .ToList();

            // Calculate period-over-period changes
            for (int i = 1; i < periodData.Count; i++)
            {
                var current = periodData[i];
                var previous = periodData[i - 1];

                if (previous.PeriodAllocationsL > 0)
                    current.AllocationVolumeChange = ((current.PeriodAllocationsL - previous.PeriodAllocationsL) / previous.PeriodAllocationsL) * 100;

                if (previous.PeriodAllocationsValueUSD > 0)
                    current.AllocationValueChange = ((current.PeriodAllocationsValueUSD - previous.PeriodAllocationsValueUSD) / previous.PeriodAllocationsValueUSD) * 100;

                if (previous.FIFOVelocity > 0)
                    current.FIFOVelocityChange = ((current.FIFOVelocity - previous.FIFOVelocity) / previous.FIFOVelocity) * 100;
            }

            return periodData;
        }

        #endregion

        #region Private Methods - Exception Detection

        private List<AllocationException> DetectAllocationExceptions(List<Allocation> allocations)
        {
            var exceptions = new List<AllocationException>();

            // Check for chronological FIFO violations
            var purchaseGroups = allocations.GroupBy(a => a.PurchaseId);
            foreach (var group in purchaseGroups)
            {
                var sortedAllocations = group.OrderBy(a => a.Consumption.ConsumptionDate).ToList();
                for (int i = 1; i < sortedAllocations.Count; i++)
                {
                    if (sortedAllocations[i].Consumption.ConsumptionDate < sortedAllocations[i - 1].Consumption.ConsumptionDate)
                    {
                        exceptions.Add(new AllocationException
                        {
                            ExceptionType = "FIFO Chronology Violation",
                            ExceptionLevel = "Warning",
                            Description = "Allocation does not follow chronological FIFO order",
                            PurchaseId = group.Key,
                            AllocationId = sortedAllocations[i].Id,
                            InvoiceReference = sortedAllocations[i].Purchase.InvoiceReference,
                            VesselName = sortedAllocations[i].Consumption.Vessel.Name,
                            TransactionDate = sortedAllocations[i].Consumption.ConsumptionDate,
                            QuantityAffected = sortedAllocations[i].AllocatedQuantity,
                            ValueAffected = sortedAllocations[i].AllocatedValueUSD,
                            RecommendedAction = "Review allocation sequence and re-run FIFO if necessary",
                            IsResolved = false
                        });
                    }
                }
            }

            // Check for negative balances
            foreach (var allocation in allocations)
            {
                if (allocation.PurchaseBalanceAfter < 0)
                {
                    exceptions.Add(new AllocationException
                    {
                        ExceptionType = "Negative Balance",
                        ExceptionLevel = "Critical",
                        Description = "Purchase balance after allocation is negative",
                        PurchaseId = allocation.PurchaseId,
                        AllocationId = allocation.Id,
                        InvoiceReference = allocation.Purchase.InvoiceReference,
                        VesselName = allocation.Purchase.Vessel.Name,
                        TransactionDate = allocation.Purchase.PurchaseDate,
                        QuantityAffected = Math.Abs(allocation.PurchaseBalanceAfter),
                        ValueAffected = allocation.AllocatedValueUSD,
                        RecommendedAction = "Investigate over-allocation and correct purchase quantities",
                        IsResolved = false
                    });
                }
            }

            // Check for value calculation inconsistencies
            foreach (var allocation in allocations)
            {
                var expectedValue = allocation.AllocatedQuantity * allocation.Purchase.PricePerLiterUSD;
                if (Math.Abs(allocation.AllocatedValueUSD - expectedValue) > 0.10m) // Tolerance of 10 cents
                {
                    exceptions.Add(new AllocationException
                    {
                        ExceptionType = "Value Calculation Error",
                        ExceptionLevel = "Warning",
                        Description = $"Allocated value ({allocation.AllocatedValueUSD:C2}) doesn't match expected ({expectedValue:C2})",
                        PurchaseId = allocation.PurchaseId,
                        ConsumptionId = allocation.ConsumptionId,
                        AllocationId = allocation.Id,
                        InvoiceReference = allocation.Purchase.InvoiceReference,
                        VesselName = allocation.Consumption.Vessel.Name,
                        TransactionDate = allocation.Consumption.ConsumptionDate,
                        QuantityAffected = allocation.AllocatedQuantity,
                        ValueAffected = Math.Abs(allocation.AllocatedValueUSD - expectedValue),
                        RecommendedAction = "Recalculate allocation values using current price data",
                        IsResolved = false
                    });
                }
            }

            return exceptions.OrderBy(e => e.ExceptionLevel).ThenBy(e => e.TransactionDate).ToList();
        }

        private List<AllocationException> DetectAllocationExceptionsFixed(List<Purchase> allPurchases, List<Consumption> allConsumptions, List<Allocation> allocations)
        {
            // Use the existing DetectAllocationExceptions method but with the comprehensive data
            return DetectAllocationExceptions(allocations);
        }

        #endregion
    }
}