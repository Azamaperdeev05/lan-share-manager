using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;
using LANShareManager.Infrastructure.Process;

using LANShareManager.Infrastructure.OS;

namespace LANShareManager.Infrastructure.Network;

public class WindowsNetworkService : INetworkService
{
    private readonly ILoggerService _logger;
    private readonly PowerShellProcessRunner _runner;
    private readonly IOsService _osService;

    public WindowsNetworkService(ILoggerService logger, PowerShellProcessRunner runner, IOsService? osService = null)
    {
        _logger = logger;
        _runner = runner;
        _osService = osService ?? new WindowsOsService(logger);
    }

    public async Task<NetworkInfo> GetNetworkInfoAsync()
    {
        var info = new NetworkInfo
        {
            ComputerName = Environment.MachineName,
            SmbPort = 445,
            OsInfo = _osService.GetOsInfo()
        };

        // Network interfaces inspection
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            // Try to find the adapter with an IPv4 default gateway
            NetworkInterface? activeAdapter = null;
            IPAddress? selectedIp = null;
            string gateway = string.Empty;

            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                var gateways = ipProps.GatewayAddresses
                    .Where(g => g.Address.AddressFamily == AddressFamily.InterNetwork &&
                               !g.Address.Equals(IPAddress.Any) &&
                               !g.Address.Equals(IPAddress.None))
                    .ToList();

                if (gateways.Count > 0)
                {
                    // Find suitable IPv4 address on this adapter
                    var unicast = ipProps.UnicastAddresses
                        .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork &&
                                            !IPAddress.IsLoopback(u.Address) &&
                                            !IsApipa(u.Address));

                    if (unicast != null)
                    {
                        activeAdapter = ni;
                        selectedIp = unicast.Address;
                        gateway = gateways[0].Address.ToString();
                        break;
                    }
                }
            }

            // Fallback if no gateway found (e.g. ad-hoc LAN or bridge)
            if (selectedIp == null)
            {
                foreach (var ni in interfaces)
                {
                    var unicast = ni.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork &&
                                            !IPAddress.IsLoopback(u.Address) &&
                                            !IsApipa(u.Address));
                    if (unicast != null)
                    {
                        activeAdapter = ni;
                        selectedIp = unicast.Address;
                        break;
                    }
                }
            }

            info.LocalIPv4 = selectedIp?.ToString() ?? "127.0.0.1";
            info.ActiveAdapterName = activeAdapter?.Name ?? (activeAdapter?.Description ?? "Локальное подключение");
            info.DefaultGateway = gateway;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error detecting network adapters: {ex.Message}");
            info.LocalIPv4 = "127.0.0.1";
        }

        // Query Windows Network Category Profile (Private, Public, Domain)
        info.NetworkProfile = await DetectNetworkCategoryAsync(info.ActiveAdapterName);

        _logger.LogInfo($"Detected Network: PC={info.ComputerName}, OS={info.OsInfo?.FullDescription}, IP={info.LocalIPv4}, Profile={info.NetworkProfile}, Adapter={info.ActiveAdapterName}");
        return info;
    }

    public async Task<bool> SwitchNetworkToPrivateAsync(string? interfaceAlias = null)
    {
        _logger.LogInfo("Attempting to switch network profile to Private...");
        string script = string.IsNullOrWhiteSpace(interfaceAlias)
            ? "Get-NetConnectionProfile | Set-NetConnectionProfile -NetworkCategory Private"
            : $"Get-NetConnectionProfile -InterfaceAlias '{interfaceAlias}' | Set-NetConnectionProfile -NetworkCategory Private";

        var res = await _runner.RunPowerShellCommandAsync(script);
        if (res.Success)
        {
            _logger.LogInfo("Successfully updated network profile to Private.");
            return true;
        }

        _logger.LogWarning($"Failed to switch network profile: {res.StandardError}");
        return false;
    }

    private async Task<NetworkCategory> DetectNetworkCategoryAsync(string adapterName)
    {
        try
        {
            string ps = "Get-NetConnectionProfile | Select-Object -ExpandProperty NetworkCategory";
            var res = await _runner.RunPowerShellCommandAsync(ps);
            if (res.Success && !string.IsNullOrWhiteSpace(res.StandardOutput))
            {
                string text = res.StandardOutput.Trim();
                if (text.Contains("Private", StringComparison.OrdinalIgnoreCase)) return NetworkCategory.Private;
                if (text.Contains("Domain", StringComparison.OrdinalIgnoreCase)) return NetworkCategory.DomainAuthenticated;
                if (text.Contains("Public", StringComparison.OrdinalIgnoreCase)) return NetworkCategory.Public;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"DetectNetworkCategory error: {ex.Message}");
        }

        return NetworkCategory.Unknown;
    }

    public static bool IsApipa(IPAddress ip)
    {
        byte[] bytes = ip.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254;
    }
}
