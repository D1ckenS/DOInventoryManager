using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class FleetEfficiencyService
    {
        #region Data Models

        public class FleetEfficiencyResult
        {
            public FleetOverview Overview { get; set; } = new();
            public List<VesselEfficiencyDetail> VesselEfficiency { get; set; } = [];
            public List<RouteEfficiencyComparison> RouteComparison { get; set; } = [];
            public List<MonthlyEfficiencyTrend> MonthlyTrends { get; set; } = [];
            public List<VesselRanking> EfficiencyRankings { get; set; } = [];
            public List<CostEfficiencyDetail> CostEfficiency { get; set; } = [];
            public List<SeasonalEfficiencyPattern> SeasonalPatterns { get; set; } = [];
        }

        public class FleetOverview
        {
            public int TotalActiveVessels { get; set; }
            public int TotalVesselRoutes { get; set; }
            public int TotalBoatRoutes { get; set; }
            public decimal TotalFleetConsumptionL { get; set; }
            public decimal TotalFleetConsumptionT { get; set; }
            public int TotalLegsCompleted { get; set; }
            public decimal AvgFleetEfficiencyLPerLeg { get; set; }
            public decimal AvgFleetEfficiencyTPerLeg { get; set; }
            public decimal TotalFleetCostUSD { get; set; }
            public decimal AvgCostPerLegUSD { get; set; }
            public decimal AvgCostPerLiterUSD { get; set; }
            public string MostEfficientVessel { get; set; } = string.Empty;
            public string LeastEfficientVessel { get; set; } = string.Empty;
            public string BestRoute { get; set; } = string.Empty;
            public decimal BestRouteEfficiency { get; set; }
        }

        public class VesselEfficiencyDetail
        {
            public string VesselName { get; set; } = string.Empty;
            public string VesselType { get; set; } = string.Empty;
            public string Route { get; set; } = string.Empty;
            public decimal TotalConsumptionL { get; set; }
            public decimal TotalConsumptionT { get; set; }
            public int TotalLegs { get; set; }
            public decimal EfficiencyLPerLeg { get; set; }
            public decimal EfficiencyTPerLeg { get; set; }
            public decimal TotalCostUSD { get; set; }
            public decimal CostPerLegUSD { get; set; }
            public decimal CostPerLiterUSD { get; set; }
            public decimal CostPerTonUSD { get; set; }
            public int MonthsActive { get; set; }
            public int ConsumptionEntries { get; set; }
            public DateTime FirstConsumption { get; set; }
            public DateTime LastConsumption { get; set; }
            public decimal EfficiencyRank { get; set; }
            public string EfficiencyGrade { get; set; } = string.Empty;
            public decimal EfficiencyImprovement { get; set; } // vs previous period

            // Formatted properties
            public string FormattedCostPerLeg => CostPerLegUSD < 0 ? $"({Math.Abs(CostPerLegUSD):C2})" : CostPerLegUSD.ToString("C2");
            public string FormattedTotalCost => TotalCostUSD < 0 ? $"({Math.Abs(TotalCostUSD):C2})" : TotalCostUSD.ToString("C2");
            public string FormattedFirstConsumption => FirstConsumption.ToString("dd/MM/yyyy");
            public string FormattedLastConsumption => LastConsumption.ToString("dd/MM/yyyy");
            public string FormattedImprovement => EfficiencyImprovement >= 0 ? $"+{EfficiencyImprovement:N1}%" : $"({Math.Abs(EfficiencyImprovement):N1}%)";
        }

        public class RouteEfficiencyComparison
        {
            public string RouteType { get; set; } = string.Empty; // "Vessel Route" or "Boat Route"
            public string RouteName { get; set; } = string.Empty;
            public int VesselCount { get; set; }
            public decimal AvgEfficiencyLPerLeg { get; set; }
            public decimal AvgEfficiencyTPerLeg { get; set; }
            public decimal AvgCostPerLegUSD { get; set; }
            public decimal AvgCostPerLiterUSD { get; set; }
            public decimal TotalConsumptionL { get; set; }
            public decimal TotalConsumptionT { get; set; }
            public int TotalLegs { get; set; }
            public decimal TotalCostUSD { get; set; }
            public decimal BestVesselEfficiencyL { get; set; }
            public decimal WorstVesselEfficiencyL { get; set; }
            public string BestVesselName { get; set; } = string.Empty;
            public string WorstVesselName { get; set; } = string.Empty;
            public decimal EfficiencyVariance { get; set; }

            // Formatted properties
            public string FormattedTotalCost => TotalCostUSD < 0 ? $"({Math.Abs(TotalCostUSD):C2})" : TotalCostUSD.ToString("C2");
            public string FormattedAvgCostPerLeg => AvgCostPerLegUSD < 0 ? $"({Math.Abs(AvgCostPerLegUSD):C2})" : AvgCostPerLegUSD.ToString("C2");
        }

        public class MonthlyEfficiencyTrend
        {
            public string Month { get; set; } = string.Empty;
            public DateTime MonthDate { get; set; }
            public decimal FleetAvgEfficiencyL { get; set; }
            public decimal FleetAvgEfficiencyT { get; set; }
            public decimal FleetAvgCostPerLeg { get; set; }
            public decimal TotalConsumptionL { get; set; }
            public decimal TotalConsumptionT { get; set; }
            public int TotalLegs { get; set; }
            public int ActiveVessels { get; set; }
            public decimal TotalCostUSD { get; set; }
            public decimal MonthOverMonthChangeL { get; set; }
            public decimal MonthOverMonthChangeT { get; set; }
            public decimal CostEfficiencyChange { get; set; }

            // Formatted properties
            public string FormattedTotalCost => TotalCostUSD < 0 ? $"({Math.Abs(TotalCostUSD):C2})" : TotalCostUSD.ToString("C2");
            public string FormattedFleetCostPerLeg => FleetAvgCostPerLeg < 0 ? $"({Math.Abs(FleetAvgCostPerLeg):C2})" : FleetAvgCostPerLeg.ToString("C2");
            public string FormattedMoMChangeL => MonthOverMonthChangeL >= 0 ? $"+{MonthOverMonthChangeL:N1}%" : $"({Math.Abs(MonthOverMonthChangeL):N1}%)";
            public string FormattedMoMChangeT => MonthOverMonthChangeT >= 0 ? $"+{MonthOverMonthChangeT:N1}%" : $"({Math.Abs(MonthOverMonthChangeT):N1}%)";
            public string FormattedCostChange => CostEfficiencyChange >= 0 ? $"+{CostEfficiencyChange:N1}%" : $"({Math.Abs(CostEfficiencyChange):N1}%)";
        }

        public class VesselRanking
        {
            public int Rank { get; set; }
            public string VesselName { get; set; } = string.Empty;
            public string VesselType { get; set; } = string.Empty;
            public string Route { get; set; } = string.Empty;
            public decimal EfficiencyLPerLeg { get; set; }
            public decimal EfficiencyTPerLeg { get; set; }
            public decimal CostPerLegUSD { get; set; }
            public decimal OverallEfficiencyScore { get; set; }
            public string PerformanceCategory { get; set; } = string.Empty; // "Excellent", "Good", "Average", "Poor"
            public decimal BenchmarkDifference { get; set; } // vs fleet average
            public string TrendDirection { get; set; } = string.Empty; // "Improving", "Stable", "Declining"

            // Formatted properties
            public string FormattedCostPerLeg => CostPerLegUSD < 0 ? $"({Math.Abs(CostPerLegUSD):C2})" : CostPerLegUSD.ToString("C2");
            public string FormattedBenchmarkDiff => BenchmarkDifference >= 0 ? $"+{BenchmarkDifference:N1}%" : $"({Math.Abs(BenchmarkDifference):N1}%)";
        }

        public class CostEfficiencyDetail
        {
            public string VesselName { get; set; } = string.Empty;
            public string VesselType { get; set; } = string.Empty;
            public decimal TotalFIFOCostUSD { get; set; }
            public decimal CostPerLiterFIFO { get; set; }
            public decimal CostPerTonFIFO { get; set; }
            public decimal CostPerLegFIFO { get; set; }
            public decimal AvgFuelPricePaid { get; set; }
            public decimal CostEfficiencyRank { get; set; }
            public decimal ProcurementEfficiency { get; set; } // vs fleet avg fuel price
            public decimal OperationalEfficiency { get; set; } // fuel consumption efficiency
            public decimal OverallCostScore { get; set; }
            public int NumberOfSuppliers { get; set; }
            public decimal FuelInventoryTurnover { get; set; }

            // Formatted properties
            public string FormattedTotalFIFOCost => TotalFIFOCostUSD < 0 ? $"({Math.Abs(TotalFIFOCostUSD):C2})" : TotalFIFOCostUSD.ToString("C2");
            public string FormattedCostPerLeg => CostPerLegFIFO < 0 ? $"({Math.Abs(CostPerLegFIFO):C2})" : CostPerLegFIFO.ToString("C2");
            public string FormattedAvgFuelPrice => AvgFuelPricePaid.ToString("C6");
            public string FormattedProcurementEff => ProcurementEfficiency >= 0 ? $"+{ProcurementEfficiency:N1}%" : $"({Math.Abs(ProcurementEfficiency):N1}%)";
        }

        public class SeasonalEfficiencyPattern
        {
            public string Season { get; set; } = string.Empty; // Q1, Q2, Q3, Q4
            public string SeasonName { get; set; } = string.Empty; // "Winter", "Spring", etc.
            public decimal AvgEfficiencyL { get; set; }
            public decimal AvgEfficiencyT { get; set; }
            public decimal AvgCostPerLeg { get; set; }
            public decimal SeasonalVariance { get; set; }
            public int DataPoints { get; set; } // number of months/entries
            public decimal YearOverYearChange { get; set; }
            public string WeatherImpact { get; set; } = string.Empty;
            public decimal CapacityUtilization { get; set; }

            // Formatted properties
            public string FormattedAvgCostPerLeg => AvgCostPerLeg < 0 ? $"({Math.Abs(AvgCostPerLeg):C2})" : AvgCostPerLeg.ToString("C2");
            public string FormattedYoYChange => YearOverYearChange >= 0 ? $"+{YearOverYearChange:N1}%" : $"({Math.Abs(YearOverYearChange):N1}%)";
        }

        #endregion

        #region Helper Methods

        private decimal CalculateTotalConsumptionTons(List<Consumption> consumptions, List<Allocation> allocations)
        {
            decimal totalTons = 0;

            foreach (var consumption in consumptions)
            {
                var fifoAllocations = allocations.Where(a => a.ConsumptionId == consumption.Id).ToList();
                if (fifoAllocations.Any())
                {
                    // Use FIFO density from oldest allocation
                    var oldestAllocation = fifoAllocations.OrderBy(a => a.Purchase.PurchaseDate).First();
                    var fifoDepth = oldestAllocation.Purchase.Density;
                    totalTons += consumption.GetConsumptionTons(fifoDepth);
                }
                else
                {
                    // Use default density if no allocations (fallback)
                    totalTons += consumption.GetConsumptionTons(0.85m); // Default marine fuel density
                }
            }

            return totalTons;
        }

        private decimal CalculateConsumptionTonsForVessel(List<Consumption> consumptions, List<Allocation> allocations, int vesselId)
        {
            decimal totalTons = 0;
            var vesselConsumptions = consumptions.Where(c => c.VesselId == vesselId).ToList();

            foreach (var consumption in vesselConsumptions)
            {
                var fifoAllocations = allocations.Where(a => a.ConsumptionId == consumption.Id).ToList();
                if (fifoAllocations.Any())
                {
                    var oldestAllocation = fifoAllocations.OrderBy(a => a.Purchase.PurchaseDate).First();
                    var fifoDepth = oldestAllocation.Purchase.Density;
                    totalTons += consumption.GetConsumptionTons(fifoDepth);
                }
                else
                {
                    totalTons += consumption.GetConsumptionTons(0.85m);
                }
            }

            return totalTons;
        }

        #endregion

        #region Main Service Method

        public async Task<FleetEfficiencyResult> GenerateFleetEfficiencyAnalysisAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var context = new InventoryContext();

            // Default to last 12 months if no dates provided
            var endDate = toDate ?? DateTime.Today;
            var startDate = fromDate ?? endDate.AddMonths(-12);

            // Single optimized query to get all needed data
            var allConsumptions = await context.Consumptions
                .Include(c => c.Vessel)
                .Where(c => c.ConsumptionDate >= startDate && c.ConsumptionDate <= endDate)
                .OrderBy(c => c.ConsumptionDate)
                .ToListAsync();

            var allAllocations = await context.Allocations
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Supplier)
                .Include(a => a.Purchase)
                    .ThenInclude(p => p.Vessel)
                .Include(a => a.Consumption)
                    .ThenInclude(c => c.Vessel)
                .Where(a => a.Consumption.ConsumptionDate >= startDate && a.Consumption.ConsumptionDate <= endDate)
                .ToListAsync();

            var allVessels = await context.Vessels.ToListAsync();

            if (!allConsumptions.Any())
            {
                return new FleetEfficiencyResult(); // Return empty result
            }

            var result = new FleetEfficiencyResult
            {
                Overview = GenerateFleetOverview(allConsumptions, allAllocations, allVessels),
                VesselEfficiency = GenerateVesselEfficiencyDetails(allConsumptions, allAllocations),
                RouteComparison = GenerateRouteComparison(allConsumptions, allAllocations),
                MonthlyTrends = GenerateMonthlyTrends(allConsumptions, allAllocations),
                EfficiencyRankings = GenerateEfficiencyRankings(allConsumptions, allAllocations),
                CostEfficiency = GenerateCostEfficiencyDetails(allConsumptions, allAllocations),
                SeasonalPatterns = GenerateSeasonalPatterns(allConsumptions, allAllocations)
            };

            return result;
        }

        #endregion

        #region Private Methods - Fleet Overview

        private FleetOverview GenerateFleetOverview(List<Consumption> consumptions, List<Allocation> allocations, List<Vessel> allVessels)
        {
            var activeVessels = consumptions.Select(c => c.VesselId).Distinct().ToList();
            var vesselRoutes = allVessels.Where(v => v.Type == "Vessel").Count();
            var boatRoutes = allVessels.Where(v => v.Type == "Boat").Count();

            var totalConsumptionL = consumptions.Sum(c => c.ConsumptionLiters);
            var totalConsumptionT = CalculateTotalConsumptionTons(consumptions, allocations);
            var totalLegs = consumptions.Sum(c => c.LegsCompleted ?? 0);
            var totalCost = allocations.Sum(a => a.AllocatedValueUSD);

            var vesselEfficiencies = consumptions
                .GroupBy(c => new { c.VesselId, c.Vessel.Name })
                .Select(g => new
                {
                    VesselName = g.Key.Name,
                    EfficiencyL = g.Sum(c => c.LegsCompleted ?? 0) > 0 ? g.Sum(c => c.ConsumptionLiters) / g.Sum(c => c.LegsCompleted ?? 0) : 0
                })
                .Where(v => v.EfficiencyL > 0)
                .OrderBy(v => v.EfficiencyL)
                .ToList();

            var routeEfficiencies = consumptions
                .GroupBy(c => c.Vessel.Route)
                .Select(g => new
                {
                    Route = g.Key,
                    AvgEfficiency = g.Sum(c => c.LegsCompleted ?? 0) > 0 ? g.Sum(c => c.ConsumptionLiters) / g.Sum(c => c.LegsCompleted ?? 0) : 0
                })
                .Where(r => r.AvgEfficiency > 0)
                .OrderBy(r => r.AvgEfficiency)
                .ToList();

            return new FleetOverview
            {
                TotalActiveVessels = activeVessels.Count,
                TotalVesselRoutes = vesselRoutes,
                TotalBoatRoutes = boatRoutes,
                TotalFleetConsumptionL = totalConsumptionL,
                TotalFleetConsumptionT = totalConsumptionT,
                TotalLegsCompleted = totalLegs,
                AvgFleetEfficiencyLPerLeg = totalLegs > 0 ? totalConsumptionL / totalLegs : 0,
                AvgFleetEfficiencyTPerLeg = totalLegs > 0 ? (decimal)totalConsumptionT / totalLegs : 0,
                TotalFleetCostUSD = totalCost,
                AvgCostPerLegUSD = totalLegs > 0 ? totalCost / totalLegs : 0,
                AvgCostPerLiterUSD = totalConsumptionL > 0 ? totalCost / totalConsumptionL : 0,
                MostEfficientVessel = vesselEfficiencies.FirstOrDefault()?.VesselName ?? "N/A",
                LeastEfficientVessel = vesselEfficiencies.LastOrDefault()?.VesselName ?? "N/A",
                BestRoute = routeEfficiencies.FirstOrDefault()?.Route ?? "N/A",
                BestRouteEfficiency = routeEfficiencies.FirstOrDefault()?.AvgEfficiency ?? 0
            };
        }

        #endregion

        #region Private Methods - Vessel Efficiency Details

        private List<VesselEfficiencyDetail> GenerateVesselEfficiencyDetails(List<Consumption> consumptions, List<Allocation> allocations)
        {
            var vesselData = consumptions
                .GroupBy(c => new { c.VesselId, c.Vessel.Name, c.Vessel.Type, c.Vessel.Route })
                .Select(g =>
                {
                    var totalConsumptionL = g.Sum(c => c.ConsumptionLiters);
                    var totalConsumptionT = CalculateConsumptionTonsForVessel(consumptions, allocations, g.Key.VesselId);
                    var totalLegs = g.Sum(c => c.LegsCompleted ?? 0);
                    var vesselAllocations = allocations.Where(a => a.Consumption.VesselId == g.Key.VesselId).ToList();
                    var totalCost = vesselAllocations.Sum(a => a.AllocatedValueUSD);
                    var monthsActive = g.Select(c => c.Month).Distinct().Count();

                    return new VesselEfficiencyDetail
                    {
                        VesselName = g.Key.Name,
                        VesselType = g.Key.Type,
                        Route = g.Key.Route,
                        TotalConsumptionL = totalConsumptionL,
                        TotalConsumptionT = totalConsumptionT,
                        TotalLegs = totalLegs,
                        EfficiencyLPerLeg = totalLegs > 0 ? totalConsumptionL / totalLegs : 0,
                        EfficiencyTPerLeg = totalLegs > 0 ? totalConsumptionT / totalLegs : 0,
                        TotalCostUSD = totalCost,
                        CostPerLegUSD = totalLegs > 0 ? totalCost / totalLegs : 0,
                        CostPerLiterUSD = totalConsumptionL > 0 ? totalCost / totalConsumptionL : 0,
                        CostPerTonUSD = totalConsumptionT > 0 ? totalCost / totalConsumptionT : 0,
                        MonthsActive = monthsActive,
                        ConsumptionEntries = g.Count(),
                        FirstConsumption = g.Min(c => c.ConsumptionDate),
                        LastConsumption = g.Max(c => c.ConsumptionDate),
                        EfficiencyRank = 0, // Will be calculated after sorting
                        EfficiencyGrade = "N/A",
                        EfficiencyImprovement = 0 // Will be calculated with trend analysis
                    };
                })
                .OrderBy(v => v.EfficiencyLPerLeg)
                .ToList();

            // Assign ranks and grades
            for (int i = 0; i < vesselData.Count; i++)
            {
                vesselData[i].EfficiencyRank = i + 1;
                vesselData[i].EfficiencyGrade = i < vesselData.Count * 0.25 ? "Excellent" :
                                               i < vesselData.Count * 0.50 ? "Good" :
                                               i < vesselData.Count * 0.75 ? "Average" : "Poor";
            }

            return vesselData;
        }

        #endregion

        #region Private Methods - Route Comparison

        private List<RouteEfficiencyComparison> GenerateRouteComparison(List<Consumption> consumptions, List<Allocation> allocations)
        {
            return consumptions
                .GroupBy(c => new { c.Vessel.Type, c.Vessel.Route })
                .Select(g =>
                {
                    var routeConsumptions = g.ToList();
                    var vesselCount = routeConsumptions.Select(c => c.VesselId).Distinct().Count();
                    var totalConsumptionL = routeConsumptions.Sum(c => c.ConsumptionLiters);
                    var totalConsumptionT = CalculateTotalConsumptionTons(routeConsumptions, allocations);
                    var totalLegs = routeConsumptions.Sum(c => c.LegsCompleted ?? 0);
                    var routeAllocations = allocations.Where(a => a.Consumption.Vessel.Route == g.Key.Route).ToList();
                    var totalCost = routeAllocations.Sum(a => a.AllocatedValueUSD);

                    var vesselEfficiencies = routeConsumptions
                        .GroupBy(c => new { c.VesselId, c.Vessel.Name })
                        .Select(vg => new
                        {
                            VesselName = vg.Key.Name,
                            EfficiencyL = vg.Sum(c => c.LegsCompleted ?? 0) > 0 ? vg.Sum(c => c.ConsumptionLiters) / vg.Sum(c => c.LegsCompleted ?? 0) : 0
                        })
                        .Where(v => v.EfficiencyL > 0)
                        .OrderBy(v => v.EfficiencyL)
                        .ToList();

                    return new RouteEfficiencyComparison
                    {
                        RouteType = g.Key.Type == "Vessel" ? "Vessel Route" : "Boat Route",
                        RouteName = g.Key.Route,
                        VesselCount = vesselCount,
                        AvgEfficiencyLPerLeg = totalLegs > 0 ? totalConsumptionL / (decimal)totalLegs : 0,
                        AvgEfficiencyTPerLeg = totalLegs > 0 ? totalConsumptionT / (decimal)totalLegs : 0,
                        AvgCostPerLegUSD = totalLegs > 0 ? totalCost / (decimal)totalLegs : 0,
                        AvgCostPerLiterUSD = totalConsumptionL > 0 ? totalCost / totalConsumptionL : 0,
                        TotalConsumptionL = totalConsumptionL,
                        TotalConsumptionT = totalConsumptionT,
                        TotalLegs = totalLegs,
                        TotalCostUSD = totalCost,
                        BestVesselEfficiencyL = vesselEfficiencies.FirstOrDefault()?.EfficiencyL ?? 0,
                        WorstVesselEfficiencyL = vesselEfficiencies.LastOrDefault()?.EfficiencyL ?? 0,
                        BestVesselName = vesselEfficiencies.FirstOrDefault()?.VesselName ?? "N/A",
                        WorstVesselName = vesselEfficiencies.LastOrDefault()?.VesselName ?? "N/A",
                        EfficiencyVariance = vesselEfficiencies.Any() ? vesselEfficiencies.Max(v => v.EfficiencyL) - vesselEfficiencies.Min(v => v.EfficiencyL) : 0
                    };
                })
                .OrderBy(r => r.AvgEfficiencyLPerLeg)
                .ToList();
        }

        #endregion

        #region Private Methods - Monthly Trends

        private List<MonthlyEfficiencyTrend> GenerateMonthlyTrends(List<Consumption> consumptions, List<Allocation> allocations)
        {
            var monthlyData = consumptions
                .GroupBy(c => c.Month)
                .Select(g =>
                {
                    var monthConsumptions = g.ToList();
                    var totalConsumptionL = monthConsumptions.Sum(c => c.ConsumptionLiters);
                    var totalConsumptionT = CalculateTotalConsumptionTons(monthConsumptions, allocations);
                    var totalLegs = monthConsumptions.Sum(c => c.LegsCompleted ?? 0);
                    var activeVessels = monthConsumptions.Select(c => c.VesselId).Distinct().Count();
                    var monthAllocations = allocations.Where(a => a.Month == g.Key).ToList();
                    var totalCost = monthAllocations.Sum(a => a.AllocatedValueUSD);

                    return new MonthlyEfficiencyTrend
                    {
                        Month = g.Key,
                        MonthDate = DateTime.ParseExact(g.Key, "yyyy-MM", null),
                        FleetAvgEfficiencyL = totalLegs > 0 ? totalConsumptionL / (decimal)totalLegs : 0,
                        FleetAvgEfficiencyT = totalLegs > 0 ? totalConsumptionT / (decimal)totalLegs : 0,
                        FleetAvgCostPerLeg = totalLegs > 0 ? totalCost / (decimal)totalLegs : 0,
                        TotalConsumptionL = totalConsumptionL,
                        TotalConsumptionT = totalConsumptionT,
                        TotalLegs = totalLegs,
                        ActiveVessels = activeVessels,
                        TotalCostUSD = totalCost,
                        MonthOverMonthChangeL = 0, // Will be calculated below
                        MonthOverMonthChangeT = 0,
                        CostEfficiencyChange = 0
                    };
                })
                .OrderBy(m => m.MonthDate)
                .ToList();

            // Calculate month-over-month changes
            for (int i = 1; i < monthlyData.Count; i++)
            {
                var current = monthlyData[i];
                var previous = monthlyData[i - 1];

                if (previous.FleetAvgEfficiencyL > 0)
                {
                    current.MonthOverMonthChangeL = ((current.FleetAvgEfficiencyL - previous.FleetAvgEfficiencyL) / previous.FleetAvgEfficiencyL) * 100;
                }
                if (previous.FleetAvgEfficiencyT > 0)
                {
                    current.MonthOverMonthChangeT = ((current.FleetAvgEfficiencyT - previous.FleetAvgEfficiencyT) / previous.FleetAvgEfficiencyT) * 100;
                }
                if (previous.FleetAvgCostPerLeg > 0)
                {
                    current.CostEfficiencyChange = ((current.FleetAvgCostPerLeg - previous.FleetAvgCostPerLeg) / previous.FleetAvgCostPerLeg) * 100;
                }
            }

            return monthlyData;
        }

        #endregion

        #region Private Methods - Efficiency Rankings

        private List<VesselRanking> GenerateEfficiencyRankings(List<Consumption> consumptions, List<Allocation> allocations)
        {
            var fleetAvgEfficiency = consumptions.Sum(c => c.LegsCompleted ?? 0) > 0
                ? consumptions.Sum(c => c.ConsumptionLiters) / consumptions.Sum(c => c.LegsCompleted ?? 0)
                : 0;

            var rankings = consumptions
                .GroupBy(c => new { c.VesselId, c.Vessel.Name, c.Vessel.Type, c.Vessel.Route })
                .Select(g =>
                {
                    var totalConsumptionL = g.Sum(c => c.ConsumptionLiters);
                    var totalConsumptionT = CalculateConsumptionTonsForVessel(consumptions, allocations, g.Key.VesselId);
                    var totalLegs = g.Sum(c => c.LegsCompleted ?? 0);
                    var efficiencyL = totalLegs > 0 ? totalConsumptionL / totalLegs : 0;
                    var vesselAllocations = allocations.Where(a => a.Consumption.VesselId == g.Key.VesselId).ToList();
                    var totalCost = vesselAllocations.Sum(a => a.AllocatedValueUSD);
                    var costPerLeg = totalLegs > 0 ? totalCost / totalLegs : 0;

                    // Calculate overall efficiency score (lower is better for fuel consumption, normalized)
                    var efficiencyScore = fleetAvgEfficiency > 0 ? (efficiencyL / fleetAvgEfficiency) * 100 : 100;

                    return new VesselRanking
                    {
                        VesselName = g.Key.Name,
                        VesselType = g.Key.Type,
                        Route = g.Key.Route,
                        EfficiencyLPerLeg = efficiencyL,
                        EfficiencyTPerLeg = totalLegs > 0 ? totalConsumptionT / totalLegs : 0,
                        CostPerLegUSD = costPerLeg,
                        OverallEfficiencyScore = efficiencyScore,
                        BenchmarkDifference = fleetAvgEfficiency > 0 ? ((efficiencyL - fleetAvgEfficiency) / fleetAvgEfficiency) * 100 : 0,
                        TrendDirection = "Stable" // Would need historical data for actual trend
                    };
                })
                .OrderBy(r => r.EfficiencyLPerLeg) // Best efficiency first (lowest consumption per leg)
                .ToList();

            // Assign ranks and performance categories
            for (int i = 0; i < rankings.Count; i++)
            {
                rankings[i].Rank = i + 1;
                rankings[i].PerformanceCategory = i < rankings.Count * 0.25 ? "Excellent" :
                                                 i < rankings.Count * 0.50 ? "Good" :
                                                 i < rankings.Count * 0.75 ? "Average" : "Poor";
            }

            return rankings;
        }

        #endregion

        #region Private Methods - Cost Efficiency

        private List<CostEfficiencyDetail> GenerateCostEfficiencyDetails(List<Consumption> consumptions, List<Allocation> allocations)
        {
            return consumptions
                .GroupBy(c => new { c.VesselId, c.Vessel.Name, c.Vessel.Type })
                .Select(g =>
                {
                    var vesselAllocations = allocations.Where(a => a.Consumption.VesselId == g.Key.VesselId).ToList();
                    var totalFIFOCost = vesselAllocations.Sum(a => a.AllocatedValueUSD);
                    var totalConsumptionL = g.Sum(c => c.ConsumptionLiters);
                    var totalConsumptionT = CalculateConsumptionTonsForVessel(consumptions, allocations, g.Key.VesselId); // FIXED THIS LINE
                    var totalLegs = g.Sum(c => c.LegsCompleted ?? 0);
                    var supplierCount = vesselAllocations.Select(a => a.Purchase.SupplierId).Distinct().Count();

                    return new CostEfficiencyDetail
                    {
                        VesselName = g.Key.Name,
                        VesselType = g.Key.Type,
                        TotalFIFOCostUSD = totalFIFOCost,
                        CostPerLiterFIFO = totalConsumptionL > 0 ? totalFIFOCost / totalConsumptionL : 0,
                        CostPerTonFIFO = totalConsumptionT > 0 ? totalFIFOCost / totalConsumptionT : 0,
                        CostPerLegFIFO = totalLegs > 0 ? totalFIFOCost / totalLegs : 0,
                        AvgFuelPricePaid = vesselAllocations.Any() ? vesselAllocations.Average(a => a.AllocatedValueUSD / a.AllocatedQuantity) : 0,
                        NumberOfSuppliers = supplierCount,
                        CostEfficiencyRank = 0, // Will be assigned after sorting
                        ProcurementEfficiency = 0, // Will be calculated vs fleet average
                        OperationalEfficiency = 0, // Will be calculated vs fleet average
                        OverallCostScore = 0,
                        FuelInventoryTurnover = 0 // Would need inventory data
                    };
                })
                .OrderBy(c => c.CostPerLiterFIFO)
                .ToList();
        }

        #endregion

        #region Private Methods - Seasonal Patterns

        private List<SeasonalEfficiencyPattern> GenerateSeasonalPatterns(List<Consumption> consumptions, List<Allocation> allocations)
        {
            var seasonalData = consumptions
                .GroupBy(c => GetSeason(DateTime.ParseExact(c.Month, "yyyy-MM", null)))
                .Select(g =>
                {
                    var seasonConsumptions = g.ToList();
                    var totalConsumptionL = seasonConsumptions.Sum(c => c.ConsumptionLiters);
                    var totalConsumptionT = CalculateTotalConsumptionTons(seasonConsumptions, allocations);
                    var totalLegs = seasonConsumptions.Sum(c => c.LegsCompleted);
                    var seasonAllocations = allocations.Where(a => seasonConsumptions.Any(sc => sc.Month == a.Month)).ToList();
                    var totalCost = seasonAllocations.Sum(a => a.AllocatedValueUSD);

                    return new SeasonalEfficiencyPattern
                    {
                        Season = g.Key.Item1,
                        SeasonName = g.Key.Item2,
                        AvgEfficiencyL = totalLegs > 0 ? totalConsumptionL / (decimal)totalLegs : 0,
                        AvgEfficiencyT = totalLegs > 0 ? totalConsumptionT / (decimal)totalLegs : 0,
                        AvgCostPerLeg = totalLegs > 0 ? totalCost / (decimal)totalLegs : 0,
                        DataPoints = seasonConsumptions.Select(c => c.Month).Distinct().Count(),
                        SeasonalVariance = 0, // Would need multi-year data
                        YearOverYearChange = 0, // Would need multi-year data
                        WeatherImpact = GetWeatherImpact(g.Key.Item2),
                        CapacityUtilization = 0 // Would need capacity data
                    };
                })
                .OrderBy(s => s.Season)
                .ToList();

            return seasonalData;
        }

        private (string, string) GetSeason(DateTime date)
        {
            int month = date.Month;
            return month switch
            {
                12 or 1 or 2 => ("Q1", "Winter"),
                3 or 4 or 5 => ("Q2", "Spring"),
                6 or 7 or 8 => ("Q3", "Summer"),
                9 or 10 or 11 => ("Q4", "Autumn"),
                _ => ("Q1", "Winter")
            };
        }

        private string GetWeatherImpact(string season)
        {
            return season switch
            {
                "Winter" => "Moderate Impact",
                "Spring" => "Low Impact",
                "Summer" => "High Impact",
                "Autumn" => "Low Impact",
                _ => "Unknown"
            };
        }

        #endregion
    }
}