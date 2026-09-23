using System.Text.Json;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;
using LANShareManager.Infrastructure.Process;

namespace LANShareManager.Infrastructure.SMB;

public class WindowsSmbService : ISmbService
{
    private readonly ILoggerService _logger;
    private readonly PowerShellProcessRunner _runner;
    private bool? _smbCmdletsAvailable;

    public WindowsSmbService(ILoggerService logger, PowerShellProcessRunner runner)
    {
        _logger = logger;
        _runner = runner;
    }

    public async Task<bool> IsSmbSupportedAsync()
    {
        if (_smbCmdletsAvailable.HasValue)
            return _smbCmdletsAvailable.Value;

        var result = await _runner.RunPowerShellCommandAsync("Get-Command Get-SmbShare -ErrorAction SilentlyContinue");
        _smbCmdletsAvailable = result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput);
        return _smbCmdletsAvailable.Value;
    }

    public async Task<IReadOnlyList<SmbShareInfo>> GetSharesAsync(bool includeSpecial = false)
    {
        _logger.LogInfo($"Fetching SMB shares (includeSpecial={includeSpecial})...");
        var shares = new List<SmbShareInfo>();

        bool hasCmdlets = await IsSmbSupportedAsync();
        if (hasCmdlets)
        {
            string psScript = @"
Get-SmbShare | Select-Object Name, Path, Description, Special | ConvertTo-Json -Compress
";
            var result = await _runner.RunPowerShellCommandAsync(psScript);
            if (result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                try
                {
                    using var doc = JsonDocument.Parse(result.StandardOutput);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in doc.RootElement.EnumerateArray())
                        {
                            var info = ParseShareElement(elem);
                            if (info != null && (includeSpecial || !info.IsSpecial))
                                shares.Add(info);
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        var info = ParseShareElement(doc.RootElement);
                        if (info != null && (includeSpecial || !info.IsSpecial))
                            shares.Add(info);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to parse Get-SmbShare JSON output: {ex.Message}. Falling back to net share.");
                }
            }
        }

        if (shares.Count == 0)
        {
            // Fallback to "net share" parsing
            shares.AddRange(await GetSharesViaNetShareAsync(includeSpecial));
        }

        // Enrich with access permissions for non-special shares
        foreach (var share in shares.Where(s => !s.IsSpecial))
        {
            share.Access = await GetShareAccessModeAsync(share.Name);
        }

        return shares;
    }

    public async Task<SmbShareInfo?> GetShareByNameAsync(string shareName)
    {
        var all = await GetSharesAsync(includeSpecial: true);
        return all.FirstOrDefault(s => string.Equals(s.Name, shareName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ShareExistsAsync(string shareName)
    {
        var share = await GetShareByNameAsync(shareName);
        return share != null;
    }

    public async Task CreateShareAsync(string shareName, string folderPath, AccessMode accessMode, string description = "")
    {
        _logger.LogInfo($"Creating SMB share '{shareName}' on path '{folderPath}' with access {accessMode}");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        bool hasCmdlets = await IsSmbSupportedAsync();

        if (hasCmdlets)
        {
            string accessParam = accessMode switch
            {
                AccessMode.ReadOnly => "-ReadAccess 'Everyone'",
                AccessMode.ReadWrite => "-ChangeAccess 'Everyone'",
                AccessMode.FullControl => "-FullAccess 'Everyone'",
                _ => "-ChangeAccess 'Everyone'"
            };

            // Also grant Administrators FullAccess
            string script = $@"
$exists = Get-SmbShare -Name '{shareName}' -ErrorAction SilentlyContinue
if ($exists) {{
    Set-SmbShare -Name '{shareName}' -Path '{folderPath}' -Description '{description}' -Force
}} else {{
    New-SmbShare -Name '{shareName}' -Path '{folderPath}' -Description '{description}' {accessParam} -FullAccess 'Administrators'
}}
";
            var result = await _runner.RunPowerShellCommandAsync(script);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Не удалось создать общий ресурс SMB '{shareName}': {result.StandardError}");
            }

            // Ensure access rights are specifically updated
            await UpdateShareAccessAsync(shareName, accessMode);
        }
        else
        {
            // Fallback via "net share"
            string permArg = accessMode switch
            {
                AccessMode.ReadOnly => "READ",
                AccessMode.ReadWrite => "CHANGE",
                AccessMode.FullControl => "FULL",
                _ => "CHANGE"
            };

            string netArgs = $"share \"{shareName}={folderPath}\" /GRANT:Everyone,{permArg} /REMARK:\"{description}\"";
            var res = await _runner.RunProcessAsync("net.exe", netArgs);
            if (!res.Success && !res.StandardOutput.Contains("уже существует", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Ошибка создания ресурса net share: {res.StandardError} {res.StandardOutput}");
            }
        }
    }

    public async Task UpdateShareAccessAsync(string shareName, AccessMode accessMode)
    {
        _logger.LogInfo($"Updating SMB permissions for '{shareName}' to {accessMode}");

        bool hasCmdlets = await IsSmbSupportedAsync();
        if (hasCmdlets)
        {
            string right = accessMode switch
            {
                AccessMode.ReadOnly => "Read",
                AccessMode.ReadWrite => "Change",
                AccessMode.FullControl => "Full",
                _ => "Change"
            };

            string script = $@"
Revoke-SmbShareAccess -Name '{shareName}' -AccountName 'Everyone' -Force -ErrorAction SilentlyContinue
Grant-SmbShareAccess -Name '{shareName}' -AccountName 'Everyone' -AccessRight {right} -Force
Grant-SmbShareAccess -Name '{shareName}' -AccountName 'Administrators' -AccessRight Full -Force
";
            var result = await _runner.RunPowerShellCommandAsync(script);
            if (!result.Success)
            {
                _logger.LogWarning($"UpdateShareAccessAsync PowerShell warning: {result.StandardError}");
            }
        }
        else
        {
            string permArg = accessMode switch
            {
                AccessMode.ReadOnly => "READ",
                AccessMode.ReadWrite => "CHANGE",
                AccessMode.FullControl => "FULL",
                _ => "CHANGE"
            };
            await _runner.RunProcessAsync("net.exe", $"share \"{shareName}\" /GRANT:Everyone,{permArg}");
        }
    }

    public async Task RemoveShareAsync(string shareName)
    {
        _logger.LogInfo($"Removing SMB share '{shareName}' (underlying folder remains untouched)...");

        bool hasCmdlets = await IsSmbSupportedAsync();
        if (hasCmdlets)
        {
            var res = await _runner.RunPowerShellCommandAsync($"Remove-SmbShare -Name '{shareName}' -Force");
            if (!res.Success)
            {
                throw new InvalidOperationException($"Ошибка при удалении SMB ресурса '{shareName}': {res.StandardError}");
            }
        }
        else
        {
            var res = await _runner.RunProcessAsync("net.exe", $"share \"{shareName}\" /DELETE /Y");
            if (!res.Success)
            {
                throw new InvalidOperationException($"Ошибка при удалении через net share: {res.StandardError}");
            }
        }

        _logger.LogInfo($"Successfully removed SMB share '{shareName}'. Directory on disk was NOT deleted.");
    }

    private async Task<AccessMode> GetShareAccessModeAsync(string shareName)
    {
        try
        {
            bool hasCmdlets = await IsSmbSupportedAsync();
            if (hasCmdlets)
            {
                string script = $@"
Get-SmbShareAccess -Name '{shareName}' | Where-Object {{ $_.AccountName -match 'Everyone|Все|S-1-1-0' }} | Select-Object -ExpandProperty AccessRight
";
                var res = await _runner.RunPowerShellCommandAsync(script);
                if (res.Success && !string.IsNullOrWhiteSpace(res.StandardOutput))
                {
                    string outText = res.StandardOutput.Trim();
                    if (outText.Contains("Full", StringComparison.OrdinalIgnoreCase)) return AccessMode.FullControl;
                    if (outText.Contains("Change", StringComparison.OrdinalIgnoreCase)) return AccessMode.ReadWrite;
                    if (outText.Contains("Read", StringComparison.OrdinalIgnoreCase)) return AccessMode.ReadOnly;
                }
            }
        }
        catch
        {
            // Ignore failure, default to ReadWrite
        }
        return AccessMode.ReadWrite;
    }

    private static SmbShareInfo? ParseShareElement(JsonElement elem)
    {
        string name = elem.TryGetProperty("Name", out var pName) ? pName.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(name)) return null;

        string path = elem.TryGetProperty("Path", out var pPath) ? pPath.GetString() ?? "" : "";
        string desc = elem.TryGetProperty("Description", out var pDesc) ? pDesc.GetString() ?? "" : "";
        bool special = elem.TryGetProperty("Special", out var pSpecial) && pSpecial.GetBoolean();

        if (name.EndsWith("$") || name.Equals("IPC$", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("ADMIN$", StringComparison.OrdinalIgnoreCase))
        {
            special = true;
        }

        return new SmbShareInfo
        {
            Name = name,
            Path = path,
            Description = desc,
            IsSpecial = special,
            Status = "Active"
        };
    }

    private async Task<List<SmbShareInfo>> GetSharesViaNetShareAsync(bool includeSpecial)
    {
        var list = new List<SmbShareInfo>();
        var res = await _runner.RunProcessAsync("net.exe", "share");
        if (!res.Success) return list;

        string[] lines = res.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        bool headerPassed = false;

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.StartsWith("---"))
            {
                headerPassed = true;
                continue;
            }
            if (!headerPassed || line.Contains("Команда выполнена успешно") || line.Contains("The command completed successfully"))
                continue;

            string[] parts = rawLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                string name = parts[0];
                string path = parts[1];
                bool isSpecial = name.EndsWith("$") || name.Equals("IPC$", StringComparison.OrdinalIgnoreCase);

                if (includeSpecial || !isSpecial)
                {
                    list.Add(new SmbShareInfo
                    {
                        Name = name,
                        Path = path,
                        IsSpecial = isSpecial,
                        Status = "Active"
                    });
                }
            }
        }
        return list;
    }
}
