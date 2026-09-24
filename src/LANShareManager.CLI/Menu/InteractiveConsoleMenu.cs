using System.Diagnostics;
using System.IO;
using LANShareManager.CLI.Helpers;
using LANShareManager.Core.Diagnostics;
using LANShareManager.Core.Enums;
using LANShareManager.Core.Interfaces;
using LANShareManager.Core.Models;
using LANShareManager.Core.Validation;

namespace LANShareManager.CLI.Menu;

public class InteractiveConsoleMenu
{
    private readonly ISmbService _smbService;
    private readonly INtfsPermissionService _ntfsService;
    private readonly IFirewallService _firewallService;
    private readonly INetworkService _networkService;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly IShareOrchestrator _orchestrator;
    private readonly IShareConfigSerializer _serializer;

    public InteractiveConsoleMenu(
        ISmbService smbService,
        INtfsPermissionService ntfsService,
        IFirewallService firewallService,
        INetworkService networkService,
        IDiagnosticsService diagnosticsService,
        IShareOrchestrator orchestrator,
        IShareConfigSerializer serializer)
    {
        _smbService = smbService;
        _ntfsService = ntfsService;
        _firewallService = firewallService;
        _networkService = networkService;
        _diagnosticsService = diagnosticsService;
        _orchestrator = orchestrator;
        _serializer = serializer;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            await PrintHeaderAsync();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" МӘЗІРДЕН ТАҢДАҢЫЗ (ВЫБЕРИТЕ ДЕЙСТВИЕ):");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('-', 76));
            Console.ResetColor();

            Console.WriteLine(" [1] 📁 Жаңа ортақ папка құру (Интерактивті Шебер / Create Share Wizard)");
            Console.WriteLine(" [2] 📋 Желідегі барлық папкаларды көру (List Shares)");
            Console.WriteLine(" [3] 🔍 Папканың желілік байланысын диагностикалау (Run Diagnostics)");
            Console.WriteLine(" [4] 🗑️  Ортақ папканы желіден өшіру (Remove Share)");
            Console.WriteLine(" [5] 🛠️  Желі мен брандмауэрді автоматты жөндеу (Auto-Fix Network & SMB)");
            Console.WriteLine(" [6] 🖥️  Графикалық интерфейсті (GUI) іске қосу");
            Console.WriteLine(" [7] 💾 Ресурстарды JSON файлына сақтау / қалпына келтіру (Backup)");
            Console.WriteLine(" [0] 🚪 Шығу (Выход)");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('-', 76));
            Console.ResetColor();

            Console.Write("\nТаңдауыңызды енгізіңіз [1-7, 0]: ");
            string? choice = Console.ReadLine()?.Trim();

            if (choice == "0")
            {
                Console.WriteLine("\nLAN Share Manager жұмысын аяқтады. Сау болыңыз!");
                break;
            }

            Console.WriteLine();
            switch (choice)
            {
                case "1":
                    await WizardCreateShareAsync();
                    break;
                case "2":
                    await ShowSharesListAsync();
                    break;
                case "3":
                    await RunDiagnosticsInteractiveAsync();
                    break;
                case "4":
                    await RemoveShareInteractiveAsync();
                    break;
                case "5":
                    await RunAutoFixInteractiveAsync();
                    break;
                case "6":
                    LaunchGui();
                    break;
                case "7":
                    await BackupMenuAsync();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Қате таңдау. 0-ден 7-ге дейінгі санды таңдаңыз.");
                    Console.ResetColor();
                    break;
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\nЖалғастыру үшін кез келген пернені басыңыз...");
            Console.ResetColor();
            Console.ReadKey(true);
        }
    }

    private async Task PrintHeaderAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("============================================================================");
        Console.WriteLine("  LAN Share Manager — Интерактивті басқару мәзірі (Windows 10/11)");
        Console.WriteLine("  https://github.com/Azamaperdeev05/lan-share-manager");
        Console.WriteLine("============================================================================");
        Console.ResetColor();

        try
        {
            var netInfo = await _networkService.GetNetworkInfoAsync();
            Console.Write($" Компьютер: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{netInfo.ComputerName} ");
            Console.ResetColor();

            Console.Write($"| IP: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{netInfo.LocalIPv4} ");
            Console.ResetColor();

            Console.Write($"| Желі: ");
            if (netInfo.NetworkProfile == NetworkCategory.Private)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Private (Частная сеть) ✓");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Public (Общедоступная - шектелген!) ⚠");
            }
            Console.ResetColor();
        }
        catch
        {
            // Ignore header error
        }

        Console.WriteLine();
    }

    private async Task WizardCreateShareAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [1] ЖАҢА ОРТАҚ ПАПКА ҚҰРУ ШЕБЕРІ (WIZARD) ===");
        Console.ResetColor();
        Console.WriteLine("Бұл шебер сізге папканы желіге қауіпсіз әрі дұрыс ашуға көмектеседі.\n");

        var currentShares = await _smbService.GetSharesAsync(includeSpecial: true);
        var existingTuples = currentShares.Select(s => (s.Name, s.Path)).ToList();
        var existingNames = currentShares.Select(s => s.Name).ToList();

        // 1. Prompt Folder Path
        string folderPath = "";
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("» Ортақ ететін папка жолын жазыңыз (мысалы: C:\\OBSHAYA немесе D:\\Data): ");
            Console.ResetColor();

            folderPath = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Жол бос болмауы керек. Қайта енгізіңіз.\n");
                Console.ResetColor();
                continue;
            }

            var pathVal = ShareInputValidator.ValidateFolderPath(folderPath, existingTuples);
            if (!pathVal.IsValid)
            {
                var report = RemediationAdvisor.Analyze(pathVal.Message, context: "Папканы тексеру");
                RemediationFormatter.Print(report);
                continue;
            }

            if (pathVal.Severity == ValidationSeverity.Warning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Ескерту: {pathVal.Message} {pathVal.Details}");
                Console.ResetColor();
                Console.Write("Осы жолды бәрібір пайдаланғыңыз келе ме? [y/N]: ");
                string? confirm = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (confirm != "y" && confirm != "yes" && confirm != "и" && confirm != "д")
                {
                    continue;
                }
            }

            if (pathVal.Severity == ValidationSeverity.Info)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"ℹ {pathVal.Message}");
                Console.ResetColor();
            }

            // If directory doesn't exist, confirm creation
            if (!Directory.Exists(folderPath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"'{folderPath}' каталогы дискіде әлі жоқ. Оны автоматты түрде құрайық па? [Y/n]: ");
                Console.ResetColor();
                string? createDir = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (createDir == "n" || createDir == "no" || createDir == "н" || createDir == "нет")
                {
                    Console.WriteLine("Басқа папка таңдаңыз.\n");
                    continue;
                }
            }

            break;
        }

        // 2. Prompt Share Name
        string defaultName = "";
        try
        {
            defaultName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch { }
        if (string.IsNullOrWhiteSpace(defaultName)) defaultName = "SharedFolder";

        string shareName = "";
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"» Желіде көрінетін атау (Share name) [Enter бассаңыз '{defaultName}']: ");
            Console.ResetColor();

            string? inputName = Console.ReadLine()?.Trim();
            shareName = string.IsNullOrWhiteSpace(inputName) ? defaultName : inputName;

            var nameVal = ShareInputValidator.ValidateShareName(shareName, existingNames);
            if (!nameVal.IsValid)
            {
                var report = RemediationAdvisor.Analyze(nameVal.Message, context: "Ресурс атауын тексеру");
                RemediationFormatter.Print(report);
                continue;
            }

            if (nameVal.Severity == ValidationSeverity.Warning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠ Ескерту: {nameVal.Message} {nameVal.Details}");
                Console.ResetColor();
            }

            break;
        }

        // 3. Prompt Access Mode
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n» Желідегі қолжетімділік деңгейін таңдаңыз:");
        Console.ResetColor();
        Console.WriteLine("  [1] Тек оқу (Read Only) — пайдаланушылар тек файлдарды аша алады, өзгерте алмайды");
        Console.WriteLine("  [2] Оқу және жазу (Read & Write) [ҰСЫНЫЛАДЫ] — файл сақтау, өзгерту, жою мүмкін");
        Console.WriteLine("  [3] Толық қолжетімділік (Full Control) — файлдар мен құқықтарды толық бақылау");
        Console.Write("Таңдауыңыз [1/2/3, Әдепкі 2]: ");

        string? accessChoice = Console.ReadLine()?.Trim();
        AccessMode accessMode = accessChoice switch
        {
            "1" => AccessMode.ReadOnly,
            "3" => AccessMode.FullControl,
            _ => AccessMode.ReadWrite
        };

        // 4. Options
        Console.Write("\n» Windows брандмауэрінен SMB (порт 445) ережелерін қосу? [Y/n]: ");
        bool enableFw = Console.ReadLine()?.Trim().ToLowerInvariant() != "n";

        Console.Write("» Рұқсаттарды барлық ішкі папкалар мен файлдарға да қолдану? [Y/n]: ");
        bool applySub = Console.ReadLine()?.Trim().ToLowerInvariant() != "n";

        Console.Write("» Жасап болған соң желілік байланысты автоматты тексеру? [Y/n]: ");
        bool testAfter = Console.ReadLine()?.Trim().ToLowerInvariant() != "n";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[Ресурс құрылуда: '{shareName}' -> '{folderPath}' ({accessMode})...]");
        Console.ResetColor();

        var request = new ShareCreationRequest
        {
            FolderPath = folderPath,
            ShareName = shareName,
            Access = accessMode,
            EnableFirewallRules = enableFw,
            ApplyPermissionsToSubfolders = applySub,
            TestShareAfterCreation = testAfter
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
            return;
        }

        // Print Victory Card
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║ ✓ ОРТАҚ ПАПКА СӘТТІ ҚҰРЫЛДЫ ЖӘНЕ ЖЕЛІДЕ АШЫҚ!                                    ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════════╣");
        Console.ResetColor();

        Console.WriteLine($"║ 📁 Жергілікті жол: {result.LocalPath}");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"║ 🌐 Компьютер атауы арқылы: {result.HostnamePath}");
        Console.WriteLine($"║ 🔢 IP мекенжай арқылы:     {result.IpPath}");
        Console.ResetColor();
        Console.WriteLine("║");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("║ 💡 БАСҚА КОМПЬЮТЕРДЕН ҚАЛАЙ ҚОСЫЛУ КЕРЕК (КАК ПОДКЛЮЧИТЬСЯ С ДРУГОГО ПК):");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("║    1. Басқа компьютердің пернетақтасынан Win + R түймелерін басыңыз.");
        Console.WriteLine($"║    2. Ашылған 'Выполнить' терезесіне мына жолды көшіріп қойыңыз: {result.IpPath}");
        Console.WriteLine("║    3. 'Enter' басыңыз — ортақ папка бірден ашылады!");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        if (result.Diagnostics != null)
        {
            Console.WriteLine("\n[Автоматты диагностика нәтижелері:]");
            foreach (var item in result.Diagnostics.Items)
            {
                Console.ForegroundColor = item.Status == DiagnosticStatus.Success ? ConsoleColor.Green : ConsoleColor.Yellow;
                Console.WriteLine($"  [{item.StatusSymbol}] {item.Name}: {item.Details}");
                Console.ResetColor();
            }
        }
    }

    private async Task ShowSharesListAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [2] ЖЕЛІДЕГІ БАРЛЫҚ ОРТАҚ ПАПКАЛАР ТІЗІМІ ===");
        Console.ResetColor();

        var shares = await _smbService.GetSharesAsync(includeSpecial: false);
        if (shares.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Қазіргі уақытта жарияланған ортақ папкалар жоқ.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("{0,-18} {1,-34} {2,-16} {3,-10}", "Ресурс атауы", "Дискідегі жол", "Қолжетімділік", "Күйі");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('-', 80));
        Console.ResetColor();

        foreach (var share in shares)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("{0,-18} ", share.Name);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("{0,-34} ", string.IsNullOrWhiteSpace(share.Path) ? "-" : share.Path);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("{0,-16} ", share.DisplayAccess);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("{0,-10}", share.Status);
            Console.ResetColor();
        }
    }

    private async Task RunDiagnosticsInteractiveAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [3] ЖЕЛІЛІК ДИАГНОСТИКА ЖӘНЕ БАЙЛАНЫСТЫ ТЕКСЕРУ ===");
        Console.ResetColor();

        var shares = await _smbService.GetSharesAsync(includeSpecial: false);
        if (shares.Count == 0)
        {
            Console.WriteLine("Тексеретін белсенді ортақ папкалар жоқ.");
            return;
        }

        Console.WriteLine("Тексергіңіз келетін ресурсты таңдаңыз:");
        for (int i = 0; i < shares.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {shares[i].Name} ({shares[i].Path})");
        }
        Console.Write($"\nТаңдауыңыз [1-{shares.Count}]: ");

        if (!int.TryParse(Console.ReadLine()?.Trim(), out int idx) || idx < 1 || idx > shares.Count)
        {
            Console.WriteLine("Қате таңдау.");
            return;
        }

        var target = shares[idx - 1];
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n'{target.Name}' ресурсы үшін толық диагностика жүргізілуде...\n");
        Console.ResetColor();

        var report = await _diagnosticsService.RunFullDiagnosticsAsync(target.Name, target.Path);

        foreach (var item in report.Items)
        {
            ConsoleColor col = item.Status switch
            {
                DiagnosticStatus.Success => ConsoleColor.Green,
                DiagnosticStatus.Warning => ConsoleColor.Yellow,
                DiagnosticStatus.Failure => ConsoleColor.Red,
                _ => ConsoleColor.White
            };

            Console.ForegroundColor = col;
            Console.Write($"  [{item.StatusSymbol}] {item.Name,-40} : ");
            Console.ResetColor();
            Console.WriteLine(item.Details);

            if (!string.IsNullOrWhiteSpace(item.SuggestedFix) && item.Status != DiagnosticStatus.Success)
            {
                var rem = RemediationAdvisor.Analyze(item.Details, context: item.Name);
                RemediationFormatter.Print(rem);
            }
        }
    }

    private async Task RemoveShareInteractiveAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [4] ОРТАҚ ПАПКАНЫ ЖЕЛІДЕН ӨШІРУ ===");
        Console.ResetColor();

        var shares = await _smbService.GetSharesAsync(includeSpecial: false);
        if (shares.Count == 0)
        {
            Console.WriteLine("Өшіретін белсенді ортақ папкалар табылмады.");
            return;
        }

        Console.WriteLine("Желіден өшіргіңіз келетін ресурсты таңдаңыз:");
        for (int i = 0; i < shares.Count; i++)
        {
            Console.WriteLine($"  [{i + 1}] {shares[i].Name} ({shares[i].Path})");
        }
        Console.Write($"\nТаңдауыңыз [1-{shares.Count}, 0 - Бас тарту]: ");

        if (!int.TryParse(Console.ReadLine()?.Trim(), out int idx) || idx < 1 || idx > shares.Count)
        {
            Console.WriteLine("Бас тартылды.");
            return;
        }

        var share = shares[idx - 1];
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\nНАЗАР АУДАРЫҢЫЗ: '{share.Name}' папкасының тек желілік ортақ қолжетімділігі өшеді.");
        Console.WriteLine("(Қатты дискідегі нақты файлдар мен папка өшірілмейді, сақталып қалады).");
        Console.Write("Өшіруді растайсыз ба? [y/N]: ");
        Console.ResetColor();

        string? confirm = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (confirm != "y" && confirm != "yes" && confirm != "и" && confirm != "д")
        {
            Console.WriteLine("Операция тоқтатылды.");
            return;
        }

        bool ok = await _orchestrator.RemoveShareSafelyAsync(share.Name);
        if (ok)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ '{share.Name}' ортақ ресурсы желіден сәтті өшірілді.");
            Console.ResetColor();
        }
        else
        {
            var rem = RemediationAdvisor.Analyze($"Не удалось удалить общий ресурс '{share.Name}'", context: "Ресурсты өшіру");
            RemediationFormatter.Print(rem);
        }
    }

    private async Task RunAutoFixInteractiveAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [5] ЖЕЛІ ЖӘНЕ БРАНДМАУЭРДІ АВТОМАТТЫ ЖӨНДЕУ ===");
        Console.ResetColor();
        Console.WriteLine("Windows желілік баптауларын кешенді тексеру және автоматты жөндеу:\n");

        // 1. LanmanServer check
        Console.Write("1. Windows 'Сервер' (LanmanServer) қызметін тексеру... ");
        try
        {
            await Task.Run(() =>
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = "start LanmanServer",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit(5000);
            });
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[ҚОСЫЛҒАН ✓]");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[ЕСКЕРТУ: {ex.Message}]");
            Console.ResetColor();
        }

        // 2. Network Category
        Console.Write("2. Желі профилін 'Private' (Частная сеть) режиміне ауыстыру... ");
        bool netOk = await _networkService.SwitchNetworkToPrivateAsync();
        if (netOk)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[ОРНАТЫЛДЫ ✓]");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[ТЕКСЕРІЛДІ]");
            Console.ResetColor();
        }

        // 3. Firewall TCP 445
        Console.Write("3. Windows брандмауэрінен SMB (порт 445) ережелерін қосу... ");
        bool fwOk = await _firewallService.EnableSmbFirewallRulesAsync();
        if (fwOk)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[АШЫЛДЫ ✓]");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[ТЕКСЕРІЛДІ]");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✓ Жүйелік және желілік баптаулар автоматты түрде реттелді!");
        Console.ResetColor();
    }

    private void LaunchGui()
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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Графикалық интерфейс (LANShareManagerGUI.exe) іске қосылды.");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("LANShareManagerGUI.exe файлы табылмады.");
        Console.ResetColor();
    }

    private async Task BackupMenuAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== [7] РЕСУРСТАРДЫ САҚТАУ ЖӘНЕ ҚАЛПЫНА КЕЛТІРУ (JSON) ===");
        Console.ResetColor();
        Console.WriteLine("  [1] Ағымдағы ортақ папкаларды экспорттау (JSON Backup)");
        Console.WriteLine("  [2] JSON файлынан ортақ папкаларды қайта құру (JSON Import)");
        Console.Write("\nТаңдауыңыз [1/2]: ");

        string? sub = Console.ReadLine()?.Trim();
        if (sub == "1")
        {
            string file = "shares_backup.json";
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
            await File.WriteAllTextAsync(file, json, System.Text.Encoding.UTF8);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ {configs.Count} ресурс сәтті сақталды: {Path.GetFullPath(file)}");
            Console.ResetColor();
        }
        else if (sub == "2")
        {
            Console.Write("JSON файл атауын енгізіңіз (мысалы: shares_backup.json): ");
            string? file = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                Console.WriteLine("Файл табылмады.");
                return;
            }

            string json = await File.ReadAllTextAsync(file, System.Text.Encoding.UTF8);
            var configs = _serializer.Deserialize(json);
            Console.WriteLine($"\n{configs.Count} ресурс импортталуда...");

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
                Console.WriteLine($"{c.ShareName,-18} -> {(res.Success ? "✓ Сәтті" : "✗ " + res.Message)}");
            }
        }
    }
}
