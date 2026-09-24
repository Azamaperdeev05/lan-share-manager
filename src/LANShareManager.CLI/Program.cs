using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using LANShareManager.CLI.Helpers;
using LANShareManager.CLI.Menu;
using LANShareManager.Core.Diagnostics;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;
using LANShareManager.Core.Serialization;
using LANShareManager.Infrastructure.Diagnostics;
using LANShareManager.Infrastructure.Firewall;
using LANShareManager.Infrastructure.Logging;
using LANShareManager.Infrastructure.Network;
using LANShareManager.Infrastructure.NTFS;
using LANShareManager.Infrastructure.Orchestration;
using LANShareManager.Infrastructure.Process;
using LANShareManager.Infrastructure.SMB;

namespace LANShareManager.CLI;

public class Program
{
    private static ILoggerService _logger = null!;
    private static ISmbService _smbService = null!;
    private static INtfsPermissionService _ntfsService = null!;
    private static IFirewallService _firewallService = null!;
    private static INetworkService _networkService = null!;
    private static IDiagnosticsService _diagnosticsService = null!;
    private static IShareOrchestrator _orchestrator = null!;
    private static IShareConfigSerializer _serializer = null!;

    public static async Task<int> Main(string[] args)
    {
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        InitializeServices();

        // 1. If run without arguments, or with --menu / -i, launch full interactive wizard menu!
        if (args.Length == 0 || args[0].Equals("--menu", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-i", StringComparison.OrdinalIgnoreCase))
        {
            var menu = new InteractiveConsoleMenu(
                _smbService,
                _ntfsService,
                _firewallService,
                _networkService,
                _diagnosticsService,
                _orchestrator,
                _serializer);
            await menu.RunAsync();
            return 0;
        }

        // Print header for command line execution
        PrintBanner();

        if (args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            return 0;
        }

        string command = args[0].ToLowerInvariant();

        try
        {
            return command switch
            {
                "create" => await HandleCreateAsync(args.Skip(1).ToArray()),
                "list" => await HandleListAsync(args.Skip(1).ToArray()),
                "test" => await HandleTestAsync(args.Skip(1).ToArray()),
                "remove" => await HandleRemoveAsync(args.Skip(1).ToArray()),
                "export" => await HandleExportAsync(args.Skip(1).ToArray()),
                "import" => await HandleImportAsync(args.Skip(1).ToArray()),
                "gui" => HandleLaunchGui(),
                _ => HandleUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            var report = RemediationAdvisor.Analyze(ex.Message, ex, command);
            RemediationFormatter.Print(report);

            if (report.CanAutoFix)
            {
                await RemediationFormatter.OfferAutoFixAsync(
                    report,
                    _firewallService,
                    _networkService,
                    async () => await Task.CompletedTask);
            }

            _logger.LogError($"CLI command '{command}' unhandled exception", ex);
            return 1;
        }
    }

    private static void InitializeServices()
    {
        _logger = new FileLoggerService();
        var processRunner = new PowerShellProcessRunner(_logger);
        _smbService = new WindowsSmbService(_logger, processRunner);
#pragma warning disable CA1416
        _ntfsService = new NtfsPermissionService(_logger, processRunner);
#pragma warning restore CA1416
        _firewallService = new WindowsFirewallService(_logger, processRunner);
        _networkService = new WindowsNetworkService(_logger, processRunner);
        _diagnosticsService = new WindowsDiagnosticsService(_smbService, _ntfsService, _firewallService, _networkService, _logger);
        _orchestrator = new ShareOrchestrator(_smbService, _ntfsService, _firewallService, _networkService, _diagnosticsService, _logger);
        _serializer = new ShareConfigSerializer();
    }

    private static bool IsAdministrator()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
        return true;
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine(" LAN Share Manager - Управление общими папками SMB");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        bool isAdmin = IsAdministrator();
        Console.Write("Привилегии администратора: ");
        if (isAdmin)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Включены (Administrator)");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠ Требуются права администратора для изменения SMB/NTFS/Firewall");
        }
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Использование:");
        Console.WriteLine("  LANShareManager create --path <путь> --name <имя> [--access <readonly|readwrite|full>] [--no-firewall] [--no-subfolders] [--no-test]");
        Console.WriteLine("  LANShareManager list [--all]");
        Console.WriteLine("  LANShareManager test --name <имя> [--path <путь>]");
        Console.WriteLine("  LANShareManager remove --name <имя> [--force]");
        Console.WriteLine("  LANShareManager export --output <файл.json>");
        Console.WriteLine("  LANShareManager import --input <файл.json>");
        Console.WriteLine("  LANShareManager gui");
        Console.WriteLine();
        Console.WriteLine("Примеры:");
        Console.WriteLine("  LANShareManager create --path \"C:\\OBSHAYA\" --name \"OBSHAYA\" --access readwrite");
        Console.WriteLine("  LANShareManager test --name \"OBSHAYA\"");
        Console.WriteLine("  LANShareManager list");
        Console.WriteLine("  LANShareManager remove --name \"OBSHAYA\"");
    }

    private static async Task<int> HandleCreateAsync(string[] args)
    {
        var dict = ParseArgs(args);
        if (!dict.TryGetValue("path", out var path) || !dict.TryGetValue("name", out var name))
        {
            var report = RemediationAdvisor.Analyze("Аргументы --path и --name обязательны.", context: "Команда 'create'");
            RemediationFormatter.Print(report);
            Console.WriteLine("Пример: LANShareManager create --path \"C:\\OBSHAYA\" --name \"OBSHAYA\" --access readwrite\n");
            return 1;
        }

        AccessMode access = AccessMode.ReadWrite;
        if (dict.TryGetValue("access", out var accessStr))
        {
            access = accessStr.ToLowerInvariant() switch
            {
                "readonly" or "read" => AccessMode.ReadOnly,
                "readwrite" or "write" or "change" => AccessMode.ReadWrite,
                "full" or "fullcontrol" => AccessMode.FullControl,
                _ => AccessMode.ReadWrite
            };
        }

        bool enableFirewall = !dict.ContainsKey("no-firewall");
        bool applySubfolders = !dict.ContainsKey("no-subfolders");
        bool testShare = !dict.ContainsKey("no-test");

        Console.WriteLine($"Создание общего ресурса '{name}' для папки '{path}'...");
        Console.WriteLine($"Уровень доступа: {access} | Брандмауэр: {enableFirewall} | Подпапки: {applySubfolders}\n");

        var request = new ShareCreationRequest
        {
            FolderPath = Path.GetFullPath(path),
            ShareName = name,
            Access = access,
            EnableFirewallRules = enableFirewall,
            ApplyPermissionsToSubfolders = applySubfolders,
            TestShareAfterCreation = testShare
        };

        var result = await _orchestrator.CreateOrConfigureShareAsync(request);

        if (!result.Success)
        {
            var report = RemediationAdvisor.Analyze(result.Message, context: "Ортақ ресурс жасау");
            RemediationFormatter.Print(report);

            if (report.CanAutoFix)
            {
                await RemediationFormatter.OfferAutoFixAsync(
                    report,
                    _firewallService,
                    _networkService,
                    async () => await Task.CompletedTask);
            }
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ Ресурс успешно создан и настроен!\n");
        Console.ResetColor();

        Console.WriteLine($"Локальный путь:  {result.LocalPath}");
        Console.WriteLine($"Сетевой путь:    {result.HostnamePath}");
        Console.WriteLine($"IP путь:         {result.IpPath}\n");

        Console.WriteLine($"SMB служба:       {(result.SmbEnabled ? "✓ Включено" : "✗ Ошибка")}");
        Console.WriteLine($"Права SMB:        {(result.SharePermissionsConfigured ? "✓ Настроены" : "✗ Ошибка")}");
        Console.WriteLine($"Права NTFS:       {(result.NtfsPermissionsConfigured ? "✓ Настроены" : "✗ Ошибка")}");
        Console.WriteLine($"Брандмауэр:       {(result.FirewallConfigured ? "✓ Настроен" : "- Пропущен")}");

        if (result.Diagnostics != null)
        {
            Console.WriteLine("\nРезультаты диагностики подключения:");
            PrintDiagnosticReport(result.Diagnostics);
        }

        return 0;
    }

    private static async Task<int> HandleListAsync(string[] args)
    {
        var dict = ParseArgs(args);
        bool includeSpecial = dict.ContainsKey("all");

        Console.WriteLine("Запрос списка активных SMB ресурсов...\n");
        var shares = await _smbService.GetSharesAsync(includeSpecial);

        if (shares.Count == 0)
        {
            Console.WriteLine("Нет активных общих папок.");
            return 0;
        }

        Console.WriteLine("{0,-16} {1,-32} {2,-18} {3,-10}", "Имя ресурса", "Локальный путь", "Доступ", "Статус");
        Console.WriteLine(new string('-', 80));

        foreach (var share in shares)
        {
            Console.WriteLine("{0,-16} {1,-32} {2,-18} {3,-10}",
                share.Name,
                string.IsNullOrWhiteSpace(share.Path) ? "-" : share.Path,
                share.DisplayAccess,
                share.Status);
        }

        return 0;
    }

    private static async Task<int> HandleTestAsync(string[] args)
    {
        var dict = ParseArgs(args);
        if (!dict.TryGetValue("name", out var name))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ошибка: Укажите имя ресурса (--name).");
            Console.ResetColor();
            return 1;
        }

        dict.TryGetValue("path", out var path);
        if (string.IsNullOrWhiteSpace(path))
        {
            var share = await _smbService.GetShareByNameAsync(name);
            path = share?.Path ?? string.Empty;
        }

        Console.WriteLine($"Запуск диагностики для ресурса '{name}'...\n");
        var report = await _diagnosticsService.RunFullDiagnosticsAsync(name, path);
        PrintDiagnosticReport(report);

        return report.OverallSuccess ? 0 : 1;
    }

    private static async Task<int> HandleRemoveAsync(string[] args)
    {
        var dict = ParseArgs(args);
        if (!dict.TryGetValue("name", out var name))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ошибка: Укажите имя ресурса (--name).");
            Console.ResetColor();
            return 1;
        }

        bool force = dict.ContainsKey("force");
        if (!force)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"ВНИМАНИЕ: Вы действительно хотите удалить общий сетевой доступ '{name}'?");
            Console.WriteLine("(Папка и файлы на жестком диске НЕ будут удалены).");
            Console.Write("Подтвердите действие [y/N]: ");
            Console.ResetColor();

            string? confirm = Console.ReadLine();
            if (!string.Equals(confirm?.Trim(), "y", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(confirm?.Trim(), "yes", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(confirm?.Trim(), "д", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Операция отменена пользователем.");
                return 0;
            }
        }

        bool success = await _orchestrator.RemoveShareSafelyAsync(name);
        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Общий ресурс '{name}' успешно удален из сети.");
            Console.ResetColor();
            return 0;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ Не удалось удалить общий ресурс '{name}'.");
        Console.ResetColor();
        return 1;
    }

    private static async Task<int> HandleExportAsync(string[] args)
    {
        var dict = ParseArgs(args);
        dict.TryGetValue("output", out var outputFile);
        outputFile ??= "shares_export.json";

        var shares = await _smbService.GetSharesAsync(includeSpecial: false);
        var configs = shares.Select(s => new ShareExportConfig
        {
            ShareName = s.Name,
            Path = s.Path,
            Access = s.Access.ToString(),
            Firewall = true,
            ApplyToSubfolders = true
        }).ToList();

        string json = _serializer.Serialize(configs);
        await File.WriteAllTextAsync(outputFile, json, Encoding.UTF8);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Экспорт {configs.Count} ресурсов успешно сохранен в файл: {outputFile}");
        Console.ResetColor();
        return 0;
    }

    private static async Task<int> HandleImportAsync(string[] args)
    {
        var dict = ParseArgs(args);
        if (!dict.TryGetValue("input", out var inputFile) || !File.Exists(inputFile))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ошибка: Укажите существующий файл конфигурации (--input <file.json>).");
            Console.ResetColor();
            return 1;
        }

        string json = await File.ReadAllTextAsync(inputFile, Encoding.UTF8);
        var configs = _serializer.Deserialize(json);
        if (configs.Count == 0)
        {
            Console.WriteLine("В указанном файле не найдено корректных конфигураций ресурсов.");
            return 1;
        }

        Console.WriteLine($"Найдено {configs.Count} записей для импорта. Применяем конфигурацию...\n");
        foreach (var c in configs)
        {
            Enum.TryParse<AccessMode>(c.Access, true, out var access);
            var req = new ShareCreationRequest
            {
                FolderPath = c.Path,
                ShareName = c.ShareName,
                Access = access,
                EnableFirewallRules = c.Firewall,
                ApplyPermissionsToSubfolders = c.ApplyToSubfolders,
                TestShareAfterCreation = false
            };
            var res = await _orchestrator.CreateOrConfigureShareAsync(req);
            Console.WriteLine($"{c.ShareName,-16} -> {(res.Success ? "✓ Успешно" : "✗ " + res.Message)}");
        }

        return 0;
    }

    private static int HandleLaunchGui()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        string guiExe = Path.Combine(dir, "LANShareManagerGUI.exe");

        if (File.Exists(guiExe))
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = guiExe,
                UseShellExecute = true
            });
            return 0;
        }

        Console.WriteLine("Исполняемый файл GUI (LANShareManagerGUI.exe) не найден рядом с CLI.");
        return 1;
    }

    private static int HandleUnknownCommand(string command)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Неизвестная команда: {command}");
        Console.ResetColor();
        PrintUsage();
        return 1;
    }

    private static void PrintDiagnosticReport(DiagnosticReport report)
    {
        foreach (var item in report.Items)
        {
            ConsoleColor color = item.Status switch
            {
                DiagnosticStatus.Success => ConsoleColor.Green,
                DiagnosticStatus.Warning => ConsoleColor.Yellow,
                DiagnosticStatus.Failure => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = color;
            Console.Write($"[{item.StatusSymbol}] {item.Name,-42} : ");
            Console.ResetColor();
            Console.WriteLine(item.Details);

            if (!string.IsNullOrWhiteSpace(item.SuggestedFix) && item.Status != DiagnosticStatus.Success)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"    Рекомендация: {item.SuggestedFix}");
                Console.ResetColor();
            }
        }
        Console.WriteLine();
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.StartsWith("--"))
            {
                string key = arg.Substring(2);
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    dict[key] = args[i + 1];
                    i++;
                }
                else
                {
                    dict[key] = "true";
                }
            }
        }
        return dict;
    }
}
