using LANShareManager.Core.Localization;
using Xunit;

namespace LANShareManager.Tests;

public class LocalizationTests
{
    [Fact]
    public void SetLanguage_ChangesCurrentLanguageAndFiresEvent()
    {
        bool eventFired = false;
        LocalizationService.LanguageChanged += () => eventFired = true;

        LocalizationService.SetLanguage(AppLanguage.English);
        Assert.Equal(AppLanguage.English, LocalizationService.CurrentLanguage);
        Assert.True(eventFired);

        LocalizationService.SetLanguage(AppLanguage.Kazakh);
        Assert.Equal(AppLanguage.Kazakh, LocalizationService.CurrentLanguage);
    }

    [Theory]
    [InlineData(AppLanguage.Kazakh, "Win + R")]
    [InlineData(AppLanguage.Russian, "Win + R")]
    [InlineData(AppLanguage.English, "Win + R")]
    public void GetConnectGuide_ContainsWinRInstructionsInAllLanguages(AppLanguage lang, string expectedSubstring)
    {
        var guide = LocalizationService.GetConnectGuide("WorkShare", "192.168.1.50", "DESKTOP-ABC", lang);

        Assert.Contains(expectedSubstring, guide.Step1);
        Assert.Equal(@"\\192.168.1.50\WorkShare", guide.RecommendedPath);
        Assert.Equal(@"\\DESKTOP-ABC\WorkShare", guide.HostnamePath);
        Assert.Equal(@"net use Z: \\192.168.1.50\WorkShare /persistent:yes", guide.CmdNetUse);
        Assert.NotEmpty(guide.TipCredentials);
        Assert.NotEmpty(guide.TipDriveMap);
    }

    [Theory]
    [InlineData("AppTitle", AppLanguage.Kazakh, "LAN Share Manager")]
    [InlineData("AppTitle", AppLanguage.Russian, "LAN Share Manager")]
    [InlineData("AppTitle", AppLanguage.English, "LAN Share Manager")]
    [InlineData("BtnCreateShare", AppLanguage.Kazakh, "Жаңа ортақ папка")]
    [InlineData("BtnCreateShare", AppLanguage.Russian, "Создать общий доступ")]
    [InlineData("BtnCreateShare", AppLanguage.English, "Create Network Share")]
    public void GetString_ReturnsExpectedTranslation(string key, AppLanguage lang, string expectedSubstring)
    {
        string text = LocalizationService.GetString(key, lang);
        Assert.Contains(expectedSubstring, text);
    }

    [Fact]
    public void GetString_ReturnsKey_WhenKeyNotFound()
    {
        string result = LocalizationService.GetString("NonExistentKey123");
        Assert.Equal("NonExistentKey123", result);
    }
}
