using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;

namespace LANShareManager.Infrastructure.Diagnostics;

public class WindowsDiagnosticsService : IDiagnosticsService
{
    private readonly ISmbService _smbService;
    private readonly INtfsPermissionService _ntfsService;
    private readonly IFirewallService _firewallService;
    private readonly INetworkService _networkService;
    private readonly ILoggerService _logger;

    public WindowsDiagnosticsService(
        ISmbService smbService,
        INtfsPermissionService ntfsService,
        IFirewallService firewallService,
        INetworkService networkService,
        ILoggerService logger)
    {
        _smbService = smbService;
        _ntfsService = ntfsService;
        _firewallService = firewallService;
        _networkService = networkService;
        _logger = logger;
    }

    public async Task<DiagnosticReport> RunFullDiagnosticsAsync(string shareName, string localPath)
    {
        _logger.LogInfo($"Running comprehensive diagnostics for share '{shareName}' (Path: '{localPath}')...");
        var report = new DiagnosticReport();
        var netInfo = await _networkService.GetNetworkInfoAsync();

        // 1. LanmanServer (Служба "Сервер")
        report.Items.Add(CheckLanmanServerService());

        // 2. TCP Port 445 (SMB) listening
        report.Items.Add(await CheckSmbPortListeningAsync());

        // 3. Share Existence
        bool shareExists = await _smbService.ShareExistsAsync(shareName);
        report.Items.Add(new DiagnosticItem
        {
            Name = $"Общий ресурс '{shareName}'",
            Status = shareExists ? DiagnosticStatus.Success : DiagnosticStatus.Failure,
            Details = shareExists ? "Ресурс зарегистрирован в SMB подсистеме Windows" : "Общий ресурс не найден в списке активных SMB ресурсов",
            SuggestedFix = shareExists ? null : "Создайте общий ресурс через кнопку 'Создать общий доступ'."
        });

        // 4. Local Folder Existence
        bool folderExists = Directory.Exists(localPath);
        report.Items.Add(new DiagnosticItem
        {
            Name = "Локальная папка на диске",
            Status = folderExists ? DiagnosticStatus.Success : DiagnosticStatus.Failure,
            Details = folderExists ? $"Папка существует: {localPath}" : $"Каталог отсутствует: {localPath}",
            SuggestedFix = folderExists ? null : "Убедитесь, что путь указан верно и папка не была удалена."
        });

        // 5. NTFS Permissions
        if (folderExists)
        {
            bool ntfsOk = _ntfsService.CheckFolderPermissions(localPath, AccessMode.ReadOnly);
            report.Items.Add(new DiagnosticItem
            {
                Name = "Разрешения безопасности NTFS",
                Status = ntfsOk ? DiagnosticStatus.Success : DiagnosticStatus.Warning,
                Details = ntfsOk ? "Разрешения для группы 'Все' (Everyone) настроены" : "В списках ACL папки не найдены ожидаемые права для группы 'Все'",
                SuggestedFix = ntfsOk ? null : "Перенастройте разрешения через свойства общего ресурса."
            });
        }

        // 6. Windows Firewall
        bool firewallSmbAllowed = await _firewallService.IsSmbRuleEnabledAsync();
        report.Items.Add(new DiagnosticItem
        {
            Name = "Брандмауэр Windows (SMB TCP 445)",
            Status = firewallSmbAllowed ? DiagnosticStatus.Success : DiagnosticStatus.Warning,
            Details = firewallSmbAllowed ? "Входящие подключения SMB разрешены правилом брандмауэра" : "Правило для SMB (порт 445) не активно в брандмауэре",
            SuggestedFix = firewallSmbAllowed ? null : "Включите опцию 'Включить правила брандмауэра для SMB'."
        });

        // 7. Network Profile Category
        if (netInfo.NetworkProfile == NetworkCategory.Public)
        {
            report.Items.Add(new DiagnosticItem
            {
                Name = "Сетевой профиль Windows",
                Status = DiagnosticStatus.Warning,
                Details = "Текущая сеть определена как 'Общедоступная' (Public). В этом режиме Windows блокирует обнаружение и общий доступ к файлам.",
                SuggestedFix = "Переключите тип сети на 'Частная' (Private) в параметрах сети Windows."
            });
        }
        else
        {
            report.Items.Add(new DiagnosticItem
            {
                Name = "Сетевой профиль Windows",
                Status = DiagnosticStatus.Success,
                Details = $"Текущий профиль: {netInfo.NetworkProfileDisplay}",
                SuggestedFix = null
            });
        }

        // 8. Probe \\localhost\SHARE
        report.Items.Add(TestUncPathAccess($"\\\\localhost\\{shareName}", "Доступ через \\\\localhost"));

        // 9. Probe \\COMPUTERNAME\SHARE
        report.Items.Add(TestUncPathAccess($"\\\\{netInfo.ComputerName}\\{shareName}", $"Доступ через имя ПК (\\\\{netInfo.ComputerName})"));

        // 10. Probe \\IP\SHARE
        if (!string.IsNullOrWhiteSpace(netInfo.LocalIPv4) && netInfo.LocalIPv4 != "127.0.0.1")
        {
            report.Items.Add(TestUncPathAccess($"\\\\{netInfo.LocalIPv4}\\{shareName}", $"Доступ через локальный IP (\\\\{netInfo.LocalIPv4})"));
        }

        return report;
    }

    private static DiagnosticItem CheckLanmanServerService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var sc = new ServiceController("LanmanServer");
                bool isRunning = sc.Status == ServiceControllerStatus.Running;
                return new DiagnosticItem
                {
                    Name = "Служба SMB (LanmanServer / Сервер)",
                    Status = isRunning ? DiagnosticStatus.Success : DiagnosticStatus.Failure,
                    Details = $"Состояние службы: {sc.Status}",
                    SuggestedFix = isRunning ? null : "Запустите службу 'Сервер' (LanmanServer) через services.msc или команду: net start LanmanServer"
                };
            }
            catch (Exception ex)
            {
                return new DiagnosticItem
                {
                    Name = "Служба SMB (LanmanServer / Сервер)",
                    Status = DiagnosticStatus.Warning,
                    Details = $"Не удалось опросить ServiceController: {ex.Message}",
                    SuggestedFix = "Проверьте статус службы 'Сервер' в оснастке services.msc."
                };
            }
        }

        return new DiagnosticItem
        {
            Name = "Служба SMB (LanmanServer)",
            Status = DiagnosticStatus.Info,
            Details = "Проверка службы доступна в среде Windows."
        };
    }

    private static async Task<DiagnosticItem> CheckSmbPortListeningAsync()
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync("127.0.0.1", 445);
            var completed = await Task.WhenAny(connectTask, Task.Delay(1500));

            if (completed == connectTask && client.Connected)
            {
                return new DiagnosticItem
                {
                    Name = "Порт TCP 445 (SMB Listener)",
                    Status = DiagnosticStatus.Success,
                    Details = "Порт 445 открыт и принимает входящие подключения",
                    SuggestedFix = null
                };
            }

            return new DiagnosticItem
            {
                Name = "Порт TCP 445 (SMB Listener)",
                Status = DiagnosticStatus.Failure,
                Details = "Порт 445 не отвечает на локальные запросы подключения",
                SuggestedFix = "Убедитесь, что служба 'Сервер' запущена и антивирус не блокирует порт 445."
            };
        }
        catch (Exception ex)
        {
            return new DiagnosticItem
            {
                Name = "Порт TCP 445 (SMB Listener)",
                Status = DiagnosticStatus.Failure,
                Details = $"Ошибка при подключении к порту 445: {ex.Message}",
                SuggestedFix = "Проверьте активность службы LanmanServer."
            };
        }
    }

    private static DiagnosticItem TestUncPathAccess(string uncPath, string displayName)
    {
        try
        {
            // Directory.Exists checks if the SMB share path responds and is accessible to current user
            bool accessible = Directory.Exists(uncPath);
            return new DiagnosticItem
            {
                Name = displayName,
                Status = accessible ? DiagnosticStatus.Success : DiagnosticStatus.Warning,
                Details = accessible ? $"Успешный отклик по пути: {uncPath}" : $"Не удалось открыть сетевой каталог: {uncPath}",
                SuggestedFix = accessible ? null : "Возможные причины:\n1. Недостаточно прав у текущего пользователя.\n2. Брандмауэр блокирует обращение по сетевому интерфейсу.\n3. Включен профиль сети 'Общедоступная'."
            };
        }
        catch (Exception ex)
        {
            return new DiagnosticItem
            {
                Name = displayName,
                Status = DiagnosticStatus.Failure,
                Details = $"Ошибка доступа: {ex.Message}",
                SuggestedFix = "Проверьте права учетной записи и сетевые разрешения."
            };
        }
    }
}
