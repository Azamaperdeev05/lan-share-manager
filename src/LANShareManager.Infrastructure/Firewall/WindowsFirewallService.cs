using LANShareManager.Core.Interfaces;
using LANShareManager.Infrastructure.Process;

namespace LANShareManager.Infrastructure.Firewall;

public class WindowsFirewallService : IFirewallService
{
    private readonly ILoggerService _logger;
    private readonly PowerShellProcessRunner _runner;

    public WindowsFirewallService(ILoggerService logger, PowerShellProcessRunner runner)
    {
        _logger = logger;
        _runner = runner;
    }

    public async Task<bool> IsFirewallEnabledAsync()
    {
        // Check firewall state across profiles
        var result = await _runner.RunProcessAsync("netsh.exe", "advfirewall show allprofiles state");
        if (result.Success)
        {
            return result.StandardOutput.Contains("ON", StringComparison.OrdinalIgnoreCase) ||
                   result.StandardOutput.Contains("ВКЛЮЧЕНО", StringComparison.OrdinalIgnoreCase) ||
                   result.StandardOutput.Contains("Включен", StringComparison.OrdinalIgnoreCase);
        }

        // PowerShell fallback
        var psResult = await _runner.RunPowerShellCommandAsync("(Get-NetFirewallProfile -Name Private).Enabled");
        if (psResult.Success && bool.TryParse(psResult.StandardOutput.Trim(), out bool isEnabled))
        {
            return isEnabled;
        }

        return true; // Default assumed enabled for safety
    }

    public async Task<bool> IsSmbRuleEnabledAsync()
    {
        // Check rule for TCP 445 inbound
        string psCheck = @"
$rules = Get-NetFirewallRule -Direction Inbound -Enabled True -ErrorAction SilentlyContinue |
    Get-NetFirewallPortFilter | Where-Object { $_.LocalPort -eq '445' -and $_.Protocol -eq 'TCP' }
if ($rules) { 'True' } else { 'False' }
";
        var res = await _runner.RunPowerShellCommandAsync(psCheck);
        if (res.Success && res.StandardOutput.Trim().Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check via netsh
        var netshRes = await _runner.RunProcessAsync("netsh.exe", "advfirewall firewall show rule name=\"File and Printer Sharing (SMB-In)\"");
        if (netshRes.Success && netshRes.StandardOutput.Contains("Yes", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public async Task<bool> EnableSmbFirewallRulesAsync(bool privateOnly = true)
    {
        _logger.LogInfo($"Configuring Windows Firewall SMB rules (privateOnly={privateOnly})...");

        // Idempotency check: if rule is already active on port 445, do not re-create
        if (await IsSmbRuleEnabledAsync())
        {
            _logger.LogInfo("Firewall rule for SMB (TCP 445) is already enabled.");
            return true;
        }

        string profileArg = privateOnly ? "private" : "private,domain";

        // Step 1: Try enabling the standard built-in "File and Printer Sharing" group in English & Russian
        string[] groupNames = { "File and Printer Sharing", "Общий доступ к файлам и принтерам" };
        bool groupEnabled = false;

        foreach (var group in groupNames)
        {
            var res = await _runner.RunProcessAsync("netsh.exe", $"advfirewall firewall set rule group=\"{group}\" new enable=Yes profile={profileArg}");
            if (res.Success && !res.StandardOutput.Contains("Правило не найдено", StringComparison.OrdinalIgnoreCase) &&
                               !res.StandardOutput.Contains("No rules match", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInfo($"Enabled firewall rule group '{group}'.");
                groupEnabled = true;
                break;
            }
        }

        // Step 2: Ensure targeted rule explicitly on TCP port 445 for the Private profile
        string dedicatedRuleName = "LAN Share Manager - SMB Inbound (TCP 445)";
        string psEnsureRule = $@"
$existing = Get-NetFirewallRule -DisplayName '{dedicatedRuleName}' -ErrorAction SilentlyContinue
if (-not $existing) {{
    New-NetFirewallRule -DisplayName '{dedicatedRuleName}' -Direction Inbound -Protocol TCP -LocalPort 445 -Action Allow -Profile {profileArg} -Description 'Разрешает входящий трафик SMB для локальной сети'
}} else {{
    Set-NetFirewallRule -DisplayName '{dedicatedRuleName}' -Enabled True -Profile {profileArg}
}}
";
        var dedicatedRes = await _runner.RunPowerShellCommandAsync(psEnsureRule);
        if (dedicatedRes.Success)
        {
            _logger.LogInfo("Dedicated inbound SMB firewall rule for TCP 445 ensured.");
            return true;
        }

        // Fallback to netsh rule creation
        string addRuleCmd = $"advfirewall firewall add rule name=\"{dedicatedRuleName}\" dir=in action=allow protocol=TCP localport=445 profile={profileArg}";
        var netshAdd = await _runner.RunProcessAsync("netsh.exe", addRuleCmd);
        return netshAdd.Success || groupEnabled;
    }
}
