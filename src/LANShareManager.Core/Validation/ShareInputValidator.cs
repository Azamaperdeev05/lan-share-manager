using System.IO;
using System.Text.RegularExpressions;

namespace LANShareManager.Core.Validation;

public enum ValidationSeverity
{
    Success,
    Info,
    Warning,
    Error
}

public class ValidationResult
{
    public bool IsValid => Severity != ValidationSeverity.Error;
    public ValidationSeverity Severity { get; }
    public string? Message { get; }
    public string? Details { get; }

    public ValidationResult(ValidationSeverity severity, string? message = null, string? details = null)
    {
        Severity = severity;
        Message = message;
        Details = details;
    }

    public static ValidationResult Success(string? message = null) => new(ValidationSeverity.Success, message);
    public static ValidationResult Info(string message) => new(ValidationSeverity.Info, message);
    public static ValidationResult Warning(string warning, string? details = null) => new(ValidationSeverity.Warning, warning, details);
    public static ValidationResult Fail(string error, string? details = null) => new(ValidationSeverity.Error, error, details);

    public string? ErrorMessage => Severity == ValidationSeverity.Error ? Message : null;
}

public static class ShareInputValidator
{
    // SMB invalid characters for share names
    private static readonly char[] InvalidShareChars = { '"', '\\', '/', ':', '*', '?', '<', '>', '|', '%', '+', '=', ';', ',', '[', ']' };

    // Reserved DOS device names
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    // Reserved administrative SMB share names
    private static readonly HashSet<string> AdministrativeShares = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADMIN$", "IPC$", "PRINT$", "FAX$", "NETLOGON", "SYSVOL"
    };

    public static ValidationResult ValidateShareName(string? shareName, IEnumerable<string>? existingShareNames = null)
    {
        if (string.IsNullOrWhiteSpace(shareName))
        {
            return ValidationResult.Fail("Имя общего ресурса не может быть пустым.");
        }

        if (shareName.StartsWith(" ") || shareName.EndsWith(" "))
        {
            return ValidationResult.Fail("Имя ресурса не должно начинаться или заканчиваться пробелом.");
        }

        string trimmed = shareName.Trim();

        if (trimmed.Length > 80)
        {
            return ValidationResult.Fail("Длина имени общего ресурса не должна превышать 80 символов.");
        }

        if (trimmed.IndexOfAny(InvalidShareChars) >= 0)
        {
            return ValidationResult.Fail("Имя содержит недопустимые символы SMB: \\ / : * ? \" < > | % + = ; , [ ]");
        }

        if (AdministrativeShares.Contains(trimmed))
        {
            return ValidationResult.Fail($"'{trimmed}' является зарезервированным административным ресурсом Windows.");
        }

        if (ReservedNames.Contains(trimmed))
        {
            return ValidationResult.Fail($"'{trimmed}' является зарезервированным системным именем Windows.");
        }

        // Check if ends with $ (hidden share)
        if (trimmed.EndsWith("$"))
        {
            return ValidationResult.Warning(
                $"Символ '$' в конце имени сделает ресурс '{trimmed}' скрытым.",
                "Скрытый ресурс не отображается в сетевом окружении, подключение к нему возможно только по прямому пути.");
        }

        // Check for collision with existing shares
        if (existingShareNames != null)
        {
            foreach (var existing in existingShareNames)
            {
                if (string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationResult.Warning(
                        $"Ресурс с именем '{trimmed}' уже существует.",
                        "При создании его путь и настройки прав доступа будут обновлены.");
                }
            }
        }

        return ValidationResult.Success("Имя ресурса корректно.");
    }

    public static ValidationResult ValidateFolderPath(string? path, IEnumerable<(string ShareName, string FolderPath)>? existingShares = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Fail("Путь к папке не указан.");
        }

        path = path.Trim();

        if (path.Length < 3)
        {
            return ValidationResult.Fail("Укажите полный путь к папке (например: C:\\Folder).");
        }

        // Check for drive letter root (e.g. C:\) or UNC path
        bool isDriveRoot = Regex.IsMatch(path, @"^[a-zA-Z]:[\\/]");
        bool isUnc = path.StartsWith(@"\\") || path.StartsWith("//");
        bool isRooted = Path.IsPathRooted(path);

        if (isUnc)
        {
            return ValidationResult.Fail("Нельзя расшарить сетевой путь UNC (\\\\...). Выберите локальную папку на этом компьютере.");
        }

        if (!isDriveRoot && !isRooted)
        {
            return ValidationResult.Fail("Путь должен содержать букву локального диска (например: C:\\Folder) и быть абсолютным.");
        }

        char[] invalidPathChars = Path.GetInvalidPathChars();
        if (path.IndexOfAny(invalidPathChars) >= 0)
        {
            return ValidationResult.Fail("Путь содержит недопустимые файловые символы.");
        }

        // Check if drive root exists
        try
        {
            string? driveRoot = Path.GetPathRoot(path);
            if (!string.IsNullOrWhiteSpace(driveRoot))
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    if (!Directory.Exists(driveRoot))
                    {
                        return ValidationResult.Fail($"Диск '{driveRoot}' не найден или отключен в системе.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"Ошибка проверки диска: {ex.Message}");
        }

        // Check if path points to an existing file (not directory)
        if (File.Exists(path))
        {
            return ValidationResult.Fail("Указанный путь является файлом, а не папкой. Выберите папку.");
        }

        // System folders safety check
        string normalized = path.Replace('/', '\\').TrimEnd('\\');

        // Check root drive (e.g. C: or C:\)
        if (Regex.IsMatch(normalized, @"^[a-zA-Z]:$", RegexOptions.IgnoreCase))
        {
            return ValidationResult.Warning(
                $"Выбран корень диска '{path}'.",
                "Предоставление общего доступа к корню диска (особенно системного C:\\) не рекомендуется в целях безопасности.");
        }

        // Check Windows system folders via regex pattern (works for all drive letters and cross-platform)
        if (Regex.IsMatch(normalized, @"^[a-zA-Z]:\\Windows\\System32($|\\)", RegexOptions.IgnoreCase))
        {
            return ValidationResult.Fail("Запрещено создавать общий доступ к каталогу System32.");
        }

        if (Regex.IsMatch(normalized, @"^[a-zA-Z]:\\Windows($|\\)", RegexOptions.IgnoreCase))
        {
            return ValidationResult.Fail("Запрещено создавать общий доступ к системному каталогу Windows в целях безопасности ОС.");
        }

        if (Regex.IsMatch(normalized, @"^[a-zA-Z]:\\Program Files( \(x86\))?($|\\)", RegexOptions.IgnoreCase))
        {
            return ValidationResult.Warning(
                "Вы выбрали каталог Program Files.",
                "Убедитесь, что действительно хотите предоставить сетевой доступ к папке установленных программ.");
        }

        // Additional check using environment special folders on Windows runtime
        try
        {
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows).TrimEnd('\\');
            string sysDir = Environment.GetFolderPath(Environment.SpecialFolder.System).TrimEnd('\\');
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).TrimEnd('\\');

            if (!string.IsNullOrWhiteSpace(winDir) && (normalized.Equals(winDir, StringComparison.OrdinalIgnoreCase) || normalized.StartsWith(winDir + "\\", StringComparison.OrdinalIgnoreCase)))
            {
                return ValidationResult.Fail("Запрещено создавать общий доступ к системному каталогу Windows в целях безопасности ОС.");
            }

            if (!string.IsNullOrWhiteSpace(sysDir) && (normalized.Equals(sysDir, StringComparison.OrdinalIgnoreCase) || normalized.StartsWith(sysDir + "\\", StringComparison.OrdinalIgnoreCase)))
            {
                return ValidationResult.Fail("Запрещено создавать общий доступ к каталогу System32.");
            }

            if (!string.IsNullOrWhiteSpace(progFiles) && normalized.Equals(progFiles, StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Warning(
                    "Вы выбрали каталог Program Files.",
                    "Убедитесь, что действительно хотите предоставить сетевой доступ к папке установленных программ.");
            }
        }
        catch { }

        // Check if folder is already shared
        if (existingShares != null)
        {
            foreach (var (shareName, folderPath) in existingShares)
            {
                if (!string.IsNullOrWhiteSpace(folderPath) &&
                    string.Equals(folderPath.TrimEnd('\\'), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationResult.Info(
                        $"Эта папка уже опубликована в сети под именем '{shareName}'.");
                }
            }
        }

        // Check whether folder already exists or will be created
        if (!Directory.Exists(path))
        {
            return ValidationResult.Info("Папка еще не создана на диске и будет создана автоматически при публикации.");
        }

        return ValidationResult.Success("Папка найдена и готова к публикации.");
    }
}

