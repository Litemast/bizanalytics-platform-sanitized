using BizAnalytics.Api.Contracts.Analytics;

namespace BizAnalytics.Api.Contracts.Entrepreneurs;

public class IndividualEntrepreneurSearchResponse
{
    public string Mode { get; set; } = "demo";
    public bool Found { get; set; }
    public string SearchedIin { get; set; } = string.Empty;
    public string SourceName { get; set; } = "KGD Registry";
    public string? Message { get; set; }
    public IndividualEntrepreneurRegistryProfileResponse? Registry { get; set; }
    public List<InsightResponse> Insights { get; set; } = [];
    public List<EntrepreneurReportFormResponse> ReportForms { get; set; } = [];
    public List<EntrepreneurLinkResponse> OfficialLinks { get; set; } = [];
}

public class IndividualEntrepreneurRegistryProfileResponse
{
    public string Iin { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TaxpayerType { get; set; } = "IP";
    public DateTime? TaxpayerBeginDate { get; set; }
    public DateTime? TaxpayerEndDate { get; set; }
    public string? TaxpayerEndReason { get; set; }
    public DateTime? ActualityDate { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string Residency { get; set; } = string.Empty;
    public string Oked { get; set; } = string.Empty;
    public string OkedName { get; set; } = string.Empty;
    public DateTime? OkedDate { get; set; }
    public string TaxMode { get; set; } = string.Empty;
    public DateTime? TaxModeDate { get; set; }
    public string VatInfo { get; set; } = string.Empty;
    public DateTime? VatDate { get; set; }
    public string RiskDegree { get; set; } = string.Empty;
    public DateTime? RiskRelevanceDate { get; set; }
    public DateTime? RiskBeginDate { get; set; }
    public DateTime? RiskEndDate { get; set; }
    public decimal TaxDebt { get; set; }
    public decimal TaxDebt1 { get; set; }
    public decimal TaxDebt2 { get; set; }
    public decimal TaxDebt3 { get; set; }
    public decimal TaxDebt4 { get; set; }
    public List<EntrepreneurRegistryFlagResponse> Flags { get; set; } = [];
    public List<EntrepreneurStatisticResponse> Statistics { get; set; } = [];
    public List<EntrepreneurSpecialTaxModeResponse> SpecialTaxModes { get; set; } = [];
}

public class EntrepreneurRegistryFlagResponse
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class EntrepreneurStatisticResponse
{
    public int Year { get; set; }
    public decimal WorkersCount { get; set; }
    public decimal TaxIn { get; set; }
    public decimal Knn { get; set; }
    public decimal KnnAverage { get; set; }
    public decimal VatAmount { get; set; }
}

public class EntrepreneurSpecialTaxModeResponse
{
    public string Type { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public DateTime? BeginDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class EntrepreneurReportFormResponse
{
    public string FormCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FilingPeriodicity { get; set; } = string.Empty;
    public string FilingDeadline { get; set; } = string.Empty;
    public string PaymentDeadline { get; set; } = string.Empty;
    public string Applicability { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
    public List<string> Sections { get; set; } = [];
    public List<string> HighlightFields { get; set; } = [];
    public string OfficialSourceUrl { get; set; } = string.Empty;
}

public class EntrepreneurLinkResponse
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
