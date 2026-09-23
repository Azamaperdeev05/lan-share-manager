using System.Diagnostics;
using System.Text;
using LANShareManager.Core.Interfaces;

namespace LANShareManager.Infrastructure.Process;

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool Success => ExitCode == 0;
}

public class PowerShellProcessRunner
{
    private readonly ILoggerService? _logger;

    public PowerShellProcessRunner(ILoggerService? logger = null)
    {
        _logger = logger;
    }

    public async Task<ProcessResult> RunPowerShellCommandAsync(string command, int timeoutSeconds = 30)
    {
        _logger?.LogInfo($"Executing PowerShell: {command}");

        // Base64 encode command to avoid quoting/escaping problems in cmd / powershell
        byte[] bytes = Encoding.Unicode.GetBytes(command);
        string encodedCommand = Convert.ToBase64String(bytes);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            await process.WaitForExitAsync(cts.Token);

            string output = outputBuilder.ToString().Trim();
            string error = errorBuilder.ToString().Trim();

            if (process.ExitCode != 0)
            {
                _logger?.LogWarning($"PowerShell exited with code {process.ExitCode}. Error: {error}");
            }

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = output,
                StandardError = error
            };
        }
        catch (OperationCanceledException)
        {
            _logger?.LogError($"PowerShell command timed out after {timeoutSeconds}s: {command}");
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = $"Команда PowerShell прервана по таймауту ({timeoutSeconds} сек)."
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to execute PowerShell command: {command}", ex);
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = ex.Message
            };
        }
    }

    public async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, int timeoutSeconds = 30)
    {
        _logger?.LogInfo($"Executing process: {fileName} {arguments}");

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        try
        {
            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            await process.WaitForExitAsync(cts.Token);

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString().Trim(),
                StandardError = errorBuilder.ToString().Trim()
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to execute process {fileName} {arguments}", ex);
            return new ProcessResult
            {
                ExitCode = -1,
                StandardError = ex.Message
            };
        }
    }
}
