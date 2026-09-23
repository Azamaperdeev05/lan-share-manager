using LANShareManager.Core.Interfaces;

namespace LANShareManager.Infrastructure.Logging;

public class FileLoggerService : ILoggerService
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public FileLoggerService(string? customLogDir = null)
    {
        if (!string.IsNullOrWhiteSpace(customLogDir))
        {
            _logDirectory = customLogDir;
        }
        else
        {
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(programData))
            {
                programData = Path.GetTempPath();
            }
            _logDirectory = Path.Combine(programData, "LANShareManager", "Logs");
        }

        try
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }
        catch
        {
            // Fallback to temp path if ProgramData is inaccessible
            _logDirectory = Path.Combine(Path.GetTempPath(), "LANShareManager", "Logs");
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public string GetLogDirectory() => _logDirectory;

    public void LogInfo(string message) => WriteLog("INFO", message);
    public void LogWarning(string message) => WriteLog("WARN", message);
    public void LogError(string message, Exception? ex = null)
    {
        string fullMessage = ex == null ? message : $"{message} | Exception: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}";
        WriteLog("ERROR", fullMessage);
    }

    private void WriteLog(string level, string message)
    {
        try
        {
            string fileName = $"lan_share_manager_{DateTime.Now:yyyy-MM-dd}.log";
            string filePath = Path.Combine(_logDirectory, fileName);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string line = $"{timestamp} [{level}] {message}";

            lock (_lock)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging should never crash the application
        }
    }
}
