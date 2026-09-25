using System.Text.RegularExpressions;

namespace LANShareManager.Core.Diagnostics;

public class RemediationReport
{
    public string CategoryKey { get; set; } = "GENERAL";
    public string CategoryName { get; set; } = "Жалпы қателік (Общая ошибка)";
    public string FailurePoint { get; set; } = string.Empty;
    public string ErrorTitle { get; set; } = string.Empty;
    public string CauseDescription { get; set; } = string.Empty;
    public List<string> RemediationSteps { get; set; } = new();
    public string? QuickCommand { get; set; }
    public bool CanAutoFix { get; set; }
    public string? AutoFixDescription { get; set; }
}

public static class RemediationAdvisor
{
    public static RemediationReport Analyze(string? errorMessage, Exception? ex = null, string? context = null)
    {
        string combined = $"{errorMessage} {ex?.Message} {ex?.InnerException?.Message} {context}".Trim();

        // 1. UAC / Administrator Privileges
        if (combined.Contains("UnauthorizedAccessException", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Access is denied", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Отказано в доступе", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("requires administrative", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Администратор", StringComparison.OrdinalIgnoreCase) && combined.Contains("прав", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "UAC_ADMIN",
                CategoryName = "Әкімші құқықтары (UAC / Administrator)",
                FailurePoint = "Windows қауіпсіздік жүйесі (UAC)",
                ErrorTitle = "Жүйелік Әкімші (Administrator) құқығы жеткіліксіз",
                CauseDescription = "Windows SMB желілік ортақ папкаларын басқару, NTFS рұқсаттарын жазу және брандмауэр порттарын ашу үшін толық әкімшілік артықшылықтар қажет.",
                RemediationSteps = new List<string>
                {
                    "Ағымдағы PowerShell немесе CMD терезесін жабыңыз.",
                    "PowerShell белгішесін тінтуірдің ОҢ ЖАҒЫМЕН басып: 'Запуск от имени администратора' (Run as Administrator) таңдаңыз.",
                    "Бағдарламаны қайта іске қосыңыз."
                },
                QuickCommand = "powershell -Command \"Start-Process powershell -Verb RunAs\"",
                CanAutoFix = false
            };
        }

        // 2. LanmanServer (Windows Server Service)
        if (combined.Contains("LanmanServer", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Служба Сервер", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Служба не запущена", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("2114", StringComparison.OrdinalIgnoreCase) || // NERR_ServerNotStarted
            combined.Contains("The server service is not started", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "LANMAN_SERVICE",
                CategoryName = "Жүйелік қызмет (LanmanServer / Служба 'Сервер')",
                FailurePoint = "Windows 'Сервер' (LanmanServer) желілік қызметі",
                ErrorTitle = "Файлдық сервер қызметі тоқтатылған немесе іске қосылмаған",
                CauseDescription = "Windows жүйесінде желіге файлдарды таратуға жауапты негізгі 'Сервер' (LanmanServer) қызметі сөніп тұр. Ол қосылмайынша SMB ортақ папкалары жұмыс істемейді.",
                RemediationSteps = new List<string>
                {
                    "Әкімшілік PowerShell терезесін ашып: 'net start LanmanServer' пәрменін орындаңыз.",
                    "Немесе Win+R басып 'services.msc' ашыңыз, тізімнен 'Сервер' қызметін тауып, оны қосыңыз (Тип запуска: Автоматически).",
                    "Қызмет қосылған соң папканы қайта жариялаңыз."
                },
                QuickCommand = "net start LanmanServer",
                CanAutoFix = true,
                AutoFixDescription = "LanmanServer қызметін қазір автоматты түрде қосу"
            };
        }

        // 3. Network Category Public (Общедоступная сеть)
        if (combined.Contains("Public", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Общедоступная", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Общественная", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "NETWORK_PUBLIC",
                CategoryName = "Желі конфигурациясы (Network Category)",
                FailurePoint = "Желілік адаптер профилі (Public Network Profile)",
                ErrorTitle = "Желі профилі 'Общественная сеть' (Public) болып тұр",
                CauseDescription = "Windows қауіпсіздік саясаты бойынша Public желіде басқа компьютерлерге папкалар көрінбейді және кіру бұғатталады. Жергілікті желіде папка көрінуі үшін оны 'Private' (Частная) қылу қажет.",
                RemediationSteps = new List<string>
                {
                    "Желіні 'Частная сеть' (Private) режиміне ауыстырыңыз.",
                    "Оны төмендегі жылдам пәрмен арқылы немесе бағдарлама мәзіріндегі [5] пункт арқылы орындауға болады.",
                    "Windows 'Параметры' -> 'Сеть и интернет' -> Wi-Fi/Ethernet бөлімінде 'Частная сеть' параметрін белгілеңіз."
                },
                QuickCommand = "Set-NetConnectionProfile -InterfaceAlias \"*\" -NetworkCategory Private",
                CanAutoFix = true,
                AutoFixDescription = "Желі профилін Private (Частная) күйіне ауыстыру"
            };
        }

        // 4. Third-party Antivirus / Firewall Interception (Checked before generic Windows Firewall)
        if (combined.Contains("Kaspersky", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("ESET", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Bitdefender", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Norton", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Avast", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Dr.Web", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Сторонний сетевой экран", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "THIRD_PARTY_AV",
                CategoryName = "Сыртқы антивирус / Брандмауэр (Third-Party AV)",
                FailurePoint = "Сыртқы антивирустың дербес желілік экраны",
                ErrorTitle = "Сыртқы антивирус немесе файрвол SMB порттарын (TCP 445) бұғаттауда",
                CauseDescription = "Орнатылған сыртқы антивирус (Kaspersky, ESET, Bitdefender және т.б.) өзінің дербес файрволы арқылы Windows брандмауэрінен тыс желілік портты жауып тастайды.",
                RemediationSteps = new List<string>
                {
                    "Антивирус бағдарламасын ашып, 'Сетевой экран' (Брандмауэр) бөліміне өтіңіз.",
                    "Ағымдағы локалды желі байланысын 'Доверенная сеть' (Сенімді желі) немесе 'Локальная сеть' күйіне ауыстырыңыз.",
                    "TCP 445 порты үшін кіріс пакеттерге рұқсат ережесін қосыңыз."
                },
                QuickCommand = null,
                CanAutoFix = false
            };
        }

        // 5. WMI / CIM Repository Corruption (MAS pattern)
        if (combined.Contains("WMI", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("winmgmt", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("inconsistent", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("ManagementException", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("0x800410", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("0x800440", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "WMI_CORRUPTION",
                CategoryName = "WMI репозиторийі (Windows Management Instrumentation)",
                FailurePoint = "Windows WMI / CIM жүйесі",
                ErrorTitle = "WMI репозиторийі зақымдалған немесе Winmgmt қызметі істен шыққан",
                CauseDescription = "Windows жүйесінде WMI (Windows Management Instrumentation) деректер базасы зақымдалғандықтан, PowerShell және CIM арқылы SMB ресурстарын сұрау немесе басқару сәтсіз аяқталуда.",
                RemediationSteps = new List<string>
                {
                    "Әкімшілік терезеде 'winmgmt /salvagerepository' пәрменін орындап, деректер қорын қалпына келтіріңіз.",
                    "Егер репозиторий қалпына келмесе, 'winmgmt /resetrepository' қолданыңыз.",
                    "'Winmgmt' қызметінің қосылып тұрғанын және авто-іске қосылуын тексеріңіз (sc config Winmgmt start= auto)."
                },
                QuickCommand = "winmgmt /salvagerepository",
                CanAutoFix = true,
                AutoFixDescription = "WMI репозиторийін автоматты түрде қалпына келтіру (winmgmt /salvagerepository)"
            };
        }

        // 6. Firewall / SMB Port 445 blocked
        if (combined.Contains("Firewall", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("Брандмауэр", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("445", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "FIREWALL_445",
                CategoryName = "Брандмауэр (Windows Defender Firewall)",
                FailurePoint = "Желілік брандмауэр порттары (TCP 445 / SMB)",
                ErrorTitle = "Желілік порт 445 немесе брандмауэр ережесі бұғатталған",
                CauseDescription = "Windows кіріс брандмауэрі файлдар мен принтерлерге ортақ кіру порттарын бөгеп тұрғандықтан, басқа құрылғылар бұл компьютерге қосыла алмайды.",
                RemediationSteps = new List<string>
                {
                    "Windows брандмауэрінен 'Общий доступ к файлам и принтерам' ережелер тобын қосыңыз.",
                    "Егер сыртқы антивирус (Kaspersky, ESET, Dr.Web, Norton) болса, оның желі қорғанысынан осы желіні 'Локальная/Доверенная сеть' деп белгілеңіз."
                },
                QuickCommand = "netsh advfirewall firewall set rule group=\"File and Printer Sharing\" new enable=Yes",
                CanAutoFix = true,
                AutoFixDescription = "Windows брандмауэріндегі SMB ережелерін автоматты түрде ашу"
            };
        }

        // 7. System Directory Forbidden (Windows, System32)
        if (combined.Contains("System32", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("каталогу Windows", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("системному каталогу", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "SYSTEM_FOLDER",
                CategoryName = "Қауіпсіздік шектеуі (System Directory Protection)",
                FailurePoint = "Жүйелік каталог қорғанысы",
                ErrorTitle = "C:\\Windows немесе System32 папкаларын ортақ етуге тыйым салынған",
                CauseDescription = "Windows операциялық жүйесінің өзекті жүйелік папкаларын желіге жариялау бүкіл компьютердің бұзылуына немесе қауіпсіздік осалдығына әкеледі.",
                RemediationSteps = new List<string>
                {
                    "Желіге жариялау үшін кәдімгі жұмыс папкасын таңдаңыз (мысалы: C:\\Shares\\SharedDocs немесе D:\\Data).",
                    "Желіде басқа адамдармен бөліскіңіз келетін файлдарды сол жаңа папкаға көшіріңіз."
                },
                CanAutoFix = false
            };
        }

        // 6. Invalid Path or Offline Drive
        if (combined.Contains("Диск", StringComparison.OrdinalIgnoreCase) && combined.Contains("не найден", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("не указан", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("DirectoryNotFoundException", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("DriveNotFound", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "INVALID_DRIVE",
                CategoryName = "Диск пен жол қатесі (Storage & Path)",
                FailurePoint = "Локалды дискілік жол (Drive Path)",
                ErrorTitle = "Көрсетілген диск немесе жол жүйеде табылмады",
                CauseDescription = "Енгізілген дискілік әріп немесе каталог компьютерде жоқ немесе сыртқы флэш-карта/диск ажыратылған.",
                RemediationSteps = new List<string>
                {
                    "'Бұл Компьютер' (Этот компьютер) ашып, дискінің қосылып тұрғанын және әрпінің дұрыстығын тексеріңіз (мысалы: C:\\ немесе D:\\).",
                    "Желілік UNC (\\\\компьютер\\папка) жолын емес, тек локалды диск жолын енгізіңіз.",
                    "Win+R -> diskmgmt.msc ашып, дискілердің күйін тексеріңіз."
                },
                CanAutoFix = false
            };
        }

        // 7. Invalid Share Name or Reserved Share Name
        if (combined.Contains("недопустимые символы", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("зарезервированным", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("ADMIN$", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("IPC$", StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationReport
            {
                CategoryKey = "SHARE_NAME",
                CategoryName = "Ресурс атауының қатесі (Share Name Format)",
                FailurePoint = "SMB ресурсын атау ережелері",
                ErrorTitle = "Ресурс атауында тыйым салынған таңбалар немесе жүйелік атау бар",
                CauseDescription = "SMB протоколында \\ / : * ? \" < > | % + = ; , [ ] таңбаларына және ADMIN$, IPC$, CON, PRN сияқты Windows жүйелік атауларына тыйым салынған.",
                RemediationSteps = new List<string>
                {
                    "Ресурсқа тек қарапайым әріптер, сандар, сызықша және астын сызу таңбаларын қолданыңыз (мысалы: SharedDocs, My_Share, Obshaya).",
                    "Бос орынды атаудың басына немесе соңына қоймаңыз."
                },
                CanAutoFix = false
            };
        }

        // Fallback: General Error
        return new RemediationReport
        {
            CategoryKey = "GENERAL",
            CategoryName = "Жалпы жүйелік қате (General Error)",
            FailurePoint = context ?? "SMB операциясы",
            ErrorTitle = string.IsNullOrWhiteSpace(errorMessage) ? (ex?.Message ?? "Белгісіз қателік") : errorMessage,
            CauseDescription = ex?.Message ?? "Операцияны орындау кезінде күтпеген қателік туындады.",
            RemediationSteps = new List<string>
            {
                "Бағдарламаның лог файлын тексеріңіз (%LOCALAPPDATA%\\LANShareManager\\logs).",
                "Компьютерде 'Служба Сервер' және 'Брандмауэр' қосылып тұрғанын тексеріңіз.",
                "Пәрменді әкімшілік құқықпен (Run as Administrator) қайта қайталап көріңіз."
            },
            CanAutoFix = false
        };
    }
}
