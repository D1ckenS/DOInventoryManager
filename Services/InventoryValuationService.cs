using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class InventoryValuationService
    {
        #region Data Models

        public class InventoryValuationSummary
        {
            public decimal TotalInventoryLiters { get; set; }
            public decimal TotalInventoryTons { get; set; }
            public decimal TotalFIFOValueUSD { get; set; }
            public int NumberOfPurchaseLots { get; set; }
            public int VesselsWithInventory { get; set; }
            public int SuppliersWithInventory { get; set; }
            public decimal AvgCostPerLiterUSD { get; set; }
            public decimal AvgCostPerTonUSD { get; set; }
        }

        public class VesselInventoryItem
        {
            public string VesselName { get; set; } = string.Empty;
            public string VesselType { get; set; } = string.Empty;
            public string Route { get; set; } = string.Empty;
            public decimal CurrentInventoryL { get; set; }
            public decimal CurrentInventoryT { get; set; }
            public decimal FIFOValueUSD { get; set; }
            public decimal AvgCostPerLiterUSD { get; set; }
            public decimal AvgCostPerTonUSD { get; set; }
            public int PurchaseLotsCount { get; set; }
            public DateTime OldestPurchaseDate { get; set; }
            public DateTime NewestPurchaseDate { get; set; }
            public int DaysOldestInventory { get; set; }

            public string FormattedOldestDate => OldestPurchaseDate.ToString("dd/MM/yyyy");
            public string FormattedNewestDate => NewestPurchaseDate.ToString("dd/MM/yyyy");
        }

        public class SupplierInventoryItem
        {
            public string SupplierName { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public decimal RemainingInventoryL { get; set; }
            public decimal RemainingInventoryT { get; set; }
            public decimal FIFOValueOriginal { get; set; }
            public decimal FIFOValueUSD { get; set; }
            public decimal AvgCostPerLiterOriginal { get; set; }
            public decimal AvgCostPerLiterUSD { get; set; }
            public int PurchaseLotsCount { get; set; }
            public int VesselsSupplied { get; set; }
            public DateTime OldestPurchaseDate { get; set; }
            public DateTime NewestPurchaseDate { get; set; }

            public string FormattedFIFOValueOriginal => Currency == "USD" ? FIFOValueUSD.ToString("C2") : $"{FIFOValueOriginal:N3} {Currency}";
            public string FormattedAvgCostOriginal => Currency == "USD" ? AvgCostPerLiterUSD.ToString("C6") : $"{AvgCostPerLiterOriginal:N6} {Currency}";
        }

        public class PurchaseLotItem
        {
            public int PurchaseId { get; set; }
            public DateTime PurchaseDate { get; set; }
            public string InvoiceReference { get; set; } = string.Empty;
            public string VesselName { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public decimal OriginalQuantityL { get; set; }
            public decimal RemainingQuantityL { get; set; }
            public decimal RemainingQuantityT { get; set; }
            public decimal RemainingValueOriginal { get; set; }
            public decimal RemainingValueUSD { get; set; }
            public decimal CostPerLiterOriginal { get; set; }
            public decimal CostPerLiterUSD { get; set; }
            public int DaysInInventory { get; set; }
            public decimal PercentageRemaining { get; set; }

            public string FormattedPurchaseDate => PurchaseDate.ToString("dd/MM/yyyy");
            public string FormattedRemainingValue => Currency == "USD" ? RemainingValueUSD.ToString("C2") : $"{RemainingValueOriginal:N3} {Currency}";
            public string FormattedCostPerLiter => Currency == "USD" ? CostPerLiterUSD.ToString("C6") : $"{CostPerLiterOriginal:N6} {Currency}";
            public string AgingCategory => DaysInInventory switch
            {
                <= 30 => "Fresh (≤30 days)",
                <= 60 => "Current (31-60 days)",
                <= 90 => "Aging (61-90 days)",
                _ => "Old (90+ days)"
            };
        }

        public class InventoryValuationResult
        {
            public InventoryValuationSummary Summary { get; set; } = new();
            public List<VesselInventoryItem> VesselInventory { get; set; } = [];
            public List<SupplierInventoryItem> SupplierInventory { get; set; } = [];
            public List<PurchaseLotItem> PurchaseLots { get; set; } = [];
        }

        #endregion

        public async Task<InventoryValuationResult> GenerateInventoryValuationAsync()
        {
            using var context = new InventoryContext();

            var result = new InventoryValuationResult();

            // Get all purchases with remaining inventory
            var activePurchases = await context.Purchases
                .Include(p => p.Vessel)
                .Include(p => p.Supplier)
                .Where(p => p.RemainingQuantity > 0)
                .OrderBy(p => p.PurchaseDate)
                .ToListAsync();

            if (!activePurchases.Any())
            {
                return result; // Return empty result if no inventory
            }

            // Generate all reports
            result.Summary = GenerateSummary(activePurchases);
            result.VesselInventory = GenerateVesselInventory(activePurchases);
            result.SupplierInventory = GenerateSupplierInventory(activePurchases);
            result.PurchaseLots = GeneratePurchaseLots(activePurchases);

            return result;
        }

        private InventoryValuationSummary GenerateSummary(List<Purchase> purchases)
        {
            var today = DateTime.Today;
            var totalInventoryLiters = purchases.Sum(p => p.RemainingQuantity);
            var totalInventoryTons = purchases.Sum(p => p.RemainingQuantityTons);
            var totalFIFOValue = purchases.Sum(p => (p.RemainingQuantity / p.QuantityLiters) * p.TotalValueUSD);

            return new InventoryValuationSummary
            {
                TotalInventoryLiters = totalInventoryLiters,
                TotalInventoryTons = totalInventoryTons,
                TotalFIFOValueUSD = totalFIFOValue,
                NumberOfPurchaseLots = purchases.Count,
                VesselsWithInventory = purchases.Select(p => p.VesselId).Distinct().Count(),
                SuppliersWithInventory = purchases.Select(p => p.SupplierId).Distinct().Count(),
                AvgCostPerLiterUSD = totalInventoryLiters > 0 ? totalFIFOValue / totalInventoryLiters : 0m,
                AvgCostPerTonUSD = totalInventoryTons > 0 ? totalFIFOValue / totalInventoryTons : 0m
            };
        }

        private List<VesselInventoryItem> GenerateVesselInventory(List<Purchase> purchases)
        {
            var today = DateTime.Today;

            return purchases
                .GroupBy(p => new { p.VesselId, p.Vessel.Name, p.Vessel.Type, p.Vessel.Route })
                .Select(g =>
                {
                    var totalInventoryL = g.Sum(p => p.RemainingQuantity);
                    var totalInventoryT = g.Sum(p => p.RemainingQuantityTons);
                    var totalFIFOValue = g.Sum(p => (p.RemainingQuantity / p.QuantityLiters) * p.TotalValueUSD);

                    return new VesselInventoryItem
                    {
                        VesselName = g.Key.Name,
                        VesselType = g.Key.Type,
                        Route = g.Key.Route,
                        CurrentInventoryL = totalInventoryL,
                        CurrentInventoryT = totalInventoryT,
                        FIFOValueUSD = totalFIFOValue,
                        PurchaseLotsCount = g.Count(),
                        OldestPurchaseDate = g.Min(p => p.PurchaseDate),
                        NewestPurchaseDate = g.Max(p => p.PurchaseDate),
                        DaysOldestInventory = (today - g.Min(p => p.PurchaseDate)).Days,
                        AvgCostPerLiterUSD = totalInventoryL > 0 ? totalFIFOValue / totalInventoryL : 0m,
                        AvgCostPerTonUSD = totalInventoryT > 0 ? totalFIFOValue / totalInventoryT : 0m
                    };
                })
                .OrderByDescending(v => v.FIFOValueUSD)
                .ToList();
        }

        private List<SupplierInventoryItem> GenerateSupplierInventory(List<Purchase> purchases)
        {
            return purchases
                .GroupBy(p => new { p.SupplierId, p.Supplier.Name, p.Supplier.Currency })
                .Select(g =>
                {
                    var totalInventoryL = g.Sum(p => p.RemainingQuantity);
                    var totalInventoryT = g.Sum(p => p.RemainingQuantityTons);
                    var totalFIFOValueOriginal = g.Sum(p => (p.RemainingQuantity / p.QuantityLiters) * p.TotalValue);
                    var totalFIFOValueUSD = g.Sum(p => (p.RemainingQuantity / p.QuantityLiters) * p.TotalValueUSD);

                    return new SupplierInventoryItem
                    {
                        SupplierName = g.Key.Name,
                        Currency = g.Key.Currency,
                        RemainingInventoryL = totalInventoryL,
                        RemainingInventoryT = totalInventoryT,
                        FIFOValueOriginal = totalFIFOValueOriginal,
                        FIFOValueUSD = totalFIFOValueUSD,
                        AvgCostPerLiterOriginal = totalInventoryL > 0 ? totalFIFOValueOriginal / totalInventoryL : 0m,
                        AvgCostPerLiterUSD = totalInventoryL > 0 ? totalFIFOValueUSD / totalInventoryL : 0m,
                        PurchaseLotsCount = g.Count(),
                        VesselsSupplied = g.Select(p => p.VesselId).Distinct().Count(),
                        OldestPurchaseDate = g.Min(p => p.PurchaseDate),
                        NewestPurchaseDate = g.Max(p => p.PurchaseDate)
                    };
                })
                .OrderByDescending(s => s.FIFOValueUSD)
                .ToList();
        }

        private List<PurchaseLotItem> GeneratePurchaseLots(List<Purchase> purchases)
        {
            var today = DateTime.Today;

            return purchases
                .Select(p => new PurchaseLotItem
                {
                    PurchaseId = p.Id,
                    PurchaseDate = p.PurchaseDate,
                    InvoiceReference = p.InvoiceReference,
                    VesselName = p.Vessel.Name,
                    SupplierName = p.Supplier.Name,
                    Currency = p.Supplier.Currency,
                    OriginalQuantityL = p.QuantityLiters,
                    RemainingQuantityL = p.RemainingQuantity,
                    RemainingQuantityT = p.RemainingQuantityTons,
                    RemainingValueOriginal = (p.RemainingQuantity / p.QuantityLiters) * p.TotalValue,
                    RemainingValueUSD = (p.RemainingQuantity / p.QuantityLiters) * p.TotalValueUSD,
                    CostPerLiterOriginal = p.PricePerLiter,
                    CostPerLiterUSD = p.PricePerLiterUSD,
                    DaysInInventory = (today - p.PurchaseDate).Days,
                    PercentageRemaining = (p.RemainingQuantity / p.QuantityLiters) * 100
                })
                .OrderBy(p => p.PurchaseDate)
                .ToList();
        }
    }
}