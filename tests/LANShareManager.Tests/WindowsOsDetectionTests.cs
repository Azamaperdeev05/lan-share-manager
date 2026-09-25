using LANShareManager.Core.Localization;
using LANShareManager.Core.Models;
using LANShareManager.Infrastructure.OS;
using Xunit;

namespace LANShareManager.Tests;

public class WindowsOsDetectionTests
{
    [Theory]
    [InlineData(22000, true)]
    [InlineData(22621, true)]
    [InlineData(22631, true)]
    [InlineData(26100, true)]
    [InlineData(19045, false)]
    [InlineData(19044, false)]
    [InlineData(17763, false)]
    [InlineData(10240, false)]
    public void IsWindows11_DetectedByBuildThreshold(int buildNumber, bool expectedIsWin11)
    {
        var osInfo = new WindowsOsInfo { BuildNumber = buildNumber };
        Assert.Equal(expectedIsWin11, osInfo.IsWindows11);
    }

    [Theory]
    [InlineData("Server", true)]
    [InlineData("Server Core", true)]
    [InlineData("Windows Server 2022", true)]
    [InlineData("Client", false)]
    [InlineData("Workstation", false)]
    public void IsServer_DetectedByInstallationType(string installationType, bool expectedIsServer)
    {
        var osInfo = new WindowsOsInfo { InstallationType = installationType };
        Assert.Equal(expectedIsServer, osInfo.IsServer);
    }

    [Fact]
    public void BuildString_FormatsWithUpdateRevision()
    {
        var withUbr = new WindowsOsInfo { BuildNumber = 22631, UpdateRevision = 3880 };
        Assert.Equal("22631.3880", withUbr.BuildString);

        var withoutUbr = new WindowsOsInfo { BuildNumber = 22631, UpdateRevision = 0 };
        Assert.Equal("22631", withoutUbr.BuildString);
    }

    [Fact]
    public void FullDescription_FormatsAllAvailableFields()
    {
        var info = new WindowsOsInfo
        {
            ProductName = "Windows 11 Pro",
            DisplayVersion = "23H2",
            BuildNumber = 22631,
            UpdateRevision = 3880,
            Architecture = "x64"
        };

        string desc = info.FullDescription;
        Assert.Contains("Windows 11 Pro", desc);
        Assert.Contains("23H2", desc);
        Assert.Contains("x64", desc);
        Assert.Contains("22631.3880", desc);
    }

    [Fact]
    public void WindowsOsService_GetOsInfo_ReturnsValidInstanceAndCaches()
    {
        var service = new WindowsOsService();
        var info1 = service.GetOsInfo();

        Assert.NotNull(info1);
        Assert.False(string.IsNullOrWhiteSpace(info1.ProductName));
        Assert.False(string.IsNullOrWhiteSpace(info1.Architecture));

        var info2 = service.GetOsInfo();
        Assert.Same(info1, info2);
    }

    [Theory]
    [InlineData(AppLanguage.Kazakh, "Жүйе")]
    [InlineData(AppLanguage.Russian, "Версия ОС")]
    [InlineData(AppLanguage.English, "OS Version")]
    public void Localization_ContainsOsVersionInAllLanguages(AppLanguage lang, string expectedSubstring)
    {
        string text = LocalizationService.GetString("OsVersion", lang);
        Assert.Contains(expectedSubstring, text);
    }

    [Fact]
    public void AntivirusSummary_WithThirdParty_FormatsCorrectly()
    {
        var info = new WindowsOsInfo
        {
            InstalledAntivirus = new List<string> { "Kaspersky Endpoint Security", "Malwarebytes Premium" }
        };

        Assert.True(info.HasThirdPartyAntivirus);
        Assert.Equal("Kaspersky Endpoint Security, Malwarebytes Premium", info.AntivirusSummary);
    }

    [Fact]
    public void AntivirusSummary_WithoutThirdParty_ReturnsDefender()
    {
        var info = new WindowsOsInfo();
        Assert.False(info.HasThirdPartyAntivirus);
        Assert.Equal("Windows Defender", info.AntivirusSummary);

        var infoWithDefenderOnly = new WindowsOsInfo
        {
            InstalledAntivirus = new List<string> { "Windows Defender" }
        };
        Assert.False(infoWithDefenderOnly.HasThirdPartyAntivirus);
        Assert.Equal("Windows Defender", infoWithDefenderOnly.AntivirusSummary);
    }

    [Theory]
    [InlineData("WMI repository is inconsistent", "WMI_CORRUPTION", "winmgmt /salvagerepository")]
    [InlineData("winmgmt service failed to start", "WMI_CORRUPTION", "winmgmt /salvagerepository")]
    [InlineData("System.Management.ManagementException 0x80041010", "WMI_CORRUPTION", "winmgmt /salvagerepository")]
    public void RemediationAdvisor_AnalyzesWmiCorruption(string errorMsg, string expectedCategory, string expectedQuickCmd)
    {
        var report = LANShareManager.Core.Diagnostics.RemediationAdvisor.Analyze(errorMsg);

        Assert.Equal(expectedCategory, report.CategoryKey);
        Assert.True(report.CanAutoFix);
        Assert.Equal(expectedQuickCmd, report.QuickCommand);
        Assert.Contains("WMI", report.ErrorTitle);
    }

    [Theory]
    [InlineData("Kaspersky Endpoint Security blocked incoming connection", "THIRD_PARTY_AV")]
    [InlineData("ESET NOD32 Antivirus firewall active", "THIRD_PARTY_AV")]
    [InlineData("Bitdefender Total Security port blocked", "THIRD_PARTY_AV")]
    public void RemediationAdvisor_AnalyzesThirdPartyAntivirus(string errorMsg, string expectedCategory)
    {
        var report = LANShareManager.Core.Diagnostics.RemediationAdvisor.Analyze(errorMsg);

        Assert.Equal(expectedCategory, report.CategoryKey);
        Assert.False(report.CanAutoFix);
        Assert.Contains("445", string.Join(" ", report.RemediationSteps));
    }
}
