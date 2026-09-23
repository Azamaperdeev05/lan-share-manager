using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;
using LANShareManager.Infrastructure.Orchestration;
using Xunit;

namespace LANShareManager.Tests;

public class MockSmbService : ISmbService
{
    public readonly Dictionary<string, (string Path, AccessMode Access)> ExistingShares = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> IsSmbSupportedAsync() => Task.FromResult(true);

    public Task<IReadOnlyList<SmbShareInfo>> GetSharesAsync(bool includeSpecial = false)
    {
        var list = ExistingShares.Select(kv => new SmbShareInfo
        {
            Name = kv.Key,
            Path = kv.Value.Path,
            Access = kv.Value.Access,
            Status = "Active",
            IsSpecial = false
        }).ToList();
        return Task.FromResult<IReadOnlyList<SmbShareInfo>>(list);
    }

    public Task<SmbShareInfo?> GetShareByNameAsync(string shareName)
    {
        if (ExistingShares.TryGetValue(shareName, out var val))
        {
            return Task.FromResult<SmbShareInfo?>(new SmbShareInfo
            {
                Name = shareName,
                Path = val.Path,
                Access = val.Access,
                Status = "Active"
            });
        }
        return Task.FromResult<SmbShareInfo?>(null);
    }

    public Task<bool> ShareExistsAsync(string shareName) => Task.FromResult(ExistingShares.ContainsKey(shareName));

    public Task CreateShareAsync(string shareName, string folderPath, AccessMode accessMode, string description = "")
    {
        ExistingShares[shareName] = (folderPath, accessMode);
        return Task.CompletedTask;
    }

    public Task UpdateShareAccessAsync(string shareName, AccessMode accessMode)
    {
        if (ExistingShares.TryGetValue(shareName, out var val))
        {
            ExistingShares[shareName] = (val.Path, accessMode);
        }
        return Task.CompletedTask;
    }

    public Task RemoveShareAsync(string shareName)
    {
        ExistingShares.Remove(shareName);
        return Task.CompletedTask;
    }
}

public class MockNtfsService : INtfsPermissionService
{
    public readonly Dictionary<string, AccessMode> ConfiguredPaths = new();

    public bool ConfigureFolderPermissions(string folderPath, AccessMode accessMode, bool applyToSubfolders = true)
    {
        ConfiguredPaths[folderPath] = accessMode;
        return true;
    }

    public bool CheckFolderPermissions(string folderPath, AccessMode accessMode)
    {
        return ConfiguredPaths.TryGetValue(folderPath, out var current) && current == accessMode;
    }
}

public class MockFirewallService : IFirewallService
{
    public bool Enabled { get; set; } = true;
    public bool SmbRuleEnabled { get; set; } = true;

    public Task<bool> IsFirewallEnabledAsync() => Task.FromResult(Enabled);
    public Task<bool> IsSmbRuleEnabledAsync() => Task.FromResult(SmbRuleEnabled);
    public Task<bool> EnableSmbFirewallRulesAsync(bool privateOnly = true)
    {
        SmbRuleEnabled = true;
        return Task.FromResult(true);
    }
}

public class MockNetworkService : INetworkService
{
    public Task<NetworkInfo> GetNetworkInfoAsync() => Task.FromResult(new NetworkInfo
    {
        ComputerName = "TEST-PC",
        LocalIPv4 = "192.168.1.150",
        NetworkProfile = NetworkCategory.Private,
        ActiveAdapterName = "Ethernet",
        DefaultGateway = "192.168.1.1"
    });

    public Task<bool> SwitchNetworkToPrivateAsync(string? interfaceAlias = null) => Task.FromResult(true);
}

public class MockDiagnosticsService : IDiagnosticsService
{
    public Task<DiagnosticReport> RunFullDiagnosticsAsync(string shareName, string localPath)
    {
        var report = new DiagnosticReport();
        report.Items.Add(new DiagnosticItem { Name = "SMB Service", Status = DiagnosticStatus.Success, Details = "Running" });
        report.Items.Add(new DiagnosticItem { Name = "Port 445", Status = DiagnosticStatus.Success, Details = "Listening" });
        report.Items.Add(new DiagnosticItem { Name = "Share Exists", Status = DiagnosticStatus.Success, Details = "Exists" });
        return Task.FromResult(report);
    }
}

public class MockLogger : ILoggerService
{
    public void LogError(string message, Exception? ex = null) { }
    public void LogInfo(string message) { }
    public void LogWarning(string message) { }
    public string GetLogDirectory() => "/tmp";
}

public class ServiceUnitTests
{
    [Fact]
    public async Task Orchestrator_CreateShare_ConfiguresEverythingIdempotently()
    {
        var smb = new MockSmbService();
        var ntfs = new MockNtfsService();
        var fw = new MockFirewallService();
        var net = new MockNetworkService();
        var diag = new MockDiagnosticsService();
        var logger = new MockLogger();

        var orchestrator = new ShareOrchestrator(smb, ntfs, fw, net, diag, logger);

        string tempFolder = Path.Combine(Path.GetTempPath(), "TestShare_" + Guid.NewGuid().ToString("N"));

        var request = new ShareCreationRequest
        {
            FolderPath = tempFolder,
            ShareName = "TEST_SHARE",
            Access = AccessMode.ReadWrite,
            EnableFirewallRules = true,
            ApplyPermissionsToSubfolders = true,
            TestShareAfterCreation = true
        };

        var result = await orchestrator.CreateOrConfigureShareAsync(request);

        Assert.True(result.Success);
        Assert.True(Directory.Exists(tempFolder));
        Assert.True(await smb.ShareExistsAsync("TEST_SHARE"));
        Assert.True(result.SmbEnabled);
        Assert.True(result.SharePermissionsConfigured);
        Assert.True(result.NtfsPermissionsConfigured);
        Assert.True(result.FirewallConfigured);
        Assert.Equal(@"\\TEST-PC\TEST_SHARE", result.HostnamePath);
        Assert.Equal(@"\\192.168.1.150\TEST_SHARE", result.IpPath);

        // Run second time to verify idempotency
        var secondResult = await orchestrator.CreateOrConfigureShareAsync(request);
        Assert.True(secondResult.Success);

        // Remove share safely - directory on disk must remain
        bool removed = await orchestrator.RemoveShareSafelyAsync("TEST_SHARE");
        Assert.True(removed);
        Assert.False(await smb.ShareExistsAsync("TEST_SHARE"));
        Assert.True(Directory.Exists(tempFolder)); // Folder on disk still exists!

        // Cleanup temp folder
        Directory.Delete(tempFolder, true);
    }

    [Theory]
    [InlineData(AccessMode.ReadOnly)]
    [InlineData(AccessMode.ReadWrite)]
    [InlineData(AccessMode.FullControl)]
    public async Task Orchestrator_PermissionModes_SetsProperAccess(AccessMode mode)
    {
        var smb = new MockSmbService();
        var ntfs = new MockNtfsService();
        var fw = new MockFirewallService();
        var net = new MockNetworkService();
        var diag = new MockDiagnosticsService();
        var logger = new MockLogger();

        var orchestrator = new ShareOrchestrator(smb, ntfs, fw, net, diag, logger);
        string tempFolder = Path.Combine(Path.GetTempPath(), "TestMode_" + Guid.NewGuid().ToString("N"));

        var req = new ShareCreationRequest
        {
            FolderPath = tempFolder,
            ShareName = "MODE_SHARE",
            Access = mode,
            EnableFirewallRules = false,
            TestShareAfterCreation = false
        };

        var res = await orchestrator.CreateOrConfigureShareAsync(req);
        Assert.True(res.Success);

        var share = await smb.GetShareByNameAsync("MODE_SHARE");
        Assert.NotNull(share);
        Assert.Equal(mode, share.Access);
        Assert.Equal(mode, ntfs.ConfiguredPaths[tempFolder]);

        if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder);
    }
}
