using System.Text.RegularExpressions;

namespace LANShareManager.Core.Validation;

public class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Fail(string error) => new(false, error);
}

public static class ShareInputValidator
{
    private static readonly char[] InvalidShareChars = { '"', '\\', '/', ':', '*', '?', '<', '>', '|', '%' };
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static ValidationResult ValidateShareName(string? shareName)
    {
        if (string.IsNullOrWhiteSpace(shareName))
        {
            return ValidationResult.Fail("Имя общего ресурса не может быть пустым.");
        }

        shareName = shareName.Trim();

        if (shareName.Length > 80)
        {
            return ValidationResult.Fail("Длина имени общего ресурса не должна превышать 80 символов.");
        }

        if (shareName.IndexOfAny(InvalidShareChars) >= 0)
        {
            return ValidationResult.Fail("Имя содержит недопустимые символы: \\ / : * ? \" < > | %");
        }

        if (ReservedNames.Contains(shareName))
        {
            return ValidationResult.Fail($"'{shareName}' является зарезервированным системным именем Windows.");
        }

        return ValidationResult.Success();
    }

    public static ValidationResult ValidateFolderPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationResult.Fail("Путь к папке не указан.");
        }

        path = path.Trim();

        if (path.Length < 3)
        {
            return ValidationResult.Fail("Укажите корректный путь к папке (например: C:\\Folder).");
        }

        // Check for drive letter root (e.g. C:\), UNC (\\server\share), or rooted path
        bool isDriveRoot = Regex.IsMatch(path, @"^[a-zA-Z]:[\\/]");
        bool isUnc = path.StartsWith(@"\\") || path.StartsWith("//");
        bool isRooted = Path.IsPathRooted(path);

        if (!isDriveRoot && !isUnc && !isRooted)
        {
            return ValidationResult.Fail("Путь должен содержать букву диска (например: C:\\Folder) или быть абсолютным.");
        }

        char[] invalidPathChars = Path.GetInvalidPathChars();
        if (path.IndexOfAny(invalidPathChars) >= 0)
        {
            return ValidationResult.Fail("Путь содержит недопустимые файловые символы.");
        }

        return ValidationResult.Success();
    }
}
