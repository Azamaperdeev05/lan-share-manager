using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;
using LANShareManager.Core.Validation;

namespace LANShareManager.Infrastructure.Orchestration;

public class ShareOrchestrator : IShareOrchestrator
{
    private readonly ISmbService _smbService;
    private readonly INtfsPermissionService _ntfsService;
    private readonly IFirewallService _firewallService;
    private readonly INetworkService _networkService;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly ILoggerService _logger;

    public ShareOrchestrator(
        ISmbService smbService,
        INtfsPermissionService ntfsService,
        IFirewallService firewallService,
        INetworkService networkService,
        IDiagnosticsService diagnosticsService,
        ILoggerService logger)
    {
        _smbService = smbService;
        _ntfsService = ntfsService;
        _firewallService = firewallService;
        _networkService = networkService;
        _diagnosticsService = diagnosticsService;
        _logger = logger;
    }

    public async Task<ShareCreationResult> CreateOrConfigureShareAsync(ShareCreationRequest request)
    {
        _logger.LogInfo($"Starting share creation/orchestration for '{request.ShareName}' at '{request.FolderPath}'");

        var result = new ShareCreationResult
        {
            LocalPath = request.FolderPath
        };

        // 1. Validation
        var nameValidation = ShareInputValidator.ValidateShareName(request.ShareName);
        if (!nameValidation.IsValid)
        {
            result.Success = false;
            result.Message = nameValidation.ErrorMessage ?? "Некорректное имя ресурса.";
            return result;
        }

        var pathValidation = ShareInputValidator.ValidateFolderPath(request.FolderPath);
        if (!pathValidation.IsValid)
        {
            result.Success = false;
            result.Message = pathValidation.ErrorMessage ?? "Некорректный путь к папке.";
            return result;
        }

        try
        {
            // 2. Ensure Directory Exists
            if (!Directory.Exists(request.FolderPath))
            {
                Directory.CreateDirectory(request.FolderPath);
                _logger.LogInfo($"Created directory on disk: {request.FolderPath}");
            }

            // 3. NTFS Permissions
            bool ntfsConfigured = _ntfsService.ConfigureFolderPermissions(
                request.FolderPath,
                request.Access,
                request.ApplyPermissionsToSubfolders);
            result.NtfsPermissionsConfigured = ntfsConfigured;

            // 4. Create/Configure SMB Share
            await _smbService.CreateShareAsync(
                request.ShareName,
                request.FolderPath,
                request.Access,
                $"LAN Share Manager: {request.ShareName}");
            result.SmbEnabled = true;
            result.SharePermissionsConfigured = true;

            // 5. Windows Firewall
            if (request.EnableFirewallRules)
            {
                bool fwOk = await _firewallService.EnableSmbFirewallRulesAsync(privateOnly: true);
                result.FirewallConfigured = fwOk;
            }

            // 6. Network Detection
            var netInfo = await _networkService.GetNetworkInfoAsync();
            result.HostnamePath = $"\\\\{netInfo.ComputerName}\\{request.ShareName}";
            result.IpPath = !string.IsNullOrWhiteSpace(netInfo.LocalIPv4) && netInfo.LocalIPv4 != "127.0.0.1"
                ? $"\\\\{netInfo.LocalIPv4}\\{request.ShareName}"
                : result.HostnamePath;

            // 7. Connectivity Testing
            if (request.TestShareAfterCreation)
            {
                result.Diagnostics = await _diagnosticsService.RunFullDiagnosticsAsync(request.ShareName, request.FolderPath);
                result.ConnectivityTested = result.Diagnostics.OverallSuccess;
            }

            result.Success = true;
            result.Message = "Общий ресурс успешно создан и настроен.";
            _logger.LogInfo($"Share orchestration completed successfully for '{request.ShareName}'. Paths: {result.HostnamePath} / {result.IpPath}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to create or configure share '{request.ShareName}'", ex);
            result.Success = false;
            result.Message = $"Ошибка при создании общего ресурса: {ex.Message}";
            return result;
        }
    }

    public async Task<bool> RemoveShareSafelyAsync(string shareName)
    {
        _logger.LogInfo($"Removing SMB share '{shareName}' safely...");
        try
        {
            await _smbService.RemoveShareAsync(shareName);
            _logger.LogInfo($"Share '{shareName}' removed successfully. Files on disk preserved.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error removing share '{shareName}'", ex);
            return false;
        }
    }

    public async Task<bool> UpdateShareAccessAsync(string shareName, AccessMode newAccess, bool updateNtfs = true)
    {
        _logger.LogInfo($"Updating share '{shareName}' access mode to {newAccess} (updateNtfs={updateNtfs})");
        try
        {
            await _smbService.UpdateShareAccessAsync(shareName, newAccess);

            if (updateNtfs)
            {
                var share = await _smbService.GetShareByNameAsync(shareName);
                if (share != null && !string.IsNullOrWhiteSpace(share.Path))
                {
                    _ntfsService.ConfigureFolderPermissions(share.Path, newAccess, applyToSubfolders: true);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating share access for '{shareName}'", ex);
            return false;
        }
    }

    public async Task<DiagnosticReport> TestShareConnectivityAsync(string shareName)
    {
        var share = await _smbService.GetShareByNameAsync(shareName);
        string path = share?.Path ?? string.Empty;
        return await _diagnosticsService.RunFullDiagnosticsAsync(shareName, path);
    }
}
