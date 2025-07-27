using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class ReportService
    {
        #region Data Models

        public class VesselAccountStatementResult
        {
            public VesselAccountSummary Summary { get; set; } = new();
            public List<VesselAccountTransaction> Transactions { get; set; } = new();

            // Calculated totals for the total row
            public decimal TotalDebits => Transactions.Sum(t => t.DebitQuantity);
            public decimal TotalCredits => Transactions.Sum(t => t.CreditQuantity);
            public decimal NetBalance => TotalDebits - TotalCredits;
            public decimal TotalValue => Transactions.Sum(t => t.ValueUSD);

            // Formatted totals with brackets for negatives
            public string FormattedTotalDebits => $"{TotalDebits:N3}";
            public string FormattedTotalCredits => $"{TotalCredits:N3}";
            public string FormattedNetBalance
            {
                get
                {
                    if (NetBalance < 0)
                        return $"({Math.Abs(NetBalance):N3})";
                    else
                        return NetBalance.ToString("N3");
                }
            }
            public string FormattedTotalValue
            {
                get
                {
                    if (TotalValue < 0)
                        return $"({Math.Abs(TotalValue):C2})";
                    else
                        return TotalValue.ToString("C2");
                }
            }
        }

        public class VesselAccountSummary
        {
            public string VesselName { get; set; } = string.Empty;
            public decimal TotalPurchases { get; set; }
            public decimal TotalConsumption { get; set; }
            public decimal CurrentBalance { get; set; }
            public decimal TotalValue { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            // Formatted properties for display with brackets for negatives
            public string FormattedCurrentBalance
            {
                get
                {
                    if (CurrentBalance < 0)
                        return $"({Math.Abs(CurrentBalance):N3}) L";
                    else
                        return $"{CurrentBalance:N3} L";
                }
            }

            public string FormattedTotalValue
            {
                get
                {
                    if (TotalValue < 0)
                        return $"({Math.Abs(TotalValue):C2})";
                    else
                        return TotalValue.ToString("C2");
                }
            }
        }

        public class VesselAccountTransaction
        {
            public DateTime TransactionDate { get; set; }
            public string TransactionType { get; set; } = string.Empty; // "Purchase" or "Consumption"
            public string Reference { get; set; } = string.Empty; // Invoice ref or consumption ID
            public string SupplierName { get; set; } = string.Empty; // For purchases
            public decimal DebitQuantity { get; set; } // Purchases (incoming fuel)
            public decimal CreditQuantity { get; set; } // Consumption (outgoing fuel)
            public decimal RunningBalance { get; set; }
            public decimal ValueUSD { get; set; }
            public string Description { get; set; } = string.Empty;

            // Formatted properties for display with brackets for negatives
            public string FormattedValueUSD
            {
                get
                {
                    if (ValueUSD < 0)
                        return $"({Math.Abs(ValueUSD):C2})";
                    else
                        return ValueUSD.ToString("C2");
                }
            }

            public string FormattedRunningBalance
            {
                get
                {
                    if (RunningBalance < 0)
                        return $"({Math.Abs(RunningBalance):N3})";
                    else
                        return RunningBalance.ToString("N3");
                }
            }
        }

        public class SupplierAccountReportResult
        {
            public SupplierAccountSummary Summary { get; set; } = new();
            public List<SupplierAccountTransaction> Transactions { get; set; } = new();

            // Calculated totals for the total row
            public decimal TotalPurchases => Transactions.Sum(t => t.PurchaseQuantity);
            public decimal TotalConsumption => Transactions.Sum(t => t.ConsumptionQuantity);
            public decimal NetBalance => TotalPurchases - TotalConsumption;
            public decimal TotalValue => Transactions.Sum(t => t.Value);
            public decimal TotalValueUSD => Transactions.Sum(t => t.ValueUSD);

            // Formatted totals with brackets for negatives
            public string FormattedTotalPurchases => $"{TotalPurchases:N3}";
            public string FormattedTotalConsumption => $"{TotalConsumption:N3}";
            public string FormattedNetBalance
            {
                get
                {
                    if (NetBalance < 0)
                        return $"({Math.Abs(NetBalance):N3})";
                    else
                        return NetBalance.ToString("N3");
                }
            }
            public string FormattedTotalValue
            {
                get
                {
                    var currency = Transactions.FirstOrDefault()?.Currency ?? "USD";
                    if (TotalValue < 0)
                        return $"({Math.Abs(TotalValue):N3}) {currency}";
                    else
                        return $"{TotalValue:N3} {currency}";
                }
            }
            public string FormattedTotalValueUSD
            {
                get
                {
                    if (TotalValueUSD < 0)
                        return $"({Math.Abs(TotalValueUSD):C2})";
                    else
                        return TotalValueUSD.ToString("C2");
                }
            }
        }

        public class SupplierAccountSummary
        {
            public string SupplierName { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public decimal BeginningBalance { get; set; }
            public decimal PeriodPurchases { get; set; }
            public decimal PeriodConsumption { get; set; }
            public decimal EndingBalance { get; set; }
            public DateTime FromDate { get; set; }
            public DateTime ToDate { get; set; }

            // Formatted properties for display with brackets for negatives
            public string FormattedBeginningBalance
            {
                get
                {
                    if (BeginningBalance < 0)
                        return $"({Math.Abs(BeginningBalance):N3}) L";
                    else
                        return $"{BeginningBalance:N3} L";
                }
            }

            public string FormattedEndingBalance
            {
                get
                {
                    if (EndingBalance < 0)
                        return $"({Math.Abs(EndingBalance):N3}) L";
                    else
                        return $"{EndingBalance:N3} L";
                }
            }
        }

        public class SupplierAccountTransaction
        {
            public DateTime TransactionDate { get; set; }
            public string VesselName { get; set; } = string.Empty;
            public string TransactionType { get; set; } = string.Empty; // "Purchase" or "Consumption"
            public string Reference { get; set; } = string.Empty;
            public decimal PurchaseQuantity { get; set; }
            public decimal ConsumptionQuantity { get; set; }
            public decimal RunningBalance { get; set; }
            public decimal Value { get; set; } // In supplier currency
            public string Currency { get; set; } = string.Empty;
            public decimal ValueUSD { get; set; }

            // Formatted properties for display with brackets for negatives
            public string FormattedValue
            {
                get
                {
                    if (Value < 0)
                        return $"({Math.Abs(Value):N3}) {Currency}";
                    else
                        return $"{Value:N3} {Currency}";
                }
            }

            public string FormattedValueUSD
            {
                get
                {
                    if (ValueUSD < 0)
                        return $"({Math.Abs(ValueUSD):C2})";
                    else
                        return ValueUSD.ToString("C2");
                }
            }

            public string FormattedRunningBalance
            {
                get
                {
                    if (RunningBalance < 0)
                        return $"({Math.Abs(RunningBalance):N3})";
                    else
                        return RunningBalance.ToString("N3");
                }
            }
        }

        #endregion

        #region Vessel Account Statement

        public async Task<VesselAccountStatementResult> GenerateVesselAccountStatementAsync(int vesselId, DateTime fromDate, DateTime toDate)
        {
            using var context = new InventoryContext();

            var vessel = await context.Vessels.FindAsync(vesselId);
            if (vessel == null)
                throw new ArgumentException("Vessel not found");

            var result = new VesselAccountStatementResult();
            var transactions = new List<VesselAccountTransaction>();

            // Get all purchases for this vessel in date range
            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Where(p => p.VesselId == vesselId && p.PurchaseDate >= fromDate && p.PurchaseDate <= toDate)
                .OrderBy(p => p.PurchaseDate)
                .ThenBy(p => p.Id)
                .ToListAsync();

            // Get all consumptions for this vessel in date range
            var consumptions = await context.Consumptions
                .Include(c => c.Allocations)
                    .ThenInclude(a => a.Purchase)
                        .ThenInclude(p => p.Supplier)
                .Where(c => c.VesselId == vesselId && c.ConsumptionDate >= fromDate && c.ConsumptionDate <= toDate)
                .OrderBy(c => c.ConsumptionDate)
                .ThenBy(c => c.Id)
                .ToListAsync();

            // Calculate beginning balance using ToListAsync to avoid SQLite SumAsync issues
            var beginningPurchasesList = await context.Purchases
                .Where(p => p.VesselId == vesselId && p.PurchaseDate < fromDate)
                .Select(p => p.QuantityLiters)
                .ToListAsync();

            var beginningConsumptionsList = await context.Consumptions
                .Where(c => c.VesselId == vesselId && c.ConsumptionDate < fromDate)
                .Select(c => c.ConsumptionLiters)
                .ToListAsync();

            var beginningPurchases = beginningPurchasesList.Sum();
            var beginningConsumptions = beginningConsumptionsList.Sum();
            var runningBalance = beginningPurchases - beginningConsumptions;

            // Add beginning balance entry if there's activity before the period
            if (beginningPurchases > 0 || beginningConsumptions > 0)
            {
                transactions.Add(new VesselAccountTransaction
                {
                    TransactionDate = fromDate.AddDays(-1),
                    TransactionType = "Beginning Balance",
                    Reference = "OPENING",
                    SupplierName = "",
                    DebitQuantity = 0,
                    CreditQuantity = 0,
                    RunningBalance = runningBalance,
                    ValueUSD = 0,
                    Description = "Opening balance from previous periods"
                });
            }

            // Create combined list of all transactions
            var allTransactions = new List<(DateTime Date, string Type, object Data)>();

            // Add purchases
            foreach (var purchase in purchases)
            {
                allTransactions.Add((purchase.PurchaseDate, "Purchase", purchase));
            }

            // Add consumptions
            foreach (var consumption in consumptions)
            {
                allTransactions.Add((consumption.ConsumptionDate, "Consumption", consumption));
            }

            // Sort by date and process
            foreach (var transaction in allTransactions.OrderBy(t => t.Date).ThenBy(t => t.Type == "Purchase" ? 0 : 1))
            {
                if (transaction.Type == "Purchase" && transaction.Data is Purchase purchase)
                {
                    runningBalance += purchase.QuantityLiters;
                    transactions.Add(new VesselAccountTransaction
                    {
                        TransactionDate = purchase.PurchaseDate,
                        TransactionType = "Purchase",
                        Reference = purchase.InvoiceReference,
                        SupplierName = purchase.Supplier.Name,
                        DebitQuantity = purchase.QuantityLiters,
                        CreditQuantity = 0,
                        RunningBalance = runningBalance,
                        ValueUSD = purchase.TotalValueUSD,
                        Description = $"Fuel purchase from {purchase.Supplier.Name}"
                    });
                }
                else if (transaction.Type == "Consumption" && transaction.Data is Consumption consumption)
                {
                    runningBalance -= consumption.ConsumptionLiters;
                    var totalAllocatedValue = consumption.Allocations.Sum(a => a.AllocatedValueUSD);

                    transactions.Add(new VesselAccountTransaction
                    {
                        TransactionDate = consumption.ConsumptionDate,
                        TransactionType = "Consumption",
                        Reference = $"CONS-{consumption.Id:000}",
                        SupplierName = string.Join(", ", consumption.Allocations.Select(a => a.Purchase.Supplier.Name).Distinct()),
                        DebitQuantity = 0,
                        CreditQuantity = consumption.ConsumptionLiters,
                        RunningBalance = runningBalance,
                        ValueUSD = -totalAllocatedValue, // Negative for consumption
                        Description = $"Fuel consumption - {consumption.LegsCompleted} legs completed"
                    });
                }
            }

            // Calculate summary
            result.Summary = new VesselAccountSummary
            {
                VesselName = vessel.Name,
                TotalPurchases = purchases.Sum(p => p.QuantityLiters),
                TotalConsumption = consumptions.Sum(c => c.ConsumptionLiters),
                CurrentBalance = runningBalance,
                TotalValue = purchases.Sum(p => p.TotalValueUSD),
                FromDate = fromDate,
                ToDate = toDate
            };

            result.Transactions = transactions;
            return result;
        }

        #endregion

        #region Supplier Account Report

        public async Task<SupplierAccountReportResult> GenerateSupplierAccountReportAsync(int supplierId, DateTime fromDate, DateTime toDate)
        {
            using var context = new InventoryContext();

            var supplier = await context.Suppliers.FindAsync(supplierId);
            if (supplier == null)
                throw new ArgumentException("Supplier not found");

            var result = new SupplierAccountReportResult();
            var transactions = new List<SupplierAccountTransaction>();

            // Get all purchases from this supplier in date range
            var purchases = await context.Purchases
                .Include(p => p.Vessel)
                .Where(p => p.SupplierId == supplierId && p.PurchaseDate >= fromDate && p.PurchaseDate <= toDate)
                .OrderBy(p => p.PurchaseDate)
                .ThenBy(p => p.Id)
                .ToListAsync();

            // Get all allocations for this supplier's fuel in date range
            var allocations = await context.Allocations
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Vessel)
                .Include(a => a.Consumption)
                    .ThenInclude(c => c.Vessel)
                .Where(a => a.Purchase.SupplierId == supplierId && a.Consumption.ConsumptionDate >= fromDate && a.Consumption.ConsumptionDate <= toDate)
                .OrderBy(a => a.Consumption.ConsumptionDate)
                .ThenBy(a => a.Id)
                .ToListAsync();

            // Calculate beginning balance using ToListAsync to avoid SQLite SumAsync issues
            var beginningPurchasesList = await context.Purchases
                .Where(p => p.SupplierId == supplierId && p.PurchaseDate < fromDate)
                .Select(p => p.QuantityLiters)
                .ToListAsync();

            var beginningAllocationsList = await context.Allocations
                .Include(a => a.Purchase)
                .Include(a => a.Consumption)
                .Where(a => a.Purchase.SupplierId == supplierId && a.Consumption.ConsumptionDate < fromDate)
                .Select(a => a.AllocatedQuantity)
                .ToListAsync();

            var beginningPurchases = beginningPurchasesList.Sum();
            var beginningConsumptions = beginningAllocationsList.Sum();
            var runningBalance = beginningPurchases - beginningConsumptions;

            // Add beginning balance entry if there's activity before the period
            if (beginningPurchases > 0 || beginningConsumptions > 0)
            {
                transactions.Add(new SupplierAccountTransaction
                {
                    TransactionDate = fromDate.AddDays(-1),
                    VesselName = "",
                    TransactionType = "Beginning Balance",
                    Reference = "OPENING",
                    PurchaseQuantity = 0,
                    ConsumptionQuantity = 0,
                    RunningBalance = runningBalance,
                    Value = 0,
                    Currency = supplier.Currency,
                    ValueUSD = 0
                });
            }

            // Create combined list of all transactions
            var allTransactions = new List<(DateTime Date, string Type, object Data)>();

            // Add purchases
            foreach (var purchase in purchases)
            {
                allTransactions.Add((purchase.PurchaseDate, "Purchase", purchase));
            }

            // Add consumptions (via allocations)
            foreach (var allocation in allocations)
            {
                allTransactions.Add((allocation.Consumption.ConsumptionDate, "Consumption", allocation));
            }

            // Sort by date and process
            foreach (var transaction in allTransactions.OrderBy(t => t.Date).ThenBy(t => t.Type == "Purchase" ? 0 : 1))
            {
                if (transaction.Type == "Purchase" && transaction.Data is Purchase purchase)
                {
                    runningBalance += purchase.QuantityLiters;
                    transactions.Add(new SupplierAccountTransaction
                    {
                        TransactionDate = purchase.PurchaseDate,
                        VesselName = purchase.Vessel.Name,
                        TransactionType = "Purchase",
                        Reference = purchase.InvoiceReference,
                        PurchaseQuantity = purchase.QuantityLiters,
                        ConsumptionQuantity = 0,
                        RunningBalance = runningBalance,
                        Value = purchase.TotalValue,
                        Currency = supplier.Currency,
                        ValueUSD = purchase.TotalValueUSD
                    });
                }
                else if (transaction.Type == "Consumption" && transaction.Data is Allocation allocation)
                {
                    runningBalance -= allocation.AllocatedQuantity;
                    transactions.Add(new SupplierAccountTransaction
                    {
                        TransactionDate = allocation.Consumption.ConsumptionDate,
                        VesselName = allocation.Consumption.Vessel.Name,
                        TransactionType = "Consumption",
                        Reference = $"CONS-{allocation.ConsumptionId:000}",
                        PurchaseQuantity = 0,
                        ConsumptionQuantity = allocation.AllocatedQuantity,
                        RunningBalance = runningBalance,
                        Value = allocation.AllocatedValue,
                        Currency = supplier.Currency,
                        ValueUSD = allocation.AllocatedValueUSD
                    });
                }
            }

            // Calculate summary
            result.Summary = new SupplierAccountSummary
            {
                SupplierName = supplier.Name,
                Currency = supplier.Currency,
                BeginningBalance = beginningPurchases - beginningConsumptions,
                PeriodPurchases = purchases.Sum(p => p.QuantityLiters),
                PeriodConsumption = allocations.Sum(a => a.AllocatedQuantity),
                EndingBalance = runningBalance,
                FromDate = fromDate,
                ToDate = toDate
            };

            result.Transactions = transactions;
            return result;
        }

        #endregion
    }
}