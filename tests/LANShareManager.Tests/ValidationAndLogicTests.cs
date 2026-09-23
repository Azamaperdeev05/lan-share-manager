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
}
