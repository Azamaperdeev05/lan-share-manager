namespace LANShareManager.Core.Localization;

public enum AppLanguage
{
    Kazakh,
    Russian,
    English
}

public class ConnectGuide
{
    public string Title { get; set; } = string.Empty;
    public string Step1 { get; set; } = string.Empty;
    public string Step2 { get; set; } = string.Empty;
    public string Step3 { get; set; } = string.Empty;
    public string RecommendedPath { get; set; } = string.Empty;
    public string HostnamePath { get; set; } = string.Empty;
    public string TipCredentials { get; set; } = string.Empty;
    public string TipDriveMap { get; set; } = string.Empty;
    public string CmdNetUse { get; set; } = string.Empty;
}

public static class LocalizationService
{
    public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.Kazakh;

    public static event Action? LanguageChanged;

    public static void SetLanguage(AppLanguage language)
    {
        CurrentLanguage = language;
        LanguageChanged?.Invoke();
    }

    public static ConnectGuide GetConnectGuide(string shareName, string? ip, string? hostname, AppLanguage? lang = null)
    {
        var targetLang = lang ?? CurrentLanguage;
        string safeIp = string.IsNullOrWhiteSpace(ip) ? "192.168.1.X" : ip;
        string safeHost = string.IsNullOrWhiteSpace(hostname) ? Environment.MachineName : hostname;
        string ipPath = $@"\\{safeIp}\{shareName}";
        string hostPath = $@"\\{safeHost}\{shareName}";
        string netUseCmd = $"net use Z: {ipPath} /persistent:yes";

        return targetLang switch
        {
            AppLanguage.Kazakh => new ConnectGuide
            {
                Title = "Басқа компьютерден қалай қосылу керек? (Win + R нұсқаулығы)",
                Step1 = "Басқа компьютерде пернетақтадан Win + R пернелерін басыңыз (Windows-тың «Орындау / Выполнить» терезесі ашылады).",
                Step2 = $"Ашылған жолға мына желілік жолды енгізіңіз немесе көшіріп қойыңыз:\n   {ipPath}",
                Step3 = "Enter (немесе 'ОК') пернесін басыңыз. Ортақ папка лезде ашылады!",
                RecommendedPath = ipPath,
                HostnamePath = hostPath,
                TipCredentials = "Ескерту: Егер Windows логин мен құпиясөз сұраса, осы компьютердің пайдаланушы аты мен паролін енгізіңіз (мысалы: .\\{пайдаланушы}).",
                TipDriveMap = "Кеңес: Папканы тұрақты дискі ретінде бекіту үшін CMD арқылы мына пәрменді орындауға болады:",
                CmdNetUse = netUseCmd
            },
            AppLanguage.English => new ConnectGuide
            {
                Title = "How to connect from another computer? (Win + R Guide)",
                Step1 = "On the other computer, press the Win + R keys on your keyboard (opens the Windows 'Run' dialog).",
                Step2 = $"In the text box, type or paste this network UNC path:\n   {ipPath}",
                Step3 = "Press Enter (or click 'OK'). The shared folder will open immediately!",
                RecommendedPath = ipPath,
                HostnamePath = hostPath,
                TipCredentials = "Note: If Windows prompts for network credentials, enter the username and password of this host computer (e.g. .\\username).",
                TipDriveMap = "Tip: To permanently map this share as a network drive letter (e.g. Z:), run in CMD:",
                CmdNetUse = netUseCmd
            },
            _ => new ConnectGuide
            {
                Title = "Как подключиться с другого компьютера? (Инструкция Win + R)",
                Step1 = "На другом компьютере нажмите сочетание клавиш Win + R на клавиатуре (откроется окно «Выполнить»).",
                Step2 = $"В поле ввода вставьте или введите сетевой путь:\n   {ipPath}",
                Step3 = "Нажмите Enter (или 'ОК'). Общая папка мгновенно откроется в Проводнике!",
                RecommendedPath = ipPath,
                HostnamePath = hostPath,
                TipCredentials = "Примечание: Если Windows запросит логин и пароль, введите имя пользователя и пароль этого ПК (например: .\\ИмяПользователя).",
                TipDriveMap = "Совет: Чтобы подключить папку как постоянный сетевой диск (Z:), выполните в командной строке:",
                CmdNetUse = netUseCmd
            }
        };
    }

    public static string GetString(string key, AppLanguage? lang = null)
    {
        var targetLang = lang ?? CurrentLanguage;
        if (Strings.TryGetValue(key, out var dict) && dict.TryGetValue(targetLang, out var val))
        {
            return val;
        }
        return key;
    }

    private static readonly Dictionary<string, Dictionary<AppLanguage, string>> Strings = new()
    {
        // App Headers & Badges
        ["AppTitle"] = new()
        {
            [AppLanguage.Kazakh] = "LAN Share Manager — SMB ортақ папкаларды басқару",
            [AppLanguage.Russian] = "LAN Share Manager — Управление общими папками SMB",
            [AppLanguage.English] = "LAN Share Manager — Windows SMB File Sharing Tool"
        },
        ["AppSubtitle"] = new()
        {
            [AppLanguage.Kazakh] = "Windows үшін SMB желілік ортақ папкаларын автоматтандырылған баптау",
            [AppLanguage.Russian] = "Автоматизированное создание и управление сетевыми папками SMB для Windows",
            [AppLanguage.English] = "Automated creation and management of Windows SMB network shares"
        },
        ["AdminActive"] = new()
        {
            [AppLanguage.Kazakh] = "✓ Әкімші құқығы: Қосулы",
            [AppLanguage.Russian] = "✓ Администратор: Активен",
            [AppLanguage.English] = "✓ Administrator: Active"
        },
        ["AdminRequired"] = new()
        {
            [AppLanguage.Kazakh] = "⚠ Әкімші құқығы қажет (UAC)",
            [AppLanguage.Russian] = "⚠ Требуются права Администратора",
            [AppLanguage.English] = "⚠ Administrator Rights Required"
        },

        // Network Info Bar
        ["HostName"] = new()
        {
            [AppLanguage.Kazakh] = "Компьютер атауы:",
            [AppLanguage.Russian] = "Имя компьютера (Host):",
            [AppLanguage.English] = "Computer Name (Host):"
        },
        ["LocalIp"] = new()
        {
            [AppLanguage.Kazakh] = "Жергілікті IPv4 (LAN):",
            [AppLanguage.Russian] = "Локальный IPv4 (LAN):",
            [AppLanguage.English] = "Local IPv4 (LAN):"
        },
        ["NetworkProfile"] = new()
        {
            [AppLanguage.Kazakh] = "Желі профилі:",
            [AppLanguage.Russian] = "Профиль сети:",
            [AppLanguage.English] = "Network Profile:"
        },
        ["BtnSwitchPrivate"] = new()
        {
            [AppLanguage.Kazakh] = "Желіні Private қылу",
            [AppLanguage.Russian] = "Сделать сеть частной",
            [AppLanguage.English] = "Switch to Private"
        },
        ["BtnRefreshNetwork"] = new()
        {
            [AppLanguage.Kazakh] = "Жаңарту",
            [AppLanguage.Russian] = "Обновить",
            [AppLanguage.English] = "Refresh"
        },

        // Action Toolbar
        ["SharesTitle"] = new()
        {
            [AppLanguage.Kazakh] = "Сетевые папкалар (SMB Shares):",
            [AppLanguage.Russian] = "Сетевые ресурсы (SMB Shares):",
            [AppLanguage.English] = "Shared Folders (SMB Shares):"
        },
        ["ShowAdminShares"] = new()
        {
            [AppLanguage.Kazakh] = "Жүйелік ресурстарды көрсету (C$, ADMIN$)",
            [AppLanguage.Russian] = "Показать служебные (C$, ADMIN$)",
            [AppLanguage.English] = "Show administrative shares (C$, ADMIN$)"
        },
        ["BtnCreateShare"] = new()
        {
            [AppLanguage.Kazakh] = "+ Жаңа ортақ папка құру",
            [AppLanguage.Russian] = "+ Создать общий доступ",
            [AppLanguage.English] = "+ Create Network Share"
        },
        ["BtnHowToConnect"] = new()
        {
            [AppLanguage.Kazakh] = "🌐 Қалай қосылу керек? (Win+R)",
            [AppLanguage.Russian] = "🌐 Как подключиться? (Win+R)",
            [AppLanguage.English] = "🌐 How to Connect? (Win+R)"
        },

        // DataGrid Columns
        ["ColShareName"] = new()
        {
            [AppLanguage.Kazakh] = "Ресурс атауы",
            [AppLanguage.Russian] = "Имя ресурса",
            [AppLanguage.English] = "Share Name"
        },
        ["ColLocalPath"] = new()
        {
            [AppLanguage.Kazakh] = "Жергілікті жол",
            [AppLanguage.Russian] = "Локальный путь",
            [AppLanguage.English] = "Local Path"
        },
        ["ColAccess"] = new()
        {
            [AppLanguage.Kazakh] = "Қолжетімділік деңгейі",
            [AppLanguage.Russian] = "Уровень доступа",
            [AppLanguage.English] = "Access Level"
        },
        ["ColStatus"] = new()
        {
            [AppLanguage.Kazakh] = "Күйі",
            [AppLanguage.Russian] = "Статус",
            [AppLanguage.English] = "Status"
        },

        // Bottom Actions
        ["BtnOpenFolder"] = new()
        {
            [AppLanguage.Kazakh] = "Папканы ашу",
            [AppLanguage.Russian] = "Открыть папку",
            [AppLanguage.English] = "Open Folder"
        },
        ["BtnCopyPath"] = new()
        {
            [AppLanguage.Kazakh] = "Жолды көшіру",
            [AppLanguage.Russian] = "Копировать путь",
            [AppLanguage.English] = "Copy Path"
        },
        ["BtnEditAccess"] = new()
        {
            [AppLanguage.Kazakh] = "Құқықты өзгерту",
            [AppLanguage.Russian] = "Изменить доступ",
            [AppLanguage.English] = "Edit Access"
        },
        ["BtnDiagnostics"] = new()
        {
            [AppLanguage.Kazakh] = "Диагностика",
            [AppLanguage.Russian] = "Диагностика",
            [AppLanguage.English] = "Diagnostics"
        },
        ["BtnRemove"] = new()
        {
            [AppLanguage.Kazakh] = "Өшіру",
            [AppLanguage.Russian] = "Удалить",
            [AppLanguage.English] = "Delete"
        },
        ["BtnExport"] = new()
        {
            [AppLanguage.Kazakh] = "Экспорт...",
            [AppLanguage.Russian] = "Экспорт...",
            [AppLanguage.English] = "Export..."
        },
        ["BtnImport"] = new()
        {
            [AppLanguage.Kazakh] = "Импорт...",
            [AppLanguage.Russian] = "Импорт...",
            [AppLanguage.English] = "Import..."
        },
        ["BtnLogs"] = new()
        {
            [AppLanguage.Kazakh] = "Логтар",
            [AppLanguage.Russian] = "Журнал логов",
            [AppLanguage.English] = "Logs"
        },
        ["StatusReady"] = new()
        {
            [AppLanguage.Kazakh] = "Дайын",
            [AppLanguage.Russian] = "Готово",
            [AppLanguage.English] = "Ready"
        }
    };
}
