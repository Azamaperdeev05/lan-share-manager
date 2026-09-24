using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Infrastructure.Process;

namespace LANShareManager.Infrastructure.NTFS;

[SupportedOSPlatform("windows")]
public class NtfsPermissionService : INtfsPermissionService
{
    private readonly ILoggerService _logger;
    private readonly PowerShellProcessRunner _runner;

    public NtfsPermissionService(ILoggerService logger, PowerShellProcessRunner runner)
    {
        _logger = logger;
        _runner = runner;
    }

    public bool ConfigureFolderPermissions(string folderPath, AccessMode accessMode, bool applyToSubfolders = true)
    {
        _logger.LogInfo($"Configuring NTFS permissions on '{folderPath}' for mode {accessMode} (Subfolders={applyToSubfolders})");

        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning($"Directory '{folderPath}' does not exist. Creating it first.");
            Directory.CreateDirectory(folderPath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // Pre-flight check: Non-NTFS drives (FAT32, exFAT) do not support NTFS ACLs.
                // In such cases, security is managed purely at the SMB share permission level.
                string? root = Path.GetPathRoot(folderPath);
                if (!string.IsNullOrWhiteSpace(root))
                {
                    try
                    {
                        var drive = new DriveInfo(root);
                        if (drive.IsReady &&
                            !drive.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase) &&
                            !drive.DriveFormat.Equals("ReFS", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInfo($"Drive '{root}' is formatted as {drive.DriveFormat} (no NTFS ACL support). Permissions enforced via SMB.");
                            return true;
                        }
                    }
                    catch { }
                }

                var directoryInfo = new DirectoryInfo(folderPath);
                DirectorySecurity security = directoryInfo.GetAccessControl(AccessControlSections.Access);

                var everyoneSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                FileSystemRights rights = GetRightsForAccessMode(accessMode);

                InheritanceFlags inheritance = applyToSubfolders
                    ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit
                    : InheritanceFlags.None;

                PropagationFlags propagation = PropagationFlags.None;

                // Idempotency check: see if an existing rule for WorldSid already grants at least these rights
                bool alreadyGranted = false;
                foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                {
                    if (rule.IdentityReference.Equals(everyoneSid) &&
                        rule.AccessControlType == AccessControlType.Allow &&
                        (rule.FileSystemRights & rights) == rights)
                    {
                        alreadyGranted = true;
                        break;
                    }
                }

                if (alreadyGranted)
                {
                    _logger.LogInfo($"NTFS permissions for Everyone ({accessMode}) are already satisfied on '{folderPath}'.");
                    return true;
                }

                // Remove previous Everyone rules to avoid duplicate/conflicting rules
                security.PurgeAccessRules(everyoneSid);

                // Add new desired rule for Everyone
                var newRule = new FileSystemAccessRule(
                    everyoneSid,
                    rights,
                    inheritance,
                    propagation,
                    AccessControlType.Allow);

                security.AddAccessRule(newRule);

                // Ensure Administrators also have FullControl explicitly
                var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                var adminRule = new FileSystemAccessRule(
                    adminSid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);
                security.AddAccessRule(adminRule);

                // Apply without modifying SYSTEM, creator or owner rules
                directoryInfo.SetAccessControl(security);
                _logger.LogInfo($"Successfully applied NTFS ACL via System.Security.AccessControl on '{folderPath}'.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed applying ACL via System.Security.AccessControl: {ex.Message}. Falling back to icacls...");
            }
        }

        // Fallback using icacls command line tool
        return ConfigurePermissionsWithIcacls(folderPath, accessMode, applyToSubfolders);
    }

    public bool CheckFolderPermissions(string folderPath, AccessMode accessMode)
    {
        if (!Directory.Exists(folderPath))
            return false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var directoryInfo = new DirectoryInfo(folderPath);
                DirectorySecurity security = directoryInfo.GetAccessControl(AccessControlSections.Access);
                var everyoneSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                FileSystemRights requiredRights = GetRightsForAccessMode(accessMode);

                foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                {
                    if (rule.IdentityReference.Equals(everyoneSid) &&
                        rule.AccessControlType == AccessControlType.Allow)
                    {
                        if ((rule.FileSystemRights & requiredRights) == requiredRights)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"CheckFolderPermissions: {ex.Message}");
            }
        }

        return true;
    }

    private bool ConfigurePermissionsWithIcacls(string folderPath, AccessMode accessMode, bool applyToSubfolders)
    {
        // icacls: (OI)(CI) = container and object inherit
        // R = Read/Execute, M = Modify, F = Full
        string permChar = accessMode switch
        {
            AccessMode.ReadOnly => "(OI)(CI)RX",
            AccessMode.ReadWrite => "(OI)(CI)M",
            AccessMode.FullControl => "(OI)(CI)F",
            _ => "(OI)(CI)M"
        };

        string safePath = folderPath.TrimEnd('\\');
        string args = $"\"{safePath}\" /grant:r \"*S-1-1-0\":{permChar} /T /C /Q";

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "icacls.exe",
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(10000);
                if (proc.ExitCode == 0)
                {
                    _logger.LogInfo($"Successfully applied NTFS permissions via icacls on '{folderPath}'.");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"icacls execution failed on '{folderPath}': {ex.Message}");
        }

        return false;
    }

    private static FileSystemRights GetRightsForAccessMode(AccessMode mode) => mode switch
    {
        AccessMode.ReadOnly => FileSystemRights.ReadAndExecute | FileSystemRights.Synchronize,
        AccessMode.ReadWrite => FileSystemRights.Modify | FileSystemRights.Synchronize,
        AccessMode.FullControl => FileSystemRights.FullControl,
        _ => FileSystemRights.Modify
    };
}
