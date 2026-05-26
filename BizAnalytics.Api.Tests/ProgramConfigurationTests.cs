using System.Collections;
using Xunit;

namespace BizAnalytics.Api.Tests;

public class ProgramConfigurationTests
{
    [Fact]
    public void BuildKgdRegistryEnvironmentOverrides_UsesLiveMode_WhenPortalTokenIsProvided()
    {
        IDictionary environmentVariables = new Hashtable
        {
            ["KGD_PORTAL_BASE_URL"] = "https://portal.kgd.gov.kz",
            ["KGD_PORTAL_TOKEN"] = "live-token"
        };

        var overrides = Program.BuildKgdRegistryEnvironmentOverrides(environmentVariables);

        Assert.Equal("https://portal.kgd.gov.kz", overrides["KgdRegistry:BaseUrl"]);
        Assert.Equal("live-token", overrides["KgdRegistry:PortalToken"]);
        Assert.Equal("live", overrides["KgdRegistry:Mode"]);
    }

    [Fact]
    public void BuildKgdRegistryEnvironmentOverrides_KeepsExplicitMode()
    {
        IDictionary environmentVariables = new Hashtable
        {
            ["KGD_REGISTRY_MODE"] = "demo",
            ["KGD_PORTAL_TOKEN"] = "demo-token"
        };

        var overrides = Program.BuildKgdRegistryEnvironmentOverrides(environmentVariables);

        Assert.Equal("demo", overrides["KgdRegistry:Mode"]);
        Assert.Equal("demo-token", overrides["KgdRegistry:PortalToken"]);
    }
}
