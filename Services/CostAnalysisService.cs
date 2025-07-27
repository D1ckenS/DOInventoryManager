using DOInventoryManager.Data;
using DOInventoryManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DOInventoryManager.Services
{
    public class CostAnalysisService
    {
        #region Data Models

        public class CostAnalysisResult
        {
            public CostAnalysisOverview Overview { get; set; } = new();
            public List<PriceTrendAnalysis> PriceTrends { get; set; } = [];
            public List<SupplierCostComparison> SupplierComparison { get; set; } = [];
            public List<CostVarianceAnalysis> CostVariance { get; set; } = [];
            public List<ProcurementEfficiencyMetric> ProcurementEfficiency { get; set; } = [];
            public List<MarketBenchmarkAnalysis> MarketBenchmarking { get; set; } = [];
        }

        public class CostAnalysisOverview
        {
            public decimal TotalProcurementValueUSD { get; set; }
            public decimal AvgCostPerLiterUSD { get; set; }
            public decimal AvgCostPerTonUSD { get; set; }
            public int TotalPurchaseTransactions { get; set; }
            public int UniqueSuppliersUsed { get; set; }
            public decimal PriceVolatilityIndex { get; set; }
            public string BestPerformingSupplier { get; set; } = string.Empty;
            public string WorstPerformingSupplier { get; set; } = string.Empty;
            public decimal CostSavingsOpportunityUSD { get; set; }
            public decimal ProcurementEfficiencyScore { get; set; }
            public decimal LowestCostPerLiterUSD { get; set; }
            public decimal HighestCostPerLiterUSD { get; set; }
            public string MostCostEfficientMonth { get; set; } = string.Empty;
            public string LeastCostEfficientMonth { get; set; } = string.Empty;

            // Formatted properties
            public string FormattedTotalProcurement => TotalProcurementValueUSD < 0 ? $"({Math.Abs(TotalProcurementValueUSD):C2})" : TotalProcurementValueUSD.ToString("C2");
            public string FormattedCostSavings => CostSavingsOpportunityUSD < 0 ? $"({Math.Abs(CostSavingsOpportunityUSD):C2})" : CostSavingsOpportunityUSD.ToString("C2");
            public string FormattedProcurementScore => $"{ProcurementEfficiencyScore:N1}%";
        }

        public class PriceTrendAnalysis
        {
            public string Month { get; set; } = string.Empty;
            public DateTime MonthDate { get; set; }
            public decimal AvgCostPerLiterUSD { get; set; }
            public decimal AvgCostPerTonUSD { get; set; }
            public decimal TotalVolumeL { get; set; }
            public decimal TotalValueUSD { get; set; }
            public int TransactionCount { get; set; }
            public decimal PriceVarianceFromPrevious { get; set; }
            public decimal VolumeVarianceFromPrevious { get; set; }
            public string TrendDirection { get; set; } = string.Empty;
            public decimal MarketVolatility { get; set; }
            public string BestSupplierThisMonth { get; set; } = string.Empty;
            public decimal BestPriceThisMonth { get; set; }
            public string WorstSupplierThisMonth { get; set; } = string.Empty;
            public decimal WorstPriceThisMonth { get; set; }

            // Formatted properties
            public string FormattedMonth => MonthDate.ToString("MMM yyyy");
            public string FormattedTotalValue => TotalValueUSD < 0 ? $"({Math.Abs(TotalValueUSD):C2})" : TotalValueUSD.ToString("C2");
            public string FormattedPriceVariance => PriceVarianceFromPrevious < 0 ? $"({Math.Abs(PriceVarianceFromPrevious):N2}%)" : $"{PriceVarianceFromPrevious:N2}%";
            public string FormattedVolumeVariance => VolumeVarianceFromPrevious < 0 ? $"({Math.Abs(VolumeVarianceFromPrevious):N2}%)" : $"{VolumeVarianceFromPrevious:N2}%";
            public string TrendIcon => TrendDirection switch
            {
                "Increasing" => "▲",
                "Decreasing" => "▼",
                "Stable" => "→",
                _ => "●"
            };
        }

        public class SupplierCostComparison
        {
            public string SupplierName { get; set; } = string.Empty;
            public string Currency { get; set; } = string.Empty;
            public decimal AvgCostPerLiterUSD { get; set; }
            public decimal AvgCostPerTonUSD { get; set; }
            public decimal AvgCostPerLiterOriginal { get; set; }
            public decimal TotalVolumeL { get; set; }
            public decimal TotalValueUSD { get; set; }
            public decimal MarketSharePercent { get; set; }
            public int TransactionCount { get; set; }
            public decimal PriceConsistencyScore { get; set; }
            public decimal CostCompetitivenessRank { get; set; }
            public decimal PriceVariabilityIndex { get; set; }
            public DateTime EarliestPurchase { get; set; }
            public DateTime LatestPurchase { get; set; }
            public decimal CostAdvantageVsMarket { get; set; }
            public string PerformanceCategory { get; set; } = string.Empty;
            public decimal PaymentReliabilityScore { get; set; }

            // Formatted properties
            public string FormattedAvgCostOriginal => Currency == "USD" ? AvgCostPerLiterOriginal.ToString("C6") : $"{AvgCostPerLiterOriginal:N6} {Currency}";
            public string FormattedTotalValue => TotalValueUSD < 0 ? $"({Math.Abs(TotalValueUSD):C2})" : TotalValueUSD.ToString("C2");
            public string FormattedMarketShare => $"{MarketSharePercent:N1}%";
            public string FormattedCostAdvantage => CostAdvantageVsMarket < 0 ? $"({Math.Abs(CostAdvantageVsMarket):N2}%)" : $"{CostAdvantageVsMarket:N2}%";
            public string FormattedPriceConsistency => $"{PriceConsistencyScore:N1}%";
        }

        public class CostVarianceAnalysis
        {
            public string Month { get; set; } = string.Empty;
            public DateTime MonthDate { get; set; }
            public decimal BudgetedCostPerLiterUSD { get; set; }
            public decimal ActualCostPerLiterUSD { get; set; }
            public decimal VarianceAmountUSD { get; set; }
            public decimal VariancePercentage { get; set; }
            public decimal BudgetedVolumeL { get; set; }
            public decimal ActualVolumeL { get; set; }
            public decimal VolumeVarianceL { get; set; }
            public decimal TotalBudgetUSD { get; set; }
            public decimal TotalActualUSD { get; set; }
            public decimal TotalVarianceUSD { get; set; }
            public string VarianceCategory { get; set; } = string.Empty;
            public string PrimaryVarianceDriver { get; set; } = string.Empty;
            public decimal CostInflationRate { get; set; }

            // Formatted properties
            public string FormattedMonth => MonthDate.ToString("MMM yyyy");
            public string FormattedVarianceAmount => VarianceAmountUSD < 0 ? $"({Math.Abs(VarianceAmountUSD):C6})" : VarianceAmountUSD.ToString("C6");
            public string FormattedVariancePercentage => VariancePercentage < 0 ? $"({Math.Abs(VariancePercentage):N2}%)" : $"{VariancePercentage:N2}%";
            public string FormattedTotalVariance => TotalVarianceUSD < 0 ? $"({Math.Abs(TotalVarianceUSD):C2})" : TotalVarianceUSD.ToString("C2");
            public string FormattedVolumeVariance => VolumeVarianceL < 0 ? $"({Math.Abs(VolumeVarianceL):N3})" : VolumeVarianceL.ToString("N3");
            public string VarianceIcon => VarianceCategory switch
            {
                "Favorable" => "✅",
                "Unfavorable" => "⚠️",
                "Within Range" => "→",
                _ => "●"
            };
        }

        public class ProcurementEfficiencyMetric
        {
            public string SupplierName { get; set; } = string.Empty;
            public decimal EfficiencyScore { get; set; }
            public decimal CostOptimizationRating { get; set; }
            public decimal PriceNegotiationScore { get; set; }
            public decimal VolumeEfficiencyScore { get; set; }
            public decimal TimingEfficiencyScore { get; set; }
            public decimal QualityConsistencyScore { get; set; }
            public decimal PaymentTermsScore { get; set; }
            public decimal RiskAssessmentScore { get; set; }
            public int OptimalPurchaseCount { get; set; }
            public int SubOptimalPurchaseCount { get; set; }
            public decimal PotentialSavingsUSD { get; set; }
            public string RecommendedAction { get; set; } = string.Empty;
            public string EfficiencyGrade { get; set; } = string.Empty;
            public decimal BenchmarkPosition { get; set; }

            // Formatted properties
            public string FormattedEfficiencyScore => $"{EfficiencyScore:N1}%";
            public string FormattedPotentialSavings => PotentialSavingsUSD < 0 ? $"({Math.Abs(PotentialSavingsUSD):C2})" : PotentialSavingsUSD.ToString("C2");
            public string FormattedBenchmarkPosition => $"{BenchmarkPosition:N1}%";
            public string EfficiencyIcon => EfficiencyGrade switch
            {
                "A" => "🥇",
                "B" => "🥈",
                "C" => "🥉",
                "D" => "⚠️",
                "F" => "❌",
                _ => "●"
            };
        }

        public class MarketBenchmarkAnalysis
        {
            public string Period { get; set; } = string.Empty;
            public DateTime PeriodDate { get; set; }
            public decimal MarketAverageCostUSD { get; set; }
            public decimal CompanyAverageCostUSD { get; set; }
            public decimal MarketPositionPercentile { get; set; }
            public decimal CostAdvantageUSD { get; set; }
            public decimal CompetitiveIndexScore { get; set; }
            public string MarketPerformanceCategory { get; set; } = string.Empty;
            public decimal PriceVolatilityVsMarket { get; set; }
            public decimal VolumeShareEstimate { get; set; }
            public string BenchmarkStatus { get; set; } = string.Empty;
            public decimal OptimizationOpportunityUSD { get; set; }
            public string StrategicRecommendation { get; set; } = string.Empty;
            public decimal MarketTrendAlignment { get; set; }
            public string RiskLevel { get; set; } = string.Empty;

            // Formatted properties
            public string FormattedPeriod => PeriodDate.ToString("MMM yyyy");
            public string FormattedMarketPosition => $"{MarketPositionPercentile:N1}%";
            public string FormattedCostAdvantage => CostAdvantageUSD < 0 ? $"({Math.Abs(CostAdvantageUSD):C6})" : CostAdvantageUSD.ToString("C6");
            public string FormattedOptimizationOpportunity => OptimizationOpportunityUSD < 0 ? $"({Math.Abs(OptimizationOpportunityUSD):C2})" : OptimizationOpportunityUSD.ToString("C2");
            public string BenchmarkIcon => BenchmarkStatus switch
            {
                "Leading" => "🎯",
                "Competitive" => "✅",
                "Below Average" => "⚠️",
                "Underperforming" => "❌",
                _ => "●"
            };
        }

        #endregion

        #region Main Service Method

        public async Task<CostAnalysisResult> GenerateCostAnalysisAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var context = new InventoryContext();

            // Default to last 12 months if no dates provided
            var endDate = toDate ?? DateTime.Today;
            var startDate = fromDate ?? endDate.AddMonths(-12);

            // Get all purchases in date range
            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Vessel)
                .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate)
                .OrderBy(p => p.PurchaseDate)
                .ToListAsync();

            if (!purchases.Any())
            {
                return new CostAnalysisResult(); // Return empty result
            }

            var result = new CostAnalysisResult
            {
                Overview = GenerateOverview(purchases),
                PriceTrends = GeneratePriceTrends(purchases),
                SupplierComparison = GenerateSupplierComparison(purchases),
                CostVariance = GenerateCostVarianceAnalysis(purchases),
                ProcurementEfficiency = GenerateProcurementEfficiency(purchases),
                MarketBenchmarking = GenerateMarketBenchmarking(purchases)
            };

            return result;
        }

        #endregion

        #region Private Methods - Overview

        private CostAnalysisOverview GenerateOverview(List<Purchase> purchases)
        {
            var totalValue = purchases.Sum(p => p.TotalValueUSD);
            var totalVolume = purchases.Sum(p => p.QuantityLiters);
            var avgCostPerLiter = totalVolume > 0 ? totalValue / totalVolume : 0;
            var totalTons = purchases.Sum(p => p.QuantityTons);
            var avgCostPerTon = totalTons > 0 ? totalValue / totalTons : 0;

            // Calculate price volatility
            var monthlyCosts = purchases
                .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
                .Select(g => g.Sum(p => p.QuantityLiters) > 0 ? g.Sum(p => p.TotalValueUSD) / g.Sum(p => p.QuantityLiters) : 0)
                .Where(cost => cost > 0)
                .ToList();

            var volatility = CalculateVolatility(monthlyCosts);

            // Find best and worst suppliers
            var supplierPerformance = purchases
                .GroupBy(p => p.Supplier.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    AvgCost = g.Sum(p => p.QuantityLiters) > 0 ? g.Sum(p => p.TotalValueUSD) / g.Sum(p => p.QuantityLiters) : 0
                })
                .Where(s => s.AvgCost > 0)
                .OrderBy(s => s.AvgCost)
                .ToList();

            var bestSupplier = supplierPerformance.FirstOrDefault()?.Name ?? "N/A";
            var worstSupplier = supplierPerformance.LastOrDefault()?.Name ?? "N/A";

            // Calculate cost savings opportunity (difference between worst and best average costs)
            var costSavings = supplierPerformance.Any() && supplierPerformance.Count > 1
                ? (supplierPerformance.Last().AvgCost - supplierPerformance.First().AvgCost) * totalVolume
                : 0;

            // Calculate procurement efficiency score
            var procurementScore = CalculateProcurementEfficiencyScore(purchases);

            // Find lowest and highest costs
            var costs = purchases.Where(p => p.QuantityLiters > 0).Select(p => p.TotalValueUSD / p.QuantityLiters).ToList();
            var lowestCost = costs.Any() ? costs.Min() : 0;
            var highestCost = costs.Any() ? costs.Max() : 0;

            // Find most and least cost efficient months
            var monthlyEfficiency = purchases
                .GroupBy(p => $"{p.PurchaseDate:yyyy-MM}")
                .Select(g => new
                {
                    Month = g.Key,
                    AvgCost = g.Sum(p => p.QuantityLiters) > 0 ? g.Sum(p => p.TotalValueUSD) / g.Sum(p => p.QuantityLiters) : 0
                })
                .Where(m => m.AvgCost > 0)
                .OrderBy(m => m.AvgCost)
                .ToList();

            var mostEfficient = monthlyEfficiency.FirstOrDefault()?.Month ?? "N/A";
            var leastEfficient = monthlyEfficiency.LastOrDefault()?.Month ?? "N/A";

            return new CostAnalysisOverview
            {
                TotalProcurementValueUSD = totalValue,
                AvgCostPerLiterUSD = avgCostPerLiter,
                AvgCostPerTonUSD = avgCostPerTon,
                TotalPurchaseTransactions = purchases.Count,
                UniqueSuppliersUsed = purchases.Select(p => p.SupplierId).Distinct().Count(),
                PriceVolatilityIndex = volatility,
                BestPerformingSupplier = bestSupplier,
                WorstPerformingSupplier = worstSupplier,
                CostSavingsOpportunityUSD = costSavings,
                ProcurementEfficiencyScore = procurementScore,
                LowestCostPerLiterUSD = lowestCost,
                HighestCostPerLiterUSD = highestCost,
                MostCostEfficientMonth = mostEfficient,
                LeastCostEfficientMonth = leastEfficient
            };
        }

        #endregion

        #region Private Methods - Price Trends

        private List<PriceTrendAnalysis> GeneratePriceTrends(List<Purchase> purchases)
        {
            var monthlyData = purchases
                .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
                .Select(g =>
                {
                    var monthDate = new DateTime(g.Key.Year, g.Key.Month, 1);
                    var totalVolume = g.Sum(p => p.QuantityLiters);
                    var totalValue = g.Sum(p => p.TotalValueUSD);
                    var avgCostPerLiter = totalVolume > 0 ? totalValue / totalVolume : 0;
                    var totalTons = g.Sum(p => p.QuantityTons);
                    var avgCostPerTon = totalTons > 0 ? totalValue / totalTons : 0;

                    // Find best and worst suppliers this month
                    var supplierPrices = g
                        .GroupBy(p => p.Supplier.Name)
                        .Select(sg => new
                        {
                            Name = sg.Key,
                            AvgPrice = sg.Sum(p => p.QuantityLiters) > 0 ? sg.Sum(p => p.TotalValueUSD) / sg.Sum(p => p.QuantityLiters) : 0
                        })
                        .Where(sp => sp.AvgPrice > 0)
                        .OrderBy(sp => sp.AvgPrice)
                        .ToList();

                    var bestSupplier = supplierPrices.FirstOrDefault();
                    var worstSupplier = supplierPrices.LastOrDefault();

                    // Calculate market volatility for this month
                    var supplierCosts = supplierPrices.Select(sp => sp.AvgPrice).ToList();
                    var volatility = CalculateVolatility(supplierCosts);

                    return new PriceTrendAnalysis
                    {
                        Month = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                        MonthDate = monthDate,
                        AvgCostPerLiterUSD = avgCostPerLiter,
                        AvgCostPerTonUSD = avgCostPerTon,
                        TotalVolumeL = totalVolume,
                        TotalValueUSD = totalValue,
                        TransactionCount = g.Count(),
                        MarketVolatility = volatility,
                        BestSupplierThisMonth = bestSupplier?.Name ?? "N/A",
                        BestPriceThisMonth = bestSupplier?.AvgPrice ?? 0,
                        WorstSupplierThisMonth = worstSupplier?.Name ?? "N/A",
                        WorstPriceThisMonth = worstSupplier?.AvgPrice ?? 0
                    };
                })
                .OrderBy(m => m.MonthDate)
                .ToList();

            // Calculate period-over-period changes and trends
            for (int i = 1; i < monthlyData.Count; i++)
            {
                var current = monthlyData[i];
                var previous = monthlyData[i - 1];

                if (previous.AvgCostPerLiterUSD > 0)
                {
                    current.PriceVarianceFromPrevious = ((current.AvgCostPerLiterUSD - previous.AvgCostPerLiterUSD) / previous.AvgCostPerLiterUSD) * 100;
                }

                if (previous.TotalVolumeL > 0)
                {
                    current.VolumeVarianceFromPrevious = ((current.TotalVolumeL - previous.TotalVolumeL) / previous.TotalVolumeL) * 100;
                }

                // Determine trend direction
                current.TrendDirection = current.PriceVarianceFromPrevious switch
                {
                    > 2 => "Increasing",
                    < -2 => "Decreasing",
                    _ => "Stable"
                };
            }

            return monthlyData;
        }

        #endregion

        #region Private Methods - Supplier Comparison

        private List<SupplierCostComparison> GenerateSupplierComparison(List<Purchase> purchases)
        {
            var totalMarketValue = purchases.Sum(p => p.TotalValueUSD);

            var supplierData = purchases
                .GroupBy(p => new { p.SupplierId, p.Supplier.Name, p.Supplier.Currency })
                .Select(g =>
                {
                    var totalVolume = g.Sum(p => p.QuantityLiters);
                    var totalValue = g.Sum(p => p.TotalValueUSD);
                    var totalValueOriginal = g.Sum(p => p.TotalValue);
                    var avgCostPerLiterUSD = totalVolume > 0 ? totalValue / totalVolume : 0;
                    var avgCostPerLiterOriginal = totalVolume > 0 ? totalValueOriginal / totalVolume : 0;
                    var totalTons = g.Sum(p => p.QuantityTons);
                    var avgCostPerTonUSD = totalTons > 0 ? totalValue / totalTons : 0;
                    var marketShare = totalMarketValue > 0 ? (totalValue / totalMarketValue) * 100 : 0;

                    // Calculate price consistency (lower standard deviation = higher consistency)
                    var prices = g.Where(p => p.QuantityLiters > 0).Select(p => p.TotalValueUSD / p.QuantityLiters).ToList();
                    var priceConsistency = CalculatePriceConsistencyScore(prices);
                    var priceVariability = CalculateVolatility(prices);

                    // Calculate payment reliability score
                    var paymentReliability = CalculatePaymentReliabilityScore(g.ToList());

                    return new SupplierCostComparison
                    {
                        SupplierName = g.Key.Name,
                        Currency = g.Key.Currency,
                        AvgCostPerLiterUSD = avgCostPerLiterUSD,
                        AvgCostPerTonUSD = avgCostPerTonUSD,
                        AvgCostPerLiterOriginal = avgCostPerLiterOriginal,
                        TotalVolumeL = totalVolume,
                        TotalValueUSD = totalValue,
                        MarketSharePercent = marketShare,
                        TransactionCount = g.Count(),
                        PriceConsistencyScore = priceConsistency,
                        PriceVariabilityIndex = priceVariability,
                        EarliestPurchase = g.Min(p => p.PurchaseDate),
                        LatestPurchase = g.Max(p => p.PurchaseDate),
                        PaymentReliabilityScore = paymentReliability
                    };
                })
                .OrderBy(s => s.AvgCostPerLiterUSD)
                .ToList();

            // Calculate competitive rankings and market advantages
            var marketAvgCost = supplierData.Any() ? supplierData.Average(s => s.AvgCostPerLiterUSD) : 0;

            for (int i = 0; i < supplierData.Count; i++)
            {
                var supplier = supplierData[i];
                supplier.CostCompetitivenessRank = i + 1;
                supplier.CostAdvantageVsMarket = marketAvgCost > 0 ? ((marketAvgCost - supplier.AvgCostPerLiterUSD) / marketAvgCost) * 100 : 0;

                supplier.PerformanceCategory = supplier.CostCompetitivenessRank switch
                {
                    1 => "Best in Class",
                    <= 3 => "Highly Competitive",
                    <= 5 => "Competitive",
                    <= 8 => "Average",
                    _ => "Below Average"
                };
            }

            return supplierData;
        }

        #endregion

        #region Private Methods - Cost Variance Analysis

        private List<CostVarianceAnalysis> GenerateCostVarianceAnalysis(List<Purchase> purchases)
        {
            // For this implementation, we'll use a rolling 12-month average as "budget"
            // In a real scenario, this would come from actual budget data

            var monthlyActuals = purchases
                .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
                .Select(g =>
                {
                    var monthDate = new DateTime(g.Key.Year, g.Key.Month, 1);
                    var totalVolume = g.Sum(p => p.QuantityLiters);
                    var totalValue = g.Sum(p => p.TotalValueUSD);
                    var actualCostPerLiter = totalVolume > 0 ? totalValue / totalVolume : 0;

                    return new
                    {
                        MonthDate = monthDate,
                        Month = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                        ActualCostPerLiter = actualCostPerLiter,
                        ActualVolume = totalVolume,
                        ActualValue = totalValue
                    };
                })
                .OrderBy(m => m.MonthDate)
                .ToList();

            // Calculate rolling budget baseline (simplified approach)
            var overallAvgCost = monthlyActuals.Any() ? monthlyActuals.Average(m => m.ActualCostPerLiter) : 0;
            var overallAvgVolume = monthlyActuals.Any() ? monthlyActuals.Average(m => m.ActualVolume) : 0;

            var varianceAnalysis = monthlyActuals.Select(m =>
            {
                var budgetedCost = overallAvgCost; // Simplified budget
                var budgetedVolume = overallAvgVolume; // Simplified budget
                var budgetedValue = budgetedCost * budgetedVolume;

                var costVariance = m.ActualCostPerLiter - budgetedCost;
                var costVariancePercent = budgetedCost > 0 ? (costVariance / budgetedCost) * 100 : 0;
                var volumeVariance = m.ActualVolume - budgetedVolume;
                var totalVariance = m.ActualValue - budgetedValue;

                var varianceCategory = Math.Abs(costVariancePercent) switch
                {
                    <= 5 => "Within Range",
                    _ => costVariancePercent < 0 ? "Favorable" : "Unfavorable"
                };

                var primaryDriver = Math.Abs(costVariancePercent) > Math.Abs(volumeVariance / budgetedVolume * 100)
                    ? "Price Variance" : "Volume Variance";

                return new CostVarianceAnalysis
                {
                    Month = m.Month,
                    MonthDate = m.MonthDate,
                    BudgetedCostPerLiterUSD = budgetedCost,
                    ActualCostPerLiterUSD = m.ActualCostPerLiter,
                    VarianceAmountUSD = costVariance,
                    VariancePercentage = costVariancePercent,
                    BudgetedVolumeL = budgetedVolume,
                    ActualVolumeL = m.ActualVolume,
                    VolumeVarianceL = volumeVariance,
                    TotalBudgetUSD = budgetedValue,
                    TotalActualUSD = m.ActualValue,
                    TotalVarianceUSD = totalVariance,
                    VarianceCategory = varianceCategory,
                    PrimaryVarianceDriver = primaryDriver,
                    CostInflationRate = costVariancePercent
                };
            }).ToList();

            return varianceAnalysis;
        }

        #endregion

        #region Private Methods - Procurement Efficiency

        private List<ProcurementEfficiencyMetric> GenerateProcurementEfficiency(List<Purchase> purchases)
        {
            var supplierMetrics = purchases
                .GroupBy(p => p.Supplier.Name)
                .Select(g =>
                {
                    var supplierPurchases = g.ToList();
                    var avgCostPerLiter = supplierPurchases.Sum(p => p.QuantityLiters) > 0
                        ? supplierPurchases.Sum(p => p.TotalValueUSD) / supplierPurchases.Sum(p => p.QuantityLiters) : 0;

                    // Calculate various efficiency scores
                    var costOptimization = CalculateCostOptimizationRating(supplierPurchases);
                    var priceNegotiation = CalculatePriceNegotiationScore(supplierPurchases);
                    var volumeEfficiency = CalculateVolumeEfficiencyScore(supplierPurchases);
                    var timingEfficiency = CalculateTimingEfficiencyScore(supplierPurchases);
                    var qualityConsistency = CalculateQualityConsistencyScore(supplierPurchases);
                    var paymentTerms = CalculatePaymentTermsScore(supplierPurchases);
                    var riskAssessment = CalculateRiskAssessmentScore(supplierPurchases);

                    // Overall efficiency score (weighted average)
                    var efficiencyScore = (costOptimization * 0.3m + priceNegotiation * 0.2m + volumeEfficiency * 0.15m +
                                         timingEfficiency * 0.15m + qualityConsistency * 0.1m + paymentTerms * 0.05m + riskAssessment * 0.05m);

                    // Count optimal vs suboptimal purchases
                    var marketAvg = purchases.Where(p => p.QuantityLiters > 0).Average(p => p.TotalValueUSD / p.QuantityLiters);
                    var optimalCount = supplierPurchases.Count(p => p.QuantityLiters > 0 && (p.TotalValueUSD / p.QuantityLiters) <= marketAvg);
                    var subOptimalCount = supplierPurchases.Count - optimalCount;

                    // Calculate potential savings
                    var potentialSavings = supplierPurchases
                        .Where(p => p.QuantityLiters > 0 && (p.TotalValueUSD / p.QuantityLiters) > marketAvg)
                        .Sum(p => ((p.TotalValueUSD / p.QuantityLiters) - marketAvg) * p.QuantityLiters);

                    var efficiencyGrade = efficiencyScore switch
                    {
                        >= 90 => "A",
                        >= 80 => "B",
                        >= 70 => "C",
                        >= 60 => "D",
                        _ => "F"
                    };

                    var recommendedAction = efficiencyScore switch
                    {
                        >= 85 => "Maintain current strategy",
                        >= 70 => "Monitor and optimize",
                        >= 60 => "Improve procurement process",
                        _ => "Consider alternative suppliers"
                    };

                    return new ProcurementEfficiencyMetric
                    {
                        SupplierName = g.Key,
                        EfficiencyScore = efficiencyScore,
                        CostOptimizationRating = costOptimization,
                        PriceNegotiationScore = priceNegotiation,
                        VolumeEfficiencyScore = volumeEfficiency,
                        TimingEfficiencyScore = timingEfficiency,
                        QualityConsistencyScore = qualityConsistency,
                        PaymentTermsScore = paymentTerms,
                        RiskAssessmentScore = riskAssessment,
                        OptimalPurchaseCount = optimalCount,
                        SubOptimalPurchaseCount = subOptimalCount,
                        PotentialSavingsUSD = potentialSavings,
                        RecommendedAction = recommendedAction,
                        EfficiencyGrade = efficiencyGrade,
                        BenchmarkPosition = efficiencyScore
                    };
                })
                .OrderByDescending(m => m.EfficiencyScore)
                .ToList();

            return supplierMetrics;
        }

        #endregion

        #region Private Methods - Market Benchmarking

        private List<MarketBenchmarkAnalysis> GenerateMarketBenchmarking(List<Purchase> purchases)
        {
            var random = new Random(42); // Use seed for consistent results

            var monthlyBenchmarks = purchases
                .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
                .Select(g =>
                {
                    var monthDate = new DateTime(g.Key.Year, g.Key.Month, 1);
                    var companyAvgCost = g.Sum(p => p.QuantityLiters) > 0 ? g.Sum(p => p.TotalValueUSD) / g.Sum(p => p.QuantityLiters) : 0;

                    // Simulate market average (would come from external data in real scenario)
                    var marketAvgCost = companyAvgCost * (0.95m + (decimal)(random.NextDouble() * 0.1)); // ±5% random variation

                    var costAdvantage = companyAvgCost - marketAvgCost;
                    var marketPosition = marketAvgCost > 0 ? ((marketAvgCost - companyAvgCost) / marketAvgCost) * 100 + 50 : 50;

                    // Constrain percentile to realistic range
                    marketPosition = Math.Max(5, Math.Min(95, marketPosition));

                    var competitiveIndex = (marketPosition - 50) * 2; // Convert to -100 to +100 scale

                    var performanceCategory = marketPosition switch
                    {
                        >= 80 => "Leading",
                        >= 60 => "Competitive",
                        >= 40 => "Below Average",
                        _ => "Underperforming"
                    };

                    var benchmarkStatus = performanceCategory;

                    var optimizationOpportunity = costAdvantage > 0 ? costAdvantage * g.Sum(p => p.QuantityLiters) : 0;

                    var strategicRecommendation = performanceCategory switch
                    {
                        "Leading" => "Maintain competitive advantage and explore market expansion",
                        "Competitive" => "Continue current strategy with minor optimizations",
                        "Below Average" => "Implement cost reduction initiatives and supplier optimization",
                        _ => "Urgent procurement strategy review and supplier diversification required"
                    };

                    var riskLevel = performanceCategory switch
                    {
                        "Leading" => "Low",
                        "Competitive" => "Medium",
                        "Below Average" => "High",
                        _ => "Critical"
                    };

                    return new MarketBenchmarkAnalysis
                    {
                        Period = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                        PeriodDate = monthDate,
                        MarketAverageCostUSD = marketAvgCost,
                        CompanyAverageCostUSD = companyAvgCost,
                        MarketPositionPercentile = marketPosition,
                        CostAdvantageUSD = costAdvantage,
                        CompetitiveIndexScore = competitiveIndex,
                        MarketPerformanceCategory = performanceCategory,
                        PriceVolatilityVsMarket = 0, // Would be calculated with market data
                        VolumeShareEstimate = 0, // Would be calculated with market data
                        BenchmarkStatus = benchmarkStatus,
                        OptimizationOpportunityUSD = optimizationOpportunity,
                        StrategicRecommendation = strategicRecommendation,
                        MarketTrendAlignment = 0, // Would be calculated with market data
                        RiskLevel = riskLevel
                    };
                })
                .OrderBy(m => m.PeriodDate)
                .ToList();

            return monthlyBenchmarks;
        }

        #endregion

        #region Private Helper Methods

        private decimal CalculateVolatility(List<decimal> values)
        {
            if (values.Count < 2) return 0;

            var mean = values.Average();
            var sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));
            var variance = sumSquaredDiffs / (values.Count - 1);
            var standardDeviation = (decimal)Math.Sqrt((double)variance);

            return mean > 0 ? (standardDeviation / mean) * 100 : 0; // Coefficient of variation as percentage
        }

        private decimal CalculateProcurementEfficiencyScore(List<Purchase> purchases)
        {
            if (!purchases.Any()) return 0;

            // Simplified efficiency score based on cost consistency and volume optimization
            var costs = purchases.Where(p => p.QuantityLiters > 0).Select(p => p.TotalValueUSD / p.QuantityLiters).ToList();
            var volatility = CalculateVolatility(costs);

            // Lower volatility = higher efficiency (inverted scale)
            var consistencyScore = Math.Max(0, 100 - volatility);

            // Volume efficiency (prefer larger, less frequent purchases)
            var avgVolumePerTransaction = purchases.Average(p => p.QuantityLiters);
            var volumeScore = Math.Min(100, avgVolumePerTransaction / 1000 * 10); // Scale to 0-100

            return (consistencyScore + volumeScore) / 2;
        }

        private decimal CalculatePriceConsistencyScore(List<decimal> prices)
        {
            if (prices.Count < 2) return 100;

            var volatility = CalculateVolatility(prices);
            return Math.Max(0, 100 - volatility); // Higher consistency = lower volatility
        }

        private decimal CalculatePaymentReliabilityScore(List<Purchase> purchases)
        {
            var purchasesWithDueDates = purchases.Where(p => p.DueDate.HasValue).ToList();
            if (!purchasesWithDueDates.Any()) return 100; // Default high score if no due dates

            var onTimePayments = purchasesWithDueDates.Count(p => p.PaymentDate.HasValue && p.PaymentDate <= p.DueDate!.Value);
            return (decimal)onTimePayments / purchasesWithDueDates.Count * 100;
        }

        private decimal CalculateCostOptimizationRating(List<Purchase> purchases)
        {
            // Simplified: based on achieving lower costs over time
            if (purchases.Count < 2) return 75; // Default score

            var costs = purchases.OrderBy(p => p.PurchaseDate)
                .Where(p => p.QuantityLiters > 0)
                .Select(p => p.TotalValueUSD / p.QuantityLiters)
                .ToList();

            if (costs.Count < 2) return 75;

            var costTrend = costs.Last() - costs.First();
            var improvement = costs.First() > 0 ? (-costTrend / costs.First()) * 100 : 0;

            return Math.Max(0, Math.Min(100, 75 + improvement)); // Base 75, adjust for improvement
        }

        private decimal CalculatePriceNegotiationScore(List<Purchase> purchases)
        {
            // Simplified: based on price variance within supplier
            var costs = purchases.Where(p => p.QuantityLiters > 0).Select(p => p.TotalValueUSD / p.QuantityLiters).ToList();
            var volatility = CalculateVolatility(costs);

            return Math.Max(50, 100 - volatility); // Lower volatility suggests better negotiation
        }

        private decimal CalculateVolumeEfficiencyScore(List<Purchase> purchases)
        {
            var avgVolume = purchases.Average(p => p.QuantityLiters);
            return Math.Min(100, avgVolume / 5000 * 100); // Scale based on volume efficiency
        }

        private decimal CalculateTimingEfficiencyScore(List<Purchase> purchases)
        {
            // Simplified: based on purchase frequency optimization
            if (purchases.Count < 2) return 75;

            var daysBetweenPurchases = purchases.OrderBy(p => p.PurchaseDate)
                .Zip(purchases.OrderBy(p => p.PurchaseDate).Skip(1), (first, second) => (second.PurchaseDate - first.PurchaseDate).Days)
                .ToList();

            var avgDaysBetween = daysBetweenPurchases.Average();
            var optimalRange = avgDaysBetween >= 15 && avgDaysBetween <= 60; // Optimal 2-8 weeks between purchases

            return optimalRange ? 85 : 60;
        }

        private decimal CalculateQualityConsistencyScore(List<Purchase> purchases)
        {
            // Simplified: based on density consistency (quality indicator)
            var densities = purchases.Select(p => p.Density).ToList();
            var volatility = CalculateVolatility(densities);

            return Math.Max(60, 100 - volatility * 10); // Lower density variance = higher quality consistency
        }

        private decimal CalculatePaymentTermsScore(List<Purchase> purchases)
        {
            var purchasesWithTerms = purchases.Where(p => p.DueDate.HasValue && p.InvoiceReceiptDate.HasValue).ToList();
            if (!purchasesWithTerms.Any()) return 75; // Default score

            var avgPaymentTerms = purchasesWithTerms.Average(p => (p.DueDate!.Value - p.InvoiceReceiptDate!.Value).Days);

            return avgPaymentTerms switch
            {
                >= 30 => 95, // 30+ days is excellent
                >= 15 => 85, // 15-30 days is good
                >= 7 => 70,  // 7-15 days is fair
                _ => 50      // Less than 7 days is poor
            };
        }

        private decimal CalculateRiskAssessmentScore(List<Purchase> purchases)
        {
            // Simplified: based on supplier concentration risk
            var supplierCount = purchases.Select(p => p.SupplierId).Distinct().Count();
            var supplierDiversification = Math.Min(100, supplierCount * 20); // More suppliers = lower risk

            return Math.Max(40, supplierDiversification);
        }

        #endregion
    }
}