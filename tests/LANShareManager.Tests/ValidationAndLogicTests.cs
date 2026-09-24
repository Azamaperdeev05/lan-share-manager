using LANShareManager.Core.Enums;
using LANShareManager.Core.Models;
using LANShareManager.Core.Serialization;
using LANShareManager.Core.Validation;
using LANShareManager.Infrastructure.Network;
using System.Net;
using Xunit;

namespace LANShareManager.Tests;

public class ValidationAndLogicTests
{
    [Theory]
    [InlineData("OBSHAYA")]
    [InlineData("Share123")]
    [InlineData("Public_Docs")]
    [InlineData("Shared-Folder")]
    public void ValidateShareName_ValidNames_ReturnsSuccess(string name)
    {
        var result = ShareInputValidator.ValidateShareName(name);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateShareName_EmptyOrNull_ReturnsFailure(string? name)
    {
        var result = ShareInputValidator.ValidateShareName(name);
        Assert.False(result.IsValid);
        Assert.Contains("не может быть пустым", result.ErrorMessage);
    }

    [Theory]
    [InlineData("Share/Name")]
    [InlineData("Share\\Name")]
    [InlineData("Share:Name")]
    [InlineData("Share*Name")]
    [InlineData("Share?Name")]
    [InlineData("Share\"Name")]
    [InlineData("Share<Name")]
    [InlineData("Share>Name")]
    [InlineData("Share|Name")]
    public void ValidateShareName_InvalidChars_ReturnsFailure(string name)
    {
        var result = ShareInputValidator.ValidateShareName(name);
        Assert.False(result.IsValid);
        Assert.Contains("недопустимые символы", result.ErrorMessage);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("LPT1")]
    public void ValidateShareName_ReservedWindowsNames_ReturnsFailure(string name)
    {
        var result = ShareInputValidator.ValidateShareName(name);
        Assert.False(result.IsValid);
        Assert.Contains("зарезервированным", result.ErrorMessage);
    }

    [Theory]
    [InlineData("C:\\OBSHAYA")]
    [InlineData("D:\\Shared\\Folder")]
    [InlineData("E:\\Data_2026")]
    public void ValidateFolderPath_ValidPaths_ReturnsSuccess(string path)
    {
        var result = ShareInputValidator.ValidateFolderPath(path);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("relative/path")]
    [InlineData("folder")]
    public void ValidateFolderPath_InvalidOrRelativePaths_ReturnsFailure(string? path)
    {
        var result = ShareInputValidator.ValidateFolderPath(path);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("ADMIN$")]
    [InlineData("IPC$")]
    [InlineData("PRINT$")]
    public void ValidateShareName_AdministrativeShares_ReturnsFailure(string name)
    {
        var result = ShareInputValidator.ValidateShareName(name);
        Assert.False(result.IsValid);
        Assert.Contains("административным ресурсом", result.ErrorMessage);
    }

    [Fact]
    public void ValidateShareName_ExistingCollision_ReturnsWarning()
    {
        var existing = new[] { "ExistingShare", "Public" };
        var result = ShareInputValidator.ValidateShareName("EXISTINGSHARE", existing);
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Contains("уже существует", result.Message);
    }

    [Fact]
    public void ValidateShareName_HiddenShareWithDollar_ReturnsWarning()
    {
        var result = ShareInputValidator.ValidateShareName("Confidential$");
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Contains("скрытым", result.Message);
    }

    [Theory]
    [InlineData("C:\\Windows")]
    [InlineData("C:\\Windows\\System32")]
    public void ValidateFolderPath_SystemDirectories_ReturnsFailure(string path)
    {
        var result = ShareInputValidator.ValidateFolderPath(path);
        Assert.False(result.IsValid);
        Assert.Contains("Запрещено создавать общий доступ", result.ErrorMessage);
    }

    [Fact]
    public void ValidateFolderPath_DriveRoot_ReturnsWarning()
    {
        var result = ShareInputValidator.ValidateFolderPath("C:\\");
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, result.Severity);
        Assert.Contains("корень диска", result.Message);
    }

    [Fact]
    public void ValidateFolderPath_ExistingFolderPublished_ReturnsInfo()
    {
        var existing = new[] { ("PublicDocs", "D:\\Shared\\Folder") };
        var result = ShareInputValidator.ValidateFolderPath("D:\\Shared\\Folder", existing);
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Info, result.Severity);
        Assert.Contains("уже опубликована", result.Message);
    }

    [Fact]
    public void NetworkService_IsApipa_CorrectlyIdentifiesLinkLocal()
    {
        var apipa = IPAddress.Parse("169.254.1.1");
        var regularLan = IPAddress.Parse("192.168.1.100");
        var loopback = IPAddress.Parse("127.0.0.1");

        Assert.True(WindowsNetworkService.IsApipa(apipa));
        Assert.False(WindowsNetworkService.IsApipa(regularLan));
        Assert.False(WindowsNetworkService.IsApipa(loopback));
    }

    [Fact]
    public void ShareConfigSerializer_ExportAndImport_RoundtripsAccurately()
    {
        var serializer = new ShareConfigSerializer();
        var configs = new List<ShareExportConfig>
        {
            new()
            {
                ShareName = "OBSHAYA",
                Path = "C:\\OBSHAYA",
                Access = AccessMode.ReadWrite.ToString(),
                Firewall = true,
                ApplyToSubfolders = true
            },
            new()
            {
                ShareName = "Documents",
                Path = "C:\\Documents",
                Access = AccessMode.ReadOnly.ToString(),
                Firewall = false,
                ApplyToSubfolders = true
            }
        };

        string json = serializer.Serialize(configs);
        Assert.Contains("OBSHAYA", json);
        Assert.Contains("Documents", json);

        var deserialized = serializer.Deserialize(json);
        Assert.Equal(2, deserialized.Count);
        Assert.Equal("OBSHAYA", deserialized[0].ShareName);
        Assert.Equal("C:\\OBSHAYA", deserialized[0].Path);
        Assert.Equal(AccessMode.ReadWrite.ToString(), deserialized[0].Access);
        Assert.True(deserialized[0].Firewall);

        Assert.Equal("Documents", deserialized[1].ShareName);
        Assert.Equal(AccessMode.ReadOnly.ToString(), deserialized[1].Access);
        Assert.False(deserialized[1].Firewall);
    }

    [Fact]
    public void RemediationAdvisor_UnauthorizedAccess_ReturnsUacAdminGuidance()
    {
        var ex = new UnauthorizedAccessException("Access is denied to SMB resources.");
        var report = LANShareManager.Core.Diagnostics.RemediationAdvisor.Analyze(ex.Message, ex);

        Assert.Equal("UAC_ADMIN", report.CategoryKey);
        Assert.NotEmpty(report.RemediationSteps);
        Assert.Contains("Administrator", report.ErrorTitle);
        Assert.Contains("Запуск от имени администратора", report.RemediationSteps[1]);
    }

    [Fact]
    public void RemediationAdvisor_LanmanServerStopped_ReturnsLanmanWithAutoFix()
    {
        var report = LANShareManager.Core.Diagnostics.RemediationAdvisor.Analyze("The server service is not started (LanmanServer)");

        Assert.Equal("LANMAN_SERVICE", report.CategoryKey);
        Assert.True(report.CanAutoFix);
        Assert.Equal("net start LanmanServer", report.QuickCommand);
    }

    [Fact]
    public void RemediationAdvisor_PublicNetwork_ReturnsNetworkPublicWithAutoFix()
    {
        var report = LANShareManager.Core.Diagnostics.RemediationAdvisor.Analyze("Current network profile is Public (Общедоступная)");

        Assert.Equal("NETWORK_PUBLIC", report.CategoryKey);
        Assert.True(report.CanAutoFix);
        Assert.Contains("Private", report.QuickCommand);
    }

    [Fact]
    public void RemediationAdvisor_FirewallBlocked_ReturnsFirewallGuidance()
    {
        var report = LANShareManager.Core.Diagnostics.RemediationAdvisor.Analyze("Port 445 is blocked by Windows Firewall");

        Assert.Equal("FIREWALL_445", report.CategoryKey);
        Assert.True(report.CanAutoFix);
        Assert.Contains("File and Printer Sharing", report.QuickCommand);
    }

    [Fact]
    public void RemediationAdvisor_SystemFolder_ReturnsSystemFolderProtection()
    {
        var report = LANShareManager.Core.Diagnostics.RemediationAdvisor.Analyze("Запрещено создавать общий доступ к системному каталогу Windows");

        Assert.Equal("SYSTEM_FOLDER", report.CategoryKey);
        Assert.False(report.CanAutoFix);
        Assert.Contains("C:\\Windows", report.ErrorTitle);
    }
}
