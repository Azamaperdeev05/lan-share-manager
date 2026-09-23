using LANShareManager.Core.Enums;
using LANShareManager.Core.Models;

namespace LANShareManager.Core.Interfaces;

public interface ISmbService
{
    Task<bool> IsSmbSupportedAsync();
    Task<IReadOnlyList<SmbShareInfo>> GetSharesAsync(bool includeSpecial = false);
    Task<SmbShareInfo?> GetShareByNameAsync(string shareName);
    Task<bool> ShareExistsAsync(string shareName);
    Task CreateShareAsync(string shareName, string folderPath, AccessMode accessMode, string description = "");
    Task UpdateShareAccessAsync(string shareName, AccessMode accessMode);
    Task RemoveShareAsync(string shareName);
}

public interface INtfsPermissionService
{
    bool ConfigureFolderPermissions(string folderPath, AccessMode accessMode, bool applyToSubfolders = true);
    bool CheckFolderPermissions(string folderPath, AccessMode accessMode);
}

public interface IFirewallService
{
    Task<bool> IsFirewallEnabledAsync();
    Task<bool> IsSmbRuleEnabledAsync();
    Task<bool> EnableSmbFirewallRulesAsync(bool privateOnly = true);
}

public interface INetworkService
{
    Task<NetworkInfo> GetNetworkInfoAsync();
    Task<bool> SwitchNetworkToPrivateAsync(string? interfaceAlias = null);
}

public interface IDiagnosticsService
{
    Task<DiagnosticReport> RunFullDiagnosticsAsync(string shareName, string localPath);
}

public interface ILoggerService
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? ex = null);
    string GetLogDirectory();
}

public interface IShareConfigSerializer
{
    string Serialize(IEnumerable<ShareExportConfig> configs);
    IReadOnlyList<ShareExportConfig> Deserialize(string json);
}

public interface IShareOrchestrator
{
    Task<ShareCreationResult> CreateOrConfigureShareAsync(ShareCreationRequest request);
    Task<bool> RemoveShareSafelyAsync(string shareName);
    Task<bool> UpdateShareAccessAsync(string shareName, AccessMode newAccess, bool updateNtfs = true);
    Task<DiagnosticReport> TestShareConnectivityAsync(string shareName);
}
