using LANShareManager.Core.Enums;

namespace LANShareManager.Core.Models;

public class SmbShareInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AccessMode Access { get; set; } = AccessMode.ReadWrite;
    public string Status { get; set; } = "Active";
    public bool IsSpecial { get; set; }

    public string DisplayAccess => Access switch
    {
        AccessMode.ReadOnly => "Только чтение",
        AccessMode.ReadWrite => "Чтение и запись",
        AccessMode.FullControl => "Полный доступ",
        _ => Access.ToString()
    };
}

public class ShareCreationRequest
{
    public string FolderPath { get; set; } = string.Empty;
    public string ShareName { get; set; } = string.Empty;
    public AccessMode Access { get; set; } = AccessMode.ReadWrite;
    public bool EnableFirewallRules { get; set; } = true;
    public bool ApplyPermissionsToSubfolders { get; set; } = true;
    public bool TestShareAfterCreation { get; set; } = true;
}

public class ShareCreationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string HostnamePath { get; set; } = string.Empty;
    public string IpPath { get; set; } = string.Empty;
    public bool SmbEnabled { get; set; }
    public bool SharePermissionsConfigured { get; set; }
    public bool NtfsPermissionsConfigured { get; set; }
    public bool FirewallConfigured { get; set; }
    public bool ConnectivityTested { get; set; }
    public DiagnosticReport? Diagnostics { get; set; }
}

public class NetworkInfo
{
    public string ComputerName { get; set; } = string.Empty;
    public string LocalIPv4 { get; set; } = string.Empty;
    public string ActiveAdapterName { get; set; } = string.Empty;
    public string DefaultGateway { get; set; } = string.Empty;
    public NetworkCategory NetworkProfile { get; set; } = NetworkCategory.Unknown;
    public int SmbPort { get; set; } = 445;

    public string NetworkProfileDisplay => NetworkProfile switch
    {
        NetworkCategory.Private => "Частная (Private)",
        NetworkCategory.Public => "Общедоступная (Public)",
        NetworkCategory.DomainAuthenticated => "Доменная (Domain)",
        _ => "Неизвестно"
    };
}

public class DiagnosticItem
{
    public string Name { get; set; } = string.Empty;
    public DiagnosticStatus Status { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? SuggestedFix { get; set; }

    public string StatusSymbol => Status switch
    {
        DiagnosticStatus.Success => "✓",
        DiagnosticStatus.Warning => "⚠",
        DiagnosticStatus.Failure => "✗",
        _ => "ℹ"
    };
}

public class DiagnosticReport
{
    public List<DiagnosticItem> Items { get; set; } = new();
    public bool OverallSuccess => Items.All(i => i.Status != DiagnosticStatus.Failure);
}

public class ShareExportConfig
{
    public string ShareName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Access { get; set; } = "ReadWrite";
    public bool Firewall { get; set; } = true;
    public bool ApplyToSubfolders { get; set; } = true;
}
