using BizAnalytics.Api.Contracts.Analytics;
using BizAnalytics.Api.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace BizAnalytics.Api.Services;

public class AnalyticsAggregationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AnalyticsAggregationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AnalysisBundleResponse BuildAnalysis(IEnumerable<SalesRecord> records)
    {
        return BuildAnalysis(records, [], []);
    }

    public AnalysisBundleResponse BuildAnalysis(
        IEnumerable<SalesRecord> records,
        IEnumerable<FinancialRecord> financialRecords,
        IEnumerable<EducationRecord> educationRecords)
    {
        var items = records
            .Select(record => new AnalyticsProjection(
                DateTime.SpecifyKind(record.Date.Date, DateTimeKind.Utc),
                record.ProductName.Trim(),
                record.Quantity,
                record.Amount,
                string.IsNullOrWhiteSpace(record.SourceFileName)
                    ? DefaultSourceName()
                    : record.SourceFileName.Trim()))
            .OrderBy(record => record.Date)
            .ToList();

        var financialItems = financialRecords
            .OrderBy(record => record.Period)
            .ToList();

        var educationItems = educationRecords
            .OrderBy(record => record.StudentName)
            .ThenBy(record => record.Subject)
            .ToList();

        var revenue = BuildRevenue(items);
        var topProducts = BuildTopProducts(items);
        var sourceComparisons = BuildSourceComparisons(items);
        var salesInsights = BuildSalesInsights(revenue, topProducts, sourceComparisons, items);
        var financial = financialItems.Count > 0 ? BuildFinancialAnalysis(financialItems) : null;
        var education = educationItems.Count > 0 ? BuildEducationAnalysis(educationItems) : null;

        var response = new AnalysisBundleResponse
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ReportType = ResolveReportType(items.Count > 0, financial is not null, education is not null),
            Summary = BuildSummary(items),
            Revenue = revenue,
            TopProducts = topProducts,
            PriceTrends = BuildPriceTrends(items),
            SourceComparisons = sourceComparisons,
            Financial = financial,
            Education = education
        };

        response.Insights.AddRange(salesInsights);

        return response;
    }

    private string ResolveReportType(bool hasSales, bool hasFinancial, bool hasEducation)
    {
        var count = (hasSales ? 1 : 0) + (hasFinancial ? 1 : 0) + (hasEducation ? 1 : 0);
        if (count > 1)
        {
            return "mixed_report";
        }

        if (hasFinancial)
        {
            return "financial_report";
        }

        if (hasEducation)
        {
            return "education_report";
        }

        return hasSales ? "sales_report" : "unknown";
    }

    private SummaryResponse BuildSummary(IReadOnlyCollection<AnalyticsProjection> items)
    {
        if (items.Count == 0)
        {
            return new SummaryResponse();
        }

        return new SummaryResponse
        {
            TotalRevenue = items.Sum(item => item.Amount),
            TotalSalesCount = items.Count,
            TotalQuantity = items.Sum(item => item.Quantity),
            AverageCheck = items.Sum(item => item.Amount) / items.Count
        };
    }

    private List<RevenuePointResponse> BuildRevenue(IEnumerable<AnalyticsProjection> items)
    {
        return items
            .GroupBy(item => item.Date)
            .Select(group => new RevenuePointResponse
            {
                Date = group.Key,
                Revenue = group.Sum(item => item.Amount)
            })
            .OrderBy(point => point.Date)
            .ToList();
    }

    private List<TopProductResponse> BuildTopProducts(IEnumerable<AnalyticsProjection> items)
    {
        return items
            .GroupBy(item => item.ProductName)
            .Select(group => new TopProductResponse
            {
                ProductName = group.Key,
                TotalQuantity = group.Sum(item => item.Quantity),
                TotalRevenue = group.Sum(item => item.Amount)
            })
            .OrderByDescending(item => item.TotalRevenue)
            .ThenByDescending(item => item.TotalQuantity)
            .ThenBy(item => item.ProductName)
            .Take(10)
            .ToList();
    }

    private List<PriceTrendPointResponse> BuildPriceTrends(IEnumerable<AnalyticsProjection> items)
    {
        return items
            .GroupBy(item => item.Date)
            .Select(group => new PriceTrendPointResponse
            {
                Date = group.Key,
                AverageUnitPrice = group.Sum(item => item.Amount) / group.Sum(item => item.Quantity)
            })
            .OrderBy(point => point.Date)
            .ToList();
    }

    private List<SourceComparisonResponse> BuildSourceComparisons(IEnumerable<AnalyticsProjection> items)
    {
        return items
            .GroupBy(item => item.SourceFileName)
            .Select(group => new SourceComparisonResponse
            {
                SourceName = group.Key,
                RecordsCount = group.Count(),
                TotalQuantity = group.Sum(item => item.Quantity),
                TotalRevenue = group.Sum(item => item.Amount),
                AverageUnitPrice = group.Sum(item => item.Amount) / group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.TotalRevenue)
            .ThenBy(item => item.SourceName)
            .Take(8)
            .ToList();
    }

    private FinancialAnalysisResponse BuildFinancialAnalysis(IReadOnlyCollection<FinancialRecord> records)
    {
        var periods = records
            .GroupBy(record => new { Period = record.Period.Date, record.PeriodLabel })
            .Select(group =>
            {
                var revenue = group.Sum(record => record.Revenue);
                var expenses = group.Sum(record => record.Expenses);
                var profit = group.Sum(record => record.Profit);

                return new FinancialPeriodPointResponse
                {
                    Period = DateTime.SpecifyKind(group.Key.Period, DateTimeKind.Utc),
                    PeriodLabel = string.IsNullOrWhiteSpace(group.Key.PeriodLabel)
                        ? group.Key.Period.ToString("yyyy-MM-dd")
                        : group.Key.PeriodLabel,
                    Revenue = revenue,
                    Expenses = expenses,
                    Profit = profit,
                    Profitability = CalculatePercent(profit, revenue)
                };
            })
            .OrderBy(point => point.Period)
            .ToList();

        var totalRevenue = periods.Sum(point => point.Revenue);
        var totalExpenses = periods.Sum(point => point.Expenses);
        var totalProfit = periods.Sum(point => point.Profit);
        var profitSeries = periods.Select(point => point.Profit).ToList();

        var analysis = new FinancialAnalysisResponse
        {
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            TotalProfit = totalProfit,
            Profitability = CalculatePercent(totalProfit, totalRevenue),
            LinearRegressionForecast = ForecastLinearRegression(profitSeries),
            MovingAverageForecast = ForecastMovingAverage(profitSeries),
            TrendExtrapolationForecast = ForecastTrendExtrapolation(profitSeries),
            Periods = periods,
            ForecastTrend = BuildFinancialForecastTrend(periods)
        };

        analysis.Insights = BuildFinancialInsights(analysis);

        return analysis;
    }

    private EducationAnalysisResponse BuildEducationAnalysis(IReadOnlyCollection<EducationRecord> records)
    {
        var scores = records
            .Select(record => new EducationProjection(
                record.StudentName.Trim(),
                record.Subject.Trim(),
                record.AverageScore ?? record.Grade,
                record.Grade))
            .Where(record => !string.IsNullOrWhiteSpace(record.StudentName) && !string.IsNullOrWhiteSpace(record.Subject))
            .ToList();

        if (scores.Count == 0)
        {
            return new EducationAnalysisResponse();
        }

        var maxScore = scores.Max(item => item.Score) <= 5 ? 5m : 100m;
        var successThreshold = maxScore <= 5 ? 3m : 60m;
        var students = BuildEducationPerformance(scores.GroupBy(item => item.StudentName));
        var subjects = BuildEducationPerformance(scores.GroupBy(item => item.Subject));
        var bestStudent = students.FirstOrDefault();
        var worstStudent = students.LastOrDefault();
        var bestSubject = subjects.FirstOrDefault();
        var worstSubject = subjects.LastOrDefault();

        var analysis = new EducationAnalysisResponse
        {
            AverageScore = scores.Average(item => item.Score),
            BestStudent = bestStudent?.Name ?? string.Empty,
            WorstStudent = worstStudent?.Name ?? string.Empty,
            BestSubject = bestSubject?.Name ?? string.Empty,
            WorstSubject = worstSubject?.Name ?? string.Empty,
            SuccessRate = CalculatePercent(scores.Count(item => item.Score >= successThreshold), scores.Count),
            GradeDistribution = scores
                .GroupBy(item => Math.Round(item.Grade, 0, MidpointRounding.AwayFromZero).ToString("0"))
                .Select(group => new GradeDistributionPointResponse
                {
                    Grade = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(item => decimal.TryParse(item.Grade, out var grade) ? grade : 0)
                .ToList(),
            StudentPerformance = students,
            SubjectPerformance = subjects,
            StudentForecasts = BuildStudentForecasts(scores, maxScore),
            RiskStudents = students
                .Where(item => item.AverageScore < successThreshold)
                .Select(item => new RiskStudentResponse
                {
                    StudentName = item.Name,
                    AverageScore = item.AverageScore,
                    RiskLevel = item.AverageScore < successThreshold * 0.75m ? "high" : "medium"
                })
                .ToList()
        };

        analysis.Insights = BuildEducationInsights(analysis);

        return analysis;
    }

    private static List<EducationPerformancePointResponse> BuildEducationPerformance(
        IEnumerable<IGrouping<string, EducationProjection>> groups)
    {
        return groups
            .Select(group => new EducationPerformancePointResponse
            {
                Name = group.Key,
                AverageScore = group.Average(item => item.Score)
            })
            .OrderByDescending(item => item.AverageScore)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private static List<StudentForecastResponse> BuildStudentForecasts(
        IReadOnlyCollection<EducationProjection> scores,
        decimal maxScore)
    {
        return scores
            .GroupBy(item => item.StudentName)
            .Select(group =>
            {
                var ordered = group.ToList();
                var average = ordered.Average(item => item.Score);
                var first = ordered.First().Score;
                var last = ordered.Last().Score;
                var trend = ordered.Count > 1 ? (last - first) / (ordered.Count - 1) : 0;
                var forecast = Math.Clamp(average + trend, 0, maxScore);

                return new StudentForecastResponse
                {
                    StudentName = group.Key,
                    CurrentAverage = average,
                    ForecastAverage = forecast
                };
            })
            .OrderByDescending(item => item.ForecastAverage)
            .ThenBy(item => item.StudentName)
            .Take(12)
            .ToList();
    }

    private List<InsightResponse> BuildSalesInsights(
        IReadOnlyList<RevenuePointResponse> revenue,
        IReadOnlyList<TopProductResponse> topProducts,
        IReadOnlyList<SourceComparisonResponse> sourceComparisons,
        IReadOnlyList<AnalyticsProjection> items)
    {
        var insights = new List<InsightResponse>();

        var bestDay = revenue.FirstOrDefault(point => point.Revenue == revenue.MaxBy(item => item.Revenue)?.Revenue);
        if (bestDay is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Peak revenue day" : "Пиковый день по выручке",
                Description = IsEnglishRequested()
                    ? $"{bestDay.Date:dd.MM.yyyy} delivered the strongest revenue result."
                    : $"{bestDay.Date:dd.MM.yyyy} дал лучший результат по выручке.",
                Tone = "peak"
            });
        }

        var bestProduct = topProducts.FirstOrDefault();
        if (bestProduct is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Main revenue driver" : "Главный драйвер выручки",
                Description = IsEnglishRequested()
                    ? $"{bestProduct.ProductName} leads the portfolio by revenue."
                    : $"{bestProduct.ProductName} лидирует по вкладу в выручку.",
                Tone = "leader"
            });
        }

        var volatileProduct = items
            .GroupBy(item => item.ProductName)
            .Select(group => new
            {
                ProductName = group.Key,
                MinUnitPrice = group.Min(item => item.UnitPrice),
                MaxUnitPrice = group.Max(item => item.UnitPrice)
            })
            .Select(item => new
            {
                item.ProductName,
                Spread = item.MaxUnitPrice - item.MinUnitPrice
            })
            .OrderByDescending(item => item.Spread)
            .FirstOrDefault(item => item.Spread > 0);

        if (volatileProduct is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Largest price fluctuation" : "Наибольшее колебание цены",
                Description = IsEnglishRequested()
                    ? $"{volatileProduct.ProductName} shows the widest spread between minimum and maximum unit price."
                    : $"{volatileProduct.ProductName} показывает самый широкий разброс между минимальной и максимальной ценой.",
                Tone = "volatility"
            });
        }

        var dominantSource = sourceComparisons.FirstOrDefault();
        if (dominantSource is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Leading dataset" : "Лидирующий набор данных",
                Description = IsEnglishRequested()
                    ? $"{dominantSource.SourceName} contributes the largest revenue share."
                    : $"{dominantSource.SourceName} вносит самый большой вклад в выручку.",
                Tone = "leader"
            });
        }

        if (revenue.Count > 1)
        {
            var firstDay = revenue.First();
            var lastDay = revenue.Last();
            var isGrowing = lastDay.Revenue >= firstDay.Revenue;

            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Revenue trend" : "Тренд выручки",
                Description = IsEnglishRequested()
                    ? isGrowing
                        ? $"Revenue moved upward from {firstDay.Date:dd.MM.yyyy} to {lastDay.Date:dd.MM.yyyy}."
                        : $"Revenue cooled down from {firstDay.Date:dd.MM.yyyy} to {lastDay.Date:dd.MM.yyyy}."
                    : isGrowing
                        ? $"Выручка показывает рост от {firstDay.Date:dd.MM.yyyy} к {lastDay.Date:dd.MM.yyyy}."
                        : $"Выручка снизилась от {firstDay.Date:dd.MM.yyyy} к {lastDay.Date:dd.MM.yyyy}.",
                Tone = "trend"
            });
        }

        var quantityLeader = topProducts
            .OrderByDescending(item => item.TotalQuantity)
            .ThenByDescending(item => item.TotalRevenue)
            .FirstOrDefault();
        if (quantityLeader is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Volume leader" : "Лидер по объему продаж",
                Description = IsEnglishRequested()
                    ? $"{quantityLeader.ProductName} leads by sold quantity."
                    : $"{quantityLeader.ProductName} лидирует по проданному объему.",
                Tone = "leader"
            });
        }

        if (items.Count > 0)
        {
            var averageCheck = items.Sum(item => item.Amount) / items.Count;
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Average order value" : "Средний чек",
                Description = IsEnglishRequested()
                    ? $"The current average order value is {averageCheck:N2}."
                    : $"Текущее значение среднего чека составляет {averageCheck:N2}.",
                Tone = "signal"
            });
        }

        return insights;
    }

    private List<InsightResponse> BuildFinancialInsights(FinancialAnalysisResponse analysis)
    {
        var insights = new List<InsightResponse>();
        var bestPeriod = analysis.Periods.MaxBy(item => item.Profit);
        var worstPeriod = analysis.Periods.MinBy(item => item.Profit);
        var bestMargin = analysis.Periods.MaxBy(item => item.Profitability);
        var highestExpensePeriod = analysis.Periods.MaxBy(item => item.Expenses);

        if (bestPeriod is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Best financial period" : "Лучший финансовый период",
                Description = IsEnglishRequested()
                    ? $"{bestPeriod.PeriodLabel} delivered the highest profit."
                    : $"{bestPeriod.PeriodLabel} показывает максимальную прибыль.",
                Tone = "peak"
            });
        }

        if (worstPeriod is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested()
                    ? worstPeriod.Profit < 0 ? "Loss period" : "Weakest financial period"
                    : worstPeriod.Profit < 0 ? "Период с убытком" : "Самый слабый период",
                Description = IsEnglishRequested()
                    ? worstPeriod.Profit < 0
                        ? $"{worstPeriod.PeriodLabel} needs attention because profit is negative."
                        : $"{worstPeriod.PeriodLabel} shows the lowest profit level."
                    : worstPeriod.Profit < 0
                        ? $"{worstPeriod.PeriodLabel} требует внимания: прибыль отрицательная."
                        : $"{worstPeriod.PeriodLabel} показывает минимальный уровень прибыли.",
                Tone = "risk"
            });
        }

        if (bestMargin is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Highest profitability" : "Максимальная рентабельность",
                Description = IsEnglishRequested()
                    ? $"{bestMargin.PeriodLabel} achieved the strongest profitability."
                    : $"{bestMargin.PeriodLabel} показывает наилучшую рентабельность.",
                Tone = "leader"
            });
        }

        if (highestExpensePeriod is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Expense pressure" : "Пиковая нагрузка по расходам",
                Description = IsEnglishRequested()
                    ? $"{highestExpensePeriod.PeriodLabel} has the highest expense load."
                    : $"{highestExpensePeriod.PeriodLabel} дает максимальную нагрузку по расходам.",
                Tone = "volatility"
            });
        }

        insights.Add(new InsightResponse
        {
            Title = IsEnglishRequested() ? "Profit forecast" : "Прогноз прибыли",
            Description = IsEnglishRequested()
                ? $"The next projected value is around {analysis.LinearRegressionForecast:N2}."
                : $"Следующее прогнозное значение прибыли около {analysis.LinearRegressionForecast:N2}.",
            Tone = "trend"
        });

        return insights;
    }

    private List<InsightResponse> BuildEducationInsights(EducationAnalysisResponse analysis)
    {
        var insights = new List<InsightResponse>();

        if (!string.IsNullOrWhiteSpace(analysis.BestStudent))
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Top student" : "Лучший ученик",
                Description = IsEnglishRequested()
                    ? $"{analysis.BestStudent} has the strongest average score."
                    : $"{analysis.BestStudent} показывает самый высокий средний балл.",
                Tone = "leader"
            });
        }

        if (!string.IsNullOrWhiteSpace(analysis.BestSubject))
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Strongest subject" : "Сильный предмет",
                Description = IsEnglishRequested()
                    ? $"{analysis.BestSubject} shows the best average performance."
                    : $"{analysis.BestSubject} показывает лучший средний результат.",
                Tone = "peak"
            });
        }

        if (!string.IsNullOrWhiteSpace(analysis.WorstSubject))
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Subject requiring attention" : "Предмет требует внимания",
                Description = IsEnglishRequested()
                    ? $"{analysis.WorstSubject} has the lowest average score."
                    : $"{analysis.WorstSubject} имеет самый низкий средний балл.",
                Tone = "risk"
            });
        }

        if (analysis.RiskStudents.Count > 0)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Risk group" : "Группа риска",
                Description = IsEnglishRequested()
                    ? $"{analysis.RiskStudents.Count} students may need additional attention."
                    : $"{analysis.RiskStudents.Count} учеников нуждаются в дополнительном внимании.",
                Tone = "risk"
            });
        }

        insights.Add(new InsightResponse
        {
            Title = IsEnglishRequested() ? "Success rate" : "Процент успеваемости",
            Description = IsEnglishRequested()
                ? $"The current success rate is {analysis.SuccessRate:N2}%."
                : $"Текущий процент успеваемости составляет {analysis.SuccessRate:N2}%.",
            Tone = "trend"
        });

        var bestForecast = analysis.StudentForecasts.MaxBy(item => item.ForecastAverage);
        if (bestForecast is not null)
        {
            insights.Add(new InsightResponse
            {
                Title = IsEnglishRequested() ? "Forecast leader" : "Лидер прогноза",
                Description = IsEnglishRequested()
                    ? $"{bestForecast.StudentName} is expected to keep one of the strongest results."
                    : $"{bestForecast.StudentName} по прогнозу сохранит один из самых сильных результатов.",
                Tone = "signal"
            });
        }

        return insights;
    }

    private static List<FinancialForecastPointResponse> BuildFinancialForecastTrend(
        IReadOnlyList<FinancialPeriodPointResponse> periods)
    {
        if (periods.Count == 0)
        {
            return [];
        }

        var profits = periods.Select(point => point.Profit).ToList();
        var forecastCount = Math.Max(1, periods.Count);
        var stepDays = InferForecastStepDays(periods);
        var lastPeriod = periods[^1].Period.Date;
        var forecast = new List<FinancialForecastPointResponse>();

        for (var index = 1; index <= forecastCount; index++)
        {
            var period = DateTime.SpecifyKind(lastPeriod.AddDays(stepDays * index), DateTimeKind.Utc);
            forecast.Add(new FinancialForecastPointResponse
            {
                Period = period,
                PeriodLabel = period.ToString("yyyy-MM-dd"),
                ForecastProfit = ForecastLinearRegressionAt(profits, profits.Count + index)
            });
        }

        return forecast;
    }

    private static int InferForecastStepDays(IReadOnlyList<FinancialPeriodPointResponse> periods)
    {
        if (periods.Count < 2)
        {
            return 1;
        }

        var gaps = periods
            .Zip(periods.Skip(1), (previous, next) => (next.Period.Date - previous.Period.Date).Days)
            .Where(days => days > 0)
            .OrderBy(days => days)
            .ToList();

        if (gaps.Count == 0)
        {
            return 1;
        }

        return Math.Max(1, gaps[gaps.Count / 2]);
    }

    private static decimal ForecastLinearRegression(IReadOnlyList<decimal> values)
    {
        return ForecastLinearRegressionAt(values, values.Count + 1);
    }

    private static decimal ForecastLinearRegressionAt(IReadOnlyList<decimal> values, int targetX)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        if (values.Count == 1)
        {
            return values[0];
        }

        var n = values.Count;
        var sumX = Enumerable.Range(1, n).Sum();
        var sumY = values.Sum();
        var sumXY = values.Select((value, index) => (index + 1) * value).Sum();
        var sumXX = Enumerable.Range(1, n).Sum(value => value * value);
        var denominator = n * sumXX - sumX * sumX;

        if (denominator == 0)
        {
            return values[^1];
        }

        var slope = (n * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / n;

        return intercept + slope * targetX;
    }

    private static decimal ForecastMovingAverage(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var window = Math.Min(3, values.Count);
        return values.TakeLast(window).Average();
    }

    private static decimal ForecastTrendExtrapolation(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        if (values.Count == 1)
        {
            return values[0];
        }

        return values[^1] + (values[^1] - values[^2]);
    }

    private static decimal CalculatePercent(decimal numerator, decimal denominator)
    {
        return denominator == 0 ? 0 : numerator / denominator * 100m;
    }

    private static decimal CalculatePercent(int numerator, int denominator)
    {
        return denominator == 0 ? 0 : (decimal)numerator / denominator * 100m;
    }

    private string DefaultSourceName()
    {
        return IsEnglishRequested() ? "Imported dataset" : "Импортированный набор";
    }

    private bool IsEnglishRequested()
    {
        var acceptLanguage = _httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return false;
        }

        var firstLanguage = acceptLanguage
            .Split(',', ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return firstLanguage?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
    }

    private sealed record AnalyticsProjection(
        DateTime Date,
        string ProductName,
        int Quantity,
        decimal Amount,
        string SourceFileName)
    {
        public decimal UnitPrice => Quantity == 0 ? 0 : Amount / Quantity;
    }

    private sealed record EducationProjection(
        string StudentName,
        string Subject,
        decimal Score,
        decimal Grade);
}
