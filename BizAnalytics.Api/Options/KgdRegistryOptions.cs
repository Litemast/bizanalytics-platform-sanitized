namespace BizAnalytics.Api.Options;

public class KgdRegistryOptions
{
    public string Mode { get; set; } = "demo";
    public string BaseUrl { get; set; } = "https://portal.kgd.gov.kz";
    public string PortalToken { get; set; } = string.Empty;
}
