using System.Diagnostics;
using System.Runtime.InteropServices;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;
using LANShareManager.Infrastructure.Process;
using Microsoft.Win32;

namespace LANShareManager.Infrastructure.OS;

public class WindowsOsService : IOsService
{
    private readonly ILoggerService? _logger;
    private readonly PowerShellProcessRunner? _runner;
    private WindowsOsInfo? _cachedOsInfo;

    public WindowsOsService(ILoggerService? logger = null, PowerShellProcessRunner? runner = null)
    {
        _logger = logger;
        _runner = runner;
    }

    public WindowsOsInfo GetOsInfo()
    {
        if (_cachedOsInfo != null)
        {
            return _cachedOsInfo;
        }

        var info = new WindowsOsInfo();

        try
        {
            // 1. Process and Native Architecture detection
            info.Architecture = DetectArchitecture();

            // 2. Exact Build Number detection from kernel32.dll (MAS technique: avoids compatibility mode lying)
            info.BuildNumber = DetectKernelBuildNumber();

            // 3. Query Windows Registry for Edition, DisplayVersion, UBR (Update Build Revision)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ReadWindowsRegistry(info);
            }
            else
            {
                info.ProductName = RuntimeInformation.OSDescription;
            }

            // 4. Normalize OS Name (Windows 11 vs 10 detection based on Build >= 22000)
            NormalizeOsName(info);

            // 5. Detect Installed Antivirus Products via root\SecurityCenter2 (MAS diagnostic pattern)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DetectInstalledAntivirus(info);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Error detecting Windows OS details: {ex.Message}");
            if (string.IsNullOrWhiteSpace(info.ProductName))
            {
                info.ProductName = RuntimeInformation.OSDescription;
            }
        }

        _cachedOsInfo = info;
        _logger?.LogInfo($"Detected OS: {info.FullDescription}, AV: {info.AntivirusSummary}");
        return info;
    }

    private void DetectInstalledAntivirus(WindowsOsInfo info)
    {
        try
        {
            var runner = _runner ?? new PowerShellProcessRunner(_logger);
            string script = "Get-CimInstance -Namespace root\\SecurityCenter2 -ClassName AntiVirusProduct -ErrorAction SilentlyContinue | Select-Object -ExpandProperty displayName";
            var res = runner.RunPowerShellCommandAsync(script, 5).GetAwaiter().GetResult();
            if (res.Success && !string.IsNullOrWhiteSpace(res.StandardOutput))
            {
                var lines = res.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && !info.InstalledAntivirus.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    {
                        info.InstalledAntivirus.Add(trimmed);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"Error detecting installed antivirus: {ex.Message}");
        }
    }

    private static int DetectKernelBuildNumber()
    {
        int buildNumber = 0;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string kernel32Path = Path.Combine(sysDir, "kernel32.dll");

                if (File.Exists(kernel32Path))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(kernel32Path);
                    buildNumber = fvi.FileBuildPart;
                }
            }
            catch
            {
                // Fallback below
            }
        }

        if (buildNumber <= 0)
        {
            buildNumber = Environment.OSVersion.Version.Build;
        }

        return buildNumber;
    }

    private static string DetectArchitecture()
    {
        string? nativeArch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
        if (!string.IsNullOrWhiteSpace(nativeArch))
        {
            return nativeArch.ToUpperInvariant() switch
            {
                "AMD64" => "x64",
                "ARM64" => "ARM64",
                "X86" => "x86",
                _ => nativeArch
            };
        }

        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "ARM64",
            Architecture.X86 => "x86",
            Architecture.Arm => "ARM",
            _ => RuntimeInformation.OSArchitecture.ToString()
        };
    }

    private static void ReadWindowsRegistry(WindowsOsInfo info)
    {
#pragma warning disable CA1416 // Validate platform compatibility
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                string? prod = key.GetValue("ProductName")?.ToString();
                if (!string.IsNullOrWhiteSpace(prod))
                {
                    info.ProductName = prod;
                }

                string? displayVer = key.GetValue("DisplayVersion")?.ToString();
                if (string.IsNullOrWhiteSpace(displayVer))
                {
                    displayVer = key.GetValue("ReleaseId")?.ToString();
                }
                if (!string.IsNullOrWhiteSpace(displayVer))
                {
                    info.DisplayVersion = displayVer;
                }

                object? ubrVal = key.GetValue("UBR");
                if (ubrVal != null && int.TryParse(ubrVal.ToString(), out int ubr))
                {
                    info.UpdateRevision = ubr;
                }

                string? installType = key.GetValue("InstallationType")?.ToString();
                if (!string.IsNullOrWhiteSpace(installType))
                {
                    info.InstallationType = installType;
                }

                if (info.BuildNumber <= 0)
                {
                    object? buildVal = key.GetValue("CurrentBuild") ?? key.GetValue("CurrentBuildNumber");
                    if (buildVal != null && int.TryParse(buildVal.ToString(), out int b))
                    {
                        info.BuildNumber = b;
                    }
                }
            }
        }
        catch
        {
            // Registry read failed or access denied
        }
#pragma warning restore CA1416
    }

    private static void NormalizeOsName(WindowsOsInfo info)
    {
        // Windows 11 Build threshold rule: Build >= 22000 is always Windows 11!
        // In early Windows 11 builds, Microsoft kept "Windows 10 Pro" in the ProductName registry value.
        if (info.BuildNumber >= 22000)
        {
            if (string.IsNullOrWhiteSpace(info.ProductName) || info.ProductName.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                info.ProductName = "Windows 11";
            }
            else if (info.ProductName.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
            {
                info.ProductName = "Windows 11" + info.ProductName.Substring("Windows 10".Length);
            }
        }
        else if (info.BuildNumber >= 10240)
        {
            if (string.IsNullOrWhiteSpace(info.ProductName) || info.ProductName.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                info.ProductName = "Windows 10";
            }
        }
        else if (string.IsNullOrWhiteSpace(info.ProductName))
        {
            info.ProductName = RuntimeInformation.OSDescription;
        }
    }
}
