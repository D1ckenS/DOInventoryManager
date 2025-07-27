using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class RoutePerformanceService
    {
        #region Data Models

        public class RoutePerformanceResult
        {
            public RoutePerformanceOverview Overview { get; set; } = new();
            public List<RouteComparison> RouteComparisons { get; set; } = [];
            public List<VesselRoutePerformance> VesselPerformance { get; set; } = [];
            public List<RouteEfficiencyTrend> EfficiencyTrends { get; set; } = [];
            public List<RouteCostAnalysis> CostAnalysis { get; set; } = [];
            public List<RouteOptimizationRecommendation> OptimizationRecommendations { get; set; } = [];
            public List<RouteProfitabilityAnalysis> ProfitabilityAnalysis { get; set; } = [];
        }

        public class RoutePerformanceOverview
        {
            public int TotalActiveRoutes { get; set; }
            public string MostEfficientRoute { get; set; } = string.Empty;
            public string LeastEfficientRoute { get; set; } = string.Empty;
            public decimal BestRouteEfficiencyLPerLeg { get; set; }
            public decimal WorstRouteEfficiencyLPerLeg { get; set; }
            public decimal TotalRouteDistanceKm { get; set; }
            public int TotalLegsCompleted { get; set; }
            public decimal TotalFuelConsumedL { get; set; }
            public decimal TotalFuelConsumedT { get; set; }
            public decimal TotalRouteCostUSD { get; set; }
            public decimal AvgCostPerKm { get; set; }
            public decimal AvgFuelPerKm { get; set; }
            public string MostProfitableRoute { get; set; } = string.Empty;
            public string LeastProfitableRoute { get; set; } = string.Empty;
            public decimal RouteEfficiencyGap { get; set; }

            // Formatted properties
            public string FormattedTotalCost => TotalRouteCostUSD < 0 ? $"({Math.Abs(TotalRouteCostUSD):C2})" : TotalRouteCostUSD.ToString("C2");
            public string FormattedRouteGap => $"{RouteEfficiencyGap:N1}%";
        }

        public class RouteComparison
        {
            public string RouteType { get; set; } = string.Empty;
            public string RouteDescription { get; set; } = string.Empty;
            public decimal EstimatedDistanceKm { get; set; }
            public int ActiveVessels { get; set; }
            public int TotalLegsCompleted { get; set; }
            public decimal TotalFuelConsumedL { get; set; }
            public decimal TotalFuelConsumedT { get; set; }
            public decimal AvgEfficiencyLPerLeg { get; set; }
            public decimal AvgEfficiencyTPerLeg { get; set; }
            public decimal AvgEfficiencyLPerKm { get; set; }
            public decimal TotalRouteCostUSD { get; set; }
            public decimal AvgCostPerLeg { get; set; }
            public decimal AvgCostPerKm { get; set; }
            public decimal RouteUtilizationRate { get; set; }
            public string PerformanceCategory { get; set; } = string.Empty;
            public decimal CompetitiveAdvantage { get; set; }

            // Formatted properties
            public string FormattedTotalCost => TotalRouteCostUSD < 0 ? $"({Math.Abs(TotalRouteCostUSD):C2})" : TotalRouteCostUSD.ToString("C2");
            public string FormattedCostPerLeg => AvgCostPerLeg < 0 ? $"({Math.Abs(AvgCostPerLeg):C2})" : AvgCostPerLeg.ToString("C2");
            public string FormattedCostPerKm => AvgCostPerKm < 0 ? $"({Math.Abs(AvgCostPerKm):C6})" : AvgCostPerKm.ToString("C6");
            public string FormattedUtilization => $"{RouteUtilizationRate:N1}%";
            public string FormattedAdvantage => CompetitiveAdvantage < 0 ? $"({Math.Abs(CompetitiveAdvantage):N2}%)" : $"{CompetitiveAdvantage:N2}%";
        }

        public class VesselRoutePerformance
        {
            public string VesselName { get; set; } = string.Empty;
            public string VesselType { get; set; } = string.Empty;
            public string Route { get; set; } = string.Empty;
            public decimal EstimatedDistanceKm { get; set; }
            public int LegsCompleted { get; set; }
            public decimal TotalFuelL { get; set; }
            public decimal TotalFuelT { get; set; }
            public decimal EfficiencyLPerLeg { get; set; }
            public decimal EfficiencyTPerLeg { get; set; }
            public decimal EfficiencyLPerKm { get; set; }
            public decimal TotalCostUSD { get; set; }
            public decimal CostPerLeg { get; set; }
            public decimal CostPerKm { get; set; }
            public int RouteRank { get; set; }
            public string PerformanceGrade { get; set; } = string.Empty;
            public decimal RouteOptimizationScore { get; set; }
            public string RecommendedAction { get; set; } = string.Empty;
            public decimal BenchmarkDeviationPercent { get; set; }

            // Formatted properties
            public string FormattedTotalCost => TotalCostUSD < 0 ? $"({Math.Abs(TotalCostUSD):C2})" : TotalCostUSD.ToString("C2");
            public string FormattedCostPerLeg => CostPerLeg < 0 ? $"({Math.Abs(CostPerLeg):C2})" : CostPerLeg.ToString("C2");
            public string FormattedCostPerKm => CostPerKm < 0 ? $"({Math.Abs(CostPerKm):C6})" : CostPerKm.ToString("C6");
            public string FormattedOptimizationScore => $"{RouteOptimizationScore:N1}%";
            public string FormattedBenchmarkDeviation => BenchmarkDeviationPercent < 0 ? $"({Math.Abs(BenchmarkDeviationPercent):N2}%)" : $"{BenchmarkDeviationPercent:N2}%";
            public string GradeIcon => PerformanceGrade switch
            {
                "A" => "🥇",
                "B" => "🥈",
                "C" => "🥉",
                "D" => "⚠️",
                "F" => "❌",
                _ => "●"
            };
        }

        public class RouteEfficiencyTrend
        {
            public string Month { get; set; } = string.Empty;
            public DateTime MonthDate { get; set; }
            public string RouteType { get; set; } = string.Empty;
            public decimal AvgEfficiencyLPerLeg { get; set; }
            public decimal AvgEfficiencyTPerLeg { get; set; }
            public decimal AvgEfficiencyLPerKm { get; set; }
            public int TotalLegs { get; set; }
            public decimal TotalFuelL { get; set; }
            public decimal MonthOverMonthChangeL { get; set; }
            public decimal MonthOverMonthChangeT { get; set; }
            public decimal SeasonalAdjustment { get; set; }
            public string TrendDirection { get; set; } = string.Empty;
            public decimal EfficiencyVolatility { get; set; }
            public string OptimalityIndicator { get; set; } = string.Empty;

            // Formatted properties
            public string FormattedMonth => MonthDate.ToString("MMM yyyy");
            public string FormattedMoMChangeL => MonthOverMonthChangeL < 0 ? $"({Math.Abs(MonthOverMonthChangeL):N2}%)" : $"{MonthOverMonthChangeL:N2}%";
            public string FormattedMoMChangeT => MonthOverMonthChangeT < 0 ? $"({Math.Abs(MonthOverMonthChangeT):N2}%)" : $"{MonthOverMonthChangeT:N2}%";
            public string TrendIcon => TrendDirection switch
            {
                "Improving" => "📈",
                "Declining" => "📉",
                "Stable" => "→",
                _ => "●"
            };
        }

        public class RouteCostAnalysis
        {
            public string RouteType { get; set; } = string.Empty;
            public decimal TotalRouteCostUSD { get; set; }
            public decimal AvgCostPerLeg { get; set; }
            public decimal AvgCostPerKm { get; set; }
            public decimal AvgCostPerLiter { get; set; }
            public decimal AvgCostPerTon { get; set; }
            public decimal FuelCostPercentage { get; set; }
            public decimal OperationalCostPercentage { get; set; }
            public decimal CostEfficiencyRank { get; set; }
            public decimal CostVariabilityIndex { get; set; }
            public decimal BenchmarkCostAdvantage { get; set; }
            public decimal CostOptimizationPotential { get; set; }
            public string CostCategory { get; set; } = string.Empty;
            public string CostDrivers { get; set; } = string.Empty;

            // Formatted properties
            public string FormattedTotalCost => TotalRouteCostUSD < 0 ? $"({Math.Abs(TotalRouteCostUSD):C2})" : TotalRouteCostUSD.ToString("C2");
            public string FormattedCostPerLeg => AvgCostPerLeg < 0 ? $"({Math.Abs(AvgCostPerLeg):C2})" : AvgCostPerLeg.ToString("C2");
            public string FormattedCostPerKm => AvgCostPerKm < 0 ? $"({Math.Abs(AvgCostPerKm):C6})" : AvgCostPerKm.ToString("C6");
            public string FormattedFuelPercentage => $"{FuelCostPercentage:N1}%";
            public string FormattedCostAdvantage => BenchmarkCostAdvantage < 0 ? $"({Math.Abs(BenchmarkCostAdvantage):N2}%)" : $"{BenchmarkCostAdvantage:N2}%";
            public string FormattedOptimizationPotential => CostOptimizationPotential < 0 ? $"({Math.Abs(CostOptimizationPotential):C2})" : CostOptimizationPotential.ToString("C2");
        }

        public class RouteOptimizationRecommendation
        {
            public string RouteType { get; set; } = string.Empty;
            public string RecommendationType { get; set; } = string.Empty;
            public string RecommendationTitle { get; set; } = string.Empty;
            public string RecommendationDescription { get; set; } = string.Empty;
            public decimal PotentialSavingsUSD { get; set; }
            public decimal EfficiencyImprovementPercent { get; set; }
            public string ImplementationComplexity { get; set; } = string.Empty;
            public string TimeToImplement { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty;
            public decimal ROIEstimate { get; set; }
            public string RequiredResources { get; set; } = string.Empty;
            public string RiskLevel { get; set; } = string.Empty;

            // Formatted properties
            public string FormattedPotentialSavings => PotentialSavingsUSD < 0 ? $"({Math.Abs(PotentialSavingsUSD):C2})" : PotentialSavingsUSD.ToString("C2");
            public string FormattedEfficiencyImprovement => $"{EfficiencyImprovementPercent:N1}%";
            public string FormattedROI => $"{ROIEstimate:N1}%";
            public string PriorityIcon => Priority switch
            {
                "High" => "🔴",
                "Medium" => "🟡",
                "Low" => "🟢",
                _ => "⚪"
            };
        }

        public class RouteProfitabilityAnalysis
        {
            public string RouteType { get; set; } = string.Empty;
            public decimal TotalRevenueUSD { get; set; }
            public decimal TotalCostUSD { get; set; }
            public decimal NetProfitUSD { get; set; }
            public decimal ProfitMarginPercent { get; set; }
            public decimal RevenuePerLeg { get; set; }
            public decimal RevenuePerKm { get; set; }
            public decimal ProfitPerLeg { get; set; }
            public decimal ProfitPerKm { get; set; }
            public decimal ROIPercent { get; set; }
            public decimal PaybackPeriodMonths { get; set; }
            public string ProfitabilityTrend { get; set; } = string.Empty;
            public decimal BreakEvenLegsRequired { get; set; }
            public string ProfitabilityRank { get; set; } = string.Empty;

            // Formatted properties
            public string FormattedRevenue => TotalRevenueUSD < 0 ? $"({Math.Abs(TotalRevenueUSD):C2})" : TotalRevenueUSD.ToString("C2");
            public string FormattedCost => TotalCostUSD < 0 ? $"({Math.Abs(TotalCostUSD):C2})" : TotalCostUSD.ToString("C2");
            public string FormattedNetProfit => NetProfitUSD < 0 ? $"({Math.Abs(NetProfitUSD):C2})" : NetProfitUSD.ToString("C2");
            public string FormattedProfitMargin => ProfitMarginPercent < 0 ? $"({Math.Abs(ProfitMarginPercent):N2}%)" : $"{ProfitMarginPercent:N2}%";
            public string FormattedRevenuePerLeg => RevenuePerLeg < 0 ? $"({Math.Abs(RevenuePerLeg):C2})" : RevenuePerLeg.ToString("C2");
            public string FormattedProfitPerLeg => ProfitPerLeg < 0 ? $"({Math.Abs(ProfitPerLeg):C2})" : ProfitPerLeg.ToString("C2");
        }

        #endregion

        #region Main Service Method

        public async Task<RoutePerformanceResult> GenerateRoutePerformanceAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var context = new InventoryContext();

            // Default to last 12 months if no dates provided
            var endDate = toDate ?? DateTime.Today;
            var startDate = fromDate ?? endDate.AddMonths(-12);

            // Get all consumptions in date range
            var consumptions = await context.Consumptions
                .Include(c => c.Vessel)
                .Include(c => c.Allocations)
                    .ThenInclude(a => a.Purchase)
                .Where(c => c.ConsumptionDate >= startDate && c.ConsumptionDate <= endDate)
                .OrderBy(c => c.ConsumptionDate)
                .ToListAsync();

            if (!consumptions.Any())
            {
                return new RoutePerformanceResult(); // Return empty result
            }

            var result = new RoutePerformanceResult
            {
                Overview = GenerateOverview(consumptions),
                RouteComparisons = GenerateRouteComparisons(consumptions),
                VesselPerformance = GenerateVesselPerformance(consumptions),
                EfficiencyTrends = GenerateEfficiencyTrends(consumptions),
                CostAnalysis = GenerateCostAnalysis(consumptions),
                OptimizationRecommendations = GenerateOptimizationRecommendations(consumptions),
                ProfitabilityAnalysis = GenerateProfitabilityAnalysis(consumptions)
            };

            return result;
        }

        #endregion

        #region Private Methods - Overview

        private RoutePerformanceOverview GenerateOverview(List<Consumption> consumptions)
        {
            var routePerformance = consumptions
                .GroupBy(c => c.Vessel.Route)
                .Select(g => new
                {
                    Route = g.Key,
                    AvgEfficiency = g.Sum(c => c.LegsCompleted) > 0 ? g.Sum(c => c.ConsumptionLiters) / g.Sum(c => c.LegsCompleted) : 0,
                    TotalCost = g.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD)
                })
                .Where(r => r.AvgEfficiency > 0)
                .OrderBy(r => r.AvgEfficiency)
                .ToList();

            var bestRoute = routePerformance.FirstOrDefault();
            var worstRoute = routePerformance.LastOrDefault();

            var totalLegs = consumptions.Sum(c => c.LegsCompleted);
            var totalFuelL = consumptions.Sum(c => c.ConsumptionLiters);
            var totalFuelT = CalculateTotalConsumptionTons(consumptions);
            var totalCost = consumptions.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD);

            // Estimate distances based on route types
            var vesselRouteDistance = 520m; // Aqaba-Nuweibaa-Aqaba (approximate)
            var boatRouteDistance = 340m; // Aqaba-Taba-Aqaba (approximate)
            var totalDistance = CalculateTotalRouteDistance(consumptions, vesselRouteDistance, boatRouteDistance);

            var efficiencyGap = (bestRoute != null && worstRoute != null && bestRoute.AvgEfficiency > 0)
                ? ((worstRoute.AvgEfficiency - bestRoute.AvgEfficiency) / bestRoute.AvgEfficiency) * 100
                : 0;

            // Find most/least profitable routes (simplified based on cost efficiency)
            var routeProfitability = routePerformance.OrderBy(r => r.TotalCost / Math.Max(1, totalLegs)).ToList();
            var mostProfitable = routeProfitability.FirstOrDefault()?.Route ?? "N/A";
            var leastProfitable = routeProfitability.LastOrDefault()?.Route ?? "N/A";

            return new RoutePerformanceOverview
            {
                TotalActiveRoutes = consumptions.Select(c => c.Vessel.Route).Distinct().Count(),
                MostEfficientRoute = bestRoute?.Route ?? "N/A",
                LeastEfficientRoute = worstRoute?.Route ?? "N/A",
                BestRouteEfficiencyLPerLeg = bestRoute?.AvgEfficiency ?? 0,
                WorstRouteEfficiencyLPerLeg = worstRoute?.AvgEfficiency ?? 0,
                TotalRouteDistanceKm = totalDistance,
                TotalLegsCompleted = totalLegs,
                TotalFuelConsumedL = totalFuelL,
                TotalFuelConsumedT = totalFuelT,
                TotalRouteCostUSD = totalCost,
                AvgCostPerKm = totalDistance > 0 ? totalCost / totalDistance : 0,
                AvgFuelPerKm = totalDistance > 0 ? totalFuelL / totalDistance : 0,
                MostProfitableRoute = mostProfitable,
                LeastProfitableRoute = leastProfitable,
                RouteEfficiencyGap = efficiencyGap
            };
        }

        #endregion

        #region Private Methods - Route Comparisons

        private List<RouteComparison> GenerateRouteComparisons(List<Consumption> consumptions)
        {
            var vesselRouteDistance = 520m; // Aqaba-Nuweibaa-Aqaba
            var boatRouteDistance = 340m; // Aqaba-Taba-Aqaba

            var routeComparisons = consumptions
                .GroupBy(c => c.Vessel.Route)
                .Select(g =>
                {
                    var routeType = g.First().Vessel.Type;
                    var estimatedDistance = routeType == "Vessel" ? vesselRouteDistance : boatRouteDistance;
                    var totalLegs = g.Sum(c => c.LegsCompleted);
                    var totalFuelL = g.Sum(c => c.ConsumptionLiters);
                    var totalFuelT = CalculateConsumptionTonsForRoute(g.ToList());
                    var totalCost = g.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD);
                    var activeVessels = g.Select(c => c.VesselId).Distinct().Count();

                    var avgEfficiencyLPerLeg = totalLegs > 0 ? totalFuelL / totalLegs : 0;
                    var avgEfficiencyTPerLeg = totalLegs > 0 ? totalFuelT / totalLegs : 0;
                    var avgEfficiencyLPerKm = (totalLegs * estimatedDistance) > 0 ? totalFuelL / (totalLegs * estimatedDistance) : 0;
                    var avgCostPerLeg = totalLegs > 0 ? totalCost / totalLegs : 0;
                    var avgCostPerKm = (totalLegs * estimatedDistance) > 0 ? totalCost / (totalLegs * estimatedDistance) : 0;

                    // Calculate utilization rate (simplified as operational days)
                    var dateRange = (consumptions.Max(c => c.ConsumptionDate) - consumptions.Min(c => c.ConsumptionDate)).Days;
                    var utilization = dateRange > 0 ? (g.Count() * 30m) / dateRange * 100 : 0; // Rough estimate

                    var performanceCategory = avgEfficiencyLPerLeg switch
                    {
                        <= 200 => "Excellent",
                        <= 300 => "Good",
                        <= 400 => "Average",
                        <= 500 => "Below Average",
                        _ => "Poor"
                    };

                    return new RouteComparison
                    {
                        RouteType = routeType,
                        RouteDescription = g.Key,
                        EstimatedDistanceKm = estimatedDistance,
                        ActiveVessels = activeVessels,
                        TotalLegsCompleted = totalLegs,
                        TotalFuelConsumedL = totalFuelL,
                        TotalFuelConsumedT = totalFuelT,
                        AvgEfficiencyLPerLeg = avgEfficiencyLPerLeg,
                        AvgEfficiencyTPerLeg = avgEfficiencyTPerLeg,
                        AvgEfficiencyLPerKm = avgEfficiencyLPerKm,
                        TotalRouteCostUSD = totalCost,
                        AvgCostPerLeg = avgCostPerLeg,
                        AvgCostPerKm = avgCostPerKm,
                        RouteUtilizationRate = Math.Min(100, utilization),
                        PerformanceCategory = performanceCategory,
                        CompetitiveAdvantage = 0 // Will be calculated below
                    };
                })
                .OrderBy(r => r.AvgEfficiencyLPerLeg)
                .ToList();

            // Calculate competitive advantage
            var avgRouteEfficiency = routeComparisons.Any() ? routeComparisons.Average(r => r.AvgEfficiencyLPerLeg) : 0;
            foreach (var route in routeComparisons)
            {
                route.CompetitiveAdvantage = avgRouteEfficiency > 0
                    ? ((avgRouteEfficiency - route.AvgEfficiencyLPerLeg) / avgRouteEfficiency) * 100
                    : 0;
            }

            return routeComparisons;
        }

        #endregion

        #region Private Methods - Vessel Performance

        private List<VesselRoutePerformance> GenerateVesselPerformance(List<Consumption> consumptions)
        {
            var vesselRouteDistance = 520m;
            var boatRouteDistance = 340m;

            var vesselPerformance = consumptions
                .GroupBy(c => new { c.VesselId, c.Vessel.Name, c.Vessel.Type, c.Vessel.Route })
                .Select(g =>
                {
                    var estimatedDistance = g.Key.Type == "Vessel" ? vesselRouteDistance : boatRouteDistance;
                    var totalLegs = g.Sum(c => c.LegsCompleted);
                    var totalFuelL = g.Sum(c => c.ConsumptionLiters);
                    var totalFuelT = CalculateConsumptionTonsForVessel(g.ToList());
                    var totalCost = g.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD);

                    var efficiencyLPerLeg = totalLegs > 0 ? totalFuelL / totalLegs : 0;
                    var efficiencyTPerLeg = totalLegs > 0 ? totalFuelT / totalLegs : 0;
                    var efficiencyLPerKm = (totalLegs * estimatedDistance) > 0 ? totalFuelL / (totalLegs * estimatedDistance) : 0;
                    var costPerLeg = totalLegs > 0 ? totalCost / totalLegs : 0;
                    var costPerKm = (totalLegs * estimatedDistance) > 0 ? totalCost / (totalLegs * estimatedDistance) : 0;

                    return new
                    {
                        VesselName = g.Key.Name,
                        VesselType = g.Key.Type,
                        Route = g.Key.Route,
                        EstimatedDistance = estimatedDistance,
                        TotalLegs = totalLegs,
                        TotalFuelL = totalFuelL,
                        TotalFuelT = totalFuelT,
                        TotalCost = totalCost,
                        EfficiencyLPerLeg = efficiencyLPerLeg,
                        EfficiencyTPerLeg = efficiencyTPerLeg,
                        EfficiencyLPerKm = efficiencyLPerKm,
                        CostPerLeg = costPerLeg,
                        CostPerKm = costPerKm
                    };
                })
                .Where(v => v.EfficiencyLPerLeg > 0)
                .OrderBy(v => v.EfficiencyLPerLeg)
                .ToList();

            // Calculate benchmarks for scoring
            var avgEfficiency = vesselPerformance.Any() ? vesselPerformance.Average(v => v.EfficiencyLPerLeg) : 0;

            var result = vesselPerformance.Select((v, index) =>
            {
                var benchmarkDeviation = avgEfficiency > 0 ? ((v.EfficiencyLPerLeg - avgEfficiency) / avgEfficiency) * 100 : 0;

                var optimizationScore = CalculateOptimizationScore(v.EfficiencyLPerLeg, avgEfficiency, v.CostPerLeg);

                var grade = optimizationScore switch
                {
                    >= 90 => "A",
                    >= 80 => "B",
                    >= 70 => "C",
                    >= 60 => "D",
                    _ => "F"
                };

                var recommendedAction = grade switch
                {
                    "A" => "Maintain excellence, share best practices",
                    "B" => "Good performance, minor optimizations possible",
                    "C" => "Moderate improvements needed",
                    "D" => "Significant optimization required",
                    _ => "Urgent performance review needed"
                };

                return new VesselRoutePerformance
                {
                    VesselName = v.VesselName,
                    VesselType = v.VesselType,
                    Route = v.Route,
                    EstimatedDistanceKm = v.EstimatedDistance,
                    LegsCompleted = v.TotalLegs,
                    TotalFuelL = v.TotalFuelL,
                    TotalFuelT = v.TotalFuelT,
                    EfficiencyLPerLeg = v.EfficiencyLPerLeg,
                    EfficiencyTPerLeg = v.EfficiencyTPerLeg,
                    EfficiencyLPerKm = v.EfficiencyLPerKm,
                    TotalCostUSD = v.TotalCost,
                    CostPerLeg = v.CostPerLeg,
                    CostPerKm = v.CostPerKm,
                    RouteRank = index + 1,
                    PerformanceGrade = grade,
                    RouteOptimizationScore = optimizationScore,
                    RecommendedAction = recommendedAction,
                    BenchmarkDeviationPercent = benchmarkDeviation
                };
            }).ToList();

            return result;
        }

        #endregion

        #region Private Methods - Efficiency Trends

        private List<RouteEfficiencyTrend> GenerateEfficiencyTrends(List<Consumption> consumptions)
        {
            var monthlyTrends = consumptions
                .GroupBy(c => new { c.Month, c.Vessel.Route, c.Vessel.Type })
                .Select(g =>
                {
                    var monthParts = g.Key.Month.Split('-');
                    var year = int.Parse(monthParts[0]);
                    var month = int.Parse(monthParts[1]);
                    var monthDate = new DateTime(year, month, 1);

                    var totalLegs = g.Sum(c => c.LegsCompleted);
                    var totalFuelL = g.Sum(c => c.ConsumptionLiters);
                    var totalFuelT = CalculateConsumptionTonsForRoute(g.ToList());

                    var estimatedDistance = g.Key.Type == "Vessel" ? 520m : 340m;
                    var avgEfficiencyLPerLeg = totalLegs > 0 ? totalFuelL / totalLegs : 0;
                    var avgEfficiencyTPerLeg = totalLegs > 0 ? totalFuelT / totalLegs : 0;
                    var avgEfficiencyLPerKm = (totalLegs * estimatedDistance) > 0 ? totalFuelL / (totalLegs * estimatedDistance) : 0;

                    return new
                    {
                        Month = g.Key.Month,
                        MonthDate = monthDate,
                        RouteType = g.Key.Route,
                        AvgEfficiencyLPerLeg = avgEfficiencyLPerLeg,
                        AvgEfficiencyTPerLeg = avgEfficiencyTPerLeg,
                        AvgEfficiencyLPerKm = avgEfficiencyLPerKm,
                        TotalLegs = totalLegs,
                        TotalFuelL = totalFuelL
                    };
                })
                .Where(t => t.AvgEfficiencyLPerLeg > 0)
                .OrderBy(t => t.MonthDate)
                .ThenBy(t => t.RouteType)
                .ToList();

            // Group by route type to calculate month-over-month changes
            var result = new List<RouteEfficiencyTrend>();

            foreach (var routeGroup in monthlyTrends.GroupBy(t => t.RouteType))
            {
                var routeTrends = routeGroup.OrderBy(t => t.MonthDate).ToList();

                for (int i = 0; i < routeTrends.Count; i++)
                {
                    var current = routeTrends[i];
                    var previous = i > 0 ? routeTrends[i - 1] : null;

                    var momChangeL = previous != null && previous.AvgEfficiencyLPerLeg > 0
                        ? ((current.AvgEfficiencyLPerLeg - previous.AvgEfficiencyLPerLeg) / previous.AvgEfficiencyLPerLeg) * 100
                        : 0;

                    var momChangeT = previous != null && previous.AvgEfficiencyTPerLeg > 0
                        ? ((current.AvgEfficiencyTPerLeg - previous.AvgEfficiencyTPerLeg) / previous.AvgEfficiencyTPerLeg) * 100
                        : 0;

                    var trendDirection = Math.Abs(momChangeL) <= 5 ? "Stable" :
                                       momChangeL < 0 ? "Improving" : "Declining"; // Lower consumption = better efficiency

                    var seasonalAdjustment = CalculateSeasonalAdjustment(current.MonthDate);
                    var volatility = CalculateEfficiencyVolatility(routeTrends.Take(i + 1).Select(t => t.AvgEfficiencyLPerLeg).ToList());

                    var optimality = current.AvgEfficiencyLPerLeg switch
                    {
                        <= 200 => "Optimal",
                        <= 300 => "Good",
                        <= 400 => "Acceptable",
                        _ => "Sub-optimal"
                    };

                    result.Add(new RouteEfficiencyTrend
                    {
                        Month = current.Month,
                        MonthDate = current.MonthDate,
                        RouteType = current.RouteType,
                        AvgEfficiencyLPerLeg = current.AvgEfficiencyLPerLeg,
                        AvgEfficiencyTPerLeg = current.AvgEfficiencyTPerLeg,
                        AvgEfficiencyLPerKm = current.AvgEfficiencyLPerKm,
                        TotalLegs = current.TotalLegs,
                        TotalFuelL = current.TotalFuelL,
                        MonthOverMonthChangeL = momChangeL,
                        MonthOverMonthChangeT = momChangeT,
                        SeasonalAdjustment = seasonalAdjustment,
                        TrendDirection = trendDirection,
                        EfficiencyVolatility = volatility,
                        OptimalityIndicator = optimality
                    });
                }
            }

            return result;
        }

        #endregion

        #region Private Methods - Cost Analysis

        private List<RouteCostAnalysis> GenerateCostAnalysis(List<Consumption> consumptions)
        {
            var costAnalysis = consumptions
                .GroupBy(c => c.Vessel.Route)
                .Select(g =>
                {
                    var totalLegs = g.Sum(c => c.LegsCompleted);
                    var totalFuelL = g.Sum(c => c.ConsumptionLiters);
                    var totalFuelT = CalculateConsumptionTonsForRoute(g.ToList());
                    var totalCost = g.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD);

                    var routeType = g.First().Vessel.Type;
                    var estimatedDistance = routeType == "Vessel" ? 520m : 340m;
                    var totalKm = totalLegs * estimatedDistance;

                    var avgCostPerLeg = totalLegs > 0 ? totalCost / totalLegs : 0;
                    var avgCostPerKm = totalKm > 0 ? totalCost / totalKm : 0;
                    var avgCostPerLiter = totalFuelL > 0 ? totalCost / totalFuelL : 0;
                    var avgCostPerTon = totalFuelT > 0 ? totalCost / totalFuelT : 0;

                    // Calculate cost breakdown (simplified)
                    var fuelCostPercentage = 85m; // Assume 85% of cost is fuel
                    var operationalCostPercentage = 15m; // Assume 15% is operational

                    // Calculate cost variability
                    var monthlyCosts = g.GroupBy(c => c.Month)
                        .Select(mg => mg.Sum(c => c.LegsCompleted) > 0 ? mg.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD) / mg.Sum(c => c.LegsCompleted) : 0)
                        .Where(cost => cost > 0)
                        .ToList();

                    var costVariability = CalculateVolatility(monthlyCosts);

                    var costCategory = avgCostPerLeg switch
                    {
                        <= 1000 => "Low Cost",
                        <= 2000 => "Moderate Cost",
                        <= 3000 => "High Cost",
                        _ => "Very High Cost"
                    };

                    var costDrivers = fuelCostPercentage > 80 ? "Fuel Dominant" :
                                    fuelCostPercentage > 60 ? "Fuel Primary" : "Mixed Drivers";

                    return new RouteCostAnalysis
                    {
                        RouteType = g.Key,
                        TotalRouteCostUSD = totalCost,
                        AvgCostPerLeg = avgCostPerLeg,
                        AvgCostPerKm = avgCostPerKm,
                        AvgCostPerLiter = avgCostPerLiter,
                        AvgCostPerTon = avgCostPerTon,
                        FuelCostPercentage = fuelCostPercentage,
                        OperationalCostPercentage = operationalCostPercentage,
                        CostVariabilityIndex = costVariability,
                        CostCategory = costCategory,
                        CostDrivers = costDrivers,
                        CostEfficiencyRank = 0, // Will be set below
                        BenchmarkCostAdvantage = 0, // Will be calculated below
                        CostOptimizationPotential = 0 // Will be calculated below
                    };
                })
                .OrderBy(c => c.AvgCostPerLeg)
                .ToList();

            // Calculate rankings and benchmarks
            var avgCostPerLeg = costAnalysis.Any() ? costAnalysis.Average(c => c.AvgCostPerLeg) : 0;

            for (int i = 0; i < costAnalysis.Count; i++)
            {
                var cost = costAnalysis[i];
                cost.CostEfficiencyRank = i + 1;
                cost.BenchmarkCostAdvantage = avgCostPerLeg > 0 ? ((avgCostPerLeg - cost.AvgCostPerLeg) / avgCostPerLeg) * 100 : 0;

                // Calculate optimization potential (difference from best performer)
                var bestCost = costAnalysis.First().AvgCostPerLeg;
                var totalLegsForRoute = consumptions.Where(c => c.Vessel.Route == cost.RouteType).Sum(c => c.LegsCompleted);
                cost.CostOptimizationPotential = totalLegsForRoute > 0 ? (cost.AvgCostPerLeg - bestCost) * totalLegsForRoute : 0;
            }

            return costAnalysis;
        }

        #endregion

        #region Private Methods - Optimization Recommendations

        private List<RouteOptimizationRecommendation> GenerateOptimizationRecommendations(List<Consumption> consumptions)
        {
            var recommendations = new List<RouteOptimizationRecommendation>();

            // Analyze route performance for recommendations
            var routePerformance = consumptions
                .GroupBy(c => c.Vessel.Route)
                .Select(g => new
                {
                    Route = g.Key,
                    AvgEfficiency = g.Sum(c => c.LegsCompleted) > 0 ? g.Sum(c => c.ConsumptionLiters) / g.Sum(c => c.LegsCompleted) : 0,
                    TotalCost = g.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD),
                    TotalLegs = g.Sum(c => c.LegsCompleted),
                    VesselCount = g.Select(c => c.VesselId).Distinct().Count()
                })
                .ToList();

            var bestEfficiency = routePerformance.Any() ? routePerformance.Min(r => r.AvgEfficiency) : 0;

            foreach (var route in routePerformance)
            {
                if (route.AvgEfficiency > bestEfficiency * 1.1m) // 10% worse than best
                {
                    var potentialSavings = (route.AvgEfficiency - bestEfficiency) * route.TotalLegs * 0.5m; // Estimated fuel cost per liter
                    var efficiencyImprovement = bestEfficiency > 0 ? ((route.AvgEfficiency - bestEfficiency) / route.AvgEfficiency) * 100 : 0;

                    recommendations.Add(new RouteOptimizationRecommendation
                    {
                        RouteType = route.Route,
                        RecommendationType = "Efficiency Improvement",
                        RecommendationTitle = "Optimize Fuel Consumption",
                        RecommendationDescription = $"Route shows {efficiencyImprovement:N1}% higher fuel consumption than best performing route. Consider optimizing speed, maintenance schedules, and operational procedures.",
                        PotentialSavingsUSD = potentialSavings,
                        EfficiencyImprovementPercent = efficiencyImprovement,
                        ImplementationComplexity = "Medium",
                        TimeToImplement = "3-6 months",
                        Priority = efficiencyImprovement > 20 ? "High" : "Medium",
                        ROIEstimate = potentialSavings > 0 ? (potentialSavings / (route.TotalCost * 0.1m)) * 100 : 0, // 10% implementation cost estimate
                        RequiredResources = "Operations team, maintenance crew, training",
                        RiskLevel = "Low"
                    });
                }

                // Fleet utilization recommendation
                if (route.VesselCount > 1)
                {
                    recommendations.Add(new RouteOptimizationRecommendation
                    {
                        RouteType = route.Route,
                        RecommendationType = "Fleet Optimization",
                        RecommendationTitle = "Optimize Fleet Allocation",
                        RecommendationDescription = $"Route has {route.VesselCount} vessels. Analyze load balancing and consider consolidating operations on most efficient vessels.",
                        PotentialSavingsUSD = route.TotalCost * 0.05m, // 5% potential savings
                        EfficiencyImprovementPercent = 5,
                        ImplementationComplexity = "High",
                        TimeToImplement = "6-12 months",
                        Priority = "Medium",
                        ROIEstimate = 50,
                        RequiredResources = "Fleet management, scheduling system, operations analysis",
                        RiskLevel = "Medium"
                    });
                }
            }

            // General recommendations
            recommendations.Add(new RouteOptimizationRecommendation
            {
                RouteType = "All Routes",
                RecommendationType = "Technology Implementation",
                RecommendationTitle = "Implement Route Optimization Software",
                RecommendationDescription = "Deploy advanced route planning and real-time optimization tools to improve fuel efficiency and reduce operational costs across all routes.",
                PotentialSavingsUSD = routePerformance.Sum(r => r.TotalCost) * 0.08m, // 8% potential savings
                EfficiencyImprovementPercent = 8,
                ImplementationComplexity = "High",
                TimeToImplement = "12-18 months",
                Priority = "High",
                ROIEstimate = 200,
                RequiredResources = "IT infrastructure, software licensing, training, integration support",
                RiskLevel = "Medium"
            });

            return recommendations.OrderByDescending(r => r.PotentialSavingsUSD).ToList();
        }

        #endregion

        #region Private Methods - Profitability Analysis

        private List<RouteProfitabilityAnalysis> GenerateProfitabilityAnalysis(List<Consumption> consumptions)
        {
            // Note: This is a simplified profitability analysis
            // In a real scenario, revenue data would come from actual booking/pricing systems

            var profitabilityAnalysis = consumptions
                .GroupBy(c => c.Vessel.Route)
                .Select(g =>
                {
                    var totalLegs = g.Sum(c => c.LegsCompleted);
                    var totalCost = g.SelectMany(c => c.Allocations).Sum(a => a.AllocatedValueUSD);

                    // Estimate revenue based on route type and legs (simplified calculation)
                    var routeType = g.First().Vessel.Type;
                    var revenuePerLeg = routeType == "Vessel" ? 5000m : 3000m; // Estimated revenue per leg
                    var totalRevenue = totalLegs * revenuePerLeg;

                    var netProfit = totalRevenue - totalCost;
                    var profitMargin = totalRevenue > 0 ? (netProfit / totalRevenue) * 100 : 0;
                    var revenuePerKm = routeType == "Vessel" ? totalRevenue / (totalLegs * 520m) : totalRevenue / (totalLegs * 340m);
                    var profitPerLeg = totalLegs > 0 ? netProfit / totalLegs : 0;
                    var profitPerKm = routeType == "Vessel" ? netProfit / (totalLegs * 520m) : netProfit / (totalLegs * 340m);
                    var roi = totalCost > 0 ? (netProfit / totalCost) * 100 : 0;

                    // Simplified payback calculation
                    var monthlyProfit = netProfit / 12; // Assume annual data spread over 12 months
                    var paybackPeriod = monthlyProfit > 0 ? totalCost / monthlyProfit : 0;

                    var profitabilityTrend = profitMargin switch
                    {
                        >= 20 => "Excellent",
                        >= 15 => "Good",
                        >= 10 => "Acceptable",
                        >= 5 => "Marginal",
                        _ => "Poor"
                    };

                    // Break-even analysis
                    var fixedCostEstimate = totalCost * 0.3m; // Estimate 30% fixed costs
                    var variableCostPerLeg = totalLegs > 0 ? (totalCost * 0.7m) / totalLegs : 0;
                    var contributionPerLeg = revenuePerLeg - variableCostPerLeg;
                    var breakEvenLegs = contributionPerLeg > 0 ? fixedCostEstimate / contributionPerLeg : 0;

                    return new RouteProfitabilityAnalysis
                    {
                        RouteType = g.Key,
                        TotalRevenueUSD = totalRevenue,
                        TotalCostUSD = totalCost,
                        NetProfitUSD = netProfit,
                        ProfitMarginPercent = profitMargin,
                        RevenuePerLeg = revenuePerLeg,
                        RevenuePerKm = revenuePerKm,
                        ProfitPerLeg = profitPerLeg,
                        ProfitPerKm = profitPerKm,
                        ROIPercent = roi,
                        PaybackPeriodMonths = Math.Min(120, paybackPeriod), // Cap at 10 years
                        ProfitabilityTrend = profitabilityTrend,
                        BreakEvenLegsRequired = breakEvenLegs,
                        ProfitabilityRank = "" // Will be set below
                    };
                })
                .OrderByDescending(p => p.ProfitMarginPercent)
                .ToList();

            // Set profitability rankings
            for (int i = 0; i < profitabilityAnalysis.Count; i++)
            {
                profitabilityAnalysis[i].ProfitabilityRank = (i + 1).ToString();
            }

            return profitabilityAnalysis;
        }

        #endregion

        #region Private Helper Methods

        private decimal CalculateTotalConsumptionTons(List<Consumption> consumptions)
        {
            // Use average density for tons calculation (simplified approach)
            var avgDensity = 0.85m; // Typical marine fuel density
            return consumptions.Sum(c => (c.ConsumptionLiters / 1000) * avgDensity);
        }

        private decimal CalculateConsumptionTonsForRoute(List<Consumption> consumptions)
        {
            var avgDensity = 0.85m;
            return consumptions.Sum(c => (c.ConsumptionLiters / 1000) * avgDensity);
        }

        private decimal CalculateConsumptionTonsForVessel(List<Consumption> consumptions)
        {
            var avgDensity = 0.85m;
            return consumptions.Sum(c => (c.ConsumptionLiters / 1000) * avgDensity);
        }

        private decimal CalculateTotalRouteDistance(List<Consumption> consumptions, decimal vesselDistance, decimal boatDistance)
        {
            var totalDistance = 0m;
            foreach (var consumption in consumptions)
            {
                var distance = consumption.Vessel.Type == "Vessel" ? vesselDistance : boatDistance;
                totalDistance += consumption.LegsCompleted * distance;
            }
            return totalDistance;
        }

        private decimal CalculateOptimizationScore(decimal efficiency, decimal avgEfficiency, decimal cost)
        {
            var efficiencyScore = avgEfficiency > 0 ? Math.Max(0, 100 - ((efficiency - avgEfficiency) / avgEfficiency * 100)) : 75;
            var costScore = cost < 2000 ? 90 : cost < 3000 ? 75 : cost < 4000 ? 60 : 40;
            return (efficiencyScore + costScore) / 2;
        }

        private decimal CalculateSeasonalAdjustment(DateTime monthDate)
        {
            // Simplified seasonal adjustment based on month
            return monthDate.Month switch
            {
                12 or 1 or 2 => 1.05m, // Winter - higher consumption
                3 or 4 or 5 => 0.98m,   // Spring - optimal conditions
                6 or 7 or 8 => 1.02m,   // Summer - moderate increase
                _ => 1.00m              // Fall - baseline
            };
        }

        private decimal CalculateEfficiencyVolatility(List<decimal> efficiencies)
        {
            if (efficiencies.Count < 2) return 0;

            var mean = efficiencies.Average();
            var sumSquaredDiffs = efficiencies.Sum(e => (e - mean) * (e - mean));
            var variance = sumSquaredDiffs / (efficiencies.Count - 1);
            var standardDeviation = (decimal)Math.Sqrt((double)variance);

            return mean > 0 ? (standardDeviation / mean) * 100 : 0;
        }

        private decimal CalculateVolatility(List<decimal> values)
        {
            if (values.Count < 2) return 0;

            var mean = values.Average();
            var sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));
            var variance = sumSquaredDiffs / (values.Count - 1);
            var standardDeviation = (decimal)Math.Sqrt((double)variance);

            return mean > 0 ? (standardDeviation / mean) * 100 : 0;
        }

        #endregion
    }
}