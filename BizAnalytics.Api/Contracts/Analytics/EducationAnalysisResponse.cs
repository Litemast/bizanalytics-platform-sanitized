namespace BizAnalytics.Api.Contracts.Analytics;

public class EducationAnalysisResponse
{
    public decimal AverageScore { get; set; }
    public string BestStudent { get; set; } = string.Empty;
    public string WorstStudent { get; set; } = string.Empty;
    public string BestSubject { get; set; } = string.Empty;
    public string WorstSubject { get; set; } = string.Empty;
    public decimal SuccessRate { get; set; }
    public List<GradeDistributionPointResponse> GradeDistribution { get; set; } = [];
    public List<EducationPerformancePointResponse> StudentPerformance { get; set; } = [];
    public List<EducationPerformancePointResponse> SubjectPerformance { get; set; } = [];
    public List<StudentForecastResponse> StudentForecasts { get; set; } = [];
    public List<RiskStudentResponse> RiskStudents { get; set; } = [];
    public List<InsightResponse> Insights { get; set; } = [];
}

public class GradeDistributionPointResponse
{
    public string Grade { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class EducationPerformancePointResponse
{
    public string Name { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
}

public class StudentForecastResponse
{
    public string StudentName { get; set; } = string.Empty;
    public decimal CurrentAverage { get; set; }
    public decimal ForecastAverage { get; set; }
}

public class RiskStudentResponse
{
    public string StudentName { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public string RiskLevel { get; set; } = "medium";
}
