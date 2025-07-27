using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class AlertService
    {
        public class DueDateAlert
        {
            public int PurchaseId { get; set; }
            public string InvoiceReference { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string VesselName { get; set; } = string.Empty;
            public DateTime InvoiceReceiptDate { get; set; }
            public DateTime DueDate { get; set; }
            public decimal TotalValueUSD { get; set; }
            public string Currency { get; set; } = string.Empty;
            public decimal TotalValue { get; set; }
            public int DaysUntilDue { get; set; }
            public DueDateAlertLevel AlertLevel { get; set; }

            public string FormattedDueDate => DueDate.ToString("dd/MM/yyyy");
            public string FormattedReceiptDate => InvoiceReceiptDate.ToString("dd/MM/yyyy");
            public string FormattedValue => Currency == "USD" ? TotalValueUSD.ToString("C2") : $"{TotalValue:N3} {Currency}";

            public string AlertMessage => AlertLevel switch
            {
                DueDateAlertLevel.DueToday => "⚠️ DUE TODAY!",
                DueDateAlertLevel.Overdue => $"🔴 OVERDUE by {Math.Abs(DaysUntilDue)} days!",
                DueDateAlertLevel.DueTomorrow => "⏰ Due Tomorrow",
                DueDateAlertLevel.AlertDay => $"📋 Due in {DaysUntilDue} days (Alert Day)",
                _ => ""
            };
        }

        public class PaymentSummary
        {
            public decimal TotalOverdueAmount { get; set; }
            public int OverdueCount { get; set; }
            public decimal DueTodayAmount { get; set; }
            public int DueTodayCount { get; set; }
            public decimal DueThisWeekAmount { get; set; }
            public int DueThisWeekCount { get; set; }
            public decimal DueNextWeekAmount { get; set; }
            public int DueNextWeekCount { get; set; }
            public decimal TotalOutstandingAmount { get; set; }
            public int TotalOutstandingCount { get; set; }
        }

        public class PaymentAgingItem
        {
            public int PurchaseId { get; set; }
            public string InvoiceReference { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string VesselName { get; set; } = string.Empty;
            public DateTime InvoiceReceiptDate { get; set; }
            public DateTime DueDate { get; set; }
            public decimal TotalValueUSD { get; set; }
            public string Currency { get; set; } = string.Empty;
            public decimal TotalValue { get; set; }
            public int DaysOverdue { get; set; }
            public string AgingCategory { get; set; } = string.Empty; // "Current", "1-30 Days", "31-60 Days", "61-90 Days", "90+ Days"
            public PaymentStatus PaymentStatus { get; set; }

            public string FormattedDueDate => DueDate.ToString("dd/MM/yyyy");
            public string FormattedReceiptDate => InvoiceReceiptDate.ToString("dd/MM/yyyy");
            public string FormattedValue => Currency == "USD" ? TotalValueUSD.ToString("C2") : $"{TotalValue:N3} {Currency}";
            public string StatusText => PaymentStatus switch
            {
                PaymentStatus.Overdue => $"OVERDUE {Math.Abs(DaysOverdue)} days",
                PaymentStatus.DueToday => "DUE TODAY",
                PaymentStatus.DueTomorrow => "Due Tomorrow", 
                PaymentStatus.DueThisWeek => $"Due in {DaysOverdue} days",
                PaymentStatus.DueNextWeek => $"Due in {DaysOverdue} days",
                _ => "Current"
            };
        }

        public class PaidInvoiceItem
        {
            public int PurchaseId { get; set; }
            public string InvoiceReference { get; set; } = string.Empty;
            public string SupplierName { get; set; } = string.Empty;
            public string VesselName { get; set; } = string.Empty;
            public DateTime PaymentDate { get; set; }
            public DateTime InvoiceReceiptDate { get; set; }
            public DateTime DueDate { get; set; }
            public decimal TotalValueUSD { get; set; }
            public string Currency { get; set; } = string.Empty;
            public decimal TotalValue { get; set; }
            public decimal PaymentAmount { get; set; }
            public decimal PaymentAmountUSD { get; set; }

            public string FormattedPaymentDate => PaymentDate.ToString("dd/MM/yyyy");
            public string FormattedDueDate => DueDate.ToString("dd/MM/yyyy");
            public string FormattedReceiptDate => InvoiceReceiptDate.ToString("dd/MM/yyyy");
            public string FormattedValue => Currency == "USD" ? TotalValueUSD.ToString("C2") : $"{TotalValue:N3} {Currency}";
            public string FormattedPaymentAmount => Currency == "USD" ? PaymentAmountUSD.ToString("C2") : $"{PaymentAmount:N3} {Currency}";
        }

        public enum PaymentStatus
        {
            Current,
            DueNextWeek,
            DueThisWeek, 
            DueTomorrow,
            DueToday,
            Overdue
        }

        public class SupplierPaymentSummary
        {
            public string SupplierName { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public decimal TotalOutstanding { get; set; }
            public decimal TotalOverdue { get; set; }
            public int TotalInvoices { get; set; }
            public int OverdueInvoices { get; set; }
            public decimal AvgDaysOverdue { get; set; }
            public string FormattedOutstanding => Currency == "USD" ? TotalOutstanding.ToString("C2") : $"{TotalOutstanding:N3} {Currency}";
            public string FormattedOverdue => Currency == "USD" ? TotalOverdue.ToString("C2") : $"{TotalOverdue:N3} {Currency}";
        }

        // Add these new methods to AlertService class:

        public async Task<PaymentSummary> GetPaymentSummaryAsync()
        {
            try
            {
                using var context = new InventoryContext();
                var today = DateTime.Today;
                var endOfWeek = today.AddDays(7 - (int)today.DayOfWeek); // End of current week (Saturday)
                var endOfNextWeek = endOfWeek.AddDays(7); // End of next week

                var purchasesWithDueDates = await context.Purchases
                    .Include(p => p.Supplier)
                    .Include(p => p.Vessel)
                    .Where(p => p.DueDate.HasValue && p.InvoiceReceiptDate.HasValue && !p.PaymentDate.HasValue)
                    .ToListAsync();

                var summary = new PaymentSummary();

                foreach (var purchase in purchasesWithDueDates)
                {
                    var dueDate = purchase.DueDate!.Value.Date;
                    var daysUntilDue = (dueDate - today).Days;

                    summary.TotalOutstandingAmount += purchase.TotalValueUSD;
                    summary.TotalOutstandingCount++;

                    if (daysUntilDue < 0) // Overdue
                    {
                        summary.TotalOverdueAmount += purchase.TotalValueUSD;
                        summary.OverdueCount++;
                    }
                    else if (daysUntilDue == 0) // Due today
                    {
                        summary.DueTodayAmount += purchase.TotalValueUSD;
                        summary.DueTodayCount++;
                    }
                    else if (dueDate <= endOfWeek) // Due this week
                    {
                        summary.DueThisWeekAmount += purchase.TotalValueUSD;
                        summary.DueThisWeekCount++;
                    }
                    else if (dueDate <= endOfNextWeek) // Due next week
                    {
                        summary.DueNextWeekAmount += purchase.TotalValueUSD;
                        summary.DueNextWeekCount++;
                    }
                }

                return summary;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting payment summary: {ex.Message}");
                return new PaymentSummary();
            }
        }

        public async Task<List<PaymentAgingItem>> GetPaymentAgingAnalysisAsync()
        {
            try
            {
                using var context = new InventoryContext();
                var today = DateTime.Today;

                var purchasesWithDueDates = await context.Purchases
                    .Include(p => p.Supplier)
                    .Include(p => p.Vessel)
                    .Where(p => p.DueDate.HasValue && p.InvoiceReceiptDate.HasValue && !p.PaymentDate.HasValue)
                    .ToListAsync();

                var agingItems = new List<PaymentAgingItem>();

                foreach (var purchase in purchasesWithDueDates)
                {
                    var dueDate = purchase.DueDate!.Value.Date;
                    var daysOverdue = (today - dueDate).Days;
                    var daysUntilDue = (dueDate - today).Days;

                    var agingCategory = daysOverdue switch
                    {
                        <= 0 => "Current",
                        <= 30 => "1-30 Days",
                        <= 60 => "31-60 Days", 
                        <= 90 => "61-90 Days",
                        _ => "90+ Days"
                    };

                    var paymentStatus = daysUntilDue switch
                    {
                        < 0 => PaymentStatus.Overdue,
                        0 => PaymentStatus.DueToday,
                        1 => PaymentStatus.DueTomorrow,
                        <= 7 => PaymentStatus.DueThisWeek,
                        <= 14 => PaymentStatus.DueNextWeek,
                        _ => PaymentStatus.Current
                    };

                    agingItems.Add(new PaymentAgingItem
                    {
                        PurchaseId = purchase.Id,
                        InvoiceReference = purchase.InvoiceReference,
                        SupplierName = purchase.Supplier.Name,
                        VesselName = purchase.Vessel.Name,
                        InvoiceReceiptDate = purchase.InvoiceReceiptDate!.Value,
                        DueDate = dueDate,
                        TotalValueUSD = purchase.TotalValueUSD,
                        Currency = purchase.Supplier.Currency,
                        TotalValue = purchase.TotalValue,
                        DaysOverdue = Math.Max(0, daysOverdue),
                        AgingCategory = agingCategory,
                        PaymentStatus = paymentStatus
                    });
                }

                return agingItems.OrderBy(a => a.DueDate).ThenBy(a => a.SupplierName).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting payment aging analysis: {ex.Message}");
                return [];
            }
        }

        public async Task<List<SupplierPaymentSummary>> GetSupplierPaymentSummaryAsync()
        {
            try
            {
                using var context = new InventoryContext();
                var today = DateTime.Today;

                var purchasesWithDueDates = await context.Purchases
                    .Include(p => p.Supplier)
                    .Where(p => p.DueDate.HasValue && p.InvoiceReceiptDate.HasValue && !p.PaymentDate.HasValue)
                    .ToListAsync();

                var supplierSummaries = purchasesWithDueDates
                    .GroupBy(p => new { p.SupplierId, p.Supplier.Name, p.Supplier.Currency })
                    .Select(g =>
                    {
                        var overduePurchases = g.Where(p => p.DueDate!.Value.Date < today).ToList();
                        var avgDaysOverdue = overduePurchases.Any()
                            ? (decimal)overduePurchases.Average(p => (today - p.DueDate!.Value.Date).Days)
                            : 0m;

                        return new SupplierPaymentSummary
                        {
                            SupplierName = g.Key.Name,
                            Currency = g.Key.Currency,
                            TotalOutstanding = g.Sum(p => p.TotalValue),
                            TotalOverdue = overduePurchases.Sum(p => p.TotalValue),
                            TotalInvoices = g.Count(),
                            OverdueInvoices = overduePurchases.Count,
                            AvgDaysOverdue = avgDaysOverdue
                        };
                    })
                    .OrderByDescending(s => s.TotalOverdue)
                    .ThenByDescending(s => s.TotalOutstanding)
                    .ToList();

                return supplierSummaries;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting supplier payment summary: {ex.Message}");
                return [];
            }
        }

        public enum DueDateAlertLevel
        {
            None,
            AlertDay,      // 1 business day before (accounting for weekends)
            DueTomorrow,   // Due tomorrow (actual tomorrow)
            DueToday,      // Due today
            Overdue        // Past due date
        }

        public async Task<List<DueDateAlert>> GetDueDateAlertsAsync()
        {
            try
            {
                using var context = new InventoryContext();

                var today = DateTime.Today;

                // Get purchases with due dates
                var purchasesWithDueDates = await context.Purchases
                    .Include(p => p.Supplier)
                    .Include(p => p.Vessel)
                    .Where(p => p.DueDate.HasValue && p.InvoiceReceiptDate.HasValue && !p.PaymentDate.HasValue)
                    .ToListAsync();

                var alerts = new List<DueDateAlert>();

                foreach (var purchase in purchasesWithDueDates)
                {
                    var dueDate = purchase.DueDate!.Value.Date;
                    var daysUntilDue = (dueDate - today).Days;
                    var alertLevel = GetAlertLevel(today, dueDate);

                    // Only include alerts that need attention
                    if (alertLevel != DueDateAlertLevel.None)
                    {
                        alerts.Add(new DueDateAlert
                        {
                            PurchaseId = purchase.Id,
                            InvoiceReference = purchase.InvoiceReference,
                            SupplierName = purchase.Supplier.Name,
                            VesselName = purchase.Vessel.Name,
                            InvoiceReceiptDate = purchase.InvoiceReceiptDate!.Value,
                            DueDate = dueDate,
                            TotalValueUSD = purchase.TotalValueUSD,
                            Currency = purchase.Supplier.Currency,
                            TotalValue = purchase.TotalValue,
                            DaysUntilDue = daysUntilDue,
                            AlertLevel = alertLevel
                        });
                    }
                }

                // Sort by priority: Overdue, Due Today, Due Tomorrow, Alert Day
                return alerts.OrderBy(a => (int)a.AlertLevel).ThenBy(a => a.DueDate).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting due date alerts: {ex.Message}");
                return [];
            }
        }

        private DueDateAlertLevel GetAlertLevel(DateTime today, DateTime dueDate)
        {
            var daysUntilDue = (dueDate - today).Days;

            // Overdue
            if (daysUntilDue < 0)
                return DueDateAlertLevel.Overdue;

            // Due today
            if (daysUntilDue == 0)
                return DueDateAlertLevel.DueToday;

            // Due tomorrow
            if (daysUntilDue == 1)
                return DueDateAlertLevel.DueTomorrow;

            // Alert day logic (1 business day before, accounting for Fri-Sat weekends)
            var alertDate = GetBusinessDayBefore(dueDate);
            if (today.Date == alertDate.Date)
                return DueDateAlertLevel.AlertDay;

            // Special case: If due date is on weekend (Fri/Sat), treat as due on next Sunday
            if (dueDate.DayOfWeek == DayOfWeek.Friday || dueDate.DayOfWeek == DayOfWeek.Saturday)
            {
                // Find next Sunday after the weekend due date
                var nextSunday = dueDate;
                while (nextSunday.DayOfWeek != DayOfWeek.Sunday)
                {
                    nextSunday = nextSunday.AddDays(1);
                }

                // Check if we should alert for the effective Sunday due date
                var effectiveAlertDate = GetBusinessDayBefore(nextSunday);
                if (today.Date == effectiveAlertDate.Date)
                    return DueDateAlertLevel.AlertDay;
            }

            return DueDateAlertLevel.None;
        }

        private DateTime GetBusinessDayBefore(DateTime dueDate)
        {
            // Get 1 business day before due date, skipping weekends (Friday-Saturday)
            var alertDate = dueDate.AddDays(-1);

            // If alert date falls on weekend, move to Thursday (last working day)
            if (alertDate.DayOfWeek == DayOfWeek.Friday)
                alertDate = alertDate.AddDays(-1); // Move to Thursday
            else if (alertDate.DayOfWeek == DayOfWeek.Saturday)
                alertDate = alertDate.AddDays(-2); // Move to Thursday

            return alertDate;
        }

        public async Task<int> GetAlertCountAsync()
        {
            var alerts = await GetDueDateAlertsAsync();
            return alerts.Count;
        }

        public async Task<bool> HasCriticalAlertsAsync()
        {
            var alerts = await GetDueDateAlertsAsync();
            return alerts.Any(a => a.AlertLevel == DueDateAlertLevel.Overdue ||
                                 a.AlertLevel == DueDateAlertLevel.DueToday);
        }

        public string GetAlertSummary(List<DueDateAlert> alerts)
        {
            if (!alerts.Any())
                return "No payment alerts";

            var overdue = alerts.Count(a => a.AlertLevel == DueDateAlertLevel.Overdue);
            var dueToday = alerts.Count(a => a.AlertLevel == DueDateAlertLevel.DueToday);
            var dueTomorrow = alerts.Count(a => a.AlertLevel == DueDateAlertLevel.DueTomorrow);
            var alertDay = alerts.Count(a => a.AlertLevel == DueDateAlertLevel.AlertDay);

            var parts = new List<string>();

            if (overdue > 0)
                parts.Add($"{overdue} overdue");
            if (dueToday > 0)
                parts.Add($"{dueToday} due today");
            if (dueTomorrow > 0)
                parts.Add($"{dueTomorrow} due tomorrow");
            if (alertDay > 0)
                parts.Add($"{alertDay} due soon");

            return string.Join(", ", parts);
        }

        public async Task<List<PaidInvoiceItem>> GetPaidInvoicesAsync()
        {
            try
            {
                using var context = new InventoryContext();

                var paidPurchases = await context.Purchases
                    .Include(p => p.Supplier)
                    .Include(p => p.Vessel)
                    .Where(p => p.PaymentDate.HasValue)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                var paidItems = new List<PaidInvoiceItem>();

                foreach (var purchase in paidPurchases)
                {
                    paidItems.Add(new PaidInvoiceItem
                    {
                        PurchaseId = purchase.Id,
                        InvoiceReference = purchase.InvoiceReference,
                        SupplierName = purchase.Supplier.Name,
                        VesselName = purchase.Vessel.Name,
                        PaymentDate = purchase.PaymentDate!.Value,
                        InvoiceReceiptDate = purchase.InvoiceReceiptDate ?? DateTime.MinValue,
                        DueDate = purchase.DueDate ?? DateTime.MinValue,
                        TotalValueUSD = purchase.TotalValueUSD,
                        Currency = purchase.Supplier.Currency,
                        TotalValue = purchase.TotalValue,
                        PaymentAmount = purchase.PaymentAmount ?? purchase.TotalValue,
                        PaymentAmountUSD = purchase.PaymentAmountUSD ?? purchase.TotalValueUSD
                    });
                }

                return paidItems;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting paid invoices: {ex.Message}");
                return [];
            }
        }
    }
}