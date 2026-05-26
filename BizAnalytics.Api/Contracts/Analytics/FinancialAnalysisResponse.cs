namespace BizAnalytics.Api.Contracts.Analytics;

public class FinancialAnalysisResponse
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal Profitability { get; set; }
    public decimal LinearRegressionForecast { get; set; }
    public decimal MovingAverageForecast { get; set; }
    public decimal TrendExtrapolationForecast { get; set; }
    public List<FinancialPeriodPointResponse> Periods { get; set; } = [];
    public List<FinancialForecastPointResponse> ForecastTrend { get; set; } = [];
    public List<InsightResponse> Insights { get; set; } = [];
}

public class FinancialPeriodPointResponse
{
    public DateTime Period { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public decimal Profitability { get; set; }
}

public class FinancialForecastPointResponse
{
    public DateTime Period { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal ForecastProfit { get; set; }
}
