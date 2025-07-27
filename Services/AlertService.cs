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
                    .Where(p => p.DueDate.HasValue && p.InvoiceReceiptDate.HasValue)
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
    }
}